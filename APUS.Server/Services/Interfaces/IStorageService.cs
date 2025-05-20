namespace APUS.Server.Services.Interfaces
{
	public interface IStorageService
	{
		void CreateActivityFolder(string activityId, string userId);
		void CreateUserFolder(string userId);
		IEnumerable<string> GetImageFileNames(string activityId, string userId);
		string ReturnFilePath(string activityId, string userId);
		Task SaveImages(string activityId, IFormFileCollection images, string userId);
		Task SaveTrack(string activityId, string userId, IFormFile trackFile);
	}
}