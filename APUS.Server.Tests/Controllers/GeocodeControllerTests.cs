using APUS.Server.Controllers.MapController;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Data;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Controllers
{
	public class GeocodeControllerTests
	{
		private readonly Mock<IDbConnection> _dbMock = new();
		private readonly GeocodeController _ctrl;

		public GeocodeControllerTests()
		{
			_ctrl = new GeocodeController(_dbMock.Object);
		}

		[Fact]
		public async Task Search_EmptyQuery_ReturnsBadRequest()
		{
			var result = await _ctrl.Search("   ", null, null);

			result.Result.Should().BeOfType<BadRequestObjectResult>()
				.Which.Value.Should().Be("Query cannot be empty.");
		}
	}
}
