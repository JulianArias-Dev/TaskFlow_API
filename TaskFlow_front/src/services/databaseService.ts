/**
 * DatabaseService — adaptador sobre la TaskFlow API (.NET).
 *
 * Reemplaza la implementación Firestore original. Mantiene el patrón Singleton
 * y la superficie pública que ya consumían los componentes para minimizar
 * refactors, pero internamente todo viaja por REST.
 *
 * El modelo legado del frontend (Project/Board/Task con campos sueltos como
 * `key`, `status`, `assigneeIds`, etc.) se mapea desde los DTOs del backend al
 * vuelo, exponiendo los tipos de `types/models.ts` que ya espera la UI.
 */

import { api } from '../lib/api';
import { auth } from '../lib/firebase';
import type {
  Board,
  BoardColumn,
  Project,
  ProjectStatus,
  SavedFilter,
  Task,
  AttachmentItem,
  CommentItem,
} from '../types/models';
import type {
  BoardApi,
  CatalogItem,
  ColumnApi,
  CreateBoardRequest,
  CreateProjectRequest,
  CreateTaskRequest,
  NotificationApi,
  ProjectApi,
  SavedFilterApi,
  TaskApi,
  UpdateProjectRequest,
  UpdateTaskRequest,
  UserApi,
} from '../types/api';

// ---------------------------------------------------------------------------
// Mappers backend ↔ frontend
// ---------------------------------------------------------------------------

function statusToEnum(status: string): ProjectStatus {
  const norm = (status ?? '').toUpperCase().replace(/[\s_-]+/g, '');
  switch (norm) {
    case 'ENPROGRESO':
    case 'INPROGRESS':
    case 'ACTIVE':
      return 'EN_PROGRESO' as ProjectStatus;
    case 'PAUSADO':
    case 'PAUSED':
    case 'ONHOLD':
      return 'PAUSADO' as ProjectStatus;
    case 'COMPLETADO':
    case 'COMPLETED':
      return 'COMPLETADO' as ProjectStatus;
    case 'ARCHIVADO':
    case 'ARCHIVED':
    case 'CANCELLED':
    case 'CANCELED':
      return 'ARCHIVADO' as ProjectStatus;
    case 'PLANIFICADO':
    case 'PLANNED':
    default:
      return 'PLANIFICADO' as ProjectStatus;
  }
}

function mapColumn(c: ColumnApi): BoardColumn {
  return {
    id: c.id,
    name: c.name,
    wipLimit: c.wipLimit ?? null,
  };
}

function mapBoard(b: BoardApi): Board {
  return {
    id: b.id,
    projectId: b.projectId,
    name: b.name,
    type: 'kanban',
    description: b.description,
    order: b.displayOrder,
    columns: (b.columns ?? []).map(mapColumn),
    createdAt: b.createdAt,
    updatedAt: b.updatedAt,
  };
}

function mapProject(p: ProjectApi): Project {
  return {
    id: p.id,
    name: p.name,
    description: p.description ?? '',
    status: statusToEnum(p.status),
    startDate: p.startDate ?? null,
    endDate: p.endDate ?? null,
    ownerId: p.ownerId,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
    color: p.color,
  };
}

function mapTask(t: TaskApi, projectId: string, boardId: string): Task {
  return {
    id: t.id,
    projectId,
    boardId,
    title: t.title,
    description: t.description,
    status: t.columnId, // status del frontend = columnId del backend
    // Mantenemos los nombres tal como vienen del catálogo del backend
    // (LOW/MEDIUM/HIGH/CRITICAL para Priority; Feature/Bug/Task/... para Type).
    // El componente de UI debe ser tolerante a estos valores.
    priority: t.priority ?? 'MEDIUM',
    type: t.type ?? 'Task',
    dueDate: t.dueDate ? t.dueDate!.split('T')[0] : null, // El backend devuelve ISO string; el frontend espera solo la fecha (YYYY-MM-DD).
    estimatedHours: t.estimatedHours,
    loggedHours: t.actualHours,
    assigneeIds: t.assignedUserIds ?? [],
    labels: (t.tags ?? []).map((raw) => {
      const [name, color] = raw.split('|');
      return { name, color: color ?? '#3498db' };
    }),
    createdAt: t.createdAt,
    updatedAt: t.updatedAt,
    fileCount: t.fileCount ?? 0,
    parentTaskId: t.parentTaskId ?? null,
    subTaskCount: t.subTaskCount ?? 0,
  };
}

// ---------------------------------------------------------------------------
// Cache de catálogos (status/priority/type) — los IDs viven en el backend.
// ---------------------------------------------------------------------------

class CatalogCache {
  private taskStatuses?: CatalogItem[];
  private taskPriorities?: CatalogItem[];
  private taskTypes?: CatalogItem[];
  private projectStatuses?: CatalogItem[];

  async getTaskStatuses(): Promise<CatalogItem[]> {
    if (!this.taskStatuses) {
      this.taskStatuses = await api.get<CatalogItem[]>('/BaseCatalog/task-status', { unwrap: false });
    }
    return this.taskStatuses;
  }
  async getTaskPriorities(): Promise<CatalogItem[]> {
    if (!this.taskPriorities) {
      this.taskPriorities = await api.get<CatalogItem[]>('/BaseCatalog/task-priorities', { unwrap: false });
    }
    return this.taskPriorities;
  }
  async getTaskTypes(): Promise<CatalogItem[]> {
    if (!this.taskTypes) {
      this.taskTypes = await api.get<CatalogItem[]>('/BaseCatalog/task-types', { unwrap: false });
    }
    return this.taskTypes;
  }
  async getProjectStatuses(): Promise<CatalogItem[]> {
    if (!this.projectStatuses) {
      this.projectStatuses = await api.get<CatalogItem[]>('/BaseCatalog/project-status', { unwrap: false });
    }
    return this.projectStatuses;
  }

  async findId(items: CatalogItem[], hint: string, fallback = 1): Promise<number> {
    const norm = hint.toUpperCase();
    const match = items.find((i) => i.code?.toUpperCase() === norm || i.name?.toUpperCase() === norm);
    return match?.id ?? fallback;
  }
}

// ---------------------------------------------------------------------------
// DatabaseService — singleton
// ---------------------------------------------------------------------------

export class DatabaseService {
  private static instance: DatabaseService;
  private catalog = new CatalogCache();

  private constructor() {}

  public static getInstance(): DatabaseService {
    if (!DatabaseService.instance) {
      DatabaseService.instance = new DatabaseService();
    }
    return DatabaseService.instance;
  }

  private requireUser() {
    if (!auth.currentUser) throw new Error('No autenticado');
    return auth.currentUser;
  }

  // ============================ PROYECTOS ============================

  async createProject(
    projectData: Omit<Project, 'id' | 'createdAt' | 'updatedAt' | 'ownerId'>,
  ): Promise<string> {
    const user = this.requireUser();
    const projectStatuses = await this.catalog.getProjectStatuses();
    const statusId = await this.catalog.findId(projectStatuses, projectData.status || 'PLANIFICADO', 1);

    const toIsoOrNull = (v: unknown): string | null => {
      if (v === null || v === undefined || v === '') return null;
      return String(v);
    };

    const payload: CreateProjectRequest = {
      name: projectData.name,
      description: projectData.description ?? '',
      color: projectData.color ?? '#3498db',
      startDate: toIsoOrNull(projectData.startDate) ?? new Date().toISOString(),
      endDate: toIsoOrNull(projectData.endDate),
      statusId,
    };

    const created = await api.post<ProjectApi>('/Projects', payload, {
      query: { ownerId: user.uid },
    });

    return created.id;
  }

  async getProjects(): Promise<{ project: Project; progress: number }[]> {
    const user = this.requireUser();
    const [owned, member] = await Promise.all([
      api.get<ProjectApi[]>(`/Projects/owner/${user.uid}`).catch(() => []),
      api.get<ProjectApi[]>(`/Projects/member/${user.uid}`).catch(() => []),
    ]);

    const byId = new Map<string, ProjectApi>();
    for (const p of [...owned, ...member]) byId.set(p.id, p);

    const result: { project: Project; progress: number }[] = [];
    for (const p of byId.values()) {
      const project = mapProject(p);
      const tasks = await this.getTasks(p.id);
      const done = tasks.filter((t) => t.status?.toLowerCase().includes('done') || t.status?.toLowerCase().includes('hecho')).length;
      const progress = tasks.length === 0 ? 0 : Math.round((done / tasks.length) * 100);
      result.push({ project, progress });
    }
    return result;
  }

  async updateProject(projectId: string, updates: Partial<Project>): Promise<void> {
    const toIsoOrNull = (v: unknown): string | null | undefined => {
      if (v === undefined) return undefined;
      if (v === null || v === '') return null;
      return String(v);
    };

    const payload: UpdateProjectRequest = {
      name: updates.name,
      description: updates.description,
      startDate: toIsoOrNull(updates.startDate),
      endDate: toIsoOrNull(updates.endDate),
    };

    if (updates.status) {
      const statuses = await this.catalog.getProjectStatuses();
      payload.statusId = await this.catalog.findId(statuses, updates.status, 1);
    }

    await api.put<ProjectApi>(`/Projects/${projectId}`, payload);
  }

  async deleteProject(projectId: string): Promise<void> {
    await api.delete(`/Projects/${projectId}`);
  }

  /**
   * Agrega un miembro a un proyecto. `projectRoleId` corresponde al catálogo
   * `ProjectRoles` del backend; si no se pasa el backend lo defaultea a
   * Developer.
   */
  async addProjectMember(
    projectId: string,
    email: string,
    projectRoleId?: number,
  ): Promise<boolean> {
    const user = await api.get<UserApi>(`/Users/email/${encodeURIComponent(email)}`, { unwrap: false });
    if (!user?.id) throw new Error('Usuario no encontrado o no registrado en el sistema.');
    await api.post(`/Projects/${projectId}/members/${user.id}`, undefined, {
      query: projectRoleId ? { projectRoleId } : undefined,
    });
    return true;
  }

  async getProjectMembers(projectId: string): Promise<{ uid: string; email: string; role: string }[]> {
    const users = await api.get<UserApi[]>(`/Users/project/${projectId}`, { unwrap: false });
    return (users ?? []).map((u) => ({ uid: u.id, email: u.email, role: u.role }));
  }

  async cloneProject(projectId: string, _newName: string): Promise<string | null> {
    const user = this.requireUser();
    const clone = await api.post<ProjectApi>(`/Projects/${projectId}/clone`, undefined, {
      query: { newOwnerId: user.uid },
    });
    return clone?.id ?? null;
  }

  // ============================ TABLEROS ============================

  async createBoard(
    projectId: string,
    boardData: Omit<Board, 'id' | 'projectId' | 'createdAt' | 'updatedAt'>,
  ): Promise<string> {
    const payload: CreateBoardRequest = {
      name: boardData.name,
      description: boardData.description ?? '',
      projectId,
      displayOrder: boardData.order ?? 0,
    };
    const created = await api.post<BoardApi>('/Boards', payload);

    // Crear columnas por defecto si las pasó el caller.
    if (boardData.columns && boardData.columns.length > 0) {
      for (let i = 0; i < boardData.columns.length; i++) {
        const col = boardData.columns[i];
        await api
          .post('/Columns', {
            name: col.name,
            boardId: created.id,
            displayOrder: i,
            wipLimit: col.wipLimit ?? null,
          })
          .catch((e) => console.warn('[createBoard] failed to create column', col.name, e));
      }
    }
    return created.id;
  }

  async getBoards(projectId: string): Promise<Board[]> {
    const boards = await api.get<BoardApi[]>(`/Boards/project/${projectId}`);
    return (boards ?? [])
      .map(mapBoard)
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  }

  async updateBoard(_projectId: string, boardId: string, updates: Partial<Board>): Promise<void> {
    await api.put(`/Boards/${boardId}`, {
      name: updates.name,
      description: updates.description,
      displayOrder: updates.order,
    });
  }

  async deleteBoard(_projectId: string, boardId: string): Promise<void> {
    await api.delete(`/Boards/${boardId}`);
  }

  // ============================ COLUMNAS ============================

  async createColumn(
    boardId: string,
    column: { name: string; displayOrder?: number; wipLimit?: number | null },
  ): Promise<string> {
    const created = await api.post<ColumnApi>('/Columns', {
      name: column.name,
      boardId,
      displayOrder: column.displayOrder ?? 0,
      wipLimit: column.wipLimit ?? null,
    });
    return created.id;
  }

  async updateColumn(
    columnId: string,
    updates: { name?: string; displayOrder?: number; wipLimit?: number | null },
  ): Promise<void> {
    await api.put(`/Columns/${columnId}`, updates);
  }

  async deleteColumn(columnId: string): Promise<void> {
    await api.delete(`/Columns/${columnId}`);
  }

  async reorderColumns(columnsInOrder: { id: string }[]): Promise<void> {
    await Promise.all(
      columnsInOrder.map((c, idx) => this.updateColumn(c.id, { displayOrder: idx })),
    );
  }

  // ============================ TAREAS ============================

  /**
   * Crea una tarea. Si el caller especifica un tipo (Feature, Bug, ...) usamos
   * el endpoint `POST /api/Tasks/by-type` que aplica el **Factory Method**
   * del PDF: el backend resuelve los defaults razonables para ese tipo. Para
   * los campos extra (dueDate, prioridad explícita, asignados) hacemos un
   * `PUT` posterior con `updateTask` — así demostramos el patrón sin perder
   * la flexibilidad del formulario.
   */
  async createTask(
    projectId: string,
    taskData: Omit<Task, 'id' | 'projectId' | 'createdAt' | 'updatedAt'> & { parentTaskId?: string | null },
  ): Promise<string> {
    this.requireUser();

    const types = await this.catalog.getTaskTypes();
    const typeId = await this.catalog.findId(types, this.normalizeType(taskData.type || 'Task'), 5);

    if (taskData.parentTaskId) {
      // Si se especificó parentTaskId, el endpoint de creación es diferente y no aplica Factory Method.
      const payload: CreateTaskRequest = {
        title: taskData.title,
        description: taskData.description ?? '',
        typeId,
        statusId: 1, // statusId por defecto (To Do) — el frontend lo interpreta como columnId al mapear.
        priorityId: 2, // priorityId por defecto
        columnId: taskData.status,
        parentTaskId: taskData.parentTaskId,
        tags: taskData.labels?.map((l) => `${l.name}|${l.color}`),
      };
      const created = await api.post<TaskApi>('/Tasks', payload);
      return created.id;
    }

    // 1) Factory Method — crea la tarea con defaults del tipo.
    const created = await api.post<TaskApi>(
      '/Tasks/by-type',
      undefined,
      {
        query: {
          taskType: typeId,
          title: taskData.title,
          description: taskData.description ?? '',
          columnId: taskData.status, // status del frontend = columnId del backend
        },
      },
    );

    // 2) Si el formulario aportó datos extra que el Factory no setea, los
    //    aplicamos vía PUT (prioridad explícita, fechas, asignados, etiquetas).
    const needsUpdate =
      !!taskData.priority ||
      !!taskData.dueDate ||
      taskData.estimatedHours !== undefined ||
      (taskData.assigneeIds && taskData.assigneeIds.length > 0) ||
      (taskData.labels && taskData.labels.length > 0);

    if (needsUpdate) {
      await this.updateTask(projectId, created.id, taskData);
    }

    return created.id;
  }

  async getTasks(projectId: string, boardId?: string): Promise<Task[]> {
    const tasks = await api.get<TaskApi[]>(`/Tasks/project/${projectId}`);
    return (tasks ?? []).map((t) => mapTask(t, projectId, boardId ?? ''));
  }

  async updateTaskStatus(_projectId: string, taskId: string, status: string): Promise<void> {
    // status del frontend = columnId; pero el endpoint UpdateTask espera statusId del catálogo.
    // Heurística: si parece un GUID, asumimos que es un columnId y haremos un PATCH específico vía updateTask.
    // El backend espera columnas; el campo Column se actualiza al mover task entre columnas (no expuesto aún).
    // Por ahora, actualizamos statusId si los catálogos coinciden por código.
    const statuses = await this.catalog.getTaskStatuses();
    const statusId = await this.catalog.findId(statuses, status, 1);
    const payload: UpdateTaskRequest = { statusId };
    await api.put(`/Tasks/${taskId}`, payload);
  }

  async updateTask(_projectId: string, taskId: string, updates: Partial<Task>): Promise<void> {
    const payload: UpdateTaskRequest = {
      title: updates.title,
      description: updates.description,
      dueDate: updates.dueDate ? updates.dueDate + 'T12:00:00' : undefined,
      estimatedHours: updates.estimatedHours ?? undefined,
      actualHours: updates.loggedHours ?? undefined,
      assignedUserIds: updates.assigneeIds,
      tags: updates.labels?.map((l) => `${l.name}|${l.color}`),
    };

    if (updates.priority) {
      const priorities = await this.catalog.getTaskPriorities();
      payload.priorityId = await this.catalog.findId(priorities, this.normalizePriority(updates.priority), 2);
    }
    if (updates.type) {
      const types = await this.catalog.getTaskTypes();
      payload.typeId = await this.catalog.findId(types, this.normalizeType(updates.type), 5);
    }
    if (updates.status) {
      const statuses = await this.catalog.getTaskStatuses();
      payload.statusId = await this.catalog.findId(statuses, updates.status, 1);
    }

    await api.put(`/Tasks/${taskId}`, payload);
  }

  async deleteTask(_projectId: string, taskId: string): Promise<void> {
    await api.delete(`/Tasks/${taskId}`);
  }

  /** Stub — la auditoría la maneja internamente el backend. */
  async auditProjectAction(
    _projectId: string,
    _action: 'CREATE' | 'UPDATE' | 'DELETE',
    _entityType: 'BOARD' | 'TASK' | 'PROJECT',
    _entityId: string,
    _details?: string,
  ): Promise<void> {
    // No-op: el backend registra los AuditLog automáticamente desde sus services.
  }

  private normalizePriority(priority: string): string {
  const map: Record<string, string> = {
    'BAJA': 'LOW',
    'MEDIA': 'MEDIUM',
    'ALTA': 'HIGH',
    'URGENTE': 'CRITICAL',
  };
  return map[priority?.toUpperCase()] ?? priority;
  }

  private normalizeType(type: string): string {
    const map: Record<string, string> = {
      'FEATURE': 'Feature',
      'BUG': 'Bug',
      'IMPROVEMENT': 'Improvement',
      'RESEARCH': 'Research',
      'TASK': 'Task',
    };
    return map[type?.toUpperCase()] ?? type;
  }

  async getSubTasks(parentTaskId: string, projectId: string): Promise<Task[]> {
  const tasks = await api.get<TaskApi[]>(`/Tasks/${parentTaskId}/subtasks`).catch(() => []);
  return (tasks ?? []).map((t) => mapTask(t, projectId, ''));
  }

  // ============================ ADJUNTOS ============================

async uploadAttachment(taskId: string, file: File): Promise<void> {
  const formData = new FormData();
  formData.append('File', file);
  formData.append('TaskId', taskId);

  await api.post('/Attachments/upload', formData);
}

async getAttachments(taskId: string): Promise<AttachmentItem[]> {
  const res = await api.get<any[]>(`/Attachments/task/${taskId}`);
  return (res ?? []).map(f => ({
    id: f.id,
    fileName: f.fileName,
    fileUrl: f.fileUrl,
    mimeType: f.mimeType,
  }));
}

async deleteAttachment(attachmentId: string): Promise<void> {
  await api.delete(`/Attachments/${attachmentId}`);
}

// ============================ COMENTARIOS ============================

async getComments(taskId: string): Promise<CommentItem[]> {
  const res = await api.get<any[]>(`/Comments/task/${taskId}`);
  return (res ?? []).map(c => ({
    id: c.id,
    content: c.content,
    userId: c.userId,
    userName: c.userName,
    userAvatar: c.userAvatar ?? null,
    createdAt: c.createdAt,
    isEdited: c.isEdited ?? false,
  }));
}

async createComment(taskId: string, content: string, userId: string): Promise<CommentItem> {
  const res = await api.post<any>('/Comments', { taskId, content, userId });
  return {
    id: res.id,
    content: res.content,
    userId: res.userId,
    userName: res.userName,
    userAvatar: res.userAvatar ?? null,
    createdAt: res.createdAt,
    isEdited: false,
  };
}

async updateComment(commentId: string, content: string): Promise<CommentItem> {
  const res = await api.put<any>(`/Comments/${commentId}`, { content });
  return {
    id: res.id,
    content: res.content,
    userId: res.userId,
    userName: res.userName,
    userAvatar: res.userAvatar ?? null,
    createdAt: res.createdAt,
    isEdited: res.isEdited ?? true,
  };
}

async deleteComment(commentId: string): Promise<void> {
  await api.delete(`/Comments/${commentId}`);
}

  // ============================ NOTIFICACIONES ============================

  async createNotification(_notification: unknown): Promise<void> {
    // No-op: las notificaciones se generan en el servidor cuando hay cambios de tarea.
    console.warn('[databaseService] createNotification es manejado por el backend.');
  }

  async getMyNotifications(): Promise<NotificationApi[]> {
    return api.get<NotificationApi[]>('/Users/my-notifications', { unwrap: false }).catch(() => []);
  }

  async markNotificationRead(notificationId: string): Promise<void> {
    await api.post(`/Users/notifications/${notificationId}/read`, undefined, { unwrap: false });
  }

  // ============================ SAVED FILTERS ============================

  async saveFilter(filter: Omit<SavedFilter, 'id' | 'createdAt' | 'userId'>): Promise<void> {
    await api.post('/SavedFilters', {
      projectId: filter.projectId,
      name: filter.name,
      criteria: typeof filter.criteria === 'string' ? filter.criteria : JSON.stringify(filter.criteria),
    });
  }

  async getSavedFilters(projectId: string): Promise<SavedFilter[]> {
    const user = this.requireUser();
    const filters = await api
      .get<SavedFilterApi[]>(`/SavedFilters/user/${user.uid}`, { unwrap: false })
      .catch(() => [] as SavedFilterApi[]);
    return (filters ?? [])
      .filter((f) => f.projectId === projectId)
      .map((f) => ({
        id: f.id,
        projectId: f.projectId,
        name: f.name,
        userId: f.userId,
        criteria: (() => {
          try {
            return JSON.parse(f.criteria);
          } catch {
            return f.criteria;
          }
        })(),
        createdAt: f.createdAt,
      }));
  }

  async deleteSavedFilter(filterId: string): Promise<void> {
    await api.delete(`/SavedFilters/${filterId}`);
  }
}

export const dbService = DatabaseService.getInstance();
