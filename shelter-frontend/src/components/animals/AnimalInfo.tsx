import {
  CalendarIcon,
  IdentificationIcon,
  TagIcon,
  UserIcon,
} from '@heroicons/react/24/outline';
import { Badge, Card } from '@/components/common';
import {
  AnimalDetail,
  getSpeciesLabel,
  getGenderLabel,
  getSizeLabel,
  getStatusLabel,
  formatAge,
} from '@/types';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';

interface AnimalInfoProps {
  animal: AnimalDetail;
  showSensitiveData?: boolean;
}

type StatusBadgeColor = 'green' | 'blue' | 'yellow' | 'red' | 'gray' | 'purple';

const statusColors: Record<string, StatusBadgeColor> = {
  Available: 'green',
  Reserved: 'yellow',
  InAdoptionProcess: 'yellow',
  Quarantine: 'red',
  UnderVeterinaryCare: 'red',
  Adopted: 'blue',
  Deceased: 'gray',
  Returned: 'purple',
  Transferred: 'gray',
};

// Species icon (paw)
function SpeciesIcon() {
  return (
    <svg className="h-5 w-5 text-warm-400" viewBox="0 0 512 512" fill="currentColor">
      <path d="M226.5 92.9c14.3 42.9-.3 86.2-32.6 96.8s-70.1-15.6-84.4-58.5s.3-86.2 32.6-96.8s70.1 15.6 84.4 58.5zM100.4 198.6c18.9 32.4 14.3 70.1-10.2 84.1s-59.7-.9-78.5-33.3S-2.7 179.3 21.8 165.3s59.7 .9 78.5 33.3zM69.2 401.2C121.6 259.9 214.7 224 256 224s134.4 35.9 186.8 177.2c3.6 9.7 5.2 20.1 5.2 30.5v1.6c0 25.8-20.9 46.7-46.7 46.7c-11.5 0-22.9-1.4-34-4.2l-88-22c-15.3-3.8-31.3-3.8-46.6 0l-88 22c-11.1 2.8-22.5 4.2-34 4.2C84.9 480 64 459.1 64 433.3v-1.6c0-10.4 1.6-20.8 5.2-30.5zM421.8 282.7c-24.5-14-29.1-51.7-10.2-84.1s54-47.3 78.5-33.3s29.1 51.7 10.2 84.1s-54 47.3-78.5 33.3zM310.1 189.7c-32.3-10.6-46.9-53.9-32.6-96.8s52.1-69.1 84.4-58.5s46.9 53.9 32.6 96.8s-52.1 69.1-84.4 58.5z"/>
    </svg>
  );
}

// Gender icon
function GenderIcon({ gender }: { gender: string }) {
  if (gender === 'Male') {
    return (
      <svg className="h-5 w-5 text-warm-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="10" cy="14" r="5" />
        <path d="M19 5l-5.4 5.4M19 5h-5M19 5v5" />
      </svg>
    );
  }
  if (gender === 'Female') {
    return (
      <svg className="h-5 w-5 text-warm-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="12" cy="9" r="5" />
        <path d="M12 14v7M9 18h6" />
      </svg>
    );
  }
  return <UserIcon className="h-5 w-5 text-warm-400" />;
}

export function AnimalInfo({ animal, showSensitiveData = false }: AnimalInfoProps) {
  const statusColor = statusColors[animal.status] || 'gray';
  const isInProcess = ['Reserved', 'InAdoptionProcess'].includes(animal.status);

  return (
    <Card className="overflow-hidden border border-warm-200">
      <div className="p-6">
        {/* Header with name and status */}
        <div className="flex items-start justify-between gap-4 mb-6">
          <div>
            <h1 className="text-3xl font-bold text-warm-900">{animal.name}</h1>
            {animal.breed && (
              <p className="text-lg text-warm-700 mt-1">{animal.breed}</p>
            )}
          </div>
          <Badge
            color={statusColor}
            size="lg"
            className={isInProcess ? 'animate-pulse' : ''}
          >
            {getStatusLabel(animal.status)}
          </Badge>
        </div>

        {/* Quick info grid */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          <InfoItem
            icon={<SpeciesIcon />}
            label="Gatunek"
            value={getSpeciesLabel(animal.species)}
          />
          <InfoItem
            icon={<GenderIcon gender={animal.gender} />}
            label="Płeć"
            value={getGenderLabel(animal.gender)}
          />
          <InfoItem
            icon={<CalendarIcon className="h-5 w-5 text-warm-400" />}
            label="Wiek"
            value={formatAge(animal.ageInMonths)}
          />
          <InfoItem
            icon={<TagIcon className="h-5 w-5 text-warm-400" />}
            label="Rozmiar"
            value={getSizeLabel(animal.size)}
          />
        </div>

        {/* Detailed info */}
        <div className="space-y-4 border-t border-warm-200 pt-6">
          {animal.color && (
            <DetailRow label="Umaszczenie" value={animal.color} />
          )}

          {showSensitiveData && animal.chipNumber && (
            <DetailRow
              label="Numer chipa"
              value={animal.chipNumber}
              icon={<IdentificationIcon className="h-4 w-4" />}
            />
          )}

          {showSensitiveData && (
            <DetailRow
              label="Numer ewidencyjny"
              value={animal.registrationNumber}
              icon={<TagIcon className="h-4 w-4" />}
            />
          )}

          <DetailRow
            label="Data przyjęcia"
            value={format(new Date(animal.admissionDate), 'd MMMM yyyy', { locale: pl })}
            icon={<CalendarIcon className="h-4 w-4" />}
          />

          {animal.admissionCircumstances && (
            <div className="pt-2">
              <p className="text-sm font-medium text-warm-700 mb-1">Okoliczności przyjęcia</p>
              <p className="text-warm-700">{animal.admissionCircumstances}</p>
            </div>
          )}
        </div>

        {/* Description */}
        {animal.description && (
          <div className="border-t border-warm-200 pt-6 mt-6">
            <h3 className="text-lg font-semibold text-warm-900 mb-3">O mnie</h3>
            <p className="text-warm-700 whitespace-pre-line leading-relaxed">
              {animal.description}
            </p>
          </div>
        )}
      </div>
    </Card>
  );
}

interface InfoItemProps {
  icon: React.ReactNode;
  label: string;
  value: string;
}

function InfoItem({ icon, label, value }: InfoItemProps) {
  return (
    <div className="bg-warm-50 rounded-lg p-3 text-center">
      <div className="flex justify-center mb-1">{icon}</div>
      <p className="text-xs text-warm-700 mb-0.5">{label}</p>
      <p className="font-medium text-warm-900">{value}</p>
    </div>
  );
}

interface DetailRowProps {
  label: string;
  value: string;
  icon?: React.ReactNode;
}

function DetailRow({ label, value, icon }: DetailRowProps) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-warm-700 flex items-center gap-2">
        {icon}
        {label}
      </span>
      <span className="font-medium text-warm-900">{value}</span>
    </div>
  );
}
