using APUS.Server.Domain.DTOs.Feature.Activity;

namespace APUS.Server.Services.Interfaces
{
	public interface ITCXFileService : IActivityImportService
	{
		ImportActivityModel ImportActivity(MemoryStream tcxStream);
	}
}