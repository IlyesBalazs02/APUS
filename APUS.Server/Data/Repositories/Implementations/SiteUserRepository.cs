using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public class SiteUserRepository : ISiteUserRepository
	{
		private readonly AppDbContext _context;
		private readonly UserManager<SiteUser> _userManager;
		private readonly IStorageService _storageService;

		public SiteUserRepository(AppDbContext context, UserManager<SiteUser> userManager, IStorageService storageService)
		{
			_context = context;
			_userManager = userManager;
			_storageService = storageService;
		}

		public async Task<List<SiteUser>> ReadAllAsync()
		{
			return await _context.SiteUsers
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<IdentityResult> CreaterAsync(string firstName, string lastName, string username, string email, string passwrod)
		{
			var user = new SiteUser { FirstName = firstName, LastName = lastName, UserName = username, Email = email };
			var result = await _userManager.CreateAsync(user, passwrod);

			if (result.Succeeded)
			{
				_context.Add(new PrivacySettings
				{
					UserId = user.Id,
					AllowFollow = true,
					ActivityVisibility = VisibilityLevel.Everyone,
					ProfileVisibility = VisibilityLevel.Everyone
				});
				await _context.SaveChangesAsync();

				_storageService.CreateUserFolder(user.Id);
				_storageService.CreateLAModelFolder(user.Id);
				_storageService.CreateTrackFile(user.Id);
			}

			return result;

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
