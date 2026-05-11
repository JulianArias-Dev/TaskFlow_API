import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import type { CatalogItem } from '../types/api';

/**
 * Catálogos disponibles en el backend (.NET BaseCatalogController).
 * Cada uno se mapea a un endpoint `/api/BaseCatalog/<kind>`.
 */
export type CatalogKind =
  | 'task-types'
  | 'task-priorities'
  | 'task-status'
  | 'project-status'
  | 'project-roles'
  | 'app-roles';

// Cache compartida en memoria a nivel de módulo — evita pedir el mismo
// catálogo varias veces si distintos componentes lo necesitan.
const cache = new Map<CatalogKind, Promise<CatalogItem[]>>();

function fetchCatalog(kind: CatalogKind): Promise<CatalogItem[]> {
  if (!cache.has(kind)) {
    cache.set(
      kind,
      api
        .get<CatalogItem[]>(`/BaseCatalog/${kind}`, { unwrap: false })
        .catch((err) => {
          // Si falla, no dejamos la promesa rechazada cacheada — reintentamos luego.
          cache.delete(kind);
          throw err;
        }),
    );
  }
  return cache.get(kind)!;
}

/**
 * Hook React que carga un catálogo del backend y lo expone como array.
 * Útil para poblar `<select>` con las opciones reales que la BD soporta,
 * en vez de duros codificados que se desincronizan con el catálogo.
 *
 * @example
 *   const { items, loading } = useCatalog('task-priorities');
 *   items.map(i => <option key={i.id} value={i.id}>{i.name}</option>)
 */
export function useCatalog(kind: CatalogKind) {
  const [items, setItems] = useState<CatalogItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    fetchCatalog(kind)
      .then((data) => {
        if (cancelled) return;
        setItems(data ?? []);
        setError(null);
      })
      .catch((err: Error) => {
        if (cancelled) return;
        setError(err);
        setItems([]);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [kind]);

  return { items, loading, error };
}

/**
 * Versión imperativa — útil cuando un servicio (no un componente) necesita
 * resolver un id por nombre/código.
 */
export async function getCatalog(kind: CatalogKind): Promise<CatalogItem[]> {
  return fetchCatalog(kind);
}

/** Helper: encuentra el id de un item por código o nombre (case-insensitive). */
export function findCatalogId(
  items: CatalogItem[],
  hint: string,
  fallback = 0,
): number {
  if (!hint) return fallback;
  const norm = hint.toUpperCase();
  const match = items.find(
    (i) => i.code?.toUpperCase() === norm || i.name?.toUpperCase() === norm,
  );
  return match?.id ?? fallback;
}
