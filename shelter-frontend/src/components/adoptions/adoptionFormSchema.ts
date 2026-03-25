import { z } from 'zod';

// Step 1: Personal Info
export const personalInfoSchema = z.object({
  firstName: z
    .string()
    .min(2, 'Imię musi mieć minimum 2 znaki')
    .max(50, 'Imię może mieć maksymalnie 50 znaków'),
  lastName: z
    .string()
    .min(2, 'Nazwisko musi mieć minimum 2 znaki')
    .max(50, 'Nazwisko może mieć maksymalnie 50 znaków'),
  email: z
    .string()
    .email('Nieprawidłowy adres email'),
  phone: z
    .string()
    .regex(/^[0-9+\s-]{9,15}$/, 'Nieprawidłowy numer telefonu'),
  dateOfBirth: z
    .string()
    .refine((date) => {
      const birthDate = new Date(date);
      const today = new Date();
      let age = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();
      if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        age--;
      }
      return age >= 18;
    }, 'Musisz mieć ukończone 18 lat'),
  street: z
    .string()
    .min(3, 'Adres musi mieć minimum 3 znaki'),
  city: z
    .string()
    .min(2, 'Miasto musi mieć minimum 2 znaki'),
  postalCode: z
    .string()
    .regex(/^\d{2}-\d{3}$/, 'Nieprawidłowy kod pocztowy (format: XX-XXX)'),
});

// Step 2: Living Conditions
export const livingConditionsSchema = z.object({
  housingType: z.enum(['apartment', 'house', 'houseWithGarden'], {
    message: 'Wybierz typ mieszkania',
  }),
  hasChildren: z.boolean(),
  childrenAges: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 100,
      'Maksymalnie 100 znaków'
    ),
  hasOtherAnimals: z.boolean(),
  otherAnimalsDescription: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 200,
      'Maksymalnie 200 znaków'
    ),
  experienceLevel: z.enum(['none', 'basic', 'intermediate', 'advanced'], {
    message: 'Wybierz poziom doświadczenia',
  }),
  availableCareTime: z.enum(['lessThan1Hour', 'oneToThreeHours', 'moreThan3Hours'], {
    message: 'Wybierz dostępny czas na opiekę',
  }),
  experienceDescription: z
    .string()
    .optional()
    .refine(
      (val) => !val || val.length <= 500,
      'Maksymalnie 500 znaków'
    ),
});

// Step 3: Motivation
export const motivationSchema = z.object({
  whyAdopt: z
    .string()
    .min(50, 'Odpowiedź musi mieć minimum 50 znaków')
    .max(1000, 'Odpowiedź może mieć maksymalnie 1000 znaków'),
  howCare: z
    .string()
    .min(50, 'Odpowiedź musi mieć minimum 50 znaków')
    .max(1000, 'Odpowiedź może mieć maksymalnie 1000 znaków'),
});

// Step 4: Consents
export const consentsSchema = z.object({
  gdprConsent: z
    .boolean()
    .refine((val) => val === true, 'Zgoda na przetwarzanie danych jest wymagana'),
  rulesConsent: z
    .boolean()
    .refine((val) => val === true, 'Akceptacja regulaminu jest wymagana'),
});

// Step 5: Summary confirmation
export const summarySchema = z.object({
  confirmSubmission: z
    .boolean()
    .refine((val) => val === true, 'Musisz potwierdzić poprawność danych'),
});

// Combined schema for the entire form
// Using .merge() instead of spread to preserve .refine() validations
export const adoptionFormSchema = personalInfoSchema
  .merge(livingConditionsSchema)
  .merge(motivationSchema)
  .merge(consentsSchema)
  .merge(summarySchema);

export type PersonalInfoData = z.infer<typeof personalInfoSchema>;
export type LivingConditionsData = z.infer<typeof livingConditionsSchema>;
export type MotivationData = z.infer<typeof motivationSchema>;
export type ConsentsData = z.infer<typeof consentsSchema>;
export type SummaryData = z.infer<typeof summarySchema>;
export type AdoptionFormData = z.infer<typeof adoptionFormSchema>;

// Default values
export const defaultFormValues: AdoptionFormData = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  dateOfBirth: '',
  street: '',
  city: '',
  postalCode: '',
  housingType: 'apartment',
  hasChildren: false,
  childrenAges: '',
  hasOtherAnimals: false,
  otherAnimalsDescription: '',
  experienceLevel: 'none',
  availableCareTime: 'oneToThreeHours',
  experienceDescription: '',
  whyAdopt: '',
  howCare: '',
  gdprConsent: false,
  rulesConsent: false,
  confirmSubmission: false,
};

// Helper labels
export const housingTypeLabels: Record<string, string> = {
  apartment: 'Mieszkanie',
  house: 'Dom',
  houseWithGarden: 'Dom z ogrodem',
};

export const experienceLevelLabels: Record<string, string> = {
  none: 'Brak doświadczenia',
  basic: 'Podstawowe',
  intermediate: 'Średniozaawansowane',
  advanced: 'Zaawansowane',
};

export const careTimeLabels: Record<string, string> = {
  lessThan1Hour: 'Poniżej godziny dziennie',
  oneToThreeHours: '1-3 godziny dziennie',
  moreThan3Hours: 'Powyżej 3 godzin dziennie',
};
