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
		public List<MainActivity> Activities = new List<MainActivity>();
		public ActivitiesController(ILogger<ActivitiesController> logger)
		{
			_logger = logger;

			Activities.Add(new Running { Time = 30, HeartRate = 120, Date = DateTime.Now, Pace = 5, Distance = 1000 });
			Activities.Add(new Bouldering { Time = 45, HeartRate = 130, Date = DateTime.Now, Difficulty = 5, RedPoint = false });
			Activities.Add(new MainActivity { Time = 60, HeartRate = 140, Date = DateTime.Now });
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
			Activities.Add((MainActivity)newActivity);

			return Ok();
		}

		[HttpGet]
		public IEnumerable<MainActivity> Get()
		{
			Console.WriteLine("asd!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			return Activities.ToArray();
		}
	}

}
