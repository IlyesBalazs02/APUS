namespace APUS.Server.DTOs
{
	public class TrackpointDto
	{
		public DateTime Time { get; set; }
		public double? Lat { get; set; }
		public double? Lon { get; set; }
		public int? Hr { get; set; }
		public double? Pace { get; set; }
		public double? Alt { get; set; }
	}
}
