import { UseFormRegister, FieldErrors } from 'react-hook-form';
import { UserIcon } from '@heroicons/react/24/outline';
import { Input } from '@/components/common';
import { AdoptionFormData } from './adoptionFormSchema';

interface PersonalInfoStepProps {
  register: UseFormRegister<AdoptionFormData>;
  errors: FieldErrors<AdoptionFormData>;
}

export function PersonalInfoStep({ register, errors }: PersonalInfoStepProps) {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
          <UserIcon className="w-5 h-5 text-primary-600" />
        </div>
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Dane osobowe</h2>
          <p className="text-sm text-gray-500">Podaj swoje dane kontaktowe</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label="Imię"
          {...register('firstName')}
          error={errors.firstName?.message}
          placeholder="Jan"
        />

        <Input
          label="Nazwisko"
          {...register('lastName')}
          error={errors.lastName?.message}
          placeholder="Kowalski"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label="Email"
          type="email"
          {...register('email')}
          error={errors.email?.message}
          placeholder="jan.kowalski@example.com"
        />

        <Input
          label="Telefon"
          type="tel"
          {...register('phone')}
          error={errors.phone?.message}
          placeholder="+48 123 456 789"
        />
      </div>

      <Input
        label="Data urodzenia"
        type="date"
        {...register('dateOfBirth')}
        error={errors.dateOfBirth?.message}
        helperText="Musisz mieć ukończone 18 lat"
      />

      <div className="border-t border-gray-200 pt-6 mt-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Adres zamieszkania</h3>

        <Input
          label="Ulica i numer"
          {...register('street')}
          error={errors.street?.message}
          placeholder="ul. Przykładowa 123/4"
        />

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
          <Input
            label="Miasto"
            {...register('city')}
            error={errors.city?.message}
            placeholder="Warszawa"
          />

          <Input
            label="Kod pocztowy"
            {...register('postalCode')}
            error={errors.postalCode?.message}
            placeholder="00-000"
          />
        </div>
      </div>
    </div>
  );
}
