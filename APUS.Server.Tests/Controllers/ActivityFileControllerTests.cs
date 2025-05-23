using APUS.Server.Controllers;
using APUS.Server.Data;
using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class ActivityFileControllerTests
	{
		private readonly Mock<ILogger<ActivityFileController>> _logger = new();
		private readonly Mock<IActivityRepository> _activityRepository = new();
		private readonly Mock<IStorageService> _storageService = new();
		private readonly Mock<ITrackpointLoader> _loader = new();
		private readonly Mock<ICreateOsmMapPng> _createOsmMapPng = new();

		private Func<string, IActivityImportService> _importerFactory;

		private ActivityFileController CreateController(string? userId = "test-user")
		{
			var ctrl = new ActivityFileController(
				_logger.Object,
				_activityRepository.Object,
				_storageService.Object,
				_loader.Object,
				_createOsmMapPng.Object,
				ext => _importerFactory(ext));

			// Fake an authenticated user
			var httpContext = new DefaultHttpContext();
			if (userId != null)
			{
				httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.NameIdentifier, userId)
				}, "TestAuth"));
			}

			ctrl.ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			};

			return ctrl;
		}

		private IFormFile MakeFakeFile(string name, byte[] data)
		{
			var ms = new MemoryStream(data);
			return new FormFile(ms, 0, data.Length, "trackFile", name)
			{
				Headers = new HeaderDictionary(),
				ContentType = "application/octet-stream"
			};
		}

		[Fact]
		public async Task ImportFile_NoFile_ReturnsBadRequest()
		{
			var ctrl = CreateController();
			var result = await ctrl.UploadActivityFile(null!);
			result.Should().BeOfType<BadRequestObjectResult>()
				  .Which.Value.Should().Be("No file provided.");
		}

		[Fact]
		public async Task UploadActivityFile_UnexpectedException_Returns500()
		{
			var fakeFile = MakeFakeFile("bad.txt", new byte[] { 0x0 });
			var importerMock = new Mock<IActivityImportService>();
			importerMock
				.Setup(s => s.ImportActivity(It.IsAny<MemoryStream>()))
				.Throws<InvalidOperationException>();

			_importerFactory = ext => importerMock.Object;

			var ctrl = CreateController();

			var result = await ctrl.UploadActivityFile(fakeFile);

			var objResult = result.Should()
								  .BeOfType<ObjectResult>()
								  .Subject;
			objResult.StatusCode.Should().Be(500);
		}



		[Fact]
		public async Task ParseFile_CreateActivity_CreateStorage_CreatePng()
		{
			var fake = MakeFakeFile("track.gpx", new byte[] { 4, 5, 6 });
			var imported = new ImportActivityModel
			{
				HasGpsTrack = true,
				TotalDistanceKm = 8.5,
				TotalAscentMeters = 200,
				TotalDescentMeters = 180,
				AvgPace = 5.5,
				StartTime = DateTime.UtcNow,
				Duration = TimeSpan.FromMinutes(40),
				TotalCalories = 300,
				AverageHeartRate = 135,
				MaximumHeartRate = 160
			};

			_importerFactory = _ => Mock.Of<IActivityImportService>(srv =>
				srv.ImportActivity(It.IsAny<MemoryStream>()) == imported);

			_createOsmMapPng
			  .Setup(p => p.GeneratePng(It.IsAny<GpsRelatedActivity>()))
			  .Returns(Task.CompletedTask);

			var ctrl = CreateController();

			var result = await ctrl.UploadActivityFile(fake);

			result.Should().BeOfType<CreatedAtRouteResult>();

			_createOsmMapPng.Verify(p =>
				p.GeneratePng(It.IsAny<GpsRelatedActivity>()),
				Times.Once);
		}


		[Fact]
		public async Task UploadActivityFile_NoGps_CreatesEntityButNoPng()
		{
			var fake = MakeFakeFile("test.fit", new byte[] { 1, 2, 3 });
			var imported = new ImportActivityModel
			{
				HasGpsTrack = false,
				StartTime = DateTime.UtcNow,
				Duration = TimeSpan.FromMinutes(10),
				TotalCalories = 100,
				AverageHeartRate = 120,
				MaximumHeartRate = 150
			};

			_importerFactory = _ => Mock.Of<IActivityImportService>(srv =>
				srv.ImportActivity(It.IsAny<MemoryStream>()) == imported);

			var ctrl = CreateController();

			var result = await ctrl.UploadActivityFile(fake);

			var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
			created.RouteName.Should().Be(nameof(ActivitiesController.GetById));

			_activityRepository.Verify(r => r.CreateAsync(It.IsAny<MainActivity>()), Times.Once);
			_storageService.Verify(s =>s.CreateActivityFolder(It.IsAny<string>(), "test-user"),Times.Once);

			_storageService.Verify(s =>s.SaveTrackAsync(It.IsAny<string>(), "test-user", fake),Times.Once);

			_createOsmMapPng.Verify(p =>p.GeneratePng(It.IsAny<MainActivity>()),Times.Never);
		}

		[Fact]
		public async Task GetTrackfile_ActivityNotFound_Returns404()
		{
			_activityRepository.Setup(r => r.ReadByIdAsync("nope"))
				 .ReturnsAsync((MainActivity?)null);
			var ctrl = CreateController();

			var result = await ctrl.GetTrackfile("nope");
			result.Result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task GetTrackfile_WithData_ReturnsOkList()
		{
			var act = new MainActivity { Id = "A1" };
			_activityRepository.Setup(r => r.ReadByIdAsync("A1")).ReturnsAsync(act);
			var pts = new List<TrackpointDto> {
				new(){ Lat=1, Lon=2, Time = DateTime.Now }
			};
			_loader.Setup(l => l.LoadTrack(act, CancellationToken.None)).ReturnsAsync(pts);

			var ctrl = CreateController();
			var result = await ctrl.GetTrackfile("A1");

			var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeEquivalentTo(pts);
		}
	}
}
