import React, { useState, useEffect } from 'react';
import { Task, TaskLabel, BoardColumn, TaskHistoryEvent, TaskAttachment } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { X, Plus, Tag, Paperclip, Download, Copy, Trash2, Loader2 } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { dbService } from '../../services/databaseService';
import { auth } from '../../lib/firebase';
import { uploadAttachment } from '../../services/storageService';
import { TaskFactory, TaskPrototype } from '../../lib/designPatterns';

const PREDEFINED_COLORS = [
  '#ef4444', '#f97316', '#f59e0b', '#10b981', 
  '#3b82f6', '#6366f1', '#8b5cf6', '#ec4899', '#64748b'
];

interface TaskModalProps {
  task?: Task | null;
  projectId: string;
  boardId: string;
  columns: BoardColumn[];
  statusId: string; // The column ID originally clicked / or task's current column
  onClose: () => void;
  onSave: (taskData: Partial<Task>) => Promise<void>;
  onClone?: (taskData: Partial<Task>) => Promise<void>;
}

export function TaskModal({ task, projectId, boardId, columns, statusId, onClose, onSave, onClone }: TaskModalProps) {
  const [title, setTitle] = useState(task?.title || '');
  const [description, setDescription] = useState(task?.description || '');
  const [priority, setPriority] = useState<'BAJA' | 'MEDIA' | 'ALTA' | 'URGENTE'>(task?.priority || 'MEDIA');
  const [type, setType] = useState<'BUG' | 'FEATURE' | 'TASK' | 'IMPROVEMENT'>(task?.type || 'TASK');
  const [dueDate, setDueDate] = useState(task?.dueDate || '');
  const [estimatedHours, setEstimatedHours] = useState(task?.estimatedHours?.toString() || '');
  const [loggedHours, setLoggedHours] = useState(task?.loggedHours?.toString() || '');
  const [labels, setLabels] = useState<TaskLabel[]>(task?.labels || []);
  const [assigneeIds, setAssigneeIds] = useState<string[]>(task?.assigneeIds || []);
  const [subtasks, setSubtasks] = useState<any[]>(task?.subtasks || []);
  const [comments, setComments] = useState<any[]>(task?.comments || []);
  const [attachments, setAttachments] = useState<TaskAttachment[]>(task?.attachments || []);
  const [isUploading, setIsUploading] = useState(false);
  const [currentStatus, setCurrentStatus] = useState(task?.status || statusId);
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showLabelInput, setShowLabelInput] = useState(false);
  const [newLabelName, setNewLabelName] = useState('');
  const [newLabelColor, setNewLabelColor] = useState(PREDEFINED_COLORS[0]);
  
  const [newSubtaskTitle, setNewSubtaskTitle] = useState('');
  const [newCommentContent, setNewCommentContent] = useState('');
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingCommentContent, setEditingCommentContent] = useState('');

  const [members, setMembers] = useState<any[]>([]);

  useEffect(() => {
    dbService.getProjectMembers(projectId).then(setMembers).catch(console.error);
  }, [projectId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    
    setIsSubmitting(true);
    try {
      let updatedHistory = task?.history || [];
      const oldStatus = task?.status;
      
      const changes: string[] = [];
      if (task) {
        if (oldStatus !== currentStatus) changes.push(`Estado cambiado de ${oldStatus} a ${currentStatus}`);
        if (task.title !== title.trim()) changes.push('Título actualizado');
        if (task.description !== description.trim()) changes.push('Descripción actualizada');
        if (task.priority !== priority) changes.push(`Prioridad cambiada a ${priority}`);
        if (task.type !== type) changes.push(`Tipo cambiado a ${type}`);
        if (task.dueDate !== dueDate) changes.push('Fecha de vencimiento actualizada');
        if (task.estimatedHours?.toString() !== estimatedHours && estimatedHours) changes.push('Horas estimadas actualizadas');
        if (task.loggedHours?.toString() !== loggedHours && loggedHours) changes.push('Horas trabajadas actualizadas');
      }

      if (task && changes.length > 0) {
         updatedHistory = [...updatedHistory, {
           id: Date.now().toString(),
           type: 'UPDATE',
           details: changes.join('. '),
           userId: auth.currentUser!.uid,
           timestamp: new Date().toISOString()
         }];
      } else if (!task) {
         updatedHistory = [{
           id: Date.now().toString(),
           type: 'CREATE',
           details: 'Tarea creada',
           userId: auth.currentUser!.uid,
           timestamp: new Date().toISOString()
         }];
      }

      const baseSaveData: Partial<Task> = {
        boardId,
        status: currentStatus,
        title: title.trim(),
        description: description.trim(),
        priority,
        dueDate: dueDate || null,
        estimatedHours: estimatedHours ? parseFloat(estimatedHours) : undefined,
        loggedHours: loggedHours ? parseFloat(loggedHours) : undefined,
        labels: labels.length > 0 ? labels : undefined,
        assigneeIds,
        subtasks,
        comments,
        attachments,
        history: updatedHistory
      };

      const saveData = task 
        ? { ...baseSaveData, type } // Simple update para tareas existentes
        : TaskFactory.createTask(type, baseSaveData); // Usa Factory Method para nuevas tareas

      await onSave(saveData);
      onClose();
    } catch (error) {
      console.error(error);
      alert('Error al guardar la tarea');
      setIsSubmitting(false);
    }
  };

  const handleClone = async () => {
    if (!onClone || !title.trim()) return;
    setIsSubmitting(true);
    try {
      const sourceData: Task = {
        id: task?.id || `temp-${Date.now()}`,
        boardId,
        status: currentStatus,
        title: title.trim(),
        description: description.trim(),
        priority: priority as any,
        type: type as any,
        dueDate: dueDate || undefined,
        estimatedHours: estimatedHours ? parseFloat(estimatedHours) : undefined,
        loggedHours: loggedHours ? parseFloat(loggedHours) : undefined,
        labels: labels.length > 0 ? labels : undefined,
        assigneeIds,
        attachments,
        subtasks,
        comments: [],
        history: [],
        projectId,
        reporterId: auth.currentUser!.uid,
        createdAt: task?.createdAt || new Date(),
        updatedAt: new Date()
      };

      const prototype = new TaskPrototype(sourceData);
      const clonedTask = prototype.clone();
      
      // Ajustar detalles que requiere el callback onClone
      clonedTask.history = [{
         id: Date.now().toString(),
         type: 'CLONED',
         userId: auth.currentUser!.uid,
         timestamp: new Date().toISOString()
      }];

      await onClone(clonedTask);
      onClose();
    } catch (error) {
      console.error(error);
      alert('Error al clonar la tarea');
      setIsSubmitting(false);
    }
  };

  const handleAddLabel = () => {
    if (!newLabelName.trim()) return;
    setLabels([...labels, { name: newLabelName.trim(), color: newLabelColor }]);
    setNewLabelName('');
    setShowLabelInput(false);
  };

  const handleRemoveLabel = (idxToRemove: number) => {
    setLabels(labels.filter((_, idx) => idx !== idxToRemove));
  };
  
  const toggleAssignee = (uid: string) => {
    if (assigneeIds.includes(uid)) {
      setAssigneeIds(assigneeIds.filter(id => id !== uid));
    } else {
      setAssigneeIds([...assigneeIds, uid]);
    }
  };

  const handleAddSubtask = () => {
    if (!newSubtaskTitle.trim()) return;
    setSubtasks([...subtasks, {
      id: Date.now().toString(),
      title: newSubtaskTitle.trim(),
      completed: false
    }]);
    setNewSubtaskTitle('');
  };

  const toggleSubtask = (id: string) => {
    setSubtasks(subtasks.map(st => st.id === id ? { ...st, completed: !st.completed } : st));
  };

  const removeSubtask = (id: string) => {
    setSubtasks(subtasks.filter(st => st.id !== id));
  };

  const handleAddComment = () => {
    if (!newCommentContent.trim()) return;
    setComments([...comments, {
      id: Date.now().toString(),
      userId: auth.currentUser!.uid,
      content: newCommentContent.trim(),
      createdAt: new Date().toISOString()
    }]);
    setNewCommentContent('');
  };

  const handleDeleteComment = (id: string) => {
    setComments(comments.filter(c => c.id !== id));
  };

  const startEditComment = (id: string, content: string) => {
    setEditingCommentId(id);
    setEditingCommentContent(content);
  };

  const saveEditComment = () => {
    if (!editingCommentContent.trim()) return;
    setComments(comments.map(c => c.id === editingCommentId ? {
      ...c,
      content: editingCommentContent.trim(),
      updatedAt: new Date().toISOString()
    } : c));
    setEditingCommentId(null);
    setEditingCommentContent('');
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploading(true);
    const { url, error } = await uploadAttachment(file);
    setIsUploading(false);

    if (error) {
      alert(error);
      return;
    }

    if (url) {
      setAttachments([...attachments, {
        id: Date.now().toString(),
        name: file.name,
        url,
        size: file.size,
        type: file.type,
        createdAt: new Date().toISOString()
      }]);
    }
  };

  const removeAttachment = (id: string) => {
    setAttachments(attachments.filter(a => a.id !== id));
  };

  return (
    <motion.div 
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-gray-900/50 backdrop-blur-sm"
    >
      <motion.div 
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="bg-white dark:bg-gray-800 dark:text-gray-100 rounded-xl shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden"
      >
        <div className="flex items-center justify-between p-4 border-b border-gray-100 dark:border-gray-700">
          <h2 className="text-xl font-bold text-gray-900 dark:text-gray-50">{task ? 'Editar Tarea' : 'Nueva Tarea'}</h2>
          <button onClick={onClose} className="p-2 text-gray-400 hover:text-gray-600 dark:text-gray-300 rounded-full hover:bg-gray-100 dark:bg-gray-700">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form id="task-form" onSubmit={handleSubmit} className="p-6 overflow-y-auto flex-1 space-y-6">
          <Input
            label="Título"
            value={title}
            onChange={e => setTitle(e.target.value)}
            placeholder="Título de la tarea"
            required
            autoFocus
          />

          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Descripción</label>
            <textarea
              className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 min-h-[100px] resize-y"
              value={description}
              onChange={e => setDescription(e.target.value)}
              placeholder="Descripción detallada de la tarea..."
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Estado (Columna)</label>
              <select 
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={currentStatus}
                onChange={e => setCurrentStatus(e.target.value)}
              >
                {columns.map(col => (
                  <option key={col.id} value={col.id}>{col.name}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Tipo</label>
              <select 
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={type}
                onChange={e => setType(e.target.value as any)}
              >
                <option value="TASK">Tarea</option>
                <option value="FEATURE">Nueva Funcionalidad</option>
                <option value="BUG">Error (Bug)</option>
                <option value="IMPROVEMENT">Mejora</option>
              </select>
            </div>

            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Prioridad</label>
              <select 
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={priority}
                onChange={e => setPriority(e.target.value as any)}
              >
                <option value="BAJA">Baja</option>
                <option value="MEDIA">Media</option>
                <option value="ALTA">Alta</option>
                <option value="URGENTE">Urgente</option>
              </select>
            </div>
            
            <Input
              label="Fecha de Vencimiento"
              type="date"
              value={dueDate}
              onChange={e => setDueDate(e.target.value)}
            />

            <Input
              label="Estimación (Horas)"
              type="number"
              min="0"
              step="0.5"
              value={estimatedHours}
              onChange={e => setEstimatedHours(e.target.value)}
            />
            
            <Input
              label="Horas Trabajadas"
              type="number"
              min="0"
              step="0.5"
              value={loggedHours}
              onChange={e => setLoggedHours(e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Responsables</label>
            <div className="flex flex-wrap gap-2">
              {members.map(member => (
                <button
                  key={member.uid}
                  type="button"
                  onClick={() => toggleAssignee(member.uid)}
                  className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors border ${
                    assigneeIds.includes(member.uid)
                      ? 'bg-blue-100 border-blue-200 text-blue-800'
                      : 'bg-white dark:bg-gray-800 dark:text-gray-100 border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900'
                  }`}
                >
                  {member.displayName || member.email}
                </button>
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Etiquetas</label>
            
            <div className="flex flex-wrap gap-2 mb-2">
              {labels.map((lbl, idx) => (
                <div key={idx} className="flex items-center gap-1.5 px-3 py-1 rounded-full text-sm font-medium text-white shadow-sm" style={{ backgroundColor: lbl.color }}>
                  <span>{lbl.name}</span>
                  <button type="button" onClick={() => handleRemoveLabel(idx)} className="hover:bg-black/20 rounded-full p-0.5">
                    <X className="w-3 h-3" />
                  </button>
                </div>
              ))}
            </div>

            {!showLabelInput ? (
              <Button type="button" variant="outline" className="text-sm py-1 h-auto" onClick={() => setShowLabelInput(true)}>
                <Tag className="w-4 h-4 mr-2" /> Añadir Etiqueta
              </Button>
            ) : (
              <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 flex flex-col gap-3">
                <Input 
                  label="Nombre de la etiqueta"
                  value={newLabelName}
                  onChange={e => setNewLabelName(e.target.value)}
                  placeholder="Ej. Frontend"
                />
                <div>
                  <label className="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Color</label>
                  <div className="flex gap-2">
                    {PREDEFINED_COLORS.map(color => (
                      <button
                        key={color}
                        type="button"
                        onClick={() => setNewLabelColor(color)}
                        className={`w-6 h-6 rounded-full border-2 ${newLabelColor === color ? 'border-gray-900 scale-110' : 'border-transparent'}`}
                        style={{ backgroundColor: color }}
                      />
                    ))}
                  </div>
                </div>
                <div className="flex gap-2 justify-end mt-2">
                  <Button type="button" variant="ghost" className="text-sm" onClick={() => setShowLabelInput(false)}>Cancelar</Button>
                  <Button type="button" className="text-sm" onClick={handleAddLabel}>Añadir</Button>
                </div>
              </div>
            )}
          </div>

          <div className="space-y-3 pt-4 border-t border-gray-100 dark:border-gray-700">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Subtareas</label>
            <div className="space-y-2">
              {subtasks.map((st) => (
                <div key={st.id} className="flex items-center justify-between group bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 p-2 rounded-lg border border-gray-100 dark:border-gray-700">
                  <div className="flex items-center gap-2">
                    <input 
                      type="checkbox" 
                      checked={st.completed} 
                      onChange={() => toggleSubtask(st.id)}
                      className="w-4 h-4 text-blue-600 rounded border-gray-300 dark:border-gray-600 focus:ring-blue-500"
                    />
                    <span className={`text-sm ${st.completed ? 'line-through text-gray-400' : 'text-gray-700 dark:text-gray-200'}`}>
                      {st.title}
                    </span>
                  </div>
                  <button type="button" onClick={() => removeSubtask(st.id)} className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-red-500 p-1">
                    <X className="w-3.5 h-3.5" />
                  </button>
                </div>
              ))}
            </div>
            <div className="flex gap-2">
              <Input 
                value={newSubtaskTitle}
                onChange={e => setNewSubtaskTitle(e.target.value)}
                placeholder="Nueva subtarea..."
                className="flex-1"
                onKeyDown={e => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    handleAddSubtask();
                  }
                }}
              />
              <Button type="button" variant="outline" onClick={handleAddSubtask}><Plus className="w-4 h-4"/></Button>
            </div>
          </div>

          <div className="space-y-3 pt-4 border-t border-gray-100 dark:border-gray-700">
            <div className="flex justify-between items-center">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Archivos Adjuntos</label>
              <div>
                <input type="file" id="file-upload" className="hidden" onChange={handleFileUpload} />
                <label htmlFor="file-upload" className="cursor-pointer text-sm text-blue-600 hover:text-blue-800 flex items-center gap-1 font-medium bg-blue-50 px-2 py-1 rounded-lg">
                  {isUploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Paperclip className="w-4 h-4" />}
                  {isUploading ? 'Subiendo...' : 'Adjuntar'}
                </label>
              </div>
            </div>
            
            {attachments.length > 0 && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {attachments.map(att => (
                  <div key={att.id} className="flex items-center justify-between p-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 dark:text-gray-100 group">
                    <div className="flex items-center gap-2 overflow-hidden">
                      <div className="w-8 h-8 rounded bg-gray-100 dark:bg-gray-700 flex items-center justify-center flex-shrink-0 text-gray-500 dark:text-gray-400">
                        <Paperclip className="w-4 h-4" />
                      </div>
                      <div className="overflow-hidden">
                        <p className="text-xs font-medium text-gray-700 dark:text-gray-200 truncate" title={att.name}>{att.name}</p>
                        <p className="text-[10px] text-gray-400">{(att.size / 1024 / 1024).toFixed(2)} MB</p>
                      </div>
                    </div>
                    <div className="flex items-center opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                      <a href={att.url} target="_blank" rel="noreferrer" className="p-1.5 text-gray-500 dark:text-gray-400 hover:text-blue-600 rounded-lg hover:bg-gray-100 dark:bg-gray-700" title="Descargar">
                        <Download className="w-3.5 h-3.5" />
                      </a>
                      <button type="button" onClick={() => removeAttachment(att.id)} className="p-1.5 text-gray-500 dark:text-gray-400 hover:text-red-600 rounded-lg hover:bg-gray-100 dark:bg-gray-700" title="Eliminar">
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="space-y-3 pt-4 border-t border-gray-100 dark:border-gray-700">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Comentarios</label>
            <div className="space-y-3">
              {comments.map((comment) => {
                const author = members.find(m => m.uid === comment.userId);
                const isOwner = auth.currentUser?.uid === comment.userId;
                const isEditing = editingCommentId === comment.id;

                return (
                  <div key={comment.id} className="flex gap-3 text-sm bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 border border-gray-100 dark:border-gray-700 p-3 rounded-lg relative group">
                    <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold flex-shrink-0">
                      {author?.displayName?.charAt(0) || author?.email?.charAt(0) || '?'}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="font-medium text-gray-900 dark:text-gray-50">{author?.displayName || author?.email || 'Usuario'}</span>
                        <span className="text-xs text-gray-400">{new Date(comment.createdAt).toLocaleString()}</span>
                      </div>
                      
                      {isEditing ? (
                        <div className="mt-2 space-y-2">
                          <textarea 
                            className="w-full px-3 py-2 border border-blue-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500 min-h-[60px]"
                            value={editingCommentContent}
                            onChange={(e) => setEditingCommentContent(e.target.value)}
                          />
                          <div className="flex gap-2 justify-end">
                            <Button type="button" variant="ghost" className="text-xs py-1 h-auto" onClick={() => setEditingCommentId(null)}>Cancelar</Button>
                            <Button type="button" className="text-xs py-1 h-auto" onClick={saveEditComment}>Guardar</Button>
                          </div>
                        </div>
                      ) : (
                        <p className="text-gray-700 dark:text-gray-200 whitespace-pre-wrap">{comment.content}</p>
                      )}
                    </div>
                    
                    {isOwner && !isEditing && (
                      <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 flex gap-1 transition-opacity">
                        <button type="button" onClick={() => startEditComment(comment.id, comment.content)} className="p-1 hover:bg-gray-200 dark:bg-gray-600 text-gray-500 dark:text-gray-400 rounded">
                          Editar
                        </button>
                        <button type="button" onClick={() => handleDeleteComment(comment.id)} className="p-1 hover:bg-gray-200 dark:bg-gray-600 text-red-500 rounded">
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
            
            <div className="space-y-2 mt-2">
              <textarea
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 min-h-[80px]"
                placeholder="Escribe un comentario..."
                value={newCommentContent}
                onChange={e => setNewCommentContent(e.target.value)}
              />
              <div className="flex justify-end">
                <Button type="button" className="text-sm py-1.5 px-3 h-auto" onClick={handleAddComment} disabled={!newCommentContent.trim()}>Comentar</Button>
              </div>
            </div>
          </div>

          {task?.history && task.history.length > 0 && (
            <div className="space-y-2 pt-4 border-t border-gray-100 dark:border-gray-700">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Historial de Cambios</label>
              <div className="space-y-2 max-h-40 overflow-y-auto pr-2">
                {[...task.history].reverse().map(event => {
                  const author = members.find(m => m.uid === event.userId);
                  const authorName = author?.displayName || author?.email || 'Usuario';
                  
                  return (
                    <div key={event.id} className="text-xs bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 border border-gray-100 dark:border-gray-700 rounded-lg p-2 text-gray-600 dark:text-gray-300">
                      <div className="font-medium text-gray-800 dark:text-gray-100 mb-1">{authorName} <span className="text-gray-400 font-normal">({new Date(event.timestamp).toLocaleString()})</span></div>
                      {event.type === 'STATUS_CHANGE' && (
                        <div>
                          <strong>Movimiento:</strong> de <em>{columns.find(c => c.id === event.from)?.name || event.from}</em> a <em>{columns.find(c => c.id === event.to)?.name || event.to}</em> 
                        </div>
                      )}
                      {event.details && (
                        <div className="text-gray-600 dark:text-gray-300">{event.details}</div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </form>

        <div className="p-4 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 flex justify-between gap-3">
          <div>
            {task && onClone && (
              <Button type="button" variant="outline" onClick={handleClone} disabled={isSubmitting} className="flex items-center gap-2">
                <Copy className="w-4 h-4" /> Clonar Tarea
              </Button>
            )}
          </div>
          <div className="flex gap-3">
            <Button type="button" variant="ghost" onClick={onClose} disabled={isSubmitting}>Cancelar</Button>
            <Button form="task-form" type="submit" isLoading={isSubmitting}>
              {task ? 'Guardar Cambios' : 'Crear Tarea'}
            </Button>
          </div>
        </div>
      </motion.div>
    </motion.div>
  );
}
