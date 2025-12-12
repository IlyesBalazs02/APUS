using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Domain.DTOs.Groups
{
	public sealed class CreateCommentRequest
	{
		[Required]
		[StringLength(1000, MinimumLength = 1)]
		public string Text { get; set; } = null!;
	}
}
