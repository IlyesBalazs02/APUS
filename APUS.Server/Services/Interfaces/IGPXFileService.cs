using APUS.Server.DTOs;

namespace APUS.Server.Services.Interfaces
{
	public interface IGPXFileService : IActivityImportService
	{
		ImportActivityModel ImportActivity(Stream GPXStream);
	}
}