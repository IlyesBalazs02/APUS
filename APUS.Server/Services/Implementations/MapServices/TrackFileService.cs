using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations.MapServices
{
	public class TrackFileService : ITrackFileService
	{
		private readonly IWebHostEnvironment _env;

		public TrackFileService(IWebHostEnvironment env)
		{
			_env = env;
		}

		public IEnumerable<string> GetTrackNamesForUser(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Enumerable.Empty<string>();

			var tracksDir = Path.Combine(_env.WebRootPath, "Users", userId, "Tracks");

			if (!Directory.Exists(tracksDir))
				return Enumerable.Empty<string>();

			// Return file names without extension, ordered alphabetically
			return Directory
				.EnumerateFiles(tracksDir, "*.gpx", SearchOption.TopDirectoryOnly)
				.Select(path => Path.GetFileNameWithoutExtension(path))
				.OrderBy(name => name)
				.ToList();
		}
	}
}
