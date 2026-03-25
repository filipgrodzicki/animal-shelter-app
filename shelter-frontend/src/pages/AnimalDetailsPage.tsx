import { useParams, Link } from 'react-router-dom';
import { ArrowLeftIcon, ShareIcon } from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import {
  AnimalPhotoGallery,
  AnimalInfo,
  AnimalCharacteristics,
  AnimalMedicalHistory,
  MedicalSummary,
  AdoptButton,
} from '@/components/animals';
import { Spinner, Button } from '@/components/common';
import { useAnimal } from '@/hooks';
import { useAuth } from '@/context/AuthContext';
import { isStaff } from '@/types';
import toast from 'react-hot-toast';

export function AnimalDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { animal, isLoading, error, refetch } = useAnimal(id);
  const { user } = useAuth();
  const userIsStaff = isStaff(user);

  const handleShare = async () => {
    const url = window.location.href;
    const title = animal ? `${animal.name} - Schronisko dla Zwierząt` : 'Schronisko dla Zwierząt';

    if (navigator.share) {
      try {
        await navigator.share({ title, url });
      } catch (err) {
        // User cancelled sharing
      }
    } else {
      await navigator.clipboard.writeText(url);
      toast.success('Link skopiowany do schowka');
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <PageContainer>
        <div className="flex flex-col items-center justify-center py-24">
          <Spinner size="lg" />
          <p className="mt-4 text-gray-500">Ładowanie danych zwierzęcia...</p>
        </div>
      </PageContainer>
    );
  }

  // Error state
  if (error || !animal) {
    return (
      <PageContainer>
        <div className="text-center py-24">
          <img
            src="/images/dog_1887470.png"
            alt=""
            className="w-20 h-20 mx-auto mb-6 object-contain opacity-40"
          />
          <h2 className="text-2xl font-bold text-warm-900 mb-4">
            Nie znaleziono zwierzęcia
          </h2>
          <p className="text-gray-600 mb-8 max-w-md mx-auto">
            Przepraszamy, ale nie możemy znaleźć tego zwierzęcia.
            Mogło zostać adoptowane lub usunięte z systemu.
          </p>
          <div className="flex flex-wrap justify-center gap-4">
            <Button onClick={() => refetch()} variant="outline">
              Spróbuj ponownie
            </Button>
            <Button as={Link} to="/animals">
              <ArrowLeftIcon className="h-4 w-4 mr-2" />
              Wróć do listy zwierząt
            </Button>
          </div>
        </div>
      </PageContainer>
    );
  }

  return (
    <>
      <PageContainer className="pb-24 md:pb-8">
        {/* Navigation and actions */}
        <div className="flex items-center justify-between mb-6">
          <Link
            to="/animals"
            className="inline-flex items-center text-gray-600 hover:text-primary-600 transition-colors"
          >
            <ArrowLeftIcon className="h-4 w-4 mr-2" />
            Wróć do listy
          </Link>

          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={handleShare}
              className="hidden sm:flex"
            >
              <ShareIcon className="h-4 w-4 mr-1" />
              Udostępnij
            </Button>
          </div>
        </div>

        {/* Main content grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left column - Photos and details */}
          <div className="lg:col-span-2 space-y-6">
            {/* Photo gallery */}
            <AnimalPhotoGallery
              photos={animal.photos}
              animalName={animal.name}
            />

            {/* Basic info - visible on desktop */}
            <div className="hidden lg:block">
              <AnimalInfo animal={animal} showSensitiveData={userIsStaff} />
            </div>

            {/* Characteristics */}
            <AnimalCharacteristics animal={animal} />

            {/* Medical history - only for staff */}
            {userIsStaff && animal.medicalRecords && animal.medicalRecords.length > 0 && (
              <AnimalMedicalHistory records={animal.medicalRecords} />
            )}
          </div>

          {/* Right column - Sidebar */}
          <div className="space-y-6">
            {/* Basic info - visible on mobile/tablet */}
            <div className="lg:hidden">
              <AnimalInfo animal={animal} showSensitiveData={userIsStaff} />
            </div>

            {/* Medical summary */}
            {animal.medicalRecords && animal.medicalRecords.length > 0 && (
              <div className="hidden lg:block">
                <MedicalSummary records={animal.medicalRecords} />
              </div>
            )}

            {/* Adopt button card - desktop */}
            <div className="hidden md:block">
              <AdoptButton animal={animal} variant="card" />
            </div>

          </div>
        </div>
      </PageContainer>

      {/* Fixed adopt button - mobile */}
      <AdoptButton animal={animal} variant="fixed" />
    </>
  );
}
