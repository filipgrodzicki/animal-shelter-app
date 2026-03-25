import { Link } from 'react-router-dom';
import {
  HeartIcon,
  UserGroupIcon,
  AcademicCapIcon,
  SparklesIcon,
  CheckCircleIcon,
  ClockIcon,
  CalendarIcon,
} from '@heroicons/react/24/outline';
import { PageContainer, PageHeader } from '@/components/layout';
import { Button, Card } from '@/components/common';

const benefits = [
  {
    icon: HeartIcon,
    title: 'Pomagasz zwierzętom',
    description: 'Twoja praca bezpośrednio wpływa na życie zwierząt w schronisku.',
  },
  {
    icon: UserGroupIcon,
    title: 'Poznajesz wspaniałych ludzi',
    description: 'Dołączasz do społeczności ludzi kochających zwierzęta.',
  },
  {
    icon: AcademicCapIcon,
    title: 'Zdobywasz doświadczenie',
    description: 'Uczysz się opieki nad zwierzętami od profesjonalistów.',
  },
  {
    icon: SparklesIcon,
    title: 'Rozwijasz się',
    description: 'Wolontariat to świetna okazja do rozwoju osobistego.',
  },
];

const activities = [
  {
    title: 'Spacery z psami',
    description: 'Codzienne spacery są niezbędne dla zdrowia i dobrostanu naszych psów.',
  },
  {
    title: 'Socjalizacja kotów',
    description: 'Pomagasz kotom przyzwyczaić się do kontaktu z ludźmi.',
  },
  {
    title: 'Pomoc przy karmieniu',
    description: 'Przygotowywanie posiłków i karmienie zwierząt.',
  },
  {
    title: 'Sprzątanie',
    description: 'Utrzymanie czystości w boksach i pomieszczeniach.',
  },
  {
    title: 'Eventy i zbiórki',
    description: 'Pomoc przy organizacji wydarzeń i akcji promocyjnych.',
  },
  {
    title: 'Transport',
    description: 'Przewożenie zwierząt do weterynarza lub na wizyty adopcyjne.',
  },
];

const steps = [
  {
    number: '1',
    title: 'Wypełnij formularz',
    description: 'Zgłoś się przez formularz online - zajmie Ci to tylko 5 minut.',
  },
  {
    number: '2',
    title: 'Rozmowa telefoniczna',
    description: 'Skontaktujemy się z Tobą w ciągu 7 dni, by omówić szczegóły.',
  },
  {
    number: '3',
    title: 'Szkolenie wstępne',
    description: 'Weź udział w 3-godzinnym szkoleniu z naszym zespołem.',
  },
  {
    number: '4',
    title: 'Rozpocznij wolontariat',
    description: 'Zacznij pomagać pod opieką doświadczonego wolontariusza.',
  },
];

const requirements = [
  'Ukończone 16 lat (osoby młodsze z opiekunem)',
  'Dyspozycyjność min. 2 godziny tygodniowo',
  'Odpowiedzialność i zaangażowanie',
  'Udział w szkoleniu wstępnym',
  'Brak przeciwwskazań zdrowotnych',
];

export function VolunteerPage() {
  return (
    <PageContainer>
      <PageHeader
        title="Zostań wolontariuszem"
        description="Dołącz do naszego zespołu i pomagaj zwierzętom każdego dnia. Twoja pomoc ma ogromne znaczenie!"
      />

      {/* Benefits */}
      <section className="mb-16">
        <h2 className="text-2xl font-bold text-gray-900 mb-8 text-center">
          Dlaczego warto zostać wolontariuszem?
        </h2>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          {benefits.map((benefit) => (
            <Card key={benefit.title} className="p-6 text-center">
              <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center mx-auto mb-4">
                <benefit.icon className="h-6 w-6 text-primary-600" />
              </div>
              <h3 className="font-semibold text-gray-900 mb-2">{benefit.title}</h3>
              <p className="text-gray-600 text-sm">{benefit.description}</p>
            </Card>
          ))}
        </div>
      </section>

      {/* Activities */}
      <section className="mb-16">
        <h2 className="text-2xl font-bold text-gray-900 mb-8 text-center">
          Co możesz robić jako wolontariusz?
        </h2>
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {activities.map((activity) => (
            <Card key={activity.title} className="p-6">
              <h3 className="font-semibold text-gray-900 mb-2">{activity.title}</h3>
              <p className="text-gray-600 text-sm">{activity.description}</p>
            </Card>
          ))}
        </div>
      </section>

      {/* How it works */}
      <section className="mb-16">
        <h2 className="text-2xl font-bold text-gray-900 mb-8 text-center">
          Jak zostać wolontariuszem?
        </h2>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          {steps.map((step) => (
            <div key={step.number} className="text-center">
              <div className="w-12 h-12 rounded-full bg-primary-600 text-white font-bold flex items-center justify-center mx-auto mb-4">
                {step.number}
              </div>
              <h3 className="font-semibold text-gray-900 mb-2">{step.title}</h3>
              <p className="text-gray-600 text-sm">{step.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Requirements */}
      <section className="mb-16">
        <Card className="p-8 max-w-2xl mx-auto">
          <div className="flex items-center gap-3 mb-6">
            <CheckCircleIcon className="w-8 h-8 text-primary-600" />
            <h2 className="text-2xl font-bold text-gray-900">Wymagania</h2>
          </div>
          <ul className="space-y-3">
            {requirements.map((req) => (
              <li key={req} className="flex items-start gap-3">
                <CheckCircleIcon className="w-5 h-5 text-primary-600 flex-shrink-0 mt-0.5" />
                <span className="text-gray-700">{req}</span>
              </li>
            ))}
          </ul>
          <div className="mt-6 pt-6 border-t border-gray-200 flex items-center gap-6 text-sm text-gray-500">
            <div className="flex items-center gap-2">
              <ClockIcon className="w-5 h-5" />
              <span>Min. 2h/tydzień</span>
            </div>
            <div className="flex items-center gap-2">
              <CalendarIcon className="w-5 h-5" />
              <span>Elastyczny grafik</span>
            </div>
          </div>
        </Card>
      </section>

      {/* CTA */}
      <section className="text-center bg-primary-50 rounded-2xl p-8 md:p-12">
        <h2 className="text-2xl md:text-3xl font-bold text-gray-900 mb-4">
          Gotowy, aby zacząć pomagać?
        </h2>
        <p className="text-gray-600 mb-8 max-w-xl mx-auto">
          Wypełnij formularz zgłoszeniowy i dołącz do naszego zespołu wolontariuszy.
          Razem możemy więcej!
        </p>
        <Button as={Link} to="/volunteer/register" size="lg">
          Zapisz się teraz
        </Button>
      </section>
    </PageContainer>
  );
}
