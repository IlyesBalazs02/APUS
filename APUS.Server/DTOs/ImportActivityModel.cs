﻿namespace APUS.Server.DTOs

{
	//The tcx and gpx files data is collected here and with this class will the new activity be created
	public class ImportActivityModel
	{
		public DateTime StartTime { get; set; }
		public double? TotalDistanceMeters { get; set; }
		public double? TotalDistanceKm { get; set; }
		public double? TotalAscentMeters { get; set; }
		public double? TotalDescentMeters { get; set; }
		public double? AvgPace { get; set; } // m/s
		public TimeSpan Duration { get; set; }
		public double TotalTimeSeconds { get; set; }
		public int? TotalCalories { get; set; }
		public int? AverageHeartRate { get; set; }
		public int? MaximumHeartRate { get; set; }
		public bool HasGpsTrack { get; set; }

	}
}
