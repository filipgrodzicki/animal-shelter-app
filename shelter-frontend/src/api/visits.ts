import { get, post, buildQueryString } from './client';
import {
  DayAvailability,
  TimeSlot,
  VisitSlotRequest,
  VisitConfirmation,
  GetAvailableSlotsParams,
} from '@/types';

const BASE_URL = '/appointments';

// Helper to transform backend slot to frontend format
function transformSlot(slot: Record<string, unknown>): TimeSlot {
  return {
    id: String(slot.id || ''),
    date: String(slot.date || ''),
    startTime: String(slot.startTime || ''),
    endTime: String(slot.endTime || ''),
    maxCapacity: Number(slot.maxCapacity || 0),
    currentBookings: Number(slot.currentBookings || 0),
    remainingCapacity: Number(slot.remainingCapacity || 0),
    isAvailable: Boolean(slot.isAvailable),
    notes: slot.notes ? String(slot.notes) : undefined,
  };
}

// Helper to transform backend availability to frontend format
function transformAvailability(day: Record<string, unknown>): DayAvailability {
  const slots = Array.isArray(day.slots) ? day.slots.map(transformSlot) : [];
  return {
    date: String(day.date || ''),
    totalSlots: Number(day.totalSlots || 0),
    availableSlots: Number(day.availableSlots || 0),
    totalCapacity: Number(day.totalCapacity || 0),
    remainingCapacity: Number(day.remainingCapacity || 0),
    slots,
    hasAvailability: Number(day.availableSlots || 0) > 0,
  };
}

export const visitsApi = {
  // Get available slots for a date range
  getAvailableSlots: async (params: GetAvailableSlotsParams): Promise<DayAvailability[]> => {
    // Backend expects 'from' and 'to' instead of 'startDate' and 'endDate'
    const queryParams: Record<string, string> = {
      from: params.startDate,
      to: params.endDate,
    };
    const queryString = buildQueryString(queryParams);
    const result = await get<Record<string, unknown>[]>(`${BASE_URL}/available${queryString}`);
    return result.map(transformAvailability);
  },

  // Get slots for a specific day (uses the same available endpoint with date range)
  getDaySlots: async (date: string, _applicationId?: string): Promise<DayAvailability> => {
    // Use the available endpoint with same start/end date
    const queryParams = { from: date, to: date };
    const queryString = buildQueryString(queryParams);
    const result = await get<Record<string, unknown>[]>(`${BASE_URL}/available${queryString}`);
    // Return the first day or an empty availability
    if (result.length > 0) {
      return transformAvailability(result[0]);
    }
    return {
      date,
      slots: [],
      totalSlots: 0,
      availableSlots: 0,
      totalCapacity: 0,
      remainingCapacity: 0,
      hasAvailability: false
    };
  },

  // Book a visit slot
  bookSlot: async (data: VisitSlotRequest): Promise<VisitConfirmation> => {
    // Backend expects slotId and applicationId in different format
    const bookingData = {
      slotId: data.slotId,
      applicationId: data.applicationId,
    };
    const result = await post<Record<string, unknown>>(`${BASE_URL}/bookings`, bookingData);
    // Transform to frontend format
    return {
      bookingId: String(result.bookingId || ''),
      slotId: String(result.slotId || ''),
      scheduledDate: String(result.date || ''),
      startTime: String(result.startTime || ''),
      endTime: String(result.endTime || ''),
      adopterName: String(result.adopterName || ''),
      animalName: String(result.animalName || ''),
      message: String(result.message || ''),
      confirmationNumber: String(result.bookingId || '').slice(0, 8).toUpperCase(),
    };
  },

  // Get visit confirmation details
  getConfirmation: async (confirmationId: string): Promise<VisitConfirmation> => {
    const result = await get<Record<string, unknown>>(`${BASE_URL}/bookings/${confirmationId}`);
    return {
      bookingId: String(result.bookingId || result.id || ''),
      slotId: String(result.slotId || ''),
      scheduledDate: String(result.date || ''),
      startTime: String(result.startTime || ''),
      endTime: String(result.endTime || ''),
      adopterName: String(result.adopterName || ''),
      animalName: String(result.animalName || ''),
      message: String(result.message || ''),
      confirmationNumber: String(result.bookingId || result.id || '').slice(0, 8).toUpperCase(),
    };
  },

  // Cancel a booked visit
  cancelVisit: async (bookingId: string, reason: string, cancelledBy: string = 'Pracownik'): Promise<void> => {
    return post(`${BASE_URL}/bookings/${bookingId}/cancel`, { reason, cancelledBy });
  },
};
