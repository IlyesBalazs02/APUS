using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface ISiteUserRepository
	{
		Task<IdentityResult> CreaterAsync(string firstName, string lastName, string username, string email, string passwrod);
		Task<List<SiteUser>> ReadAllAsync();
		Task<List<SiteUser>> SearchByNameAsync(string? term);
		Task<List<SiteUser>> SearchByNamePagedAsync(string? term, int skip, int takePlusOne);
	}
}