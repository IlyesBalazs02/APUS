using APUS.Server.Controllers;
using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class AuthControllerTests
	{
		private readonly Mock<IConfiguration> _configMock = new();
		private readonly Mock<UserManager<SiteUser>> _userMgrMock;
		private readonly Mock<SignInManager<SiteUser>> _signInMgrMock;
		private readonly Mock<IStorageService> _storageMock = new();
		private readonly AuthController _auth;

		public AuthControllerTests()
		{
			var storeMock = new Mock<IUserStore<SiteUser>>();
			_userMgrMock = new Mock<UserManager<SiteUser>>(
				storeMock.Object, null, null, null, null, null, null, null, null);

			var contextAccessor = new Mock<IHttpContextAccessor>();
			var claimsFactory = new Mock<IUserClaimsPrincipalFactory<SiteUser>>();
			_signInMgrMock = new Mock<SignInManager<SiteUser>>(
				_userMgrMock.Object,
				contextAccessor.Object,
				claimsFactory.Object,
				null, null, null, null);

			_configMock
				.Setup(c => c["Jwt:Key"])
				.Returns("ThisIsASecretKeyForTestingPurposes!");
			_configMock
				.Setup(c => c["Jwt:Issuer"])
				.Returns("TestIssuer");
			_configMock
				.Setup(c => c["Jwt:Audience"])
				.Returns("TestAudience");

			_auth = new AuthController(
				_configMock.Object,
				_userMgrMock.Object,
				_signInMgrMock.Object,
				_storageMock.Object);

			_auth.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(
						new ClaimsIdentity(new[] {
							new Claim(ClaimTypes.NameIdentifier, "u123")
						}, "Test"))
				}
			};
		}

		[Fact]
		public async Task Register_PasswordsDontMatch_ReturnsBadRequest()
		{
			var dto = new RegisterDto
			{
				Email = "a@b.com",
				Password = "pass1",
				ConfirmPassword = "pass2"
			};

			var result = await _auth.Register(dto);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			bad.Value.Should().Be("Passwords must match.");
		}

		[Fact]
		public async Task Register_CreateFails_ReturnsBadRequestWithErrors()
		{
			var dto = new RegisterDto
			{
				Email = "a@b.com",
				Password = "pwd",
				ConfirmPassword = "pwd"
			};
			_userMgrMock
			   .Setup(m => m.CreateAsync(It.IsAny<SiteUser>(), dto.Password))
			   .ReturnsAsync(IdentityResult.Failed(
				   new IdentityError { Code = "E1", Description = "Bad" }));

			var result = await _auth.Register(dto);

			var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
			var errs = Assert.IsAssignableFrom<IEnumerable<IdentityError>>(bad.Value);
			errs.Should().Contain(e => e.Code == "E1");
		}

		[Fact]
		public async Task Register_Succeeds_CreatesFolderAndReturnsOk()
		{
			var dto = new RegisterDto
			{
				Email = "c@d.com",
				Password = "pwd",
				ConfirmPassword = "pwd"
			};
			_userMgrMock
			   .Setup(m => m.CreateAsync(It.IsAny<SiteUser>(), dto.Password))
			   .ReturnsAsync(IdentityResult.Success);

			var result = await _auth.Register(dto);

			result.Should().BeOfType<OkResult>();
			_storageMock.Verify(s => s.CreateUserFolder(It.IsAny<string>()), Times.Once);
		}

		[Fact]
		public async Task Login_InvalidCredentials_ReturnsUnauthorized()
		{
			var dto = new LoginDto { Email = "x@y.com", Password = "nop" };
			_signInMgrMock
			  .Setup(s => s.PasswordSignInAsync(
				 dto.Email, dto.Password, false, false))
			  .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);


			var result = await _auth.Login(dto);

			result.Should().BeOfType<UnauthorizedObjectResult>()
				  .Which.Value.Should().Be("Invalid login");
		}

		[Fact]
		public async Task Login_ValidCredentials_ReturnsJwtToken()
		{
			var dto = new LoginDto { Email = "u@v.com", Password = "yes" };
			_signInMgrMock
			  .Setup(s => s.PasswordSignInAsync(
				 dto.Email, dto.Password, false, false))
			  .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

			var user = new SiteUser { Id = "ID99", Email = dto.Email, UserName = dto.Email };
			_userMgrMock
			  .Setup(m => m.FindByEmailAsync(dto.Email))
			  .ReturnsAsync(user);

			var result = await _auth.Login(dto);

			var ok = result.Should().BeOfType<OkObjectResult>().Subject;
			var payload = ok.Value.Should().BeOfType<Dictionary<string, string>>().Subject;
			payload.Should().ContainKey("token");

			var jwt = payload["token"];
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			handler.ReadJwtToken(jwt).Claims
				   .Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "ID99");
		}
	}
}
