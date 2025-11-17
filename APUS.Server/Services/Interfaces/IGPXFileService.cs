using APUS.Server.Domain.DTOs.Feature.Activity;

namespace APUS.Server.Services.Interfaces
{
	public interface IGPXFileService : IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream GPXStream);
	}
}