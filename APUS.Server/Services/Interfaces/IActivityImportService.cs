using APUS.Server.Domain.DTOs.Feature;

namespace APUS.Server.Services.Interfaces
{
	public interface IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream activityStream);
	}
}
