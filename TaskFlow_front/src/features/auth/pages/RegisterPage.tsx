import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { authService } from '../../../services/authService';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';

export function RegisterPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (password.length < 6) {
      setError('La contraseña debe tener al menos 6 caracteres.');
      return;
    }
    
    setIsLoading(true);
    setError('');

    try {
      await authService.register(email, password, name);
      navigate('/');
    } catch (err: any) {
      console.error(err);
      if (err.code === 'auth/email-already-in-use') {
        setError('Este correo ya está registrado.');
      } else {
        setError('Ocurrió un error al registrar. Inténtalo de nuevo.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <motion.form
      key="register"
      initial={{ opacity: 0, x: 20 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -20 }}
      onSubmit={handleSubmit}
      className="flex flex-col gap-5"
    >
      <Input
        label="Nombre completo"
        type="text"
        placeholder="Juan Pérez"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />

      <Input
        label="Correo electrónico"
        type="email"
        placeholder="nombre@empresa.com"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      
      <Input
        label="Contraseña"
        type="password"
        placeholder="Mínimo 6 caracteres"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />

      {error && (
        <div className="p-3 bg-red-50 border border-red-100 rounded-md text-sm text-red-600">
          {error}
        </div>
      )}

      <Button type="submit" isLoading={isLoading} className="w-full mt-2">
        Crear cuenta
      </Button>

      <p className="text-sm text-center text-gray-500 dark:text-gray-400 mt-4">
        ¿Ya tienes cuenta?{' '}
        <Link to="/login" className="text-blue-600 font-semibold hover:underline">
          Inicia sesión
        </Link>
      </p>
    </motion.form>
  );
}
