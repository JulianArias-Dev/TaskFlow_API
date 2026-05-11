import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Mail } from 'lucide-react';
import { Button } from '../../../components/ui/Button';

/**
 * La recuperación de contraseña aún no está expuesta en el backend
 * (no hay servicio de email transaccional con plantillas configurado).
 * Mostramos una pantalla informativa estática hasta que se implemente
 * un endpoint POST /api/Auth/forgot-password.
 */
export function ForgotPasswordPage() {
  return (
    <motion.div
      key="forgot-password-info"
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-col items-center text-center gap-4 py-4"
    >
      <div className="w-16 h-16 bg-blue-100 text-blue-600 rounded-full flex items-center justify-center mb-2">
        <Mail className="w-8 h-8" />
      </div>
      <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50">
        Recuperación de contraseña
      </h3>
      <p className="text-sm text-gray-500 dark:text-gray-400 max-w-sm">
        Esta funcionalidad aún no está disponible. Si olvidaste tu contraseña,
        contacta a un administrador para restablecerla manualmente.
      </p>
      <Link to="/login" className="w-full mt-4">
        <Button variant="outline" className="w-full">
          Volver a iniciar sesión
        </Button>
      </Link>
    </motion.div>
  );
}
