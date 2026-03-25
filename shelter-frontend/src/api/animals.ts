import { get, post, put, buildQueryString } from './client';
import {
  AnimalListItem,
  AnimalDetail,
  AnimalFilters,
  AnimalNote,
  AnimalNoteType,
  MedicalRecordAttachment,
  PagedResult,
  PaginationParams,
} from '@/types';

const BASE_URL = '/animals';

export interface GetAnimalsParams extends PaginationParams, AnimalFilters {}

export const animalsApi = {
  // Get paginated list of animals
  getAnimals: async (params: GetAnimalsParams): Promise<PagedResult<AnimalListItem>> => {
    const queryString = buildQueryString(params );
    return get<PagedResult<AnimalListItem>>(`${BASE_URL}${queryString}`);
  },

  // Get animal by ID
  getAnimal: async (id: string): Promise<AnimalDetail> => {
    return get<AnimalDetail>(`${BASE_URL}/${id}`);
  },

  // Get available animals for public view
  getAvailableAnimals: async (params: Omit<GetAnimalsParams, 'publicOnly'>): Promise<PagedResult<AnimalListItem>> => {
    const queryString = buildQueryString({ ...params, publicOnly: true } );
    return get<PagedResult<AnimalListItem>>(`${BASE_URL}${queryString}`);
  },

  // Create new animal (staff only)
  createAnimal: async (data: CreateAnimalRequest): Promise<AnimalDetail> => {
    return post<AnimalDetail>(BASE_URL, data);
  },

  // Update animal (staff only)
  updateAnimal: async (id: string, data: UpdateAnimalRequest): Promise<AnimalDetail> => {
    return put<AnimalDetail>(`${BASE_URL}/${id}`, data);
  },

  // Change animal status
  changeStatus: async (id: string, data: ChangeStatusRequest): Promise<AnimalDetail> => {
    return put<AnimalDetail>(`${BASE_URL}/${id}/status`, data);
  },

  // Upload photo
  uploadPhoto: async (id: string, file: File, isMain: boolean = false): Promise<void> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('isMain', String(isMain));

    return post(`${BASE_URL}/${id}/photos`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  // Delete photo
  deletePhoto: async (animalId: string, photoId: string): Promise<void> => {
    return post(`${BASE_URL}/${animalId}/photos/${photoId}/delete`, {});
  },

  // Get status history
  getStatusHistory: async (id: string) => {
    return get(`${BASE_URL}/${id}/status-history`);
  },

  // Get permitted actions
  getPermittedActions: async (id: string): Promise<string[]> => {
    return get<string[]>(`${BASE_URL}/${id}/permitted-actions`);
  },

  // Add medical record
  addMedicalRecord: async (id: string, data: AddMedicalRecordRequest): Promise<void> => {
    return post(`${BASE_URL}/${id}/medical-records`, data);
  },

  // Get medical records
  getMedicalRecords: async (id: string) => {
    return get(`${BASE_URL}/${id}/medical-records`);
  },

  // Upload medical record attachment (WF-06)
  uploadMedicalRecordAttachment: async (
    animalId: string,
    recordId: string,
    file: File,
    description?: string
  ): Promise<MedicalRecordAttachment> => {
    const formData = new FormData();
    formData.append('file', file);
    if (description) {
      formData.append('description', description);
    }

    return post<MedicalRecordAttachment>(
      `${BASE_URL}/${animalId}/medical-records/${recordId}/attachments`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
  },

  // Get animal notes
  getAnimalNotes: async (
    id: string,
    params?: GetAnimalNotesParams
  ): Promise<PagedResult<AnimalNote>> => {
    const queryString = buildQueryString(params || {});
    return get<PagedResult<AnimalNote>>(`${BASE_URL}/${id}/notes${queryString}`);
  },

  // Add animal note
  addAnimalNote: async (id: string, data: AddAnimalNoteRequest): Promise<AnimalNote> => {
    return post<AnimalNote>(`${BASE_URL}/${id}/notes`, data);
  },
};

// Request types
export interface CreateAnimalRequest {
  species: string;
  breed: string;
  name: string;
  ageInMonths?: number;
  gender: string;
  size: string;
  color: string;
  chipNumber?: string;
  admissionDate: string;
  admissionCircumstances: string;
  description?: string;
  experienceLevel: string;
  childrenCompatibility: string;
  animalCompatibility: string;
  spaceRequirement: string;
  careTime: string;
  // Distinguishing marks (required by regulation)
  distinguishingMarks?: string;
  // Optional data of the person surrendering the animal
  surrenderedByFirstName?: string;
  surrenderedByLastName?: string;
  surrenderedByPhone?: string;
}

export interface UpdateAnimalRequest {
  name?: string;
  ageInMonths?: number;
  description?: string;
  experienceLevel?: string;
  childrenCompatibility?: string;
  animalCompatibility?: string;
  spaceRequirement?: string;
  careTime?: string;
}

export interface ChangeStatusRequest {
  newStatus: string;
  changedBy: string;
  reason?: string;
}

export interface AddMedicalRecordRequest {
  type: string;
  title: string;
  description: string;
  recordDate: string;
  diagnosis?: string;
  treatment?: string;
  medications?: string;
  nextVisitDate?: string;
  veterinarianName?: string;
  notes?: string;
  cost?: number;
  // WF-06: Data entry person info
  enteredBy: string;
  enteredByUserId?: string;
}

export interface GetAnimalNotesParams {
  noteType?: AnimalNoteType;
  isImportant?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AddAnimalNoteRequest {
  volunteerId?: string;
  noteType: AnimalNoteType;
  title: string;
  content: string;
  isImportant?: boolean;
  observationDate?: string;
}
