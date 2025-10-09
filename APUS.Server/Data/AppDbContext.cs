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


			// Add index to UserId
			modelBuilder.Entity<MainActivity>()
				.HasIndex(a => a.UserId)
				.HasDatabaseName("IX_MainActivities_UserId");


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



		}

	}
}
