using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations.MapServices
{
	public class ActivityTrackLookupService : IActivityTrackLookupService
	{
		private readonly IActivityRepository _activityRepository;
		private readonly ITrackpointLoader _trackpointLoader;

		public ActivityTrackLookupService(
			IActivityRepository activityRepository,
			ITrackpointLoader trackpointLoader)
		{
			_activityRepository = activityRepository;
			_trackpointLoader = trackpointLoader;
		}

		public async Task<(double Lat, double Lon)?> FindClosestPointAsync(
			string activityId,
			string userId,
			DateTime photoTimeUtc,
			CancellationToken ct = default)
		{
			// 1) Load the activity (needed so loader can find the file on disk)
			var activity = await _activityRepository.ReadByIdAsync(activityId);
			if (activity == null)
				return null;

			// (optional safety: only allow matching owner)
			if (!string.Equals(activity.UserId, userId, StringComparison.OrdinalIgnoreCase))
				return null;

			List<TrackpointDto> trackpoints;
			try
			{
				trackpoints = await _trackpointLoader.LoadTrack(activity, ct);
			}
			catch
			{
				// If TCX/GPX parsing fails we don't want to break image upload
				return null;
			}

			if (trackpoints == null || trackpoints.Count == 0)
				return null;

			// Only GPS points
			var candidates = trackpoints
				.Where(tp => tp.Lat.HasValue && tp.Lon.HasValue)
				.ToList();

			if (!candidates.Any())
				return null;

			var closest = FindClosestBinarySearch(candidates, photoTimeUtc);
			return closest;
		}

		private static (double Lat, double Lon)? FindClosestBinarySearch(
	List<TrackpointDto> points,
	DateTime photoTimeUtc)
		{
			if (points.Count == 0)
				return null;

			// Extract timestamps
			var times = points
				.Select(tp => DateTime.SpecifyKind(tp.Time, DateTimeKind.Utc))
				.ToList();

			// binary search:
			int index = times.BinarySearch(photoTimeUtc);

			if (index >= 0)
			{
				// exact match → perfect
				var tp = points[index];
				return (tp.Lat!.Value, tp.Lon!.Value);
			}

			// No exact match → BinarySearch returns bitwise complement of next larger element
			index = ~index;

			TrackpointDto? best = null;

			TimeSpan bestDelta = TimeSpan.MaxValue;

			// candidate A: the next later point
			if (index < points.Count)
			{
				var tp = points[index];
				var delta = (times[index] - photoTimeUtc).Duration();
				if (delta < bestDelta)
				{
					bestDelta = delta;
					best = tp;
				}
			}

			// candidate B: the previous earlier point
			if (index > 0)
			{
				var tp = points[index - 1];
				var delta = (times[index - 1] - photoTimeUtc).Duration();
				if (delta < bestDelta)
				{
					bestDelta = delta;
					best = tp;
				}
			}

			if (best == null)
				return null;

			return (best.Lat!.Value, best.Lon!.Value);
		}

	}
}
