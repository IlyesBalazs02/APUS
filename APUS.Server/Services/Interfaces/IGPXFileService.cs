using APUS.Server.Domain.DTOs.Feature;

namespace APUS.Server.Services.Interfaces
{
	public interface IGPXFileService : IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream GPXStream);
	}
}