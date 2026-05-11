import { useState, useEffect } from 'react';
import type { AuthUser } from '../../lib/firebase';
import { Layout, User as UserIcon, Calendar, Edit2, Check, X, Briefcase, Settings, LogOut, Shield, ChevronLeft, BarChart2, Users, Upload, BookOpen } from 'lucide-react';
import { authService } from '../../services/authService';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { ProjectsView } from '../projects/ProjectsView';
import { NotificationsMenu } from '../notifications/NotificationsMenu';
import { AdminDashboard } from '../admin/AdminDashboard';
import { ProjectDetailView } from '../projects/ProjectDetailView';
import { PatternsView } from '../patterns/PatternsView';
import { Project } from '../../types/models';

export function ProfileDashboard({ user }: { user: AuthUser }) {
  const [token, setToken] = useState<string | null>(null);
  const [profile, setProfile] = useState<any>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [activeTab, setActiveTab] = useState<'projects' | 'profile' | 'admin' | 'patterns'>('projects');
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [projectTab, setProjectTab] = useState<'board' | 'dashboard' | 'settings'>('board');
  const [isCollapsed, setIsCollapsed] = useState(false);
  
  // Edit form state
  const [editName, setEditName] = useState('');
  const [editDesc, setEditDesc] = useState('');
  const [editPhoto, setEditPhoto] = useState('');
  const [editTheme, setEditTheme] = useState<'light' | 'dark'>('light');
  
  const [notificationPrefs, setNotificationPrefs] = useState<Record<string, { inApp: boolean; email: boolean }>>({
    ASSIGNED: { inApp: true, email: true },
    DUE_OVERDUE: { inApp: true, email: true },
    COMMENT: { inApp: true, email: true },
    STATUS_CHANGE: { inApp: true, email: true },
  });
  const [savingPrefs, setSavingPrefs] = useState(false);

  useEffect(() => {
    authService.getIdToken().then(setToken);
    loadProfile();
  }, [user.uid]);

  useEffect(() => {
    if (profile?.theme === 'dark') {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, [profile?.theme]);

  const loadProfile = async () => {
    setLoading(true);
    const data = await authService.getUserProfile(user.uid);
    if (data && data.isActive === false) {
      alert('Tu cuenta ha sido desactivada por un administrador.');
      authService.logout();
      return;
    }
    // Componer un perfil normalizado a partir del UserApi del backend + el AuthUser local.
    const profileShape = data
      ? {
          uid: data.id,
          email: data.email,
          displayName: data.name,
          photoURL: data.avatarUrl ?? user.photoURL ?? null,
          role: data.role,
          isActive: data.isActive,
          createdAt: data.createdAt,
          updatedAt: data.updatedAt,
          lastLoginAt: data.lastLoginAt,
          description: '',
          theme: 'light' as const,
        }
      : null;
    setProfile(profileShape);
    if (profileShape) {
      setEditName(profileShape.displayName || user.displayName || '');
      setEditDesc(profileShape.description || '');
      setEditPhoto(profileShape.photoURL || user.photoURL || '');
      setEditTheme(profileShape.theme);
    }

    // Cargar preferencias de notificación (RF-05.3). Si falla, dejamos los
    // defaults que ya están en el state inicial.
    try {
      const prefs = await authService.getNotificationPreferences();
      if (prefs && Object.keys(prefs).length > 0) {
        setNotificationPrefs((prev) => ({ ...prev, ...prefs }));
      }
    } catch (err) {
      console.warn('[ProfileDashboard] no se pudieron cargar preferencias:', err);
    }

    setLoading(false);
  };

  /**
   * Guarda las preferencias de notificación de forma independiente. El usuario
   * no necesita estar en "modo edición" para tocar los toggles — un cambio
   * se persiste inmediatamente.
   */
  const persistNotificationPrefs = async (next: typeof notificationPrefs) => {
    setNotificationPrefs(next);
    setSavingPrefs(true);
    try {
      const saved = await authService.updateNotificationPreferences(next);
      setNotificationPrefs((prev) => ({ ...prev, ...saved }));
    } catch (err) {
      console.error('Error guardando preferencias:', err);
      alert('No se pudieron guardar las preferencias de notificación.');
    } finally {
      setSavingPrefs(false);
    }
  };

  const handleUploadPhoto = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploadingPhoto(true);
    try {
      // TODO: subir el archivo a un endpoint del backend (p.ej. POST /Attachments/avatar).
      // De momento generamos una data URL local para previsualización.
      const newPhotoUrl: string = await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as string);
        reader.onerror = () => reject(new Error('No se pudo leer el archivo'));
        reader.readAsDataURL(file);
      });
      setEditPhoto(newPhotoUrl);
      
      // Optionally auto-save if we are not in edit mode
      if (!isEditing) {
         setSaving(true);
         await authService.updateUserProfile(profile?.displayName || user.displayName || '', profile?.description || '', newPhotoUrl);
         await loadProfile();
         setSaving(false);
      }

    } catch (error) {
      console.error('Error uploading photo:', error);
      alert('Error al subir la foto. ' + (error instanceof Error ? error.message : ""));
    } finally {
      setUploadingPhoto(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await authService.updateUserProfile(editName, editDesc, editPhoto);
      await authService.updateNotificationPreferences(notificationPrefs);
      await authService.updateUserTheme(editTheme);
      await loadProfile();
      setIsEditing(false);
    } catch (e) {
      console.error(e);
      alert('Error updating profile');
    } finally {
      setSaving(false);
    }
  };

  const formatDate = (timestamp: any) => {
    if (!timestamp) return 'Nunca';
    if (timestamp.toDate) return timestamp.toDate().toLocaleString('es-ES');
    return new Date(timestamp).toLocaleString('es-ES');
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-800 dark:bg-gray-900">
        <div className="w-8 h-8 border-4 border-blue-600/30 border-t-blue-600 rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 overflow-hidden">
      {/* Sidebar - Desktop */}
      <aside className={`hidden md:flex flex-col bg-white dark:bg-gray-800 dark:bg-gray-900 border-r border-gray-100 dark:border-gray-700 transition-all duration-300 ease-in-out ${isCollapsed ? 'w-20' : 'w-64'}`}>
        <div className="p-6 flex items-center justify-between border-b border-gray-100 dark:border-gray-700 h-16 min-h-[64px] relative">
          {!isCollapsed && (
            <div className="flex items-center gap-2 animate-in fade-in slide-in-from-left-2 duration-300">
              <div className="bg-blue-600 p-2 rounded-lg">
                <Layout className="w-5 h-5 text-white" />
              </div>
              <span className="font-bold text-gray-900 dark:text-gray-50 tracking-tight">TaskFlow</span>
            </div>
          )}
          {isCollapsed && (
            <div className="mx-auto bg-blue-600 p-2 rounded-lg animate-in fade-in zoom-in-95 duration-300">
              <Layout className="w-5 h-5 text-white" />
            </div>
          )}
          <button 
            onClick={() => setIsCollapsed(!isCollapsed)}
            className={`p-1.5 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-400 absolute transition-all duration-300 ${isCollapsed ? 'left-16' : 'right-2'}`}
          >
            <ChevronLeft className={`w-4 h-4 transition-transform duration-300 ${isCollapsed ? 'rotate-180' : ''}`} />
          </button>
        </div>
        <div className="p-4 flex-1 space-y-1 overflow-y-auto">
          {selectedProject && activeTab === 'projects' ? (
            <>
              <button 
                onClick={() => setSelectedProject(null)}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors mb-4 border-b border-gray-100 dark:border-gray-700 pb-4 ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Volver a Proyectos"
              >
                <ChevronLeft className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span className="truncate">Volver a Proyectos</span>}
              </button>
              
              {!isCollapsed && (
                <div className="px-3 mb-2 animate-in fade-in duration-300">
                  <p className="text-[10px] font-bold text-gray-400 uppercase tracking-widest">Proyecto Actual</p>
                  <p className="text-sm font-bold text-gray-900 dark:text-gray-50 truncate">{selectedProject.name}</p>
                </div>
              )}

              <button 
                onClick={() => setProjectTab('board')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${projectTab === 'board' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Tablero"
              >
                <Layout className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span>Tablero</span>}
              </button>
              <button 
                onClick={() => setProjectTab('dashboard')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${projectTab === 'dashboard' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Estadísticas"
              >
                <BarChart2 className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span>Estadísticas</span>}
              </button>
              <button 
                onClick={() => setProjectTab('settings')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${projectTab === 'settings' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Configuración"
              >
                <Settings className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span>Configuración</span>}
              </button>

              <div className="pt-4 mt-4 border-t border-gray-100 dark:border-gray-700 space-y-1">
                {!isCollapsed && <p className="px-3 mb-2 text-[10px] font-bold text-gray-400 uppercase tracking-widest animate-in fade-in duration-300">Navegación</p>}
                <button 
                  onClick={() => setActiveTab('profile')}
                  className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 ${isCollapsed ? 'justify-center px-2' : ''}`}
                  title="Mi Perfil"
                >
                  <UserIcon className="w-4 h-4 flex-shrink-0" />
                  {!isCollapsed && <span>Mi Perfil</span>}
                </button>
                
                {profile?.role?.toUpperCase() === 'ADMIN' && (
                  <button 
                    onClick={() => setActiveTab('admin')}
                    className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 ${isCollapsed ? 'justify-center px-2' : ''}`}
                    title="Administración"
                  >
                    <Shield className="w-4 h-4 flex-shrink-0" />
                    {!isCollapsed && <span>Administración</span>}
                  </button>
                )}
                <button 
                  onClick={() => setActiveTab('patterns')}
                  className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 ${isCollapsed ? 'justify-center px-2' : ''}`}
                  title="Patrones de Diseño"
                >
                  <BookOpen className="w-4 h-4 flex-shrink-0" />
                  {!isCollapsed && <span>Patrones de Diseño</span>}
                </button>
              </div>
            </>
          ) : (
            <>
              <button 
                onClick={() => setActiveTab('projects')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${activeTab === 'projects' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Proyectos"
              >
                <Briefcase className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span>Proyectos</span>}
              </button>
              <button 
                onClick={() => setActiveTab('profile')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${activeTab === 'profile' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Mi Perfil"
              >
                <UserIcon className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span>Mi Perfil</span>}
              </button>
              
              {profile?.role?.toUpperCase() === 'ADMIN' && (
                <button
                  onClick={() => setActiveTab('admin')}
                  className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${activeTab === 'admin' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                  title="Administración"
                >
                  <Shield className="w-4 h-4 flex-shrink-0" />
                  {!isCollapsed && <span>Administración</span>}
                </button>
              )}
              <button 
                onClick={() => setActiveTab('patterns')}
                className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${activeTab === 'patterns' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'} ${isCollapsed ? 'justify-center px-2' : ''}`}
                title="Patrones de Diseño"
              >
                <BookOpen className="w-4 h-4 flex-shrink-0" />
                {!isCollapsed && <span>Patrones de Diseño</span>}
              </button>
            </>
          )}
        </div>
        <div className="p-4 border-t border-gray-100 dark:border-gray-700">
          <button 
            onClick={() => authService.logout()} 
            className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors ${isCollapsed ? 'justify-center px-2' : ''}`}
            title="Cerrar Sesión"
          >
            <LogOut className="w-4 h-4 flex-shrink-0" />
            {!isCollapsed && <span>Cerrar Sesión</span>}
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col h-screen overflow-y-auto">
        {/* Desktop Header for Notifications */}
        <div className="hidden md:flex justify-end p-4 bg-white/80 backdrop-blur-md sticky top-0 z-10 border-b border-gray-100 dark:border-gray-700">
          <NotificationsMenu />
        </div>

        {/* Mobile Header */}
        <div className="md:hidden bg-white dark:bg-gray-800 dark:text-gray-100 border-b border-gray-200 dark:border-gray-700 p-4 flex justify-between items-center sticky top-0 z-10">
          <div className="flex items-center gap-2">
            <Layout className="w-5 h-5 text-blue-600" />
            <span className="font-bold text-gray-900 dark:text-gray-50">TaskFlow</span>
          </div>
          <div className="flex gap-2">
            <NotificationsMenu />
            {selectedProject && activeTab === 'projects' ? (
               <button onClick={() => setSelectedProject(null)} className="p-2 rounded-md text-gray-500">
                 <ChevronLeft className="w-5 h-5" />
               </button>
            ) : (
              <>
                <button onClick={() => setActiveTab('projects')} className={`p-2 rounded-md ${activeTab === 'projects' ? 'bg-blue-50 text-blue-600' : 'text-gray-600 dark:text-gray-300'}`}>
                  <Briefcase className="w-5 h-5" />
                </button>
                <button onClick={() => setActiveTab('profile')} className={`p-2 rounded-md ${activeTab === 'profile' ? 'bg-blue-50 text-blue-600' : 'text-gray-600 dark:text-gray-300'}`}>
                  <UserIcon className="w-5 h-5" />
                </button>
                {profile?.role?.toUpperCase() === 'ADMIN' && (
                  <button onClick={() => setActiveTab('admin')} className={`p-2 rounded-md ${activeTab === 'admin' ? 'bg-blue-50 text-blue-600' : 'text-gray-600 dark:text-gray-300'}`}>
                    <Shield className="w-5 h-5" />
                  </button>
                )}
                <button onClick={() => setActiveTab('patterns')} className={`p-2 rounded-md ${activeTab === 'patterns' ? 'bg-blue-50 text-blue-600' : 'text-gray-600 dark:text-gray-300'}`}>
                  <BookOpen className="w-5 h-5" />
                </button>
              </>
            )}
            <button onClick={() => authService.logout()} className="p-2 rounded-md text-red-600">
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className={`flex-1 ${selectedProject && activeTab === 'projects' ? 'p-0' : 'p-6 sm:p-10'} max-w-7xl mx-auto w-full`}>
          {user && profile && (!selectedProject || activeTab !== 'projects') && (
            <div className="flex items-center gap-2 mb-8 bg-blue-50 text-blue-700 px-4 py-2 rounded-lg text-sm font-medium border border-blue-100 inline-flex">
              <span className="font-bold uppercase tracking-wider">{profile.role || 'DEVELOPER'}</span>
              <span className="text-blue-300">•</span>
              <span>{profile.displayName || user.email}</span>
            </div>
          )}

          {activeTab === 'projects' ? (
            selectedProject ? (
              <ProjectDetailView 
                project={selectedProject} 
                userRole={profile?.role}
                initialTab={projectTab}
                onBack={() => setSelectedProject(null)} 
                onUpdate={() => {
                   // Refresh logic if needed, or rely on live listeners if any
                }}
              />
            ) : (
              <ProjectsView userRole={profile?.role} onSelectProject={setSelectedProject} />
            )
          ) : activeTab === 'admin' ? (
            <AdminDashboard />
          ) : activeTab === 'patterns' ? (
            <PatternsView />
          ) : (
            <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-8 sm:p-10 rounded-3xl shadow-sm border border-gray-200 dark:border-gray-700">
              {/* Header Profile Section */}
              <div className="flex flex-col sm:flex-row items-center sm:items-start gap-8 mb-10">
                <div className="shrink-0 relative group">
                  {profile?.photoURL || user.photoURL || editPhoto ? (
                    <img 
                      src={editPhoto || profile?.photoURL || user.photoURL} 
                      alt="Avatar" 
                      className="w-24 h-24 sm:w-32 sm:h-32 rounded-full object-cover border-4 border-blue-50 dark:border-blue-900/30 shadow-lg transition-all duration-300 group-hover:brightness-75"
                    />
                  ) : (
                    <div className="w-24 h-24 sm:w-32 sm:h-32 rounded-full bg-blue-100 dark:bg-blue-900/30 border-4 border-blue-50 dark:border-blue-900/30 shadow-lg flex items-center justify-center text-blue-600 dark:text-blue-400 transition-all duration-300 group-hover:brightness-75">
                      <UserIcon className="w-10 h-10 sm:w-16 sm:h-16" />
                    </div>
                  )}
                  
                  <label className="absolute inset-0 flex flex-col items-center justify-center bg-black/50 text-white rounded-full cursor-pointer opacity-0 group-hover:opacity-100 transition-all z-10 duration-300">
                     {uploadingPhoto ? (
                       <div className="w-6 h-6 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                     ) : (
                       <>
                         <Upload className="w-6 h-6 mb-1" />
                         <span className="text-[10px] font-medium tracking-wide uppercase">Subir Foto</span>
                       </>
                     )}
                     <input type="file" className="hidden" accept="image/*" onChange={handleUploadPhoto} disabled={uploadingPhoto} />
                  </label>
                  
                </div>
                
                <div className="flex-1 text-center sm:text-left w-full mt-2">
                  {isEditing ? (
                    <div className="space-y-5 animate-in fade-in slide-in-from-bottom-2 duration-300">
                      <div className="grid grid-cols-1 gap-5 bg-gray-50 dark:bg-gray-800 dark:bg-gray-900/50 p-6 rounded-2xl border border-gray-100 dark:border-gray-700">
                        <Input 
                          label="Nombre Completo"
                          value={editName}
                          placeholder="Tu nombre completo"
                          onChange={e => setEditName(e.target.value)}
                        />
                        <Input 
                          label="Biografía / Rol"
                          placeholder="Añade una biografía, rol, o descripción sobre ti..."
                          value={editDesc}
                          onChange={e => setEditDesc(e.target.value)}
                        />
                      </div>
                      
                      <div className="border-t border-gray-100 dark:border-gray-700 pt-6 mt-8">
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50 mb-4">Preferencias Visuales</h3>
                        <div className="flex gap-4">
                          <label className="flex items-center gap-2 cursor-pointer p-3 border border-gray-200 dark:border-gray-700 rounded-lg flex-1 hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900">
                            <input 
                              type="radio" 
                              name="theme" 
                              value="light"
                              checked={editTheme === 'light'}
                              onChange={() => setEditTheme('light')}
                              className="w-4 h-4 text-blue-600"
                            />
                            <span className="text-sm font-medium text-gray-700 dark:text-gray-200">Claro</span>
                          </label>
                          <label className="flex items-center gap-2 cursor-pointer p-3 border border-gray-200 dark:border-gray-700 rounded-lg flex-1 hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900">
                            <input 
                              type="radio" 
                              name="theme" 
                              value="dark"
                              checked={editTheme === 'dark'}
                              onChange={() => setEditTheme('dark')}
                              className="w-4 h-4 text-blue-600"
                            />
                            <span className="text-sm font-medium text-gray-700 dark:text-gray-200">Oscuro</span>
                          </label>
                        </div>
                      </div>

                      <div className="border-t border-gray-100 dark:border-gray-700 pt-6 mt-6">
                        <div className="flex items-center justify-between mb-4">
                          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50">Preferencias de Notificación</h3>
                          {('Notification' in window && Notification.permission !== 'granted') && (
                            <Button 
                              size="sm" 
                              variant="outline" 
                              onClick={() => {
                                Notification.requestPermission().then(() => {
                                  // Refresh to apply changes
                                  window.location.reload();
                                });
                              }}
                            >
                              Habilitar Notificaciones de Navegador
                            </Button>
                          )}
                        </div>
                        {savingPrefs && (
                          <p className="text-xs text-blue-600 mb-2">Guardando preferencias…</p>
                        )}
                        <div className="space-y-4">
                          {[
                            { key: 'ASSIGNED', label: 'Nuevas Asignaciones' },
                            { key: 'DUE_OVERDUE', label: 'Tareas Vencidas' },
                            { key: 'COMMENT', label: 'Nuevos Comentarios' },
                            { key: 'STATUS_CHANGE', label: 'Cambios de Estado' }
                          ].map(pref => (
                            <div key={pref.key} className="flex flex-col sm:flex-row sm:items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 rounded-lg border border-gray-100 dark:border-gray-700">
                              <span className="text-sm font-medium text-gray-700 dark:text-gray-200">{pref.label}</span>
                              <div className="flex gap-4 mt-2 sm:mt-0">
                                <label className="flex items-center gap-2 cursor-pointer">
                                  <input
                                    type="checkbox"
                                    className="w-4 h-4 text-blue-600 rounded border-gray-300 dark:border-gray-600 focus:ring-blue-500"
                                    checked={notificationPrefs[pref.key]?.inApp ?? true}
                                    onChange={(e) => persistNotificationPrefs({
                                      ...notificationPrefs,
                                      [pref.key]: { ...(notificationPrefs[pref.key] || { inApp: true, email: true }), inApp: e.target.checked }
                                    })}
                                  />
                                  <span className="text-sm text-gray-600 dark:text-gray-300">In-App</span>
                                </label>
                                <label className="flex items-center gap-2 cursor-pointer">
                                  <input
                                    type="checkbox"
                                    className="w-4 h-4 text-blue-600 rounded border-gray-300 dark:border-gray-600 focus:ring-blue-500"
                                    checked={notificationPrefs[pref.key]?.email ?? true}
                                    onChange={(e) => persistNotificationPrefs({
                                      ...notificationPrefs,
                                      [pref.key]: { ...(notificationPrefs[pref.key] || { inApp: true, email: true }), email: e.target.checked }
                                    })}
                                  />
                                  <span className="text-sm text-gray-600 dark:text-gray-300">Email</span>
                                </label>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>

                      <div className="flex gap-2 pt-4">
                        <Button onClick={handleSave} isLoading={saving} className="bg-green-600 hover:bg-green-700">
                          <Check className="w-4 h-4" /> Guardar
                        </Button>
                        <Button onClick={() => setIsEditing(false)} variant="outline">
                          <X className="w-4 h-4" /> Cancelar
                        </Button>
                      </div>
                    </div>
                  ) : (
                    <>
                      <div className="flex justify-between items-start">
                        <div>
                          <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-50 mb-2">
                            {profile?.displayName || user.displayName || 'Usuario'}
                          </h2>
                          <p className="text-gray-500 dark:text-gray-400 mb-6 max-w-xl text-base leading-relaxed">
                            {profile?.description || 'Sin biografía. ¡Haz clic en "Editar Perfil" para añadir más información sobre ti!'}
                          </p>
                        </div>
                        <Button
                          onClick={() => setIsEditing(true)}
                          variant="outline"
                          size="sm"
                          className="hidden sm:flex text-gray-600 dark:text-gray-300"
                        >
                          <Edit2 className="w-4 h-4 mr-2" /> Editar Perfil
                        </Button>
                      </div>

                      <div className="flex flex-col sm:flex-row gap-5 text-sm text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 p-4 rounded-xl border border-gray-100 dark:border-gray-700 w-fit">
                        <div className="flex items-center justify-center sm:justify-start gap-2">
                          <UserIcon className="w-4 h-4 text-gray-400" />
                          <span className="font-medium text-gray-700 dark:text-gray-200">{user.email}</span>
                        </div>
                        <div className="hidden sm:block w-px bg-gray-200 dark:bg-gray-700"></div>
                        <div className="flex items-center justify-center sm:justify-start gap-2">
                          <Calendar className="w-4 h-4 text-gray-400" />
                          <span>Último acceso: <span className="font-medium text-gray-700 dark:text-gray-200">{formatDate(profile?.lastLoginAt)}</span></span>
                        </div>
                      </div>
                      
                      <div className="sm:hidden mt-6">
                        <Button
                          onClick={() => setIsEditing(true)}
                          variant="outline"
                          className="w-full text-gray-600 dark:text-gray-300"
                        >
                          <Edit2 className="w-4 h-4 mr-2" /> Editar Perfil
                        </Button>
                      </div>
                    </>
                  )}
                </div>
              </div>

              <div className="border-t border-gray-100 dark:border-gray-700 pt-8">
                <div className="flex justify-between items-center mb-6">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50">Seguridad de la Sesión</h3>
                  <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-green-50 text-green-700 text-xs font-semibold border border-green-100">
                    <div className="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse" />
                    Sesión Verificada
                  </div>
                </div>
                
                {token && (
                  <div className="p-4 bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 rounded-xl border border-gray-100 dark:border-gray-700">
                    <p className="text-xs font-mono text-gray-500 dark:text-gray-400 uppercase tracking-widest mb-2 font-semibold">Token JWT Activo</p>
                    <p className="text-xs font-mono text-gray-600 dark:text-gray-300 break-all leading-relaxed bg-white dark:bg-gray-800 dark:text-gray-100 p-3 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm">
                      {token}
                    </p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
