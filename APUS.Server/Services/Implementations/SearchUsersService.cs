using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations
{
	public class SearchUsersService : ISearchUsersService
	{
		private readonly ISiteUserRepository _siteUserRepository;

		public SearchUsersService(ISiteUserRepository siteUserRepository)
		{
			_siteUserRepository = siteUserRepository;
		}

		public async Task<List<SiteUser>> GetAllUser()
		{
			return await _siteUserRepository.ReadAllAsync();
		}
	}
}
