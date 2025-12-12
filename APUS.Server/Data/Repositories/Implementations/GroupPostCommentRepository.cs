using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Entities.Groups;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public sealed class GroupPostCommentRepository : IGroupPostCommentRepository
	{
		private readonly AppDbContext _context;

		public GroupPostCommentRepository(AppDbContext context)
			=> _context = context;

		public async Task<List<GroupPostComment>> GetByPostIdAsync(long groupPostId)
		{
			return await _context.GroupPostComments
				.AsNoTracking()
				.Where(c => c.GroupPostId == groupPostId)
				.Include(c => c.AuthorUser)
				.OrderBy(c => c.CreatedAtUtc)
				.ThenBy(c => c.Id)
				.ToListAsync();
		}

		public async Task<GroupPostComment> AddAsync(GroupPostComment comment)
		{
			_context.GroupPostComments.Add(comment);
			await _context.SaveChangesAsync();

			await _context.Entry(comment)
				.Reference(c => c.AuthorUser)
				.LoadAsync();

			return comment;
		}
	}
}
