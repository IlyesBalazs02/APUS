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
				.HasOne(a => a.User)
				.WithMany(u => u.Activities)
				.HasForeignKey(a => a.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		}

	}
}
