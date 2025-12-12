using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Activity;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations.Activity;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Services
{
	public class ActivityServiceTests
	{
		private readonly Mock<IActivityRepository> _repoMock = new();
		private readonly ActivityService _service;

		public ActivityServiceTests()
		{
			_service = new ActivityService(_repoMock.Object);
		}

		[Fact]
		public async Task EditActivityAsync_SameType_UpdatesFieldsAndSaves()
		{
			var existing = new Running
			{
				Id = "a1",
				UserId = "u1",
				Title = "Old",
				Description = "Old desc",
				Date = new DateTime(2024, 1, 1)
			};

			var currentType = Enum.Parse<ActivityType>(existing.ActivityType);
			var req = new EditActivityRequest
			{
				Id = "a1",
				ActivityType = currentType,
				Title = "New title",
				Description = "New desc",
				Date = new DateTime(2024, 2, 1)
			};

			_repoMock.Setup(r => r.SaveAsync(existing)).Returns(Task.CompletedTask);

			await _service.EditActivityAsync(existing, req);

			existing.Title.Should().Be("New title");
			existing.Description.Should().Be("New desc");
			existing.Date.Should().Be(req.Date);

			_repoMock.Verify(r => r.SaveAsync(existing), Times.Once);
			_repoMock.Verify(r => r.CopyProps(It.IsAny<MainActivity>(), It.IsAny<MainActivity>()), Times.Never);
			_repoMock.Verify(r => r.ReplaceAsync(It.IsAny<MainActivity>(), It.IsAny<MainActivity>()), Times.Never);
		}

		[Fact]
		public async Task EditActivityAsync_TypeChanged_CreatesReplacementAndCallsRepo()
		{
			var existing = new Running
			{
				Id = "a1",
				UserId = "u1",
				Title = "Old",
				Description = "Old desc",
				Date = new DateTime(2024, 1, 1)
			};

			var req = new EditActivityRequest
			{
				Id = "a1",
				ActivityType = ActivityType.Hiking,
				Title = "New title",
				Description = "New desc",
				Date = new DateTime(2024, 2, 1)
			};

			MainActivity? replacementCaptured = null;

			_repoMock
				.Setup(r => r.CopyProps(existing, It.IsAny<MainActivity>()))
				.Callback<MainActivity, MainActivity>((_, repl) => replacementCaptured = repl)
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.ReplaceAsync(existing, It.IsAny<MainActivity>()))
				.Returns(Task.CompletedTask);

			await _service.EditActivityAsync(existing, req);

			replacementCaptured.Should().NotBeNull();
			replacementCaptured!.Id.Should().Be(existing.Id);
			replacementCaptured.UserId.Should().Be(existing.UserId);
			replacementCaptured.Title.Should().Be(req.Title);
			replacementCaptured.Description.Should().Be(req.Description);
			replacementCaptured.Date.Should().Be(req.Date);
			replacementCaptured.Should().BeOfType<Hiking>();

			_repoMock.Verify(r => r.SaveAsync(existing), Times.Never);
			_repoMock.Verify(r => r.CopyProps(existing, It.IsAny<MainActivity>()), Times.Once);
			_repoMock.Verify(r => r.ReplaceAsync(existing, It.IsAny<MainActivity>()), Times.Once);
		}

		[Fact]
		public async Task EditActivityAsync_UnsupportedType_Throws()
		{
			var existing = new Running
			{
				Id = "a1",
				UserId = "u1",
				Title = "Old",
				Description = "Old desc",
				Date = new DateTime(2024, 1, 1)
			};

			var req = new EditActivityRequest
			{
				Id = "a1",
				ActivityType = (ActivityType)999,
				Title = "New",
				Description = "New",
				Date = DateTime.UtcNow
			};

			await Assert.ThrowsAsync<NotSupportedException>(() => _service.EditActivityAsync(existing, req));
		}

		[Fact]
		public async Task ToggleLikeAsync_ActivityNotFound_ReturnsNull()
		{
			_repoMock.Setup(r => r.ReadByIdAsync("a1")).ReturnsAsync((MainActivity?)null);

			var result = await _service.ToggleLikeAsync("a1", "u1");

			result.Should().BeNull();
		}

		[Fact]
		public async Task ToggleLikeAsync_TogglesLikeAndReturnsCounts()
		{
			var user = new SiteUser { Id = "u1" };
			var activity = new Running
			{
				Id = "a1",
				UserId = "u1",
				User = user,
				LikedBy = new List<SiteUser>()
			};

			_repoMock.Setup(r => r.ReadByIdAsync("a1")).ReturnsAsync(activity);
			_repoMock.Setup(r => r.SaveAsync(activity)).Returns(Task.CompletedTask);

			var first = await _service.ToggleLikeAsync("a1", "u1");
			first.Should().NotBeNull();
			first!.Value.likes.Should().Be(1);
			first.Value.isLiked.Should().BeTrue();
			activity.LikedBy.Should().ContainSingle().Which.Should().Be(user);

			var second = await _service.ToggleLikeAsync("a1", "u1");
			second.Should().NotBeNull();
			second!.Value.likes.Should().Be(0);
			second.Value.isLiked.Should().BeFalse();
			activity.LikedBy.Should().BeEmpty();

			_repoMock.Verify(r => r.SaveAsync(activity), Times.Exactly(2));
		}
	}
}
