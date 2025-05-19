using APUS.Server.DTOs;
using APUS.Server.Models;

namespace APUS.Server.Services
{
	public interface ITrackpointLoader
	{
		 Task<List<TrackpointDto>> LoadTrack(MainActivity activity, CancellationToken ct = default);
	}
}
