import { Task, TaskLabel, Subtask } from '../../types/models';

/**
 * Builder Pattern
 * Separa la construcción de un objeto complejo de su representación 
 * para que el mismo proceso de construcción pueda crear distintas representaciones.
 */

export class TaskBuilder {
  private task: Partial<Task>;

  constructor(projectId: string, boardId: string, title: string, reporterId: string) {
    this.task = {
      id: `task-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      projectId,
      boardId,
      title,
      reporterId,
      status: 'todo',
      type: 'TASK',
      priority: 'MEDIA',
      labels: [],
      subtasks: [],
      assigneeIds: [],
      createdAt: new Date(),
      updatedAt: new Date()
    };
  }

  setDescription(description: string): this {
    this.task.description = description;
    return this;
  }

  setPriority(priority: Task['priority']): this {
    this.task.priority = priority;
    return this;
  }

  setType(type: Task['type']): this {
    this.task.type = type;
    return this;
  }

  addLabel(label: TaskLabel): this {
    if (!this.task.labels) this.task.labels = [];
    this.task.labels.push(label);
    return this;
  }

  addSubtask(title: string): this {
    if (!this.task.subtasks) this.task.subtasks = [];
    this.task.subtasks.push({
      id: `subtask-${Date.now()}-${Math.random()}`,
      title,
      completed: false
    });
    return this;
  }

  assignTo(userId: string): this {
    if (!this.task.assigneeIds) this.task.assigneeIds = [];
    if (!this.task.assigneeIds.includes(userId)) {
      this.task.assigneeIds.push(userId);
    }
    return this;
  }
  
  setDueDate(date: string): this {
    this.task.dueDate = date;
    return this;
  }

  setEstimatedHours(hours: number): this {
    this.task.estimatedHours = hours;
    return this;
  }

  build(): Task {
    // Aquí se podrían hacer validaciones adicionales antes de retornar el objeto completo.
    return this.task as Task;
  }
}
