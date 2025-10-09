using APUS.Server.Domain.Models;

namespace APUS.Server.Services.Interfaces
{
	public interface ISearchUsersService
	{
		Task<List<SiteUser>> GetAllUser();
	}
}