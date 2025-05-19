using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace APUS.Server.Models
{
	public class SiteUser : IdentityUser
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }

		//For profile picture
		public string ContentType { get; set; }
		public byte[] Data { get; set; }

		//Navigation to activities
		public virtual ICollection<MainActivity> Activities { get; set; }

		public SiteUser()
		{
			Activities = new List<MainActivity>();
		}
	}
}
