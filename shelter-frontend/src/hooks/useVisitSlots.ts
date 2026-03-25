import { useState, useEffect, useCallback } from 'react';
import { format, addWeeks, startOfDay } from 'date-fns';
import { visitsApi } from '@/api';
import { DayAvailability, TimeSlot } from '@/types';

interface UseVisitSlotsReturn {
  availability: DayAvailability[];
  selectedDaySlots: TimeSlot[];
  isLoadingCalendar: boolean;
  isLoadingSlots: boolean;
  error: string | null;
  selectedDate: Date | null;
  selectedSlot: TimeSlot | null;
  setSelectedDate: (date: Date | null) => void;
  setSelectedSlot: (slot: TimeSlot | null) => void;
  refetchAvailability: () => void;
}

export function useVisitSlots(
  applicationId: string,
  weeksToShow: number = 4
): UseVisitSlotsReturn {
  const [availability, setAvailability] = useState<DayAvailability[]>([]);
  const [selectedDaySlots, setSelectedDaySlots] = useState<TimeSlot[]>([]);
  const [isLoadingCalendar, setIsLoadingCalendar] = useState(true);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedDate, setSelectedDate] = useState<Date | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);

  // Fetch calendar availability
  const fetchAvailability = useCallback(async () => {
    setIsLoadingCalendar(true);
    setError(null);

    try {
      const startDate = format(startOfDay(new Date()), 'yyyy-MM-dd');
      const endDate = format(addWeeks(new Date(), weeksToShow), 'yyyy-MM-dd');

      const result = await visitsApi.getAvailableSlots({
        startDate,
        endDate,
        applicationId,
      });

      setAvailability(result);
    } catch (err) {
      console.error('Failed to fetch availability:', err);
      setError('Nie udało się pobrać dostępnych terminów');
      setAvailability([]);
    } finally {
      setIsLoadingCalendar(false);
    }
  }, [applicationId, weeksToShow]);

  // Fetch slots for selected day
  const fetchDaySlots = useCallback(async (date: Date) => {
    setIsLoadingSlots(true);
    setSelectedSlot(null);

    try {
      const dateStr = format(date, 'yyyy-MM-dd');
      const result = await visitsApi.getDaySlots(dateStr, applicationId);
      setSelectedDaySlots(result.slots || []);
    } catch (err) {
      console.error('Failed to fetch day slots:', err);
      setSelectedDaySlots([]);
    } finally {
      setIsLoadingSlots(false);
    }
  }, [applicationId]);

  // Initial fetch
  useEffect(() => {
    fetchAvailability();
  }, [fetchAvailability]);

  // Fetch slots when date changes
  useEffect(() => {
    if (selectedDate) {
      fetchDaySlots(selectedDate);
    } else {
      setSelectedDaySlots([]);
      setSelectedSlot(null);
    }
  }, [selectedDate, fetchDaySlots]);

  // Handle date selection
  const handleSetSelectedDate = useCallback((date: Date | null) => {
    setSelectedDate(date);
    setSelectedSlot(null);
  }, []);

  // Handle slot selection
  const handleSetSelectedSlot = useCallback((slot: TimeSlot | null) => {
    setSelectedSlot(slot);
  }, []);

  return {
    availability,
    selectedDaySlots,
    isLoadingCalendar,
    isLoadingSlots,
    error,
    selectedDate,
    selectedSlot,
    setSelectedDate: handleSetSelectedDate,
    setSelectedSlot: handleSetSelectedSlot,
    refetchAvailability: fetchAvailability,
  };
}
