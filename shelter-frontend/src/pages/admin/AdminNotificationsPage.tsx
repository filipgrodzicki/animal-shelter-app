import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  BellIcon,
  CheckIcon,
  XMarkIcon,
  ExclamationTriangleIcon,
  FunnelIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Badge, Select, Spinner, Pagination } from '@/components/common';
import {
  notificationsApi,
  Notification,
  NotificationType,
  NotificationPriority,
  getNotificationTypeLabel,
  getNotificationPriorityColor,
  getNotificationPriorityBg,
} from '@/api/notifications';
import { getErrorMessage } from '@/api/client';

const typeOptions = [
  { value: '', label: 'Wszystkie typy' },
  { value: 'NewAdoptionApplication', label: 'Nowe zgłoszenie' },
  { value: 'ApplicationNeedsReview', label: 'Wymaga przeglądu' },
  { value: 'ApplicationEscalation', label: 'Eskalacja' },
  { value: 'VisitScheduled', label: 'Wizyta zaplanowana' },
  { value: 'VisitReminder', label: 'Przypomnienie' },
  { value: 'ContractReady', label: 'Umowa gotowa' },
  { value: 'AnimalStatusChange', label: 'Zmiana statusu' },
  { value: 'SystemAlert', label: 'Alert systemowy' },
];

const priorityOptions = [
  { value: '', label: 'Wszystkie priorytety' },
  { value: 'Urgent', label: 'Pilne' },
  { value: 'High', label: 'Wysokie' },
  { value: 'Normal', label: 'Normalne' },
  { value: 'Low', label: 'Niskie' },
];

export function AdminNotificationsPage() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [typeFilter, setTypeFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [unreadOnly, setUnreadOnly] = useState(false);

  const pageSize = 20;
  const totalPages = Math.ceil(totalCount / pageSize);

  const fetchNotifications = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await notificationsApi.getAll({
        page,
        pageSize,
        type: typeFilter as NotificationType || undefined,
        priority: priorityFilter as NotificationPriority || undefined,
        unreadOnly: unreadOnly || undefined,
      });
      setNotifications(result.items);
      setTotalCount(result.totalCount);
      setUnreadCount(result.unreadCount);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchNotifications();
  }, [page, typeFilter, priorityFilter, unreadOnly]);

  const handleMarkAsRead = async (id: string) => {
    try {
      await notificationsApi.markAsRead(id);
      setNotifications(
        notifications.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await notificationsApi.markAllAsRead();
      setNotifications(notifications.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const handleDismiss = async (id: string) => {
    try {
      await notificationsApi.dismiss(id);
      setNotifications(notifications.filter((n) => n.id !== id));
      setTotalCount((prev) => prev - 1);
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString('pl-PL', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getPriorityBadgeVariant = (priority: NotificationPriority): 'red' | 'orange' | 'blue' | 'gray' => {
    const variants: Record<NotificationPriority, 'red' | 'orange' | 'blue' | 'gray'> = {
      Urgent: 'red',
      High: 'orange',
      Normal: 'blue',
      Low: 'gray',
    };
    return variants[priority];
  };

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Powiadomienia</h1>
        <p className="mt-2 text-gray-600">
          {unreadCount > 0 ? (
            <>Masz <span className="font-semibold text-primary-600">{unreadCount}</span> nieprzeczytanych powiadomień</>
          ) : (
            'Wszystkie powiadomienia przeczytane'
          )}
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4 mb-8">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <BellIcon className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{totalCount}</p>
              <p className="text-sm text-gray-500">Wszystkie</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary-100 rounded-lg">
              <BellIcon className="h-6 w-6 text-primary-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{unreadCount}</p>
              <p className="text-sm text-gray-500">Nieprzeczytane</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <Select
          options={typeOptions}
          value={typeFilter}
          onChange={(e) => {
            setTypeFilter(e.target.value);
            setPage(1);
          }}
          className="sm:max-w-[200px]"
        />
        <Select
          options={priorityOptions}
          value={priorityFilter}
          onChange={(e) => {
            setPriorityFilter(e.target.value);
            setPage(1);
          }}
          className="sm:max-w-[180px]"
        />
        <Button
          variant={unreadOnly ? 'primary' : 'outline'}
          onClick={() => {
            setUnreadOnly(!unreadOnly);
            setPage(1);
          }}
          leftIcon={<FunnelIcon className="h-5 w-5" />}
        >
          Tylko nieprzeczytane
        </Button>
        {unreadCount > 0 && (
          <Button variant="outline" onClick={handleMarkAllAsRead}>
            Oznacz wszystkie jako przeczytane
          </Button>
        )}
      </div>

      {/* Notifications list */}
      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : notifications.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            <BellIcon className="h-12 w-12 mx-auto mb-2 text-gray-300" />
            <p>Brak powiadomień</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {notifications.map((notification) => (
              <div
                key={notification.id}
                className={`p-4 hover:bg-gray-50 transition-colors ${
                  !notification.isRead ? 'bg-blue-50/50' : ''
                }`}
              >
                <div className="flex items-start gap-4">
                  {/* Priority indicator */}
                  <div
                    className={`flex-shrink-0 p-2 rounded-full ${getNotificationPriorityBg(
                      notification.priority
                    )}`}
                  >
                    {notification.priority === 'Urgent' ? (
                      <ExclamationTriangleIcon
                        className={`h-6 w-6 ${getNotificationPriorityColor(
                          notification.priority
                        )}`}
                      />
                    ) : (
                      <BellIcon
                        className={`h-6 w-6 ${getNotificationPriorityColor(
                          notification.priority
                        )}`}
                      />
                    )}
                  </div>

                  {/* Content */}
                  <div className="flex-1 min-w-0">
                    <div className="flex flex-wrap items-center gap-2 mb-1">
                      <Badge variant={getPriorityBadgeVariant(notification.priority)}>
                        {notification.priority === 'Urgent'
                          ? 'Pilne'
                          : notification.priority === 'High'
                          ? 'Wysokie'
                          : notification.priority === 'Normal'
                          ? 'Normalne'
                          : 'Niskie'}
                      </Badge>
                      <span className="text-sm text-gray-500">
                        {getNotificationTypeLabel(notification.type)}
                      </span>
                      <span className="text-sm text-gray-400">
                        {formatTime(notification.createdAt)}
                      </span>
                      {!notification.isRead && (
                        <span className="w-2 h-2 bg-blue-500 rounded-full" />
                      )}
                    </div>
                    <h3 className="font-medium text-gray-900">{notification.title}</h3>
                    <p className="text-gray-600 mt-1">{notification.message}</p>

                    {/* Actions */}
                    <div className="flex items-center gap-4 mt-3">
                      {notification.link && (
                        <Link
                          to={notification.link}
                          onClick={() => {
                            if (!notification.isRead) {
                              handleMarkAsRead(notification.id);
                            }
                          }}
                          className="text-sm text-primary-600 hover:text-primary-700 font-medium"
                        >
                          Zobacz szczegóły →
                        </Link>
                      )}
                      {!notification.isRead && (
                        <button
                          onClick={() => handleMarkAsRead(notification.id)}
                          className="text-sm text-gray-500 hover:text-gray-700 flex items-center gap-1"
                        >
                          <CheckIcon className="h-4 w-4" />
                          Oznacz jako przeczytane
                        </button>
                      )}
                    </div>
                  </div>

                  {/* Dismiss button */}
                  <button
                    onClick={() => handleDismiss(notification.id)}
                    className="flex-shrink-0 p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded"
                    title="Odrzuć"
                  >
                    <XMarkIcon className="h-5 w-5" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="px-4 py-3 border-t border-gray-200">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              onPageChange={setPage}
            />
          </div>
        )}
      </Card>
    </PageContainer>
  );
}
