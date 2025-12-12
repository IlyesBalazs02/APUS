namespace APUS.Server.Domain.DTOs.Feature.Search
{
	public class UserSearchDto
	{
		public string Id { get; set; } = default!;
		public string FullName { get; set; } = default!;
		public string? UserName { get; set; }
		public string? AvatarUrl { get; set; }
	}
}
