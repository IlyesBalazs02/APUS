using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public sealed class ActivityImageRepository : IActivityImageRepository
	{
		private readonly AppDbContext _context;

		public ActivityImageRepository(AppDbContext context)
			=> _context = context;

		public async Task AddRangeAsync(IEnumerable<ActivityImage> images)
		{
			_context.ActivityImages.AddRange(images);
			await _context.SaveChangesAsync();
		}

		public async Task<List<ActivityImage>> GetByActivityIdAsync(string activityId)
		{
			return await _context.ActivityImages
				.Where(x => x.ActivityId == activityId)
				.OrderBy(x => x.DateTaken ?? x.UploadedAt)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task DeleteByFileNamesAsync(string activityId, IEnumerable<string> fileNames)
		{
			var names = fileNames.ToArray();
			if (names.Length == 0)
				return;

			var entities = await _context.ActivityImages
				.Where(x => x.ActivityId == activityId && names.Contains(x.FileName))
				.ToListAsync();

			_context.ActivityImages.RemoveRange(entities);
			await _context.SaveChangesAsync();
		}
	}
}
