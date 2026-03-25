import { useState, useMemo } from 'react';
import {
  format,
  startOfWeek,
  addDays,
  addWeeks,
  isSameDay,
  isToday,
  isBefore,
  startOfDay,
} from 'date-fns';
import { pl } from 'date-fns/locale';
import { ChevronLeftIcon, ChevronRightIcon } from '@heroicons/react/24/outline';
import { DayAvailability } from '@/types';
import { Spinner } from '@/components/common';
import clsx from 'clsx';

interface CalendarViewProps {
  availability: DayAvailability[];
  selectedDate: Date | null;
  onSelectDate: (date: Date) => void;
  isLoading?: boolean;
  weeksToShow?: number;
}

export function CalendarView({
  availability,
  selectedDate,
  onSelectDate,
  isLoading = false,
  weeksToShow = 4,
}: CalendarViewProps) {
  const [currentWeekStart, setCurrentWeekStart] = useState(() =>
    startOfWeek(new Date(), { weekStartsOn: 1 })
  );

  const today = startOfDay(new Date());
  const maxDate = addWeeks(today, weeksToShow);

  // Generate weeks array
  const weeks = useMemo(() => {
    const weeksArray: Date[][] = [];
    let weekStart = currentWeekStart;

    for (let w = 0; w < 2; w++) {
      const week: Date[] = [];
      for (let d = 0; d < 7; d++) {
        week.push(addDays(weekStart, d));
      }
      weeksArray.push(week);
      weekStart = addWeeks(weekStart, 1);
    }

    return weeksArray;
  }, [currentWeekStart]);

  // Create availability map for quick lookup
  const availabilityMap = useMemo(() => {
    const map = new Map<string, DayAvailability>();
    availability.forEach((day) => {
      map.set(day.date, day);
    });
    return map;
  }, [availability]);

  const getDayAvailability = (date: Date): DayAvailability | undefined => {
    const dateStr = format(date, 'yyyy-MM-dd');
    return availabilityMap.get(dateStr);
  };

  const canGoBack = !isBefore(currentWeekStart, today);
  const canGoForward = isBefore(addWeeks(currentWeekStart, 2), maxDate);

  const handlePreviousWeek = () => {
    if (canGoBack) {
      setCurrentWeekStart(addWeeks(currentWeekStart, -1));
    }
  };

  const handleNextWeek = () => {
    if (canGoForward) {
      setCurrentWeekStart(addWeeks(currentWeekStart, 1));
    }
  };

  const handleDateClick = (date: Date) => {
    const dayAvail = getDayAvailability(date);
    const hasSlots = dayAvail && (dayAvail.hasAvailability || dayAvail.availableSlots > 0);
    if (hasSlots && !isBefore(date, today)) {
      onSelectDate(date);
    }
  };

  const weekDays = ['Pon', 'Wt', 'Śr', 'Czw', 'Pt', 'Sob', 'Ndz'];

  const renderDay = (date: Date) => {
    const dayAvail = getDayAvailability(date);
    const isPast = isBefore(date, today);
    const isSelected = selectedDate && isSameDay(date, selectedDate);
    const hasSlots = dayAvail && (dayAvail.hasAvailability || dayAvail.availableSlots > 0) && !isPast;
    const isCurrentDay = isToday(date);

    return (
      <button
        key={date.toISOString()}
        onClick={() => handleDateClick(date)}
        disabled={isPast || !hasSlots}
        className={clsx(
          'relative flex flex-col items-center justify-center p-2 rounded-lg transition-all duration-200 min-h-[60px]',
          // Selected state
          isSelected && 'bg-primary-600 text-white shadow-md',
          // Available state
          !isSelected && hasSlots && 'bg-green-50 hover:bg-green-100 border border-green-200 cursor-pointer',
          // Unavailable/past state
          !isSelected && !hasSlots && 'bg-gray-50 text-gray-400 cursor-not-allowed',
          // Today indicator
          isCurrentDay && !isSelected && 'ring-2 ring-primary-300'
        )}
      >
        {/* Day number */}
        <span
          className={clsx(
            'text-lg font-semibold',
            isSelected && 'text-white',
            !isSelected && hasSlots && 'text-gray-900',
            !isSelected && !hasSlots && 'text-gray-400'
          )}
        >
          {format(date, 'd')}
        </span>

        {/* Availability indicator */}
        {hasSlots && !isSelected && dayAvail && (
          <span className="text-xs text-green-600 mt-0.5">
            {dayAvail.availableSlots} {dayAvail.availableSlots === 1 ? 'slot' : 'slotów'}
          </span>
        )}

        {/* Today badge */}
        {isCurrentDay && (
          <span
            className={clsx(
              'absolute -top-1 -right-1 text-[10px] px-1.5 py-0.5 rounded-full font-medium',
              isSelected ? 'bg-white text-primary-600' : 'bg-primary-600 text-white'
            )}
          >
            dziś
          </span>
        )}
      </button>
    );
  };

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Spinner size="md" />
        <p className="mt-3 text-sm text-gray-500">Ładowanie kalendarza...</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Month/Year header with navigation */}
      <div className="flex items-center justify-between">
        <button
          onClick={handlePreviousWeek}
          disabled={!canGoBack}
          className={clsx(
            'p-2 rounded-full transition-colors',
            canGoBack
              ? 'hover:bg-gray-100 text-gray-700'
              : 'text-gray-300 cursor-not-allowed'
          )}
        >
          <ChevronLeftIcon className="w-5 h-5" />
        </button>

        <div className="text-center">
          <h3 className="font-semibold text-gray-900">
            {format(currentWeekStart, 'LLLL yyyy', { locale: pl })}
          </h3>
          <p className="text-sm text-gray-500">
            {format(currentWeekStart, 'd MMM', { locale: pl })} -{' '}
            {format(addDays(addWeeks(currentWeekStart, 1), 6), 'd MMM', { locale: pl })}
          </p>
        </div>

        <button
          onClick={handleNextWeek}
          disabled={!canGoForward}
          className={clsx(
            'p-2 rounded-full transition-colors',
            canGoForward
              ? 'hover:bg-gray-100 text-gray-700'
              : 'text-gray-300 cursor-not-allowed'
          )}
        >
          <ChevronRightIcon className="w-5 h-5" />
        </button>
      </div>

      {/* Week days header */}
      <div className="grid grid-cols-7 gap-1">
        {weekDays.map((day, index) => (
          <div
            key={day}
            className={clsx(
              'text-center text-xs font-medium py-2',
              index >= 5 ? 'text-gray-400' : 'text-gray-600'
            )}
          >
            {day}
          </div>
        ))}
      </div>

      {/* Calendar weeks */}
      <div className="space-y-2">
        {weeks.map((week, weekIndex) => (
          <div key={weekIndex} className="grid grid-cols-7 gap-1">
            {week.map(renderDay)}
          </div>
        ))}
      </div>

      {/* Legend */}
      <div className="flex flex-wrap gap-4 pt-4 border-t border-gray-200 text-xs">
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-green-50 border border-green-200" />
          <span className="text-gray-600">Dostępne terminy</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-primary-600" />
          <span className="text-gray-600">Wybrany dzień</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-gray-50 border border-gray-200" />
          <span className="text-gray-600">Niedostępne</span>
        </div>
      </div>
    </div>
  );
}
