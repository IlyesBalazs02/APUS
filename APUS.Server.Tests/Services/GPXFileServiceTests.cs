using APUS.Server.Services.Implementations.FileServices;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Tests.Services
{
	public class GPXFileServiceTests
	{
		private readonly GPXFileService _service = new();

		[Fact]
		public void ImportActivity_NullStream_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => _service.ImportActivity(null!));
		}

		[Fact]
		public void ImportActivity_EmptyGpx_NoTrack()
		{
			var xml = @"<?xml version=""1.0""?>
<gpx version=""1.1"" creator=""test"" xmlns=""http://www.topografix.com/GPX/1/1"">
</gpx>";

			using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

			var model = _service.ImportActivity(ms);

			model.HasGpsTrack.Should().BeFalse();
			model.TotalDistanceMeters.Should().Be(0);
			model.Duration.Should().Be(TimeSpan.Zero);
			model.FinishTimeUtc.Should().BeNull();
		}

		[Fact]
		public void ImportActivity_SimpleTrack_ComputesBasicStats()
		{
			var xml = @"<?xml version=""1.0""?>
<gpx version=""1.1"" creator=""test"" xmlns=""http://www.topografix.com/GPX/1/1"" xmlns:gpxtpx=""http://www.garmin.com/xmlschemas/TrackPointExtension/v1"">
  <trk>
    <trkseg>
      <trkpt lat=""47.0"" lon=""19.0"">
        <ele>100</ele>
        <time>2024-01-01T10:00:00Z</time>
        <extensions>
          <gpxtpx:TrackPointExtension>
            <gpxtpx:hr>100</gpxtpx:hr>
          </gpxtpx:TrackPointExtension>
        </extensions>
      </trkpt>
      <trkpt lat=""47.001"" lon=""19.001"">
        <ele>100</ele>
        <time>2024-01-01T10:30:00Z</time>
        <extensions>
          <gpxtpx:TrackPointExtension>
            <gpxtpx:hr>140</gpxtpx:hr>
          </gpxtpx:TrackPointExtension>
        </extensions>
      </trkpt>
    </trkseg>
  </trk>
</gpx>";

			using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

			var model = _service.ImportActivity(ms);

			model.HasGpsTrack.Should().BeTrue();

			model.FinishTimeUtc.Should().NotBeNull();

			model.TotalTimeSeconds.Should().Be(1800);
			model.Duration.Should().Be(TimeSpan.FromSeconds(1800));

			model.TotalDistanceMeters.Should().BeGreaterThan(0);
			model.TotalDistanceMeters.Should().BeLessThan(100_000);

			model.TotalAscentMeters.Should().Be(0);
			model.TotalDescentMeters.Should().Be(0);

			model.AverageHeartRate.Should().Be(120);
			model.MaximumHeartRate.Should().Be(140);
			model.TotalCalories.Should().BeGreaterThan(0);
		}
	}
}