using System.ComponentModel.DataAnnotations;

namespace APUS.Server.DTOs
{
	public class RegisterDto
	{
		[Required] public string Email { get; set; }
		[Required] public string Password { get; set; }
		[Required] public string ConfirmPassword { get; set; }
	}
}
