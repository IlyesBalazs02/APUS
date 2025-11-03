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

		public async Task<List<SiteUser>> SearchByNameAsync(string? term)
		{
			term = (term ?? string.Empty).Trim().ToLower();

			return await _context.SiteUsers
				.AsNoTracking()
				.Where(u =>
					string.IsNullOrEmpty(term) ||
					EF.Functions.Like(u.FirstName.ToLower(), $"%{term}%") ||
					EF.Functions.Like(u.LastName.ToLower(), $"%{term}%") ||
					EF.Functions.Like((u.FirstName + " " + u.LastName).ToLower(), $"%{term}%") ||
					(u.UserName != null && EF.Functions.Like(u.UserName.ToLower(), $"%{term}%"))
				)
				.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
				.Take(50)
				.ToListAsync();
		}
	}
}
