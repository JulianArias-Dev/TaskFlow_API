using System.ComponentModel.DataAnnotations;

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

	/// <summary>
	/// Preferencia por canal para un evento concreto.
	/// </summary>
	public class NotificationChannelPreferenceDto
	{
		public bool InApp { get; set; } = true;
		public bool Email { get; set; } = true;
	}

	/// <summary>
	/// Preferencias de notificación del usuario (RF-05.3).
	/// Las claves son los códigos de evento que el sistema soporta — el cliente
	/// es libre de añadir nuevos tipos sin migrar la BD porque se persiste como
	/// JSON crudo en la columna `Users.NotificationPreferences`.
	/// </summary>
	public class NotificationPreferencesDto
	{
		[Required]
		public Dictionary<string, NotificationChannelPreferenceDto> Preferences { get; set; }
			= new();
	}
}
