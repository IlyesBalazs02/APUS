using APUS.Server.Data;
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
		private readonly IActivityRepository _activityRepository;

		public ActivitiesController(ILogger<ActivitiesController> logger, IActivityRepository activityRepository)
		{
			_logger = logger;
			_activityRepository = activityRepository;

		}

		[HttpPost]
		public IActionResult CreateActivity([FromBody] MainActivity activity)
		{
			/*Console.WriteLine("=== Base Properties ===");
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
			}*/
			_activityRepository.Create(activity);

			return Ok();
		}

		[HttpGet]
		public IEnumerable<MainActivity> Get()
		{
			return _activityRepository.Read().ToArray();
		}
	}

}
