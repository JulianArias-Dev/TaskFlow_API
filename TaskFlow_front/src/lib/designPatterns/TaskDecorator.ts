/**
 * Decorator Pattern
 * Asigna responsabilidades adicionales a un objeto dinámicamente.
 * Proporciona una alternativa flexible a la derivación de clases para extender funcionalidades.
 */

// Componente Base GUI
export interface IVisualTask {
  render(): string;
}

// Objeto Concreto a ser decorado
export class BaseTaskCard implements IVisualTask {
  constructor(private taskId: string, private title: string) {}
  
  render() {
    return `<div class="task-card" id="${this.taskId}"><h4>${this.title}</h4></div>`;
  }
}

// Decorador Base
export abstract class TaskUIDecorator implements IVisualTask {
  constructor(protected wrappedTask: IVisualTask) {}
  
  render() {
    return this.wrappedTask.render();
  }
}

// Decorador Concreto: Etiquetas
export class LabelsDecorator extends TaskUIDecorator {
  constructor(wrappedTask: IVisualTask, private labels: string[]) {
    super(wrappedTask);
  }
  
  render() {
    const baseHTML = super.render();
    const labelsHTML = `<div class="labels">` + 
      this.labels.map(l => `<span class="badge">${l}</span>`).join('') + 
      `</div>`;
    
    // Inyectarlo visualmente de forma simple (simulado)
    return baseHTML.replace('</div>', `${labelsHTML}</div>`);
  }
}

// Decorador Concreto: Adjuntos
export class AttachmentsDecorator extends TaskUIDecorator {
  constructor(wrappedTask: IVisualTask, private fileCount: number) {
    super(wrappedTask);
  }
  
  render() {
    const baseHTML = super.render();
    if (this.fileCount === 0) return baseHTML;
    
    const attachmentHTML = `<div class="attachments">📎 ${this.fileCount}</div>`;
    return baseHTML.replace('</div>', `${attachmentHTML}</div>`);
  }
}

// Decorador Concreto: Prioridad Visual
export class DeadlineDecorator extends TaskUIDecorator {
  constructor(wrappedTask: IVisualTask, private isOverdue: boolean) {
    super(wrappedTask);
  }
  
  render() {
    const baseHTML = super.render();
    const styleClass = this.isOverdue ? 'border-red-500 bg-red-50' : 'border-gray-200';
    
    return baseHTML.replace('class="task-card"', `class="task-card ${styleClass}"`);
  }
}
