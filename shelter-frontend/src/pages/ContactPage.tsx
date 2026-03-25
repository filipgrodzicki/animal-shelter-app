import { useState } from 'react';
import { useForm } from 'react-hook-form';
import {
  MapPinIcon,
  PhoneIcon,
  EnvelopeIcon,
  ClockIcon,
} from '@heroicons/react/24/outline';
import { PageContainer, PageHeader } from '@/components/layout';
import { Button, Input, Card } from '@/components/common';
import toast from 'react-hot-toast';

interface ContactFormData {
  name: string;
  email: string;
  phone?: string;
  subject: string;
  message: string;
}

const contactInfo = [
  {
    icon: MapPinIcon,
    title: 'Adres',
    content: ['ul. Przykładowa 123', '00-000 Warszawa'],
  },
  {
    icon: PhoneIcon,
    title: 'Telefon',
    content: ['+48 123 456 789', '+48 987 654 321'],
  },
  {
    icon: EnvelopeIcon,
    title: 'Email',
    content: ['kontakt@schronisko.pl', 'adopcje@schronisko.pl'],
  },
  {
    icon: ClockIcon,
    title: 'Godziny otwarcia',
    content: ['Pon-Pt: 10:00 - 18:00', 'Sob-Nd: 10:00 - 16:00'],
  },
];

export function ContactPage() {
  const [isLoading, setIsLoading] = useState(false);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ContactFormData>();

  const onSubmit = async (data: ContactFormData) => {
    setIsLoading(true);
    try {
      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('Contact form data:', data);
      toast.success('Wiadomość została wysłana. Odpowiemy najszybciej jak to możliwe.');
      reset();
    } catch {
      toast.error('Wystąpił błąd podczas wysyłania wiadomości. Spróbuj ponownie.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title="Kontakt"
        description="Masz pytania? Chętnie na nie odpowiemy. Skontaktuj się z nami."
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Contact form */}
        <div className="lg:col-span-2">
          <Card className="p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-6">Napisz do nas</h2>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <Input
                  label="Imię i nazwisko"
                  {...register('name', {
                    required: 'Imię i nazwisko jest wymagane',
                  })}
                  error={errors.name?.message}
                />

                <Input
                  label="Email"
                  type="email"
                  {...register('email', {
                    required: 'Email jest wymagany',
                    pattern: {
                      value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                      message: 'Nieprawidłowy adres email',
                    },
                  })}
                  error={errors.email?.message}
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <Input
                  label="Telefon (opcjonalnie)"
                  type="tel"
                  {...register('phone')}
                />

                <Input
                  label="Temat"
                  {...register('subject', {
                    required: 'Temat jest wymagany',
                  })}
                  error={errors.subject?.message}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Wiadomość
                </label>
                <textarea
                  className="input min-h-[150px]"
                  {...register('message', {
                    required: 'Wiadomość jest wymagana',
                    minLength: {
                      value: 20,
                      message: 'Wiadomość musi mieć min. 20 znaków',
                    },
                  })}
                />
                {errors.message && (
                  <p className="mt-1 text-sm text-red-600">{errors.message.message}</p>
                )}
              </div>

              <Button type="submit" isLoading={isLoading}>
                Wyślij wiadomość
              </Button>
            </form>
          </Card>
        </div>

        {/* Contact info */}
        <div className="space-y-6">
          {contactInfo.map((item) => (
            <Card key={item.title} className="p-4">
              <div className="flex gap-4">
                <item.icon className="h-6 w-6 text-primary-600 flex-shrink-0" />
                <div>
                  <h3 className="font-medium text-gray-900">{item.title}</h3>
                  {item.content.map((line, index) => (
                    <p key={index} className="text-gray-600 text-sm">
                      {line}
                    </p>
                  ))}
                </div>
              </div>
            </Card>
          ))}
        </div>
      </div>

      {/* Map placeholder */}
      <div id="location" className="mt-12">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">Jak do nas dojechać</h2>
        <div className="bg-gray-200 rounded-xl h-96 flex items-center justify-center">
          <div className="text-center text-gray-500">
            <MapPinIcon className="h-12 w-12 mx-auto mb-2" />
            <p>Mapa zostanie załadowana tutaj</p>
          </div>
        </div>
      </div>

      {/* Hours section */}
      <div id="hours" className="mt-12">
        <Card className="p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Godziny otwarcia</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <h3 className="font-medium text-gray-900 mb-2">Dla odwiedzających</h3>
              <ul className="space-y-1 text-gray-600">
                <li>Poniedziałek - Piątek: 10:00 - 18:00</li>
                <li>Sobota - Niedziela: 10:00 - 16:00</li>
              </ul>
            </div>
            <div>
              <h3 className="font-medium text-gray-900 mb-2">Biuro adopcji</h3>
              <ul className="space-y-1 text-gray-600">
                <li>Poniedziałek - Piątek: 9:00 - 17:00</li>
                <li>Sobota: 10:00 - 14:00</li>
                <li>Niedziela: zamknięte</li>
              </ul>
            </div>
          </div>
          <p className="mt-4 text-sm text-gray-500">
            * W święta schronisko jest zamknięte dla odwiedzających
          </p>
        </Card>
      </div>
    </PageContainer>
  );
}
