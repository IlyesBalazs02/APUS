using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Domain.Models;
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
			// 1) Load activity
			var activity = await _activityRepository.ReadByIdAsync(activityId);
			if (activity == null)
				return null;

			// optional safety: owner check
			if (!string.Equals(activity.UserId, userId, StringComparison.OrdinalIgnoreCase))
				return null;

			// 2) Compute activity time window [start, end]
			// Date on MainActivity is the start; normalize to UTC
			var startUtc = activity.Date.Kind == DateTimeKind.Utc
				? activity.Date
				: DateTime.SpecifyKind(activity.Date, DateTimeKind.Utc);

			DateTime? endUtc = null;

			if (activity is GpsRelatedActivity gps && gps.FinishTimeUtc.HasValue)
			{
				endUtc = gps.FinishTimeUtc.Value.Kind == DateTimeKind.Utc
					? gps.FinishTimeUtc.Value
					: DateTime.SpecifyKind(gps.FinishTimeUtc.Value, DateTimeKind.Utc);
			}
			else
			{
				// Fallback: use Date + Duration
				endUtc = startUtc + activity.Duration;
			}

			// Make sure photoTimeUtc is treated as UTC as well
			var photoUtc = photoTimeUtc.Kind == DateTimeKind.Utc
				? photoTimeUtc
				: DateTime.SpecifyKind(photoTimeUtc, DateTimeKind.Utc);

			// 3) Only search for lat/lon if photo time is within [start, end]
			if (photoUtc < startUtc || (endUtc.HasValue && photoUtc > endUtc.Value))
			{
				// Outside activity window → do NOT try to match GPS point
				return null;
			}

			// 4) Load trackpoints only if time window check passed
			List<TrackpointDto> trackpoints;
			try
			{
				trackpoints = await _trackpointLoader.LoadTrack(activity, ct);
			}
			catch
			{
				// If TCX/GPX parsing fails, don't break upload
				return null;
			}

			if (trackpoints == null || trackpoints.Count == 0)
				return null;

			var candidates = trackpoints
				.Where(tp => tp.Lat.HasValue && tp.Lon.HasValue)
				.ToList();

			if (!candidates.Any())
				return null;

			var closest = FindClosestBinarySearch(candidates, photoUtc);
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
