import { Task, Project, ProjectStatus } from '../../types/models';

/**
 * Prototype Pattern
 * Especifica los tipos de objetos a crear usando una instancia prototípica,
 * y crea nuevos objetos copiando este prototipo.
 */

export interface IPrototype<T> {
  clone(): T;
}

export class TaskPrototype implements IPrototype<Task> {
  private task: Task;

  constructor(task: Task) {
    this.task = task;
  }

  clone(): Task {
    const cloned = JSON.parse(JSON.stringify(this.task)) as Task;
    const now = new Date().toISOString();

    cloned.id = `task-copy-${Date.now()}`;
    cloned.createdAt = now;
    cloned.updatedAt = now;
    cloned.status = 'todo'; // Reiniciar al estado inicial
    cloned.title = `${cloned.title} (Copia)`;

    return cloned;
  }
}

export class ProjectPrototype implements IPrototype<Project> {
  private project: Project;

  constructor(project: Project) {
    this.project = project;
  }

  clone(): Project {
    const cloned = JSON.parse(JSON.stringify(this.project)) as Project;
    const now = new Date().toISOString();

    cloned.id = `proj-copy-${Date.now()}`;
    cloned.createdAt = now;
    cloned.updatedAt = now;
    cloned.status = ProjectStatus.PLANIFICADO;
    cloned.name = `${cloned.name} (Copia)`;

    return cloned;
  }
}
