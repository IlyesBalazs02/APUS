using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace APUS.Server.Models.Activities
{

	[JsonObject]
	[System.Text.Json.Serialization.JsonConverter(typeof(Newtonsoft.Json.Converters.CustomCreationConverter<MainActivity>))]
	/*[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
	[JsonDerivedType(typeof(Running), "Running")]
	[JsonDerivedType(typeof(Running), "Bouldering")]*/
	public class MainActivity
	{
		[Key]
		public string Id { get; set; }

		public string Title { get; set; }

		public string? Description { get; set; }

		public DateTime? Date { get; set; }

		public TimeSpan? Duration { get; set; }

		public int? Calories { get; set; }

		public int? AvgHeartRate { get; set; }

		public int? MaxHeartRate { get; set; }


		public string DisplayName { get; set; }

		//Frontend getComponent
		public string ActivityType { get; set; }



		public MainActivity()
		{
			ActivityType = GetType().Name;
			Id = Guid.NewGuid().ToString();
		}
	}

	public class Running : MainActivity
	{
		public int? Pace { get; set; }
		public int? Distance { get; set; }
		public Running()
		{
		}
	}
	public class Hiking : MainActivity
	{
		public int? Distance { get; set; }
		public int? Elevation { get; set; }
		public Hiking()
		{
		}
	}

	public class Bouldering : MainActivity
	{
		public int? Difficulty { get; set; }
		public Boolean? RedPoint { get; set; }
		public Bouldering()
		{
		}

	}
}
