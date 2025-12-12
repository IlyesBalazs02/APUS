using APUS.Server.Services.Implementations.MapServices;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Services
{
	public class MapsforgeServiceTests
	{
		private readonly Mock<IWebHostEnvironment> _envMock = new();
		private readonly MapsforgeService _service;
		private readonly string _root;

		public MapsforgeServiceTests()
		{
			_root = Path.Combine(Path.GetTempPath(), "apus_mapsforge_tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_root);
			_envMock.SetupGet(e => e.ContentRootPath).Returns(_root);
			_envMock.SetupGet(e => e.WebRootPath).Returns(_root);
			_service = new MapsforgeService(_envMock.Object);
		}

		[Fact]
		public void ValidateBbox_InvalidOrdering_ReturnsFalse()
		{
			var (ok, message) = _service.ValidateBbox(top: 47, bottom: 48, left: 19, right: 20);

			ok.Should().BeFalse();
			message.Should().Contain("Invalid bbox");
		}

		[Fact]
		public void ValidateBbox_TooLarge_ReturnsFalse()
		{
			var (ok, message) = _service.ValidateBbox(top: 47.6, bottom: 47.0, left: 19.0, right: 19.6);

			ok.Should().BeFalse();
			message.Should().Contain("too large");
		}

		[Fact]
		public void ValidateBbox_Valid_ReturnsOk()
		{
			var (ok, message) = _service.ValidateBbox(top: 47.4, bottom: 47.0, left: 19.0, right: 19.3);

			ok.Should().BeTrue();
			message.Should().Be("OK");
		}

		[Fact]
		public async Task GenerateMapFromTrackFileAsync_NoCoords_ReturnsNull()
		{
			var userDir = Path.Combine(_root, "Users", "u1", "Tracks");
			Directory.CreateDirectory(userDir);
			var gpxPath = Path.Combine(userDir, "track1.gpx");

			var xml = @"<?xml version=""1.0""?>
<gpx version=""1.1"" creator=""test"" xmlns=""http://www.topografix.com/GPX/1/1"">
</gpx>";

			File.WriteAllText(gpxPath, xml);

			var result = await _service.GenerateMapFromTrackFileAsync("u1", "track1");

			result.Should().BeNull();
		}

		[Fact]
		public async Task GetTrackGpxAsync_FileNotFound_Throws()
		{
			await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetTrackGpxAsync("u1", "no_such"));
		}
	}
}
