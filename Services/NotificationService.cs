using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services
{
	public class EmailSettings
	{
		public required string From { get; set; }
		public required string SmtpServer { get; set; }
		public required string Port { get; set; }
		public required string Username { get; set; }
		public required string Password { get; set; }
	}

	public interface INotificationService
	{
		System.Threading.Tasks.Task NotifyAsync(Guid userId, string subject, string content);
		System.Threading.Tasks.Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId);
		System.Threading.Tasks.Task<bool> MarkAsReadAsync(Guid notificationId);
	}

	public class NotificationService : INotificationService
	{
		private readonly IUnitOfWork _unitOfWork;
		//private readonly IConfiguration _config;
		private readonly EmailSettings _emailSettings;


		public NotificationService(IUnitOfWork unitOfWork, IOptions<EmailSettings> emailOptions)
		{
			_unitOfWork = unitOfWork;
			_emailSettings = emailOptions.Value;
		}

		public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId)
		{
			// Al no tener Queryable, obtenemos la lista y filtramos
			// NOTA: Si tienes muchos datos, esto puede ser lento, pero para el proyecto sirve.
			var allNotifications = await _unitOfWork.Notifications.GetAllAsync();

			return allNotifications
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.CreatedAt)
				.Take(30)
				.Select(n => new NotificationDto
				{
					Id = n.Id,
					Message = n.Content,
					Type = n.Subject,
					Content = n.Content,
					CreatedAt = n.CreatedAt,
					IsRead = n.IsRead
				})
				.ToList();
		}

		public async Task<bool> MarkAsReadAsync(Guid notificationId)
		{
			// Usamos el GetByIdAsync estándar del repositorio
			var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);

			if (notification == null) return false;

			notification.IsRead = true;

			// El UnitOfWork persiste los cambios de todas las entidades rastreadas
			return await _unitOfWork.SaveChangesAsync() > 0;
		}

		public async System.Threading.Tasks.Task NotifyAsync(Guid userId, string subject, string content)
		{
			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user == null) return;

			// 1. Guardar en la tabla de Notificaciones (Obligatorio en App)
			var notification = new Notification
			{
				UserId = userId,
				Subject = subject,
				Content = content
			};
			await _unitOfWork.Notifications.AddAsync(notification);
			await _unitOfWork.SaveChangesAsync();

			// 2. Enviar por Email (Opcional según preferencia del usuario)
			if (user.AllowEmail)
			{
				await SendEmailAsync(user.Email, subject, content);
			}
		}

		private async System.Threading.Tasks.Task SendEmailAsync(string targetEmail, string subject, string content)
		{
			// Si faltan datos de configuración, lanzamos una excepción clara
			string fromEmail = _emailSettings.From ?? throw new InvalidOperationException("Configuración 'From' no encontrada.");
			string smtpServer = _emailSettings.SmtpServer ?? throw new InvalidOperationException("Configuración 'SmtpServer' no encontrada.");

			var message = new MimeMessage();
			message.From.Add(MailboxAddress.Parse(_emailSettings.From)); // ¡Usa Parse!
			message.To.Add(new MailboxAddress("", targetEmail));
			message.Subject = subject;
			message.Body = new TextPart("html") { Text = content };

			using var client = new SmtpClient();

			// Dejamos que estas llamadas lancen excepciones si fallan
			await client.ConnectAsync(smtpServer, int.Parse(_emailSettings.Port ?? "465"), true);
			await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
			await client.SendAsync(message);
			await client.DisconnectAsync(true);
		}
	}
}
