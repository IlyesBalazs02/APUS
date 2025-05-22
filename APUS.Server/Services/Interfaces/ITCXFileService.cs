using APUS.Server.DTOs;

namespace APUS.Server.Services.Interfaces
{
	public interface ITCXFileService : IActivityImportService
	{
		ImportActivityModel ImportActivity(Stream tcxStream);
	}
}