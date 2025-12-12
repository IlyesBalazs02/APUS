using APUS.Server.Controllers.GroupsController;
using APUS.Server.Core.Helpers;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class GroupsControllerTests
	{
		private readonly Mock<IGroupService> _svcMock = new();
		private readonly GroupsController _ctrl;

		public GroupsControllerTests()
		{
			_ctrl = new GroupsController(_svcMock.Object);

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
		public async Task Get_NotFound_ReturnsNotFound()
		{
			_svcMock
				.Setup(s => s.GetForUserAsync(5, "u1", It.IsAny<CancellationToken>()))
				.ReturnsAsync((GroupDto?)null);

			var result = await _ctrl.Get(5, CancellationToken.None);

			result.Result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task Search_ClampsTakeToMax50()
		{
			int capturedTake = 0;

			_svcMock
				.Setup(s => s.SearchAsync("run", 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Callback<string, int, int, CancellationToken>((q, skip, take, ct) => capturedTake = take)
				.ReturnsAsync(new List<GroupDto>());

			var result = await _ctrl.Search("run", 0, 100, CancellationToken.None);

			result.Result.Should().BeOfType<OkObjectResult>();
			capturedTake.Should().Be(50);
		}

		[Fact]
		public async Task Join_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.Join(7, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.RequestToJoinAsync("u1", 7, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Decide_CallsServiceAndReturnsNoContent()
		{
			var dto = new DecideJoinRequestDto { Approve = true };

			var result = await _ctrl.Decide(3, dto, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.ApproveOrRejectAsync("u1", 3, true, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Leave_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.Leave(11, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.LeaveAsync("u1", 11, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Update_CallsServiceAndReturnsNoContent()
		{
			var dto = new UpdateGroupDto();

			var result = await _ctrl.Update(9, dto, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.UpdateAsync("u1", 9, dto, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Members_ReturnsOkWithList()
		{
			var list = new List<GroupMemberDto>();

			_svcMock
				.Setup(s => s.GetMembersAsync(4, It.IsAny<CancellationToken>()))
				.ReturnsAsync(list);

			var result = await _ctrl.Members(4, CancellationToken.None);

			result.Result.Should().BeOfType<OkObjectResult>()
				.Which.Value.Should().Be(list);
		}

		[Fact]
		public async Task Kick_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.Kick(2, "u2", CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.KickAsync("u1", 2, "u2", It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Requests_ReturnsOkWithRequests()
		{
			var list = new List<GroupJoinRequestDto>();

			_svcMock
				.Setup(s => s.GetPendingRequestsAsync("u1", 8, It.IsAny<CancellationToken>()))
				.ReturnsAsync(list);

			var result = await _ctrl.Requests(8, CancellationToken.None);

			result.Result.Should().BeOfType<OkObjectResult>()
				.Which.Value.Should().Be(list);
		}
	}
}
