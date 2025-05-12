namespace APUS.Server.Services
{
	public class ActivityStorageService : IActivityStorageService
	{
		private readonly string _uploadsRoot;

		public ActivityStorageService(IWebHostEnvironment env)
			=> _uploadsRoot = Path.Combine(env.WebRootPath, "Activities");

		public void EnsureActivityFolder(string activityId)
		{
			var path = Path.Combine(_uploadsRoot, activityId);
			Directory.CreateDirectory(path);
			path = Path.Combine(_uploadsRoot, activityId, "Images");
			Directory.CreateDirectory(path);
			path = Path.Combine(_uploadsRoot, activityId, "Tracks");
			Directory.CreateDirectory(path);
		}
	}
}
