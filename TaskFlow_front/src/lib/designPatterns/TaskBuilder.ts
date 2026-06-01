import { Task, TaskLabel } from '../../types/models';

/**
 * Builder Pattern
 * Separa la construcción de un objeto complejo de su representación,
 * permitiendo que el mismo proceso de construcción cree distintas variantes.
 */
export class TaskBuilder {
  private task: Partial<Task>;

  constructor(projectId: string, boardId: string, title: string) {
    const now = new Date().toISOString();
    this.task = {
      id: `task-${Date.now()}-${Math.random().toString(36).slice(2, 11)}`,
      projectId,
      boardId,
      title,
      status: 'todo',
      type: 'Tarea',
      priority: 'Media',
      labels: [],
      assigneeIds: [],
      createdAt: now,
      updatedAt: now,
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
    return this.task as Task;
  }
}
