using APUS.Server.Domain.Models;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IActivityImageRepository
	{
		Task AddRangeAsync(IEnumerable<ActivityImage> images);
		Task DeleteByFileNamesAsync(string activityId, IEnumerable<string> fileNames);
		Task<List<ActivityImage>> GetByActivityIdAsync(string activityId);
	}
}