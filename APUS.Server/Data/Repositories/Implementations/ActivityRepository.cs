using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace APUS.Server.Data.Repositories.Implementations
{
	public class ActivityRepository : IActivityRepository
	{
		private readonly AppDbContext _context;

		public ActivityRepository(AppDbContext context)
			=> _context = context;

		public async Task CreateAsync(MainActivity activity)
		{
			_context.Activities.Add(activity);
			await _context.SaveChangesAsync();
		}

		public async Task<IEnumerable<MainActivity>> ReadAllAsync()
		{
			return await _context.Activities
				.Include(a => a.User)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<MainActivity?> ReadByIdAsync(string id)
		{
			return await _context.Activities
				.Include(a => a.User)
				.AsNoTracking()
				.FirstOrDefaultAsync(a => a.Id == id);
		}

		public async Task<IEnumerable<MainActivity>> GetActivitiesByUserIdAsync(string userId)
		{
			return await _context.Activities
				.Include(a => a.User)
				.Where(a => a.UserId == userId)
				.ToListAsync();
		}

		public async Task UpdateAsync(string id, MainActivity activity)
		{
			var oldEntity = await _context.Activities.FindAsync(id)
							?? throw new KeyNotFoundException(id);

			if (oldEntity.GetType() == activity.GetType())
			{
				// Same subtype: update properties
				_context.Entry(oldEntity).CurrentValues.SetValues(activity);
				await _context.SaveChangesAsync();
			}
			else
			{
				// remove old
				_context.Activities.Remove(oldEntity);
				await _context.SaveChangesAsync();

				// add new
				activity.Id = id;
				_context.Activities.Add(activity);
				await _context.SaveChangesAsync();
			}

			await _context.SaveChangesAsync();
		}

		// copy all props except the activityType
		public async Task CopyProps(MainActivity existing, MainActivity replacement)
		{
			var actType = replacement.ActivityType;
			_context.Entry(replacement).CurrentValues.SetValues(existing);
			replacement.ActivityType = actType;
		}

		public async Task SaveAsync(MainActivity activity)
		{
			_context.Update(activity);
			await _context.SaveChangesAsync();
		}

		public async Task ReplaceAsync(MainActivity oldEntity, MainActivity newEntity)
		{
			_context.Activities.Remove(oldEntity);
			_context.Activities.Add(newEntity);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(string id)
		{
			var entity = await _context.Activities.FindAsync(id)
						 ?? throw new KeyNotFoundException(id);

			_context.Activities.Remove(entity);
			await _context.SaveChangesAsync();
		}

		public async Task<List<MainActivity>> GetPagedAsync(int skip, int takePlusOne)
		{
			return await _context.Activities
				.Include(a => a.User)
				.AsNoTracking()
				.OrderByDescending(a => a.Date)
				.ThenByDescending(a => a.Id)
				.Skip(skip)
				.Take(takePlusOne)
				.ToListAsync();
		}

		public async Task<List<MainActivity>> GetFeedPagedAsync(string me, int skip, int takePlusOne)
		{
			var friendIds = _context.UserRelations
				.Where(r => r.Status == UserRelationStatus.Accepted &&
						   (r.UserId == me || r.FriendId == me))
				.Select(r => r.UserId == me ? r.FriendId : r.UserId);

			return await _context.Activities
				.Include(a => a.User)
				.AsNoTracking()
				.Where(a => a.UserId == me || friendIds.Contains(a.UserId))   // me + friends
				.OrderByDescending(a => a.Date)
				.ThenByDescending(a => a.Id)
				.Skip(skip)
				.Take(takePlusOne)
				.ToListAsync();
		}

		public async Task<List<MainActivity>> GetByUserIdPagedAsync(string userId, int skip, int takePlusOne)
		{
			return await _context.Activities
				.Include(a => a.User)
				.Where(a => a.UserId == userId)
				.AsNoTracking()
				.OrderByDescending(a => a.Date)
				.ThenByDescending(a => a.Id)
				.Skip(skip)
				.Take(takePlusOne)
				.ToListAsync();
		}

		public async Task<List<MainActivity>> GetByUserIdAndDateRangeAsync(string userId, DateTime fromUtcInclusive, DateTime toUtcExclusive)
		{
			return await _context.Activities
				.Include(a => a.User)
				.AsNoTracking()
				.Where(a => a.UserId == userId &&
							a.Date >= fromUtcInclusive &&
							a.Date < toUtcExclusive)
				.ToListAsync();
		}

		public async Task<List<MainActivity>> GetByUserIdAndMonthAsync(string userId, int year, int month)
		{
			var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
			var to = from.AddMonths(1);

			return await _context.Activities
				.Include(a => a.User)
				.AsNoTracking()
				.Where(a => a.UserId == userId &&
							a.Date >= from &&
							a.Date < to)
				.ToListAsync();
		}

	}

}
