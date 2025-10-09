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
	}
}
