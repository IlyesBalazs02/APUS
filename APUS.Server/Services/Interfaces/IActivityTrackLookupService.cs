namespace APUS.Server.Services.Interfaces
{
	public interface IActivityTrackLookupService
	{
		Task<(double Lat, double Lon)?> FindClosestPointAsync(string activityId, string userId, DateTime photoTimeUtc, CancellationToken ct = default);
	}
}