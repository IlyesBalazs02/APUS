namespace APUS.Server.Services
{
	public interface IActivityStorageService
	{
		void CreateActivityFolder(string activityId);
		Task SaveImages(string activityId, IFormFileCollection images);
	}
}
