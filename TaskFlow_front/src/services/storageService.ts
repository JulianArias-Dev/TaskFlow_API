import { ref, uploadBytes, getDownloadURL } from 'firebase/storage';
import { storage } from '../lib/firebase';

const MAX_FILE_SIZE_MB = 10;
const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;

export const uploadAttachment = async (file: File): Promise<{ url: string; error: string | null }> => {
  if (file.size > MAX_FILE_SIZE_BYTES) {
    return { url: '', error: `El archivo supera el tamaño máximo permitido de ${MAX_FILE_SIZE_MB}MB` };
  }

  const fileName = `${Date.now()}_${file.name}`;
  const storageRef = ref(storage, `attachments/${fileName}`);
  
  try {
    const snapshot = await uploadBytes(storageRef, file);
    const url = await getDownloadURL(snapshot.ref);

    return { url, error: null };
  } catch (err: any) {
    console.error('Error uploading file:', err);
    return { url: '', error: err.message || 'Error al subir el archivo' };
  }
};
