using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Activity;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations.Activity
{
	public class ActivityService : IActivityService
	{
		private readonly IActivityRepository _repo;

		public ActivityService(IActivityRepository repo)
		{
			_repo = repo;
		}

		public async Task EditActivityAsync(MainActivity existing, EditActivityRequest req)
		{
			// if the type doesn't change, just save it
			if (existing.ActivityType == req.ActivityType.ToString())
			{
				existing.Title = req.Title;
				existing.Description = req.Description;
				existing.Date = req.Date;

				await _repo.SaveAsync(existing);
				return;
			}

			MainActivity CreateWithBase<T>() where T : MainActivity, new()
				=> new T
				{
					Id = existing.Id,
					UserId = existing.UserId,
					Title = req.Title,
					Description = req.Description,
					Date = req.Date,
				};

			MainActivity replacement = req.ActivityType switch
			{
				ActivityType.Running => CreateWithBase<Running>(),
				ActivityType.Hiking => CreateWithBase<Hiking>(),
				ActivityType.GpsRelatedActivity => CreateWithBase<GpsRelatedActivity>(),

				_ => throw new NotSupportedException($"Unsupported target type {req.ActivityType}")
			};

			await _repo.CopyProps(existing, replacement);

			await _repo.ReplaceAsync(existing, replacement);
		}

	}
}
