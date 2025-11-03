using APUS.Server.Domain.Models;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface ISiteUserRepository
	{
		Task<List<SiteUser>> ReadAllAsync();
		Task<List<SiteUser>> SearchByNameAsync(string? term);
		Task<List<SiteUser>> SearchByNamePagedAsync(string? term, int skip, int takePlusOne);
	}
}