import { useState, useEffect } from 'react';
import { Attendance } from '@/types';

interface TimeTrackerProps {
  currentAttendance: Attendance | null;
  onCheckIn: () => void;
  onCheckOut: (workDescription?: string) => void;
}

export function TimeTracker({ currentAttendance, onCheckIn, onCheckOut }: TimeTrackerProps) {
  const [elapsedTime, setElapsedTime] = useState<string>('00:00:00');
  const [showCheckOutModal, setShowCheckOutModal] = useState(false);
  const [workDescription, setWorkDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!currentAttendance) {
      setElapsedTime('00:00:00');
      return;
    }

    const updateElapsed = () => {
      const start = new Date(currentAttendance.checkInTime).getTime();
      const now = Date.now();
      const diff = now - start;

      const hours = Math.floor(diff / (1000 * 60 * 60));
      const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
      const seconds = Math.floor((diff % (1000 * 60)) / 1000);

      setElapsedTime(
        `${hours.toString().padStart(2, '0')}:${minutes
          .toString()
          .padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
      );
    };

    updateElapsed();
    const interval = setInterval(updateElapsed, 1000);

    return () => clearInterval(interval);
  }, [currentAttendance]);

  const handleCheckOut = async () => {
    setIsSubmitting(true);
    try {
      await onCheckOut(workDescription || undefined);
      setShowCheckOutModal(false);
      setWorkDescription('');
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleString('pl-PL', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="bg-white rounded-lg shadow-sm p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">Ewidencja czasu pracy</h2>

      {currentAttendance ? (
        <div className="text-center">
          {/* Active session */}
          <div className="mb-6">
            <p className="text-sm text-gray-500 mb-2">Aktywna sesja od</p>
            <p className="text-lg font-medium text-gray-700">
              {formatDateTime(currentAttendance.checkInTime)}
            </p>
          </div>

          {/* Timer */}
          <div className="mb-6">
            <p className="text-sm text-gray-500 mb-2">Czas pracy</p>
            <div className="text-5xl font-mono font-bold text-primary-600">{elapsedTime}</div>
          </div>

          {/* Check out button */}
          <button
            onClick={() => setShowCheckOutModal(true)}
            className="btn btn-danger px-8 py-3 text-lg"
          >
            Wymelduj sie
          </button>
        </div>
      ) : (
        <div className="text-center">
          {/* No active session */}
          <div className="mb-6">
            <div className="inline-flex items-center justify-center w-20 h-20 rounded-full bg-gray-100 mb-4">
              <svg
                className="w-10 h-10 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <p className="text-gray-600">Nie jestes obecnie zameldowany</p>
          </div>

          {/* Check in button */}
          <button onClick={onCheckIn} className="btn btn-primary px-8 py-3 text-lg">
            Zamelduj sie
          </button>
        </div>
      )}

      {/* Check out modal */}
      {showCheckOutModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Wymeldowanie</h3>
            <p className="text-gray-600 mb-4">
              Czas pracy: <span className="font-mono font-semibold">{elapsedTime}</span>
            </p>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Opis wykonanej pracy (opcjonalnie)
              </label>
              <textarea
                value={workDescription}
                onChange={(e) => setWorkDescription(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                rows={3}
                placeholder="Np. Spacery z psami, karmienie kotow..."
              />
            </div>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setShowCheckOutModal(false)}
                className="btn btn-secondary"
                disabled={isSubmitting}
              >
                Anuluj
              </button>
              <button
                onClick={handleCheckOut}
                className="btn btn-primary"
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Wymeldowywanie...' : 'Wymelduj'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
