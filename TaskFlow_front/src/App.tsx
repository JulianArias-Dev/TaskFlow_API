import { useEffect, useState } from 'react';
import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { authService } from './services/authService';
import type { AuthUser } from './lib/firebase';
import { AuthLayout } from './features/auth/AuthLayout';
import { LoginPage } from './features/auth/pages/LoginPage';
import { RegisterPage } from './features/auth/pages/RegisterPage';
import { ForgotPasswordPage } from './features/auth/pages/ForgotPasswordPage';

import { ProfileDashboard } from './features/profile/ProfileDashboard';

function ProtectedRoute({ user, children }: { user: AuthUser | null; children: React.ReactNode }) {
  if (!user) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = authService.subscribe((u) => {
      setUser(u);
      setLoading(false);
    });
    return () => unsubscribe();
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
        <div className="w-10 h-10 border-4 border-blue-600/30 border-t-blue-600 rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={
          <AuthLayout subtitle="Bienvenido de nuevo">
            <LoginPage />
          </AuthLayout>
        } />
        <Route path="/register" element={
          <AuthLayout subtitle="Crea tu cuenta">
            <RegisterPage />
          </AuthLayout>
        } />
        <Route path="/forgot-password" element={
          <AuthLayout subtitle="Recuperar contraseña">
            <ForgotPasswordPage />
          </AuthLayout>
        } />
        <Route path="/" element={
          <ProtectedRoute user={user}>
            <ProfileDashboard user={user!} />
          </ProtectedRoute>
        } />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
