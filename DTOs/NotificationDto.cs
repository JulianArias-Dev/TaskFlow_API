namespace TaskFlow_API.DTOs
{
	public class NotificationDto
	{
		public Guid Id { get; set; }
		public string Message { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string Type { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public bool IsRead { get; set; }
	}
}
