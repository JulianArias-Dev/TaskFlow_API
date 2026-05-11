import { useState, useEffect } from 'react';
import { dbService } from '../../services/databaseService';
import { UserProfile, UserRole } from '../../types/auth';
import { GlobalSettings } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Shield, Users, Settings, UserX, Check, X, Edit2, AlertTriangle } from 'lucide-react';
import { collection, getDocs, doc, setDoc, updateDoc } from 'firebase/firestore';
import { db } from '../../lib/firebase';

export function AdminDashboard() {
  const [activeTab, setActiveTab] = useState<'users' | 'settings'>('users');
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [settings, setSettings] = useState<GlobalSettings | null>(null);
  const [loading, setLoading] = useState(true);

  // Settings form
  const [platformName, setPlatformName] = useState('');
  const [maxAttachment, setMaxAttachment] = useState(10);
  const [passwordPolicy, setPasswordPolicy] = useState<'standard' | 'strict'>('standard');
  const [savingSettings, setSavingSettings] = useState(false);

  // User edit
  const [editingUserId, setEditingUserId] = useState<string | null>(null);
  const [editRole, setEditRole] = useState<UserRole>(UserRole.DEVELOPER);
  const [editActive, setEditActive] = useState(true);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      // Load users. In a real app we might need a cloud function to list all auth users, 
      // but here we list user profiles stored in Firestore.
      const usersSnap = await getDocs(collection(db, 'users'));
      const usersList = usersSnap.docs.map(d => ({ uid: d.id, ...d.data() } as UserProfile));
      setUsers(usersList);

      // Load settings
      const settingsSnap = await getDocs(collection(db, 'globalSettings'));
      if (!settingsSnap.empty) {
        const s = settingsSnap.docs[0].data() as GlobalSettings;
        setSettings({ ...s, id: settingsSnap.docs[0].id });
        setPlatformName(s.platformName);
        setMaxAttachment(s.maxAttachmentSizeMB);
        setPasswordPolicy(s.passwordPolicy);
      } else {
        setPlatformName('TaskFlow');
        setMaxAttachment(10);
        setPasswordPolicy('standard');
      }
    } catch (e) {
      console.error("Error loading admin data", e);
    } finally {
      setLoading(false);
    }
  };

  const saveSettings = async () => {
    setSavingSettings(true);
    try {
      const sRef = doc(db, 'globalSettings', 'global');
      await setDoc(sRef, {
        platformName,
        maxAttachmentSizeMB: maxAttachment,
        passwordPolicy,
        updatedAt: new Date()
      }, { merge: true });
      alert('Configuración guardada correctamente');
    } catch (e) {
      console.error("Error saving settings", e);
      alert('Error guardando configuración');
    } finally {
      setSavingSettings(false);
    }
  };

  const updateUserProfile = async (uid: string) => {
    try {
      await updateDoc(doc(db, 'users', uid), {
        role: editRole,
        isActive: editActive,
        updatedAt: new Date().toISOString()
      });
      setUsers(users.map(u => u.uid === uid ? { ...u, role: editRole, isActive: editActive } : u));
      setEditingUserId(null);
    } catch (e) {
      console.error(e);
      alert('Error al actualizar el usuario');
    }
  };

  if (loading) {
    return <div className="text-center py-10">Cargando datos de administración...</div>;
  }

  return (
    <div className="bg-white dark:bg-gray-800 dark:text-gray-100 rounded-3xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div className="border-b border-gray-200 dark:border-gray-700">
        <div className="flex px-6 py-4 gap-4">
          <button
            onClick={() => setActiveTab('users')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${activeTab === 'users' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900'}`}
          >
            <Users className="w-4 h-4" />
            Gestión de Usuarios
          </button>
          <button
            onClick={() => setActiveTab('settings')}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${activeTab === 'settings' ? 'bg-blue-50 text-blue-700' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900'}`}
          >
            <Settings className="w-4 h-4" />
            Configuración Global
          </button>
        </div>
      </div>

      <div className="p-6">
        {activeTab === 'users' ? (
          <div>
            <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 mb-6">Usuarios Registrados</h3>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm text-gray-600 dark:text-gray-300">
                <thead className="bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 text-gray-500 dark:text-gray-400 uppercase font-semibold">
                  <tr>
                    <th className="px-4 py-3">Usuario</th>
                    <th className="px-4 py-3">Email</th>
                    <th className="px-4 py-3">Rol</th>
                    <th className="px-4 py-3">Estado</th>
                    <th className="px-4 py-3">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 border-t border-gray-100 dark:border-gray-700">
                  {users.map(u => (
                    <tr key={u.uid} className="hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900">
                      <td className="px-4 py-4 font-medium text-gray-900 dark:text-gray-50">{u.displayName || 'Sin nombre'}</td>
                      <td className="px-4 py-4">{u.email}</td>
                      <td className="px-4 py-4">
                        {editingUserId === u.uid ? (
                          <select 
                            className="bg-white dark:bg-gray-800 dark:text-gray-100 border border-gray-300 dark:border-gray-600 text-sm rounded-md focus:ring-blue-500 focus:border-blue-500 block p-1.5"
                            value={editRole}
                            onChange={(e) => setEditRole(e.target.value as UserRole)}
                          >
                            <option value={UserRole.ADMIN}>ADMIN</option>
                            <option value={UserRole.PROJECT_MANAGER}>PM</option>
                            <option value={UserRole.DEVELOPER}>DEV</option>
                          </select>
                        ) : (
                          <span className="bg-blue-50 text-blue-700 py-1 px-2 rounded-md text-xs font-bold">{u.role}</span>
                        )}
                      </td>
                      <td className="px-4 py-4">
                        {editingUserId === u.uid ? (
                          <select 
                            className="bg-white dark:bg-gray-800 dark:text-gray-100 border border-gray-300 dark:border-gray-600 text-sm rounded-md focus:ring-blue-500 focus:border-blue-500 block p-1.5"
                            value={editActive ? 'true' : 'false'}
                            onChange={(e) => setEditActive(e.target.value === 'true')}
                          >
                            <option value="true">Activo</option>
                            <option value="false">Inactivo</option>
                          </select>
                        ) : (
                          <span className={`py-1 px-2 rounded-md text-xs font-bold ${u.isActive !== false ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
                            {u.isActive !== false ? 'Activo' : 'Inactivo'}
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-4">
                        {editingUserId === u.uid ? (
                          <div className="flex gap-2">
                            <Button size="sm" onClick={() => updateUserProfile(u.uid)} className="bg-green-600 hover:bg-green-700">
                              <Check className="w-4 h-4" />
                            </Button>
                            <Button size="sm" variant="outline" onClick={() => setEditingUserId(null)}>
                              <X className="w-4 h-4" />
                            </Button>
                          </div>
                        ) : (
                          <Button 
                            size="sm" 
                            variant="outline" 
                            onClick={() => {
                              setEditingUserId(u.uid);
                              setEditRole(u.role);
                              setEditActive(u.isActive !== false);
                            }}
                          >
                            <Edit2 className="w-4 h-4" /> Editar
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ) : (
          <div className="max-w-2xl">
            <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 mb-6 flex items-center gap-2">
              <Shield className="w-5 h-5 text-blue-600" />
              Configuración de la Plataforma
            </h3>
            
            <div className="space-y-6">
              <Input
                label="Nombre de la Plataforma"
                value={platformName}
                onChange={e => setPlatformName(e.target.value)}
                placeholder="TaskFlow"
              />
              
              <div>
                <label className="block text-sm font-medium text-gray-900 dark:text-gray-50 mb-2">Límite de archivos adjuntos (MB)</label>
                <input
                  type="number"
                  min="1"
                  max="100"
                  className="w-full bg-white dark:bg-gray-800 dark:text-gray-100 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-gray-50 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block p-3"
                  value={maxAttachment}
                  onChange={(e) => setMaxAttachment(parseInt(e.target.value) || 10)}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-900 dark:text-gray-50 mb-2">Política de contraseñas</label>
                <div className="flex gap-4">
                  <label className="flex items-center gap-2 cursor-pointer p-3 border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 flex-1 hover:bg-gray-100 dark:bg-gray-700">
                    <input 
                      type="radio" 
                      name="pwdPolicy" 
                      value="standard"
                      checked={passwordPolicy === 'standard'}
                      onChange={() => setPasswordPolicy('standard')}
                      className="w-4 h-4 text-blue-600"
                    />
                    <div className="flex flex-col">
                      <span className="font-semibold text-gray-900 dark:text-gray-50">Estándar</span>
                      <span className="text-xs text-gray-500 dark:text-gray-400">Min 6 caracteres</span>
                    </div>
                  </label>
                  <label className="flex items-center gap-2 cursor-pointer p-3 border border-gray-200 dark:border-gray-700 rounded-lg bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 flex-1 hover:bg-gray-100 dark:bg-gray-700">
                    <input 
                      type="radio" 
                      name="pwdPolicy" 
                      value="strict"
                      checked={passwordPolicy === 'strict'}
                      onChange={() => setPasswordPolicy('strict')}
                      className="w-4 h-4 text-blue-600"
                    />
                    <div className="flex flex-col">
                      <span className="font-semibold text-gray-900 dark:text-gray-50">Estricta</span>
                      <span className="text-xs text-gray-500 dark:text-gray-400">Especial, Num, Mayúsculas</span>
                    </div>
                  </label>
                </div>
              </div>

              <div className="pt-6 border-t border-gray-100 dark:border-gray-700">
                <Button onClick={saveSettings} isLoading={savingSettings}>
                  <Check className="w-4 h-4 mr-2" />
                  Guardar Configuración
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
