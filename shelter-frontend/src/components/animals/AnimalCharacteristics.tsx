import {
  AcademicCapIcon,
  HomeIcon,
  ClockIcon,
  UserGroupIcon,
} from '@heroicons/react/24/outline';
import { Card } from '@/components/common';
import {
  AnimalDetail,
  ExperienceLevel,
  CareTime,
  SpaceRequirement,
  getExperienceLevelLabel,
  getExperienceLevelDescription,
  getSpaceRequirementLabel,
  getSpaceRequirementDescription,
  getCareTimeLabel,
  getCareTimeDescription,
  getChildrenCompatibilityLabel,
  getAnimalCompatibilityLabel,
} from '@/types';

interface AnimalCharacteristicsProps {
  animal: AnimalDetail;
}

// Experience level indicator (1-3 bars)
function ExperienceIndicator({ level }: { level: ExperienceLevel }) {
  const levels: Record<ExperienceLevel, number> = {
    None: 1,
    Basic: 2,
    Advanced: 3,
  };
  const value = levels[level] || 1;

  return (
    <div className="flex gap-1">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className={`h-2 w-6 rounded-sm transition-colors ${
            i <= value ? 'bg-primary-500' : 'bg-gray-200'
          }`}
        />
      ))}
    </div>
  );
}

// Space requirement indicator (1-3 bars)
function SpaceIndicator({ space }: { space: SpaceRequirement }) {
  const levels: Record<SpaceRequirement, number> = {
    Apartment: 1,
    House: 2,
    HouseWithGarden: 3,
  };
  const value = levels[space] || 2;

  return (
    <div className="flex gap-1">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className={`h-2 w-6 rounded-sm transition-colors ${
            i <= value ? 'bg-blue-500' : 'bg-gray-200'
          }`}
        />
      ))}
    </div>
  );
}

// Care time indicator (1-3 bars)
function CareTimeIndicator({ careTime }: { careTime: CareTime }) {
  const levels: Record<CareTime, number> = {
    LessThan1Hour: 1,
    OneToThreeHours: 2,
    MoreThan3Hours: 3,
  };
  const value = levels[careTime] || 2;

  return (
    <div className="flex gap-1">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className={`h-2 w-6 rounded-sm transition-colors ${
            i <= value ? 'bg-amber-500' : 'bg-gray-200'
          }`}
        />
      ))}
    </div>
  );
}

// Compatibility badge (colored pill)
function CompatibilityIndicator({ value }: { value: 'Yes' | 'Partially' | 'No' }) {
  const config: Record<string, { text: string; classes: string }> = {
    Yes: { text: 'Tak', classes: 'bg-green-100 text-green-700' },
    Partially: { text: 'Częściowo', classes: 'bg-amber-100 text-amber-700' },
    No: { text: 'Nie', classes: 'bg-red-100 text-red-700' },
  };
  const { text, classes } = config[value] || { text: value, classes: 'bg-gray-100 text-gray-700' };

  return (
    <span className={`text-xs font-medium px-2.5 py-0.5 rounded-full ${classes}`}>
      {text}
    </span>
  );
}

export function AnimalCharacteristics({ animal }: AnimalCharacteristicsProps) {
  return (
    <Card className="overflow-hidden">
      <div className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-6">Charakterystyka</h3>

        <div className="space-y-6">
          {/* Experience Level */}
          <CharacteristicItem
            icon={<AcademicCapIcon className="h-6 w-6 text-primary-500" />}
            title="Wymagane doświadczenie"
            subtitle={getExperienceLevelLabel(animal.experienceLevel)}
            description={getExperienceLevelDescription(animal.experienceLevel)}
          >
            <ExperienceIndicator level={animal.experienceLevel} />
          </CharacteristicItem>

          {/* Care Time */}
          <CharacteristicItem
            icon={<ClockIcon className="h-6 w-6 text-amber-500" />}
            title="Wymagany czas opieki"
            subtitle={getCareTimeLabel(animal.careTime)}
            description={getCareTimeDescription(animal.careTime)}
          >
            <CareTimeIndicator careTime={animal.careTime} />
          </CharacteristicItem>

          {/* Space Requirement */}
          <CharacteristicItem
            icon={<HomeIcon className="h-6 w-6 text-blue-500" />}
            title="Wymagana przestrzeń"
            subtitle={getSpaceRequirementLabel(animal.spaceRequirement)}
            description={getSpaceRequirementDescription(animal.spaceRequirement)}
          >
            <SpaceIndicator space={animal.spaceRequirement} />
          </CharacteristicItem>

          {/* Children Compatibility */}
          <CharacteristicItem
            icon={<UserGroupIcon className="h-6 w-6 text-violet-500" />}
            title="Zgodność z dziećmi"
            subtitle={getChildrenCompatibilityLabel(animal.childrenCompatibility)}
          >
            <CompatibilityIndicator value={animal.childrenCompatibility} />
          </CharacteristicItem>

          {/* Animal Compatibility */}
          <CharacteristicItem
            icon={
              <svg className="h-6 w-6 text-orange-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M14.7 13.5c-1.1-2-1.441-2.5-2.7-2.5c-1.259 0-1.736.755-2.836 2.747c-.942 1.703-2.846 1.845-3.321 3.291c-.097.265-.145.677-.143.962c0 1.176.787 2 1.8 2c1.259 0 3-1 4.5-1s3.241 1 4.5 1c1.013 0 1.8-.823 1.8-2c0-.285-.049-.697-.146-.962c-.475-1.451-2.512-1.835-3.454-3.538" />
                <path d="M20.188 8.082a1.039 1.039 0 0 0-.406-.082h-.015c-.735.012-1.56.75-1.993 1.866c-.519 1.335-.28 2.7.538 3.052c.129.055.267.082.406.082c.739 0 1.575-.742 2.011-1.866c.516-1.335.273-2.7-.54-3.052" />
                <path d="M9.474 9c.055 0 .109 0 .163-.011c.944-.128 1.533-1.346 1.32-2.722c-.203-1.297-1.047-2.267-1.932-2.267c-.055 0-.109 0-.163.011c-.944.128-1.533 1.346-1.32 2.722c.204 1.293 1.048 2.267 1.932 2.267" />
                <path d="M14.526 9c.055 0 .109 0 .163-.011c.944-.128 1.533-1.346 1.32-2.722c-.203-1.297-1.047-2.267-1.932-2.267c-.055 0-.109 0-.163.011c-.944.128-1.533 1.346-1.32 2.722c.204 1.293 1.048 2.267 1.932 2.267" />
                <path d="M3.812 8.082a1.039 1.039 0 0 1 .406-.082h.015c.735.012 1.56.75 1.993 1.866c.519 1.335.28 2.7-.538 3.052a1.039 1.039 0 0 1-.406.082c-.739 0-1.575-.742-2.011-1.866c-.516-1.335-.273-2.7.54-3.052" />
              </svg>
            }
            title="Zgodność z innymi zwierzętami"
            subtitle={getAnimalCompatibilityLabel(animal.animalCompatibility)}
          >
            <CompatibilityIndicator value={animal.animalCompatibility} />
          </CharacteristicItem>
        </div>
      </div>
    </Card>
  );
}

interface CharacteristicItemProps {
  icon: React.ReactNode;
  title: string;
  subtitle: string;
  description?: string;
  children?: React.ReactNode;
}

function CharacteristicItem({
  icon,
  title,
  subtitle,
  description,
  children,
}: CharacteristicItemProps) {
  return (
    <div className="flex gap-4">
      <div className="flex-shrink-0 w-12 h-12 bg-gray-50 rounded-lg flex items-center justify-center">
        {icon}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2 mb-1">
          <p className="text-sm text-gray-500">{title}</p>
          {children}
        </div>
        <p className="font-medium text-gray-900">{subtitle}</p>
        {description && (
          <p className="text-sm text-gray-500 mt-1">{description}</p>
        )}
      </div>
    </div>
  );
}
