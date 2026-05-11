/**
 * Compat shim — sustituye al cliente Firebase original.
 *
 * El frontend antes hablaba con Firebase Auth + Firestore. Tras migrar al backend
 * .NET (TaskFlow API + JWT) este módulo expone un objeto con la misma forma que
 * `firebase/auth` consumen los componentes legados (`auth.currentUser`,
 * `onAuthStateChanged` simulado vía suscripción interna), sin arrastrar la
 * dependencia real de Firebase para auth.
 *
 * `db` y `storage` se exponen como stubs: cualquier operación Firestore/Storage
 * directa lanza un error explícito para que sea fácil detectar dónde quedan
 * llamadas pendientes de migrar.
 */

import { tokenStorage, userStorage } from './api';

export interface AuthUser {
  uid: string;
  email: string | null;
  displayName: string | null;
  photoURL: string | null;
  role: string;
  /** Devuelve el JWT actual (no genera uno nuevo). */
  getIdToken(): Promise<string | null>;
  /** Mantiene compatibilidad con código legado que consultaba estos flags. */
  readonly emailVerified: boolean;
  readonly isAnonymous: boolean;
}

type AuthListener = (user: AuthUser | null) => void;

interface StoredUser {
  uid: string;
  email: string;
  displayName: string;
  photoURL?: string | null;
  role: string;
}

class AuthShim {
  currentUser: AuthUser | null = null;
  private listeners: Set<AuthListener> = new Set();

  constructor() {
    // Rehidratar el usuario desde localStorage si existe sesión previa.
    const stored = userStorage.get<StoredUser>();
    const token = tokenStorage.get();
    if (stored && token) {
      this.currentUser = this.toAuthUser(stored);
    }
  }

  private toAuthUser(u: StoredUser): AuthUser {
    return {
      uid: u.uid,
      email: u.email,
      displayName: u.displayName,
      photoURL: u.photoURL ?? null,
      role: u.role,
      emailVerified: true,
      isAnonymous: false,
      getIdToken: async () => tokenStorage.get(),
    };
  }

  /** Llamado internamente por el authService tras login/register/logout. */
  _setUser(user: StoredUser | null): void {
    this.currentUser = user ? this.toAuthUser(user) : null;
    this.notify();
  }

  /** Permite que otros servicios actualicen el perfil en memoria + storage. */
  _patchProfile(patch: Partial<StoredUser>): void {
    const current = userStorage.get<StoredUser>();
    if (!current) return;
    const merged = { ...current, ...patch };
    userStorage.set(merged);
    this.currentUser = this.toAuthUser(merged);
    this.notify();
  }

  subscribe(listener: AuthListener): () => void {
    this.listeners.add(listener);
    // Emitir el estado actual de inmediato (igual que onAuthStateChanged).
    queueMicrotask(() => listener(this.currentUser));
    return () => {
      this.listeners.delete(listener);
    };
  }

  private notify(): void {
    for (const l of this.listeners) {
      try {
        l(this.currentUser);
      } catch (e) {
        console.error('[auth] listener error', e);
      }
    }
  }
}

export const auth = new AuthShim();

// Limpiar sesión al detectar token expirado desde el cliente HTTP.
if (typeof window !== 'undefined') {
  window.addEventListener('taskflow:auth-expired', () => {
    auth._setUser(null);
  });
}

// ---------------------------------------------------------------------------
// Stubs para db / storage — preservan los imports legados sin tirar la app.
// Cualquier uso real lanza un error con instrucciones claras.
// ---------------------------------------------------------------------------

function unsupported(target: string): never {
  throw new Error(
    `[firebase shim] '${target}' no está disponible: el frontend ahora consume el backend .NET. ` +
      `Migra esta llamada a un método de databaseService / authService que use la API REST.`,
  );
}

export const db = new Proxy(
  {},
  {
    get(_t, prop) {
      if (prop === 'toJSON' || typeof prop === 'symbol') return undefined;
      unsupported(`db.${String(prop)}`);
    },
  },
);

export const storage = new Proxy(
  {},
  {
    get(_t, prop) {
      if (prop === 'toJSON' || typeof prop === 'symbol') return undefined;
      unsupported(`storage.${String(prop)}`);
    },
  },
);
