using APUS.Server.Models.Activities;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ActivitiesController : ControllerBase
	{
		[HttpPost]
		public IActionResult CreateActivity([FromBody] MainActivity activity)
		{
			Console.WriteLine("asd");
			if (activity is Running run)
			{
				// Handle running-specific logic
				Console.WriteLine($"Running activity: {run.Distance} meters at {run.Pace} pace.");
			}
			else if (activity is Bouldering climb)
			{
				// Handle bouldering-specific logic
				Console.WriteLine($"Bouldering activity: {climb.Difficulty} difficulty with {climb.RedPoint} red points.");
			}

			return Ok();
		}
	}
}
