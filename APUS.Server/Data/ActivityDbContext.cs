using APUS.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data
{
	public class ActivityDbContext : IdentityDbContext
	{
		public DbSet<MainActivity> Activities { get; set; }
		public DbSet<GpsRelatedActivity> GpsRelatedActivities { get; set; }
		//public DbSet<ActivityImage> ActivityImages { get; set; }

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


			//user
			modelBuilder.Entity<MainActivity>()
				.HasMany(a => a.LikedBy)
				.WithMany(u => u.LikedPosts)
				.UsingEntity<Dictionary<string, object>>(
					"MainActivityLikes",
					// 1) User → join : NO ACTION
					j => j
						.HasOne<SiteUser>()
						.WithMany()
						.HasForeignKey("UserId")
						.HasConstraintName("FK_MainActivityLikes_Users_UserId")
						.OnDelete(DeleteBehavior.Restrict),

					// 2) Activity → join : CASCADE
					j => j
						.HasOne<MainActivity>()
						.WithMany()
						.HasForeignKey("MainActivityId")
						.HasConstraintName("FK_MainActivityLikes_Activities_ActivityId")
						.OnDelete(DeleteBehavior.Cascade),

					j =>
					{
						j.ToTable("MainActivityLikes", "Activities");
						j.HasKey("MainActivityId", "UserId")
						.IsClustered(false);
					});

		}

	}
}
