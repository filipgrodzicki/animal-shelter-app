import { Link } from 'react-router-dom';
import {
  ClipboardDocumentCheckIcon,
  HomeIcon,
  HeartIcon,
  DocumentTextIcon,
  CheckBadgeIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card } from '@/components/common';

const steps = [
  {
    icon: ClipboardDocumentCheckIcon,
    title: 'Wypełnij wniosek',
    description: 'Wypełnij formularz adopcyjny online, podając informacje o sobie i swoim domu.',
  },
  {
    icon: DocumentTextIcon,
    title: 'Weryfikacja',
    description: 'Nasz zespół przeanalizuje Twój wniosek i skontaktuje się w celu umówienia wizyty.',
  },
  {
    icon: HomeIcon,
    title: 'Wizyta w schronisku',
    description: 'Odwiedź schronisko, aby poznać wybrane zwierzę i upewnić się, że pasujecie do siebie.',
  },
  {
    icon: CheckBadgeIcon,
    title: 'Wizyta przedadopcyjna',
    description: 'W niektórych przypadkach przeprowadzamy wizytę domową, aby sprawdzić warunki.',
  },
  {
    icon: HeartIcon,
    title: 'Adopcja',
    description: 'Podpisz umowę adopcyjną i zabierz swojego nowego przyjaciela do domu!',
  },
];

const requirements = [
  'Ukończone 18 lat',
  'Stały adres zamieszkania',
  'Zgoda wszystkich domowników na adopcję',
  'Możliwość zapewnienia odpowiednich warunków bytowych',
  'Gotowość do ponoszenia kosztów utrzymania zwierzęcia',
  'Czas i cierpliwość na opiekę nad zwierzęciem',
];

const faqs = [
  {
    question: 'Ile kosztuje adopcja?',
    answer: 'Adopcja jest bezpłatna, ale prosimy o dobrowolną darowiznę na rzecz schroniska. Zwierzę przekazujemy zaszczepione, zaczipowane i wysterylizowane/wykastrowane.',
  },
  {
    question: 'Czy mogę adoptować, jeśli mieszkam w wynajmowanym mieszkaniu?',
    answer: 'Tak, ale wymagamy pisemnej zgody właściciela mieszkania na posiadanie zwierzęcia.',
  },
  {
    question: 'Ile trwa proces adopcyjny?',
    answer: 'Standardowo od kilku dni do dwóch tygodni, w zależności od dostępności terminów i rodzaju zwierzęcia.',
  },
  {
    question: 'Co jeśli adopcja się nie powiedzie?',
    answer: 'Prosimy o zwrot zwierzęcia do naszego schroniska. Nigdy nie porzucaj zwierzęcia - zawsze możesz je do nas oddać.',
  },
];

export function AdoptionPage() {
  return (
    <>
      {/* Hero */}
      <section className="bg-primary-600 text-white py-16">
        <PageContainer>
          <div className="max-w-3xl">
            <h1 className="text-4xl font-bold mb-4">Proces adopcji</h1>
            <p className="text-xl text-primary-100">
              Adopcja to poważna decyzja i wielka odpowiedzialność. Chcemy mieć pewność,
              że każde zwierzę trafi do kochającego, odpowiedzialnego domu.
            </p>
          </div>
        </PageContainer>
      </section>

      {/* Steps */}
      <section className="py-16 bg-white">
        <PageContainer>
          <h2 className="text-2xl font-bold text-gray-900 mb-8 text-center">
            Jak przebiega adopcja?
          </h2>

          <div className="relative">
            {/* Connection line */}
            <div className="absolute top-8 left-8 right-8 h-0.5 bg-gray-200 hidden lg:block" />

            <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-6">
              {steps.map((step, index) => (
                <div key={step.title} className="relative text-center">
                  <div className="bg-white relative z-10 mb-4 inline-flex">
                    <div className="w-16 h-16 rounded-full bg-primary-100 flex items-center justify-center mx-auto">
                      <step.icon className="h-8 w-8 text-primary-600" />
                    </div>
                  </div>
                  <div className="absolute -top-2 -right-2 w-6 h-6 bg-primary-600 text-white text-sm rounded-full flex items-center justify-center font-semibold hidden lg:flex">
                    {index + 1}
                  </div>
                  <h3 className="font-semibold text-gray-900 mb-2">{step.title}</h3>
                  <p className="text-sm text-gray-600">{step.description}</p>
                </div>
              ))}
            </div>
          </div>
        </PageContainer>
      </section>

      {/* Requirements */}
      <section className="py-16 bg-gray-50">
        <PageContainer>
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
            <div>
              <h2 className="text-2xl font-bold text-gray-900 mb-6">Wymagania</h2>
              <p className="text-gray-600 mb-6">
                Aby adoptować zwierzę z naszego schroniska, musisz spełniać następujące warunki:
              </p>
              <ul className="space-y-3">
                {requirements.map((req) => (
                  <li key={req} className="flex items-start gap-3">
                    <CheckBadgeIcon className="h-5 w-5 text-green-500 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-700">{req}</span>
                  </li>
                ))}
              </ul>
            </div>

            <Card className="p-6">
              <h2 className="text-2xl font-bold text-gray-900 mb-6">Gotowy do adopcji?</h2>
              <p className="text-gray-600 mb-6">
                Przejrzyj nasze zwierzęta i znajdź swojego idealnego towarzysza. Gdy będziesz gotowy,
                wypełnij formularz adopcyjny przy wybranym zwierzęciu.
              </p>
              <div className="space-y-3">
                <Button as={Link} to="/animals" className="w-full">
                  Zobacz zwierzęta do adopcji
                </Button>
                <Button as={Link} to="/contact" variant="outline" className="w-full">
                  Masz pytania? Skontaktuj się
                </Button>
              </div>
            </Card>
          </div>
        </PageContainer>
      </section>

      {/* FAQ */}
      <section className="py-16 bg-white">
        <PageContainer>
          <h2 className="text-2xl font-bold text-gray-900 mb-8 text-center">
            Najczęściej zadawane pytania
          </h2>

          <div className="max-w-3xl mx-auto space-y-6">
            {faqs.map((faq) => (
              <Card key={faq.question} className="p-6">
                <h3 className="font-semibold text-gray-900 mb-2">{faq.question}</h3>
                <p className="text-gray-600">{faq.answer}</p>
              </Card>
            ))}
          </div>
        </PageContainer>
      </section>
    </>
  );
}
