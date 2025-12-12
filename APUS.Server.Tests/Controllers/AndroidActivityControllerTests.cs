using APUS.Server.Controllers.AndroidControllers;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations.FileServices;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace APUS.Server.Tests.Controllers
{
	public class AndroidActivityControllerTests
	{
		private readonly Mock<IActivityRepository> _activityRepoMock = new();
		private readonly Mock<IStorageService> _storageMock = new();
		private readonly Mock<ICreateOsmMapPng> _pngMock = new();
		private readonly Mock<IActivityImportService> _importerMock = new();
		private readonly Mock<IHuberRegressor> _huberMock = new();
		private readonly Mock<ILogger<AndroidActivityController>> _loggerMock = new();
		private readonly AndroidActivityController _ctrl;

		public AndroidActivityControllerTests()
		{
			Func<string, IActivityImportService> importerFactory = _ => _importerMock.Object;

			_ctrl = new AndroidActivityController(
				_activityRepoMock.Object,
				_storageMock.Object,
				_pngMock.Object,
				importerFactory,
				_huberMock.Object,
				_loggerMock.Object);

			var httpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(
					new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.NameIdentifier, "u123")
					}, "Test"))
			};

			_ctrl.ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			};
		}

		[Fact]
		public async Task CreateNonGps_NullRequest_ReturnsBadRequest()
		{
			var result = await _ctrl.CreateNonGps(null!);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("Missing body.");
		}

		[Fact]
		public async Task CreateNonGps_EmptyActivityType_ReturnsBadRequest()
		{
			var req = new NonGpsActivityUploadRequest
			{
				ActivityType = "   ",
				DurationSeconds = 100,
				StartTimeUnixSeconds = 1
			};

			var result = await _ctrl.CreateNonGps(req);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("ActivityType is required.");
		}

		[Fact]
		public async Task CreateNonGps_NonPositiveDuration_ReturnsBadRequest()
		{
			var req = new NonGpsActivityUploadRequest
			{
				ActivityType = "Yoga",
				DurationSeconds = 0,
				StartTimeUnixSeconds = 1
			};

			var result = await _ctrl.CreateNonGps(req);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("DurationSeconds must be > 0.");
		}

		[Fact]
		public async Task CreateNonGps_ValidYogaActivity_CreatesAndReturnsOk()
		{
			MainActivity? captured = null;

			_activityRepoMock
				.Setup(r => r.CreateAsync(It.IsAny<MainActivity>()))
				.Callback<MainActivity>(a =>
				{
					a.Id = "42";
					captured = a;
				})
				.Returns(Task.CompletedTask);

			var startUnix = 1_000_000L;
			var req = new NonGpsActivityUploadRequest
			{
				ActivityType = "Yoga",
				StartTimeUnixSeconds = startUnix,
				DurationSeconds = 3600
			};

			var result = await _ctrl.CreateNonGps(req);

			var ok = result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeEquivalentTo(new { activityId = 42 });

			captured.Should().NotBeNull();
			captured.Should().BeOfType<Yoga>();
			captured!.UserId.Should().Be("u123");
			captured.Duration.Should().Be(TimeSpan.FromSeconds(3600));
			captured.Date.Should().Be(DateTimeOffset.FromUnixTimeSeconds(startUnix).UtcDateTime);
		}

		[Fact]
		public async Task CreateGps_NullFile_ReturnsBadRequest()
		{
			var result = await _ctrl.CreateGps(null!, null);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("No file provided.");
		}

		[Fact]
		public async Task CreateGps_EmptyFile_ReturnsBadRequest()
		{
			await using var ms = new MemoryStream();
			var file = new FormFile(ms, 0, 0, "track", "test.gpx");

			var result = await _ctrl.CreateGps(file, null);

			result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("No file provided.");
		}
	}
}
