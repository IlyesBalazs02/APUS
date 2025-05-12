using APUS.Server.DTOs.GetActivitiesDto;
using APUS.Server.Models;
using System.Security.Cryptography;

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
			//activity.Id = Guid.NewGuid().ToString();
			context.Activities.Add(activity);
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
			// check if the new activity is the same type
			var oldEntity = context.Activities.FirstOrDefault(a => a.Id == id);
			if (oldEntity == null) throw new KeyNotFoundException(id);

			var incomingType = activity.GetType().Name;                                         
			var existingType = oldEntity.GetType().Name;

			if (!string.Equals(existingType, incomingType, StringComparison.Ordinal))
			{
				context.Activities.Remove(oldEntity);

				context.Activities.Add(activity);
			}
			else
			{
				foreach (var prop in activity.GetType().GetProperties())
				{
					var value = prop.GetValue(activity);
					if (value != null && prop.CanWrite)
					{
						prop.SetValue(oldEntity, value);
					}
				}
			}

			context.SaveChanges();
		}
	}
	
}
