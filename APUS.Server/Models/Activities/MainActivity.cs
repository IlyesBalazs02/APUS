using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
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
		public int Time { get; set; }
		public int HeartRate { get; set; }
		public DateTime Date { get; set; }
		public String DisplayName { get; set; }

		public MainActivity() : this("Activity") { }

		public MainActivity(string displayname)
		{
			DisplayName = displayname;
			Id = Guid.NewGuid().ToString();
		}
	}

	public class Running : MainActivity
	{
		public int Pace { get; set; }
		public int Distance { get; set; }
		public Running() : base("Running")
		{
		}
	}

	public class Bouldering : MainActivity
	{
		public int Difficulty { get; set; }
		public Boolean RedPoint { get; set; }
		public Bouldering() : base("Bouldering")
		{
		}

	}
}
