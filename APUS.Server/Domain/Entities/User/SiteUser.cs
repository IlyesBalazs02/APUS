using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APUS.Server.Domain.Entities.User
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


		public GenderType Gender { get; set; } = GenderType.Unspecified;

		[MaxLength(300)]
		public string? Bio { get; set; }

		public string AvatarUrl { get; set; }

		public virtual ICollection<MainActivity> Activities { get; set; }

		public virtual ICollection<MainActivity> LikedPosts { get; set; }

		public virtual ICollection<UserRelation> FriendRequestInitiated { get; set; } = new List<UserRelation>();

		public virtual ICollection<UserRelation> FriendRequestReceived { get; set; } = new List<UserRelation>();

		public virtual PrivacySettings Privacy { get; set; }

		public SiteUser()
		{
			Activities = new List<MainActivity>();
			LikedPosts = new List<MainActivity>();
			AvatarUrl = "/Perm/DefaultProfile.png";
		}
	}
}
