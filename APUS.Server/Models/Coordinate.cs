using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Models
{
	public class Coordinate
	{
		public Guid Id { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double? Altitude { get; set; }
		public DateTime? Time { get; set; }


		// foreign key back to MainActivity
		/*public string MainActivityId { get; set; }
		public MainActivity MainActivity { get; set; }*/
		public Coordinate()
		{
			Id = Guid.NewGuid();
		}
	}
}
