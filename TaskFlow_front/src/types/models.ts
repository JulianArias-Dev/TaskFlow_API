/**
 * Modelos de dominio del frontend.
 * Reflejan los DTOs del backend (.NET) pero usando los nombres que ya consume la UI.
 *
 * Comentarios, adjuntos e historial viven en endpoints separados del backend
 * (CommentsController, AttachmentsController, AuditLogs) — no en el TaskDto —
 * por lo que NO son parte del tipo `Task` aquí.
 */

// Alineado 1:1 con el catálogo `ProjectStatuses` del backend.
// Valores coincidentes con `ProjectStatus.Name` en BD (en MAYÚSCULAS).
export enum ProjectStatus {
  ACTIVO = 'ACTIVO',
  COMPLETADO = 'COMPLETADO',
  EN_PAUSA = 'EN_PAUSA',
  CANCELADO = 'CANCELADO',
  ARCHIVADO = 'ARCHIVADO',
}

export interface Project {
  id: string;
  name: string;
  description: string;
  status: ProjectStatus;
  /** Id del catálogo `ProjectStatuses` del backend (1..5). Útil para enviar
   *  transiciones de estado sin depender de coincidencias por nombre. */
  statusId: number;
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
   * (ej. "Baja", "Media", "Alta", "Crítica"). Se mantiene como `string`
   * para no romper cuando el catálogo cambie.
   */
  priority: string;
  /** Nombre legible del tipo (ej. "Funcionalidad", "Error", "Tarea"). */
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
