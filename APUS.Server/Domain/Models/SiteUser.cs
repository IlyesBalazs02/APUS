using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APUS.Server.Domain.Models
{
	public enum GenderType
	{
		Unspecified = 0,
		Male = 1,
		Female = 2
	}

	public class SiteUser : IdentityUser
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }

		//For profile picture
		public string AvatarUrl { get; set; }

		public GenderType Gender { get; set; } = GenderType.Unspecified;

		[MaxLength(300)]
		public string Bio { get; set; }

		//Navigation to activities
		public virtual ICollection<MainActivity> Activities { get; set; }

		public virtual ICollection<MainActivity> LikedPosts { get; set; }


		// Friendships this user initiated (sent requests)
		public virtual ICollection<UserRelation> FriendRequestInitiated { get; set; } = new List<UserRelation>();
		// Friendships this user received (incoming requests)
		public virtual ICollection<UserRelation> FriendRequestReceived { get; set; } = new List<UserRelation>();

		public SiteUser()
		{
			Activities = new List<MainActivity>();
			LikedPosts = new List<MainActivity>();
		}
	}
}
