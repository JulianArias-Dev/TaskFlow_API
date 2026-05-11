import { TaskBuilder } from './TaskBuilder';
import { EmailNotificationAdapter, SlackNotificationAdapter } from './NotificationAdapter';
import { dbService } from '../../services/databaseService';
import { ProjectStatus } from '../../types/models';
import { auth } from '../../lib/firebase';

/**
 * Facade Pattern
 * Proporciona una interfaz unificada a un conjunto de operaciones del subsistema:
 * crea el proyecto, genera una tarea de bienvenida y dispara notificaciones.
 */
export class ProjectManagementFacade {
  private emailNotifier = new EmailNotificationAdapter();
  private slackNotifier = new SlackNotificationAdapter();

  async scaffoldNewProject(
    name: string,
    description: string,
    startDate: string,
    endDate: string,
  ) {
    console.log(`[Facade] Inicializando nuevo proyecto: ${name}`);

    if (!auth.currentUser) throw new Error('No autenticado');
    const ownerId = auth.currentUser.uid;

    try {
      // 1. Crear el proyecto (el backend crea board + columnas por defecto)
      const projectId = await dbService.createProject({
        name,
        description,
        startDate,
        endDate,
        status: ProjectStatus.PLANIFICADO,
      });

      // 2. Obtener el tablero autogenerado y sus columnas
      const boards = await dbService.getBoards(projectId);
      const defaultColumnId = boards[0]?.columns?.[0]?.id ?? '';
      const defaultBoardId = boards[0]?.id ?? '';

      // 3. Crear una tarea introductoria (Builder)
      const welcomeTask = new TaskBuilder(projectId, defaultBoardId, `¡Bienvenido a ${name}!`)
        .setDescription('Comienza a organizar tu trabajo. Esta tarea fue autogenerada.')
        .setPriority('ALTA')
        .setType('FEATURE')
        .addLabel({ color: '#3b82f6', name: 'Onboarding' })
        .build();

      const { id, createdAt, updatedAt, ...taskDataToCreate } = welcomeTask;
      taskDataToCreate.status = defaultColumnId;

      const taskId = await dbService.createTask(projectId, taskDataToCreate);
      console.log(`[Facade] Tarea inicial generada: ${taskId}`);

      // 4. Notificar al owner por los adapters configurados
      await Promise.all([
        this.emailNotifier.sendNotification(ownerId, `Has creado el proyecto: ${name}`),
        this.slackNotifier.sendNotification(ownerId, `Proyecto ${name} inicializado exitosamente`),
      ]);

      return { success: true, projectId, taskId };
    } catch (error) {
      console.error('[Facade] Error orquestando el proyecto:', error);
      throw error;
    }
  }
}
