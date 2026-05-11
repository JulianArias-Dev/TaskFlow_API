import { Task, Project } from '../../types/models';

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
    // Deep clone usando JSON
    const cloned = JSON.parse(JSON.stringify(this.task)) as Task;
    
    // Resetear campos identificadores y temporales para crear una copia limpia
    cloned.id = `task-copy-${Date.now()}`;
    cloned.createdAt = new Date();
    cloned.updatedAt = new Date();
    cloned.status = 'todo'; // Reiniciar al estado inicial
    
    // Modificar título para indicar que es clonada (opcional)
    cloned.title = `${cloned.title} (Copia)`;
    
    // Limpiar historial o comentarios si corresponde
    cloned.history = [];
    cloned.comments = [];
    
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
    
    cloned.id = `proj-copy-${Date.now()}`;
    cloned.createdAt = new Date();
    cloned.updatedAt = new Date();
    cloned.status = 'PLANIFICADO' as any; // Volver al estado inicial
    
    cloned.name = `${cloned.name} (Copia)`;
    
    return cloned;
  }
}
