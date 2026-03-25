// Volunteer-related types

export type VolunteerStatus = 'Candidate' | 'InTraining' | 'Active' | 'Suspended' | 'Inactive';
export type AssignmentStatus = 'Pending' | 'Confirmed' | 'Cancelled';

export interface VolunteerListItem {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  status: VolunteerStatus;
  applicationDate: string;
  totalHoursWorked: number;
  skills: string[];
}

export interface VolunteerDetail {
  id: string;
  userId?: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  age: number;
  address?: string;
  city?: string;
  postalCode?: string;
  status: VolunteerStatus;
  applicationDate: string;
  trainingStartDate?: string;
  trainingEndDate?: string;
  contractSignedDate?: string;
  contractNumber?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  skills: string[];
  availability: number[]; // DayOfWeek
  totalHoursWorked: number;
  notes?: string;
  permittedActions: string[];
  statusHistory: VolunteerStatusChange[];
  recentAttendances: AttendanceListItem[];
  createdAt: string;
  updatedAt?: string;
}

export interface VolunteerStatusChange {
  id: string;
  previousStatus: VolunteerStatus;
  newStatus: VolunteerStatus;
  trigger: string;
  changedBy: string;
  reason?: string;
  changedAt: string;
}

export interface ScheduleSlot {
  id: string;
  date: string;
  startTime: string;
  endTime: string;
  maxVolunteers: number;
  currentVolunteers: number;
  hasAvailableSpots: boolean;
  description: string;
  isActive: boolean;
  assignments?: VolunteerAssignment[];
  createdAt: string;
}

export interface VolunteerAssignment {
  id: string;
  scheduleSlotId: string;
  volunteerId: string;
  volunteerName: string;
  status: AssignmentStatus;
  assignedAt: string;
  confirmedAt?: string;
  cancelledAt?: string;
  cancellationReason?: string;
}

export interface AttendanceListItem {
  id: string;
  volunteerId: string;
  volunteerName: string;
  date: string;
  checkInTime: string;
  checkOutTime?: string;
  hoursWorked?: number;
  isApproved: boolean;
}

export interface Attendance {
  id: string;
  volunteerId: string;
  volunteerName: string;
  scheduleSlotId?: string;
  slotDescription?: string;
  checkInTime: string;
  checkOutTime?: string;
  hoursWorked?: number;
  notes?: string;
  workDescription?: string;
  isApproved: boolean;
  approvedByUserId?: string;
  approvedAt?: string;
  createdAt: string;
}

export interface VolunteerHoursReport {
  volunteerId: string;
  volunteerName: string;
  email: string;
  fromDate: string;
  toDate: string;
  attendances: AttendanceReportItem[];
  totalHoursWorked: number;
  totalDaysWorked: number;
  averageHoursPerDay: number;
  reportContent?: string;
  contentType?: string;
  fileName?: string;
}

export interface AttendanceReportItem {
  date: string;
  checkInTime: string;
  checkOutTime?: string;
  hoursWorked?: number;
  slotDescription?: string;
  workDescription?: string;
  isApproved: boolean;
}

export interface RegisterVolunteerRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  address?: string;
  city?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  skills?: string[];
  availability?: number[];
  notes?: string;
}

export interface VolunteerFilters {
  status?: VolunteerStatus;
  searchTerm?: string;
  skills?: string[];
  availableOn?: number;
}

// Helper functions
export function getVolunteerStatusLabel(status: VolunteerStatus): string {
  const labels: Record<VolunteerStatus, string> = {
    Candidate: 'Kandydat',
    InTraining: 'W szkoleniu',
    Active: 'Aktywny',
    Suspended: 'Zawieszony',
    Inactive: 'Nieaktywny',
  };
  return labels[status] || status;
}

export function getVolunteerStatusColor(status: VolunteerStatus): string {
  const colors: Record<VolunteerStatus, string> = {
    Candidate: 'badge-blue',
    InTraining: 'badge-yellow',
    Active: 'badge-green',
    Suspended: 'badge-orange',
    Inactive: 'badge-gray',
  };
  return colors[status] || 'badge-gray';
}

export function getAssignmentStatusLabel(status: AssignmentStatus): string {
  const labels: Record<AssignmentStatus, string> = {
    Pending: 'Oczekuje',
    Confirmed: 'Potwierdzone',
    Cancelled: 'Anulowane',
  };
  return labels[status] || status;
}

export function getDayOfWeekLabel(day: number): string {
  const labels: Record<number, string> = {
    0: 'Niedziela',
    1: 'Poniedziałek',
    2: 'Wtorek',
    3: 'Środa',
    4: 'Czwartek',
    5: 'Piątek',
    6: 'Sobota',
  };
  return labels[day] || '';
}

export function formatHours(hours: number): string {
  return `${hours.toFixed(1)} godz.`;
}
