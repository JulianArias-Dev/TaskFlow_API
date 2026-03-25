namespace TaskFlow_API.Models
{
	public class Notification
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public string Subject { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public bool IsRead { get; set; } = false;

		// Navegación
		public virtual User User { get; set; } = null!;
	}
}
