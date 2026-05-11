import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { authService } from '../../../services/authService';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      await authService.login(email, password);
      navigate('/');
    } catch (err: any) {
      console.error(err);
      setError('Credenciales inválidas. Por favor intenta de nuevo.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <motion.form
      key="login"
      initial={{ opacity: 0, x: 20 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -20 }}
      onSubmit={handleSubmit}
      className="flex flex-col gap-5"
    >
      <Input
        label="Correo electrónico"
        type="email"
        placeholder="nombre@empresa.com"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      
      <div className="flex flex-col gap-1">
        <Input
          label="Contraseña"
          type="password"
          placeholder="••••••••"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        <div className="flex justify-end">
          <Link to="/forgot-password" className="text-xs text-blue-600 hover:underline font-medium">
            ¿Olvidaste tu contraseña?
          </Link>
        </div>
      </div>

      {error && (
        <div className="p-3 bg-red-50 border border-red-100 rounded-md text-sm text-red-600">
          {error}
        </div>
      )}

      <Button type="submit" isLoading={isLoading} className="w-full mt-2">
        Iniciar sesión
      </Button>

      <p className="text-sm text-center text-gray-500 dark:text-gray-400 mt-4">
        ¿No tienes cuenta?{' '}
        <Link to="/register" className="text-blue-600 font-semibold hover:underline">
          Regístrate gratis
        </Link>
      </p>
    </motion.form>
  );
}
