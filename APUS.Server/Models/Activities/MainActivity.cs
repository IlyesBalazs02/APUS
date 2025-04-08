using Newtonsoft.Json;
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
		public int Time { get; set; }
		public int HeartRate { get; set; }
		public DateTime Date { get; set; }
	}

	public class Running : MainActivity
	{
		public int Pace { get; set; }
		public int Distance { get; set; }
	}

	public class Bouldering : MainActivity
	{
		public int Difficulty { get; set; }
		public int RedPoint { get; set; }
	}
}
