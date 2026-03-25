import { get, post, buildQueryString } from './client';

// Types
export type NotificationType =
  | 'NewAdoptionApplication'
  | 'ApplicationNeedsReview'
  | 'ApplicationEscalation'
  | 'VisitScheduled'
  | 'VisitReminder'
  | 'ContractReady'
  | 'AnimalStatusChange'
  | 'SystemAlert';

export type NotificationPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface Notification {
  id: string;
  type: NotificationType;
  priority: NotificationPriority;
  title: string;
  message: string;
  link: string | null;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface NotificationsResult {
  items: Notification[];
  unreadCount: number;
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UnreadCount {
  total: number;
  urgent: number;
}

// API
interface GetNotificationsParams {
  unreadOnly?: boolean;
  type?: NotificationType;
  priority?: NotificationPriority;
  page?: number;
  pageSize?: number;
}

export const notificationsApi = {
  getAll: (params: GetNotificationsParams = {}) =>
    get<NotificationsResult>(`/notifications${buildQueryString(params)}`),

  getUnreadCount: () =>
    get<UnreadCount>('/notifications/unread-count'),

  markAsRead: (id: string) =>
    post<void>(`/notifications/${id}/read`),

  markAllAsRead: () =>
    post<{ markedCount: number }>('/notifications/read-all'),

  dismiss: (id: string) =>
    post<void>(`/notifications/${id}/dismiss`),
};

// Helper functions
export const getNotificationTypeLabel = (type: NotificationType): string => {
  const labels: Record<NotificationType, string> = {
    NewAdoptionApplication: 'Nowe zgłoszenie',
    ApplicationNeedsReview: 'Wymaga przeglądu',
    ApplicationEscalation: 'Eskalacja',
    VisitScheduled: 'Wizyta zaplanowana',
    VisitReminder: 'Przypomnienie o wizycie',
    ContractReady: 'Umowa gotowa',
    AnimalStatusChange: 'Zmiana statusu zwierzęcia',
    SystemAlert: 'Alert systemowy',
  };
  return labels[type] || type;
};

export const getNotificationPriorityColor = (priority: NotificationPriority): string => {
  const colors: Record<NotificationPriority, string> = {
    Low: 'text-gray-500',
    Normal: 'text-blue-500',
    High: 'text-orange-500',
    Urgent: 'text-red-500',
  };
  return colors[priority] || 'text-gray-500';
};

export const getNotificationPriorityBg = (priority: NotificationPriority): string => {
  const colors: Record<NotificationPriority, string> = {
    Low: 'bg-gray-100',
    Normal: 'bg-blue-100',
    High: 'bg-orange-100',
    Urgent: 'bg-red-100',
  };
  return colors[priority] || 'bg-gray-100';
};
