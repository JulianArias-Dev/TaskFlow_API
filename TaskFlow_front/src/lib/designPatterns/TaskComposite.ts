/**
 * Composite Pattern
 * Compone objetos en estructuras de árbol para representar jerarquías de parte-todo.
 * Permite a los clientes tratar a los objetos individuales y a sus composiciones de manera uniforme.
 */

// Component
export interface ITaskComponent {
  getProgress(): number;
  getTitle(): string;
  getEstimatedHours(): number;
}

// Leaf (Subtarea simple)
export class SimpleSubtask implements ITaskComponent {
  constructor(private title: string, private completed: boolean, private estimatedHours: number = 2) {}

  getTitle() { return this.title; }
  
  getProgress() { return this.completed ? 100 : 0; }
  
  getEstimatedHours() { return this.estimatedHours; }
  
  complete() { this.completed = true; }
  uncomplete() { this.completed = false; }
}

// Composite (Una tarea que puede contener otras tareas o subtareas)
export class ComplexTask implements ITaskComponent {
  private components: ITaskComponent[] = [];
  
  constructor(private title: string) {}

  add(component: ITaskComponent) {
    this.components.push(component);
  }

  remove(component: ITaskComponent) {
    this.components = this.components.filter(c => c !== component);
  }

  getTitle() { return this.title; }

  // Calcula el progreso promediado de sus hijos
  getProgress(): number {
    if (this.components.length === 0) return 0;
    const totalProgress = this.components.reduce((sum, comp) => sum + comp.getProgress(), 0);
    return Math.round(totalProgress / this.components.length);
  }

  // Suma iterativa de las horas de todos sus hijos
  getEstimatedHours(): number {
    return this.components.reduce((sum, comp) => sum + comp.getEstimatedHours(), 0);
  }
}
