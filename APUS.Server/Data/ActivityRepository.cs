using APUS.Server.Models.Activities;

namespace APUS.Server.Data
{
	public class ActivityRepository : IActivityRepository
	{
		ActivityDbContext context;

		public ActivityRepository(ActivityDbContext _context)
		{
			context = _context;
		}

		public void Create(MainActivity activity)
		{
			var newActivity = Activator.CreateInstance(activity.GetType());
			foreach (var prop in activity.GetType().GetProperties())
			{
				var value = prop.GetValue(activity);
				if (value != null)
				{
					prop.SetValue(newActivity, value);
				}
			}
			// Ensure the Id is unique
			var idProperty = activity.GetType().GetProperty("Id");
			if (idProperty != null)
			{
				idProperty.SetValue(newActivity, Guid.NewGuid().ToString());
			}

			context.Activities.Add((MainActivity)newActivity);
			context.SaveChanges();
		}

		public IEnumerable<MainActivity> Read()
		{
			return context.Activities;
		}

		public MainActivity? Read(string id)
		{
			return context.Activities.FirstOrDefault(a => a.Id == id);
		}

		public void Delete(string id)
		{
			var Activity = context.Activities.FirstOrDefault(a => a.Id == id);

			if (Activity != null)
			{
				context.Activities.Remove(Activity);
				context.SaveChanges();
			}
		}
		public void Update(string id, MainActivity activity)
		{
			var old = context.Activities.FirstOrDefault(a => a.Id == id);

			foreach (var prop in activity.GetType().GetProperties())
			{
				var value = prop.GetValue(activity);
				if (value != null)
				{
					prop.SetValue(old, value);
				}
			}
			context.SaveChanges();
		}
	}
}
