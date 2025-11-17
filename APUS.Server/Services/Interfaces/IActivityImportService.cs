using APUS.Server.Domain.DTOs.Feature.Activity;

namespace APUS.Server.Services.Interfaces
{
	public interface IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream activityStream);
	}
}
