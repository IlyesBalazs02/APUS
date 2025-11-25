using APUS.Server.Services.Implementations.MapServices;

namespace APUS.Server.Services.Interfaces
{
	public interface IMapsforgeService
	{
		Task<MapsforgeFileResult?> GenerateMapAsync(string userId, double top, double bottom, double left, double right);
		Task<MapsforgeFileResult?> GenerateMapFromTrackFileAsync(string userId, string trackFileName);
		(bool ok, string message) ValidateBbox(double top, double bottom, double left, double right);
	}
}