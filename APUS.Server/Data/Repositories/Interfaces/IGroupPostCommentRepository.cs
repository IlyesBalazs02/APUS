using APUS.Server.Domain.Entities.Groups;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IGroupPostCommentRepository
	{
		Task<GroupPostComment> AddAsync(GroupPostComment comment);
		Task<List<GroupPostComment>> GetByPostIdAsync(long groupPostId);
	}
}