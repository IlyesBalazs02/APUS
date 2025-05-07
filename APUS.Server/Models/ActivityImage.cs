using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APUS.Server.Models
{
	public class ActivityImage
	{
		[Key]
		public Guid Id { get; set; }
		public byte[]? Data { get; set; }
		public DateTime? CreatedDate { get; set; }

		[Required]
		public string MainActivityId { get; set; }

		/*[ForeignKey(nameof(MainActivityId))]
		public MainActivity MainActivity { get; set; }*/

		public ActivityImage()
		{
			Id = Guid.NewGuid();
		}

	}
}
