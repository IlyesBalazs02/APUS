using APUS.Server.DTOs;

namespace APUS.Server.Services.Interfaces
{
	public interface IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream activityStream);
	}
}
