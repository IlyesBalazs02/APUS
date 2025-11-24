using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public class ActivityCommentRepository : IActivityCommentRepository
	{
		private readonly AppDbContext _context;

		public ActivityCommentRepository(AppDbContext context)
			=> _context = context;

		public async Task<List<ActivityComment>> GetByActivityIdAsync(string activityId)
		{
			return await _context.ActivityComments
				.AsNoTracking()
				.Where(c => c.ActivityId == activityId)
				.Include(c => c.AuthorUser)
				.OrderBy(c => c.CreatedAtUtc)
				.ThenBy(c => c.Id)
				.ToListAsync();
		}

		public async Task<ActivityComment> AddAsync(ActivityComment comment)
		{
			_context.ActivityComments.Add(comment);
			await _context.SaveChangesAsync();

			// ensure AuthorUser is loaded for DTO mapping
			await _context.Entry(comment)
				.Reference(c => c.AuthorUser)
				.LoadAsync();

			return comment;
		}

	}
}
