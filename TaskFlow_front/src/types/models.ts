export enum ProjectStatus {
  PLANIFICADO = 'PLANIFICADO',
  EN_PROGRESO = 'EN_PROGRESO',
  PAUSADO = 'PAUSADO',
  COMPLETADO = 'COMPLETADO',
  ARCHIVADO = 'ARCHIVADO'
}

export interface Project {
  id: string;
  name: string;
  key: string; // E.g., "PROJ"
  description: string;
  status: ProjectStatus;
  startDate?: any;
  endDate?: any;
  leadId: string;
  ownerId: string;
  createdAt: any;
  updatedAt: any;
}

export interface BoardColumn {
  id: string; // e.g., 'col-todo'
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
  createdAt: any;
  updatedAt: any;
}

export interface TaskLabel {
  color: string;
  name: string;
}

export interface TaskHistoryEvent {
  id: string;
  type: string;
  from?: string;
  to?: string;
  details?: string;
  userId: string;
  timestamp: any;
}

export interface Subtask {
  id: string;
  title: string;
  completed: boolean;
}

export interface TaskComment {
  id: string;
  userId: string; // Quien comentó
  content: string;
  createdAt: any;
  updatedAt?: any;
}

export interface TaskAttachment {
  id: string;
  name: string;
  url: string;
  size: number;
  type: string;
  createdAt: string;
}

export interface AppNotification {
  id: string;
  userId: string;
  type: 'ASSIGNED' | 'DUE_OVERDUE' | 'COMMENT' | 'STATUS_CHANGE';
  taskId: string;
  projectId: string;
  title: string;
  message: string;
  read: boolean;
  createdAt: any;
}

export interface ProjectAuditLog {
  id: string;
  projectId: string;
  action: 'CREATE' | 'UPDATE' | 'DELETE';
  entityType: 'BOARD' | 'TASK' | 'PROJECT';
  entityId: string;
  userId: string;
  details?: string;
  createdAt: any;
}

export interface SavedFilter {
  id: string;
  projectId: string;
  name: string;
  userId: string;
  criteria: any;
  createdAt: any;
}

export interface GlobalSettings {
  id: string; // typically 'global'
  platformName: string;
  maxAttachmentSizeMB: number;
  passwordPolicy: 'standard' | 'strict';
  updatedAt: any;
}

export interface Task {
  id: string;
  projectId: string;
  boardId: string;
  title: string;
  description?: string;
  status: string; // 'todo', 'in-progress', 'done', etc.
  priority: 'BAJA' | 'MEDIA' | 'ALTA' | 'URGENTE';
  type: 'BUG' | 'FEATURE' | 'TASK' | 'IMPROVEMENT';
  dueDate?: string | null;
  estimatedHours?: number | null;
  loggedHours?: number | null;
  overdueNotified?: boolean | null;
  labels?: TaskLabel[] | null;
  subtasks?: Subtask[];
  comments?: TaskComment[];
  attachments?: TaskAttachment[];
  reporterId: string;
  assigneeId?: string; // Legacy
  assigneeIds?: string[];
  history?: TaskHistoryEvent[];
  createdAt: any;
  updatedAt: any;
}
