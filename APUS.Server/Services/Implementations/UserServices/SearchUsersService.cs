using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations
{
	public class SearchUsersService : ISearchUsersService
	{
		private readonly ISiteUserRepository _siteUserRepository;
		private readonly IProfilePictureService _profilePictureService;

		public SearchUsersService(ISiteUserRepository siteUserRepository, IProfilePictureService profilePictureService)
		{
			_siteUserRepository = siteUserRepository;
			_profilePictureService = profilePictureService;
		}

		public async Task<List<SiteUser>> GetAllUser()
		{
			return await _siteUserRepository.ReadAllAsync();
		}

		//Search all user with the same name
		public async Task<IEnumerable<UserSearchDto>> SearchUsersAsync(string? query)
		{
			var users = await _siteUserRepository.SearchByNameAsync(query);

			var tasks = users.Select(async u => new UserSearchDto
			{
				Id = u.Id,
				FullName = $"{u.FirstName} {u.LastName}".Trim(),
				UserName = u.UserName,
				AvatarUrl = await _profilePictureService.GetProfilePictureUrlAsync(u.Id)
			});

			return await Task.WhenAll(tasks); //This runs all _profilePictureService.GetProfilePictureUrlAsync calls concurrently — much faster when searching many users.
		}


	}
}
