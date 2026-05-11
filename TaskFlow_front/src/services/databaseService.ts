import { 
  collection, 
  doc, 
  setDoc, 
  getDoc, 
  getDocs, 
  updateDoc, 
  deleteDoc, 
  query, 
  where, 
  serverTimestamp,
  DocumentData,
  WithFieldValue
} from 'firebase/firestore';
import { db, auth } from '../lib/firebase';
import { Project, Board, Task, ProjectStatus, AppNotification, SavedFilter } from '../types/models';

enum OperationType {
  CREATE = 'create',
  UPDATE = 'update',
  DELETE = 'delete',
  LIST = 'list',
  GET = 'get',
  WRITE = 'write',
}

interface FirestoreErrorInfo {
  error: string;
  operationType: OperationType;
  path: string | null;
  authInfo: {
    userId?: string | null;
    email?: string | null;
  }
}

/**
 * Singleton DatabaseService
 * Encapsula todas las operaciones CRUD del sistema.
 */
export class DatabaseService {
  private static instance: DatabaseService;

  private constructor() {}

  public static getInstance(): DatabaseService {
    if (!DatabaseService.instance) {
      DatabaseService.instance = new DatabaseService();
    }
    return DatabaseService.instance;
  }

  private handleAuthError(op: OperationType, path: string | null, error: any) {
    const errInfo: FirestoreErrorInfo = {
      error: error instanceof Error ? error.message : String(error),
      operationType: op,
      path,
      authInfo: {
        userId: auth.currentUser?.uid,
        email: auth.currentUser?.email,
      }
    };
    console.error(`Error en DatabaseService [${op}]:`, JSON.stringify(errInfo));
    throw new Error(JSON.stringify(errInfo));
  }

  // --- PROYECTOS ---

  async createProject(projectData: Omit<Project, 'id' | 'createdAt' | 'updatedAt' | 'ownerId'>): Promise<string> {
    if (!auth.currentUser) throw new Error("No autenticado");
    
    const projectRef = doc(collection(db, 'projects'));
    const path = `projects/${projectRef.id}`;
    
    try {
      await setDoc(projectRef, {
        ...projectData,
        id: projectRef.id,
        ownerId: auth.currentUser.uid,
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp()
      });
      // automatically add the creator as member
      await setDoc(doc(db, 'projects', projectRef.id, 'members', auth.currentUser.uid), {
        uid: auth.currentUser.uid,
        email: auth.currentUser.email,
        role: 'ADMIN',
        joinedAt: serverTimestamp()
      });

      // create a default board
      const defaultColumns = [
        { id: 'col_todo', name: 'Por hacer' },
        { id: 'col_in_progress', name: 'En progreso' },
        { id: 'col_in_review', name: 'En revisión' },
        { id: 'col_done', name: 'Completado' }
      ];

      await this.createBoard(projectRef.id, {
        name: 'Tablero Principal',
        type: 'kanban',
        description: 'Gestión general de tareas',
        order: 1,
        columns: defaultColumns
      });

      // Add Audit log
      await this.auditProjectAction(projectRef.id, 'CREATE', 'PROJECT', projectRef.id, 'Project creation');

      return projectRef.id;
    } catch (error) {
      this.handleAuthError(OperationType.CREATE, path, error);
      return '';
    }
  }

  async getProjects(): Promise<{ project: Project, progress: number }[]> {
    if (!auth.currentUser) throw new Error("No autenticado");
    const uid = auth.currentUser.uid;
    const path = 'projects';
    
    try {
      // Get owned projects
      const qOwned = query(collection(db, 'projects'), where('ownerId', '==', uid));
      const ownedSnapshot = await getDocs(qOwned);
      const ownedProjects = ownedSnapshot.docs.map(doc => doc.data() as Project);
      
      // Get member projects
      const memberSnapshot = await getDocs(collection(db, 'users', uid, 'projects'));
      const memberProjectIds = memberSnapshot.docs.map(doc => doc.data().projectId).filter(id => id);
      
      const ownedIds = new Set(ownedProjects.map(p => p.id));
      const uniqueMemberIds = Array.from(new Set(memberProjectIds)).filter(id => id && !ownedIds.has(id));
      
      const memberProjects = await Promise.all(
        uniqueMemberIds.map(async id => {
          try {
            const snap = await getDoc(doc(db, 'projects', id!));
            return snap.exists() ? snap.data() as Project : null;
          } catch(e) { return null; }
        })
      );
      
      const allProjects = [...ownedProjects, ...memberProjects.filter(p => p !== null) as Project[]];
      
      // Load progress
      return await Promise.all(allProjects.map(async (project) => {
         const boards = await this.getBoards(project.id);
         let doneColIds = ['col_done', 'done'];
         if (boards && boards.length > 0 && boards[0].columns) {
           const boardDoneColIds = boards[0].columns.filter(c => c.name.toLowerCase().includes('hecho') || c.name.toLowerCase().includes('completad') || c.name.toLowerCase().includes('done')).map(c => c.id);
           if (boardDoneColIds.length > 0) doneColIds = boardDoneColIds;
         }
         
         const tasks = await this.getTasks(project.id);
         const completed = tasks.filter(t => doneColIds.includes(t.status)).length;
         const progress = tasks.length === 0 ? 0 : Math.round((completed / tasks.length) * 100);
         return { project, progress };
      }));
    } catch (error) {
      this.handleAuthError(OperationType.LIST, path, error);
      return [];
    }
  }

  async updateProject(projectId: string, updates: Partial<Project>): Promise<void> {
    const path = `projects/${projectId}`;
    try {
      await updateDoc(doc(db, 'projects', projectId), {
        ...updates,
        updatedAt: serverTimestamp()
      });
      await this.auditProjectAction(projectId, 'UPDATE', 'PROJECT', projectId, 'Project updated');
    } catch (error) {
      this.handleAuthError(OperationType.UPDATE, path, error);
    }
  }

  async deleteProject(projectId: string): Promise<void> {
    const path = `projects/${projectId}`;
    try {
      await deleteDoc(doc(db, 'projects', projectId));
    } catch (error) {
      this.handleAuthError(OperationType.DELETE, path, error);
    }
  }

  async addProjectMember(projectId: string, email: string, role: string): Promise<boolean> {
    const path = `projects/${projectId}/members`;
    try {
      const emailSnap = await getDoc(doc(db, 'user_emails', btoa(email.toLowerCase())));
      if (!emailSnap.exists()) {
        throw new Error('Usuario no encontrado o no registrado en el sistema.');
      }
      
      const { uid } = emailSnap.data();
      
      await setDoc(doc(db, 'projects', projectId, 'members', uid), {
        uid,
        email: email.toLowerCase(),
        role,
        joinedAt: serverTimestamp()
      });
      
      await setDoc(doc(db, 'users', uid, 'projects', projectId), {
        projectId,
        joinedAt: serverTimestamp()
      });
      
      return true;
    } catch (error) {
      this.handleAuthError(OperationType.CREATE, path, error);
      return false;
    }
  }

  async getProjectMembers(projectId: string): Promise<any[]> {
    const path = `projects/${projectId}/members`;
    try {
      const snap = await getDocs(collection(db, 'projects', projectId, 'members'));
      return snap.docs.map(doc => doc.data());
    } catch (error) {
      this.handleAuthError(OperationType.LIST, path, error);
      return [];
    }
  }

  async cloneProject(projectId: string, newName: string): Promise<string | null> {
    const path = `projects/${projectId}`;
    try {
      const orgProjectSnap = await getDoc(doc(db, 'projects', projectId));
      if (!orgProjectSnap.exists()) throw new Error('Project not found');
      
      const orgProject = orgProjectSnap.data() as Project;
      
      const newProjectId = await this.createProject({
        name: newName,
        key: orgProject.key,
        description: orgProject.description || '',
        status: ProjectStatus.PLANIFICADO,
        leadId: auth.currentUser!.uid,
      });

      if (!newProjectId) return null;

      const boards = await this.getBoards(projectId);
      for (const board of boards) {
        await this.createBoard(newProjectId, {
          name: board.name,
          type: board.type,
          description: board.description || '',
          order: board.order,
          columns: board.columns || []
        });
      }

      return newProjectId;
    } catch (error) {
      this.handleAuthError(OperationType.CREATE, path, error);
      return null;
    }
  }

  // --- TABLEROS ---

  async createBoard(projectId: string, boardData: Omit<Board, 'id' | 'projectId' | 'createdAt' | 'updatedAt'>): Promise<string> {
    const boardsRef = collection(db, 'projects', projectId, 'boards');
    const boardDoc = doc(boardsRef);
    const path = `projects/${projectId}/boards/${boardDoc.id}`;

    try {
      await setDoc(boardDoc, {
        ...boardData,
        id: boardDoc.id,
        projectId,
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp()
      });
      return boardDoc.id;
    } catch (error) {
      this.handleAuthError(OperationType.CREATE, path, error);
      return '';
    }
  }

  async getBoards(projectId: string): Promise<Board[]> {
    const path = `projects/${projectId}/boards`;
    try {
      const q = query(collection(db, 'projects', projectId, 'boards'));
      const snapshot = await getDocs(q);
      return snapshot.docs.map(doc => doc.data() as Board).sort((a, b) => (a.order || 0) - (b.order || 0));
    } catch (error) {
      this.handleAuthError(OperationType.LIST, path, error);
      return [];
    }
  }

  async updateBoard(projectId: string, boardId: string, updates: Partial<Board>): Promise<void> {
    const path = `projects/${projectId}/boards/${boardId}`;
    try {
      await updateDoc(doc(db, 'projects', projectId, 'boards', boardId), {
        ...updates,
        updatedAt: serverTimestamp()
      });
    } catch (error) {
      this.handleAuthError(OperationType.UPDATE, path, error);
    }
  }

  async deleteBoard(projectId: string, boardId: string): Promise<void> {
    const path = `projects/${projectId}/boards/${boardId}`;
    try {
      await deleteDoc(doc(db, 'projects', projectId, 'boards', boardId));
    } catch (error) {
      this.handleAuthError(OperationType.DELETE, path, error);
    }
  }

  // --- TAREAS ---

  async createTask(projectId: string, taskData: Omit<Task, 'id' | 'projectId' | 'createdAt' | 'updatedAt' | 'reporterId'>): Promise<string> {
    if (!auth.currentUser) throw new Error("No autenticado");
    
    const tasksRef = collection(db, 'projects', projectId, 'tasks');
    const taskDoc = doc(tasksRef);
    const path = `projects/${projectId}/tasks/${taskDoc.id}`;

    try {
      await setDoc(taskDoc, {
        ...taskData,
        id: taskDoc.id,
        projectId,
        reporterId: auth.currentUser.uid,
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp()
      });

      if (taskData.assigneeIds && taskData.assigneeIds.length > 0) {
        for (const assigneeId of taskData.assigneeIds) {
          if (assigneeId !== auth.currentUser.uid) {
            await this.createNotification({
              userId: assigneeId,
              type: 'ASSIGNED',
              taskId: taskDoc.id,
              projectId,
              title: 'Nueva Tarea Asignada',
              message: `Se te ha asignado la tarea: ${taskData.title}`,
              read: false
            });
          }
        }
      }

      await this.auditProjectAction(projectId, 'CREATE', 'TASK', taskDoc.id, `Created task: ${taskData.title}`);

      return taskDoc.id;
    } catch (error) {
      this.handleAuthError(OperationType.CREATE, path, error);
      return '';
    }
  }

  async getTasks(projectId: string, boardId?: string): Promise<Task[]> {
    const path = `projects/${projectId}/tasks`;
    try {
      let q = query(collection(db, 'projects', projectId, 'tasks'));
      if (boardId) {
        q = query(q, where('boardId', '==', boardId));
      }
      const snapshot = await getDocs(q);
      return snapshot.docs.map(doc => doc.data() as Task);
    } catch (error) {
      this.handleAuthError(OperationType.LIST, path, error);
      return [];
    }
  }

  async updateTaskStatus(projectId: string, taskId: string, status: string): Promise<void> {
    const path = `projects/${projectId}/tasks/${taskId}`;
    try {
      const taskRef = doc(db, 'projects', projectId, 'tasks', taskId);
      
      const prevTaskSnap = await getDoc(taskRef);
      const prevTask = prevTaskSnap.data() as Task;

      await updateDoc(taskRef, {
        status,
        updatedAt: serverTimestamp()
      });

      if (prevTask && prevTask.assigneeIds && prevTask.assigneeIds.length > 0) {
        for (const assigneeId of prevTask.assigneeIds) {
          if (assigneeId !== auth.currentUser?.uid) {
            await this.createNotification({
              userId: assigneeId,
              type: 'STATUS_CHANGE',
              taskId,
              projectId,
              title: 'Estado de Tarea Actualizado',
              message: `La tarea "${prevTask.title}" cambió su estado.`,
              read: false
            });
          }
        }
      }
    } catch (error) {
      this.handleAuthError(OperationType.UPDATE, path, error);
    }
  }

  async updateTask(projectId: string, taskId: string, updates: Partial<Task>): Promise<void> {
    const path = `projects/${projectId}/tasks/${taskId}`;
    try {
      const taskRef = doc(db, 'projects', projectId, 'tasks', taskId);
      
      const prevTaskSnap = await getDoc(taskRef);
      const prevTask = prevTaskSnap.data() as Task;

      await updateDoc(taskRef, {
        ...updates,
        updatedAt: serverTimestamp()
      });

      if (prevTask) {
        if (updates.status && updates.status !== prevTask.status) {
          const assigneesToNotify = prevTask.assigneeIds || [];
          for (const assigneeId of assigneesToNotify) {
            if (assigneeId !== auth.currentUser?.uid) {
               await this.createNotification({
                 userId: assigneeId,
                 type: 'STATUS_CHANGE',
                 taskId,
                 projectId,
                 title: 'Estado de Tarea Actualizado',
                 message: `La tarea "${updates.title || prevTask.title}" cambió su estado.`,
                 read: false
               });
            }
          }
        }

        // Did we assign to new people?
        if (updates.assigneeIds) {
          const newAssignees = updates.assigneeIds.filter(id => !prevTask.assigneeIds?.includes(id));
          for (const assigneeId of newAssignees) {
            if (assigneeId !== auth.currentUser?.uid) {
               await this.createNotification({
                 userId: assigneeId,
                 type: 'ASSIGNED',
                 taskId,
                 projectId,
                 title: 'Nueva Tarea Asignada',
                 message: `Se te ha asignado la tarea: ${updates.title || prevTask.title}`,
                 read: false
               });
            }
          }
        }

        // Newly added comments
        if (updates.comments && prevTask.comments && updates.comments.length > prevTask.comments.length) {
          const assigneesToNotify = updates.assigneeIds || prevTask.assigneeIds || [];
          for (const assigneeId of assigneesToNotify) {
            if (assigneeId !== auth.currentUser?.uid) {
               await this.createNotification({
                 userId: assigneeId,
                 type: 'COMMENT',
                 taskId,
                 projectId,
                 title: 'Nuevo Comentario',
                 message: `Se ha comentado en la tarea: ${updates.title || prevTask.title}`,
                 read: false
               });
            }
          }
        }
      }

      await this.auditProjectAction(projectId, 'UPDATE', 'TASK', taskId, `Updated task: ${updates.title || prevTask?.title || taskId}`);
    } catch (error) {
      this.handleAuthError(OperationType.UPDATE, path, error);
    }
  }

  async deleteTask(projectId: string, taskId: string): Promise<void> {
    const path = `projects/${projectId}/tasks/${taskId}`;
    try {
      await deleteDoc(doc(db, 'projects', projectId, 'tasks', taskId));
      await this.auditProjectAction(projectId, 'DELETE', 'TASK', taskId, 'Deleted task');
    } catch (error) {
      this.handleAuthError(OperationType.DELETE, path, error);
    }
  }

  async auditProjectAction(
    projectId: string,
    action: 'CREATE' | 'UPDATE' | 'DELETE',
    entityType: 'BOARD' | 'TASK' | 'PROJECT',
    entityId: string,
    details?: string
  ): Promise<void> {
    if (!auth.currentUser) return;
    try {
      const logRef = doc(collection(db, 'projects', projectId, 'audit_logs'));
      await setDoc(logRef, {
        id: logRef.id,
        projectId,
        action,
        entityType,
        entityId,
        userId: auth.currentUser.uid,
        details: details || '',
        createdAt: serverTimestamp()
      });
    } catch (e) {
      console.error('Error in audit log', e);
    }
  }

  // --- NOTIFICACIONES ---

  async createNotification(notification: Omit<AppNotification, 'id' | 'createdAt'>): Promise<void> {
    if (!auth.currentUser) return;
    try {
      // Fetch user profile to check preferences
      const userRef = doc(db, 'users', notification.userId);
      const userSnap = await getDoc(userRef);
      if (userSnap.exists()) {
        const userData = userSnap.data();
        const prefs = userData.notificationPreferences || {};
        
        // Defaults to true if not set
        const inAppPref = prefs[notification.type]?.inApp ?? true;
        const emailPref = prefs[notification.type]?.email ?? true;
        
        // Let's pretend we send an email here if emailPref is true
        if (emailPref) {
           console.log(`Sending email to ${userData.email} for ${notification.type}`);
        }
        
        // If in-app notification is disabled, do not create doc
        if (!inAppPref) {
          return;
        }
      }
      
      const docRef = doc(collection(db, 'notifications'));
      await setDoc(docRef, {
        ...notification,
        id: docRef.id,
        createdAt: serverTimestamp()
      });
    } catch (e) {
      console.error('Error creating notification', e);
    }
  }

  async markNotificationRead(notificationId: string): Promise<void> {
    try {
      await updateDoc(doc(db, 'notifications', notificationId), {
        read: true
      });
    } catch (e) {
      console.error('Error marking notification read', e);
    }
  }

  // --- SAVED FILTERS ---
  async saveFilter(filter: Omit<SavedFilter, 'id' | 'createdAt' | 'userId'>): Promise<void> {
    if (!auth.currentUser) return;
    try {
      const ref = doc(collection(db, 'users', auth.currentUser.uid, 'saved_filters'));
      await setDoc(ref, {
        id: ref.id,
        userId: auth.currentUser.uid,
        projectId: filter.projectId,
        name: filter.name,
        criteria: filter.criteria,
        createdAt: serverTimestamp()
      });
    } catch (e) {
      this.handleAuthError(OperationType.CREATE, 'users/saved_filters', e);
    }
  }

  async getSavedFilters(projectId: string): Promise<SavedFilter[]> {
    if (!auth.currentUser) return [];
    try {
      const q = query(
        collection(db, 'users', auth.currentUser.uid, 'saved_filters'),
        where('projectId', '==', projectId)
      );
      const snapshot = await getDocs(q);
      return snapshot.docs.map(d => ({ ...d.data(), id: d.id } as SavedFilter));
    } catch (e) {
      this.handleAuthError(OperationType.LIST, 'users/saved_filters', e);
      return [];
    }
  }

  async deleteSavedFilter(filterId: string): Promise<void> {
    if (!auth.currentUser) return;
    try {
      await deleteDoc(doc(db, 'users', auth.currentUser.uid, 'saved_filters', filterId));
    } catch (e) {
      this.handleAuthError(OperationType.DELETE, 'users/saved_filters', e);
    }
  }
}

export const dbService = DatabaseService.getInstance();
