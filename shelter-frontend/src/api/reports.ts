import { apiClient } from './client';

// ============================================
// Types
// ============================================

export interface ShelterStatistics {
  fromDate: string;
  toDate: string;
  adoptions: AdoptionStatistics;
  volunteers: VolunteerStatistics;
  animals: AnimalStatistics;
}

export interface AdoptionStatistics {
  totalApplications: number;
  completedAdoptions: number;
  rejectedApplications: number;
  cancelledApplications: number;
  pendingApplications: number;
  averageProcessingDays: number;
  byMonth: AdoptionsByMonth[];
  bySpecies: AdoptionsBySpecies[];
}

export interface AdoptionsByMonth {
  year: number;
  month: number;
  monthName: string;
  applicationsCount: number;
  completedCount: number;
}

export interface AdoptionsBySpecies {
  species: string;
  speciesLabel: string;
  count: number;
}

export interface VolunteerStatistics {
  totalActiveVolunteers: number;
  newVolunteersInPeriod: number;
  totalHoursWorked: number;
  averageHoursPerVolunteer: number;
  totalAttendances: number;
  byMonth: VolunteerActivityByMonth[];
  topVolunteers: TopVolunteer[];
}

export interface VolunteerActivityByMonth {
  year: number;
  month: number;
  monthName: string;
  activeVolunteers: number;
  hoursWorked: number;
  attendanceCount: number;
}

export interface TopVolunteer {
  volunteerId: string;
  name: string;
  hoursWorked: number;
  daysWorked: number;
}

export interface AnimalStatistics {
  totalAnimalsInShelter: number;
  admissionsInPeriod: number;
  adoptionsInPeriod: number;
  deceasedInPeriod: number;
  bySpecies: AnimalsBySpecies[];
  byStatus: AnimalsByStatus[];
}

export interface AnimalsBySpecies {
  species: string;
  speciesLabel: string;
  count: number;
}

export interface AnimalsByStatus {
  status: string;
  statusLabel: string;
  count: number;
}

export interface GovernmentReport {
  fromDate: string;
  toDate: string;
  reportPeriod: string;
  shelterInfo: ShelterInfo;
  summary: AdmissionsAndAdoptionsSummary;
  admissions: AdmissionRecord[];
  adoptions: AdoptionRecord[];
  volunteerHours: VolunteerHoursSummary;
  reportContent?: string;
  contentType?: string;
  fileName?: string;
}

export interface ShelterInfo {
  name: string;
  address: string;
  city: string;
  postalCode: string;
  phone: string;
  email: string;
  nip?: string;
  regon?: string;
}

export interface AdmissionsAndAdoptionsSummary {
  totalAdmissions: number;
  admissionsDogs: number;
  admissionsCats: number;
  admissionsOther: number;
  totalAdoptions: number;
  adoptionsDogs: number;
  adoptionsCats: number;
  adoptionsOther: number;
  totalDeceased: number;
  currentPopulation: number;
}

export interface AdmissionRecord {
  registrationNumber: string;
  name: string;
  species: string;
  breed: string;
  gender: string;
  admissionDate: string;
  admissionCircumstances: string;
  currentStatus: string;
}

export interface AdoptionRecord {
  registrationNumber: string;
  animalName: string;
  species: string;
  breed: string;
  adoptionDate: string;
  contractNumber: string;
  adopterInitials: string;
  adopterCity: string;
}

export interface VolunteerHoursSummary {
  totalVolunteers: number;
  totalHoursWorked: number;
  totalDaysWorked: number;
  records: VolunteerHoursRecord[];
}

export interface VolunteerHoursRecord {
  volunteerName: string;
  email: string;
  hoursWorked: number;
  daysWorked: number;
  attendanceCount: number;
}

// ============================================
// API Functions
// ============================================

/**
 * Pobiera ogólne statystyki schroniska (WF-32)
 */
export async function getStatistics(
  fromDate: string,
  toDate: string
): Promise<ShelterStatistics> {
  const response = await apiClient.get<ShelterStatistics>('/reports/statistics', {
    params: { fromDate, toDate },
  });
  return response.data;
}

/**
 * Pobiera raport dla samorządu (WF-33)
 */
export async function getGovernmentReport(
  fromDate: string,
  toDate: string,
  format: 'json' | 'csv' | 'pdf' = 'json'
): Promise<GovernmentReport | Blob> {
  if (format === 'json') {
    const response = await apiClient.get<GovernmentReport>('/reports/government', {
      params: { fromDate, toDate, format },
    });
    return response.data;
  } else {
    const response = await apiClient.get('/reports/government', {
      params: { fromDate, toDate, format },
      responseType: 'blob',
    });
    return response.data;
  }
}

/**
 * Pobiera raport przyjęć w formacie CSV
 */
export async function downloadAdmissionsCsv(
  fromDate: string,
  toDate: string
): Promise<Blob> {
  const response = await apiClient.get('/reports/admissions/csv', {
    params: { fromDate, toDate },
    responseType: 'blob',
  });
  return response.data;
}

/**
 * Pobiera raport adopcji w formacie CSV
 */
export async function downloadAdoptionsCsv(
  fromDate: string,
  toDate: string
): Promise<Blob> {
  const response = await apiClient.get('/reports/adoptions/csv', {
    params: { fromDate, toDate },
    responseType: 'blob',
  });
  return response.data;
}

/**
 * Pobiera ewidencję godzin wolontariuszy w formacie CSV
 */
export async function downloadVolunteerHoursCsv(
  fromDate: string,
  toDate: string
): Promise<Blob> {
  const response = await apiClient.get('/reports/volunteer-hours/csv', {
    params: { fromDate, toDate },
    responseType: 'blob',
  });
  return response.data;
}

/**
 * Pobiera pełny raport samorządowy w formacie PDF
 */
export async function downloadGovernmentReportPdf(
  fromDate: string,
  toDate: string
): Promise<Blob> {
  const response = await apiClient.get('/reports/government', {
    params: { fromDate, toDate, format: 'pdf' },
    responseType: 'blob',
  });
  return response.data;
}

/**
 * Helper do pobierania pliku
 */
export function downloadFile(blob: Blob, fileName: string): void {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', fileName);
  document.body.appendChild(link);
  link.click();
  link.parentNode?.removeChild(link);
  window.URL.revokeObjectURL(url);
}
