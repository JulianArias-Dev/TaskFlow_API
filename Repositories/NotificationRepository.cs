using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories
{
	public interface INotificationRepository : IRepository<Notification>
	{
		System.Threading.Tasks.Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
		System.Threading.Tasks.Task<IEnumerable<Notification>> GetLatestByUserIdAsync(Guid userId, int count = 10);
		System.Threading.Tasks.Task<int> GetUnreadCountAsync(Guid userId);
		System.Threading.Tasks.Task MarkAllAsReadAsync(Guid userId);
	}

	public class NotificationRepository : Repository<Notification>, INotificationRepository
	{
		public NotificationRepository(TaskFlowDbContext context) : base(context)
		{
		}

		public async System.Threading.Tasks.Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId)
		{
			return await _dbSet
				.Where(n => n.UserId == userId && !n.IsRead)
				.OrderByDescending(n => n.CreatedAt)
				.ToListAsync();
		}

		public async System.Threading.Tasks.Task<IEnumerable<Notification>> GetLatestByUserIdAsync(Guid userId, int count = 10)
		{
			return await _dbSet
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.CreatedAt)
				.Take(count)
				.ToListAsync();
		}

		public async System.Threading.Tasks.Task<int> GetUnreadCountAsync(Guid userId)
		{
			return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);
		}

		public async System.Threading.Tasks.Task MarkAllAsReadAsync(Guid userId)
		{
			var unreadNotifications = await _dbSet
				.Where(n => n.UserId == userId && !n.IsRead)
				.ToListAsync();

			foreach (var notification in unreadNotifications)
			{
				notification.IsRead = true;
			}

			// Nota: El SaveChangesAsync se ejecutará desde el UnitOfWork
		}
	}
}
