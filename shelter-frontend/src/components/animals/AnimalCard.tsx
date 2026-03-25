import { Link, useNavigate } from 'react-router-dom';
import { HeartIcon } from '@heroicons/react/24/outline';
import { HeartIcon as HeartIconSolid } from '@heroicons/react/24/solid';
import { Card, Badge } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import {
  Animal,
  AnimalListItem,
  getSpeciesLabel,
  getGenderLabel,
  formatAge,
} from '@/types';

interface AnimalCardProps {
  animal: Animal | AnimalListItem;
  isFavorite?: boolean;
  onToggleFavorite?: (animalId: string) => void;
  showAdoptButton?: boolean;
}

type StatusBadgeColor = 'green' | 'blue' | 'yellow' | 'red' | 'gray' | 'purple';

interface StatusConfig {
  label: string;
  color: StatusBadgeColor;
}

const statusConfigs: Record<string, StatusConfig> = {
  Admitted: { label: 'Przyjęte', color: 'gray' },
  Available: { label: 'Dostępne', color: 'green' },
  Reserved: { label: 'Zarezerwowane', color: 'yellow' },
  InAdoptionProcess: { label: 'W procesie adopcji', color: 'yellow' },
  Quarantine: { label: 'Kwarantanna', color: 'red' },
  Treatment: { label: 'Leczenie', color: 'red' },
  Adopted: { label: 'Adoptowane', color: 'blue' },
  Deceased: { label: 'Zmarłe', color: 'gray' },
};

function getStatusConfig(status: string): StatusConfig {
  return statusConfigs[status] || { label: status, color: 'gray' };
}

// SVG icon for photo placeholder
function PlaceholderIcon({ species }: { species: string }) {
  const isDog = species === 'Dog';
  const isCat = species === 'Cat';

  return (
    <svg
      className="w-20 h-20 text-primary-300"
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden="true"
      role="img"
    >
      {isDog ? (
        // Dog silhouette
        <path d="M4.5 12.75a.75.75 0 0 1 .75-.75h13.5a.75.75 0 0 1 0 1.5H5.25a.75.75 0 0 1-.75-.75ZM4.5 15.75a.75.75 0 0 1 .75-.75h7.5a.75.75 0 0 1 0 1.5h-7.5a.75.75 0 0 1-.75-.75ZM4.5 18.75a.75.75 0 0 1 .75-.75h4.5a.75.75 0 0 1 0 1.5h-4.5a.75.75 0 0 1-.75-.75Z M12 2.25a.75.75 0 0 1 .75.75v2.25a.75.75 0 0 1-1.5 0V3a.75.75 0 0 1 .75-.75ZM7.5 12a4.5 4.5 0 1 1 9 0 4.5 4.5 0 0 1-9 0Z" />
      ) : isCat ? (
        // Cat silhouette
        <path d="M12 2.25c-5.385 0-9.75 4.365-9.75 9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385 2.25 12 2.25Zm-2.625 6c-.54 0-.828.419-.936.634a1.96 1.96 0 0 0-.189.866c0 .298.059.605.189.866.108.215.395.634.936.634.54 0 .828-.419.936-.634.13-.26.189-.568.189-.866 0-.298-.059-.605-.189-.866-.108-.215-.395-.634-.936-.634Zm5.25 0c-.54 0-.828.419-.936.634a1.96 1.96 0 0 0-.189.866c0 .298.059.605.189.866.108.215.395.634.936.634.54 0 .828-.419.936-.634.13-.26.189-.568.189-.866 0-.298-.059-.605-.189-.866-.108-.215-.395-.634-.936-.634Z" />
      ) : (
        // Generic animal icon (paw)
        <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 0 1-.383-.218 25.18 25.18 0 0 1-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0 1 12 5.052 5.5 5.5 0 0 1 16.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 0 1-4.244 3.17 15.247 15.247 0 0 1-.383.219l-.022.012-.007.004-.003.001a.752.752 0 0 1-.704 0l-.003-.001Z" />
      )}
    </svg>
  );
}

export function AnimalCard({
  animal,
  isFavorite = false,
  onToggleFavorite,
  showAdoptButton = true,
}: AnimalCardProps) {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const statusConfig = getStatusConfig(animal.status);
  const isAvailableForAdoption = animal.status === 'Available';

  const handleAdoptClick = (e: React.MouseEvent) => {
    e.preventDefault();
    if (isAuthenticated) {
      navigate(`/adoption/apply/${animal.id}`);
    } else {
      navigate('/login', { state: { from: { pathname: `/adoption/apply/${animal.id}` } } });
    }
  };

  return (
    <Card className="group overflow-hidden rounded-2xl transition-all duration-300 hover:shadow-lg hover:-translate-y-1 flex flex-col bg-white border border-warm-200 hover:border-primary-200">
      {/* Image */}
      <div className="relative aspect-square overflow-hidden bg-warm-100">
        <Link to={`/animals/${animal.id}`} className="block h-full w-full">
          {animal.mainPhotoUrl ? (
            <img
              src={animal.mainPhotoUrl}
              alt={animal.name}
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-warm-50 to-warm-200">
              <PlaceholderIcon species={animal.species} />
            </div>
          )}
        </Link>

        {/* Favorite button */}
        {onToggleFavorite && (
          <button
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              onToggleFavorite(animal.id);
            }}
            className="absolute right-3 top-3 rounded-full bg-white/90 p-2 shadow-md transition-all hover:bg-white hover:scale-110 focus:outline-none focus:ring-2 focus:ring-primary-500"
            aria-label={isFavorite ? `Usuń ${animal.name} z ulubionych` : `Dodaj ${animal.name} do ulubionych`}
            aria-pressed={isFavorite}
          >
            {isFavorite ? (
              <HeartIconSolid className="h-5 w-5 text-red-500" aria-hidden="true" />
            ) : (
              <HeartIcon className="h-5 w-5 text-warm-700 group-hover:text-red-400" aria-hidden="true" />
            )}
          </button>
        )}

        {/* Status badge */}
        <div className="absolute left-3 top-3">
          <Badge
            color={statusConfig.color}
            size="sm"
          >
            {statusConfig.label}
          </Badge>
        </div>
      </div>

      {/* Content */}
      <div className="flex flex-1 flex-col p-5">
        {/* Header */}
        <div className="mb-2">
          <Link to={`/animals/${animal.id}`}>
            <h3 className="text-lg font-semibold text-warm-900 hover:text-primary-600 transition-colors line-clamp-1">
              {animal.name}
            </h3>
          </Link>
          {animal.breed && (
            <p className="text-sm text-warm-700 line-clamp-1">{animal.breed}</p>
          )}
        </div>

        {/* Details */}
        <div className="mb-3 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-warm-700">
          <span>{getSpeciesLabel(animal.species)}</span>
          <span className="text-warm-300">•</span>
          <span>{getGenderLabel(animal.gender)}</span>
          <span className="text-warm-300">•</span>
          <span>{formatAge(animal.ageInMonths)}</span>
        </div>

        {/* Description */}
        {animal.description && (
          <p className="mb-4 text-sm text-warm-700 line-clamp-2 flex-grow">
            {animal.description}
          </p>
        )}

        {/* Actions */}
        <div className="mt-auto space-y-2">
          {showAdoptButton && isAvailableForAdoption && (
            <button
              onClick={handleAdoptClick}
              className="flex items-center justify-center w-full px-4 py-2.5 bg-primary-600 text-white font-medium rounded-xl hover:bg-primary-700 transition-all duration-200 group/btn cursor-pointer"
              aria-label={`Adoptuj ${animal.name}`}
            >
              Adoptuj mnie
            </button>
          )}

          <Link
            to={`/animals/${animal.id}`}
            className="block text-center text-sm font-medium text-primary-600 hover:text-primary-700 transition-colors py-1.5 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 rounded hover:bg-primary-50"
            aria-label={`Zobacz szczegóły: ${animal.name}`}
          >
            Zobacz szczegóły →
          </Link>
        </div>
      </div>
    </Card>
  );
}
