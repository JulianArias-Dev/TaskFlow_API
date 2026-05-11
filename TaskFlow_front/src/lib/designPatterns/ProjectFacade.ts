import { TaskBuilder } from './TaskBuilder';
import { EmailNotificationAdapter, SlackNotificationAdapter } from './NotificationAdapter';
import { dbService } from '../../services/databaseService';
import { ProjectStatus } from '../../types/models';
import { auth } from '../../lib/firebase';

/**
 * Facade Pattern
 * Proporciona una interfaz unificada a un conjunto de interfaces en un subsistema.
 * Define una interfaz de más alto nivel que hace el subsistema más fácil de usar.
 */

export class ProjectManagementFacade {
  private emailNotifier = new EmailNotificationAdapter();
  private slackNotifier = new SlackNotificationAdapter();

  /**
   * Operación compleja encapsulada. 
   * Configura un proyecto con sus columnas Kanban por defecto, crea una tarea 
   * integradora usando el Builder, e informa al equipo a través de Notificaciones.
   */
  async scaffoldNewProject(name: string, key: string, description: string, startDate: string, endDate: string) {
    console.log(`[Facade] Inicializando nuevo proyecto: ${name}`);
    
    if (!auth.currentUser) throw new Error("No autenticado");
    const ownerId = auth.currentUser.uid;

    try {
      // 1. Crear el proyecto (Manejado por el servicio de BD que además crea el tablero por defecto)
      const projectId = await dbService.createProject({
        name,
        key,
        description,
        startDate,
        endDate,
        status: ProjectStatus.PLANIFICADO,
        leadId: ownerId
      });
      
      // 2. Obtener el tablero autogenerado y sus columnas
      const boards = await dbService.getBoards(projectId);
      const defaultColumnId = boards[0]?.columns?.[0]?.id || 'col_todo';

      // 3. Crear una Tarea Introductoria (Usando nuestro Builder)
      const welcomeTaskObj = new TaskBuilder(projectId, boards[0]?.id || '', `¡Bienvenido a ${name}!`, ownerId)
        .setDescription('Comienza a organizar tu trabajo. Esta tarea fue autogenerada.')
        .setPriority('ALTA')
        .setType('FEATURE')
        .addLabel({ color: 'bg-blue-100 text-blue-700', name: 'Onboarding' })
        .build();

      const { id, createdAt, updatedAt, ...taskDataToCreate } = welcomeTaskObj as any;
      taskDataToCreate.status = defaultColumnId;

      const taskId = await dbService.createTask(projectId, taskDataToCreate);
      console.log(`[Facade] Tarea inicial generada: ${taskId}`);

      // 4. Integrar con ambos servicios de notificaciones en paralelo (Simulados para el owner)
      await Promise.all([
        this.emailNotifier.sendNotification(ownerId, `Has creado el proyecto: ${name}`),
        this.slackNotifier.sendNotification(ownerId, `Proyecto ${name} inicializado exitosamente`)
      ]);
      console.log(`[Facade] Notificaciones enviadas exitosamente al líder.`);

      // Retornar un resumen amigable
      return {
        success: true,
        projectId,
        taskId
      };

    } catch (error) {
      console.error(`[Facade] Error orquestando el proyecto:`, error);
      throw error;
    }
  }
}

