import { UseFormRegister, FieldErrors, UseFormWatch } from 'react-hook-form';
import { HeartIcon } from '@heroicons/react/24/outline';
import { AdoptionFormData } from './adoptionFormSchema';

interface MotivationStepProps {
  register: UseFormRegister<AdoptionFormData>;
  errors: FieldErrors<AdoptionFormData>;
  watch: UseFormWatch<AdoptionFormData>;
  animalName?: string;
}

export function MotivationStep({
  register,
  errors,
  watch,
  animalName,
}: MotivationStepProps) {
  const whyAdopt = watch('whyAdopt') || '';
  const howCare = watch('howCare') || '';

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
          <HeartIcon className="w-5 h-5 text-primary-600" />
        </div>
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Motywacja</h2>
          <p className="text-sm text-gray-500">
            Pomóż nam lepiej zrozumieć Twoje intencje
          </p>
        </div>
      </div>

      {/* Why adopt */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Dlaczego chcesz adoptować {animalName ? `${animalName}` : 'to zwierzę'}?
          <span className="text-red-500 ml-1">*</span>
        </label>
        <textarea
          {...register('whyAdopt')}
          placeholder="Opisz, co skłoniło Cię do decyzji o adopcji tego konkretnego zwierzęcia. Co Cię w nim przyciągnęło? Dlaczego uważasz, że będziecie do siebie pasować?"
          className="input min-h-[150px]"
        />
        <div className="flex justify-between mt-1">
          {errors.whyAdopt ? (
            <p className="text-sm text-red-600">{errors.whyAdopt.message}</p>
          ) : (
            <p className="text-xs text-gray-500">Minimum 50 znaków</p>
          )}
          <span
            className={`text-xs ${
              whyAdopt.length < 50 ? 'text-gray-400' : 'text-green-600'
            }`}
          >
            {whyAdopt.length}/1000
          </span>
        </div>
      </div>

      {/* How to care */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Jak zamierzasz się opiekować {animalName ? `${animalName}` : 'zwierzęciem'}?
          <span className="text-red-500 ml-1">*</span>
        </label>
        <textarea
          {...register('howCare')}
          placeholder="Opisz, jak wyobrażasz sobie codzienną opiekę nad zwierzęciem. Jakie będą jego posiłki, aktywności, wizyty u weterynarza? Kto będzie się nim zajmować podczas Twojej nieobecności?"
          className="input min-h-[150px]"
        />
        <div className="flex justify-between mt-1">
          {errors.howCare ? (
            <p className="text-sm text-red-600">{errors.howCare.message}</p>
          ) : (
            <p className="text-xs text-gray-500">Minimum 50 znaków</p>
          )}
          <span
            className={`text-xs ${
              howCare.length < 50 ? 'text-gray-400' : 'text-green-600'
            }`}
          >
            {howCare.length}/1000
          </span>
        </div>
      </div>

    </div>
  );
}
