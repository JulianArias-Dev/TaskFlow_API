/**
 * Cliente HTTP centralizado para hablar con la TaskFlow API (.NET).
 *
 * - Inyecta automáticamente el JWT (Bearer) desde localStorage.
 * - Normaliza la respuesta envuelta en ResponseDto<T> del backend.
 * - Permite a la capa de servicios trabajar con tipos limpios.
 */

import type { ApiResponse } from '../types/api';

const TOKEN_STORAGE_KEY = 'taskflow.jwt';
const USER_STORAGE_KEY = 'taskflow.user';

// La URL base se resuelve con prioridad:
//   1) VITE_API_URL          (definida en .env / docker build args)
//   2) window.__TASKFLOW_API  (inyectable en runtime para escenarios sin rebuild)
//   3) '/api'                  (default — nginx proxy en producción)
function resolveBaseUrl(): string {
  const envUrl = (import.meta as any).env?.VITE_API_URL as string | undefined;
  if (envUrl && envUrl.trim().length > 0) return envUrl.replace(/\/$/, '');
  const runtime = (globalThis as any).__TASKFLOW_API as string | undefined;
  if (runtime && runtime.trim().length > 0) return runtime.replace(/\/$/, '');
  return '/api';
}

const API_BASE_URL = resolveBaseUrl();

export class ApiError extends Error {
  status: number;
  errors: string[];
  payload?: unknown;

  constructor(message: string, status: number, errors: string[] = [], payload?: unknown) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
    this.payload = payload;
  }
}

export const tokenStorage = {
  get(): string | null {
    try {
      return localStorage.getItem(TOKEN_STORAGE_KEY);
    } catch {
      return null;
    }
  },
  set(token: string): void {
    try {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    } catch {
      /* ignore */
    }
  },
  clear(): void {
    try {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      localStorage.removeItem(USER_STORAGE_KEY);
    } catch {
      /* ignore */
    }
  },
};

export const userStorage = {
  get<T = unknown>(): T | null {
    try {
      const raw = localStorage.getItem(USER_STORAGE_KEY);
      return raw ? (JSON.parse(raw) as T) : null;
    } catch {
      return null;
    }
  },
  set(user: unknown): void {
    try {
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
    } catch {
      /* ignore */
    }
  },
};

// URL raíz del servidor (sin /api) — para recursos estáticos como uploads
export const SERVER_BASE_URL = (() => {
  const envUrl = (import.meta as any).env?.VITE_API_URL as string | undefined;
  if (envUrl && envUrl.trim().length > 0) return envUrl.replace(/\/$/, '').replace(/\/api$/, '');
  const runtime = (globalThis as any).__TASKFLOW_API as string | undefined;
  if (runtime && runtime.trim().length > 0) return runtime.replace(/\/$/, '').replace(/\/api$/, '');
  return 'http://localhost:8080';
})();

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  query?: Record<string, string | number | boolean | undefined | null>;
  headers?: Record<string, string>;
  skipAuth?: boolean;
  /** Si es true, parsea la respuesta como ApiResponse<T> y devuelve `data`. */
  unwrap?: boolean;
  signal?: AbortSignal;
};

function buildQuery(params?: RequestOptions['query']): string {
  if (!params) return '';
  const usp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null) continue;
    usp.append(k, String(v));
  }
  const s = usp.toString();
  return s ? `?${s}` : '';
}

async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, query, headers = {}, skipAuth = false, unwrap = true, signal } = opts;

  const finalHeaders: Record<string, string> = {
    Accept: 'application/json',
    ...headers,
  };

  if (body !== undefined && !(body instanceof FormData)) {
    finalHeaders['Content-Type'] = 'application/json';
  }

  if (!skipAuth) {
    const token = tokenStorage.get();
    if (token) finalHeaders['Authorization'] = `Bearer ${token}`;
  }

  const url = `${API_BASE_URL}${path.startsWith('/') ? '' : '/'}${path}${buildQuery(query)}`;

  const response = await fetch(url, {
    method,
    headers: finalHeaders,
    body:
      body === undefined
        ? undefined
        : body instanceof FormData
          ? body
          : JSON.stringify(body),
    signal,
  });

  // 204 No Content
  if (response.status === 204) {
    return undefined as unknown as T;
  }

  let payload: unknown = null;
  const text = await response.text();
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = text;
    }
  }

  if (!response.ok) {
    // Si el token expiró, limpiar storage para forzar re-login.
    if (response.status === 401 && !skipAuth) {
      tokenStorage.clear();
      // Notificar a la app para que pueda redirigir a /login
      window.dispatchEvent(new CustomEvent('taskflow:auth-expired'));
    }

    // Normaliza dos formatos de error posibles del backend:
    //   1) ResponseDto:   { success:false, message, errors: string[] }
    //   2) ProblemDetails (ASP.NET DataAnnotations / ApiController):
    //      { type, title, status, errors: { Field: [msg1, msg2] } }
    const errors: string[] = [];
    if (payload && typeof payload === 'object') {
      const p = payload as Record<string, unknown>;
      if (Array.isArray(p.errors)) {
        errors.push(...(p.errors as string[]));
      } else if (p.errors && typeof p.errors === 'object') {
        for (const v of Object.values(p.errors as Record<string, unknown>)) {
          if (Array.isArray(v)) errors.push(...(v as string[]));
          else if (typeof v === 'string') errors.push(v);
        }
      }
    }

    // El "message" preferido es el primer error concreto; si no hay,
    // se usa el message/title del payload o el statusText.
    const message =
      errors[0] ||
      (payload as ApiResponse)?.message ||
      (payload as { title?: string })?.title ||
      (typeof payload === 'string' ? payload : null) ||
      response.statusText ||
      'Request failed';

    throw new ApiError(message, response.status, errors, payload);
  }

  if (!unwrap) return payload as T;

  // El backend usa ResponseDto<T> con shape { success, message, data, errors }
  if (
    payload &&
    typeof payload === 'object' &&
    'success' in (payload as object) &&
    'data' in (payload as object)
  ) {
    const wrapped = payload as ApiResponse<T>;
    if (!wrapped.success) {
      throw new ApiError(wrapped.message ?? 'Request failed', response.status, wrapped.errors ?? [], wrapped);
    }
    return (wrapped.data as T) ?? (undefined as unknown as T);
  }

  // Si la respuesta no viene envuelta (p.ej. Users controller devuelve User directamente)
  return payload as T;
}

export const api = {
  get<T>(path: string, opts?: Omit<RequestOptions, 'method' | 'body'>) {
    return request<T>(path, { ...opts, method: 'GET' });
  },
  post<T>(path: string, body?: unknown, opts?: Omit<RequestOptions, 'method' | 'body'>) {
    return request<T>(path, { ...opts, method: 'POST', body });
  },
  put<T>(path: string, body?: unknown, opts?: Omit<RequestOptions, 'method' | 'body'>) {
    return request<T>(path, { ...opts, method: 'PUT', body });
  },
  patch<T>(path: string, body?: unknown, opts?: Omit<RequestOptions, 'method' | 'body'>) {
    return request<T>(path, { ...opts, method: 'PATCH', body });
  },
  delete<T>(path: string, opts?: Omit<RequestOptions, 'method' | 'body'>) {
    return request<T>(path, { ...opts, method: 'DELETE' });
  },
  baseUrl: API_BASE_URL,
};
