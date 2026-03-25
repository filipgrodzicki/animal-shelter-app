import { Link, useNavigate } from 'react-router-dom';
import { PhoneIcon, HomeIcon, DocumentTextIcon } from '@heroicons/react/24/outline';
import { Button, Card } from '@/components/common';
import { AnimalDetail, getStatusLabel } from '@/types';
import { useAuth } from '@/context/AuthContext';

interface AdoptButtonProps {
  animal: AnimalDetail;
  variant?: 'card' | 'inline' | 'fixed';
}

export function AdoptButton({ animal, variant = 'card' }: AdoptButtonProps) {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleAdoptClick = () => {
    if (isAuthenticated) {
      navigate(`/adoption/apply/${animal.id}`);
    } else {
      navigate('/login', { state: { from: { pathname: `/adoption/apply/${animal.id}` } } });
    }
  };
  const isAvailable = animal.status === 'Available';
  const isReserved = animal.status === 'Reserved';
  const isInProcess = animal.status === 'InAdoptionProcess';
  const canAdopt = isAvailable || isReserved;

  // Fixed mobile button
  if (variant === 'fixed') {
    if (!canAdopt) return null;

    return (
      <div className="fixed bottom-0 left-0 right-0 p-4 bg-white border-t border-gray-200 shadow-lg md:hidden z-40">
        <Button
          onClick={handleAdoptClick}
          className="w-full"
          size="lg"
        >
          <DocumentTextIcon className="h-5 w-5 mr-2" />
          {isReserved ? 'Złóż wniosek' : 'Adoptuj mnie'}
        </Button>
      </div>
    );
  }

  // Inline button
  if (variant === 'inline') {
    if (!canAdopt) {
      return (
        <div className="text-center p-4 bg-gray-100 rounded-lg">
          <p className="text-gray-600">
            {isInProcess
              ? 'Trwa proces adopcyjny tego zwierzęcia'
              : `Status: ${getStatusLabel(animal.status)}`}
          </p>
        </div>
      );
    }

    return (
      <Button
        onClick={handleAdoptClick}
        className="w-full"
        size="lg"
      >
        <DocumentTextIcon className="h-5 w-5 mr-2" />
        {isReserved ? 'Złóż wniosek' : 'Adoptuj mnie'}
      </Button>
    );
  }

  // Card variant (default)
  return (
    <Card className="overflow-hidden sticky top-24">
      <div className="p-6">
        {canAdopt ? (
          <>
            <div className="text-center mb-6">
              <div className="w-16 h-16 mx-auto mb-4 bg-primary-100 rounded-full flex items-center justify-center">
                <HomeIcon className="h-8 w-8 text-primary-600" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">
                Chcesz mnie adoptować?
              </h3>
              <p className="text-gray-600 text-sm">
                {isReserved
                  ? 'To zwierzę jest wstępnie zarezerwowane, ale możesz złożyć wniosek na wypadek, gdyby adopcja nie doszła do skutku.'
                  : 'Wypełnij formularz adopcyjny, a skontaktujemy się z Tobą w ciągu 2-3 dni roboczych.'}
              </p>
            </div>

            <Button
              onClick={handleAdoptClick}
              className="w-full"
              size="lg"
            >
              <DocumentTextIcon className="h-5 w-5 mr-2" />
              {isReserved ? 'Złóż wniosek' : 'Adoptuj mnie'}
            </Button>

            <div className="mt-4 text-center">
              <p className="text-xs text-gray-500">
                Adopcja jest bezpłatna. Prosimy jedynie o dobrowolną darowiznę.
              </p>
            </div>
          </>
        ) : (
          <>
            <div className="text-center mb-6">
              <div className="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
                <HomeIcon className="h-8 w-8 text-gray-400" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">
                {isInProcess ? 'W procesie adopcji' : 'Niedostępne do adopcji'}
              </h3>
              <p className="text-gray-600 text-sm">
                {isInProcess
                  ? 'To zwierzę jest obecnie w procesie adopcyjnym. Sprawdź inne zwierzęta!'
                  : `Aktualny status: ${getStatusLabel(animal.status)}`}
              </p>
            </div>

            <Button as={Link} to="/animals" variant="outline" className="w-full">
              Zobacz inne zwierzęta
            </Button>
          </>
        )}

        {/* Contact section */}
        <div className="mt-6 pt-6 border-t border-gray-100">
          <p className="text-sm text-gray-600 text-center mb-3">
            Masz pytania o {animal.name}?
          </p>
          <Button
            as={Link}
            to="/contact"
            variant="outline"
            className="w-full"
            size="sm"
          >
            <PhoneIcon className="h-4 w-4 mr-2" />
            Skontaktuj się z nami
          </Button>
        </div>
      </div>
    </Card>
  );
}
