using APUS.Server.Controllers.GroupsController;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class GroupPostControllerTests
	{
		private readonly Mock<IGroupService> _svcMock = new();
		private readonly Mock<IGroupPostCommentRepository> _commentRepoMock = new();
		private readonly GroupPostController _ctrl;

		public GroupPostControllerTests()
		{
			_ctrl = new GroupPostController(_svcMock.Object, _commentRepoMock.Object);

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
		public async Task Delete_CallsServiceAndReturnsNoContent()
		{
			var result = await _ctrl.Delete(3, CancellationToken.None);

			result.Should().BeOfType<NoContentResult>();
			_svcMock.Verify(s => s.DeletePostAsync("u1", 3, It.IsAny<CancellationToken>()), Times.Once);
		}


		[Fact]
		public async Task AddComment_InvalidModel_ReturnsBadRequest()
		{
			_ctrl.ModelState.AddModelError("Text", "Required");

			var req = new CreateCommentRequest { Text = "Test" };
			var result = await _ctrl.AddComment(5, req, CancellationToken.None);

			result.Result.Should().BeOfType<BadRequestObjectResult>();
		}

	}
}
