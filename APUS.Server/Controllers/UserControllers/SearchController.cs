using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.UserControllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class SearchController : ControllerBase
	{
		private readonly ISearchUsersService _searchUsersService;

		public SearchController(ISearchUsersService searchUsersService)
		{
			_searchUsersService = searchUsersService;
		}

		[HttpGet("search-users")]
		public async Task<ActionResult<PagedResponse<UserSearchDto>>> Search(
		[FromQuery] string? query,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 30)
		{
			// Limit page size
			if (take is < 1 or > 100) take = 30;

			var result = await _searchUsersService.SearchUsersPagedAsync(query, skip, take);

			return Ok(result);
		}

	}
}
