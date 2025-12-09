using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data
{
	public class AppDbContext : IdentityDbContext<SiteUser>
	{
		public DbSet<MainActivity> Activities { get; set; }
		public DbSet<UserRelation> UserRelations { get; set; }
		public DbSet<SiteUser> SiteUsers { get; set; }
		public DbSet<PrivacySettings> PrivacySettings { get; set; }
		public DbSet<ActivityImage> ActivityImages { get; set; }


		//groups
		public DbSet<Group> Groups { get; set; }
		public DbSet<GroupMembership> GroupMemberships { get; set; }
		public DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }
		public DbSet<GroupPost> GroupPosts { get; set; }

		//comments
		public DbSet<CommentBase> Comments { get; set; }
		public DbSet<ActivityComment> ActivityComments { get; set; }
		public DbSet<GroupPostComment> GroupPostComments { get; set; }

		public DbSet<GroupEvent> GroupEvents { get; set; }
		public DbSet<GroupEventParticipant> GroupEventParticipants { get; set; } = null!;

		public AppDbContext(DbContextOptions<AppDbContext> opt) :base(opt)
		{

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder); 


			modelBuilder.Entity<MainActivity>().ToTable("MainActivities", "Activities");

			modelBuilder.Entity<GpsRelatedActivity>().ToTable("GpsRelatedActivities", "Activities");

			modelBuilder.Entity<Running>().ToTable("Running", "Activities");
			modelBuilder.Entity<Hiking>().ToTable("Hiking", "Activities");
			modelBuilder.Entity<Bouldering>().ToTable("Bouldering", "Activities");


			// Add indexes for UserId and paging
			// affected emthods: GetByUserIdPagedAsync (UserId, Date, Id) ; GetPagedAsync (Date, Id) ; GetFeedPagedAsync (Date, Id)
			modelBuilder.Entity<MainActivity>(b =>
			{
				b.HasIndex(a => a.UserId)
				 .HasDatabaseName("IX_MainActivities_UserId");

				// user profile listing: WHERE UserId = ? ORDER BY Date, Id
				b.HasIndex(a => new { a.UserId, a.Date, a.Id })
				 .HasDatabaseName("IX_MainActivities_User_Date_Id");

				// global / friends feed ordering by Date, Id
				b.HasIndex(a => new { a.Date, a.Id })
				 .HasDatabaseName("IX_MainActivities_Date_Id");
			});


			modelBuilder.Entity<MainActivity>()
				.HasOne(t => t.User)
				.WithMany(u => u.Activities)
				.HasForeignKey(t => t.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<MainActivity>()
				.HasMany(a => a.LikedBy)
				.WithMany(u => u.LikedPosts)
				.UsingEntity<Dictionary<string, object>>(
					"ActivityLikes",
					j => j
						.HasOne<SiteUser>()
						.WithMany()
						.HasForeignKey("LikedByUsersId")
						.HasConstraintName("FK_ActivityLikes_AspNetUsers_LikedByUsersId")
						.OnDelete(DeleteBehavior.Cascade),
					j => j
						.HasOne<MainActivity>()
						.WithMany()
						.HasForeignKey("LikedPostsId")
						.HasConstraintName("FK_ActivityLikes_MainActivities_LikedPostsId")
						.OnDelete(DeleteBehavior.Restrict),
					j =>
					{
						j.ToTable("ActivityLikes");
						j.HasKey("LikedByUsersId", "LikedPostsId");
					});

			modelBuilder.Entity<SiteUser>()
				.Property(u => u.Bio)
				.HasDefaultValue(string.Empty);


			#region images
			modelBuilder.Entity<ActivityImage>()
				.HasKey(x => x.Id);

			modelBuilder.Entity<ActivityImage>()
				.Property(x => x.Id)
				.ValueGeneratedNever();

			modelBuilder.Entity<ActivityImage>()
				   .HasIndex(x => x.ActivityId);

			modelBuilder.Entity<ActivityImage>()
				.HasOne(x => x.Activity)
				.WithMany(a => a.Images)
				.HasForeignKey(x => x.ActivityId)
				.OnDelete(DeleteBehavior.Cascade);

			#endregion


			// for the SearchByNamePagedAsync method 
			modelBuilder.Entity<SiteUser>(b =>
			{
				b.HasIndex(u => new { u.LastName, u.FirstName, u.Id })
				 .HasDatabaseName("IX_SiteUsers_Last_First_Id");
			});


			modelBuilder.Entity<UserRelation>(t =>
			{
				t.HasKey(f => new { f.UserId, f.FriendId });

				t.HasOne(f => f.User)
				   .WithMany(u => u.FriendRequestInitiated)
				   .HasForeignKey(f => f.UserId)
				   .OnDelete(DeleteBehavior.NoAction);

				t.HasOne(f => f.Friend)
				   .WithMany(u => u.FriendRequestReceived)
				   .HasForeignKey(f => f.FriendId)
				   .OnDelete(DeleteBehavior.NoAction);

				// prevent self-friendship
				t.HasCheckConstraint("CK_Friendship_NotSelf", "[UserId] <> [FriendId]");
				t.HasIndex(f => new { f.UserId, f.Status, f.FriendId });
				t.HasIndex(f => new { f.FriendId, f.Status, f.UserId });
			});


			// 1:1 SiteUser <-> PrivacySettings with unique FK
			modelBuilder.Entity<PrivacySettings>()
				.HasIndex(p => p.UserId)
				.IsUnique();

			modelBuilder.Entity<PrivacySettings>()
				.HasOne(p => p.User)
				.WithOne(u => u.Privacy)
				.HasForeignKey<PrivacySettings>(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);


			// Groups 
			modelBuilder.Entity<Group>(b =>
			{
				b.ToTable("Groups", schema: "Social");
				b.HasKey(x => x.Id);
				b.Property(x => x.Name).IsRequired().HasMaxLength(128);
				b.Property(x => x.Description).HasMaxLength(2000);
				b.Property(x => x.IsOpen).HasDefaultValue(true);
				b.HasIndex(x => x.Name);

				b.HasOne(x => x.CreatedByUser)
				 .WithMany()
				 .HasForeignKey(x => x.CreatedByUserId)
				 .OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<GroupMembership>(b =>
			{
				b.ToTable("GroupMemberships", "Social");
				b.HasKey(x => new { x.GroupId, x.UserId });
				b.Property(x => x.Role).HasConversion<int>();
				b.Property(x => x.JoinedAtUtc).IsRequired();

				b.HasOne(x => x.Group)
				 .WithMany(g => g.Members)
				 .HasForeignKey(x => x.GroupId)
				 .OnDelete(DeleteBehavior.Cascade);

				b.HasOne(x => x.User)
				 .WithMany()
				 .HasForeignKey(x => x.UserId)
				 .OnDelete(DeleteBehavior.Restrict);

				b.HasIndex(x => new { x.GroupId, x.Role });
			});

			modelBuilder.Entity<GroupJoinRequest>(b =>
			{
				b.ToTable("GroupJoinRequests", "Social");
				b.HasKey(x => x.Id);
				b.Property(x => x.Status).HasConversion<int>();
				b.Property(x => x.CreatedAtUtc).IsRequired();

				b.HasOne(x => x.Group)
				 .WithMany(g => g.JoinRequests)
				 .HasForeignKey(x => x.GroupId)
				 .OnDelete(DeleteBehavior.Cascade);

				b.HasOne(x => x.RequesterUser)
				 .WithMany()
				 .HasForeignKey(x => x.RequesterUserId)
				 .OnDelete(DeleteBehavior.Restrict);

				b.HasIndex(x => new { x.GroupId, x.RequesterUserId }).IsUnique();
			});

			modelBuilder.Entity<GroupPost>(b =>
			{
				b.ToTable("GroupPosts", "Social");
				b.HasKey(x => x.Id);

				b.Property(x => x.Title)
				 .IsRequired()
				 .HasMaxLength(200);

				b.Property(x => x.Text)
				 .IsRequired()
				 .HasMaxLength(4000);

				b.Property(x => x.CreatedAtUtc)
				 .IsRequired();

				b.HasOne(x => x.Group)
				 .WithMany(g => g.Posts)
				 .HasForeignKey(x => x.GroupId)
				 .OnDelete(DeleteBehavior.Cascade);

				b.HasOne(x => x.AuthorUser)
				 .WithMany()
				 .HasForeignKey(x => x.AuthorUserId)
				 .OnDelete(DeleteBehavior.Restrict);

				// for paging in UI
				b.HasIndex(x => new { x.GroupId, x.CreatedAtUtc, x.Id });
			});

			modelBuilder.Entity<GroupPost>()
				.HasMany(p => p.LikedBy)
				.WithMany()
				.UsingEntity<Dictionary<string, object>>(
					"GroupPostLikes",
					j => j
						.HasOne<SiteUser>()
						.WithMany()
						.HasForeignKey("LikedByUsersId")
						.OnDelete(DeleteBehavior.Cascade),
					j => j
						.HasOne<GroupPost>()
						.WithMany()
						.HasForeignKey("LikedPostsId")
						.OnDelete(DeleteBehavior.Cascade),
					j =>
					{
						j.ToTable("GroupPostLikes", "Social");
						j.HasKey("LikedByUsersId", "LikedPostsId");
					});

			// --------- GroupEvents ----------
			modelBuilder.Entity<GroupEvent>(b =>
			{
				b.ToTable("GroupEvents", "Social");
				b.HasKey(x => x.Id);

				b.Property(x => x.Title)
					.IsRequired()
					.HasMaxLength(200);

				b.Property(x => x.Description)
					.HasMaxLength(4000);

				b.Property(x => x.CreatedAtUtc)
					.IsRequired();

				b.HasOne(x => x.Group)
					.WithMany(g => g.Events)            // requires property on Group, see below
					.HasForeignKey(x => x.GroupId)
					.OnDelete(DeleteBehavior.Cascade);

				b.HasOne(x => x.CreatedByUser)
					.WithMany()
					.HasForeignKey(x => x.CreatedByUserId)
					.OnDelete(DeleteBehavior.Restrict);

				b.HasIndex(x => new { x.GroupId, x.StartsAtUtc, x.CreatedAtUtc, x.Id });
			});

			modelBuilder.Entity<GroupEventParticipant>(b =>
			{
				b.HasKey(p => new { p.GroupEventId, p.UserId });

				b.HasOne(p => p.GroupEvent)
					.WithMany(e => e.Participants)
					.HasForeignKey(p => p.GroupEventId);

				b.HasOne(p => p.User)
					.WithMany()
					.HasForeignKey(p => p.UserId);
			});

			// Comments
			modelBuilder.Entity<CommentBase>(b =>
			{
				b.ToTable("Comments", "Social");
				b.HasKey(x => x.Id);

				b.Property(x => x.Text)
				 .IsRequired()
				 .HasMaxLength(1000);

				b.Property(x => x.CreatedAtUtc)
				 .IsRequired();

				b.HasOne(x => x.AuthorUser)
				 .WithMany()
				 .HasForeignKey(x => x.AuthorUserId)
				 .OnDelete(DeleteBehavior.Restrict);

				b.HasDiscriminator<string>("CommentType")
				 .HasValue<ActivityComment>("Activity")
				 .HasValue<GroupPostComment>("GroupPost");
			});

			modelBuilder.Entity<ActivityComment>(b =>
			{
				b.Property(x => x.ActivityId).IsRequired();

				b.HasOne(x => x.Activity)
				 .WithMany(a => a.Comments)
				 .HasForeignKey(x => x.ActivityId)
				 .OnDelete(DeleteBehavior.Cascade);

				b.HasIndex(x => new { x.ActivityId, x.CreatedAtUtc, x.Id });
			});

			modelBuilder.Entity<GroupPostComment>(b =>
			{
				b.Property(x => x.GroupPostId).IsRequired();

				b.HasOne(x => x.GroupPost)
				 .WithMany(p => p.Comments)
				 .HasForeignKey(x => x.GroupPostId)
				 .OnDelete(DeleteBehavior.Cascade);

				b.HasIndex(x => new { x.GroupPostId, x.CreatedAtUtc, x.Id });
			});

		}

	}
}
