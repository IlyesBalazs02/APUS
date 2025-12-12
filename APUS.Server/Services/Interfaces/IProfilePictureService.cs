namespace APUS.Server.Services.Interfaces
{
	public interface IProfilePictureService
	{
		Task DeleteProfilePictureAsync(string userId);
		Task<string> GetProfilePictureUrlAsync(string userId);
		Task<string> UploadProfilePictureAsync(string userId, IFormFile file);
	}
}