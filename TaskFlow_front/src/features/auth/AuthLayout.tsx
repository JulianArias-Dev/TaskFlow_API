import { ReactNode } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Layout } from 'lucide-react';

interface AuthLayoutProps {
  children: ReactNode;
  subtitle: string;
}

export function AuthLayout({ children, subtitle }: AuthLayoutProps) {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 flex flex-col items-center justify-center p-6 sm:p-12">
      <motion.div
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex items-center gap-3 mb-8"
      >
        <div className="bg-blue-600 p-2 rounded-lg">
          <Layout className="w-8 h-8 text-white" />
        </div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-50 tracking-tight">TaskFlow</h1>
      </motion.div>

      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        className="w-full max-w-md bg-white dark:bg-gray-800 dark:text-gray-100 rounded-xl shadow-xl border border-gray-100 dark:border-gray-700 overflow-hidden"
      >
        <div className="p-8">
          <div className="mb-8">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-50 mb-1">
              {subtitle}
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Gestor de proyectos eficiente para equipos modernos.
            </p>
          </div>

          <AnimatePresence mode="wait">
            {children}
          </AnimatePresence>
        </div>
        
        <div className="bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 p-4 border-t border-gray-100 dark:border-gray-700 text-center">
          <p className="text-xs text-gray-400">
            © 2026 TaskFlow. Hecho para la eficiencia.
          </p>
        </div>
      </motion.div>
    </div>
  );
}
