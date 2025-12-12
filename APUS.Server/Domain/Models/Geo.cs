namespace APUS.Server.Domain.Models
{
	public class PlaceSearchResult
	{
		public long Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Class { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public double Lat { get; set; }
		public double Lon { get; set; }
	}
}
