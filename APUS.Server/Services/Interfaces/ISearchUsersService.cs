using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;

namespace APUS.Server.Services.Interfaces
{
	public interface ISearchUsersService
	{
		Task<List<SiteUser>> GetAllUser();
		Task<IEnumerable<UserSearchDto>> SearchUsersAsync(string? query);
		Task<PagedResponse<UserSearchDto>> SearchUsersPagedAsync(string? query, int skip, int take);

	}
}