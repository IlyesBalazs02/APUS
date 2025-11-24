using APUS.Server.Domain.Models;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IActivityCommentRepository
	{
		Task<ActivityComment> AddAsync(ActivityComment comment);
		Task<List<ActivityComment>> GetByActivityIdAsync(string activityId);
	}
}