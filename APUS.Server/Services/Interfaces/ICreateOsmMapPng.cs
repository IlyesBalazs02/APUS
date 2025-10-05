using APUS.Server.Domain.Models;

namespace APUS.Server.Services.Interfaces
{
	public interface ICreateOsmMapPng
	{
		Task GeneratePng(MainActivity activity);
	}
}