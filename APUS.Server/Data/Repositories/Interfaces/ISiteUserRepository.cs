using APUS.Server.Domain.Models;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface ISiteUserRepository
	{
		Task<List<SiteUser>> ReadAllAsync();
	}
}