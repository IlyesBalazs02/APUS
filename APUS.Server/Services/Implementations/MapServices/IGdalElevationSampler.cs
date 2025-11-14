namespace APUS.Server.Services.Implementations.MapServices
{
	public interface IGdalElevationSampler
	{
		void Dispose();
		float? Sample(double lat, double lon);
	}
}