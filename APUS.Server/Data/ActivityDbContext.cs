using APUS.Server.Models.Activities;
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
			modelBuilder.Entity<MainActivity>()
				.HasDiscriminator<string>("ActivityType")
				.HasValue<MainActivity>("Activity")
				.HasValue<Running>("Running")
				.HasValue<Bouldering>("Bouldering");
		}
	}
}
