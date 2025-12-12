using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations.UserServices;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Services
{
	public class FriendServiceTests
	{
		private readonly Mock<IUserRelationRepository> _repoMock = new();
		private readonly FriendService _service;

		public FriendServiceTests()
		{
			_service = new FriendService(_repoMock.Object);
		}

		[Fact]
		public async Task GetStatusesAsync_OnlySelf_ReturnsEmpty()
		{
			var result = await _service.GetStatusesAsync("me", new[] { "me" }, CancellationToken.None);

			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetStatusesAsync_NoRelations_DefaultsToCanSend()
		{
			var targets = new[] { "a", "b" };

			_repoMock.Setup(r => r.GetBetweenAsync("me", targets, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new List<UserRelation>());

			_repoMock.Setup(r => r.GetAllowFollowMapAsync(targets, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new Dictionary<string, bool?>());

			var result = await _service.GetStatusesAsync("me", targets, CancellationToken.None);

			result.Keys.Should().BeEquivalentTo(targets);
		}

		[Fact]
		public async Task SendRequestAsync_CannotSendToSelf_ReturnsFalse()
		{
			var ok = await _service.SendRequestAsync("me", "me", CancellationToken.None);

			ok.Should().BeFalse();
		}

		[Fact]
		public async Task SendRequestAsync_ExistingAccepted_ReturnsFalse()
		{
			var rel = new UserRelation
			{
				UserId = "me",
				FriendId = "you",
				Status = UserRelationStatus.Accepted
			};

			_repoMock.Setup(r => r.FindEitherDirectionAsync("me", "you", It.IsAny<CancellationToken>()))
				.ReturnsAsync(rel);

			var ok = await _service.SendRequestAsync("me", "you", CancellationToken.None);

			ok.Should().BeFalse();
		}

		[Fact]
		public async Task SendRequestAsync_ExistingPendingOppositeDirection_Accepts()
		{
			var rel = new UserRelation
			{
				UserId = "you",
				FriendId = "me",
				Status = UserRelationStatus.Pending
			};

			_repoMock.Setup(r => r.FindEitherDirectionAsync("me", "you", It.IsAny<CancellationToken>()))
				.ReturnsAsync(rel);

			_repoMock.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var ok = await _service.SendRequestAsync("me", "you", CancellationToken.None);

			ok.Should().BeTrue();
			rel.Status.Should().Be(UserRelationStatus.Accepted);
			_repoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task SendRequestAsync_TargetDoesNotAllowFollow_ReturnsFalse()
		{
			_repoMock.Setup(r => r.FindEitherDirectionAsync("me", "you", It.IsAny<CancellationToken>()))
				.ReturnsAsync((UserRelation?)null);

			_repoMock.Setup(r => r.GetAllowFollowAsync("you", It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			var ok = await _service.SendRequestAsync("me", "you", CancellationToken.None);

			ok.Should().BeFalse();
		}

		[Fact]
		public async Task SendRequestAsync_NewPending_AddsRelation()
		{
			_repoMock.Setup(r => r.FindEitherDirectionAsync("me", "you", It.IsAny<CancellationToken>()))
				.ReturnsAsync((UserRelation?)null);

			_repoMock.Setup(r => r.GetAllowFollowAsync("you", It.IsAny<CancellationToken>()))
				.ReturnsAsync((bool?)null);

			_repoMock.Setup(r => r.AddAsync(It.IsAny<UserRelation>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			_repoMock.Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var ok = await _service.SendRequestAsync("me", "you", CancellationToken.None);

			ok.Should().BeTrue();
			_repoMock.Verify(r => r.AddAsync(It.Is<UserRelation>(ur => ur.UserId == "me" && ur.FriendId == "you" && ur.Status == UserRelationStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);
			_repoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

	}
}
