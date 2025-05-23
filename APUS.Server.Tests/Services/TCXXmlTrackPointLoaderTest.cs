using APUS.Server.Models;
using APUS.Server.Services.Implementations;
using APUS.Server.Services.Interfaces;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using OsmSharp.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Services
{
	public class TCXXmlTrackPointLoaderTest
	{
		private const string SampleTcx = @"<?xml version=""1.0""?>
		<TrainingCenterDatabase xmlns =""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2""
		xmlns:ns3=""http://www.garmin.com/xmlschemas/ActivityExtension/v2"">
		  <Activities>
		    <Activity Sport =""Running"">
		      <Track>
		        <Trackpoint>
		          <Time>2025-05-23T10:00:00Z</Time>
		          <Position>
		            <LatitudeDegrees>47.0</LatitudeDegrees>
		            <LongitudeDegrees>19.0</LongitudeDegrees>
		          </Position>
		          <AltitudeMeters>100</AltitudeMeters>
		          <HeartRateBpm>
		            <Value>120</Value>
		          </HeartRateBpm>
		          <Extensions>
		            <ns3:TPX>
		              <ns3:Speed>2.5</ns3:Speed>
		           </ns3:TPX>
		          </Extensions>
		        </Trackpoint>
		        <Trackpoint>
		          <Time>2025-05-23T10:05:00Z</Time>
		          <AltitudeMeters>105</AltitudeMeters>
		          <HeartRateBpm>
		            <Value>125</Value>
		          </HeartRateBpm>
		        </Trackpoint>
		      </Track>
		    </Activity>
		  </Activities>
		</TrainingCenterDatabase>";


		[Fact]
		public async Task LoadTrack_WithValidTcx_ReturnsCorrectDtos()
		{
			// Arrange: write sample TCX to temp file
			var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tcx");
			await File.WriteAllTextAsync(tempFile, SampleTcx);

			var activity = new MainActivity { Id = "A1", UserId = "U1" };

			var storageMock = new Mock<IStorageService>();
			storageMock
				.Setup(s => s.ReturnFirstFilePath(activity.Id, activity.UserId))
				.Returns(tempFile);

			var loader = new TcxXmlTrackpointLoader(storageMock.Object);

			// Act
			var result = await loader.LoadTrack(activity, CancellationToken.None);

			// Assert
			result.Should().HaveCount(2);
			var first = result[0];
			first.Time.ToUniversalTime().Should().Be(DateTime.Parse("2025-05-23T10:00:00Z").ToUniversalTime());
			first.Lat.Should().Be(47.0);
			first.Lon.Should().Be(19.0);
			first.Alt.Should().Be(100);
			first.Hr.Should().Be(120);
			first.Pace.Should().BeApproximately(2.5, 0.001);

			var second = result[1];
			second.Time.ToUniversalTime().Should().Be(DateTime.Parse("2025-05-23T10:05:00Z").ToUniversalTime());
			second.Lat.Should().BeNull();
			second.Lon.Should().BeNull();
			second.Alt.Should().Be(105);
			second.Hr.Should().Be(125);
			second.Pace.Should().BeNull();
		}

		[Fact]
		public async Task LoadTrack_FileNotFound_ThrowsFileNotFoundException()
		{
			// Arrange: storage returns non-existent path
			var storageMock = new Mock<IStorageService>();
			storageMock
				.Setup(s => s.ReturnFirstFilePath(It.IsAny<string>(), It.IsAny<string>()))
				.Returns("nonexistent.tcx");

			var loader = new TcxXmlTrackpointLoader(storageMock.Object);
			var activity = new MainActivity { Id = "X", UserId = "U" };

			// Act & Assert
			await Assert.ThrowsAsync<FileNotFoundException>(() =>
				loader.LoadTrack(activity, CancellationToken.None));
		}
	}
}
