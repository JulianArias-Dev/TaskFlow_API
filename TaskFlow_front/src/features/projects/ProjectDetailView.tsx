import { useState, useEffect } from 'react';
import { ArrowLeft, Settings, Plus, Edit2, Trash2, ChevronLeft, ChevronRight, Clock, Box, Tag, Users, UserCircle, CornerDownRight, ListTree } from 'lucide-react';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import { motion, AnimatePresence } from 'framer-motion';
import confetti from 'canvas-confetti';
import { Project, Board, BoardColumn, ProjectStatus, Task } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { dbService } from '../../services/databaseService';
import { ProjectSettingsModal } from './ProjectSettingsModal';
import { TaskModal } from './TaskModal';
import { ProjectDashboard } from './ProjectDashboard';

import { TaskFilterBar, TaskFilterCriteria } from './TaskFilterBar';
import { TaskManagerProxy, TagFactory } from '../../lib/designPatterns';

export function ProjectDetailView({ 
  project, 
  userRole,
  initialTab = 'board',
  onBack, 
  onUpdate 
}: { 
  project: Project; 
  userRole?: string;
  initialTab?: 'board' | 'dashboard' | 'settings';
  onBack: () => void;
  onUpdate: () => void;
}) {
  const [activeTab, setActiveTab] = useState<'board' | 'dashboard' | 'settings'>(initialTab);
  const [board, setBoard] = useState<Board | null>(null);
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [showSettings, setShowSettings] = useState(false);
  const [members, setMembers] = useState<{ id: string; name: string }[]>([]);

  useEffect(() => {
    setActiveTab(initialTab);
    if (initialTab === 'settings') {
      setShowSettings(true);
    } else {
      setShowSettings(false);
    }
  }, [initialTab]);
  
  const [undoState, setUndoState] = useState<{ task: Task | null; timeoutId?: ReturnType<typeof setTimeout> }>({ task: null });
  
  const [taskModalState, setTaskModalState] = useState<{ isOpen: boolean; task?: Task | null; columnId: string }>({
    isOpen: false, columnId: ''
  });

  const [columnModalState, setColumnModalState] = useState<{ isOpen: boolean, isEdit: boolean, col?: BoardColumn }>({ isOpen: false, isEdit: false });
  const [confirmDeleteColId, setConfirmDeleteColId] = useState<string | null>(null);
  const [activeUsers, setActiveUsers] = useState<any[]>([]);

  useEffect(() => {
    // Feature de presencia en tiempo real pendiente de migrar al backend
    // (antes vivía en Firestore). Por ahora simplemente vaciamos la lista.
    setActiveUsers([]);
  }, [project.id]);

  const [filterCriteria, setFilterCriteria] = useState<TaskFilterCriteria>({
    searchTerm: '',
    assignees: [],
    priorities: [],
    types: [],
    labels: [],
    dateFrom: '',
    dateTo: ''
  });

  const isArchived = project.status === ProjectStatus.ARCHIVADO;

  const loadBoard = async () => {
    setLoading(true);
    const boards = await dbService.getBoards(project.id);
    if (boards.length > 0) {
      setBoard(boards[0]);
      const boardTasks = await dbService.getTasks(project.id, boards[0].id);
      setTasks(boardTasks);
      // El backend genera notificaciones de vencimiento (overdue) en su capa
      // de servicio; ya no es responsabilidad del cliente disparar la auditoría.
    }
    
    try {
      const pms = await dbService.getProjectMembers(project.id) as any[];
      setMembers(pms.map(m => ({ id: m.uid, name: m.email })));
    } catch (e) {
      console.error(e);
    }
    
    setLoading(false);
  };

  useEffect(() => {
    loadBoard();
  }, [project.id]);

  const handleCreateColumn = () => {
    if (!board || isArchived) return;
    setColumnModalState({ isOpen: true, isEdit: false });
  };

  const handleEditColumn = (col: BoardColumn) => {
    if (!board || isArchived) return;
    setColumnModalState({ isOpen: true, isEdit: true, col });
  };

  const handleDeleteColumn = (colId: string) => {
    if (!board || isArchived) return;
    setConfirmDeleteColId(colId);
  };

  const confirmDeleteColumn = async () => {
    if (!board || !confirmDeleteColId) return;
    try {
      await dbService.deleteColumn(confirmDeleteColId);
    } catch (e) {
      alert(e instanceof Error ? e.message : 'No se pudo eliminar la columna');
    }
    setConfirmDeleteColId(null);
    await loadBoard();
  };

  const saveColumn = async (name: string, wipLimitRaw: string) => {
    if (!board) return;
    const wipLimit = wipLimitRaw && !isNaN(parseInt(wipLimitRaw)) ? parseInt(wipLimitRaw) : null;

    if (columnModalState.isEdit && columnModalState.col) {
      const updatedName = name.trim() || columnModalState.col.name;
      await dbService.updateColumn(columnModalState.col.id, { name: updatedName, wipLimit });
    } else {
      await dbService.createColumn(board.id, {
        name: name.trim() || 'Nueva Columna',
        displayOrder: board.columns.length,
        wipLimit,
      });
    }

    setColumnModalState({ isOpen: false, isEdit: false });
    await loadBoard();
  };

  const handleMoveColumn = async (index: number, direction: 'left' | 'right') => {
    if (!board || isArchived) return;
    const newCols = [...board.columns];
    const targetIndex = direction === 'left' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= newCols.length) return;

    [newCols[index], newCols[targetIndex]] = [newCols[targetIndex], newCols[index]];
    await dbService.reorderColumns(newCols.map((c) => ({ id: c.id })));
    await loadBoard();
  };

  const handleCreateTask = (colId: string) => {
    if (!board || isArchived) return;

    const blockedStatus = ['completed', 'completado', 'cancelled', 'archivado'];
    if (blockedStatus.includes(project.status.toLowerCase())) {
      alert('No se pueden crear tareas en un proyecto con estado ' + project.status);
      return;
    }

    setTaskModalState({ isOpen: true, columnId: colId });
  };

  const handleEditTask = (task: Task) => {
    if (!board || isArchived) return;

    const blockedStatus = ['completed', 'completado', 'cancelled', 'archivado'];
    if (blockedStatus.includes(project.status.toLowerCase())) {
      alert('No se pueden editar tareas en un proyecto con estado ' + project.status);
      return;
    }

    setTaskModalState({ isOpen: true, task, columnId: task.status });
  };

  const triggerUndoableAction = (oldTask: Task) => {
    if (undoState.timeoutId) clearTimeout(undoState.timeoutId);
    const timeoutId = setTimeout(() => {
      setUndoState({ task: null });
    }, 10000); // 10 seconds to undo
    setUndoState({ task: oldTask, timeoutId });
  };

  const undoLastAction = async () => {
    if (!undoState.task) return;
    
    // Reverse the update
    await dbService.updateTask(project.id, undoState.task.id, undoState.task);
    
    if (undoState.timeoutId) clearTimeout(undoState.timeoutId);
    setUndoState({ task: null });
    await loadBoard();
  };

  const handleSaveTask = async (taskData: Partial<Task>) => {
    if (!board) return;
    if (taskModalState.task) {
      triggerUndoableAction(taskModalState.task);
      await dbService.updateTask(project.id, taskModalState.task.id, taskData);
    } else {
      await dbService.createTask(project.id, taskData as any);
    }
    await loadBoard();
  };

  const handleCloneTask = async (taskData: Partial<Task>) => {
    if (!board) return;
    await dbService.createTask(project.id, taskData as any);
    await loadBoard();
  };

  const onDragEnd = async (result: any) => {
    if (isArchived) return;
    
    const { source, destination, draggableId } = result;
    if (!destination) return;
    if (source.droppableId === destination.droppableId && source.index === destination.index) return;

    const taskId = draggableId;
    const task = tasks.find(t => t.id === taskId);
    if (!task) return;

    const oldStatus = task.status;
    const newStatus = destination.droppableId;

    if (oldStatus !== newStatus) {
      triggerUndoableAction(task);
      
      // Check if dropped into 'Done' column
      const destCol = board?.columns.find(c => c.id === newStatus);
      if (destCol && (destCol.name.toLowerCase().includes('hecho') || destCol.name.toLowerCase().includes('completad') || destCol.name.toLowerCase().includes('done'))) {
        confetti({
          particleCount: 100,
          spread: 70,
          origin: { y: 0.6 },
          colors: ['#10b981', '#3b82f6', '#f59e0b']
        });
      }

      // Optimistic update
      setTasks(prev => prev.map(t => t.id === taskId ? { ...t, status: newStatus } : t));

      const proxy = new TaskManagerProxy();
      await proxy.updateTask(project.id, taskId, { status: newStatus });
    }
  };

  const [confirmDeleteTaskId, setConfirmDeleteTaskId] = useState<string | null>(null);

  const handleDeleteTask = (taskId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!board || isArchived) return;
    setConfirmDeleteTaskId(taskId);
  };

  const confirmDeleteTask = async () => {
    if (!board || !confirmDeleteTaskId) return;
    try {
      const proxy = new TaskManagerProxy();
      await proxy.deleteTask(project.id, confirmDeleteTaskId);
      setConfirmDeleteTaskId(null);
      await loadBoard();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Error borrando tarea');
    }
  };

  const getPriorityColor = (priority: string) => {
    // Catálogo actual del backend: Baja/Media/Alta/Crítica.
    // También aceptamos nombres legacy (LOW/MEDIUM/HIGH/CRITICAL, URGENTE).
    switch (priority?.toUpperCase()) {
      case 'BAJA':
      case 'LOW':
        return 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300';
      case 'MEDIA':
      case 'MEDIUM':
        return 'bg-blue-100 text-blue-700';
      case 'ALTA':
      case 'HIGH':
        return 'bg-orange-100 text-orange-700';
      case 'CRÍTICA':
      case 'CRITICA':
      case 'URGENTE':
      case 'URGENT':
      case 'CRITICAL':
        return 'bg-red-100 text-red-700';
      default:
        return 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300';
    }
  };

  const getTypeStyle = (type: string) => {
    switch (type?.toUpperCase()) {
      case 'BUG':
        return 'text-red-500 border-red-200 bg-red-50';
      case 'FEATURE':
        return 'text-purple-500 border-purple-200 bg-purple-50';
      case 'IMPROVEMENT':
      case 'ENHANCEMENT':
        return 'text-green-500 border-green-200 bg-green-50';
      case 'RESEARCH':
        return 'text-yellow-600 border-yellow-200 bg-yellow-50';
      case 'TASK':
      default:
        return 'text-blue-500 border-blue-200 bg-blue-50';
    }
  };

  const filteredTasks = tasks.filter(task => {
    if (filterCriteria.searchTerm) {
      const term = filterCriteria.searchTerm.toLowerCase();
      if (!task.title.toLowerCase().includes(term) && !(task.description || '').toLowerCase().includes(term)) {
        return false;
      }
    }
    if (filterCriteria.assignees.length > 0) {
      const hasAssignee = task.assigneeIds?.some(id => filterCriteria.assignees.includes(id));
      if (!hasAssignee) return false;
    }
    if (filterCriteria.priorities.length > 0) {
      if (!filterCriteria.priorities.includes(task.priority)) return false;
    }
    if (filterCriteria.types.length > 0) {
      if (!filterCriteria.types.includes(task.type?.toUpperCase())) return false;
    }
    if (filterCriteria.labels.length > 0) {
      const hasLabel = task.labels?.some(l => filterCriteria.labels.includes(l.name));
      if (!hasLabel) return false;
    }
    if (filterCriteria.dateFrom) {
      if (!task.dueDate || new Date(task.dueDate) < new Date(filterCriteria.dateFrom)) return false;
    }
    if (filterCriteria.dateTo) {
      if (!task.dueDate || new Date(task.dueDate) > new Date(filterCriteria.dateTo)) return false;
    }
    return true;
  });

  const uniqueLabels = Array.from(new Map(tasks.flatMap(t => t.labels || []).map(l => [l.name, l])).values());

  return (
    <div className="flex flex-col h-full bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 min-h-screen">
      <div className="p-4 md:p-6 pb-0 border-b border-gray-200 dark:border-gray-700 bg-white/50 dark:bg-gray-800/50 backdrop-blur-sm sticky top-0 z-20">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-50">{project.name}</h2>
          </div>
          
          <div className="flex items-center gap-2">
            {activeUsers.length > 0 && (
              <div className="flex items-center mr-4">
                <span className="text-xs text-gray-500 mr-2 flex items-center"><span className="w-2 h-2 rounded-full bg-green-500 mr-1 animate-pulse"></span> Viendo: </span>
                <div className="flex -space-x-2">
                  {activeUsers.slice(0, 3).map(u => (
                    <div key={u.userId} className="w-8 h-8 rounded-full bg-blue-100 border-2 border-white dark:border-gray-800 flex items-center justify-center text-xs font-bold text-blue-700" title={u.email}>
                      {u.email.substring(0, 2).toUpperCase()}
                    </div>
                  ))}
                  {activeUsers.length > 3 && (
                    <div className="w-8 h-8 rounded-full bg-gray-100 border-2 border-white dark:border-gray-800 flex items-center justify-center text-xs font-bold text-gray-600">
                      +{activeUsers.length - 3}
                    </div>
                  )}
                </div>
              </div>
            )}
            <Button size="sm" variant="outline" onClick={() => setShowSettings(true)}>
              <Settings className="w-4 h-4 mr-2" />
              Ajustes
            </Button>
          </div>
        </div>

        {!loading && board && activeTab === 'board' && (
          <div className="pb-4">
            <TaskFilterBar 
              projectId={project.id} 
              criteria={filterCriteria} 
              onFilterChange={setFilterCriteria} 
              uniqueAssignees={members}
              uniqueLabels={uniqueLabels}
            />
          </div>
        )}
      </div>

      <div className="flex-1 p-4 md:p-6">
        {loading ? (
          <div className="flex-1 flex items-center justify-center py-20">
            <div className="w-8 h-8 border-4 border-blue-600/30 border-t-blue-600 rounded-full animate-spin" />
          </div>
        ) : activeTab === 'dashboard' ? (
          <ProjectDashboard project={project} tasks={tasks} columns={board?.columns || []} />
        ) : board ? (
        <div className="flex-1 overflow-x-auto pb-4">
          <DragDropContext onDragEnd={onDragEnd}>
            <div className="flex gap-4 h-full items-start">
              {board.columns?.map((col, idx) => {
                const columnTasks = filteredTasks.filter(t => t.status === col.id);
                const isOverWip = col.wipLimit != null && columnTasks.length > col.wipLimit;
                
                return (
                <div key={col.id} className="min-w-[300px] w-[300px] bg-gray-200/50 rounded-xl flex flex-col max-h-full">
                  <div className="p-3 font-semibold text-gray-700 dark:text-gray-200 flex justify-between items-center group">
                    <div className="flex items-center gap-2">
                      <span>{col.name}</span>
                      <span className={`text-xs py-0.5 px-2 rounded-full ${isOverWip ? 'bg-red-100 text-red-600' : 'bg-gray-300 text-gray-600 dark:text-gray-300'}`}>
                        {columnTasks.length}{col.wipLimit ? ` / ${col.wipLimit}` : ''}
                      </span>
                    </div>
                    
                    {!isArchived && (
                      <div className="opacity-0 group-hover:opacity-100 flex gap-1 transition-opacity">
                        <button onClick={() => handleMoveColumn(idx, 'left')} disabled={idx === 0} className="p-1 hover:bg-gray-300 rounded disabled:opacity-30">
                          <ChevronLeft className="w-3.5 h-3.5" />
                        </button>
                        <button onClick={() => handleMoveColumn(idx, 'right')} disabled={idx === board.columns.length - 1} className="p-1 hover:bg-gray-300 rounded disabled:opacity-30">
                          <ChevronRight className="w-3.5 h-3.5" />
                        </button>
                        <button onClick={() => handleEditColumn(col)} className="p-1 hover:bg-gray-300 rounded text-blue-600">
                          <Edit2 className="w-3.5 h-3.5" />
                        </button>
                        <button onClick={() => handleDeleteColumn(col.id)} className="p-1 hover:bg-gray-300 rounded text-red-600">
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    )}
                  </div>
                  
                  <Droppable droppableId={col.id}>
                    {(provided, snapshot) => (
                      <div 
                        className={`p-2 flex-1 overflow-y-auto space-y-2 relative min-h-[50px] ${snapshot.isDraggingOver ? 'bg-gray-300/30' : ''}`}
                        {...provided.droppableProps}
                        ref={provided.innerRef}
                      >
                        {columnTasks.map((task, index) => {
                          // Resolución del padre para mostrar el badge "↳ subtarea de X"
                          // Las tareas del proyecto ya están en memoria (estado `tasks`),
                          // así que es una búsqueda O(n) sin queries extra.
                          const parent = task.parentTaskId
                            ? tasks.find((t) => t.id === task.parentTaskId)
                            : undefined;
                          return (
                          <Draggable key={task.id} draggableId={task.id} index={index} isDragDisabled={isArchived}>
                            {(provided, snapshot) => (
                              <div
                                ref={provided.innerRef}
                                {...provided.draggableProps}
                                {...provided.dragHandleProps}
                                className={`bg-white dark:bg-gray-800 dark:text-gray-100 p-3 rounded-lg shadow-sm border ${task.parentTaskId ? 'border-l-4 border-l-indigo-300 border-y-gray-100 border-r-gray-100 dark:border-y-gray-700 dark:border-r-gray-700' : 'border-gray-100 dark:border-gray-700'} transition-shadow group flex flex-col gap-2 relative ${snapshot.isDragging ? 'shadow-lg rotate-1 z-50' : 'hover:shadow-md'}`}
                              >
                                {parent && (
                                  <button
                                    type="button"
                                    onClick={(e) => { e.stopPropagation(); handleEditTask(parent); }}
                                    className="flex items-center gap-1 text-[10px] text-indigo-600 hover:underline self-start"
                                    title={`Abrir tarea padre: ${parent.title}`}
                                  >
                                    <CornerDownRight className="w-3 h-3" />
                                    <span className="truncate max-w-[200px]">subtarea de: {parent.title}</span>
                                  </button>
                                )}
                                {task.labels && task.labels.length > 0 && (
                                  <div className="flex flex-wrap gap-1">
                                    {task.labels.map((lbl, i) => {
                                      const flyweight = TagFactory.getTagType(lbl.name, '', '');
                                      return (
                                        <span key={i} className="text-[10px] px-1.5 py-0.5 rounded-full font-medium text-white" style={{ backgroundColor: lbl.color }}>
                                          {flyweight.name}
                                        </span>
                                      );
                                    })}
                                  </div>
                                )}
                                <div className="flex justify-between items-start">
                                  <p className={`text-sm font-medium text-gray-900 dark:text-gray-50 ${!isArchived ? 'cursor-pointer hover:text-blue-600' : ''}`} onClick={() => !isArchived && handleEditTask(task)}>
                                    {task.title}
                                  </p>
                                  {!isArchived && (
                                    <button onClick={(e) => handleDeleteTask(task.id, e)} className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-red-500 transition-opacity flex-shrink-0 ml-2">
                                      <Trash2 className="w-3.5 h-3.5" />
                                    </button>
                                  )}
                                </div>
                                {task.description && (
                                  <p className="text-xs text-gray-500 dark:text-gray-400 line-clamp-2">{task.description}</p>
                                )}
                                <div className="flex flex-wrap items-center gap-2 mt-auto pt-2">
                                  <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full uppercase tracking-wider ${getPriorityColor(task.priority)}`}>
                                    {task.priority || 'NORMAL'}
                                  </span>
                                  {task.type && (
                                    <span className={`text-[10px] font-bold px-2 py-0.5 rounded uppercase tracking-wider border ${getTypeStyle(task.type)}`}>
                                      {task.type}
                                    </span>
                                  )}
                                  {task.subTaskCount > 0 && (
                                    <span
                                      className="flex items-center gap-0.5 text-[10px] font-medium text-indigo-600 bg-indigo-50 dark:bg-indigo-900/30 px-1.5 py-0.5 rounded"
                                      title={`${task.subTaskCount} subtarea${task.subTaskCount !== 1 ? 's' : ''}`}
                                    >
                                      <ListTree className="w-3 h-3" /> {task.subTaskCount}
                                    </span>
                                  )}
                                  {(task.dueDate || task.estimatedHours || task.loggedHours) && (
                                    <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 ml-auto">
                                      {task.dueDate && (
                                        <span className={`flex items-center gap-0.5 ${!col.name.toLowerCase().includes('hecho') && !col.name.toLowerCase().includes('completad') && !col.name.toLowerCase().includes('done') && new Date(task.dueDate).getTime() < new Date().setHours(0,0,0,0) ? 'text-red-600 font-bold' : ''}`} title="Vencimiento"><Clock className="w-3 h-3" /> {task.dueDate.split('-').reverse().join('-')}</span>
                                      )}
                                      {task.estimatedHours && (
                                        <span className="flex items-center gap-0.5" title="Estimación"><Box className="w-3 h-3" /> {task.estimatedHours}h</span>
                                      )}
                                      {task.loggedHours && (
                                        <span className="flex items-center gap-0.5 text-blue-600 font-medium" title="Horas Trabajadas"><Clock className="w-3 h-3" /> {task.loggedHours}h</span>
                                      )}
                                    </div>
                                  )}
                                </div>
                                
                                {task.assigneeIds && task.assigneeIds.length > 0 && (
                                  <div className="flex flex-wrap border-t border-gray-100 dark:border-gray-700 pt-2 mt-1 justify-between gap-2 items-center text-xs text-gray-400">
                                    <span>{task.assigneeIds.length} responsable{task.assigneeIds.length !== 1 ? 's' : ''}</span>
                                  </div>
                                )}
                              </div>
                            )}
                          </Draggable>
                          );
                        })}
                        {provided.placeholder}
                      </div>
                    )}
                  </Droppable>
                  
                  {!isArchived && (
                    <div className="p-2">
                      <button onClick={() => handleCreateTask(col.id)} className="w-full py-2 hover:bg-gray-300/50 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:text-gray-200 text-sm font-medium rounded flex items-center justify-center gap-1 transition-colors">
                        <Plus className="w-4 h-4" /> Añadir tarea
                      </button>
                    </div>
                  )}
                </div>
                )})}

              {!isArchived && (
                <button 
                  onClick={handleCreateColumn}
                  className="min-w-[300px] w-[300px] bg-gray-200/50 hover:bg-gray-200 dark:bg-gray-600 rounded-xl flex items-center justify-center gap-2 p-4 text-gray-500 dark:text-gray-400 font-medium transition-colors border-2 border-dashed border-gray-300 dark:border-gray-600 hover:border-gray-400 h-[100px]"
                >
                  <Plus className="w-5 h-5" />
                  Añadir Columna
                </button>
              )}
            </div>
          </DragDropContext>
        </div>
      ) : (
        <div className="flex-1 flex flex-col items-center justify-center text-gray-500 dark:text-gray-400">
          <p>No se encontró un tablero para este proyecto.</p>
        </div>
      )}
    </div>

      <AnimatePresence>
        {showSettings && (
          <ProjectSettingsModal 
            project={project} 
            userRole={userRole}
            onClose={() => setShowSettings(false)} 
            onUpdate={() => { 
              onUpdate();
              loadBoard();
              setShowSettings(false);
            }} 
          />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {taskModalState.isOpen && board && (
          <TaskModal
            task={taskModalState.task}
            projectId={project.id}
            boardId={board.id}
            columns={board.columns}
            statusId={taskModalState.columnId}
            onClose={() => setTaskModalState({ isOpen: false, columnId: '' })}
            onSave={handleSaveTask}
            onClone={handleCloneTask}
          />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {columnModalState.isOpen && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
          >
            <motion.div 
              initial={{ scale: 0.95, opacity: 0, y: 20 }}
              animate={{ scale: 1, opacity: 1, y: 0 }}
              exit={{ scale: 0.95, opacity: 0, y: 20 }}
              className="bg-white dark:bg-gray-800 rounded-xl shadow-xl p-6 w-full max-w-md"
            >
              <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 mb-4">
                {columnModalState.isEdit ? 'Editar Columna' : 'Nueva Columna'}
              </h3>
              <form onSubmit={e => {
                e.preventDefault();
                const target = e.target as HTMLFormElement;
                saveColumn(target.colName.value, target.wipLimit.value);
              }}>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Nombre</label>
                    <input
                      name="colName"
                      required
                      defaultValue={columnModalState.col?.name || ''}
                      className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-gray-50"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Límite WIP (Opcional)</label>
                    <input
                      name="wipLimit"
                      type="number"
                      min="1"
                      defaultValue={columnModalState.col?.wipLimit || ''}
                      className="w-full px-3 py-2 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-gray-50"
                    />
                  </div>
                </div>
                <div className="mt-6 flex justify-end gap-3">
                  <Button type="button" variant="outline" onClick={() => setColumnModalState({ isOpen: false, isEdit: false })}>
                    Cancelar
                  </Button>
                  <Button type="submit">
                    Guardar
                  </Button>
                </div>
              </form>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {confirmDeleteTaskId && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
          >
            <motion.div 
              initial={{ scale: 0.95, opacity: 0, y: 20 }}
              animate={{ scale: 1, opacity: 1, y: 0 }}
              exit={{ scale: 0.95, opacity: 0, y: 20 }}
              className="bg-white dark:bg-gray-800 rounded-xl shadow-xl p-6 w-full max-w-md text-center"
            >
              <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 mb-4">¿Eliminar tarea?</h3>
              <p className="text-gray-600 dark:text-gray-300 mb-6 font-medium">Esta acción no se puede deshacer.</p>
              <div className="flex justify-center gap-3">
                <Button variant="outline" onClick={() => setConfirmDeleteTaskId(null)}>
                  Cancelar
                </Button>
                <Button onClick={confirmDeleteTask} className="bg-red-600 hover:bg-red-700 text-white border-0">
                  Eliminar
                </Button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {confirmDeleteColId && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
          >
            <motion.div 
              initial={{ scale: 0.95, opacity: 0, y: 20 }}
              animate={{ scale: 1, opacity: 1, y: 0 }}
              exit={{ scale: 0.95, opacity: 0, y: 20 }}
              className="bg-white dark:bg-gray-800 rounded-xl shadow-xl p-6 w-full max-w-md text-center"
            >
              <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 mb-4">¿Eliminar columna?</h3>
              <p className="text-gray-600 dark:text-gray-300 mb-6 font-medium">Las tareas seguirán existiendo pero no se verán en el tablero a menos que cambies su estado.</p>
              <div className="flex justify-center gap-3">
                <Button variant="outline" onClick={() => setConfirmDeleteColId(null)}>
                  Cancelar
                </Button>
                <Button onClick={confirmDeleteColumn} className="bg-red-600 hover:bg-red-700 text-white border-0">
                  Eliminar
                </Button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {undoState.task && (
          <motion.div 
            initial={{ opacity: 0, y: 50, x: "-50%" }}
            animate={{ opacity: 1, y: 0, x: "-50%" }}
            exit={{ opacity: 0, y: 50, x: "-50%" }}
            className="fixed bottom-6 left-1/2 z-50 origin-bottom"
          >
            <div className="bg-gray-900 text-white px-4 py-3 rounded-lg shadow-xl flex items-center justify-between gap-4 md:min-w-[300px]">
              <span className="text-sm">Tarea {undoState.task.title} modificada</span>
              <button 
                onClick={undoLastAction}
                className="text-blue-400 hover:text-blue-300 text-sm font-semibold whitespace-nowrap"
              >
                Deshacer
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
