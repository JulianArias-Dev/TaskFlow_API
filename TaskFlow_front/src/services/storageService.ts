/**
 * storageService — adaptador para subida de adjuntos contra el backend .NET.
 *
 * Antes usaba Firebase Storage; ahora delega en el endpoint
 * `/api/Attachments` del backend. Si el endpoint no está disponible se devuelve
 * un error legible que la UI puede mostrar al usuario.
 */

import { api, ApiError } from '../lib/api';

const MAX_FILE_SIZE_MB = 10;
const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;

export const uploadAttachment = async (
  file: File,
): Promise<{ url: string; error: string | null }> => {
  if (file.size > MAX_FILE_SIZE_BYTES) {
    return {
      url: '',
      error: `El archivo supera el tamaño máximo permitido de ${MAX_FILE_SIZE_MB}MB`,
    };
  }

  const formData = new FormData();
  formData.append('file', file);

  try {
    const response = await api.post<{ url: string }>('/Attachments', formData, { unwrap: false });
    return { url: response?.url ?? '', error: null };
  } catch (err) {
    const message =
      err instanceof ApiError ? err.message : err instanceof Error ? err.message : 'Error al subir el archivo';
    console.error('Error uploading file:', err);
    return { url: '', error: message };
  }
};
