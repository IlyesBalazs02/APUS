using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APUS.Server.Models
{

	[JsonObject]
	[System.Text.Json.Serialization.JsonConverter(typeof(Newtonsoft.Json.Converters.CustomCreationConverter<MainActivity>))]
	public class MainActivity
	{
		[Key]
		public string Id { get; set; }

		[Required]
		public string Title { get; set; }

		public string? Description { get; set; }

		public DateTime? Date { get; set; }

		public TimeSpan? Duration { get; set; }

		public int? Calories { get; set; }

		public int? AvgHeartRate { get; set; }

		public int? MaxHeartRate { get; set; }

		public string? DisplayName { get; set; }

		//Frontend getComponent
		public string? ActivityType { get; set; }

		public MainActivity()
		{
			ActivityType = GetType().Name;
			Id = Guid.NewGuid().ToString();
		}
	}

	public class GpsRelatedActivity : MainActivity
	{
		[System.Text.Json.Serialization.JsonIgnore]
		public string? FilePath { get; set; }
		public double? TotalDistanceKm { get; set; }
		public double? TotalAscentMeters { get; set; }
		public double? TotalDescentMeters { get; set; }
		public double? AvgPace { get; set; } // m/s
			
		[NotMapped]
		public string GetPaceInTimeFormat
		{
			get
			{
				if (AvgPace == null || AvgPace == 0)
					return "N/A";

				// Calculate seconds to cover 1 kilometer
				double secondsPerKilometer = 1000 / AvgPace.Value;

				// Convert to minutes and seconds
				int minutes = (int)(secondsPerKilometer / 60);
				int seconds = (int)(secondsPerKilometer % 60);

				// Return formatted string
				return $"{minutes:D2}:{seconds:D2}";
			}
		}
		/*public virtual ICollection<ActivityImage>? ActivityImages { get; set; } = new List<ActivityImage>();

		[System.Text.Json.Serialization.JsonIgnore]
		public string StreamPath { get; set; }

		public Boolean? ShowCoordinates { get; set; } = false;*/
	}

	public class Running : GpsRelatedActivity
	{
		public Running()
		{
		}
	}
	

	public class Bouldering : MainActivity
	{
		public int? Difficulty { get; set; }
		//public bool? RedPoint { get; set; }
		public Bouldering()
		{
		}
	}

	public class RockClimbing : MainActivity
	{
		public int? Difficulty { get; set; }
		public int? ElevationGain { get; set; }

		public RockClimbing()
		{
		}
	}

	public class Hiking : GpsRelatedActivity
	{
		public Hiking()
		{
		}
	}

	public class Yoga : MainActivity
	{
		public Yoga()
		{

		}
	}

	public class Football : MainActivity
	{
		public int? Distance { get; set; }

		public Football()
		{
		}
	}

	public class Walk : GpsRelatedActivity
	{
	}

	public class Ride : GpsRelatedActivity
	{
	}

	public class Swimming : MainActivity
	{
		public int? Distance { get; set; }
	}

	public class Ski : GpsRelatedActivity
	{

	}

	public class Tennis : MainActivity
	{

	}
}
