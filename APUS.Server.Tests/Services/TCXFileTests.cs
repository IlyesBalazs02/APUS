using APUS.Server.Services.Implementations;
using Castle.Components.DictionaryAdapter.Xml;
using FluentAssertions;
using FluentAssertions.Common;
using OsmSharp.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Services
{
	public class TCXFileTests
	{
		private const string SampleTcx = @"<?xml version=""1.0"" encoding=""UTF-8""?>
		<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2""
		                        xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
		                        xmlns:ns3=""http://www.garmin.com/xmlschemas/ActivityExtension/v2"">
		  <Activities>
		    <Activity Sport=""Running"">
		      <Lap StartTime=""2025-05-23T10:00:00Z"">
		        <TotalTimeSeconds>120</TotalTimeSeconds>
		        <DistanceMeters>400</DistanceMeters>
		        <Calories>50</Calories>
		        <AverageHeartRateBpm><Value>100</Value></AverageHeartRateBpm>
		        <MaximumHeartRateBpm><Value>150</Value></MaximumHeartRateBpm>
		        <Extensions>
		          <ns3:LX>
		            <ns3:AvgSpeed>2</ns3:AvgSpeed>
		            <ns3:AvgRunCadence>80</ns3:AvgRunCadence>
		            <ns3:MaxRunCadence>90</ns3:MaxRunCadence>
		          </ns3:LX>
		        </Extensions>
		      </Lap>
		      <Track>
        <Trackpoint>
          <Time>2025-05-23T10:00:00Z</Time>
          <Position>
            <LatitudeDegrees>47.0</LatitudeDegrees>
            <LongitudeDegrees>19.0</LongitudeDegrees>
          </Position>
          <AltitudeMeters>100</AltitudeMeters>
          <DistanceMeters>0</DistanceMeters>
          <HeartRateBpm><Value>100</Value></HeartRateBpm>
          <Extensions>
            <ns3:TPX>
              <ns3:Speed>2.5</ns3:Speed>
              <ns3:RunCadence>80</ns3:RunCadence>
              <ns3:Watts>200</ns3:Watts>
            </ns3:TPX>
          </Extensions>
        </Trackpoint>
        <Trackpoint>
          <Time>2025-05-23T10:02:00Z</Time>
          <Position>
            <LatitudeDegrees>47.001</LatitudeDegrees>
            <LongitudeDegrees>19.001</LongitudeDegrees>
          </Position>
          <AltitudeMeters>110</AltitudeMeters>
          <DistanceMeters>400</DistanceMeters>
          <HeartRateBpm><Value>150</Value></HeartRateBpm>
          <Extensions>
            <ns3:TPX>
              <ns3:Speed>3.0</ns3:Speed>
              <ns3:RunCadence>85</ns3:RunCadence>
              <ns3:Watts>250</ns3:Watts>
            </ns3:TPX>
          </Extensions>
        </Trackpoint>
      </Track>
    </Activity>
  </Activities>
</TrainingCenterDatabase>
		";

		[Fact]
		public void ImportActivity_BasicTcx_ParsesCorrectly()
		{
			// Arrange
			var service = new TCXFileService();
			using var ms = new MemoryStream(Encoding.UTF8.GetBytes(SampleTcx));

			// Act
			var model = service.ImportActivity(ms);

			// Assert
			model.Should().NotBeNull();
			model.HasGpsTrack.Should().BeTrue();
			model.StartTime.Should().Be(DateTime.Parse("2025-05-23T10:00:00Z").ToUniversalTime());
			model.TotalTimeSeconds.Should().Be(120);
			model.Duration.Should().Be(TimeSpan.FromSeconds(120));
			model.TotalDistanceMeters.Should().Be(400);
			model.TotalDistanceKm.Should().Be(0.4);
			model.AvgPace.Should().BeApproximately(2.0, 0.001);
			model.TotalCalories.Should().Be(50);
			model.AverageHeartRate.Should().Be(100);
			model.MaximumHeartRate.Should().Be(150);
			model.TotalAscentMeters.Should().Be(10);
			model.TotalDescentMeters.Should().Be(0);
		}

		[Fact]
		public void ImportActivity_NoPosition_HasGpsTrackFalse()
		{
			// TCX without <Position> elements
			const string noPos = @"<?xml version=""1.0""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
  <Activities>
    <Activity Sport=""Running"">
      <Lap StartTime=""2025-05-23T11:00:00Z"">
        <TotalTimeSeconds>60</TotalTimeSeconds>
        <DistanceMeters>100</DistanceMeters>
        <Calories>20</Calories>
      </Lap>
      <Track>
        <Trackpoint>
          <Time>2025-05-23T11:00:00Z</Time>
        </Trackpoint>
      </Track>
    </Activity>
  </Activities>
</TrainingCenterDatabase>";

			var service = new TCXFileService();
			using var ms = new MemoryStream(Encoding.UTF8.GetBytes(noPos));

			var model = service.ImportActivity(ms);

			model.HasGpsTrack.Should().BeFalse();
			model.TotalDistanceKm.Should().Be(0.1);
		}

	}
}
