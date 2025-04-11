namespace APUS.Server.Models.Activities
{
	public class ActivityService
	{
		public List<MainActivity> Activities { get; } = new List<MainActivity>();

		public ActivityService()
		{
			Activities.Add(new Running { Time = 30, HeartRate = 120, Date = DateTime.Now, Pace = 5, Distance = 1000 });
			Activities.Add(new Bouldering { Time = 45, HeartRate = 130, Date = DateTime.Now, Difficulty = 5, RedPoint = false });
			Activities.Add(new MainActivity { Time = 60, HeartRate = 140, Date = DateTime.Now });
		}
	}
}
