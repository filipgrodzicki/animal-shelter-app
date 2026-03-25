import { format, parse } from 'date-fns';
import { pl } from 'date-fns/locale';
import { ClockIcon, CheckIcon } from '@heroicons/react/24/outline';
import { TimeSlot } from '@/types';
import { Spinner } from '@/components/common';
import clsx from 'clsx';

// Helper to parse TimeOnly format (HH:mm:ss) from backend
function parseTimeOnly(timeString: string): Date {
  // If it's a full ISO string, extract just the time part
  if (timeString.includes('T')) {
    return new Date(timeString);
  }
  // Parse HH:mm:ss or HH:mm format
  const timePart = timeString.split('.')[0]; // Remove milliseconds if present
  const today = new Date();
  try {
    // Try HH:mm:ss format first
    return parse(timePart, 'HH:mm:ss', today);
  } catch {
    // Fall back to HH:mm format
    return parse(timePart.substring(0, 5), 'HH:mm', today);
  }
}

interface TimeSlotPickerProps {
  slots: TimeSlot[];
  selectedSlotId: string | null;
  onSelectSlot: (slot: TimeSlot) => void;
  isLoading?: boolean;
  selectedDate: Date | null;
}

export function TimeSlotPicker({
  slots,
  selectedSlotId,
  onSelectSlot,
  isLoading = false,
  selectedDate,
}: TimeSlotPickerProps) {
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Spinner size="md" />
        <p className="mt-3 text-sm text-gray-500">Ładowanie dostępnych godzin...</p>
      </div>
    );
  }

  if (!selectedDate) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <ClockIcon className="w-12 h-12 text-gray-300 mb-3" />
        <p className="text-gray-500">Wybierz dzień z kalendarza, aby zobaczyć dostępne godziny</p>
      </div>
    );
  }

  const availableSlots = slots.filter((slot) => slot.isAvailable);

  if (availableSlots.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <ClockIcon className="w-12 h-12 text-gray-300 mb-3" />
        <p className="text-gray-500">Brak dostępnych terminów w wybranym dniu</p>
        <p className="text-sm text-gray-600 mt-1">Wybierz inny dzień z kalendarza</p>
      </div>
    );
  }

  // Group slots by morning/afternoon
  const morningSlots = availableSlots.filter((slot) => {
    const hour = parseTimeOnly(slot.startTime).getHours();
    return hour < 12;
  });

  const afternoonSlots = availableSlots.filter((slot) => {
    const hour = parseTimeOnly(slot.startTime).getHours();
    return hour >= 12;
  });

  const formatTime = (timeString: string): string => {
    return format(parseTimeOnly(timeString), 'HH:mm');
  };

  const renderSlotButton = (slot: TimeSlot) => {
    const isSelected = selectedSlotId === slot.id;
    const remainingSpots = slot.maxCapacity - slot.currentBookings;

    return (
      <button
        key={slot.id}
        onClick={() => onSelectSlot(slot)}
        disabled={!slot.isAvailable}
        className={clsx(
          'relative flex flex-col items-center justify-center p-3 rounded-lg border-2 transition-all duration-200',
          isSelected
            ? 'border-primary-600 bg-primary-50 ring-2 ring-primary-200'
            : 'border-gray-200 hover:border-primary-300 hover:bg-gray-50',
          !slot.isAvailable && 'opacity-50 cursor-not-allowed'
        )}
      >
        {isSelected && (
          <div className="absolute -top-2 -right-2 w-5 h-5 bg-primary-600 rounded-full flex items-center justify-center">
            <CheckIcon className="w-3 h-3 text-white" />
          </div>
        )}
        <span
          className={clsx(
            'text-lg font-semibold',
            isSelected ? 'text-primary-700' : 'text-gray-900'
          )}
        >
          {formatTime(slot.startTime)}
        </span>
        <span className="text-xs text-gray-500 mt-0.5">
          do {formatTime(slot.endTime)}
        </span>
        {remainingSpots <= 2 && remainingSpots > 0 && (
          <span className="text-xs text-amber-600 mt-1">
            Zostało: {remainingSpots}
          </span>
        )}
      </button>
    );
  };

  return (
    <div className="space-y-6">
      {/* Selected date header */}
      <div className="text-center pb-4 border-b border-gray-200">
        <p className="text-sm text-gray-500 mb-1">Wybrana data</p>
        <p className="text-lg font-semibold text-gray-900">
          {format(selectedDate, 'EEEE, d MMMM yyyy', { locale: pl })}
        </p>
      </div>

      {/* Morning slots */}
      {morningSlots.length > 0 && (
        <div>
          <h4 className="text-sm font-medium text-gray-700 mb-3">
            Rano
          </h4>
          <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
            {morningSlots.map(renderSlotButton)}
          </div>
        </div>
      )}

      {/* Afternoon slots */}
      {afternoonSlots.length > 0 && (
        <div>
          <h4 className="text-sm font-medium text-gray-700 mb-3">
            Po południu
          </h4>
          <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
            {afternoonSlots.map(renderSlotButton)}
          </div>
        </div>
      )}

      {/* Info */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
        <p className="text-sm text-blue-700">
          <strong>Czas trwania wizyty:</strong> około 30-45 minut. Prosimy o punktualne przybycie.
        </p>
      </div>
    </div>
  );
}
