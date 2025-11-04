namespace APUS.Server.Domain.DTOs.Feature.Search
{
	public record FriendStatusDto(
		string UserId,
		bool CanRequest,
		string? Reason,
		string? ExistingStatus,   // "Pending", "Accepted", "Blocked"
		string? Direction         // "Outgoing", "Incoming", null
	);

	public record FriendRequestItemDto(
		string FromUserId,
		string FromFullName,
		string? FromAvatarUrl     
	);
}
