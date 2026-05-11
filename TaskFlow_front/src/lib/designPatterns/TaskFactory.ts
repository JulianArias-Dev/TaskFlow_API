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
      type: 'BUG',
      priority: baseInfo.priority || 'ALTA', // Bugs suelen ser de alta prioridad por defecto
    } as Task;
  }
}

export class FeatureTaskCreator extends TaskCreator {
  public factoryMethod(baseInfo: Partial<Task>): Task {
    return {
      ...baseInfo,
      type: 'FEATURE',
      priority: baseInfo.priority || 'MEDIA',
    } as Task;
  }
}

export class DefaultTaskCreator extends TaskCreator {
  public factoryMethod(baseInfo: Partial<Task>): Task {
    return {
      ...baseInfo,
      type: 'TASK',
      priority: baseInfo.priority || 'BAJA',
    } as Task;
  }
}

// Interfaz estática simple para llamar a la factoría adecuada según un string
export class TaskFactory {
  static createTask(type: Task['type'], baseInfo: Partial<Task>): Task {
    switch (type) {
      case 'BUG':
        return new BugTaskCreator().create(baseInfo);
      case 'FEATURE':
        return new FeatureTaskCreator().create(baseInfo);
      case 'IMPROVEMENT':
      case 'TASK':
      default:
        return new DefaultTaskCreator().create(baseInfo);
    }
  }
}
