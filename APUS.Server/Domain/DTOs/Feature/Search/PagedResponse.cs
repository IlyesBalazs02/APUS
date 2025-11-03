namespace APUS.Server.Domain.DTOs.Feature.Search
{
	public sealed class PagedResponse<T>
	{
		public required IReadOnlyList<T> Items { get; init; }
		public required bool HasMore { get; init; }
	}

}
