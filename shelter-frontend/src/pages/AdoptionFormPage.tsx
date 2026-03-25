import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeftIcon, ArrowRightIcon, PaperAirplaneIcon } from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Spinner } from '@/components/common';
import {
  FormStepper,
  Step,
  PersonalInfoStep,
  LivingConditionsStep,
  MotivationStep,
  ConsentsStep,
  SummaryStep,
  adoptionFormSchema,
  personalInfoSchema,
  livingConditionsSchema,
  motivationSchema,
  consentsSchema,
  summarySchema,
  AdoptionFormData,
  defaultFormValues,
  housingTypeLabels,
  experienceLevelLabels,
} from '@/components/adoptions';
import { useAnimal } from '@/hooks';
import { useAuth } from '@/context/AuthContext';
import { adoptionsApi } from '@/api';
import toast from 'react-hot-toast';

const steps: Step[] = [
  { id: 1, name: 'Dane osobowe', description: 'Twoje dane kontaktowe' },
  { id: 2, name: 'Warunki', description: 'Informacje o domu' },
  { id: 3, name: 'Motywacja', description: 'Dlaczego chcesz adoptować' },
  { id: 4, name: 'Zgody', description: 'Oświadczenia prawne' },
  { id: 5, name: 'Podsumowanie', description: 'Weryfikacja danych' },
];

// Schema for each step
const stepSchemas = [
  personalInfoSchema,
  livingConditionsSchema,
  motivationSchema,
  consentsSchema,
  summarySchema, // Summary step with confirmation checkbox
];

export function AdoptionFormPage() {
  const { animalId } = useParams<{ animalId: string }>();
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { animal, isLoading: isLoadingAnimal } = useAnimal(animalId);
  const { user } = useAuth();

  const {
    register,
    handleSubmit,
    watch,
    trigger,
    getValues,
    setValue,
    formState: { errors },
  } = useForm<AdoptionFormData>({
    resolver: zodResolver(adoptionFormSchema),
    defaultValues: defaultFormValues,
    mode: 'onChange',
  });

  // Auto-fill logged-in user data
  useEffect(() => {
    if (user) {
      setValue('firstName', user.firstName);
      setValue('lastName', user.lastName);
      setValue('email', user.email);
      if (user.phoneNumber) {
        setValue('phone', user.phoneNumber);
      }
    }
  }, [user, setValue]);

  // Validate current step before proceeding
  const validateCurrentStep = async (): Promise<boolean> => {
    const schema = stepSchemas[currentStep - 1];

    try {
      // Get fields for current step
      const stepFields = Object.keys(schema.shape) as (keyof AdoptionFormData)[];
      const result = await trigger(stepFields);
      return result;
    } catch {
      return false;
    }
  };

  const handleNext = async () => {
    const isValid = await validateCurrentStep();
    if (isValid) {
      setCurrentStep((prev) => Math.min(prev + 1, steps.length));
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const handlePrevious = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 1));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleGoToStep = (step: number) => {
    if (step < currentStep) {
      setCurrentStep(step);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const onSubmit = async (data: AdoptionFormData) => {
    // Guard - don't submit if we're not on the summary step
    if (currentStep !== steps.length) {
      return;
    }

    if (!animalId) {
      toast.error('Nie znaleziono zwierzęcia');
      return;
    }

    setIsSubmitting(true);

    try {
      // Build living conditions description
      const livingConditionsParts = [
        `Typ mieszkania: ${housingTypeLabels[data.housingType] || data.housingType}`,
        data.hasChildren ? `Dzieci w domu: Tak${data.childrenAges ? ` (wiek: ${data.childrenAges})` : ''}` : 'Dzieci w domu: Nie',
        data.hasOtherAnimals ? `Inne zwierzęta: Tak${data.otherAnimalsDescription ? ` (${data.otherAnimalsDescription})` : ''}` : 'Inne zwierzęta: Nie',
        `Poziom doświadczenia: ${experienceLevelLabels[data.experienceLevel] || data.experienceLevel}`,
        data.experienceDescription ? `Opis doświadczenia: ${data.experienceDescription}` : '',
      ].filter(Boolean);

      // Build motivation text
      const motivationText = [
        `Dlaczego chcę adoptować: ${data.whyAdopt}`,
        `Jak będę się opiekować: ${data.howCare}`,
      ].join('\n\n');

      // Build other pets info
      const otherPetsInfo = data.hasOtherAnimals && data.otherAnimalsDescription
        ? data.otherAnimalsDescription
        : undefined;

      // Prepare application data for API (flat structure expected by backend)
      const applicationData = {
        animalId,
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        phone: data.phone,
        address: data.street,
        city: data.city,
        postalCode: data.postalCode,
        dateOfBirth: data.dateOfBirth,
        rodoConsent: data.gdprConsent,
        motivation: motivationText,
        livingConditions: livingConditionsParts.join('\n'),
        experience: data.experienceDescription || experienceLevelLabels[data.experienceLevel] || data.experienceLevel,
        otherPetsInfo,
        // Structured matching fields
        housingType: data.housingType,
        hasChildren: data.hasChildren,
        hasOtherAnimals: data.hasOtherAnimals,
        experienceLevelApplicant: data.experienceLevel,
        availableCareTime: data.availableCareTime,
      };

      const result = await adoptionsApi.submitApplication(applicationData);

      // Navigate to confirmation page with application ID
      navigate(`/adoption/confirmation/${result.applicationId}`, {
        state: {
          applicationNumber: result.applicationId.slice(0, 8).toUpperCase(),
          animalName: animal?.name,
        },
      });

      toast.success(result.message || 'Wniosek został wysłany!');
    } catch (error) {
      console.error('Failed to submit application:', error);
      toast.error('Wystąpił błąd podczas wysyłania wniosku. Spróbuj ponownie.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Loading animal data
  if (isLoadingAnimal) {
    return (
      <PageContainer>
        <div className="flex flex-col items-center justify-center py-24">
          <Spinner size="lg" />
          <p className="mt-4 text-gray-500">Ładowanie danych...</p>
        </div>
      </PageContainer>
    );
  }

  // Animal not found or not available
  if (!animal || !['Available', 'Reserved'].includes(animal.status)) {
    return (
      <PageContainer>
        <div className="text-center py-24">
          <div className="text-6xl mb-6">😿</div>
          <h2 className="text-2xl font-bold text-gray-900 mb-4">
            To zwierzę nie jest dostępne do adopcji
          </h2>
          <p className="text-gray-600 mb-8">
            Przepraszamy, ale to zwierzę nie jest obecnie dostępne do adopcji.
            Może zostało już adoptowane lub jest w trakcie procesu adopcyjnego.
          </p>
          <Button as={Link} to="/animals">
            Zobacz inne zwierzęta
          </Button>
        </div>
      </PageContainer>
    );
  }

  const formData = getValues();

  return (
    <PageContainer className="pb-24">
      {/* Back link */}
      <Link
        to={`/animals/${animalId}`}
        className="inline-flex items-center text-gray-600 hover:text-primary-600 mb-6"
      >
        <ArrowLeftIcon className="h-4 w-4 mr-2" />
        Wróć do {animal.name}
      </Link>

      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Formularz adopcyjny</h1>
        <p className="text-gray-600 mt-2">
          Wypełnij formularz, aby złożyć wniosek o adopcję {animal.name}
        </p>
      </div>

      {/* Stepper */}
      <div className="mb-8">
        <FormStepper
          steps={steps}
          currentStep={currentStep}
          onStepClick={handleGoToStep}
          allowNavigation={true}
        />
      </div>

      {/* Form */}
      <Card className="p-6 md:p-8">
        <form
          onSubmit={handleSubmit(onSubmit)}
          onKeyDown={(e) => {
            // Block Enter to prevent accidental form submission
            if (e.key === 'Enter' && currentStep !== steps.length) {
              e.preventDefault();
            }
          }}
        >
          {/* Step content */}
          <div className="min-h-[400px]">
            {currentStep === 1 && (
              <PersonalInfoStep register={register} errors={errors} />
            )}

            {currentStep === 2 && (
              <LivingConditionsStep
                register={register}
                errors={errors}
                watch={watch}
              />
            )}

            {currentStep === 3 && (
              <MotivationStep
                register={register}
                errors={errors}
                watch={watch}
                animalName={animal.name}
              />
            )}

            {currentStep === 4 && (
              <ConsentsStep register={register} errors={errors} />
            )}

            {currentStep === 5 && (
              <SummaryStep
                data={formData}
                animal={animal}
                onEditStep={handleGoToStep}
                register={register}
                errors={errors}
              />
            )}
          </div>

          {/* Navigation buttons */}
          <div className="flex justify-between pt-6 mt-6 border-t border-gray-200">
            <Button
              type="button"
              variant="outline"
              onClick={handlePrevious}
              disabled={currentStep === 1}
              className={currentStep === 1 ? 'invisible' : ''}
            >
              <ArrowLeftIcon className="h-4 w-4 mr-2" />
              Wstecz
            </Button>

            {currentStep < steps.length ? (
              <Button type="button" onClick={handleNext}>
                Dalej
                <ArrowRightIcon className="h-4 w-4 ml-2" />
              </Button>
            ) : (
              <Button type="submit" isLoading={isSubmitting}>
                <PaperAirplaneIcon className="h-4 w-4 mr-2" />
                Wyślij wniosek
              </Button>
            )}
          </div>
        </form>
      </Card>
    </PageContainer>
  );
}
