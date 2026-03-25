import { MagnifyingGlassIcon, XMarkIcon, FunnelIcon, ArrowsUpDownIcon } from '@heroicons/react/24/outline';
import { Input, Select, Button } from '@/components/common';
import {
  Species,
  Gender,
  Size,
  AnimalStatus,
  getSpeciesLabel,
  getGenderLabel,
  getSizeLabel,
  getStatusLabel,
} from '@/types';
import { SortOption } from '@/hooks';

export interface AnimalFiltersState {
  searchTerm?: string;
  species?: Species;
  gender?: Gender;
  size?: Size;
  status?: AnimalStatus;
  ageMin?: number;
  ageMax?: number;
  isNeutered?: boolean;
}

interface AnimalFiltersProps {
  filters: AnimalFiltersState;
  onFiltersChange: (filters: AnimalFiltersState) => void;
  onClear: () => void;
  sortBy?: SortOption;
  sortDescending?: boolean;
  onSortChange?: (sortBy: SortOption, descending: boolean) => void;
  showStatusFilter?: boolean;
  showSorting?: boolean;
  compact?: boolean;
}

const speciesOptions = [
  { value: '', label: 'Wszystkie gatunki' },
  { value: 'Dog', label: getSpeciesLabel('Dog') },
  { value: 'Cat', label: getSpeciesLabel('Cat') },
];

const genderOptions = [
  { value: '', label: 'Wszystkie płcie' },
  { value: 'Male', label: getGenderLabel('Male') },
  { value: 'Female', label: getGenderLabel('Female') },
  { value: 'Unknown', label: getGenderLabel('Unknown') },
];

const sizeOptions = [
  { value: '', label: 'Wszystkie rozmiary' },
  { value: 'Small', label: getSizeLabel('Small') },
  { value: 'Medium', label: getSizeLabel('Medium') },
  { value: 'Large', label: getSizeLabel('Large') },
  { value: 'ExtraLarge', label: getSizeLabel('ExtraLarge') },
];

const statusOptions = [
  { value: '', label: 'Wszystkie statusy' },
  { value: 'Available', label: getStatusLabel('Available') },
  { value: 'Reserved', label: getStatusLabel('Reserved') },
  { value: 'InAdoptionProcess', label: getStatusLabel('InAdoptionProcess') },
  { value: 'Quarantine', label: getStatusLabel('Quarantine') },
  { value: 'Treatment', label: getStatusLabel('Treatment') },
  { value: 'Adopted', label: getStatusLabel('Adopted') },
];

const ageOptions = [
  { value: '', label: 'Dowolny wiek' },
  { value: '0-6', label: 'Szczenię/Kocię (0-6 mies.)' },
  { value: '6-24', label: 'Młody (6-24 mies.)' },
  { value: '24-84', label: 'Dorosły (2-7 lat)' },
  { value: '84+', label: 'Senior (7+ lat)' },
];

const sortOptions = [
  { value: 'AdmissionDate-desc', label: 'Najnowsze' },
  { value: 'AdmissionDate-asc', label: 'Najstarsze' },
  { value: 'Name-asc', label: 'Nazwa (A-Z)' },
  { value: 'Name-desc', label: 'Nazwa (Z-A)' },
  { value: 'Age-asc', label: 'Wiek (rosnąco)' },
  { value: 'Age-desc', label: 'Wiek (malejąco)' },
];

export function AnimalFilters({
  filters,
  onFiltersChange,
  onClear,
  sortBy = 'AdmissionDate',
  sortDescending = true,
  onSortChange,
  showStatusFilter = false,
  showSorting = true,
  compact = false,
}: AnimalFiltersProps) {
  const handleChange = (key: keyof AnimalFiltersState, value: string | boolean | number | undefined) => {
    onFiltersChange({
      ...filters,
      [key]: value === '' ? undefined : value,
    });
  };

  const handleAgeChange = (value: string) => {
    if (value === '') {
      onFiltersChange({
        ...filters,
        ageMin: undefined,
        ageMax: undefined,
      });
    } else if (value === '84+') {
      onFiltersChange({
        ...filters,
        ageMin: 84,
        ageMax: undefined,
      });
    } else {
      const [min, max] = value.split('-').map(Number);
      onFiltersChange({
        ...filters,
        ageMin: min,
        ageMax: max,
      });
    }
  };

  const handleSortChange = (value: string) => {
    if (!onSortChange) return;
    const [sort, direction] = value.split('-');
    onSortChange(sort as SortOption, direction === 'desc');
  };

  const getCurrentAgeValue = (): string => {
    if (filters.ageMin === undefined && filters.ageMax === undefined) return '';
    if (filters.ageMin === 84 && filters.ageMax === undefined) return '84+';
    if (filters.ageMin !== undefined && filters.ageMax !== undefined) {
      return `${filters.ageMin}-${filters.ageMax}`;
    }
    return '';
  };

  const getCurrentSortValue = (): string => {
    return `${sortBy}-${sortDescending ? 'desc' : 'asc'}`;
  };

  const hasActiveFilters = Object.entries(filters).some(
    ([key, value]) => value !== undefined && value !== '' && key !== 'status'
  );

  const activeFiltersCount = Object.entries(filters).filter(
    ([key, value]) => value !== undefined && value !== '' && key !== 'status'
  ).length;

  if (compact) {
    return (
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-gray-400" />
          <Input
            type="text"
            placeholder="Szukaj po imieniu..."
            value={filters.searchTerm || ''}
            onChange={(e) => handleChange('searchTerm', e.target.value)}
            className="pl-10"
          />
        </div>
        <Select
          options={speciesOptions}
          value={filters.species || ''}
          onChange={(e) => handleChange('species', e.target.value as Species)}
          className="w-40"
        />
        <Select
          options={genderOptions}
          value={filters.gender || ''}
          onChange={(e) => handleChange('gender', e.target.value as Gender)}
          className="w-40"
        />
        {showSorting && onSortChange && (
          <Select
            options={sortOptions}
            value={getCurrentSortValue()}
            onChange={(e) => handleSortChange(e.target.value)}
            className="w-40"
          />
        )}
        {hasActiveFilters && (
          <Button variant="ghost" size="sm" onClick={onClear}>
            <XMarkIcon className="h-4 w-4 mr-1" />
            Wyczyść
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-5 rounded-2xl bg-white border border-gray-100 p-6 shadow-sm">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-gray-700">
          <FunnelIcon className="h-5 w-5" />
          <span className="font-medium">Filtry</span>
          {activeFiltersCount > 0 && (
            <span className="ml-1 rounded-full bg-primary-100 px-2 py-0.5 text-xs font-medium text-primary-700">
              {activeFiltersCount}
            </span>
          )}
        </div>
        {showSorting && onSortChange && (
          <div className="flex items-center gap-2">
            <ArrowsUpDownIcon className="h-4 w-4 text-gray-400" />
            <Select
              options={sortOptions}
              value={getCurrentSortValue()}
              onChange={(e) => handleSortChange(e.target.value)}
              className="w-44"
            />
          </div>
        )}
      </div>

      {/* Search */}
      <div className="relative">
        <MagnifyingGlassIcon className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-gray-400" />
        <Input
          type="text"
          placeholder="Szukaj po imieniu, rasie..."
          value={filters.searchTerm || ''}
          onChange={(e) => handleChange('searchTerm', e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Filters grid */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Select
          label="Gatunek"
          options={speciesOptions}
          value={filters.species || ''}
          onChange={(e) => handleChange('species', e.target.value as Species)}
        />
        <Select
          label="Płeć"
          options={genderOptions}
          value={filters.gender || ''}
          onChange={(e) => handleChange('gender', e.target.value as Gender)}
        />
        <Select
          label="Rozmiar"
          options={sizeOptions}
          value={filters.size || ''}
          onChange={(e) => handleChange('size', e.target.value as Size)}
        />
        <Select
          label="Wiek"
          options={ageOptions}
          value={getCurrentAgeValue()}
          onChange={(e) => handleAgeChange(e.target.value)}
        />
      </div>

      {showStatusFilter && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Select
            label="Status"
            options={statusOptions}
            value={filters.status || ''}
            onChange={(e) => handleChange('status', e.target.value as AnimalStatus)}
          />
        </div>
      )}

      {/* Clear button */}
      {hasActiveFilters && (
        <div className="flex justify-end border-t border-gray-100 pt-4">
          <Button variant="outline" size="sm" onClick={onClear}>
            <XMarkIcon className="h-4 w-4 mr-1" />
            Wyczyść filtry
          </Button>
        </div>
      )}
    </div>
  );
}
