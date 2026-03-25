import { Animal, AnimalListItem } from '@/types';
import { AnimalCard } from './AnimalCard';
import { Spinner, EmptyState, Button } from '@/components/common';
import { ExclamationTriangleIcon, ArrowPathIcon } from '@heroicons/react/24/outline';

interface AnimalGridProps {
  animals: (Animal | AnimalListItem)[];
  isLoading?: boolean;
  error?: string | null;
  favorites?: string[];
  onToggleFavorite?: (animalId: string) => void;
  showAdoptButton?: boolean;
  emptyMessage?: string;
  emptyDescription?: string;
  onRetry?: () => void;
}

export function AnimalGrid({
  animals,
  isLoading = false,
  error = null,
  favorites = [],
  onToggleFavorite,
  showAdoptButton = true,
  emptyMessage = 'Brak zwierząt',
  emptyDescription = 'Nie znaleziono zwierząt spełniających kryteria wyszukiwania.',
  onRetry,
}: AnimalGridProps) {
  // Loading state
  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <Spinner size="lg" />
        <p className="mt-4 text-gray-500">Ładowanie zwierząt...</p>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center">
        <div className="rounded-full bg-red-100 p-4 mb-4">
          <ExclamationTriangleIcon className="h-8 w-8 text-red-600" />
        </div>
        <h3 className="text-lg font-semibold text-gray-900 mb-2">
          Nie udało się załadować zwierząt
        </h3>
        <p className="text-gray-600 max-w-md mb-6">
          {error || 'Wystąpił błąd podczas pobierania danych. Spróbuj ponownie.'}
        </p>
        {onRetry && (
          <Button onClick={onRetry} variant="outline">
            <ArrowPathIcon className="h-4 w-4 mr-2" />
            Spróbuj ponownie
          </Button>
        )}
      </div>
    );
  }

  // Empty state
  if (animals.length === 0) {
    return (
      <EmptyState
        title={emptyMessage}
        description={emptyDescription}
        icon={
          <svg
            className="h-16 w-16 text-gray-300"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1}
              d="M12 19c-4.3 0-8-2.5-9.8-6a11.5 11.5 0 0 1 0-2c1.8-3.5 5.5-6 9.8-6s8 2.5 9.8 6c.3.6.3 1.4 0 2-1.8 3.5-5.5 6-9.8 6z"
            />
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1}
              d="M12 14a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"
            />
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 3l18 18"
            />
          </svg>
        }
      />
    );
  }

  // Grid with animals
  return (
    <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {animals.map((animal) => (
        <AnimalCard
          key={animal.id}
          animal={animal}
          isFavorite={favorites.includes(animal.id)}
          onToggleFavorite={onToggleFavorite}
          showAdoptButton={showAdoptButton}
        />
      ))}
    </div>
  );
}

// Skeleton loader for initial loading
export function AnimalGridSkeleton({ count = 8 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: count }).map((_, index) => (
        <div
          key={index}
          className="bg-white rounded-xl border border-gray-200 overflow-hidden animate-pulse"
        >
          <div className="aspect-square bg-gray-200" />
          <div className="p-4 space-y-3">
            <div className="h-5 bg-gray-200 rounded w-3/4" />
            <div className="h-4 bg-gray-200 rounded w-1/2" />
            <div className="flex gap-2">
              <div className="h-4 bg-gray-200 rounded w-16" />
              <div className="h-4 bg-gray-200 rounded w-16" />
            </div>
            <div className="h-10 bg-gray-200 rounded mt-4" />
          </div>
        </div>
      ))}
    </div>
  );
}
