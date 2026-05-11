/**
 * Adapter Pattern
 * Adapta una interfaz de una clase a otra interfaz que el cliente espera.
 * Permite que clases con interfaces incompatibles trabajen juntas.
 */

export interface INotificationService {
  sendNotification(userId: string, message: string): Promise<void>;
}

// --- Servicios Externos (Adaptees) ---
// Imaginemos que estos son SDKs de terceros instalados en el proyecto

export class ExternalEmailService {
  async sendMail(emailAddress: string, body: string, subject: string) {
    console.log(`[EmailService] Sending email to ${emailAddress}: ${subject}`);
    // Lógica real de envío (SendGrid, AWS SES, etc...)
  }
}

export class ExternalSlackService {
  async pushMessage(channelOrUserId: string, text: string) {
    console.log(`[SlackService] Pushing to Slack: ${channelOrUserId}`);
    // Lógica real de integración con API de Slack
  }
}

// --- Adapters ---

// Adaptador para el servicio de Email
export class EmailNotificationAdapter implements INotificationService {
  constructor(private emailService: ExternalEmailService = new ExternalEmailService()) {}

  async sendNotification(userId: string, message: string): Promise<void> {
    // Resolver email del usuario basado en su ID (simulado)
    const userEmail = `${userId}@example.com`; 
    await this.emailService.sendMail(userEmail, message, 'Notificación de TaskFlow');
  }
}

// Adaptador para notificaciones por Slack
export class SlackNotificationAdapter implements INotificationService {
  constructor(private slackService: ExternalSlackService = new ExternalSlackService()) {}

  async sendNotification(userId: string, message: string): Promise<void> {
    // Resolver Slack ID del usuario basado en userId (simulado)
    const slackId = `U-${userId}`;
    await this.slackService.pushMessage(slackId, message);
  }
}

/**
 * Cliente que usa la interfaz común sin importar qué servicio concreto sea.
 */
export class NotificationManager {
  constructor(private service: INotificationService) {}

  async notifyUser(userId: string, message: string) {
    await this.service.sendNotification(userId, message);
  }
}
