using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Domain.Models;

namespace APUS.Server.Services.Interfaces
{
	public interface ITrackpointLoader
	{
		 Task<List<TrackpointDto>> LoadTrack(MainActivity activity, CancellationToken ct = default);
	}
}
