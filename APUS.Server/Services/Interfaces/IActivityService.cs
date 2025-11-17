using APUS.Server.Domain.DTOs.Feature.Activity;
using APUS.Server.Domain.Models;

namespace APUS.Server.Services.Interfaces
{
	public interface IActivityService
	{
		Task EditActivityAsync(MainActivity existing, EditActivityRequest req);
	}
}