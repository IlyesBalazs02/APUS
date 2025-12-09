using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace APUS.Server.Data.Repositories.Implementations
{
	public sealed class GroupEventRepository : IGroupEventRepository
	{
		private readonly AppDbContext _db;

		public GroupEventRepository(AppDbContext db)
			=> _db = db;

		public async Task<List<GroupEvent>> GetByGroupIdPagedAsync(
			long groupId,
			int skip,
			int take,
			CancellationToken ct)
		{
			return await _db.GroupEvents
				.AsNoTracking()
				.Where(e => e.GroupId == groupId)
				.Include(e => e.CreatedByUser)
				.Include(e => e.Participants)
				.OrderByDescending(e => e.StartsAtUtc ?? e.CreatedAtUtc)
				.ThenByDescending(e => e.Id)
				.Skip(skip)
				.Take(take)
				.ToListAsync(ct);
		}

		public async Task<GroupEvent?> GetByIdAsync(long eventId, CancellationToken ct)
		{
			return await _db.GroupEvents
				.Include(e => e.Group)
				.Include(e => e.CreatedByUser)
				.Include(e => e.Participants)
					.ThenInclude(p => p.User)
				.FirstOrDefaultAsync(e => e.Id == eventId, ct);
		}

		public async Task<GroupEvent> AddAsync(GroupEvent entity, CancellationToken ct)
		{
			_db.GroupEvents.Add(entity);
			await _db.SaveChangesAsync(ct);

			// ensure navigation for mapping
			await _db.Entry(entity).Reference(e => e.CreatedByUser).LoadAsync(ct);
			await _db.Entry(entity).Collection(e => e.Participants).LoadAsync(ct);

			return entity;
		}

		public async Task DeleteAsync(GroupEvent entity, CancellationToken ct)
		{
			_db.GroupEvents.Remove(entity);
			await _db.SaveChangesAsync(ct);
		}

		// ---------- participants ----------

		public async Task<IReadOnlyList<GroupEventParticipant>> GetParticipantsAsync(
			long eventId,
			CancellationToken ct)
		{
			return await _db.GroupEventParticipants
				.AsNoTracking()
				.Where(p => p.GroupEventId == eventId)
				.Include(p => p.User)
				.OrderBy(p => p.JoinedAtUtc)
				.ToListAsync(ct);
		}

		public async Task<bool> AddParticipantAsync(
			long eventId,
			string userId,
			CancellationToken ct)
		{
			var exists = await _db.GroupEventParticipants
				.AnyAsync(p => p.GroupEventId == eventId && p.UserId == userId, ct);

			if (exists)
				return false;

			var entity = new GroupEventParticipant
			{
				GroupEventId = eventId,
				UserId = userId,
				JoinedAtUtc = DateTime.UtcNow
			};

			_db.GroupEventParticipants.Add(entity);
			await _db.SaveChangesAsync(ct);
			return true;
		}

		public async Task<bool> RemoveParticipantAsync(
			long eventId,
			string userId,
			CancellationToken ct)
		{
			var entity = await _db.GroupEventParticipants
				.FirstOrDefaultAsync(p => p.GroupEventId == eventId && p.UserId == userId, ct);

			if (entity == null)
				return false;

			_db.GroupEventParticipants.Remove(entity);
			await _db.SaveChangesAsync(ct);
			return true;
		}
	}
}
