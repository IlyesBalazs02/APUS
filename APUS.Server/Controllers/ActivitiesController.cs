using APUS.Server.Models.Activities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ActivitiesController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly ActivityService _activityService;

		public ActivitiesController(ILogger<ActivitiesController> logger, ActivityService activityService)
		{
			_logger = logger;
			_activityService = activityService;

		}

		[HttpPost]
		public IActionResult CreateActivity([FromBody] MainActivity activity)
		{
			Console.WriteLine("=== Base Properties ===");
			var baseProps = typeof(MainActivity).GetProperties();
			foreach (var prop in baseProps)
			{
				Console.WriteLine($"{prop.Name}: {prop.GetValue(activity)}");
			}

			Console.WriteLine("=== Unique Properties ===");
			var allProps = activity.GetType().GetProperties();
			foreach (var prop in allProps)
			{
				// Skip base class properties
				if (!baseProps.Any(p => p.Name == prop.Name))
				{
					Console.WriteLine($"{prop.Name}: {prop.GetValue(activity)}");
				}
			}

			var newActivity = Activator.CreateInstance(activity.GetType());
			foreach (var prop in allProps)
			{
				var value = prop.GetValue(activity);
				if (value != null)
				{
					prop.SetValue(newActivity, value);
				}
			}
			_activityService.Activities.Add((MainActivity)newActivity);

			return Ok();
		}

		[HttpGet]
		public IEnumerable<MainActivity> Get()
		{
			Console.WriteLine("asd!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			return _activityService.Activities.ToArray();
		}
	}

}
