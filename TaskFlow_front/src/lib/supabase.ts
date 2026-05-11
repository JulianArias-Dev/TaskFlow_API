/**
 * Compat stub — Supabase ya no se utiliza. El frontend habla directo con el
 * backend .NET. Este archivo se conserva sólo para no romper imports legados
 * y se eliminará cuando todos los consumidores se hayan migrado.
 */

function unsupported(target: string): never {
  throw new Error(
    `[supabase shim] '${target}' no está disponible: usa el storageService (que apunta al backend .NET).`,
  );
}

export const supabase = new Proxy(
  {},
  {
    get(_t, prop) {
      if (prop === 'toJSON' || typeof prop === 'symbol') return undefined;
      unsupported(String(prop));
    },
  },
) as any;
