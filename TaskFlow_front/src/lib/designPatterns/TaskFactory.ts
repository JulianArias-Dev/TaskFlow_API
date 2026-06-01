import { Task } from '../../types/models';

/**
 * Factory Method Pattern
 * Proporciona una interfaz para crear objetos en una superclase,
 * pero permite a las subclases alterar el tipo de objetos que se crearán.
 */

export abstract class TaskCreator {
  public abstract factoryMethod(baseInfo: Partial<Task>): Task;

  public create(baseInfo: Partial<Task>): Task {
    // Generar IDs u otra lógica compartida si es necesario
    const task = this.factoryMethod(baseInfo);
    if (!task.id) task.id = `task-${Date.now()}-${Math.random()}`;
    const now = new Date().toISOString();
    if (!task.createdAt) task.createdAt = now;
    if (!task.updatedAt) task.updatedAt = now;
    return task;
  }
}

export class BugTaskCreator extends TaskCreator {
  public factoryMethod(baseInfo: Partial<Task>): Task {
    return {
      ...baseInfo,
      type: 'Error',
      priority: baseInfo.priority || 'Alta', // Bugs suelen ser de alta prioridad por defecto
    } as Task;
  }
}

export class FeatureTaskCreator extends TaskCreator {
  public factoryMethod(baseInfo: Partial<Task>): Task {
    return {
      ...baseInfo,
      type: 'Funcionalidad',
      priority: baseInfo.priority || 'Media',
    } as Task;
  }
}

export class DefaultTaskCreator extends TaskCreator {
  public factoryMethod(baseInfo: Partial<Task>): Task {
    return {
      ...baseInfo,
      type: 'Tarea',
      priority: baseInfo.priority || 'Baja',
    } as Task;
  }
}

// Interfaz estática simple para llamar a la factoría adecuada según un string.
// Acepta tanto los nombres nuevos (en español) como los legacy en inglés.
export class TaskFactory {
  static createTask(type: Task['type'], baseInfo: Partial<Task>): Task {
    switch (type?.toUpperCase()) {
      case 'BUG':
      case 'ERROR':
        return new BugTaskCreator().create(baseInfo);
      case 'FEATURE':
      case 'FUNCIONALIDAD':
        return new FeatureTaskCreator().create(baseInfo);
      case 'IMPROVEMENT':
      case 'MEJORA':
      case 'TASK':
      case 'TAREA':
      default:
        return new DefaultTaskCreator().create(baseInfo);
    }
  }
}
