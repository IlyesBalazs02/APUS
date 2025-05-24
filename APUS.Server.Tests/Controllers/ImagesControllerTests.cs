using APUS.Server.Controllers;
using APUS.Server.Data;
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
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class ImagesControllerTests
	{
		private readonly Mock<ILogger<ImagesController>> _loggerMock =
			new();
		private readonly Mock<IActivityRepository> _repoMock =
			new();
		private readonly Mock<IStorageService> _storageMock =
			new();

		private ImagesController CreateController(string? scheme = "https", string? host = "localhost", int port = 5000)
		{
			var ctrl = new ImagesController(
				_loggerMock.Object,
				_repoMock.Object,
				_storageMock.Object);

			var httpContext = new DefaultHttpContext();
			httpContext.Request.Scheme = scheme!;
			httpContext.Request.Host = new HostString(host!, port);

			ctrl.ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			};

			return ctrl;
		}

		private IFormFileCollection MakeFiles(params (string fileName, byte[] data)[] files)
		{
			var formFiles = new FormFileCollection();
			foreach (var (fn, data) in files)
			{
				var ms = new MemoryStream(data);
				formFiles.Add(new FormFile(ms, 0, data.Length, "images", fn)
				{
					Headers = new HeaderDictionary(),
					ContentType = "image/png"
				});
			}
			return formFiles;
		}

		[Fact]
		public async Task UploadImages_NoFiles_ReturnsBadRequest()
		{
			var ctrl = CreateController();
			var result = await ctrl.UploadImages("any-id", null!);
			result.Should().BeOfType<BadRequestObjectResult>()
				  .Which.Value.Should().Be("No files uploaded");
		}

		[Fact]
		public async Task UploadImages_ActivityNotFound_ReturnsNotFound()
		{
			_repoMock.Setup(r => r.ReadByIdAsync("X"))
					 .ReturnsAsync((MainActivity?)null);

			var ctrl = CreateController();
			var files = MakeFiles(("a.png", new byte[] { 1, 2, 3 }));

			var result = await ctrl.UploadImages("X", files);
			result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task UploadImages_Valid_CallsSaveAndReturnsNoContent()
		{
			var act = new MainActivity { Id = "A1", UserId = "U1" };
			_repoMock.Setup(r => r.ReadByIdAsync("A1"))
					 .ReturnsAsync(act);

			var ctrl = CreateController();
			var files = MakeFiles(("a.png", new byte[] { 1 }), ("b.jpg", new byte[] { 2 }));

			var result = await ctrl.UploadImages("A1", files);

			_storageMock.Verify(s =>
				s.SaveImagesAsync("A1", files, "U1"),
				Times.Once);

			result.Should().BeOfType<NoContentResult>();
		}

		[Fact]
		public async Task GetPictures_NoImages_ReturnsNotFound()
		{
			var act = new MainActivity { Id = "A2", UserId = "U2" };
			_repoMock.Setup(r => r.ReadByIdAsync("A2"))
					 .ReturnsAsync(act);
			_storageMock.Setup(s => s.GetImageFileNames("A2", "U2"))
						.Returns(new List<string>());

			var ctrl = CreateController();
			var result = await ctrl.GetPictures("A2");

			result.Result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task GetPictures_WithImages_ReturnsUrls()
		{
			var act = new MainActivity { Id = "A3", UserId = "U3" };
			_repoMock.Setup(r => r.ReadByIdAsync("A3"))
					 .ReturnsAsync(act);

			var names = new[] { "1.png", "2.jpg" };
			_storageMock.Setup(s => s.GetImageFileNames("A3", "U3"))
						.Returns(names);

			var ctrl = CreateController(scheme: "http", host: "myhost", port: 1234);
			var result = await ctrl.GetPictures("A3");

			var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
			var urls = ok.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject.ToArray();

			urls.Should().HaveCount(2);
			urls.Should().Contain(
				$"http://myhost:1234/Users/U3/Activities/A3/Images/1.png"
			);
			urls.Should().Contain(
				$"http://myhost:1234/Users/U3/Activities/A3/Images/2.jpg"
			);
		}

		[Fact]
		public async Task GetPicture_ActivityNotFound_ReturnsNotFound()
		{
			_repoMock.Setup(r => r.ReadByIdAsync("Q"))
					 .ReturnsAsync((MainActivity?)null);

			var ctrl = CreateController();
			var result = await ctrl.GetPicture("Q");

			result.Result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task GetPicture_ReturnsTrackImageUrl()
		{
			var act = new MainActivity { Id = "T1", UserId = "U5" };
			_repoMock.Setup(r => r.ReadByIdAsync("T1"))
					 .ReturnsAsync(act);

			// port 7244 hardcode
			var ctrl = CreateController(scheme: "https", host: "localhost", port: 7244);
			var result = await ctrl.GetPicture("T1");

			var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
			ok.Value.Should().Be(
				"https://localhost:7244/Users/U5/Activities/T1/ActivityTrackImage.png"
			);
		}
	}
}
