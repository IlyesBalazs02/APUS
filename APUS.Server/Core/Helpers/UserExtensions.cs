using System.Security.Claims;

namespace APUS.Server.Core.Helpers
{
	public static class UserExtensions
	{
		public static string GetUserId(this ClaimsPrincipal user)
			=> user.FindFirstValue(ClaimTypes.NameIdentifier)
			   ?? throw new InvalidOperationException("No user id");
	}
}
