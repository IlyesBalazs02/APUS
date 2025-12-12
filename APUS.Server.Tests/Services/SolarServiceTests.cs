using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Implementations.MapServices;
using APUS.Server.Services.Interfaces;
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
	public class SolarServiceTests
	{
		private readonly Mock<IRoutingService> _routingMock = new();
		private readonly Mock<IHuberRegressor> _huberMock = new();
		private readonly Mock<IWebHostEnvironment> _envMock = new();
		private readonly SolarService _service;

		public SolarServiceTests()
		{
			var root = Path.Combine(Path.GetTempPath(), "apus_solar_tests");
			Directory.CreateDirectory(root);

			_envMock.SetupGet(e => e.WebRootPath).Returns(root);

			_service = new SolarService(_routingMock.Object, _huberMock.Object, _envMock.Object);
		}

		[Fact]
		public void GetSolarTimes_ReturnsOrderedSunriseAndSunset()
		{
			var dateLocal = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Local);
			double lat = 47.5;
			double lon = 19.0;

			var (sunrise, sunset) = _service.GetSolarTimes(dateLocal, lat, lon);

			sunrise.Should().BeBefore(sunset);
			sunrise.Date.Should().Be(dateLocal.Date);
			sunset.Date.Should().Be(dateLocal.Date);
		}

		[Fact]
		public async Task PredictDaylightAsync_PredictionNull_ReturnsNull()
		{
			var points = new List<RouteCoordinateDto>
			{
				new RouteCoordinateDto { Lat = 47.0, Lon = 19.0 },
				new RouteCoordinateDto { Lat = 47.1, Lon = 19.1 }
			};

			var req = new DaylightRequestDto
			{
				Points = points,
				StartLocalTime = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Local)
			};

			_routingMock.Setup(r => r.SampleElevation(points)).Returns(new float?[] { 100, 120 });
			_huberMock.Setup(h => h.PredictTotalTimeSecondsAsync("u1", It.IsAny<string>()))
				.ReturnsAsync((double?)null);

			var result = await _service.PredictDaylightAsync(req, "u1");

			result.Should().BeNull();
		}

		[Fact]
		public async Task PredictDaylightAsync_ValidPrediction_ReturnsResponse()
		{
			var points = new List<RouteCoordinateDto>
			{
				new RouteCoordinateDto { Lat = 47.0, Lon = 19.0 },
				new RouteCoordinateDto { Lat = 47.01, Lon = 19.01 }
			};

			var req = new DaylightRequestDto
			{
				Points = points,
				StartLocalTime = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Local)
			};

			_routingMock.Setup(r => r.SampleElevation(points)).Returns(new float?[] { 100, 120 });
			_huberMock.Setup(h => h.PredictTotalTimeSecondsAsync("u1", It.IsAny<string>()))
				.ReturnsAsync(3600d);

			_huberMock.Setup(h => h.CoordinateAtSecondsAsync("u1", It.IsAny<string>(), It.IsAny<double>()))
				.ReturnsAsync((1.0, 2.0, 0.5));

			var result = await _service.PredictDaylightAsync(req, "u1");

			result.Should().NotBeNull();
			result!.PredictedSeconds.Should().Be(3600);
			result.PercentBeforeNightfall.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
		}
	}
}
