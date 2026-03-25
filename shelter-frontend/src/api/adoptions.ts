import { get, post, put, buildQueryString } from './client';
import {
  AdoptionApplicationListItem,
  AdoptionApplicationDetail,
  AdoptionFilters,
  SubmitAdoptionApplicationRequest,
  PagedResult,
  PaginationParams,
} from '@/types';

const BASE_URL = '/adoptions';

export interface GetAdoptionsParams extends PaginationParams, AdoptionFilters {}

export interface GetMyAdoptionsParams extends PaginationParams {
  status?: AdoptionApplicationStatus;
}

// Import AdoptionApplicationStatus type
import { AdoptionApplicationStatus } from '@/types';

export const adoptionsApi = {
  // Get paginated list of applications
  getApplications: async (params: GetAdoptionsParams): Promise<PagedResult<AdoptionApplicationListItem>> => {
    const queryString = buildQueryString(params );
    return get<PagedResult<AdoptionApplicationListItem>>(`${BASE_URL}${queryString}`);
  },

  // Get user's own applications
  getMyApplications: async (params: GetMyAdoptionsParams): Promise<PagedResult<AdoptionApplicationListItem>> => {
    const queryString = buildQueryString(params );
    return get<PagedResult<AdoptionApplicationListItem>>(`${BASE_URL}/my${queryString}`);
  },

  // Get application by ID
  getApplication: async (id: string): Promise<AdoptionApplicationDetail> => {
    return get<AdoptionApplicationDetail>(`${BASE_URL}/${id}?includeStatusHistory=true`);
  },

  // Submit new application (online)
  submitApplication: async (data: SubmitAdoptionApplicationRequest): Promise<SubmitApplicationResult> => {
    return post<SubmitApplicationResult>(BASE_URL, data);
  },

  // Submit walk-in application (staff)
  submitWalkInApplication: async (data: WalkInApplicationRequest): Promise<SubmitApplicationResult> => {
    return post<SubmitApplicationResult>(`${BASE_URL}/walk-in`, data);
  },

  // Take for review
  takeForReview: async (id: string, data: TakeForReviewRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/review`, data);
  },

  // Approve application
  approve: async (id: string, data: ApproveRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/approve`, data);
  },

  // Reject application
  reject: async (id: string, data: RejectRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/reject`, data);
  },

  // Schedule visit
  scheduleVisit: async (id: string, data: ScheduleVisitRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/schedule-visit`, data);
  },

  // Record visit attendance
  recordAttendance: async (id: string, data: RecordAttendanceRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/record-attendance`, data);
  },

  // Record visit result
  recordVisitResult: async (id: string, data: RecordVisitResultRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/record-visit`, data);
  },

  // Generate contract
  generateContract: async (id: string, data: GenerateContractRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/generate-contract`, data);
  },

  // Get contract PDF
  getContract: async (id: string): Promise<Blob> => {
    const response = await get<Blob>(`${BASE_URL}/${id}/contract`, {
      responseType: 'blob',
    });
    return response;
  },

  // Finalize adoption
  finalize: async (id: string, data: FinalizeRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/complete`, data);
  },

  // Cancel application
  cancel: async (id: string, data: CancelRequest): Promise<AdoptionApplicationDetail> => {
    return put<AdoptionApplicationDetail>(`${BASE_URL}/${id}/cancel`, data);
  },
};

// Request/Response types
export interface SubmitApplicationResult {
  applicationId: string;
  adopterId: string;
  animalId: string;
  applicationStatus: string;
  animalStatus: string;
  message: string;
}

export interface WalkInApplicationRequest extends SubmitAdoptionApplicationRequest {
  staffUserId: string;
  staffName: string;
  skipEmailConfirmation?: boolean;
}

export interface TakeForReviewRequest {
  reviewerUserId: string;
  reviewerName: string;
}

export interface ApproveRequest {
  reviewerName: string;
  notes?: string;
}

export interface RejectRequest {
  reviewerName: string;
  reason: string;
}

export interface ScheduleVisitRequest {
  visitDate: string;
  scheduledByName: string;
  notes?: string;
}

export interface RecordAttendanceRequest {
  conductedByUserId: string;
  conductedByName: string;
}

export interface RecordVisitResultRequest {
  isPositive: boolean;
  assessment: number;
  notes: string;
  recordedByName: string;
}

export interface GenerateContractRequest {
  generatedByName: string;
}

export interface FinalizeRequest {
  contractFilePath: string;
  signedByName: string;
}

export interface CancelRequest {
  reason: string;
  userName: string;
}
