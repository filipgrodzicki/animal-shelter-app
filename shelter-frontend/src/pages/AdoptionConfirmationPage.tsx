import { Link, useLocation, useParams } from 'react-router-dom';
import {
  CheckCircleIcon,
  EnvelopeIcon,
  PhoneIcon,
  ClockIcon,
  HomeIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card } from '@/components/common';

interface LocationState {
  applicationNumber?: string;
  animalName?: string;
}

export function AdoptionConfirmationPage() {
  const { applicationId } = useParams<{ applicationId: string }>();
  const location = useLocation();
  const state = location.state as LocationState | null;

  const applicationNumber = state?.applicationNumber || applicationId || 'N/A';
  const animalName = state?.animalName || 'zwierzęcia';

  return (
    <PageContainer>
      <div className="max-w-2xl mx-auto py-12">
        {/* Success message */}
        <div className="text-center mb-8">
          <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircleIcon className="w-12 h-12 text-green-600" />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-4">
            Wniosek został wysłany!
          </h1>
          <p className="text-gray-600 text-lg">
            Dziękujemy za złożenie wniosku o adopcję {animalName}.
            Twoje zgłoszenie zostało zarejestrowane w naszym systemie.
          </p>
        </div>

        {/* Application number */}
        <Card className="p-6 mb-8 bg-primary-50 border-primary-200">
          <div className="text-center">
            <p className="text-sm text-primary-600 mb-1">Numer zgłoszenia</p>
            <p className="text-2xl font-bold text-primary-700 font-mono">
              {applicationNumber}
            </p>
            <p className="text-sm text-primary-600 mt-2">
              Zachowaj ten numer - będzie potrzebny do śledzenia statusu wniosku
            </p>
          </div>
        </Card>

        {/* What's next */}
        <Card className="p-6 mb-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">Co dalej?</h2>

          <div className="space-y-6">
            <NextStep
              icon={<EnvelopeIcon className="w-6 h-6" />}
              title="Potwierdzenie email"
              description="Na podany adres email wyślemy potwierdzenie przyjęcia wniosku wraz z numerem zgłoszenia."
            />

            <NextStep
              icon={<ClockIcon className="w-6 h-6" />}
              title="Rozpatrywanie wniosku"
              description="Nasz zespół przeanalizuje Twój wniosek w ciągu 2-5 dni roboczych. Możemy poprosić o dodatkowe informacje."
            />

            <NextStep
              icon={<PhoneIcon className="w-6 h-6" />}
              title="Kontakt"
              description="Skontaktujemy się z Tobą telefonicznie lub mailowo, aby omówić szczegóły i ewentualnie umówić wizytę."
            />

            <NextStep
              icon={<HomeIcon className="w-6 h-6" />}
              title="Wizyta"
              description="W niektórych przypadkach prosimy o wizytę w schronisku lub przeprowadzamy wizytę przedadopcyjną."
            />
          </div>
        </Card>

        {/* Contact info */}
        <Card className="p-6 mb-8 bg-gray-50">
          <h3 className="font-semibold text-gray-900 mb-4">Masz pytania?</h3>
          <p className="text-gray-600 mb-4">
            Jeśli chcesz zapytać o status swojego wniosku lub masz dodatkowe pytania,
            skontaktuj się z nami:
          </p>
          <div className="space-y-2 text-gray-700">
            <p>
              <span className="font-medium">Email:</span>{' '}
              <a href="mailto:adopcje@schronisko.pl" className="text-primary-600 hover:underline">
                adopcje@schronisko.pl
              </a>
            </p>
            <p>
              <span className="font-medium">Telefon:</span>{' '}
              <a href="tel:+48123456789" className="text-primary-600 hover:underline">
                +48 123 456 789
              </a>
            </p>
            <p className="text-sm text-gray-500 mt-2">
              Przy kontakcie podaj numer zgłoszenia: <strong>{applicationNumber}</strong>
            </p>
          </div>
        </Card>

        {/* Actions */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Button as={Link} to="/">
            <HomeIcon className="w-4 h-4 mr-2" />
            Strona główna
          </Button>
          <Button as={Link} to="/animals" variant="outline">
            Zobacz inne zwierzęta
          </Button>
        </div>
      </div>
    </PageContainer>
  );
}

interface NextStepProps {
  icon: React.ReactNode;
  title: string;
  description: string;
}

function NextStep({ icon, title, description }: NextStepProps) {
  return (
    <div className="flex gap-4">
      <div className="flex-shrink-0 w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center text-primary-600">
        {icon}
      </div>
      <div>
        <h3 className="font-medium text-gray-900">{title}</h3>
        <p className="text-gray-600 text-sm mt-1">{description}</p>
      </div>
    </div>
  );
}
