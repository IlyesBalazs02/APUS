using APUS.Server.Models;

namespace APUS.Server.Data
{
	public interface IActivityRepository
	{
		Task CreateAsync(MainActivity activity);

		Task<IEnumerable<MainActivity>> ReadAllAsync();

		Task<MainActivity?> ReadByIdAsync(string id);

		Task UpdateAsync(string id, MainActivity activity);

		Task DeleteAsync(string id);
	}
}