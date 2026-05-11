/**
 * authService — autenticación contra el backend .NET (TaskFlow API).
 *
 * Reemplaza a Firebase Auth: persiste el JWT en localStorage, mantiene un
 * usuario en memoria a través del shim `lib/firebase.ts` y conserva la misma
 * superficie pública que consumían las páginas (login, register, logout,
 * resetPassword, subscribe, getIdToken, updateUserProfile, ...).
 */

import { api, tokenStorage, userStorage } from '../lib/api';
import { auth, type AuthUser } from '../lib/firebase';
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  TokenValidationInfo,
  UserApi,
} from '../types/api';

type StoredUser = {
  uid: string;
  email: string;
  displayName: string;
  photoURL?: string | null;
  role: string;
};

function persist(login: LoginResponse): StoredUser {
  tokenStorage.set(login.token);
  const stored: StoredUser = {
    uid: login.userId,
    email: login.email,
    displayName: login.name,
    photoURL: null,
    role: login.role,
  };
  userStorage.set(stored);
  auth._setUser(stored);
  return stored;
}

export const authService = {
  /** Login con email + password. Devuelve el usuario autenticado. */
  async login(email: string, pass: string): Promise<AuthUser> {
    const data = await api.post<LoginResponse>(
      '/Auth/login',
      { email, password: pass } satisfies LoginRequest,
      { skipAuth: true },
    );
    persist(data);
    return auth.currentUser!;
  },

  /** Registra y deja al usuario autenticado (el backend devuelve el JWT). */
  async register(email: string, pass: string, name: string): Promise<AuthUser> {
    const data = await api.post<LoginResponse>(
      '/Auth/register',
      {
        email,
        name,
        password: pass,
        // AppRole en el backend: 1=Admin, 2=CommonUser. Los roles "Developer"
        // / "Project Manager" pertenecen al catálogo ProjectRoles (por proyecto).
        roleId: 2,
      } satisfies RegisterRequest,
      { skipAuth: true },
    );
    persist(data);
    return auth.currentUser!;
  },

  /** Logout — revoca el token en el backend y limpia el storage local. */
  async logout(): Promise<void> {
    try {
      if (tokenStorage.get()) {
        await api.post('/Auth/logout', undefined, { unwrap: false });
      }
    } catch {
      // Si el backend falla (red caída, token ya inválido), seguimos limpiando local.
    } finally {
      tokenStorage.clear();
      auth._setUser(null);
    }
  },

  /** Valida el JWT actual contra el backend; devuelve la info del claim. */
  async validateToken(): Promise<TokenValidationInfo | null> {
    if (!tokenStorage.get()) return null;
    try {
      return await api.get<TokenValidationInfo>('/Auth/validate');
    } catch {
      return null;
    }
  },

  /** Devuelve el JWT actual (no genera uno nuevo). */
  async getIdToken(): Promise<string | null> {
    return tokenStorage.get();
  },

  /** Compatibilidad con el authService anterior. */
  async updateLastLogin(): Promise<void> {
    // El backend ya actualiza lastLoginAt en el login; no es necesario hacer nada aquí.
    return;
  },

  /**
   * Actualiza el perfil del usuario actual (nombre y avatar).
   * Usa PUT /api/Users/{id}/profile (body JSON) — soporta avatarUrl largos
   * como data URLs, a diferencia del PUT clásico con query params.
   */
  async updateUserProfile(displayName: string, _description?: string, photoURL?: string): Promise<void> {
    if (!auth.currentUser) throw new Error('No autenticado');
    const id = auth.currentUser.uid;
    const updated = await api.put<UserApi>(
      `/Users/${id}/profile`,
      { name: displayName, avatarUrl: photoURL ?? null },
      { unwrap: false },
    );
    auth._patchProfile({
      displayName: updated?.name ?? displayName,
      photoURL: updated?.avatarUrl ?? photoURL ?? null,
    });
  },

  /** Preferencias de tema — persisten contra /api/Themes (best-effort). */
  async updateUserTheme(theme: 'light' | 'dark'): Promise<void> {
    if (!auth.currentUser) throw new Error('No autenticado');
    try {
      await api.put(`/Themes/user/${auth.currentUser.uid}`, { themePreference: theme });
    } catch (err) {
      console.warn('[authService] No se pudo persistir el tema en el backend', err);
    }
  },

  /** Preferencias de notificaciones — no-op hasta que exista endpoint. */
  async updateNotificationPreferences(_prefs: unknown): Promise<void> {
    console.warn('[authService] updateNotificationPreferences aún no implementado en el backend');
  },

  /** Recupera el perfil de un usuario por uid. */
  async getUserProfile(uid: string): Promise<UserApi | null> {
    try {
      return await api.get<UserApi>(`/Users/${uid}`, { unwrap: false });
    } catch {
      return null;
    }
  },

  /**
   * Equivalente a Firebase onAuthStateChanged. Acepta un callback que recibe
   * el AuthUser actual (o null si no hay sesión) y devuelve un unsubscribe.
   */
  subscribe(callback: (user: AuthUser | null) => void) {
    return auth.subscribe(callback);
  },
};
