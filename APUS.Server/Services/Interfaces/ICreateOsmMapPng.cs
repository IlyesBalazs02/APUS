using APUS.Server.Models;

namespace APUS.Server.Services.Interfaces
{
	public interface ICreateOsmMapPng
	{
		Task GeneratePng(MainActivity activity);
	}
}