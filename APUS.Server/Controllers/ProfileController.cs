using APUS.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProfileController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly IActivityRepository _activityRepository;

		public ProfileController(ILogger<ActivitiesController> logger, IActivityRepository activityRepository)
		{
			_logger = logger;
			_activityRepository = activityRepository;
		}


	}
}
