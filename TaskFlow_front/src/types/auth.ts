export enum UserRole {
  ADMIN = 'ADMIN',
  PROJECT_MANAGER = 'PROJECT_MANAGER',
  DEVELOPER = 'DEVELOPER'
}

export interface UserProfile {
  uid: string;
  email: string;
  displayName: string;
  photoURL?: string;
  description?: string;
  role: UserRole;
  isActive?: boolean;
  theme?: 'light' | 'dark';
  createdAt: string;
  updatedAt?: string;
  lastLoginAt?: string;
}

export enum AuthOperationType {
  LOGIN = 'login',
  REGISTER = 'register',
  LOGOUT = 'logout'
}
