using APUS.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data
{
	public class ActivityDbContext : IdentityDbContext
	{
		public DbSet<MainActivity> Activities { get; set; }

		public ActivityDbContext(DbContextOptions<ActivityDbContext> opt) :base(opt)
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



		}

	}
}
