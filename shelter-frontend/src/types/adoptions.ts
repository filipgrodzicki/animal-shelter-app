// Adoption-related types

export type AdoptionApplicationStatus =
  | 'Submitted'
  | 'UnderReview'
  | 'Accepted'
  | 'Rejected'
  | 'VisitScheduled'
  | 'VisitCompleted'
  | 'PendingFinalization'
  | 'Completed'
  | 'Cancelled';

export interface Adopter {
  id: string;
  userId?: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  age: number;
  address: string;
  city: string;
  postalCode: string;
  status: string;
  rodoConsentDate?: string;
  createdAt: string;
}

export interface AdoptionApplicationListItem {
  id: string;
  applicationNumber: string;
  adopterName: string;
  adopterEmail: string;
  animalName: string;
  animalSpecies: string;
  status: AdoptionApplicationStatus;
  applicationDate: string;
  scheduledVisitDate?: string;
}

export interface MatchScoreDto {
  totalScore: number;
  totalPercentage: number;
  experienceScore: number;
  spaceScore: number;
  careTimeScore: number;
  childrenScore: number;
  otherAnimalsScore: number;
  experienceWeight: number;
  spaceWeight: number;
  careTimeWeight: number;
  childrenWeight: number;
  otherAnimalsWeight: number;
}

export interface AdoptionApplicationDetail {
  id: string;
  applicationNumber: string;
  adopterId: string;
  adopterName: string;
  adopterEmail: string;
  adopterPhone: string;
  animalId: string;
  animalName: string;
  animalSpecies: string;
  status: AdoptionApplicationStatus;
  applicationDate: string;
  adoptionMotivation?: string;
  petExperience?: string;
  livingConditions?: string;
  otherPetsInfo?: string;
  scheduledVisitDate?: string;
  visitAssessment?: number;
  visitNotes?: string;
  contractNumber?: string;
  contractFilePath?: string;
  completionDate?: string;
  rejectionReason?: string;
  cancellationReason?: string;
  statusHistory: AdoptionStatusChange[];
  permittedActions: string[];
  createdAt: string;
  updatedAt?: string;
  matchScore?: MatchScoreDto;
}

export interface AdoptionStatusChange {
  id: string;
  previousStatus: AdoptionApplicationStatus;
  newStatus: AdoptionApplicationStatus;
  trigger: string;
  changedBy: string;
  reason?: string;
  notes?: string;
  changedAt: string;
}

export interface SubmitAdoptionApplicationRequest {
  animalId: string;
  // Existing adopter ID (optional)
  existingAdopterId?: string;
  // Personal data (required if no existingAdopterId)
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
  dateOfBirth?: string;
  address?: string;
  city?: string;
  postalCode?: string;
  // Consents
  rodoConsent: boolean;
  // Application details
  motivation?: string;
  livingConditions?: string;
  experience?: string;
  otherPetsInfo?: string;
  // Structured matching fields
  housingType?: string;
  hasChildren?: boolean;
  hasOtherAnimals?: boolean;
  experienceLevelApplicant?: string;
  availableCareTime?: string;
}

export interface AdoptionFilters {
  status?: AdoptionApplicationStatus;
  adopterId?: string;
  animalId?: string;
  fromDate?: string;
  toDate?: string;
  searchTerm?: string;
}

// Helper functions
export function getAdoptionStatusLabel(status: AdoptionApplicationStatus): string {
  const labels: Record<AdoptionApplicationStatus, string> = {
    Submitted: 'Złożone',
    UnderReview: 'W rozpatrywaniu',
    Accepted: 'Zaakceptowane',
    Rejected: 'Odrzucone',
    VisitScheduled: 'Wizyta zaplanowana',
    VisitCompleted: 'Wizyta zakończona',
    PendingFinalization: 'Oczekuje na finalizację',
    Completed: 'Zakończone',
    Cancelled: 'Anulowane',
  };
  return labels[status] || status;
}

export function getAdoptionStatusColor(status: AdoptionApplicationStatus): string {
  const colors: Record<AdoptionApplicationStatus, string> = {
    Submitted: 'badge-blue',
    UnderReview: 'badge-yellow',
    Accepted: 'badge-green',
    Rejected: 'badge-red',
    VisitScheduled: 'badge-blue',
    VisitCompleted: 'badge-green',
    PendingFinalization: 'badge-yellow',
    Completed: 'badge-green',
    Cancelled: 'badge-gray',
  };
  return colors[status] || 'badge-gray';
}

export function getAdoptionStatusStep(status: AdoptionApplicationStatus): number {
  const steps: Record<AdoptionApplicationStatus, number> = {
    Submitted: 1,
    UnderReview: 2,
    Accepted: 3,
    VisitScheduled: 4,
    VisitCompleted: 5,
    PendingFinalization: 6,
    Completed: 7,
    Rejected: -1,
    Cancelled: -1,
  };
  return steps[status] || 0;
}
