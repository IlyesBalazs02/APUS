using APUS.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data
{
	public class ActivityDbContext : DbContext
	{
		public DbSet<MainActivity> Activities { get; set; }
		public ActivityDbContext(DbContextOptions<ActivityDbContext> opt) :base(opt)
		{

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<MainActivity>().ToTable("MainActivities", "Activities");

			modelBuilder.Entity<Running>().ToTable("Running", "Activities");
			modelBuilder.Entity<Hiking>().ToTable("Hiking", "Activities");
			modelBuilder.Entity<Bouldering>().ToTable("Bouldering", "Activities");
		}

	}
}
