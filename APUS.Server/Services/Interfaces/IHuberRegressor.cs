namespace APUS.Server.Services.Interfaces
{
	public interface IHuberRegressor
	{
		Task<(double lat, double lon, double progress)?> CoordinateAtSecondsAsync(string userId, string filePath, double seconds);
		Task<double?> PredictTotalTimeSecondsAsync(string userId, string filePath);
		Task TrainAsync(string userId, string filePath);
	}
}