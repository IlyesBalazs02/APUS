using APUS.Server.Data;
using APUS.Server.Models;
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
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
				return BadRequest(new { errors });
			}
			Console.WriteLine(activity);
			//_activityRepository.Create(activity);

			return Ok();
		}

		[HttpGet]
		public IEnumerable<MainActivity> Get()
		{
			//return _activityRepository.Read().ToArray();
			return null;
		}
	}

}
