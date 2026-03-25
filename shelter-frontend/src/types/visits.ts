// Visit scheduling types - matches backend DTOs

export interface TimeSlot {
  id: string;
  date: string; // YYYY-MM-DD format (from DateOnly)
  startTime: string; // HH:mm:ss format (from TimeOnly)
  endTime: string;
  maxCapacity: number;
  currentBookings: number;
  remainingCapacity: number;
  isAvailable: boolean;
  notes?: string;
}

export interface DayAvailability {
  date: string; // YYYY-MM-DD format
  totalSlots: number;
  availableSlots: number;
  totalCapacity: number;
  remainingCapacity: number;
  slots: TimeSlot[];
  // Computed for convenience
  hasAvailability?: boolean;
}

export interface WeekAvailability {
  weekStart: string;
  weekEnd: string;
  days: DayAvailability[];
}

export interface VisitSlotRequest {
  applicationId: string;
  slotId: string;
  notes?: string;
}

// Booking result from backend
export interface VisitConfirmation {
  bookingId: string;
  slotId: string;
  scheduledDate: string; // date from backend
  startTime: string;
  endTime: string;
  adopterName: string;
  animalName: string;
  message: string;
  // Computed
  confirmationNumber: string;
}

export interface GetAvailableSlotsParams {
  startDate: string;
  endDate: string;
  applicationId?: string;
}
