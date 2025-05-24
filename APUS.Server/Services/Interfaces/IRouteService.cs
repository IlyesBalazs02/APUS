namespace APUS.Server.Services.Interfaces
{
	public interface IRouteService
	{
		Task<List<(double latitude, double longitude)>> GetRouteAsync((double lat, double lon) start, (double lat, double lon) end);
	}
}