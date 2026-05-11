import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { authService } from '../../../services/authService';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');
    setSuccess(false);

    try {
      await authService.resetPassword(email);
      setSuccess(true);
    } catch (err: any) {
      console.error(err);
      if (err.code === 'auth/user-not-found') {
        setError('No hay ninguna cuenta registrada con este correo.');
      } else {
        setError('Ocurrió un error al enviar el correo. Por favor intenta de nuevo.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <motion.div
        key="success"
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="flex flex-col items-center text-center gap-4 py-4"
      >
        <div className="w-16 h-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center mb-2">
          <svg className="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50">Correo enviado</h3>
        <p className="text-gray-500 dark:text-gray-400 text-sm">
          Hemos enviado un enlace de recuperación a <strong>{email}</strong>. 
          Por favor revisa tu bandeja de entrada (y la carpeta de spam).
        </p>
        <Link to="/login" className="mt-4 w-full">
          <Button variant="outline" className="w-full">
            Volver a iniciar sesión
          </Button>
        </Link>
      </motion.div>
    );
  }

  return (
    <motion.form
      key="forgot-password"
      initial={{ opacity: 0, x: 20 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -20 }}
      onSubmit={handleSubmit}
      className="flex flex-col gap-5"
    >
      <p className="text-sm text-gray-600 dark:text-gray-300 mb-2">
        Ingresa el correo electrónico asociado a tu cuenta y te enviaremos un enlace para restablecer tu contraseña.
      </p>

      <Input
        label="Correo electrónico"
        type="email"
        placeholder="nombre@empresa.com"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />

      {error && (
        <div className="p-3 bg-red-50 border border-red-100 rounded-md text-sm text-red-600">
          {error}
        </div>
      )}

      <Button type="submit" isLoading={isLoading} className="w-full mt-2">
        Enviar enlace de recuperación
      </Button>

      <div className="text-center mt-2">
        <Link to="/login" className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:text-gray-50 font-medium transition-colors">
          &larr; Volver a iniciar sesión
        </Link>
      </div>
    </motion.form>
  );
}
