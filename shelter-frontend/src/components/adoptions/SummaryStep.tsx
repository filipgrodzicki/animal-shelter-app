import { CheckCircleIcon, PencilIcon } from '@heroicons/react/24/outline';
import { UseFormRegister, FieldErrors } from 'react-hook-form';
import { Button, Card } from '@/components/common';
import {
  AdoptionFormData,
  housingTypeLabels,
  experienceLevelLabels,
  careTimeLabels,
} from './adoptionFormSchema';
import { AnimalDetail, getSpeciesLabel, formatAge } from '@/types';

interface SummaryStepProps {
  data: AdoptionFormData;
  animal?: AnimalDetail | null;
  onEditStep: (step: number) => void;
  register: UseFormRegister<AdoptionFormData>;
  errors: FieldErrors<AdoptionFormData>;
}

export function SummaryStep({ data, animal, onEditStep, register, errors }: SummaryStepProps) {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
          <CheckCircleIcon className="w-5 h-5 text-primary-600" />
        </div>
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Podsumowanie</h2>
          <p className="text-sm text-gray-500">Sprawdź poprawność danych przed wysłaniem</p>
        </div>
      </div>

      {/* Animal info */}
      {animal && (
        <Card className="p-4 bg-primary-50 border-primary-200">
          <div className="flex items-center gap-4">
            {animal.photos?.[0]?.url ? (
              <img
                src={animal.photos[0].url}
                alt={animal.name}
                className="w-16 h-16 rounded-lg object-cover"
              />
            ) : (
              <div className="w-16 h-16 bg-primary-100 rounded-lg flex items-center justify-center">
                <svg className="w-8 h-8 text-primary-400" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 0 1-.383-.218 25.18 25.18 0 0 1-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0 1 12 5.052 5.5 5.5 0 0 1 16.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 0 1-4.244 3.17 15.247 15.247 0 0 1-.383.219l-.022.012-.007.004-.003.001a.752.752 0 0 1-.704 0l-.003-.001Z" />
                </svg>
              </div>
            )}
            <div>
              <p className="text-sm text-primary-600">Wniosek o adopcję</p>
              <h3 className="text-lg font-semibold text-gray-900">{animal.name}</h3>
              <p className="text-sm text-gray-600">
                {getSpeciesLabel(animal.species)} • {formatAge(animal.ageInMonths)}
              </p>
            </div>
          </div>
        </Card>
      )}

      {/* Personal info summary */}
      <SummarySection
        title="Dane osobowe"
        onEdit={() => onEditStep(1)}
      >
        <SummaryRow label="Imię i nazwisko" value={`${data.firstName} ${data.lastName}`} />
        <SummaryRow label="Email" value={data.email} />
        <SummaryRow label="Telefon" value={data.phone} />
        <SummaryRow label="Data urodzenia" value={formatDate(data.dateOfBirth)} />
        <SummaryRow
          label="Adres"
          value={`${data.street}, ${data.postalCode} ${data.city}`}
        />
      </SummarySection>

      {/* Living conditions summary */}
      <SummarySection
        title="Warunki mieszkaniowe"
        onEdit={() => onEditStep(2)}
      >
        <SummaryRow
          label="Typ mieszkania"
          value={housingTypeLabels[data.housingType]}
        />
        <SummaryRow
          label="Dzieci w domu"
          value={data.hasChildren ? `Tak (${data.childrenAges || 'brak informacji o wieku'})` : 'Nie'}
        />
        <SummaryRow
          label="Inne zwierzęta"
          value={
            data.hasOtherAnimals
              ? `Tak${data.otherAnimalsDescription ? `: ${data.otherAnimalsDescription}` : ''}`
              : 'Nie'
          }
        />
        <SummaryRow
          label="Doświadczenie"
          value={experienceLevelLabels[data.experienceLevel]}
        />
        <SummaryRow
          label="Dostępny czas na opiekę"
          value={careTimeLabels[data.availableCareTime]}
        />
        {data.experienceDescription && (
          <SummaryRow label="Opis doświadczenia" value={data.experienceDescription} />
        )}
      </SummarySection>

      {/* Motivation summary */}
      <SummarySection
        title="Motywacja"
        onEdit={() => onEditStep(3)}
      >
        <div className="space-y-4">
          <div>
            <p className="text-sm text-gray-500 mb-1">
              Dlaczego chcesz adoptować {animal?.name || 'to zwierzę'}?
            </p>
            <p className="text-gray-700 whitespace-pre-line">{data.whyAdopt}</p>
          </div>
          <div>
            <p className="text-sm text-gray-500 mb-1">Jak zamierzasz się opiekować?</p>
            <p className="text-gray-700 whitespace-pre-line">{data.howCare}</p>
          </div>
        </div>
      </SummarySection>

      {/* Consents summary */}
      <SummarySection
        title="Zgody"
        onEdit={() => onEditStep(4)}
      >
        <div className="space-y-2">
          <ConsentItem
            checked={data.gdprConsent}
            label="Zgoda na przetwarzanie danych osobowych (RODO)"
          />
          <ConsentItem
            checked={data.rulesConsent}
            label="Akceptacja regulaminu adopcji"
          />
        </div>
      </SummarySection>

      {/* Confirmation checkbox */}
      <div className="bg-green-50 border border-green-200 rounded-lg p-4">
        <label className="flex items-start gap-3 cursor-pointer">
          <input
            type="checkbox"
            {...register('confirmSubmission')}
            className="w-5 h-5 mt-0.5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          <div className="flex-1">
            <span className="font-medium text-green-900">
              Potwierdzam poprawność danych
              <span className="text-red-500 ml-1">*</span>
            </span>
            <p className="text-sm text-green-800 mt-1">
              Oświadczam, że wszystkie podane przeze mnie dane są prawdziwe i aktualne.
              Rozumiem, że po wysłaniu wniosku skontaktujemy się z Tobą w ciągu 2-5 dni roboczych.
            </p>
          </div>
        </label>
        {errors.confirmSubmission && (
          <p className="mt-2 text-sm text-red-600 ml-8">{errors.confirmSubmission.message}</p>
        )}
      </div>
    </div>
  );
}

interface SummarySectionProps {
  title: string;
  onEdit: () => void;
  children: React.ReactNode;
}

function SummarySection({ title, onEdit, children }: SummarySectionProps) {
  return (
    <Card className="overflow-hidden">
      <div className="flex items-center justify-between p-4 bg-gray-50 border-b border-gray-200">
        <h3 className="font-medium text-gray-900">{title}</h3>
        <Button variant="ghost" size="sm" onClick={onEdit}>
          <PencilIcon className="w-4 h-4 mr-1" />
          Edytuj
        </Button>
      </div>
      <div className="p-4">{children}</div>
    </Card>
  );
}

interface SummaryRowProps {
  label: string;
  value: string;
}

function SummaryRow({ label, value }: SummaryRowProps) {
  return (
    <div className="flex flex-col sm:flex-row sm:justify-between py-2 border-b border-gray-100 last:border-0">
      <span className="text-sm text-gray-500">{label}</span>
      <span className="text-sm text-gray-900 font-medium sm:text-right">{value}</span>
    </div>
  );
}

interface ConsentItemProps {
  checked: boolean;
  label: string;
}

function ConsentItem({ checked, label }: ConsentItemProps) {
  return (
    <div className="flex items-center gap-2">
      <CheckCircleIcon
        className={`w-5 h-5 ${checked ? 'text-green-500' : 'text-gray-300'}`}
      />
      <span className={`text-sm ${checked ? 'text-gray-900' : 'text-gray-500'}`}>
        {label}
      </span>
    </div>
  );
}

function formatDate(dateString: string): string {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString('pl-PL', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}
