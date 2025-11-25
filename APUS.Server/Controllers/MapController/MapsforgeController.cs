using APUS.Server.Core.Helpers;
using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsmSharp.API;
using System.Security.Claims;

namespace APUS.Server.Controllers.MapController
{
	[ApiController]
	[Route("api/mapsforge")]
	public class MapsforgeController : ControllerBase
	{
		private readonly IMapsforgeService _mapsforge;

		public MapsforgeController(IMapsforgeService mapsforge)
		{
			_mapsforge = mapsforge;
		}

		[HttpPost("from-track-file")]
		[Authorize]
		public async Task<IActionResult> ExportFromTrackFile([FromBody] MapsforgeTrackFileRequest request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.TrackFileName))
				return BadRequest("TrackFileName is required.");

			// Get user id from JWT claims
			var userId = User.GetUserId();

			var result = await _mapsforge.GenerateMapFromTrackFileAsync(userId, request.TrackFileName);

			if (result == null)
				return StatusCode(500, "Map generation failed (no coords or Osmosis error).");

			return File(
				result.FileBytes,
				"application/octet-stream",
				result.FileName
			);
		}
	}

	public sealed class MapsforgeTrackFileRequest
	{
		public string TrackFileName { get; set; } = string.Empty;
	}

	public static class UserExtensions
	{
		public static string GetUserId(this ClaimsPrincipal user)
			=> user.FindFirstValue(ClaimTypes.NameIdentifier)
			   ?? throw new InvalidOperationException("No user id");
	}
}
