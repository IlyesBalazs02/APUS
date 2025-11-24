namespace APUS.Server.Domain.Entities.User
{
	public enum UserRelationStatus { Pending, Accepted, Blocked }

	public class UserRelation
	{
		public string UserId { get; set; }
		public SiteUser User { get; set; }

		public string FriendId { get; set; }
		public SiteUser Friend { get; set; }

		public UserRelationStatus Status { get; set; } = UserRelationStatus.Pending;
		//public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
		//public DateTime? AcceptedAt { get; set; }
	}
}
