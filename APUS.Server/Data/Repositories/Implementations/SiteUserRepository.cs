using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public class SiteUserRepository : ISiteUserRepository
	{
		private readonly AppDbContext _context;

		public SiteUserRepository(AppDbContext context)
			=> _context = context;

		public async Task<List<SiteUser>> ReadAllAsync()
		{
			return await _context.SiteUsers
				.AsNoTracking()
				.ToListAsync();
		}

		// Basic search (no paging)
		public async Task<List<SiteUser>> SearchByNameAsync(string? term)
		{
			term = (term ?? string.Empty).Trim().ToLower();

			var q = _context.SiteUsers.AsNoTracking();

			if (!string.IsNullOrEmpty(term))
			{
				// Match where "FirstName LastName" contains the term
				q = q.Where(u =>
					EF.Functions.Like(
						(((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower()),
						$"%{term}%"
					)
				);
			}

			return await q
				.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ThenBy(u => u.Id)
				.Take(50)
				.ToListAsync();
		}

		// Same, but with paging support (skip/take)
		public async Task<List<SiteUser>> SearchByNamePagedAsync(string? term, int skip, int takePlusOne)
		{
			term = (term ?? string.Empty).Trim().ToLower();

			var q = _context.SiteUsers.AsNoTracking();

			if (!string.IsNullOrEmpty(term))
			{
				q = q.Where(u =>
					EF.Functions.Like(
						(((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower()),
						$"%{term}%"
					)
				);
			}

			return await q
				.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ThenBy(u => u.Id)
				.Skip(skip).Take(takePlusOne)
				.ToListAsync();
		}




	}
}
