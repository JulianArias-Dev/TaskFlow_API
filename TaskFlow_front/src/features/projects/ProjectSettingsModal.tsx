import { useState, useEffect } from 'react';
import { Project, ProjectStatus } from '../../types/models';
import { dbService } from '../../services/databaseService';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { motion } from 'framer-motion';
import { X, Trash2, Users, Save, Mail, ShieldAlert } from 'lucide-react';
import { auth } from '../../lib/firebase';
import { useCatalog } from '../../hooks/useCatalog';

interface Props {
  project: Project;
  userRole?: string;
  onClose: () => void;
  onUpdate: () => void;
}

export function ProjectSettingsModal({ project, userRole, onClose, onUpdate }: Props) {
  const [activeTab, setActiveTab] = useState<'general' | 'members'>('general');
  const [name, setName] = useState(project.name);
  const [description, setDescription] = useState(project.description || '');
  const [startDate, setStartDate] = useState(project.startDate || '');
  const [endDate, setEndDate] = useState(project.endDate || '');
  const [status, setStatus] = useState(project.status);
  const [saving, setSaving] = useState(false);
  
  const [members, setMembers] = useState<any[]>([]);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('');
  const [inviting, setInviting] = useState(false);

  // Catálogos del backend
  const { items: projectStatuses } = useCatalog('project-status');
  const { items: projectRoles } = useCatalog('project-roles');

  // Cuando carga el catálogo de roles, dejamos seleccionado el primero (sin
  // permisos elevados) por defecto.
  useEffect(() => {
    if (!inviteRole && projectRoles.length > 0) {
      setInviteRole(projectRoles[projectRoles.length - 1].name);
    }
  }, [projectRoles, inviteRole]);
  
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [cloneName, setCloneName] = useState('');
  const [showClonePrompt, setShowClonePrompt] = useState(false);

  const isOwnerOrAdmin = auth.currentUser?.uid === project.ownerId || userRole === 'ADMIN';

  useEffect(() => {
    if (activeTab === 'members') {
      loadMembers();
    }
  }, [activeTab]);

  const loadMembers = async () => {
    const m = await dbService.getProjectMembers(project.id);
    setMembers(m);
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isOwnerOrAdmin) return;
    setSaving(true);
    await dbService.updateProject(project.id, { name, description, startDate, endDate, status });
    setSaving(false);
    onUpdate();
    onClose();
  };

  const handleDelete = async (e?: React.MouseEvent) => {
    if (e) e.preventDefault();
    if (!isOwnerOrAdmin) return;
    try {
      await dbService.deleteProject(project.id);
      onUpdate();
      onClose();
    } catch (err: any) {
      console.error(err);
      alert("No se pudo eliminar: " + (err.message || 'Error desconocido'));
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inviteEmail) return;
    setInviting(true);
    try {
      // Convertimos el name seleccionado al Id que espera el backend.
      const roleId = projectRoles.find((r) => r.name === inviteRole)?.id;
      const ok = await dbService.addProjectMember(project.id, inviteEmail, roleId);
      if (ok) {
        setInviteEmail('');
        await loadMembers();
      } else {
        alert('Error: usuario no registrado o sin permisos');
      }
    } catch (err: any) {
      alert(err?.message || 'Error al invitar al usuario');
    } finally {
      setInviting(false);
    }
  };

  return (
    <motion.div 
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
    >
      <motion.div 
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="bg-white dark:bg-gray-800 dark:text-gray-100 rounded-2xl w-full max-w-2xl max-h-[90vh] flex flex-col shadow-2xl"
      >
        <div className="flex justify-between items-center p-6 border-b border-gray-100 dark:border-gray-700">
          <div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-50">{project.name}</h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">Configuración del proyecto</p>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:bg-gray-700 rounded-full text-gray-400 transition-colors">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="flex border-b border-gray-100 dark:border-gray-700 bg-gray-50/50 px-6">
          <button 
            onClick={() => setActiveTab('general')}
            className={`py-3 px-4 text-sm font-medium border-b-2 transition-colors ${activeTab === 'general' ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:text-gray-200'}`}
          >
            General
          </button>
          <button 
            onClick={() => setActiveTab('members')}
            className={`py-3 px-4 text-sm font-medium border-b-2 transition-colors ${activeTab === 'members' ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:text-gray-200'}`}
          >
            Miembros
          </button>
        </div>

        <div className="p-6 overflow-y-auto flex-1">
          {activeTab === 'general' ? (
            <div className="space-y-6">
              <form id="project-form" onSubmit={handleUpdate} className="space-y-4">
                <Input 
                  label="Nombre del proyecto" 
                  value={name} 
                  onChange={e => setName(e.target.value)} 
                  disabled={!isOwnerOrAdmin}
                  required 
                />
                <Input 
                  label="Descripción" 
                  value={description} 
                  onChange={e => setDescription(e.target.value)} 
                  disabled={!isOwnerOrAdmin}
                />
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <Input 
                    type="date"
                    label="Fecha de inicio" 
                    value={startDate} 
                    onChange={e => setStartDate(e.target.value)} 
                    disabled={!isOwnerOrAdmin}
                  />
                  <Input 
                    type="date"
                    label="Fecha estimada de fin" 
                    value={endDate} 
                    onChange={e => setEndDate(e.target.value)} 
                    disabled={!isOwnerOrAdmin}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Estado</label>
                  <select
                    value={status}
                    onChange={e => setStatus(e.target.value as ProjectStatus)}
                    disabled={!isOwnerOrAdmin}
                    className="w-full px-3 py-2 bg-white dark:bg-gray-800 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  >
                    {projectStatuses.length === 0 && (
                      <option value={status}>Cargando…</option>
                    )}
                    {projectStatuses.map((s) => (
                      <option key={s.id} value={s.name}>{s.name}</option>
                    ))}
                  </select>
                </div>
              </form>

              {isOwnerOrAdmin && (
                <div className="pt-6 border-t border-gray-100 dark:border-gray-700 flex flex-col gap-4">
                  <div>
                    <h3 className="text-gray-900 dark:text-gray-50 font-semibold mb-2 flex items-center gap-2">
                       Acciones rápidas
                    </h3>
                    
                    {!showClonePrompt ? (
                      <Button type="button" variant="outline" className="text-blue-600 border-blue-200 hover:bg-blue-50" onClick={() => {
                        setCloneName(`${project.name} (Copia)`);
                        setShowClonePrompt(true);
                      }}>
                        Clonar como plantilla
                      </Button>
                    ) : (
                      <div className="bg-blue-50 p-4 rounded-lg flex flex-col gap-3">
                        <label className="text-sm font-medium text-blue-900">Nombre del nuevo proyecto</label>
                        <Input 
                          value={cloneName} 
                          onChange={e => setCloneName(e.target.value)} 
                          placeholder="Nombre" 
                          autoFocus
                          className="bg-white dark:bg-gray-800 dark:text-gray-100"
                        />
                        <div className="flex gap-2">
                          <Button type="button" onClick={async () => {
                            if (!cloneName.trim()) return;
                            try {
                               await dbService.cloneProject(project.id, cloneName);
                               onUpdate();
                               onClose();
                            } catch(err: any) {
                               console.error(err);
                               alert("No se pudo clonar: " + (err.message || 'Error desconocido'));
                            }
                          }}>Confirmar</Button>
                          <Button type="button" variant="ghost" onClick={() => setShowClonePrompt(false)}>Cancelar</Button>
                        </div>
                      </div>
                    )}
                  </div>
                  
                  <div>
                    <h3 className="text-red-600 font-semibold mb-2 flex items-center gap-2">
                      <ShieldAlert className="w-4 h-4" /> Zona de peligro
                    </h3>
                    <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                      Al eliminar el proyecto se borrarán todos los tableros y tareas asociadas de forma permanente.
                    </p>
                    
                    {!showDeleteConfirm ? (
                      <Button type="button" variant="outline" className="text-red-600 border-red-200 hover:bg-red-50 hover:text-red-700" onClick={() => setShowDeleteConfirm(true)}>
                        <Trash2 className="w-4 h-4 mr-2" /> Eliminar Proyecto
                      </Button>
                    ) : (
                      <div className="bg-red-50 p-4 rounded-lg border border-red-200 flex flex-col gap-3">
                        <p className="text-red-800 text-sm font-medium">¿Seguro que deseas eliminar este proyecto y TODOS sus datos?</p>
                        <div className="flex gap-2">
                          <Button type="button" className="bg-red-600 hover:bg-red-700 text-white" onClick={handleDelete}>
                            Sí, Eliminar
                          </Button>
                          <Button type="button" variant="ghost" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-6">
              {isOwnerOrAdmin && (
                <form onSubmit={handleInvite} className="bg-blue-50 p-4 rounded-xl border border-blue-100">
                  <h3 className="text-sm font-semibold text-blue-900 mb-3 flex items-center gap-2">
                    <Users className="w-4 h-4" /> Invitar Miembro
                  </h3>
                  <div className="flex flex-col sm:flex-row gap-3">
                    <div className="flex-1">
                      <Input 
                        type="email" 
                        placeholder="correo@ejemplo.com" 
                        value={inviteEmail}
                        onChange={e => setInviteEmail(e.target.value)}
                        required
                        className="bg-white dark:bg-gray-800 dark:text-gray-100"
                      />
                    </div>
                    <select
                      value={inviteRole}
                      onChange={e => setInviteRole(e.target.value)}
                      className="px-3 py-2 bg-white dark:bg-gray-800 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    >
                      {projectRoles.length === 0 && (
                        <option value="">Cargando roles…</option>
                      )}
                      {projectRoles.map((r) => (
                        <option key={r.id} value={r.name}>{r.name}</option>
                      ))}
                    </select>
                    <Button type="submit" isLoading={inviting}>
                      <Mail className="w-4 h-4 mr-2" /> Invitar
                    </Button>
                  </div>
                </form>
              )}

              <div>
                <h3 className="text-sm font-medium text-gray-700 dark:text-gray-200 mb-3">Miembros Actuales ({members.length})</h3>
                <div className="space-y-2">
                  {members.map(m => (
                    <div key={m.uid} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 rounded-lg border border-gray-100 dark:border-gray-700">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold text-sm">
                          {m.email.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900 dark:text-gray-50">{m.email}</p>
                          <p className="text-xs text-gray-500 dark:text-gray-400 capitalize">{m.role.replace('_', ' ').toLowerCase()}</p>
                        </div>
                      </div>
                      {m.uid === project.ownerId && (
                        <span className="text-xs font-semibold bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-200 px-2 py-1 rounded">Propietario</span>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="p-4 border-t border-gray-100 dark:border-gray-700 flex justify-end gap-3 bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 rounded-b-2xl">
          <Button variant="ghost" onClick={onClose}>Cerrar</Button>
          {activeTab === 'general' && isOwnerOrAdmin && (
            <Button form="project-form" type="submit" isLoading={saving}>
              <Save className="w-4 h-4 mr-2" /> Guardar Cambios
            </Button>
          )}
        </div>
      </motion.div>
    </motion.div>
  );
}
