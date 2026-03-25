import { apiClient, get, post, put, buildQueryString } from './client';
import {
  VolunteerListItem,
  VolunteerDetail,
  VolunteerFilters,
  RegisterVolunteerRequest,
  ScheduleSlot,
  Attendance,
  AttendanceListItem,
  VolunteerHoursReport,
  PagedResult,
  PaginationParams,
} from '@/types';

const BASE_URL = '/volunteers';
const SCHEDULE_URL = '/schedule';
const ATTENDANCE_URL = '/attendance';

export interface GetVolunteersParams extends PaginationParams, VolunteerFilters {}

export const volunteersApi = {
  // Get paginated list of volunteers
  getVolunteers: async (params: GetVolunteersParams): Promise<PagedResult<VolunteerListItem>> => {
    const queryString = buildQueryString(params );
    return get<PagedResult<VolunteerListItem>>(`${BASE_URL}${queryString}`);
  },

  // Get volunteer by ID
  getVolunteer: async (id: string): Promise<VolunteerDetail> => {
    return get<VolunteerDetail>(`${BASE_URL}/${id}`);
  },

  // Get volunteer for current logged-in user
  getMyVolunteer: async (): Promise<VolunteerDetail> => {
    return get<VolunteerDetail>(`${BASE_URL}/me`);
  },

  // Register new volunteer
  register: async (data: RegisterVolunteerRequest): Promise<VolunteerDetail> => {
    return post<VolunteerDetail>(BASE_URL, data);
  },

  // Approve volunteer application
  approve: async (id: string, data: ApproveVolunteerRequest): Promise<VolunteerDetail> => {
    return put<VolunteerDetail>(`${BASE_URL}/${id}/approve`, data);
  },

  // Reject volunteer application
  reject: async (id: string, data: RejectVolunteerRequest): Promise<VolunteerDetail> => {
    return put<VolunteerDetail>(`${BASE_URL}/${id}/reject`, data);
  },

  // Complete training
  completeTraining: async (id: string, data: CompleteTrainingRequest): Promise<VolunteerDetail> => {
    return put<VolunteerDetail>(`${BASE_URL}/${id}/complete-training`, data);
  },

  // Suspend volunteer
  suspend: async (id: string, data: SuspendVolunteerRequest): Promise<VolunteerDetail> => {
    return put<VolunteerDetail>(`${BASE_URL}/${id}/suspend`, data);
  },

  // Resume volunteer
  resume: async (id: string, data: ResumeVolunteerRequest): Promise<VolunteerDetail> => {
    return put<VolunteerDetail>(`${BASE_URL}/${id}/resume`, data);
  },

  // Get hours report
  getHoursReport: async (id: string, fromDate: string, toDate: string, format: string = 'json'): Promise<VolunteerHoursReport> => {
    const queryString = buildQueryString({ fromDate, toDate, format });
    return get<VolunteerHoursReport>(`${BASE_URL}/${id}/hours-report${queryString}`);
  },

  // Get hours summary for all volunteers
  getHoursSummary: async (fromDate: string, toDate: string, status?: string) => {
    const queryString = buildQueryString({ fromDate, toDate, status });
    return get(`${BASE_URL}/hours-summary${queryString}`);
  },

  // Download volunteer certificate (PDF)
  downloadCertificate: async (id: string, fromDate: string, toDate: string): Promise<Blob> => {
    const queryString = buildQueryString({ fromDate, toDate });
    const response = await apiClient.get(`${BASE_URL}/${id}/certificate${queryString}`, {
      responseType: 'blob',
    });
    return response.data;
  },
};

export const scheduleApi = {
  // Get schedule
  getSchedule: async (fromDate: string, toDate: string, activeOnly: boolean = true): Promise<ScheduleSlot[]> => {
    const queryString = buildQueryString({ fromDate, toDate, activeOnly, includeAssignments: true });
    return get<ScheduleSlot[]>(`${SCHEDULE_URL}${queryString}`);
  },

  // Get my schedule (for volunteer)
  getMySchedule: async (volunteerId: string, fromDate?: string, toDate?: string) => {
    const queryString = buildQueryString({ volunteerId, fromDate, toDate });
    return get(`${SCHEDULE_URL}/my${queryString}`);
  },

  // Create slot
  createSlot: async (data: CreateSlotRequest): Promise<ScheduleSlot> => {
    return post<ScheduleSlot>(`${SCHEDULE_URL}/slots`, data);
  },

  // Create slots in bulk
  createSlotsBulk: async (data: CreateSlotsBulkRequest) => {
    return post(`${SCHEDULE_URL}/slots/bulk`, data);
  },

  // Assign volunteer to slot
  assignVolunteer: async (slotId: string, data: AssignVolunteerRequest) => {
    return post(`${SCHEDULE_URL}/slots/${slotId}/assign`, data);
  },

  // Confirm assignment
  confirmAssignment: async (slotId: string, assignmentId: string) => {
    return put(`${SCHEDULE_URL}/slots/${slotId}/assignments/${assignmentId}/confirm`, {});
  },

  // Cancel assignment
  cancelAssignment: async (slotId: string, assignmentId: string, reason: string) => {
    return put(`${SCHEDULE_URL}/slots/${slotId}/assignments/${assignmentId}/cancel`, { reason });
  },
};

export interface GetVolunteerAttendancesParams {
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export const attendanceApi = {
  // Get current active attendance (not checked out yet)
  getCurrentAttendance: async (volunteerId: string): Promise<Attendance | null> => {
    return get<Attendance | null>(`${ATTENDANCE_URL}/current/${volunteerId}`);
  },

  // Get volunteer attendance history
  getVolunteerAttendances: async (
    volunteerId: string,
    params?: GetVolunteerAttendancesParams
  ): Promise<PagedResult<AttendanceListItem>> => {
    const queryString = buildQueryString(params || {});
    return get<PagedResult<AttendanceListItem>>(`${ATTENDANCE_URL}/volunteer/${volunteerId}${queryString}`);
  },

  // Check in
  checkIn: async (data: CheckInRequest) => {
    return post(`${ATTENDANCE_URL}/check-in`, data);
  },

  // Check out
  checkOut: async (data: CheckOutRequest) => {
    return post(`${ATTENDANCE_URL}/check-out`, data);
  },

  // Approve attendance
  approve: async (id: string, approvedByUserId: string): Promise<Attendance> => {
    return put<Attendance>(`${ATTENDANCE_URL}/${id}/approve`, { approvedByUserId });
  },

  // Correct attendance
  correct: async (id: string, data: CorrectAttendanceRequest): Promise<Attendance> => {
    return put<Attendance>(`${ATTENDANCE_URL}/${id}/correct`, data);
  },

  // Manual entry
  manualEntry: async (data: ManualAttendanceRequest): Promise<Attendance> => {
    return post<Attendance>(`${ATTENDANCE_URL}/manual`, data);
  },
};

// Request types
export interface ApproveVolunteerRequest {
  approvedByUserId: string;
  approvedByName: string;
  trainingStartDate?: string;
  notes?: string;
}

export interface RejectVolunteerRequest {
  rejectedByName: string;
  reason: string;
}

export interface CompleteTrainingRequest {
  completedByName: string;
  contractNumber: string;
  trainingEndDate?: string;
}

export interface SuspendVolunteerRequest {
  suspendedByName: string;
  reason: string;
}

export interface ResumeVolunteerRequest {
  resumedByName: string;
  notes?: string;
}

export interface CreateSlotRequest {
  date: string;
  startTime: string;
  endTime: string;
  maxVolunteers: number;
  description: string;
  createdByUserId: string;
}

export interface CreateSlotsBulkRequest {
  startDate: string;
  endDate: string;
  daysOfWeek: number[];
  startTime: string;
  endTime: string;
  maxVolunteers: number;
  description: string;
  createdByUserId: string;
}

export interface AssignVolunteerRequest {
  volunteerId: string;
  assignedByUserId: string;
}

export interface CheckInRequest {
  volunteerId: string;
  scheduleSlotId?: string;
  notes?: string;
}

export interface CheckOutRequest {
  attendanceId: string;
  workDescription?: string;
}

export interface CorrectAttendanceRequest {
  checkInTime?: string;
  checkOutTime?: string;
  correctionNotes?: string;
}

export interface ManualAttendanceRequest {
  volunteerId: string;
  scheduleSlotId?: string;
  checkInTime: string;
  checkOutTime: string;
  workDescription?: string;
  notes?: string;
  enteredByUserId: string;
}
