namespace APUS.Server.Services.Interfaces
{
	public interface ITrackFileService
	{
		IEnumerable<string> GetTrackNamesForUser(string userId);
	}
}