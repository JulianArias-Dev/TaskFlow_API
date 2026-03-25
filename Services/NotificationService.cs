using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services
{
	public interface INotificationService
	{
		System.Threading.Tasks.Task NotifyAsync(Guid userId, string subject, string content);
	}

	public class NotificationService : INotificationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IConfiguration _config;

		public NotificationService(IUnitOfWork unitOfWork, IConfiguration config)
		{
			_unitOfWork = unitOfWork;
			_config = config;
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
			var emailSettings = _config.GetSection("EmailSettings");

			// Si faltan datos de configuración, lanzamos una excepción clara
			string fromEmail = emailSettings["From"] ?? throw new InvalidOperationException("Configuración 'From' no encontrada.");
			string smtpServer = emailSettings["SmtpServer"] ?? throw new InvalidOperationException("Configuración 'SmtpServer' no encontrada.");

			var message = new MimeMessage();
			message.From.Add(MailboxAddress.Parse(emailSettings["From"])); // ¡Usa Parse!
			message.To.Add(new MailboxAddress("", targetEmail));
			message.Subject = subject;
			message.Body = new TextPart("html") { Text = content };

			using var client = new SmtpClient();

			// Dejamos que estas llamadas lancen excepciones si fallan
			await client.ConnectAsync(smtpServer, int.Parse(emailSettings["Port"] ?? "465"), true);
			await client.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"]);
			await client.SendAsync(message);
			await client.DisconnectAsync(true);
		}
	}
}
