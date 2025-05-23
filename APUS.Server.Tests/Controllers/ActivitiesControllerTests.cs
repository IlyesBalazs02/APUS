using APUS.Server.Controllers;
using APUS.Server.Data;
using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class ActivitiesControllerTests
	{
		private readonly Mock<IActivityRepository> _repo = new();
		private readonly Mock<IStorageService> _storage = new();
		private readonly Mock<ILogger<ActivitiesController>> _logger = new();

		private ActivitiesController CreateController(string? userId = "test-user")
		{
			var ctrl = new ActivitiesController(
				_logger.Object,
				_repo.Object,
				_storage.Object);

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


		[Fact]
		public async Task CreateActivity_ValidModel_CallsRepoAndStorage()
		{
			// Arrange
			var ctrl = CreateController("TestUser");
			var activity = new MainActivity
			{
				Id = "A1B1C1D1",
				Title = "Run",
				Date = DateTime.UtcNow,
				Duration = TimeSpan.FromMinutes(30),
				Calories = 200,
				AvgHeartRate = 140,
				DisplayName = "Alice",
				//LikedBy = new List<string>(),
				Description = "Morning run"
			};

			// Act
			var actionResult = await ctrl.CreateActivity(activity);

			// Assert
			var created = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
			created.ActionName.Should().Be(nameof(ctrl.GetById));
			// repo.CreateAsync called once
			_repo.Verify(r => r.CreateAsync(activity), Times.Once);
			// storage.CreateActivityFolder called with correct args
			_storage.Verify(s => s.CreateActivityFolder("A1B1C1D1", "TestUser"), Times.Once);
		}


		[Fact]
		public async Task CreateActivity_InvalidModel_ReturnsBadRequestWithErrors()
		{
			// Arrange
			var ctrl = CreateController();
			// Simulate missing [Required] Title
			ctrl.ModelState.AddModelError("Title", "The Title field is required");
			var bad = new MainActivity();

			// Act
			var actionResult = await ctrl.CreateActivity(bad);

			// Assert
			var badReq = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
			badReq.Value.Should().BeEquivalentTo(new { errors = new[] { "The Title field is required" } });
			_repo.Verify(r => r.CreateAsync(It.IsAny<MainActivity>()), Times.Never);
		}


		[Theory]
		[InlineData("X1")]
		public async Task GetById_Found_ReturnsOk(string id)
		{
			// Arrange
			var existing = new MainActivity { Id = id };
			_repo.Setup(r => r.ReadByIdAsync(id)).ReturnsAsync(existing);
			var ctrl = CreateController();

			// Act
			var actionResult = await ctrl.GetById(id);

			// Assert
			var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeSameAs(existing);
		}


		[Fact]
		public async Task GetById_NotFound_ReturnsNotFound()
		{
			// Arrange
			_repo.Setup(r => r.ReadByIdAsync("na")).ReturnsAsync((MainActivity?)null);
			var ctrl = CreateController();

			// Act
			var actionResult = await ctrl.GetById("na");

			// Assert
			actionResult.Result.Should().BeOfType<NotFoundResult>();
		}



		[Fact]
		public async Task GetActivities_ReturnsMappedDtos()
		{
			// Arrange
			var run = new Running
			{
				Id = "R1",
				Title = "Jog",
				Date = DateTime.UtcNow,
				Duration = TimeSpan.FromMinutes(20),
				Calories = 150,
				AvgHeartRate = 130,
				DisplayName = "Me",
				TotalDistanceKm = 5,
				TotalAscentMeters = 50,
				AvgPace = 5.5,
				//LikedBy = new List<string> { "u1", "u2" }
			};
			_repo.Setup(r => r.ReadAllAsync()).ReturnsAsync(new MainActivity[] { run });
			var ctrl = CreateController();

			// Act
			var actionResult = await ctrl.GetActivities();

			// Assert
			var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
			var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<ActivityDto>>().Subject.ToArray();
			dtos.Should().ContainSingle()
				.Which.Should().BeOfType<RunningActivityDto>()
				.Which.DistanceKm.Should().Be(5);
		}

		[Fact]
		public async Task EditActivity_MismatchedId_ReturnsBadRequest()
		{
			// Arrange
			var ctrl = CreateController();
			var activity = new MainActivity { Id = "X" };

			// Act
			var result = await ctrl.EditActivity("Y", activity);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>()
				  .Which.Value.Should().Be("Mismatched activity ID.");
		}

		[Fact]
		public async Task DeleteActivity_NotFound_ReturnsNotFound()
		{
			// Arrange
			_repo.Setup(r => r.DeleteAsync("na")).Throws<KeyNotFoundException>();
			var ctrl = CreateController();

			// Act
			var result = await ctrl.DeleteActivity("na");

			// Assert
			result.Should().BeOfType<NotFoundResult>();
		}

	}
}
