import { Link } from 'react-router-dom';
import {
  HeartIcon,
  CalendarIcon,
  MapPinIcon,
  ScaleIcon,
  CheckCircleIcon,
  XCircleIcon,
  UserIcon,
} from '@heroicons/react/24/outline';
import { HeartIcon as HeartIconSolid } from '@heroicons/react/24/solid';
import { Badge, Button, Card } from '@/components/common';
import {
  Animal,
  getSpeciesLabel,
  getGenderLabel,
  getSizeLabel,
  getStatusLabel,
  getStatusColor,
  formatAge,
} from '@/types';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';

interface AnimalDetailsProps {
  animal: Animal;
  isFavorite?: boolean;
  onToggleFavorite?: () => void;
  canAdopt?: boolean;
}

// SVG icon for placeholder
function PlaceholderIcon({ size = 'lg' }: { size?: 'sm' | 'lg' }) {
  const sizeClass = size === 'lg' ? 'w-24 h-24' : 'w-5 h-5';

  return (
    <svg className={`${sizeClass} text-primary-300`} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 0 1-.383-.218 25.18 25.18 0 0 1-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0 1 12 5.052 5.5 5.5 0 0 1 16.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 0 1-4.244 3.17 15.247 15.247 0 0 1-.383.219l-.022.012-.007.004-.003.001a.752.752 0 0 1-.704 0l-.003-.001Z" />
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

export function AnimalDetails({
  animal,
  isFavorite = false,
  onToggleFavorite,
  canAdopt = true,
}: AnimalDetailsProps) {
  const statusColorMap: Record<string, 'green' | 'blue' | 'yellow' | 'red' | 'gray' | 'purple'> = {
    green: 'green',
    blue: 'blue',
    yellow: 'yellow',
    red: 'red',
    gray: 'gray',
    purple: 'purple',
  };

  const getStatusBadgeColor = (status: string): 'green' | 'blue' | 'yellow' | 'red' | 'gray' | 'purple' => {
    const color = getStatusColor(status as Animal['status']);
    return statusColorMap[color] || 'gray';
  };

  const isAvailableForAdoption = ['Available', 'Reserved'].includes(animal.status);

  return (
    <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
      {/* Main content */}
      <div className="lg:col-span-2 space-y-6">
        {/* Photo gallery */}
        <div className="overflow-hidden rounded-xl bg-warm-100">
          {animal.mainPhotoUrl ? (
            <img
              src={animal.mainPhotoUrl}
              alt={animal.name}
              className="h-96 w-full object-cover"
            />
          ) : (
            <div className="flex h-96 items-center justify-center bg-gradient-to-br from-warm-50 to-warm-200">
              <PlaceholderIcon size="lg" />
            </div>
          )}
        </div>

        {/* Additional photos */}
        {animal.photoUrls && animal.photoUrls.length > 0 && (
          <div className="grid grid-cols-4 gap-2">
            {animal.photoUrls.slice(0, 4).map((url: string, index: number) => (
              <img
                key={index}
                src={url}
                alt={`${animal.name} - zdjęcie ${index + 2}`}
                className="aspect-square rounded-lg object-cover cursor-pointer hover:opacity-80 transition-opacity"
              />
            ))}
          </div>
        )}

        {/* Description */}
        <Card className="border border-warm-200">
          <div className="p-6">
            <h2 className="text-xl font-semibold text-warm-900 mb-4">O zwierzęciu</h2>
            {animal.description ? (
              <p className="text-warm-700 whitespace-pre-line">{animal.description}</p>
            ) : (
              <p className="text-warm-400 italic">Brak opisu</p>
            )}
          </div>
        </Card>

        {/* Health info */}
        <Card className="border border-warm-200">
          <div className="p-6">
            <h2 className="text-xl font-semibold text-warm-900 mb-4">Informacje zdrowotne</h2>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex items-center gap-2">
                {animal.isNeutered ? (
                  <CheckCircleIcon className="h-5 w-5 text-status-available" />
                ) : (
                  <XCircleIcon className="h-5 w-5 text-warm-400" />
                )}
                <span className="text-warm-700">
                  {animal.isNeutered ? 'Wykastrowany/a' : 'Niewykastrowany/a'}
                </span>
              </div>
              <div className="flex items-center gap-2">
                {animal.isVaccinated ? (
                  <CheckCircleIcon className="h-5 w-5 text-status-available" />
                ) : (
                  <XCircleIcon className="h-5 w-5 text-warm-400" />
                )}
                <span className="text-warm-700">
                  {animal.isVaccinated ? 'Zaszczepiony/a' : 'Niezaszczepiony/a'}
                </span>
              </div>
              <div className="flex items-center gap-2">
                {animal.isChipped ? (
                  <CheckCircleIcon className="h-5 w-5 text-status-available" />
                ) : (
                  <XCircleIcon className="h-5 w-5 text-warm-400" />
                )}
                <span className="text-warm-700">
                  {animal.isChipped ? 'Zaczipowany/a' : 'Niezaczipowany/a'}
                </span>
              </div>
            </div>
            {animal.healthNotes && (
              <div className="mt-4 pt-4 border-t border-warm-200">
                <h3 className="font-medium text-warm-900 mb-2">Notatki zdrowotne</h3>
                <p className="text-warm-700">{animal.healthNotes}</p>
              </div>
            )}
          </div>
        </Card>

        {/* Behavior */}
        {animal.behaviorNotes && (
          <Card className="border border-warm-200">
            <div className="p-6">
              <h2 className="text-xl font-semibold text-warm-900 mb-4">Charakter i zachowanie</h2>
              <p className="text-warm-700 whitespace-pre-line">{animal.behaviorNotes}</p>
            </div>
          </Card>
        )}
      </div>

      {/* Sidebar */}
      <div className="space-y-6">
        {/* Basic info card */}
        <Card className="sticky top-24 border border-warm-200 shadow-warm">
          <div className="p-6">
            <div className="flex items-start justify-between mb-4">
              <div>
                <h1 className="text-2xl font-bold text-warm-900">{animal.name}</h1>
                <p className="text-warm-700">{animal.breed || getSpeciesLabel(animal.species)}</p>
              </div>
              {onToggleFavorite && (
                <button
                  onClick={onToggleFavorite}
                  className="p-2 rounded-full hover:bg-warm-100 transition-colors"
                >
                  {isFavorite ? (
                    <HeartIconSolid className="h-6 w-6 text-primary-600" />
                  ) : (
                    <HeartIcon className="h-6 w-6 text-warm-400" />
                  )}
                </button>
              )}
            </div>

            <div className="mb-4">
              <Badge color={getStatusBadgeColor(animal.status)} size="lg">
                {getStatusLabel(animal.status)}
              </Badge>
            </div>

            <div className="space-y-3 mb-6">
              <div className="flex items-center gap-3 text-warm-700">
                <PlaceholderIcon size="sm" />
                <span>{getSpeciesLabel(animal.species)}</span>
              </div>
              <div className="flex items-center gap-3 text-warm-700">
                <CalendarIcon className="h-5 w-5 text-warm-400" />
                <span>{formatAge(animal.ageInMonths)}</span>
              </div>
              <div className="flex items-center gap-3 text-warm-700">
                <GenderIcon gender={animal.gender} />
                <span>{getGenderLabel(animal.gender)}</span>
              </div>
              {animal.size && (
                <div className="flex items-center gap-3 text-warm-700">
                  <ScaleIcon className="h-5 w-5 text-warm-400" />
                  <span>{getSizeLabel(animal.size)}</span>
                </div>
              )}
              {animal.locationInShelter && (
                <div className="flex items-center gap-3 text-warm-700">
                  <MapPinIcon className="h-5 w-5 text-warm-400" />
                  <span>{animal.locationInShelter}</span>
                </div>
              )}
            </div>

            {animal.admissionDate && (
              <div className="text-sm text-warm-700 mb-6">
                W schronisku od: {format(new Date(animal.admissionDate), 'd MMMM yyyy', { locale: pl })}
              </div>
            )}

            {canAdopt && isAvailableForAdoption && (
              <Button as={Link} to={`/adoption/apply/${animal.id}`} className="w-full" size="lg">
                <HeartIcon className="h-5 w-5 mr-2" />
                Adoptuj mnie
              </Button>
            )}

            {!isAvailableForAdoption && (
              <div className="bg-warm-100 rounded-lg p-4 text-center text-warm-700">
                {animal.status === 'Adopted'
                  ? 'To zwierzę zostało już adoptowane'
                  : animal.status === 'InAdoptionProcess'
                  ? 'Trwa proces adopcyjny'
                  : 'To zwierzę nie jest obecnie dostępne do adopcji'}
              </div>
            )}
          </div>
        </Card>

        {/* Contact card */}
        <Card className="border border-warm-200">
          <div className="p-6">
            <h3 className="font-semibold text-warm-900 mb-3">Masz pytania?</h3>
            <p className="text-sm text-warm-700 mb-4">
              Skontaktuj się z nami, aby dowiedzieć się więcej o {animal.name}.
            </p>
            <Button as={Link} to="/contact" variant="outline" className="w-full">
              Skontaktuj się
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
