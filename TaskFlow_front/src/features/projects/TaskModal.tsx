import React, { useState, useEffect } from 'react';
import { Task, TaskLabel, BoardColumn } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { X, Tag, Copy } from 'lucide-react';
import { motion } from 'framer-motion';
import { dbService } from '../../services/databaseService';
import { TaskFactory, TaskPrototype } from '../../lib/designPatterns';
import { useCatalog } from '../../hooks/useCatalog';

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
  const [priority, setPriority] = useState<Task['priority']>(task?.priority || 'MEDIUM');
  const [type, setType] = useState<Task['type']>(task?.type?.toUpperCase() || 'TASK');
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

  // Catálogos del backend (BaseCatalogController) — sustituyen los <option>
  // hardcoded para que las opciones reflejen el estado real de la BD.
  const { items: taskTypes } = useCatalog('task-types');
  const { items: taskPriorities } = useCatalog('task-priorities');

  useEffect(() => {
    dbService
      .getProjectMembers(projectId)
      .then((m) => setMembers(m.map((x) => ({ uid: x.uid, email: x.email }))))
      .catch(console.error);
  }, [projectId]);

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
                {taskTypes.length === 0 && (
                  <option value={type}>Cargando…</option>
                )}
                {taskTypes.map((t) => (
                  <option key={t.id} value={t.name.toUpperCase()}>
                    {t.name}
                  </option>
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
                {taskPriorities.length === 0 && (
                  <option value={priority}>Cargando…</option>
                )}
                {taskPriorities.map((p) => (
                  <option key={p.id} value={p.name.toUpperCase()}>
                    {p.name}
                  </option>
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
                <span className="text-xs text-gray-400">Sin miembros disponibles</span>
              )}
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
