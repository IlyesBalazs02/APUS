using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace APUS.Server.Services.Implementations
{
	public class SearchUsersService : ISearchUsersService
	{
		private readonly ISiteUserRepository _siteUserRepository;
		private readonly IProfilePictureService _profilePictureService;
		private readonly UserManager<SiteUser> _userManager;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public SearchUsersService(ISiteUserRepository siteUserRepository, IProfilePictureService profilePictureService, UserManager<SiteUser> userManager, IHttpContextAccessor httpContextAccessor)
		{
			_siteUserRepository = siteUserRepository;
			_profilePictureService = profilePictureService;
			_userManager = userManager;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<List<SiteUser>> GetAllUser()
		{
			return await _siteUserRepository.ReadAllAsync();
		}

		public async Task<IEnumerable<UserSearchDto>> SearchUsersAsync(string? query)
		{
			var users = await _siteUserRepository.SearchByNameAsync(query);

			var result = new List<UserSearchDto>(users.Count);
			foreach (var u in users)
			{
				result.Add(new UserSearchDto
				{
					Id = u.Id,
					FullName = $"{u.FirstName} {u.LastName}".Trim(),
					UserName = u.UserName,
					AvatarUrl = await _profilePictureService.GetProfilePictureUrlAsync(u.Id)
				});
			}
			return result;
		}

		// Returns a paginated list of users as DTOs
		public async Task<PagedResponse<UserSearchDto>> SearchUsersPagedAsync(string? query, int skip, int take)
		{
			// Get current user ID from the JWT claims
			var currentUserId = _userManager.GetUserId(_httpContextAccessor.HttpContext?.User);

			// Fetch users from repository (+1 to detect "has more")
			var users = await _siteUserRepository.SearchByNamePagedAsync(query, skip, take + 1);

			// Exclude current user if logged in
			if (!string.IsNullOrEmpty(currentUserId))
				users = users.Where(u => u.Id != currentUserId).ToList();

			var hasMore = users.Count > take;
			if (hasMore) users.RemoveAt(users.Count - 1);

			// Map SiteUser entities to UserSearchDto
			var items = new List<UserSearchDto>(users.Count);
			foreach (var u in users)
			{
				items.Add(new UserSearchDto
				{
					Id = u.Id,
					FullName = $"{u.FirstName} {u.LastName}".Trim(),
					UserName = u.UserName,
					AvatarUrl = await _profilePictureService.GetProfilePictureUrlAsync(u.Id)
				});
			}

			return new PagedResponse<UserSearchDto> { Items = items, HasMore = hasMore };
		}




	}
}
