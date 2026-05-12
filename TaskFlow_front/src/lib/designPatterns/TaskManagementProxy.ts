import { dbService } from '../../services/databaseService';
import { auth } from '../firebase';
import { Task } from '../../types/models';

/**
 * Proxy Pattern
 * Proporciona un sustituto o representante para otro objeto para controlar el acceso a este.
 */

// Interfaz Sujeto
export interface ITaskManager {
  deleteTask(projectId: string, taskId: string): Promise<boolean>;
  updateTask(projectId: string, taskId: string, updates: Partial<Task>): Promise<boolean>;
}

// Sujeto Real (El core de base de datos)
export class RealTaskManager implements ITaskManager {
  async deleteTask(projectId: string, taskId: string): Promise<boolean> {
    await dbService.deleteTask(projectId, taskId);
    return true;
  }

  async updateTask(projectId: string, taskId: string, updates: Partial<Task>): Promise<boolean> {
    await dbService.updateTask(projectId, taskId, updates);
    return true;
  }
}

// Proxy Protector y Registrador (Logger)
export class TaskManagerProxy implements ITaskManager {
  private realManager: RealTaskManager;

  constructor() {
    this.realManager = new RealTaskManager();
  }

  // Lógica de Validación/Autorización previa
  private hasAdminAccess(userId: string): boolean {
    return true; 
  }

  private logActivity(action: string, taskId: string, userId: string): void {
    console.log(`[Audit Log] [${new Date().toISOString()}] Usuario: ${userId} intentó acción: ${action} sobre Tarea: ${taskId}`);
  }

  async deleteTask(projectId: string, taskId: string): Promise<boolean> {
    const userId = auth.currentUser?.uid || 'anon';
    this.logActivity('DELETE', taskId, userId);
    
    // Validación estructural/seguridad
    if (!this.hasAdminAccess(userId)) {
      console.warn(`[Proxy-Security] ACCESO DENEGADO.`);
      throw new Error('No posees los permisos necesarios para borrar tareas.');
    }
    
    // Delegar al sujeto real
    return await this.realManager.deleteTask(projectId, taskId);
  }

  async updateTask(projectId: string, taskId: string, updates: Partial<Task>): Promise<boolean> {
    const userId = auth.currentUser?.uid || 'anon';
    this.logActivity('UPDATE', taskId, userId);
    
    const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (updates.status && !isGuid.test(updates.status)) {
      console.warn(`[Proxy-Validation] Advertencia: El estado ${updates.status} no parece ser un columnId válido.`);
    }

    return await this.realManager.updateTask(projectId, taskId, updates);
  }
}


