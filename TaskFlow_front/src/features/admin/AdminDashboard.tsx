import { useState, useEffect } from 'react';
import { UserProfile, UserRole } from '../../types/auth';
import { Button } from '../../components/ui/Button';
import { Users, Check, X, Edit2 } from 'lucide-react';
import { api } from '../../lib/api';
import type { UserApi } from '../../types/api';

export function AdminDashboard() {
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [loading, setLoading] = useState(true);

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
      const apiUsers = await api.get<UserApi[]>('/Users', { unwrap: false }).catch(() => [] as UserApi[]);
      const usersList: UserProfile[] = (apiUsers ?? []).map((u) => ({
        uid: u.id,
        email: u.email,
        displayName: u.name,
        role: (u.role?.toUpperCase() as UserRole) ?? UserRole.DEVELOPER,
        isActive: u.isActive,
        photoURL: u.avatarUrl ?? undefined,
        createdAt: u.createdAt,
        updatedAt: u.updatedAt,
        lastLoginAt: u.lastLoginAt ?? undefined,
      }));
      setUsers(usersList);
    } catch (e) {
      console.error('Error loading admin data', e);
    } finally {
      setLoading(false);
    }
  };

  const saveUser = async (uid: string) => {
    try {
      const user = users.find((u) => u.uid === uid);
      if (!user) return;
      await api.put(
        `/Users/${uid}/profile`,
        { name: user.displayName, avatarUrl: user.photoURL ?? null },
        { unwrap: false },
      );
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
      <div className="border-b border-gray-200 dark:border-gray-700 px-6 py-4">
        <div className="flex items-center gap-2 text-blue-700">
          <Users className="w-5 h-5" />
          <h2 className="font-semibold">Gestión de Usuarios</h2>
        </div>
      </div>

      <div className="p-6">
        <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 mb-6">Usuarios Registrados</h3>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-gray-600 dark:text-gray-300">
            <thead className="bg-gray-50 dark:bg-gray-900 text-gray-500 dark:text-gray-400 uppercase font-semibold">
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
                <tr key={u.uid} className="hover:bg-gray-50 dark:hover:bg-gray-900">
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
                        <Button size="sm" onClick={() => saveUser(u.uid)} className="bg-green-600 hover:bg-green-700">
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
    </div>
  );
}
