using APUS.Server.Controllers.GroupsController;
using APUS.Server.Domain.DTOs.Feature.Search;
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
	public class GroupEventControllerTests
	{
		private readonly Mock<IGroupEventService> _svcMock = new();
		private readonly GroupEventController _ctrl;

		public GroupEventControllerTests()
		{
			_ctrl = new GroupEventController(_svcMock.Object);

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
		public async Task Create_InvalidModel_ReturnsValidationProblem()
		{
			_ctrl.ModelState.AddModelError("Title", "Required");

			var req = new CreateGroupEventRequest();
			var result = await _ctrl.Create(3, req, CancellationToken.None);

			result.Result.Should().BeOfType<ObjectResult>()
				.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
		}

		[Fact]
		public async Task Create_Valid_ReturnsCreated()
		{
			var req = new CreateGroupEventRequest();
			var dto = new GroupEventDto { Id = 9 };

			_svcMock
				.Setup(s => s.CreateEventAsync("u1", 3, req, It.IsAny<CancellationToken>()))
				.ReturnsAsync(dto);

			var result = await _ctrl.Create(3, req, CancellationToken.None);

			var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
			created.Value.Should().Be(dto);
			created.ActionName.Should().Be(nameof(GroupEventController.List));
			created.RouteValues.Should().ContainKey("groupId").WhoseValue.Should().Be(3L);
		}

		[Fact]
		public async Task Delete_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.Delete(4, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.DeleteEventAsync("u1", 4, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task GetParticipants_ReturnsOkWithList()
		{
			var list = new List<GroupEventParticipantDto>();

			_svcMock
				.Setup(s => s.GetParticipantsAsync("u1", 6, It.IsAny<CancellationToken>()))
				.ReturnsAsync(list);

			var result = await _ctrl.GetParticipants(6, CancellationToken.None);

			result.Result.Should().BeOfType<OkObjectResult>()
				.Which.Value.Should().Be(list);
		}

		[Fact]
		public async Task JoinEvent_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.JoinEvent(2, 7, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.JoinEventAsync("u1", 2, 7, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task LeaveEvent_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.LeaveEvent(2, 7, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.LeaveEventAsync("u1", 2, 7, It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
