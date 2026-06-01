import { useState, useEffect } from 'react';
import { Layout, Plus, Calendar, ChevronRight, BarChart } from 'lucide-react';
import { dbService } from '../../services/databaseService';
import { Project, ProjectStatus } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { motion, AnimatePresence } from 'framer-motion';
import { auth } from '../../lib/firebase';
import { ProjectSettingsModal } from './ProjectSettingsModal';

import { ProjectDetailView } from './ProjectDetailView';
import { ProjectManagementFacade } from '../../lib/designPatterns';

export function ProjectsView({ userRole, onSelectProject }: { userRole?: string, onSelectProject: (p: Project) => void }) {
  const [projectsData, setProjectsData] = useState<{ project: Project, progress: number }[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  // Form
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadProjects();
  }, []);

  const loadProjects = async () => {
    setLoading(true);
    const data = await dbService.getProjects();
    setProjectsData(data);
    setLoading(false);
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!auth.currentUser) return;
    setSaving(true);
    try {
      const facade = new ProjectManagementFacade();
      await facade.scaffoldNewProject(name, description, startDate, endDate);
      setShowCreate(false);
      setName('');
      setDescription('');
      setStartDate('');
      setEndDate('');
      await loadProjects();
    } catch (err) {
      console.error(err);
      alert('Error creando proyecto');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="w-full relative">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-xl font-bold text-gray-900 dark:text-gray-50">Mis Proyectos</h2>
        <Button onClick={() => setShowCreate(!showCreate)} className="gap-2">
          {showCreate ? <Layout className="w-4 h-4" /> : <Plus className="w-4 h-4" />}
          {showCreate ? 'Ver Proyectos' : 'Nuevo Proyecto'}
        </Button>
      </div>

      <AnimatePresence mode="wait">
        {showCreate ? (
          <motion.div
            key="create"
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            className="bg-white dark:bg-gray-800 dark:text-gray-100 p-6 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-700"
          >
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50 mb-4">Crear Proyecto</h3>
            <form onSubmit={handleCreate} className="space-y-4">
              <Input
                label="Nombre del proyecto"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
              <Input
                label="Descripción"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <Input 
                  type="date"
                  label="Fecha de inicio" 
                  value={startDate} 
                  onChange={e => setStartDate(e.target.value)} 
                />
                <Input 
                  type="date"
                  label="Fecha estimada de fin" 
                  value={endDate} 
                  onChange={e => setEndDate(e.target.value)} 
                />
              </div>
              <div className="flex justify-end gap-3 pt-4 border-t border-gray-100 dark:border-gray-700">
                <Button type="button" variant="ghost" onClick={() => setShowCreate(false)}>
                  Cancelar
                </Button>
                <Button type="submit" isLoading={saving}>
                  Crear Proyecto
                </Button>
              </div>
            </form>
          </motion.div>
        ) : (
          <motion.div
            key="list"
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            className="grid grid-cols-1 md:grid-cols-2 gap-4"
          >
            {loading ? (
              <div className="col-span-full py-12 flex justify-center">
                <div className="w-8 h-8 border-4 border-blue-600/30 border-t-blue-600 rounded-full animate-spin" />
              </div>
            ) : projectsData.length === 0 ? (
              <div className="col-span-full py-12 text-center bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 rounded-2xl border border-dashed border-gray-200 dark:border-gray-700">
                <Layout className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                <h3 className="text-gray-900 dark:text-gray-50 font-medium">No hay proyectos</h3>
                <p className="text-gray-500 dark:text-gray-400 text-sm mt-1">Crea tu primer proyecto para empezar a trabajar.</p>
              </div>
            ) : (
              projectsData.map(({ project, progress }) => (
                <div key={project.id} className={`group flex flex-col bg-white dark:bg-gray-800 dark:text-gray-100 p-5 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-700 hover:shadow-md hover:border-blue-100 transition-all cursor-pointer ${project.status === 'ARCHIVADO' ? 'opacity-60 saturate-50' : ''}`} onClick={() => onSelectProject(project)}>
                  <div className="flex justify-between items-start mb-3">
                    <div>
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="text-lg font-bold text-gray-900 dark:text-gray-50 group-hover:text-blue-600 transition-colors">{project.name}</h3>
                      </div>
                      <div className="flex items-center gap-2 mb-2">
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full uppercase tracking-wider
                          ${project.status === 'ARCHIVADO' ? 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400' :
                            project.status === 'CANCELADO' ? 'bg-red-100 text-red-700' :
                            project.status === 'COMPLETADO' ? 'bg-green-100 text-green-700' :
                            project.status === 'ACTIVO' ? 'bg-blue-100 text-blue-700' :
                            project.status === 'EN_PAUSA' ? 'bg-yellow-100 text-yellow-700' :
                            'bg-purple-100 text-purple-700'
                          }
                        `}>
                          {project.status?.replace('_', ' ') || 'ACTIVO'}
                        </span>
                      </div>
                      <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2">{project.description || 'Sin descripción'}</p>
                    </div>
                  </div>
                  
                  <div className="mt-auto pt-4 space-y-4">
                    <div className="space-y-1.5">
                      <div className="flex justify-between text-xs font-medium">
                        <span className="text-gray-500 dark:text-gray-400 flex items-center gap-1"><BarChart className="w-3.5 h-3.5"/> Progreso</span>
                        <span className={progress === 100 ? 'text-green-600' : 'text-blue-600'}>{progress}%</span>
                      </div>
                      <div className="h-2 w-full bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                        <div 
                          className={`h-full rounded-full transition-all duration-500 ${progress === 100 ? 'bg-green-500' : 'bg-blue-600'}`} 
                          style={{ width: `${progress}%` }}
                        />
                      </div>
                    </div>

                    <div className="flex items-center justify-between pt-3 border-t border-gray-50">
                      <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
                        <Calendar className="w-3.5 h-3.5" />
                        <span>{project.startDate ? new Date(project.startDate).toLocaleDateString() : 'N/A'} - {project.endDate ? new Date(project.endDate).toLocaleDateString() : 'N/A'}</span>
                      </div>
                      <div className="text-sm text-gray-400 group-hover:text-blue-600 transition-colors">
                        <ChevronRight className="w-4 h-4" />
                      </div>
                    </div>
                  </div>
                </div>
              ))
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
