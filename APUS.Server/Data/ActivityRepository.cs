using APUS.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace APUS.Server.Data
{
	public class ActivityRepository : IActivityRepository
	{
		private readonly ActivityDbContext _context;

		public ActivityRepository(ActivityDbContext context)
			=> _context = context;

		public async Task CreateAsync(MainActivity activity)
		{
			_context.Activities.Add(activity);
			await _context.SaveChangesAsync();
		}

		public async Task<IEnumerable<MainActivity>> ReadAllAsync()
		{
			return await _context.Activities
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<MainActivity?> ReadByIdAsync(string id)
		{
			return await _context.Activities
				.AsNoTracking()
				.FirstOrDefaultAsync(a => a.Id == id);
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
				// 1) remove old
				_context.Activities.Remove(oldEntity);
				await _context.SaveChangesAsync();

				// 2) add new
				activity.Id = id;
				_context.Activities.Add(activity);
				await _context.SaveChangesAsync();
			}

			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(string id)
		{
			var entity = await _context.Activities.FindAsync(id)
						 ?? throw new KeyNotFoundException(id);

			_context.Activities.Remove(entity);
			await _context.SaveChangesAsync();
		}
	}
	
}
