using APUS.Server.Domain.DTOs.Feature;

namespace APUS.Server.Services.Interfaces
{
	public interface ITCXFileService : IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream tcxStream);
	}
}