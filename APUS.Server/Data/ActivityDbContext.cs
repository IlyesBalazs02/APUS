using APUS.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data
{
	public class ActivityDbContext : DbContext
	{
		public DbSet<MainActivity> Activities { get; set; }
		public DbSet<GpsRelatedActivity> GpsRelatedActivities { get; set; }
		//public DbSet<ActivityImage> ActivityImages { get; set; }

		public ActivityDbContext(DbContextOptions<ActivityDbContext> opt) :base(opt)
		{

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			//activity and images
			/*modelBuilder.Entity<ActivityImage>()
			.HasOne(ai => ai.MainActivity)
			.WithMany(ma => ma.ActivityImages)
			.HasForeignKey(ai => ai.MainActivityId);*/

			modelBuilder.Entity<MainActivity>().ToTable("MainActivities", "Activities");

			modelBuilder.Entity<GpsRelatedActivity>().ToTable("GpsRelatedActivities", "Activities");

			modelBuilder.Entity<Running>().ToTable("Running", "Activities");
			modelBuilder.Entity<Hiking>().ToTable("Hiking", "Activities");
			modelBuilder.Entity<Bouldering>().ToTable("Bouldering", "Activities");
		}

	}
}
