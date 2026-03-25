// Animal-related types

export type Species = 'Dog' | 'Cat';
export type Gender = 'Male' | 'Female' | 'Unknown';
export type Size = 'ExtraSmall' | 'Small' | 'Medium' | 'Large' | 'ExtraLarge';
export type AnimalStatus =
  | 'Admitted'
  | 'Quarantine'
  | 'Treatment'
  | 'Available'
  | 'Reserved'
  | 'InAdoptionProcess'
  | 'Adopted'
  | 'Deceased';
export type ExperienceLevel = 'None' | 'Basic' | 'Advanced';
export type ChildrenCompatibility = 'Yes' | 'Partially' | 'No';
export type AnimalCompatibility = 'Yes' | 'Partially' | 'No';
export type SpaceRequirement = 'Apartment' | 'House' | 'HouseWithGarden';
export type CareTime = 'LessThan1Hour' | 'OneToThreeHours' | 'MoreThan3Hours';

export interface AnimalPhoto {
  id: string;
  url: string;
  thumbnailUrl?: string;
  description?: string;
  isMain: boolean;
  displayOrder: number;
}

export interface AnimalListItem {
  id: string;
  registrationNumber: string;
  name: string;
  species: Species;
  breed: string;
  ageInMonths?: number;
  gender: Gender;
  size: Size;
  status: AnimalStatus;
  mainPhotoUrl?: string;
  admissionDate: string;
  description?: string;
}

export interface AnimalDetail {
  id: string;
  registrationNumber: string;
  species: Species;
  breed: string;
  name: string;
  ageInMonths?: number;
  gender: Gender;
  size: Size;
  color: string;
  chipNumber?: string;
  distinguishingMarks?: string;
  admissionDate: string;
  admissionCircumstances: string;
  surrenderedBy?: {
    firstName: string;
    lastName: string;
    phone?: string;
  };
  status: AnimalStatus;
  description?: string;
  experienceLevel: ExperienceLevel;
  childrenCompatibility: ChildrenCompatibility;
  animalCompatibility: AnimalCompatibility;
  spaceRequirement: SpaceRequirement;
  careTime: CareTime;
  photos: AnimalPhoto[];
  statusHistory: AnimalStatusChange[];
  medicalRecords: MedicalRecord[];
  permittedActions: string[];
  createdAt: string;
  updatedAt?: string;
  // Adoption release info (for adopted animals)
  adoptionInfo?: {
    adoptionDate: string;
    releaseCircumstances?: string;
    adopter: {
      firstName: string;
      lastName: string;
      phone?: string;
      address?: string;
    };
  };
  // Additional fields for compatibility
  mainPhotoUrl?: string;
  photoUrls?: string[];
  isNeutered?: boolean;
  isVaccinated?: boolean;
  isChipped?: boolean;
  healthNotes?: string;
  behaviorNotes?: string;
  locationInShelter?: string;
}

// Alias for backward compatibility
export type Animal = AnimalDetail;

export interface AnimalStatusChange {
  id: string;
  previousStatus: AnimalStatus;
  newStatus: AnimalStatus;
  trigger: string;
  changedBy: string;
  reason?: string;
  changedAt: string;
}

export interface MedicalRecordAttachment {
  id: string;
  fileName: string;
  url: string;
  contentType?: string;
  fileSize: number;
  description?: string;
  createdAt: string;
}

export interface MedicalRecord {
  id: string;
  type: string;
  title: string;
  description: string;
  recordDate: string;
  diagnosis?: string;
  treatment?: string;
  medications?: string;
  nextVisitDate?: string;
  veterinarianName: string;
  notes?: string;
  cost?: number;
  // WF-06: Data entry person info
  enteredBy: string;
  enteredByUserId?: string;
  // WF-06: Attachments
  attachments: MedicalRecordAttachment[];
  createdAt: string;
  // Backward compatibility
  recordType?: string;
  veterinarian?: string;
  nextAppointmentDate?: string;
}

export interface AnimalFilters {
  species?: Species;
  gender?: Gender;
  size?: Size;
  status?: AnimalStatus;
  ageMin?: number;
  ageMax?: number;
  experienceLevel?: ExperienceLevel;
  childrenCompatibility?: ChildrenCompatibility;
  animalCompatibility?: AnimalCompatibility;
  spaceRequirement?: SpaceRequirement;
  careTime?: CareTime;
  searchTerm?: string;
  publicOnly?: boolean;
}

// Helper functions
export function getSpeciesLabel(species: Species): string {
  const labels: Record<Species, string> = {
    Dog: 'Pies',
    Cat: 'Kot',
  };
  return labels[species] || species;
}

export function getGenderLabel(gender: Gender): string {
  const labels: Record<Gender, string> = {
    Male: 'Samiec',
    Female: 'Samica',
    Unknown: 'Nieznana',
  };
  return labels[gender] || gender;
}

export function getSizeLabel(size: Size): string {
  const labels: Record<Size, string> = {
    ExtraSmall: 'Bardzo mały',
    Small: 'Mały',
    Medium: 'Średni',
    Large: 'Duży',
    ExtraLarge: 'Bardzo duży',
  };
  return labels[size] || size;
}

export function getStatusLabel(status: AnimalStatus): string {
  const labels: Record<AnimalStatus, string> = {
    Admitted: 'Przyjęte',
    Quarantine: 'Kwarantanna',
    Treatment: 'Leczenie',
    Available: 'Dostępny',
    Reserved: 'Zarezerwowany',
    InAdoptionProcess: 'W procesie adopcji',
    Adopted: 'Adoptowany',
    Deceased: 'Zmarły',
  };
  return labels[status] || status;
}

export function getStatusColor(status: AnimalStatus): string {
  const colors: Record<AnimalStatus, string> = {
    Admitted: 'badge-gray',
    Quarantine: 'badge-yellow',
    Treatment: 'badge-orange',
    Available: 'badge-green',
    Reserved: 'badge-blue',
    InAdoptionProcess: 'badge-blue',
    Adopted: 'badge-green',
    Deceased: 'badge-gray',
  };
  return colors[status] || 'badge-gray';
}

export function formatAge(ageInMonths?: number): string {
  if (ageInMonths === undefined || ageInMonths === null) return 'Nieznany wiek';

  if (ageInMonths < 12) {
    return `${ageInMonths} mies.`;
  }

  const years = Math.floor(ageInMonths / 12);
  const months = ageInMonths % 12;

  if (months === 0) {
    return `${years} ${years === 1 ? 'rok' : years < 5 ? 'lata' : 'lat'}`;
  }

  return `${years} ${years === 1 ? 'rok' : 'lata'} ${months} mies.`;
}

export function getExperienceLevelLabel(level: ExperienceLevel): string {
  const labels: Record<ExperienceLevel, string> = {
    None: 'Brak wymaganego doświadczenia',
    Basic: 'Podstawowe doświadczenie',
    Advanced: 'Duże doświadczenie',
  };
  return labels[level] || level;
}

export function getExperienceLevelDescription(level: ExperienceLevel): string {
  const descriptions: Record<ExperienceLevel, string> = {
    None: 'Zwierzę odpowiednie dla każdego, również dla początkujących',
    Basic: 'Wymagana podstawowa wiedza o opiece nad zwierzętami',
    Advanced: 'Wymagany doświadczony opiekun',
  };
  return descriptions[level] || '';
}

export function getChildrenCompatibilityLabel(compatibility: ChildrenCompatibility): string {
  const labels: Record<ChildrenCompatibility, string> = {
    Yes: 'Tak, idealny dla rodzin z dziećmi',
    Partially: 'Częściowo, toleruje starsze dzieci',
    No: 'Nie, niezalecany dla rodzin z dziećmi',
  };
  return labels[compatibility] || compatibility;
}

export function getChildrenCompatibilityShortLabel(compatibility: ChildrenCompatibility): string {
  const labels: Record<ChildrenCompatibility, string> = {
    Yes: 'Tak',
    Partially: 'Częściowo',
    No: 'Nie',
  };
  return labels[compatibility] || compatibility;
}

export function getAnimalCompatibilityLabel(compatibility: AnimalCompatibility): string {
  const labels: Record<AnimalCompatibility, string> = {
    Yes: 'Tak, przyjazny innym zwierzętom',
    Partially: 'Częściowo, toleruje inne zwierzęta',
    No: 'Nie, nie toleruje innych zwierząt',
  };
  return labels[compatibility] || compatibility;
}

export function getAnimalCompatibilityShortLabel(compatibility: AnimalCompatibility): string {
  const labels: Record<AnimalCompatibility, string> = {
    Yes: 'Tak',
    Partially: 'Częściowo',
    No: 'Nie',
  };
  return labels[compatibility] || compatibility;
}

export function getCareTimeLabel(careTime: CareTime): string {
  const labels: Record<CareTime, string> = {
    LessThan1Hour: 'Poniżej godziny dziennie',
    OneToThreeHours: '1-3 godziny dziennie',
    MoreThan3Hours: 'Powyżej 3 godzin dziennie',
  };
  return labels[careTime] || careTime;
}

export function getCareTimeDescription(careTime: CareTime): string {
  const descriptions: Record<CareTime, string> = {
    LessThan1Hour: 'Niezależny, wymaga niewiele uwagi',
    OneToThreeHours: 'Wymaga regularnej interakcji',
    MoreThan3Hours: 'Wymaga dużo uwagi i aktywności',
  };
  return descriptions[careTime] || '';
}

export function getSpaceRequirementLabel(space: SpaceRequirement): string {
  const labels: Record<SpaceRequirement, string> = {
    Apartment: 'Mieszkanie',
    House: 'Dom',
    HouseWithGarden: 'Dom z ogrodem',
  };
  return labels[space] || space;
}

export function getSpaceRequirementDescription(space: SpaceRequirement): string {
  const descriptions: Record<SpaceRequirement, string> = {
    Apartment: 'Może mieszkać w mieszkaniu bez dostępu do ogrodu',
    House: 'Zalecany dom z dostępem do podwórka',
    HouseWithGarden: 'Wymaga domu z dużym ogrodem',
  };
  return descriptions[space] || '';
}

export function getMedicalRecordTypeLabel(type: string): string {
  const labels: Record<string, string> = {
    Vaccination: 'Szczepienie',
    Treatment: 'Leczenie',
    Surgery: 'Operacja',
    Checkup: 'Badanie kontrolne',
    Examination: 'Badanie',
    Deworming: 'Odrobaczanie',
    Sterilization: 'Sterylizacja/Kastracja',
    Chipping: 'Czipowanie',
    Microchipping: 'Czipowanie',
    DentalCare: 'Stomatologia',
    Laboratory: 'Badania laboratoryjne',
    Other: 'Inne',
  };
  return labels[type] || type;
}

// Animal Notes
export type AnimalNoteType =
  | 'BehaviorObservation'
  | 'HealthObservation'
  | 'Feeding'
  | 'WalkActivity'
  | 'AnimalInteraction'
  | 'HumanInteraction'
  | 'Grooming'
  | 'Training'
  | 'General'
  | 'Urgent';

export interface AnimalNote {
  id: string;
  animalId: string;
  volunteerId?: string;
  authorName: string;
  noteType: AnimalNoteType;
  title: string;
  content: string;
  isImportant: boolean;
  observationDate: string;
  createdAt: string;
  updatedAt?: string;
}

export function getAnimalNoteTypeLabel(type: AnimalNoteType): string {
  const labels: Record<AnimalNoteType, string> = {
    BehaviorObservation: 'Obserwacja zachowania',
    HealthObservation: 'Obserwacja zdrowotna',
    Feeding: 'Karmienie',
    WalkActivity: 'Spacer / Aktywność',
    AnimalInteraction: 'Interakcja ze zwierzętami',
    HumanInteraction: 'Interakcja z ludźmi',
    Grooming: 'Pielęgnacja',
    Training: 'Trening',
    General: 'Ogólna',
    Urgent: 'Pilne',
  };
  return labels[type] || type;
}

export function getAnimalNoteTypeColor(type: AnimalNoteType): string {
  const colors: Record<AnimalNoteType, string> = {
    BehaviorObservation: 'blue',
    HealthObservation: 'red',
    Feeding: 'green',
    WalkActivity: 'purple',
    AnimalInteraction: 'yellow',
    HumanInteraction: 'cyan',
    Grooming: 'pink',
    Training: 'orange',
    General: 'gray',
    Urgent: 'red',
  };
  return colors[type] || 'gray';
}
