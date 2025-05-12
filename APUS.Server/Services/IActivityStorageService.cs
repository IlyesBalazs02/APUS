namespace APUS.Server.Services
{
	public interface IActivityStorageService
	{
		void EnsureActivityFolder(string activityId);
	}
}
