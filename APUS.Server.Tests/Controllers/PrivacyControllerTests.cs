using APUS.Server.Controllers;
using APUS.Server.Controllers.SettingsController;
using APUS.Server.Controllers.UserControllers;
using APUS.Server.Data;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs;
using APUS.Server.Domain.DTOs.Feature.Activity;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.DTOs.User;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations.Activity;
using APUS.Server.Services.Implementations.FileServices;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace APUS.Server.Tests.Controllers
{
	public class PrivacyControllerTests
	{
		private readonly AppDbContext _db;
		private readonly Mock<UserManager<SiteUser>> _userMgrMock;
		private readonly PrivacyController _controller;

		public PrivacyControllerTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;
			_db = new AppDbContext(options);

			var store = new Mock<IUserStore<SiteUser>>();
			_userMgrMock = new Mock<UserManager<SiteUser>>(
				store.Object, null, null, null, null, null, null, null, null);

			_controller = new PrivacyController(_userMgrMock.Object, _db);
			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity(
							new[] { new Claim(ClaimTypes.NameIdentifier, "u1") },
							"Test"))
				}
			};
		}

		[Fact]
		public async Task GetMine_UserNull_ReturnsUnauthorized()
		{
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync((SiteUser)null);

			var result = await _controller.GetMine();

			result.Result.Should().BeOfType<UnauthorizedResult>();
		}

		[Fact]
		public async Task GetMine_NoExistingSettings_CreatesDefaultsAndReturnsOk()
		{
			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);

			var result = await _controller.GetMine();

			var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
			var dto = ok.Value.Should().BeOfType<PrivacyController.PrivacyDto>().Subject;

			dto.AllowFollow.Should().BeTrue();
			dto.ActivityVisibility.Should().Be(VisibilityLevel.Everyone.ToString());
			dto.ProfileVisibility.Should().Be(VisibilityLevel.Everyone.ToString());

			var entity = await _db.PrivacySettings.FirstOrDefaultAsync(p => p.UserId == "u1");
			entity.Should().NotBeNull();
		}

		[Fact]
		public async Task UpdateMine_NullBody_ReturnsBadRequest()
		{
			var result = await _controller.UpdateMine(null);

			var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Body required.");
		}
	}

	public class ProfileControllerTests
	{
		private readonly Mock<UserManager<SiteUser>> _userMgrMock;
		private readonly Mock<IProfilePictureService> _profilePicMock;
		private readonly ProfileController _controller;
		private readonly DefaultHttpContext _httpContext;

		public ProfileControllerTests()
		{
			var store = new Mock<IUserStore<SiteUser>>();
			_userMgrMock = new Mock<UserManager<SiteUser>>(
				store.Object, null, null, null, null, null, null, null, null);

			_profilePicMock = new Mock<IProfilePictureService>();

			_controller = new ProfileController(_userMgrMock.Object, _profilePicMock.Object);

			_httpContext = new DefaultHttpContext();
			_httpContext.Request.Scheme = "https";
			_httpContext.Request.Host = new HostString("test.local");
			_httpContext.User = new ClaimsPrincipal(
				new ClaimsIdentity(
					new[] { new Claim(ClaimTypes.NameIdentifier, "u1") },
					"Test"));

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = _httpContext
			};
		}

		[Fact]
		public async Task GetProfile_UserNull_ReturnsUnauthorized()
		{
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync((SiteUser)null);

			var result = await _controller.GetProfile();

			var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
			unauthorized.Value.Should().Be("User not found.");
		}

		[Fact]
		public async Task GetProfile_ReturnsUserDataWithAbsoluteAvatar()
		{
			var user = new SiteUser
			{
				Id = "u1",
				FirstName = "John",
				LastName = "Doe",
				Bio = "bio"
			};

			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);

			_profilePicMock.Setup(p => p.GetProfilePictureUrlAsync("u1"))
				.ReturnsAsync("/Perm/avatar.png");

			var result = await _controller.GetProfile();

			var ok = result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeEquivalentTo(new
			{
				firstName = "John",
				lastName = "Doe",
				bio = "bio",
				avatarUrl = "https://test.local/Perm/avatar.png"
			});
		}

		[Fact]
		public async Task UpdateProfile_BioTooLong_ReturnsBadRequest()
		{
			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);

			var request = new ProfileController.UpdateProfileRequest
			{
				FirstName = "A",
				LastName = "B",
				Bio = new string('x', 301)
			};

			var result = await _controller.UpdateProfile(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Bio cannot exceed 300 characters.");
		}

		[Fact]
		public async Task UploadAvatar_NoFile_ReturnsBadRequest()
		{
			var result = await _controller.UploadAvatar(null);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("No file.");
		}

		[Fact]
		public async Task UploadAvatar_UserNull_ReturnsUnauthorized()
		{
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync((SiteUser)null);

			var fileMock = new Mock<IFormFile>();
			fileMock.SetupGet(f => f.Length).Returns(10);

			var result = await _controller.UploadAvatar(fileMock.Object);

			var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
			unauthorized.Value.Should().Be("User not found.");
		}
	}

	public class AccountControllerTests
	{
		private readonly Mock<UserManager<SiteUser>> _userMgrMock;
		private readonly AccountController _controller;

		public AccountControllerTests()
		{
			var store = new Mock<IUserStore<SiteUser>>();
			_userMgrMock = new Mock<UserManager<SiteUser>>(
				store.Object, null, null, null, null, null, null, null, null);

			_controller = new AccountController(_userMgrMock.Object);
			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity(
							new[] { new Claim(ClaimTypes.NameIdentifier, "u1") },
							"Test"))
				}
			};
		}

		[Fact]
		public async Task ChangeEmail_MissingFields_ReturnsBadRequest()
		{
			var request = new AccountController.ChangeEmailRequest
			{
				Password = "",
				NewEmail = ""
			};

			var result = await _controller.ChangeEmail(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Missing required fields.");
		}

		[Fact]
		public async Task ChangeEmail_UserNotFound_ReturnsUnauthorized()
		{
			var request = new AccountController.ChangeEmailRequest
			{
				Password = "pwd",
				NewEmail = "a@b.com"
			};

			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync((SiteUser)null);

			var result = await _controller.ChangeEmail(request);

			var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
			unauthorized.Value.Should().Be("User not found.");
		}

		[Fact]
		public async Task ChangeEmail_InvalidPassword_ReturnsBadRequest()
		{
			var request = new AccountController.ChangeEmailRequest
			{
				Password = "pwd",
				NewEmail = "a@b.com"
			};

			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);
			_userMgrMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
				.ReturnsAsync(false);

			var result = await _controller.ChangeEmail(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Invalid password.");
		}

		[Fact]
		public async Task ChangeEmail_UpdateFails_ReturnsBadRequest()
		{
			var request = new AccountController.ChangeEmailRequest
			{
				Password = "pwd",
				NewEmail = "a@b.com"
			};

			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);
			_userMgrMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
				.ReturnsAsync(true);
			_userMgrMock.Setup(x => x.UpdateAsync(user))
				.ReturnsAsync(IdentityResult.Failed());

			var result = await _controller.ChangeEmail(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Failed to update email.");
		}

		[Fact]
		public async Task ChangeEmail_Succeeds_ReturnsOk()
		{
			var request = new AccountController.ChangeEmailRequest
			{
				Password = "pwd",
				NewEmail = "a@b.com"
			};

			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);
			_userMgrMock.Setup(x => x.CheckPasswordAsync(user, request.Password))
				.ReturnsAsync(true);
			_userMgrMock.Setup(x => x.UpdateAsync(user))
				.ReturnsAsync(IdentityResult.Success);

			var result = await _controller.ChangeEmail(request);

			var ok = result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeEquivalentTo(new { message = "Email updated successfully." });
		}

		[Fact]
		public async Task ChangePassword_MissingFields_ReturnsBadRequest()
		{
			var request = new AccountController.ChangePasswordRequest
			{
				currentPassword = "",
				newPassword = ""
			};

			var result = await _controller.ChangePassword(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Both current and new passwords are required.");
		}

		[Fact]
		public async Task ChangePassword_UserNotFound_ReturnsUnauthorized()
		{
			var request = new AccountController.ChangePasswordRequest
			{
				currentPassword = "old",
				newPassword = "new"
			};

			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync((SiteUser)null);

			var result = await _controller.ChangePassword(request);

			var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
			unauthorized.Value.Should().Be("User not found.");
		}

		[Fact]
		public async Task ChangePassword_Fails_ReturnsBadRequestWithError()
		{
			var request = new AccountController.ChangePasswordRequest
			{
				currentPassword = "old",
				newPassword = "new"
			};

			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);
			_userMgrMock.Setup(x => x.ChangePasswordAsync(user, request.currentPassword, request.newPassword))
				.ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "err" }));

			var result = await _controller.ChangePassword(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("err");
		}

		[Fact]
		public async Task ChangePassword_Succeeds_ReturnsOk()
		{
			var request = new AccountController.ChangePasswordRequest
			{
				currentPassword = "old",
				newPassword = "new"
			};

			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);
			_userMgrMock.Setup(x => x.ChangePasswordAsync(user, request.currentPassword, request.newPassword))
				.ReturnsAsync(IdentityResult.Success);

			var result = await _controller.ChangePassword(request);

			var ok = result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeEquivalentTo(new { message = "Password updated successfully." });
		}

		[Fact]
		public async Task ChangeGender_InvalidValue_ReturnsBadRequest()
		{
			var user = new SiteUser { Id = "u1" };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);

			var request = new AccountController.GenderRequest
			{
				SelectedGender = "InvalidGender"
			};

			var result = await _controller.ChangeGender(request);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Invalid gender value.");
		}

		[Fact]
		public async Task GetGender_UserNotFound_ReturnsUnauthorized()
		{
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync((SiteUser)null);

			var result = await _controller.GetGender();

			var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
			unauthorized.Value.Should().Be("User not found.");
		}

		[Fact]
		public async Task GetGender_ReturnsGender()
		{
			var user = new SiteUser { Id = "u1", Gender = GenderType.Male };
			_userMgrMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
				.ReturnsAsync(user);

			var result = await _controller.GetGender();

			var ok = result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().BeEquivalentTo(new { gender = "Male" });
		}
	}

	public class SiteUserControllerTests
	{
		private readonly Mock<ISearchUsersService> _searchServiceMock;
		private readonly SiteUserController _controller;

		public SiteUserControllerTests()
		{
			_searchServiceMock = new Mock<ISearchUsersService>();
			_controller = new SiteUserController(_searchServiceMock.Object);
			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity(
							new[] { new Claim(ClaimTypes.NameIdentifier, "u1") },
							"Test"))
				}
			};
		}


		[Fact]
		public async Task GetAllUser_ReturnsMappedDtos()
		{
			var entities = new List<SiteUser>
			{
				new SiteUser { Id = "1", FirstName = "John", LastName = "Doe" },
				new SiteUser { Id = "2", FirstName = "Jane", LastName = "Smith" }
			};

			_searchServiceMock.Setup(s => s.GetAllUser())
				.ReturnsAsync(entities);

			var result = await _controller.GetAllUser();

			var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
			var dtos = ok.Value.Should().BeAssignableTo<IEnumerable<UserMatchDto>>().Subject.ToList();

			dtos.Should().HaveCount(2);
			dtos[0].Id.Should().Be("1");
			dtos[0].FullName.Should().Be("JohnDoe");
			dtos[1].Id.Should().Be("2");
			dtos[1].FullName.Should().Be("JaneSmith");
		}
	}

	public class ActivityFileControllerTests
	{
		private readonly Mock<ILogger<ActivityFileController>> _loggerMock = new();
		private readonly Mock<IActivityRepository> _activityRepoMock = new();
		private readonly Mock<IStorageService> _storageMock = new();
		private readonly Mock<ITrackpointLoader> _loaderMock = new();
		private readonly Mock<ICreateOsmMapPng> _pngMock = new();
		private readonly Mock<IHuberRegressor> _huberMock = new();
		private readonly Mock<IActivityImportService> _importerMock = new();
		private readonly ActivityFileController _controller;

		public ActivityFileControllerTests()
		{
			Func<string, IActivityImportService> importerFactory = _ => _importerMock.Object;

			_controller = new ActivityFileController(
				_loggerMock.Object,
				_activityRepoMock.Object,
				_storageMock.Object,
				_loaderMock.Object,
				_pngMock.Object,
				importerFactory,
				_huberMock.Object);

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity(
							new[] { new Claim(ClaimTypes.NameIdentifier, "u1") },
							"Test"))
				}
			};
		}

		[Fact]
		public async Task UploadActivityFile_NoFile_ReturnsBadRequest()
		{
			var result = await _controller.UploadActivityFile(null, null);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("No file provided.");
		}

		[Fact]
		public async Task UploadActivityFile_EmptyFile_ReturnsBadRequest()
		{
			var fileMock = new Mock<IFormFile>();
			fileMock.SetupGet(f => f.Length).Returns(0);

			var result = await _controller.UploadActivityFile(fileMock.Object, null);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("No file provided.");
		}


		[Fact]
		public async Task GetTrackfile_ActivityNotFound_ReturnsNotFound()
		{
			_activityRepoMock.Setup(r => r.ReadByIdAsync("a1"))
				.ReturnsAsync((MainActivity)null);

			var result = await _controller.GetTrackfile("a1");

			result.Result.Should().BeOfType<NotFoundResult>();
		}
	}

	public class ActivitiesControllerTests
	{
		private readonly Mock<ILogger<ActivitiesController>> _loggerMock = new();
		private readonly Mock<IActivityRepository> _activityRepoMock = new();
		private readonly Mock<IStorageService> _storageMock = new();
		private readonly Mock<IActivityService> _activityServiceMock = new();
		private readonly Mock<IActivityCommentRepository> _commentRepoMock = new();
		private readonly ActivitiesController _controller;

		public ActivitiesControllerTests()
		{
			_controller = new ActivitiesController(
				_loggerMock.Object,
				_activityRepoMock.Object,
				_storageMock.Object,
				_activityServiceMock.Object,
				_commentRepoMock.Object);

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity(
							new[] { new Claim(ClaimTypes.NameIdentifier, "u1") },
							"Test"))
				}
			};
		}

		[Fact]
		public async Task CreateActivity_InvalidModel_ReturnsBadRequestWithErrors()
		{
			_controller.ModelState.AddModelError("Title", "Required");

			var activity = new MainActivity();

			var result = await _controller.CreateActivity(activity);

			var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
			var payload = bad.Value.Should().BeAssignableTo<object>().Subject;
			payload.Should().NotBeNull();
		}

		[Fact]
		public async Task GetById_NotFound_ReturnsNotFound()
		{
			_activityRepoMock.Setup(r => r.ReadByIdAsync("a1"))
				.ReturnsAsync((MainActivity)null);

			var result = await _controller.GetById("a1");

			result.Result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task GetFeedPaged_ClampsTakeAndReturnsPagedResponse()
		{
			var activities = new List<MainActivity>
			{
				new MainActivity { Id = "1", UserId = "u2" },
				new MainActivity { Id = "2", UserId = "u2" }
			};

			_activityRepoMock.Setup(r => r.GetFeedPagedAsync("u1", 0, 11))
				.ReturnsAsync(activities);

			var result = await _controller.GetFeedPaged(0, 100);

			var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
			var dto = ok.Value.Should().BeAssignableTo<PagedResponse<ActivityDto>>().Subject;
			dto.Items.Should().HaveCount(2);
			dto.HasMore.Should().BeFalse();
		}

		[Fact]
		public async Task DeleteActivity_NotFound_ReturnsNotFound()
		{
			_activityRepoMock.Setup(r => r.ReadByIdAsync("a1"))
				.ReturnsAsync((MainActivity)null);

			var result = await _controller.DeleteActivity("a1");

			result.Should().BeOfType<NotFoundResult>();
		}
	}
}
