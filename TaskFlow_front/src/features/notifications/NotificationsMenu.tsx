import React, { useState, useEffect, useRef } from 'react';
import { Bell, Check, Clock, MessageSquare, AlertCircle, RefreshCw } from 'lucide-react';
import { collection, query, where, orderBy, onSnapshot, limit } from 'firebase/firestore';
import { db, auth } from '../../lib/firebase';
import { AppNotification } from '../../types/models';
import { dbService } from '../../services/databaseService';
import { motion, AnimatePresence } from 'framer-motion';

export function NotificationsMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const menuRef = useRef<HTMLDivElement>(null);

  const [isPermissionGranted, setIsPermissionGranted] = useState(false);

  useEffect(() => {
    if ('Notification' in window) {
      if (Notification.permission === 'granted') {
        setIsPermissionGranted(true);
      } else if (Notification.permission !== 'denied') {
        Notification.requestPermission().then(permission => {
          if (permission === 'granted') {
            setIsPermissionGranted(true);
          }
        });
      }
    }
  }, []);

  useEffect(() => {
    if (!auth.currentUser) return;

    const q = query(
      collection(db, 'notifications'),
      where('userId', '==', auth.currentUser.uid),
      orderBy('createdAt', 'desc'),
      limit(20)
    );

    const unsubscribe = onSnapshot(q, (snapshot) => {
      const notifs = snapshot.docs.map(doc => ({ ...doc.data() } as AppNotification));
      setNotifications(notifs);
      setUnreadCount(notifs.filter(n => !n.read).length);

      // Trigger browser push notification for new notifications
      snapshot.docChanges().forEach((change) => {
        if (change.type === 'added') {
          const docData = change.doc.data() as AppNotification;
          // Only show push notification if it's unread and was created recently (within last 10 seconds)
          if (!docData.read && docData.createdAt) {
            const createdAtDate = docData.createdAt.toDate ? docData.createdAt.toDate() : new Date(docData.createdAt);
            const isRecent = (new Date().getTime() - createdAtDate.getTime()) < 10000;
            if (isRecent && isPermissionGranted && 'Notification' in window) {
              new Notification(docData.title, {
                body: docData.message,
                icon: '/favicon.ico' // Or any other suitable icon
              });
            }
          }
        }
      });
    });

    return () => unsubscribe();
  }, [isPermissionGranted]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleMarkRead = async (id: string) => {
    await dbService.markNotificationRead(id);
  };

  const markAllRead = async () => {
    for (const notif of notifications.filter(n => !n.read)) {
      await dbService.markNotificationRead(notif.id);
    }
  };

  const getIcon = (type: string) => {
    switch (type) {
      case 'ASSIGNED': return <Check className="w-4 h-4 text-blue-500" />;
      case 'DUE_OVERDUE': return <AlertCircle className="w-4 h-4 text-red-500" />;
      case 'DUE_SOON': return <Clock className="w-4 h-4 text-yellow-500" />;
      case 'COMMENT': return <MessageSquare className="w-4 h-4 text-purple-500" />;
      case 'STATUS_CHANGE': return <RefreshCw className="w-4 h-4 text-green-500" />;
      default: return <Bell className="w-4 h-4 text-gray-500 dark:text-gray-400" />;
    }
  };

  return (
    <div className="relative" ref={menuRef}>
      <button 
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:bg-gray-700 rounded-full transition-colors"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-red-500 border-2 border-white rounded-full"></span>
        )}
      </button>

      <AnimatePresence>
        {isOpen && (
          <motion.div 
            initial={{ opacity: 0, y: 10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.95 }}
            transition={{ duration: 0.15 }}
            className="absolute right-0 mt-2 w-80 md:w-96 bg-white dark:bg-gray-800 dark:text-gray-100 rounded-xl shadow-xl border border-gray-100 dark:border-gray-700 overflow-hidden z-50"
          >
            <div className="p-4 border-b border-gray-100 dark:border-gray-700 flex justify-between items-center bg-gray-50/50">
              <h3 className="font-semibold text-gray-900 dark:text-gray-50">Notificaciones</h3>
              {unreadCount > 0 && (
                <button 
                  onClick={markAllRead}
                  className="text-xs text-blue-600 hover:text-blue-800 font-medium"
                >
                  Marcar todas como leídas
                </button>
              )}
            </div>

            <div className="max-h-[400px] overflow-y-auto">
              {notifications.length === 0 ? (
                <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                  <Bell className="w-8 h-8 text-gray-300 mx-auto mb-2" />
                  <p className="text-sm">No tienes notificaciones</p>
                </div>
              ) : (
                <div className="divide-y divide-gray-100">
                  {notifications.map(notif => (
                    <div 
                      key={notif.id} 
                      className={`p-4 flex gap-3 hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 transition-colors ${!notif.read ? 'bg-blue-50/30' : ''}`}
                      onClick={() => !notif.read && handleMarkRead(notif.id)}
                    >
                      <div className={`mt-0.5 w-8 h-8 rounded-full flex items-center justify-center shrink-0 ${!notif.read ? 'bg-blue-100' : 'bg-gray-100 dark:bg-gray-700'}`}>
                        {getIcon(notif.type)}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className={`text-sm ${!notif.read ? 'font-semibold text-gray-900 dark:text-gray-50' : 'font-medium text-gray-700 dark:text-gray-200'}`}>
                          {notif.title}
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 line-clamp-2">
                          {notif.message}
                        </p>
                        <p className="text-[10px] text-gray-400 mt-2 uppercase font-medium">
                          {new Date(notif.createdAt?.toDate ? notif.createdAt.toDate() : notif.createdAt).toLocaleString('es-ES')}
                        </p>
                      </div>
                      {!notif.read && (
                        <div className="w-2 h-2 rounded-full bg-blue-500 mt-2 shrink-0" />
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
