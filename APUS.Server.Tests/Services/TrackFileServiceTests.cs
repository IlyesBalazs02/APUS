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
	public class TrackFileServiceTests
	{
		private readonly Mock<IWebHostEnvironment> _envMock = new();
		private readonly TrackFileService _service;
		private readonly string _root;

		public TrackFileServiceTests()
		{
			_root = Path.Combine(Path.GetTempPath(), "apus_trackfile_tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_root);
			_envMock.SetupGet(e => e.WebRootPath).Returns(_root);
			_service = new TrackFileService(_envMock.Object);
		}

		[Fact]
		public void GetTrackNamesForUser_EmptyUserId_ReturnsEmpty()
		{
			var result = _service.GetTrackNamesForUser("");

			result.Should().BeEmpty();
		}

		[Fact]
		public void GetTrackNamesForUser_NoDirectory_ReturnsEmpty()
		{
			var result = _service.GetTrackNamesForUser("u1");

			result.Should().BeEmpty();
		}

		[Fact]
		public void GetTrackNamesForUser_ReturnsSortedNamesWithoutExtension()
		{
			var dir = Path.Combine(_root, "Users", "u1", "Tracks");
			Directory.CreateDirectory(dir);

			File.WriteAllText(Path.Combine(dir, "b_track.gpx"), "x");
			File.WriteAllText(Path.Combine(dir, "a_track.gpx"), "x");
			File.WriteAllText(Path.Combine(dir, "ignore.txt"), "x");

			var result = _service.GetTrackNamesForUser("u1").ToList();

			result.Should().Equal("a_track", "b_track");
		}
	}
}
