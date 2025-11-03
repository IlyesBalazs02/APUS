using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.UserControllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class SearchController : ControllerBase
	{
		private readonly ISiteUserRepository _siteUserRepository;

		public SearchController(ISiteUserRepository siteUserRepository)
		{
			_siteUserRepository = siteUserRepository;
		}

		
		

	}
}
