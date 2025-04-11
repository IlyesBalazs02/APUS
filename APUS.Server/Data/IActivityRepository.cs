using APUS.Server.Models.Activities;

namespace APUS.Server.Data
{
	public interface IActivityRepository
	{
		void Create(MainActivity activity);
		void Delete(string id);
		IEnumerable<MainActivity> Read();
		MainActivity Read(string id);
		void Update(string id, MainActivity activity);
	}
}