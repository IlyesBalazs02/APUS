using APUS.Server.Controllers;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class RoutingControllerTests
	{
		[Fact]
		public async Task GetRoute_CallsServiceAndReturnsList()
		{
			// Arrange
			var fakeRoute = new List<(double, double)> {
				(1.1, 2.2),
				(3.3, 4.4)
			};
			var svcMock = new Mock<IRouteService>();

			var Start = (10.0, 20.0);
			var End = (30.0, 40.0);
			svcMock
			  .Setup(s => s.GetRouteAsync(Start, End))
			  .ReturnsAsync(fakeRoute);


			var ctrl = new RoutingController(svcMock.Object);

			var req = new RoutingController.RouteRequest
			{
				Start = new RoutingController.Coordinate { Latitude = 10, Longitude = 20 },
				End = new RoutingController.Coordinate { Latitude = 30, Longitude = 40 }
			};

			// Act
			var result = await ctrl.GetRoute(req);

			// Assert
			result.Should().BeEquivalentTo(fakeRoute);

			// Instead of a literal, use locals here:
			var expectedStart = (10.0, 20.0);
			var expectedEnd = (30.0, 40.0);

			svcMock.Verify(s =>
				s.GetRouteAsync(expectedStart, expectedEnd),
				Times.Once);
		}
	}
}
