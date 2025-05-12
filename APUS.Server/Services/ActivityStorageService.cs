namespace APUS.Server.Services
{
	public class ActivityStorageService : IActivityStorageService
	{
		private readonly string _uploadsRoot;

		public ActivityStorageService(IWebHostEnvironment env)
			=> _uploadsRoot = Path.Combine(env.WebRootPath, "uploads", "activities");

		public void EnsureActivityFolder(string activityId)
		{
			var path = Path.Combine(_uploadsRoot, "activities", activityId);
			Directory.CreateDirectory(path);
			path = Path.Combine(_uploadsRoot, "Activities", activityId, "Images");
			Directory.CreateDirectory(path);
			path = Path.Combine(_uploadsRoot, "Activities", activityId, "Tracks");
			Directory.CreateDirectory(path);
		}
	}
}
