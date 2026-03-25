import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { scheduleApi } from '@/api/volunteers';
import { getAssignmentStatusLabel } from '@/types';

interface ScheduleItem {
  slotId: string;
  assignmentId: string;
  date: string;
  startTime: string;
  endTime: string;
  description: string;
  assignmentStatus: string;
  confirmedAt?: string;
  hasAttendance: boolean;
  hoursWorked?: number;
}

interface VolunteerScheduleProps {
  volunteerId: string;
}

export function VolunteerSchedule({ volunteerId }: VolunteerScheduleProps) {
  const [scheduleItems, setScheduleItems] = useState<ScheduleItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [cancelModal, setCancelModal] = useState<{ slotId: string; assignmentId: string } | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    loadSchedule();
  }, [volunteerId]);

  const loadSchedule = async () => {
    setIsLoading(true);
    try {
      const data = await scheduleApi.getMySchedule(volunteerId);
      setScheduleItems(data as ScheduleItem[]);
    } catch (err) {
      console.error('Error loading schedule:', err);
      toast.error('Nie udalo sie zaladowac grafiku');
    } finally {
      setIsLoading(false);
    }
  };

  const handleConfirm = async (slotId: string, assignmentId: string) => {
    try {
      await scheduleApi.confirmAssignment(slotId, assignmentId);
      toast.success('Dyzur potwierdzony!');
      loadSchedule();
    } catch (err) {
      console.error('Error confirming assignment:', err);
      toast.error('Blad podczas potwierdzania dyzuru');
    }
  };

  const handleCancel = async () => {
    if (!cancelModal || !cancelReason.trim()) return;
    setIsSubmitting(true);
    try {
      await scheduleApi.cancelAssignment(cancelModal.slotId, cancelModal.assignmentId, cancelReason);
      toast.success('Dyzur anulowany');
      setCancelModal(null);
      setCancelReason('');
      loadSchedule();
    } catch (err) {
      console.error('Error cancelling assignment:', err);
      toast.error('Blad podczas anulowania dyzuru');
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatDate = (dateString: string) => {
    const [year, month, day] = dateString.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    return date.toLocaleDateString('pl-PL', {
      weekday: 'long',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  };

  const formatTime = (timeString: string) => {
    if (timeString.includes('T')) {
      return new Date(timeString).toLocaleTimeString('pl-PL', {
        hour: '2-digit',
        minute: '2-digit',
      });
    }
    const [hours, minutes] = timeString.split(':');
    return `${hours}:${minutes}`;
  };

  const getStatusBadge = (status: string, hasAttendance: boolean) => {
    if (hasAttendance) {
      return (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
          Zrealizowany
        </span>
      );
    }
    switch (status) {
      case 'Confirmed':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
            {getAssignmentStatusLabel(status as 'Confirmed')}
          </span>
        );
      case 'Pending':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
            {getAssignmentStatusLabel(status as 'Pending')}
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
            {getAssignmentStatusLabel(status as 'Cancelled')}
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
            {status}
          </span>
        );
    }
  };

  const isUpcoming = (dateString: string) => {
    const [year, month, day] = dateString.split('-').map(Number);
    const slotDate = new Date(year, month - 1, day);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return slotDate >= today;
  };

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow-sm p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Grafik dyzurow</h2>
        <div className="flex justify-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary-600" />
        </div>
      </div>
    );
  }

  if (scheduleItems.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-sm p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Grafik dyzurow</h2>
        <div className="text-center py-8 text-gray-500">
          <svg
            className="w-12 h-12 mx-auto mb-4 text-gray-300"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
          <p>Brak przypisanych dyzurow</p>
          <p className="text-sm mt-1">Skontaktuj sie z administratorem, aby zostac przypisanym do grafiku.</p>
        </div>
      </div>
    );
  }

  const upcomingItems = scheduleItems.filter((item) => isUpcoming(item.date));
  const pastItems = scheduleItems.filter((item) => !isUpcoming(item.date));

  return (
    <div className="space-y-6">
      {/* Upcoming duties */}
      <div className="bg-white rounded-lg shadow-sm p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">
          Nadchodzace dyzury ({upcomingItems.length})
        </h2>
        {upcomingItems.length === 0 ? (
          <p className="text-gray-500 text-center py-4">Brak nadchodzacych dyzurow</p>
        ) : (
          <div className="space-y-3">
            {upcomingItems.map((item) => (
              <div
                key={item.assignmentId}
                className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors"
              >
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      <span className="text-sm font-medium text-gray-900">
                        {formatDate(item.date)}
                      </span>
                    </div>
                    <div className="flex items-center gap-2 mb-1">
                      <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      <span className="text-sm text-gray-600">
                        {formatTime(item.startTime)} - {formatTime(item.endTime)}
                      </span>
                    </div>
                    <p className="text-sm text-gray-500 ml-6">{item.description}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    {getStatusBadge(item.assignmentStatus, item.hasAttendance)}
                    {item.assignmentStatus === 'Pending' && (
                      <button
                        onClick={() => handleConfirm(item.slotId, item.assignmentId)}
                        className="btn btn-primary text-xs px-3 py-1"
                      >
                        Potwierdz
                      </button>
                    )}
                    {(item.assignmentStatus === 'Pending' || item.assignmentStatus === 'Confirmed') && (
                      <button
                        onClick={() => setCancelModal({ slotId: item.slotId, assignmentId: item.assignmentId })}
                        className="btn btn-secondary text-xs px-3 py-1"
                      >
                        Anuluj
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Past duties */}
      {pastItems.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">
            Poprzednie dyzury ({pastItems.length})
          </h2>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Data
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Godziny
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Opis
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Przepracowano
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {pastItems.map((item) => (
                  <tr key={item.assignmentId} className="hover:bg-gray-50">
                    <td className="px-4 py-3 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {formatDate(item.date)}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {formatTime(item.startTime)} - {formatTime(item.endTime)}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="text-sm text-gray-500">{item.description}</div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {item.hoursWorked != null ? `${item.hoursWorked.toFixed(1)} godz.` : '-'}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap">
                      {getStatusBadge(item.assignmentStatus, item.hasAttendance)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Cancel modal */}
      {cancelModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Anulowanie dyzuru</h3>
            <p className="text-gray-600 mb-4">
              Podaj powod anulowania dyzuru:
            </p>
            <div className="mb-4">
              <textarea
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                rows={3}
                placeholder="Np. Zmiana planow, choroba..."
              />
            </div>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => {
                  setCancelModal(null);
                  setCancelReason('');
                }}
                className="btn btn-secondary"
                disabled={isSubmitting}
              >
                Cofnij
              </button>
              <button
                onClick={handleCancel}
                className="btn btn-danger"
                disabled={isSubmitting || !cancelReason.trim()}
              >
                {isSubmitting ? 'Anulowanie...' : 'Anuluj dyzur'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
