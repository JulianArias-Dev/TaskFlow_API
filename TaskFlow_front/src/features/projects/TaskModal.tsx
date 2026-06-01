import React, { useState, useEffect } from 'react';
import { Task, TaskLabel, BoardColumn, AttachmentItem, CommentItem } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { X, Tag, Copy, Paperclip, MessageSquare, Send, Plus } from 'lucide-react';
import { motion } from 'framer-motion';
import { dbService } from '../../services/databaseService';
import { TaskFactory, TaskPrototype } from '../../lib/designPatterns';
import { useCatalog } from '../../hooks/useCatalog';
import { auth } from '../../lib/firebase';
import { SERVER_BASE_URL } from '../../lib/api';

const PREDEFINED_COLORS = [
  '#ef4444', '#f97316', '#f59e0b', '#10b981',
  '#3b82f6', '#6366f1', '#8b5cf6', '#ec4899', '#64748b',
];

interface TaskModalProps {
  task?: Task | null;
  projectId: string;
  boardId: string;
  columns: BoardColumn[];
  statusId: string;
  onClose: () => void;
  onSave: (taskData: Partial<Task>) => Promise<void>;
  onClone?: (taskData: Partial<Task>) => Promise<void>;
}

export function TaskModal({ task, projectId, boardId, columns, statusId, onClose, onSave, onClone }: TaskModalProps) {
  const [title, setTitle] = useState(task?.title || '');
  const [description, setDescription] = useState(task?.description || '');
  const [priority, setPriority] = useState<Task['priority']>(task?.priority || 'Media');
  const [type, setType] = useState<Task['type']>(task?.type || 'Tarea');
  const [dueDate, setDueDate] = useState(task?.dueDate ? task?.dueDate.split('T')[0] : '');
  const [estimatedHours, setEstimatedHours] = useState(task?.estimatedHours?.toString() || '');
  const [loggedHours, setLoggedHours] = useState(task?.loggedHours?.toString() || '');
  const [labels, setLabels] = useState<TaskLabel[]>(task?.labels || []);
  const [assigneeIds, setAssigneeIds] = useState<string[]>(task?.assigneeIds || []);
  const [currentStatus, setCurrentStatus] = useState(task?.status || statusId);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showLabelInput, setShowLabelInput] = useState(false);
  const [newLabelName, setNewLabelName] = useState('');
  const [newLabelColor, setNewLabelColor] = useState(PREDEFINED_COLORS[0]);

  const [members, setMembers] = useState<{ uid: string; email: string; displayName?: string }[]>([]);

  // --- Subtareas ---
  const [subTasks, setSubTasks] = useState<Task[]>([]);
  const [showSubTaskInput, setShowSubTaskInput] = useState(false);
  const [newSubTaskTitle, setNewSubTaskTitle] = useState('');
  const [subTaskProgress, setSubTaskProgress] = useState(0);

  // --- Adjuntos ---
  const [attachments, setAttachments] = useState<AttachmentItem[]>([]);
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [loadingAttachments, setLoadingAttachments] = useState(false);

  // --- Comentarios ---
  const [comments, setComments] = useState<CommentItem[]>([]);
  const [loadingComments, setLoadingComments] = useState(false);
  const [newComment, setNewComment] = useState('');
  const [submittingComment, setSubmittingComment] = useState(false);
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingContent, setEditingContent] = useState('');

  const { items: taskTypes } = useCatalog('task-types');
  const { items: taskPriorities } = useCatalog('task-priorities');

  useEffect(() => {
    dbService
      .getProjectMembers(projectId)
      .then((m) => setMembers(m.map((x) => ({ uid: x.uid, email: x.email }))))
      .catch(console.error);
  }, [projectId]);

  // Cargar adjuntos y comentarios solo en modo edición
  useEffect(() => {
    if (!task?.id) return;

    setLoadingAttachments(true);
    dbService
      .getAttachments(task.id)
      .then(setAttachments)
      .catch(console.error)
      .finally(() => setLoadingAttachments(false));

    setLoadingComments(true);
    dbService
      .getComments(task.id)
      .then(setComments)
      .catch(console.error)
      .finally(() => setLoadingComments(false));
  }, [task?.id]);

  useEffect(() => {
    if (task?.id) {
      dbService.getSubTasks(task.id, projectId).then((subs) => {
        setSubTasks(subs);
        if (subs.length > 0) {
          const doneColumnIds = columns.filter(c => c.name.toLowerCase() === 'done' || c.name.toLowerCase() === 'finalizado').map(c => c.id);
          const done = subs.filter(st => doneColumnIds.includes(st.status)).length;
          setSubTaskProgress(Math.round((done / subs.length) * 100));
        }
      });
    }
  }, [task?.id, columns]);

  const handleAddSubTask = async () => {
    if (!newSubTaskTitle.trim() || !task?.id) return;
    try {
      await dbService.createTask(projectId, {
        boardId,
        status: currentStatus,
        title: newSubTaskTitle.trim(),
        description: '',
        priority: 'Media',
        type: 'Tarea',
        assigneeIds: [],
        parentTaskId: task.id,
      }as any);
      const st = await dbService.getSubTasks(task.id, projectId);
      setSubTasks(st);
      setNewSubTaskTitle('');
      setShowSubTaskInput(false);
    } catch (error) {
      console.error(error);
      alert('Error al crear la subtarea');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    setIsSubmitting(true);

    try {
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
      };

      const saveData = task
        ? { ...baseSaveData, type }
        : TaskFactory.createTask(type, baseSaveData);

      await onSave(saveData);

      // Subir archivos pendientes — solo si la tarea ya existe (edición)
      if (task?.id && pendingFiles.length > 0) {
        await Promise.all(
          pendingFiles.map((f) => dbService.uploadAttachment(task.id, f))
        );
      }

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
      const now = new Date().toISOString();
      const sourceData: Task = {
        id: task?.id || `temp-${Date.now()}`,
        boardId,
        status: currentStatus,
        title: title.trim(),
        description: description.trim(),
        priority,
        type,
        dueDate: dueDate || null,
        estimatedHours: estimatedHours ? parseFloat(estimatedHours) : undefined,
        loggedHours: loggedHours ? parseFloat(loggedHours) : undefined,
        labels: labels.length > 0 ? labels : undefined,
        assigneeIds,
        projectId,
        createdAt: task?.createdAt || now,
        updatedAt: now,
        fileCount: 0,
        parentTaskId: null,
        subTaskCount: 0,
      };

      const prototype = new TaskPrototype(sourceData);
      const clonedTask = prototype.clone();
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
      setAssigneeIds(assigneeIds.filter((id) => id !== uid));
    } else {
      setAssigneeIds([...assigneeIds, uid]);
    }
  };

  // --- Handlers de comentarios ---
  const handleAddComment = async () => {
    if (!newComment.trim() || !task?.id || !auth.currentUser) return;
    setSubmittingComment(true);
    try {
      const created = await dbService.createComment(task.id, newComment.trim(), auth.currentUser.uid);
      setComments((prev) => [...prev, created]);
      setNewComment('');
    } catch (error) {
      console.error(error);
      alert('Error al agregar comentario');
    } finally {
      setSubmittingComment(false);
    }
  };

  const handleDeleteComment = async (commentId: string) => {
    try {
      await dbService.deleteComment(commentId);
      setComments((prev) => prev.filter((c) => c.id !== commentId));
    } catch (error) {
      console.error(error);
      alert('Error al eliminar comentario');
    }
  };

  const handleUpdateComment = async (commentId: string) => {
    if (!editingContent.trim()) return;
    try {
      const updated = await dbService.updateComment(commentId, editingContent.trim());
      setComments((prev) => prev.map((c) => (c.id === commentId ? updated : c)));
      setEditingCommentId(null);
      setEditingContent('');
    } catch (error) {
      console.error(error);
      alert('Error al editar comentario');
    }
  };

  const isImage = (mimeType: string) => mimeType.startsWith('image/');

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
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Título de la tarea"
            required
            autoFocus
          />

          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Descripción</label>
            <textarea
              className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 min-h-[100px] resize-y"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Descripción detallada de la tarea..."
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Estado (Columna)</label>
              <select
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={currentStatus}
                onChange={(e) => setCurrentStatus(e.target.value)}
              >
                {columns.map((col) => (
                  <option key={col.id} value={col.id}>{col.name}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Tipo</label>
              <select
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={type}
                onChange={(e) => setType(e.target.value as Task['type'])}
              >
                {taskTypes.length === 0 && <option value={type}>Cargando…</option>}
                {taskTypes.map((t) => (
                  <option key={t.id} value={t.name.toUpperCase()}>{t.name}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Prioridad</label>
              <select
                className="w-full px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={priority}
                onChange={(e) => setPriority(e.target.value as Task['priority'])}
              >
                {taskPriorities.length === 0 && <option value={priority}>Cargando…</option>}
                {taskPriorities.map((p) => (
                  <option key={p.id} value={p.name.toUpperCase()}>{p.name}</option>
                ))}
              </select>
            </div>

            <Input
              label="Fecha de Vencimiento"
              type="date"
              value={dueDate ?? ''}
              onChange={(e) => setDueDate(e.target.value)}
            />

            <Input
              label="Estimación (Horas)"
              type="number"
              min="0"
              step="0.5"
              value={estimatedHours}
              onChange={(e) => setEstimatedHours(e.target.value)}
            />

            <Input
              label="Horas Trabajadas"
              type="number"
              min="0"
              step="0.5"
              value={loggedHours}
              onChange={(e) => setLoggedHours(e.target.value)}
            />
          </div>

          {/* Responsables */}
          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">Responsables</label>
            <div className="flex flex-wrap gap-2">
              {members.map((member) => (
                <button
                  key={member.uid}
                  type="button"
                  onClick={() => toggleAssignee(member.uid)}
                  className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors border ${
                    assigneeIds.includes(member.uid)
                      ? 'bg-blue-100 border-blue-200 text-blue-800'
                      : 'bg-white dark:bg-gray-800 dark:text-gray-100 border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-900'
                  }`}
                >
                  {member.displayName || member.email}
                </button>
              ))}
              {members.length === 0 && (
                <span className="text-xs text-gray-400">
                  Sin miembros — agrega miembros al proyecto desde Ajustes
                </span>
              )}
            </div>
          </div>

          {/* Etiquetas */}
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
              <div className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-900 flex flex-col gap-3">
                <Input
                  label="Nombre de la etiqueta"
                  value={newLabelName}
                  onChange={(e) => setNewLabelName(e.target.value)}
                  placeholder="Ej. Frontend"
                />
                <div>
                  <label className="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Color</label>
                  <div className="flex gap-2">
                    {PREDEFINED_COLORS.map((color) => (
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

          {/* ── Adjuntos ── */}
          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 flex items-center gap-2">
              <Paperclip className="w-4 h-4" /> Archivos adjuntos
            </label>

            {task?.id && (
              loadingAttachments ? (
                <p className="text-xs text-gray-400">Cargando adjuntos...</p>
              ) : (
                <div className="flex flex-col gap-2">
                  {attachments.map((att) => (
                    <div key={att.id} className="flex items-center gap-3 px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-700">
                      {isImage(att.mimeType) ? (
                        <a href={`${SERVER_BASE_URL}/api/Attachments/${att.id}/download`} target="_blank" rel="noopener noreferrer" className="flex-shrink-0">
                          <img
                            src={`${SERVER_BASE_URL}/api/Attachments/${att.id}/download`}
                            alt={att.fileName}
                            className="w-12 h-12 object-cover rounded-md border border-gray-200 dark:border-gray-600 hover:opacity-80 transition-opacity"
                          />
                        </a>
                      ) : (
                        <div className="w-12 h-12 flex items-center justify-center rounded-md border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 flex-shrink-0">
                          <Paperclip className="w-5 h-5 text-gray-400" />
                        </div>
                      )}
                      <a
                        href={`${SERVER_BASE_URL}/api/Attachments/${att.id}/download`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-sm text-blue-600 hover:underline truncate flex-1"
                      >
                        {att.fileName}
                      </a>
                      <button
                        type="button"
                        onClick={async () => {
                          await dbService.deleteAttachment(att.id);
                          setAttachments((prev) => prev.filter((a) => a.id !== att.id));
                        }}
                        className="text-gray-400 hover:text-red-500 flex-shrink-0"
                      >
                        <X className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  ))}
                </div>
              )
            )}

            {pendingFiles.length > 0 && (
              <div className="flex flex-col gap-1">
                {pendingFiles.map((f, i) => (
                  <div key={i} className="flex items-center justify-between px-3 py-1.5 rounded-lg border border-blue-100 bg-blue-50 dark:bg-blue-900/20 dark:border-blue-800 text-sm">
                    <span className="text-blue-700 dark:text-blue-300 truncate">{f.name}</span>
                    <button
                      type="button"
                      onClick={() => setPendingFiles((prev) => prev.filter((_, idx) => idx !== i))}
                      className="ml-2 text-blue-400 hover:text-red-500 flex-shrink-0"
                    >
                      <X className="w-3.5 h-3.5" />
                    </button>
                  </div>
                ))}
              </div>
            )}

            {task?.id ? (
              <label className="cursor-pointer inline-flex items-center gap-2 px-3 py-1.5 rounded-lg border border-dashed border-gray-300 dark:border-gray-600 text-sm text-gray-500 dark:text-gray-400 hover:border-blue-400 hover:text-blue-500 transition-colors">
                <Paperclip className="w-4 h-4" />
                Adjuntar archivo (máx. 10 MB)
                <input
                  type="file"
                  className="hidden"
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    if (file) setPendingFiles((prev) => [...prev, file]);
                    e.target.value = '';
                  }}
                />
              </label>
            ) : (
              <p className="text-xs text-gray-400 italic">
                Guarda la tarea primero para poder adjuntar archivos.
              </p>
            )}
          </div>

          {/* ── Comentarios ── */}
          {task?.id && (
            <div className="space-y-3">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 flex items-center gap-2">
                <MessageSquare className="w-4 h-4" /> Comentarios
              </label>

              {loadingComments ? (
                <p className="text-xs text-gray-400">Cargando comentarios...</p>
              ) : (
                <div className="flex flex-col gap-3">
                  {comments.length === 0 && (
                    <p className="text-xs text-gray-400">Sin comentarios aún.</p>
                  )}
                  {comments.map((c) => (
                    <div key={c.id} className="flex gap-3">
                      <div className="w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-xs font-bold text-blue-700 dark:text-blue-300 flex-shrink-0 overflow-hidden">
                        {c.userAvatar
                          ? <img src={c.userAvatar} alt={c.userName} className="w-8 h-8 object-cover" />
                          : c.userName?.substring(0, 2).toUpperCase()
                        }
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1 flex-wrap">
                          <span className="text-xs font-medium text-gray-700 dark:text-gray-200">{c.userName}</span>
                          <span className="text-xs text-gray-400">
                            {new Date(c.createdAt).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                          </span>
                          {c.isEdited && <span className="text-xs text-gray-400 italic">(editado)</span>}
                        </div>

                        {editingCommentId === c.id ? (
                          <div className="flex gap-2">
                            <textarea
                              className="flex-1 px-2 py-1 text-sm border border-gray-200 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                              rows={2}
                              value={editingContent}
                              onChange={(e) => setEditingContent(e.target.value)}
                            />
                            <div className="flex flex-col gap-1">
                              <Button type="button" className="text-xs py-1 h-auto" onClick={() => handleUpdateComment(c.id)}>
                                Guardar
                              </Button>
                              <Button type="button" variant="ghost" className="text-xs py-1 h-auto" onClick={() => setEditingCommentId(null)}>
                                Cancelar
                              </Button>
                            </div>
                          </div>
                        ) : (
                          <p className="text-sm text-gray-700 dark:text-gray-200 whitespace-pre-wrap break-words">{c.content}</p>
                        )}

                        {auth.currentUser?.uid === c.userId && editingCommentId !== c.id && (
                          <div className="flex gap-2 mt-1">
                            <button
                              type="button"
                              onClick={() => { setEditingCommentId(c.id); setEditingContent(c.content); }}
                              className="text-xs text-gray-400 hover:text-blue-500"
                            >
                              Editar
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDeleteComment(c.id)}
                              className="text-xs text-gray-400 hover:text-red-500"
                            >
                              Eliminar
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <div className="flex gap-2 mt-2">
                <textarea
                  className="flex-1 px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                  rows={2}
                  placeholder="Escribe un comentario..."
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) handleAddComment();
                  }}
                />
                <button
                  type="button"
                  onClick={handleAddComment}
                  disabled={!newComment.trim() || submittingComment}
                  className="px-3 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex-shrink-0 self-end"
                >
                  <Send className="w-4 h-4" />
                </button>
              </div>
              <p className="text-xs text-gray-400">Ctrl+Enter para enviar</p>
            </div>
          )}

          {/* ── Subtareas ── */}
          {task && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
                  Subtareas {subTasks.length > 0 && `(${subTaskProgress}% completado)`}
                </label>
                <Button type="button" variant="outline" className="text-sm py-1 h-auto"
                  onClick={() => setShowSubTaskInput(true)}>
                  <Plus className="w-4 h-4 mr-1" /> Añadir
                </Button>
              </div>

              {subTasks.length > 0 && (
                <div className="w-full bg-gray-200 rounded-full h-1.5">
                  <div className="bg-blue-500 h-1.5 rounded-full transition-all"
                    style={{ width: `${subTaskProgress}%` }} />
                </div>
              )}

              <div className="space-y-1">
                {subTasks.map((st) => (
                  <div key={st.id} className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-900 rounded-lg text-sm">
                    <span className="flex-1 text-gray-700 dark:text-gray-200">{st.title}</span>
                    <span className="text-xs text-gray-400">{st.type}</span>
                  </div>
                ))}
              </div>

              {showSubTaskInput && (
                <div className="flex gap-2">
                  <input
                    className="flex-1 px-3 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Título de la subtarea..."
                    value={newSubTaskTitle}
                    onChange={(e) => setNewSubTaskTitle(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleAddSubTask()}
                    autoFocus
                  />
                  <Button type="button" className="text-sm" onClick={handleAddSubTask}>Añadir</Button>
                  <Button type="button" variant="ghost" className="text-sm"
                    onClick={() => setShowSubTaskInput(false)}>Cancelar</Button>
                </div>
              )}
            </div>
          )}
        </form>

        <div className="p-4 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 flex justify-between gap-3">
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