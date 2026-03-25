import { UseFormRegister, FieldErrors, UseFormWatch } from 'react-hook-form';
import { HomeIcon } from '@heroicons/react/24/outline';
import { Select } from '@/components/common';
import { AdoptionFormData, housingTypeLabels, experienceLevelLabels, careTimeLabels } from './adoptionFormSchema';

interface LivingConditionsStepProps {
  register: UseFormRegister<AdoptionFormData>;
  errors: FieldErrors<AdoptionFormData>;
  watch: UseFormWatch<AdoptionFormData>;
}

const housingOptions = [
  { value: 'apartment', label: housingTypeLabels.apartment },
  { value: 'house', label: housingTypeLabels.house },
  { value: 'houseWithGarden', label: housingTypeLabels.houseWithGarden },
];

const experienceOptions = [
  { value: 'none', label: experienceLevelLabels.none },
  { value: 'basic', label: experienceLevelLabels.basic },
  { value: 'intermediate', label: experienceLevelLabels.intermediate },
  { value: 'advanced', label: experienceLevelLabels.advanced },
];

const careTimeOptions = [
  { value: 'lessThan1Hour', label: careTimeLabels.lessThan1Hour },
  { value: 'oneToThreeHours', label: careTimeLabels.oneToThreeHours },
  { value: 'moreThan3Hours', label: careTimeLabels.moreThan3Hours },
];

export function LivingConditionsStep({
  register,
  errors,
  watch,
}: LivingConditionsStepProps) {
  const hasChildren = watch('hasChildren');
  const hasOtherAnimals = watch('hasOtherAnimals');
  const experienceLevel = watch('experienceLevel');

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
          <HomeIcon className="w-5 h-5 text-primary-600" />
        </div>
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Warunki mieszkaniowe</h2>
          <p className="text-sm text-gray-500">Opowiedz nam o swoim domu</p>
        </div>
      </div>

      {/* Housing type */}
      <Select
        label="Typ mieszkania"
        options={housingOptions}
        {...register('housingType')}
        error={errors.housingType?.message}
      />

      {/* Children */}
      <div className="space-y-3">
        <label className="flex items-center gap-3 cursor-pointer">
          <input
            type="checkbox"
            {...register('hasChildren')}
            className="w-5 h-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          <span className="text-gray-700">W domu mieszkają dzieci</span>
        </label>

        {hasChildren && (
          <div className="ml-8">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Wiek dzieci
            </label>
            <input
              type="text"
              {...register('childrenAges')}
              placeholder="np. 5 lat, 10 lat"
              className="input"
            />
            {errors.childrenAges && (
              <p className="mt-1 text-sm text-red-600">{errors.childrenAges.message}</p>
            )}
            <p className="mt-1 text-xs text-gray-500">
              Ta informacja pomoże nam dobrać odpowiednie zwierzę
            </p>
          </div>
        )}
      </div>

      {/* Other animals */}
      <div className="space-y-3">
        <label className="flex items-center gap-3 cursor-pointer">
          <input
            type="checkbox"
            {...register('hasOtherAnimals')}
            className="w-5 h-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          <span className="text-gray-700">Mam inne zwierzęta</span>
        </label>

        {hasOtherAnimals && (
          <div className="ml-8">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Jakie zwierzęta?
            </label>
            <textarea
              {...register('otherAnimalsDescription')}
              placeholder="np. pies rasy labrador, 3 lata, spokojny charakter"
              className="input min-h-[80px]"
            />
            {errors.otherAnimalsDescription && (
              <p className="mt-1 text-sm text-red-600">{errors.otherAnimalsDescription.message}</p>
            )}
          </div>
        )}
      </div>

      {/* Experience */}
      <div className="border-t border-gray-200 pt-6 mt-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Doświadczenie ze zwierzętami</h3>

        <Select
          label="Poziom doświadczenia"
          options={experienceOptions}
          {...register('experienceLevel')}
          error={errors.experienceLevel?.message}
        />

        <Select
          label="Dostępny czas na opiekę"
          options={careTimeOptions}
          {...register('availableCareTime')}
          error={errors.availableCareTime?.message}
        />

        {experienceLevel && experienceLevel !== 'none' && (
          <div className="mt-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Opowiedz o swoim doświadczeniu (opcjonalnie)
            </label>
            <textarea
              {...register('experienceDescription')}
              placeholder="Opisz swoje doświadczenie ze zwierzętami..."
              className="input min-h-[100px]"
            />
            {errors.experienceDescription && (
              <p className="mt-1 text-sm text-red-600">{errors.experienceDescription.message}</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
