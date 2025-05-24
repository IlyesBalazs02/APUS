using APUS.Server.Data;
using APUS.Server.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Data
{
	public class ActivityRepositoryTest : IDisposable
	{
		private readonly ActivityDbContext _context;
		private readonly ActivityRepository _repo;

		public ActivityRepositoryTest()
		{
			var options = new DbContextOptionsBuilder<ActivityDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			_context = new ActivityDbContext(options);
			_repo = new ActivityRepository(_context);
		}

		public void Dispose()
		{
			_context.Database.EnsureDeleted();
			_context.Dispose();
		}

		[Fact]
		public async Task CreateAsync_AddsActivity()
		{
			var activity = new MainActivity { Id = "1", Title = "Test", UserId = "GUID_ID" };

			await _repo.CreateAsync(activity);

			var all = await _context.Activities.ToListAsync();
			all.Should().ContainSingle()
			   .Which.Should().BeEquivalentTo(activity);
		}

		[Fact]
		public async Task ReadAllAsync_ReturnsAllEntities()
		{
			var a1 = new MainActivity { Id = "1", Title = "A", UserId = "GUID_ID" };
			var a2 = new MainActivity { Id = "2", Title = "B", UserId = "GUID_ID" };
			await _context.Activities.AddRangeAsync(a1, a2);
			await _context.SaveChangesAsync();

			var result = (await _repo.ReadAllAsync()).ToList();
			result.Should().HaveCount(2);
			result.Should().ContainEquivalentOf(a1);
			result.Should().ContainEquivalentOf(a2);
		}

		[Fact]
		public async Task ReadByIdAsync_Found_ReturnsEntity()
		{
			var activity = new MainActivity { Id = "X", Title = "Found", UserId = "GUID_ID" };
			await _context.Activities.AddAsync(activity);
			await _context.SaveChangesAsync();

			var result = await _repo.ReadByIdAsync("X");
			result.Should().NotBeNull();
			result.Should().BeEquivalentTo(activity);
		}

		[Fact]
		public async Task ReadByIdAsync_NotFound_ReturnsNull()
		{
			var result = await _repo.ReadByIdAsync("Nope");
			result.Should().BeNull();
		}

		[Fact]
		public async Task UpdateAsync_SameType_UpdatesValues()
		{
			var original = new MainActivity { Id = "U1", Title = "Old" , UserId = "GUID_ID" };
			await _context.Activities.AddAsync(original);
			await _context.SaveChangesAsync();

			var updated = new MainActivity { Id = "U1", Title = "New", Duration = TimeSpan.FromMinutes(5), UserId = "GUID_ID" };
			await _repo.UpdateAsync("U1", updated);

			var stored = await _context.Activities.FindAsync("U1");
			stored.Should().NotBeNull();
			stored.Title.Should().Be("New");
			stored.Duration.Should().Be(TimeSpan.FromMinutes(5));
		}

		[Fact]
		public async Task UpdateAsync_DifferentSubtype_ReplacesEntity()
		{
			var running = new Running { Id = "R1", Title = "Run", TotalDistanceKm = 3, UserId = "GUID_ID" };
			await _context.Activities.AddAsync(running);
			await _context.SaveChangesAsync();

			var gps = new GpsRelatedActivity { Id = "R1", Title = "GPS", TotalDistanceKm = 5, TotalAscentMeters = 10, UserId = "GUID_ID" };
			await _repo.UpdateAsync("R1", gps);

			var stored = await _context.Activities.FindAsync("R1");
			stored.Should().BeOfType<GpsRelatedActivity>();
			var storedGps = (GpsRelatedActivity)stored!;
			storedGps.TotalDistanceKm.Should().Be(5);
			storedGps.TotalAscentMeters.Should().Be(10);
		}

		[Fact]
		public async Task UpdateAsync_NonExistent_ThrowsKeyNotFoundException()
		{
			var act = new MainActivity { Id = "No", Title = "X" };
			await Assert.ThrowsAsync<KeyNotFoundException>(() => _repo.UpdateAsync("No", act));
		}

		[Fact]
		public async Task DeleteAsync_RemovesEntity()
		{
			var activity = new MainActivity { Id = "D1", Title = "Del", UserId = "GUID_ID" };
			await _context.Activities.AddAsync(activity);
			await _context.SaveChangesAsync();

			await _repo.DeleteAsync("D1");

			var all = await _context.Activities.ToListAsync();
			all.Should().BeEmpty();
		}

		[Fact]
		public async Task DeleteAsync_NonExistent_ThrowsKeyNotFoundException()
		{
			await Assert.ThrowsAsync<KeyNotFoundException>(() => _repo.DeleteAsync("X"));
		}
	}
}
