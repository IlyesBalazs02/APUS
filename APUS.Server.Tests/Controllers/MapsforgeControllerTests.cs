using APUS.Server.Controllers.MapController;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class MapsforgeControllerTests
	{
		private readonly Mock<IMapsforgeService> _svcMock = new();
		private readonly MapsforgeController _ctrl;

		public MapsforgeControllerTests()
		{
			_ctrl = new MapsforgeController(_svcMock.Object);

			var httpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(
					new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.NameIdentifier, "u1")
					}, "Test"))
			};

			_ctrl.ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			};
		}

		[Fact]
		public async Task ExportFromTrackFile_NullRequest_ReturnsBadRequest()
		{
			var result = await _ctrl.ExportFromTrackFile(null!);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("TrackFileName is required.");
		}

		[Fact]
		public async Task ExportFromTrackFile_EmptyTrackFileName_ReturnsBadRequest()
		{
			var req = new MapsforgeTrackFileRequest { TrackFileName = "  " };

			var result = await _ctrl.ExportFromTrackFile(req);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("TrackFileName is required.");
		}

		[Fact]
		public async Task DownloadTrackGpx_NullRequest_ReturnsBadRequest()
		{
			var result = await _ctrl.DownloadTrackGpx(null!);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("TrackFileName is required.");
		}

		[Fact]
		public async Task DownloadTrackGpx_EmptyTrackFileName_ReturnsBadRequest()
		{
			var req = new MapsforgeTrackFileRequest { TrackFileName = "" };

			var result = await _ctrl.DownloadTrackGpx(req);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("TrackFileName is required.");
		}
	}
}
