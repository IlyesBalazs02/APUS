using APUS.Server.Domain.Models;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IActivityRepository
	{
		Task CreateAsync(MainActivity activity);
		Task DeleteAsync(string id);
		Task<IEnumerable<MainActivity>> GetActivitiesByUserIdAsync(string userId);
		Task<List<MainActivity>> GetByUserIdPagedAsync(string userId, int skip, int takePlusOne);
		Task<List<MainActivity>> GetFeedPagedAsync(string me, int skip, int takePlusOne);
		Task<List<MainActivity>> GetPagedAsync(int skip, int takePlusOne);
		Task<IEnumerable<MainActivity>> ReadAllAsync();
		Task<MainActivity?> ReadByIdAsync(string id);
		Task UpdateAsync(string id, MainActivity activity);
		Task ReplaceAsync(MainActivity oldEntity, MainActivity newEntity);
		Task SaveAsync(MainActivity activity);
		Task CopyProps(MainActivity existing, MainActivity replacement);
		Task<List<MainActivity>> GetByUserIdAndDateRangeAsync(string userId,DateTime fromUtcInclusive,DateTime toUtcExclusive);
		Task<List<MainActivity>> GetByUserIdAndMonthAsync(string userId,int year,int month);

	}
}