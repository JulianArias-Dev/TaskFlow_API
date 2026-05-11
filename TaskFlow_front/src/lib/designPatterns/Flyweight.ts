/**
 * Flyweight Pattern
 * Usa compartición para soportar eficientemente grandes cantidades 
 * de objetos de grano fino.
 */

// Interfaz Flyweight
export interface ILabelFlyweight {
  render(taskContextParams: any): any; // Recibe estado extrínseco
}

// Flyweight Concreto (Estado Intrínseco Complejamente Compartido)
// El color, nombre y clases tailwind nunca cambian para una etiqueta específica
export class TagTypeFlyweight implements ILabelFlyweight {
  constructor(
    public readonly name: string, 
    public readonly gradientClass: string, 
    public readonly iconHTML: string
  ) {}

  // Renderiza tomando el estado extrínseco (taskId, si se puede clickear o no)
  render(taskContext: { taskId: string, isInteractive: boolean }) {
    const interativeClass = taskContext.isInteractive ? 'cursor-pointer hover:shadow-md hover:scale-105' : 'cursor-default';
    
    return `
      <div id="label-${taskContext.taskId}" class="flex items-center gap-1 px-2 py-1 rounded-full text-xs font-semibold text-white transition-all ${this.gradientClass} ${interativeClass}">
        ${this.iconHTML} <span>${this.name}</span>
      </div>
    `;
  }
}

// Flyweight Factory (Administra y provee instancias recicladas)
export class TagFactory {
  // Caché de flyweights
  private static tagTypes: Map<string, TagTypeFlyweight> = new Map();

  static getTagType(name: string, gradientClass: string, iconHTML: string): TagTypeFlyweight {
    const key = `${name}-${gradientClass}`;
    
    if (!this.tagTypes.has(key)) {
      console.log(`[Flyweight] Caché Miss. Creando y guardando en memoria nueva etiqueta compartida: ${key}`);
      this.tagTypes.set(key, new TagTypeFlyweight(name, gradientClass, iconHTML));
    } else {
       // Silencioso por defecto para evitar console spam
       // console.log(`[Flyweight] Caché Hit. Reusando instancia compartida: ${key}`);
    }
    
    return this.tagTypes.get(key)!;
  }
  
  static getCacheSize() {
    return this.tagTypes.size;
  }
}

// Contexto Cliente (La Tarea en UI que asocia las etiquetas usando muy poca memoria)
export class UI_TaskComponent {
  // En lugar de guardar objetos pesados por cada label en cada tarea (Estado Intrínseco/pesado),
  // Solo se guardan punteros en memoria a las instancias Flyweight administradas por la factoría.
  private attachedTagFlyweights: TagTypeFlyweight[] = [];

  constructor(public taskId: string, public title: string) {}

  assignTagFromFactory(name: string, gradientClass: string, iconHTML: string) {
    const flyweight = TagFactory.getTagType(name, gradientClass, iconHTML);
    this.attachedTagFlyweights.push(flyweight);
  }

  renderTaskDisplay() {
    // Generar estado extrínseco para esta tarea actual
    const contextInfo = { taskId: this.taskId, isInteractive: true };
    
    return this.attachedTagFlyweights.map(flyweight => flyweight.render(contextInfo)).join('');
  }
}
