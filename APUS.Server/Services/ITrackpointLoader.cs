using APUS.Server.DTOs;

namespace APUS.Server.Services
{
	public interface ITrackpointLoader
	{
		 Task<List<TrackpointDto>> LoadTrack(string activityId, CancellationToken ct = default);
	}
}
