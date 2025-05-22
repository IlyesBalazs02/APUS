namespace APUS.Server.Services.Interfaces
{
	public interface IStorageService
	{
		void CreateActivityFolder(string activityId, string userId);
		void CreateUserFolder(string userId);
		IEnumerable<string> GetImageFileNames(string activityId, string userId);
		string ReturnFirstFilePath(string activityId, string userId);
		string ReturnTrackImagePath(string activityId, string userId);
		Task SaveImagesAsync(string activityId, IFormFileCollection images, string userId);
		Task SaveTrackAsync(string activityId, string userId, IFormFile trackFile);
	}
}