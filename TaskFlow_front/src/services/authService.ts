import { 
  signInWithEmailAndPassword, 
  createUserWithEmailAndPassword, 
  signOut, 
  onAuthStateChanged,
  updateProfile,
  sendPasswordResetEmail,
  User as FirebaseUser 
} from 'firebase/auth';
import { doc, setDoc, updateDoc, getDoc, serverTimestamp } from 'firebase/firestore';
import { auth, db } from '../lib/firebase';

enum OperationType {
  CREATE = 'create',
  UPDATE = 'update',
  DELETE = 'delete',
  LIST = 'list',
  GET = 'get',
  WRITE = 'write',
}

interface FirestoreErrorInfo {
  error: string;
  operationType: OperationType;
  path: string | null;
  authInfo: {
    userId?: string | null;
    email?: string | null;
    emailVerified?: boolean | null;
    isAnonymous?: boolean | null;
  }
}

function handleFirestoreError(error: unknown, operationType: OperationType, path: string | null) {
  const errInfo: FirestoreErrorInfo = {
    error: error instanceof Error ? error.message : String(error),
    authInfo: {
      userId: auth.currentUser?.uid,
      email: auth.currentUser?.email,
      emailVerified: auth.currentUser?.emailVerified,
      isAnonymous: auth.currentUser?.isAnonymous,
    },
    operationType,
    path
  };
  console.error('Firestore Error: ', JSON.stringify(errInfo));
  throw new Error(JSON.stringify(errInfo));
}

export const authService = {
  async register(email: string, pass: string, name: string) {
    try {
      const userCredential = await createUserWithEmailAndPassword(auth, email, pass);
      const user = userCredential.user;
      
      await updateProfile(user, { displayName: name });
      
      const userPath = `users/${user.uid}`;
      try {
        await setDoc(doc(db, 'users', user.uid), {
          uid: user.uid,
          email: user.email,
          displayName: name,
          role: 'DEVELOPER',
          createdAt: serverTimestamp(),
          updatedAt: serverTimestamp()
        });
        await setDoc(doc(db, 'user_emails', btoa(email.toLowerCase())), {
          uid: user.uid,
          email: email.toLowerCase()
        });
      } catch (err) {
        handleFirestoreError(err, OperationType.WRITE, userPath);
      }
      
      return user;
    } catch (error) {
      throw error;
    }
  },

  async login(email: string, pass: string) {
    const cred = await signInWithEmailAndPassword(auth, email, pass);
    await this.updateLastLogin();
    return cred;
  },

  async updateLastLogin() {
    if (!auth.currentUser) return;
    const userPath = `users/${auth.currentUser.uid}`;
    try {
      await updateDoc(doc(db, 'users', auth.currentUser.uid), {
        lastLoginAt: serverTimestamp(),
        updatedAt: serverTimestamp()
      });
    } catch (err) {
      if (err instanceof Error && err.message.includes('not found')) return;
      handleFirestoreError(err, OperationType.UPDATE, userPath);
    }
  },

  async updateUserProfile(displayName: string, description?: string, photoURL?: string) {
    if (!auth.currentUser) throw new Error("No autenticado");
    const user = auth.currentUser;
    await updateProfile(user, { displayName, photoURL });
    
    const userPath = `users/${user.uid}`;
    const updates: any = {
      displayName,
      updatedAt: serverTimestamp()
    };
    if (description !== undefined) updates.description = description;
    if (photoURL !== undefined) updates.photoURL = photoURL;
    
    try {
      await updateDoc(doc(db, 'users', user.uid), updates);
    } catch (err) {
      handleFirestoreError(err, OperationType.UPDATE, userPath);
    }
  },

  async updateNotificationPreferences(preferences: any) {
    if (!auth.currentUser) throw new Error("No autenticado");
    const userPath = `users/${auth.currentUser.uid}`;
    try {
      await updateDoc(doc(db, 'users', auth.currentUser.uid), {
        notificationPreferences: preferences,
        updatedAt: serverTimestamp()
      });
    } catch (err) {
      handleFirestoreError(err, OperationType.UPDATE, userPath);
    }
  },

  async updateUserTheme(theme: 'light' | 'dark') {
    if (!auth.currentUser) throw new Error("No autenticado");
    const userPath = `users/${auth.currentUser.uid}`;
    try {
      await updateDoc(doc(db, 'users', auth.currentUser.uid), {
        theme,
        updatedAt: serverTimestamp()
      });
    } catch (err) {
      handleFirestoreError(err, OperationType.UPDATE, userPath);
    }
  },

  async getUserProfile(uid: string) {
    const userPath = `users/${uid}`;
    try {
      const docSnap = await getDoc(doc(db, 'users', uid));
      if (docSnap.exists()) {
        return docSnap.data();
      }
      return null;
    } catch (err) {
      handleFirestoreError(err, OperationType.GET, userPath);
      return null;
    }
  },

  async getIdToken() {
    if (!auth.currentUser) return null;
    return auth.currentUser.getIdToken();
  },

  async logout() {
    return signOut(auth);
  },

  async resetPassword(email: string) {
    return sendPasswordResetEmail(auth, email);
  },

  subscribe(callback: (user: FirebaseUser | null) => void) {
    return onAuthStateChanged(auth, callback);
  }
};
