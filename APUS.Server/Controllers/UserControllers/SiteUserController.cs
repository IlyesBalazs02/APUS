using APUS.Server.Domain.DTOs.Feature;
using APUS.Server.Domain.DTOs.User;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.UserControllers
{

	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class SiteUserController : ControllerBase
	{
		private readonly ISearchUsersService _searchUsersService;

		public SiteUserController(ISearchUsersService searchUsersService)
		{
			_searchUsersService = searchUsersService;
		}

		[HttpGet("get-all-user")]
		[Authorize]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(IEnumerable<UserMatchDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<IEnumerable<UserMatchDto>>> GetAllUser()
		{
			var entities = await _searchUsersService.GetAllUser();

			if (entities == null) return NotFound();

			var dtos = entities.Select(u => new UserMatchDto
			{
				Id = u.Id.ToString(),
				FullName = u.FirstName + u.LastName
			});

			return Ok(dtos);
		}
	}
}
