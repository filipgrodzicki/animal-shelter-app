import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { Button, Input, Card } from '@/components/common';
import { PageContainer } from '@/components/layout';
import { apiClient } from '@/api/client';
import toast from 'react-hot-toast';

interface VolunteerRegisterFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  address?: string;
  city?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  skills: string[];
  availability: number[];
  notes?: string;
  terms: boolean;
}

const availableSkills = [
  'Opieka nad psami',
  'Opieka nad kotami',
  'Opieka nad innymi zwierzętami',
  'Sprzątanie',
  'Karmienie',
  'Spacery',
  'Transport',
  'Pierwsza pomoc',
  'Fotografia',
  'Social media',
  'Organizacja wydarzeń',
];

const daysOfWeek = [
  { value: 0, label: 'Niedziela' },
  { value: 1, label: 'Poniedzialek' },
  { value: 2, label: 'Wtorek' },
  { value: 3, label: 'Sroda' },
  { value: 4, label: 'Czwartek' },
  { value: 5, label: 'Piatek' },
  { value: 6, label: 'Sobota' },
];

export function VolunteerRegisterPage() {
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<VolunteerRegisterFormData>({
    defaultValues: {
      skills: [],
      availability: [],
    },
  });

  const onSubmit = async (data: VolunteerRegisterFormData) => {
    setIsLoading(true);
    try {
      const { terms, ...volunteerData } = data;
      await apiClient.post('/volunteers/register', {
        ...volunteerData,
        dateOfBirth: new Date(data.dateOfBirth).toISOString(),
      });
      toast.success('Zgloszenie zostalo wyslane pomyslnie! Skontaktujemy sie z Toba wkrotce.');
      navigate('/volunteer');
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Wystapil blad podczas wysylania zgloszenia';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  const today = new Date();
  const maxDate = new Date(today.getFullYear() - 16, today.getMonth(), today.getDate())
    .toISOString()
    .split('T')[0];

  return (
    <div className="py-12">
      <PageContainer>
        <div className="max-w-2xl mx-auto">
          <div className="text-center mb-8">
            <h1 className="font-heading text-3xl font-bold text-gray-900">
              Formularz zgloszeniowy wolontariusza
            </h1>
            <p className="mt-2 text-gray-600">
              Wypelnij ponizszy formularz, aby zglosic sie jako wolontariusz w naszym schronisku.
            </p>
          </div>

          <Card className="p-8">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
              {/* Dane osobowe */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Dane osobowe</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="Imie *"
                    {...register('firstName', {
                      required: 'Imie jest wymagane',
                      minLength: { value: 2, message: 'Imie musi miec min. 2 znaki' },
                    })}
                    error={errors.firstName?.message}
                  />

                  <Input
                    label="Nazwisko *"
                    {...register('lastName', {
                      required: 'Nazwisko jest wymagane',
                      minLength: { value: 2, message: 'Nazwisko musi miec min. 2 znaki' },
                    })}
                    error={errors.lastName?.message}
                  />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                  <Input
                    label="Email *"
                    type="email"
                    {...register('email', {
                      required: 'Email jest wymagany',
                      pattern: {
                        value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                        message: 'Nieprawidlowy adres email',
                      },
                    })}
                    error={errors.email?.message}
                  />

                  <Input
                    label="Numer telefonu *"
                    type="tel"
                    {...register('phone', {
                      required: 'Numer telefonu jest wymagany',
                      pattern: {
                        value: /^[0-9+\s-]{9,15}$/,
                        message: 'Nieprawidlowy numer telefonu',
                      },
                    })}
                    error={errors.phone?.message}
                  />
                </div>

                <div className="mt-4">
                  <Input
                    label="Data urodzenia *"
                    type="date"
                    max={maxDate}
                    {...register('dateOfBirth', {
                      required: 'Data urodzenia jest wymagana',
                    })}
                    error={errors.dateOfBirth?.message}
                  />
                  <p className="mt-1 text-sm text-gray-500">
                    Wolontariusz musi miec ukonczony 16 lat
                  </p>
                </div>
              </div>

              {/* Adres */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Adres zamieszkania</h2>
                <Input
                  label="Ulica i numer"
                  {...register('address')}
                />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                  <Input
                    label="Miasto"
                    {...register('city')}
                  />
                  <Input
                    label="Kod pocztowy"
                    {...register('postalCode', {
                      pattern: {
                        value: /^\d{2}-\d{3}$/,
                        message: 'Format: XX-XXX',
                      },
                    })}
                    placeholder="00-000"
                    error={errors.postalCode?.message}
                  />
                </div>
              </div>

              {/* Kontakt w nagłych wypadkach */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">
                  Kontakt w naglych wypadkach
                </h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="Imie i nazwisko"
                    {...register('emergencyContactName')}
                  />
                  <Input
                    label="Numer telefonu"
                    type="tel"
                    {...register('emergencyContactPhone', {
                      pattern: {
                        value: /^[0-9+\s-]{9,15}$/,
                        message: 'Nieprawidlowy numer telefonu',
                      },
                    })}
                    error={errors.emergencyContactPhone?.message}
                  />
                </div>
              </div>

              {/* Umiejętności */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Umiejetnosci</h2>
                <p className="text-sm text-gray-600 mb-3">
                  Zaznacz umiejetnosci, ktore moga byc przydatne podczas wolontariatu:
                </p>
                <Controller
                  name="skills"
                  control={control}
                  render={({ field }) => (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                      {availableSkills.map((skill) => (
                        <label key={skill} className="flex items-center gap-2 cursor-pointer">
                          <input
                            type="checkbox"
                            className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                            checked={field.value.includes(skill)}
                            onChange={(e) => {
                              if (e.target.checked) {
                                field.onChange([...field.value, skill]);
                              } else {
                                field.onChange(field.value.filter((s) => s !== skill));
                              }
                            }}
                          />
                          <span className="text-sm text-gray-700">{skill}</span>
                        </label>
                      ))}
                    </div>
                  )}
                />
              </div>

              {/* Dostępność */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Dostepnosc</h2>
                <p className="text-sm text-gray-600 mb-3">
                  Zaznacz dni tygodnia, w ktore mozesz byc dostepny:
                </p>
                <Controller
                  name="availability"
                  control={control}
                  rules={{ required: 'Wybierz przynajmniej jeden dzien dostepnosci' }}
                  render={({ field }) => (
                    <div className="flex flex-wrap gap-2">
                      {daysOfWeek.map((day) => (
                        <label
                          key={day.value}
                          className={`flex items-center gap-2 px-4 py-2 rounded-lg border cursor-pointer transition-colors ${
                            field.value.includes(day.value)
                              ? 'bg-primary-50 border-primary-500 text-primary-700'
                              : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50'
                          }`}
                        >
                          <input
                            type="checkbox"
                            className="sr-only"
                            checked={field.value.includes(day.value)}
                            onChange={(e) => {
                              if (e.target.checked) {
                                field.onChange([...field.value, day.value]);
                              } else {
                                field.onChange(field.value.filter((d) => d !== day.value));
                              }
                            }}
                          />
                          <span className="text-sm font-medium">{day.label}</span>
                        </label>
                      ))}
                    </div>
                  )}
                />
                {errors.availability && (
                  <p className="mt-1 text-sm text-red-600">{errors.availability.message}</p>
                )}
              </div>

              {/* Notatki */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Dodatkowe informacje</h2>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Napisz kilka slow o sobie i swojej motywacji
                  </label>
                  <textarea
                    className="w-full rounded-lg border border-gray-300 px-4 py-2 focus:border-primary-500 focus:ring-primary-500"
                    rows={4}
                    {...register('notes')}
                    placeholder="Opowiedz nam o swoim doswiadczeniu ze zwierzetami, dlaczego chcesz zostac wolontariuszem..."
                  />
                </div>
              </div>

              {/* Zgody */}
              <div>
                <label className="flex items-start gap-2">
                  <input
                    type="checkbox"
                    className="h-4 w-4 mt-1 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                    {...register('terms', {
                      required: 'Musisz zaakceptowac regulamin',
                    })}
                  />
                  <span className="text-sm text-gray-600">
                    Akceptuje{' '}
                    <Link to="/terms" className="text-primary-600 hover:text-primary-700">
                      regulamin wolontariatu
                    </Link>{' '}
                    oraz{' '}
                    <Link to="/privacy" className="text-primary-600 hover:text-primary-700">
                      polityke prywatnosci
                    </Link>
                    . Wyrazam zgode na przetwarzanie moich danych osobowych w celach zwiazanych z wolontariatem.
                  </span>
                </label>
                {errors.terms && (
                  <p className="mt-1 text-sm text-red-600">{errors.terms.message}</p>
                )}
              </div>

              <div className="flex gap-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate('/volunteer')}
                >
                  Anuluj
                </Button>
                <Button type="submit" className="flex-1" isLoading={isLoading}>
                  Wyslij zgloszenie
                </Button>
              </div>
            </form>
          </Card>
        </div>
      </PageContainer>
    </div>
  );
}
