/**
 * Tipos TypeScript que reflejan los DTOs del backend .NET (TaskFlow API).
 * No depende de Firebase ni de ningún proveedor externo.
 */

export interface ApiResponse<T = unknown> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
  statusCode?: number;
}

export interface PagedResponse<T> {
  success: boolean;
  message: string;
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// -------------------- Auth --------------------

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  name: string;
  password: string;
  avatarUrl?: string;
  roleId?: number;
}

export interface LoginResponse {
  userId: string;
  email: string;
  name: string;
  role: string;
  token: string;
  themePreference: string;
  notifyByEmail: boolean;
  /** JSON crudo (string) con las preferencias de notificación. */
  notificationPreferences?: string | null;
  expiresAt: string;
  lastConnection: string;
}

// -------------------- Notification Preferences --------------------

export interface NotificationChannelPreference {
  inApp: boolean;
  email: boolean;
}

/**
 * Mapa código-de-evento → preferencias por canal.
 * Códigos soportados por el backend: ASSIGNED, DUE_OVERDUE, COMMENT,
 * STATUS_CHANGE. El backend completa los que falten con defaults (todo activo).
 */
export type NotificationPreferences = Record<string, NotificationChannelPreference>;

export interface NotificationPreferencesPayload {
  preferences: NotificationPreferences;
}

export interface TokenValidationInfo {
  userId: string;
  email: string;
  name: string;
  role: string;
  issuedAt: string;
  validatedAt: string;
}

// -------------------- Users --------------------

export interface UserApi {
  id: string;
  email: string;
  name: string;
  role: string;
  avatarUrl?: string | null;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string | null;
  isActive: boolean;
  projectCount: number;
  taskCount: number;
}

// -------------------- Catalogs --------------------

export interface CatalogItem {
  id: number;
  name: string;
  /** Algunos catálogos podrían exponer un código corto en el futuro — opcional. */
  code?: string;
  description?: string;
}

// -------------------- Projects --------------------

export interface CreateProjectRequest {
  name: string;
  description?: string;
  color?: string;
  startDate?: string | null;
  endDate?: string | null;
  statusId: number;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  color?: string;
  startDate?: string | null;
  endDate?: string | null;
  statusId?: number;
}

export interface ProjectApi {
  id: string;
  name: string;
  description: string;
  color: string;
  status: string;
  statusId: number;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  startDate?: string | null;
  endDate?: string | null;
  memberIds: string[];
  taskCount: number;
  memberCount: number;
  boardCount: number;
  boards: BoardApi[];
}

// -------------------- Boards --------------------

export interface CreateBoardRequest {
  name: string;
  description?: string;
  projectId: string;
  displayOrder?: number;
}

export interface UpdateBoardRequest {
  name?: string;
  description?: string;
  displayOrder?: number;
}

export interface BoardApi {
  id: string;
  name: string;
  description: string;
  projectId: string;
  createdAt: string;
  updatedAt: string;
  displayOrder: number;
  columnCount: number;
  columns: ColumnApi[];
}

// -------------------- Columns --------------------

export interface CreateColumnRequest {
  name: string;
  boardId: string;
  displayOrder?: number;
  wipLimit?: number | null;
  color?: string;
}

export interface UpdateColumnRequest {
  name?: string;
  displayOrder?: number;
  wipLimit?: number | null;
  color?: string;
}

export interface ColumnApi {
  id: string;
  name: string;
  boardId: string;
  displayOrder: number;
  wipLimit?: number | null;
  color?: string;
  taskCount: number;
  tasks?: TaskApi[];
}

// -------------------- Tasks --------------------

export interface CreateTaskRequest {
  title: string;
  description?: string;
  typeId: number;
  columnId: string;
  priorityId: number;
  assignedToUserId?: string | null;
  dueDate?: string | null;
  estimatedHours?: number;
  tags?: string[];
  parentTaskId?: string | null;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  typeId?: number;
  statusId?: number;
  priorityId?: number;
  assignedUserIds?: string[];
  dueDate?: string | null;
  estimatedHours?: number;
  actualHours?: number;
  tags?: string[];
}

export interface TaskApi {
  id: string;
  title: string;
  description: string;
  type: string;
  typeId: number;
  status: string;
  statusId: number;
  priority: string;
  priorityId: number;
  columnId: string;
  assignedUserIds: string[];
  parentTaskId?: string | null;
  createdAt: string;
  updatedAt: string;
  dueDate?: string | null;
  estimatedHours: number;
  actualHours: number;
  tags: string[];
  subTaskCount: number;
  commentCount: number;
  fileCount: number;
}

// -------------------- Notifications --------------------

export interface NotificationApi {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  taskId?: string | null;
  projectId?: string | null;
  createdAt: string;
}

// -------------------- Saved Filters --------------------

export interface SavedFilterApi {
  id: string;
  userId: string;
  projectId: string;
  name: string;
  criteria: string;
  createdAt: string;
}

export interface CreateSavedFilterRequest {
  projectId: string;
  name: string;
  criteria: string;
}
