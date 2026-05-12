/**
 * Modelos de dominio del frontend.
 * Reflejan los DTOs del backend (.NET) pero usando los nombres que ya consume la UI.
 *
 * Comentarios, adjuntos e historial viven en endpoints separados del backend
 * (CommentsController, AttachmentsController, AuditLogs) — no en el TaskDto —
 * por lo que NO son parte del tipo `Task` aquí.
 */

export enum ProjectStatus {
  PLANIFICADO = 'PLANIFICADO',
  EN_PROGRESO = 'EN_PROGRESO',
  PAUSADO = 'PAUSADO',
  COMPLETADO = 'COMPLETADO',
  ARCHIVADO = 'ARCHIVADO',
}

export interface Project {
  id: string;
  name: string;
  description: string;
  status: ProjectStatus;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  startDate?: string | null;
  endDate?: string | null;
  color?: string;
}

export interface BoardColumn {
  id: string;
  name: string;
  wipLimit?: number | null;
}

export interface Board {
  id: string;
  projectId: string;
  name: string;
  type: 'kanban' | 'scrum';
  description?: string;
  order?: number;
  columns: BoardColumn[];
  createdAt: string;
  updatedAt: string;
}

export interface TaskLabel {
  color: string;
  name: string;
}

export interface Task {
  id: string;
  projectId: string;
  boardId: string;
  title: string;
  description?: string;
  /** Almacena el `columnId` de Kanban en el que se encuentra la tarea. */
  status: string;
  /**
   * Nombre legible de la prioridad tal como viene del catálogo del backend
   * (ej. "LOW", "MEDIUM", "HIGH", "CRITICAL"). Se mantiene como `string`
   * para no romper cuando el catálogo cambie.
   */
  priority: string;
  /** Nombre legible del tipo (ej. "Feature", "Bug", "Task"). */
  type: string;
  dueDate?: string | null;
  estimatedHours?: number | null;
  loggedHours?: number | null;
  labels?: TaskLabel[] | null;
  assigneeIds?: string[];
  createdAt: string;
  updatedAt: string;
  fileCount: number;
  parentTaskId?: string | null;
  subTaskCount: number;
}

export interface AppNotification {
  id: string;
  userId: string;
  type: string;
  taskId?: string;
  projectId?: string;
  title: string;
  message: string;
  read: boolean;
  createdAt: string;
}

export interface SavedFilter {
  id: string;
  projectId: string;
  name: string;
  userId: string;
  criteria: any;
  createdAt: string;
}

export interface AttachmentItem {
  id: string;
  fileName: string;
  fileUrl: string;
  mimeType: string;
}

export interface CommentItem {
  id: string;
  content: string;
  userId: string;
  userName: string;
  userAvatar: string | null;
  createdAt: string;
  isEdited: boolean;
}
