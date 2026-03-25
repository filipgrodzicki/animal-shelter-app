import { useState, useEffect } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { XMarkIcon } from '@heroicons/react/24/outline';
import { Button, Input, Select } from '@/components/common';
import { animalsApi, CreateAnimalRequest, UpdateAnimalRequest } from '@/api/animals';
import { AnimalListItem, SelectOption } from '@/types';

interface AnimalFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  animal?: AnimalListItem | null;
}

const speciesOptions: SelectOption[] = [
  { value: 'Dog', label: 'Pies' },
  { value: 'Cat', label: 'Kot' },
];

const genderOptions: SelectOption[] = [
  { value: 'Male', label: 'Samiec' },
  { value: 'Female', label: 'Samica' },
  { value: 'Unknown', label: 'Nieznana' },
];

const sizeOptions: SelectOption[] = [
  { value: 'Small', label: 'Mały' },
  { value: 'Medium', label: 'Średni' },
  { value: 'Large', label: 'Duży' },
];

const experienceLevelOptions: SelectOption[] = [
  { value: 'None', label: 'Brak' },
  { value: 'Basic', label: 'Podstawowe' },
  { value: 'Advanced', label: 'Duże' },
];

const childrenCompatibilityOptions: SelectOption[] = [
  { value: 'Yes', label: 'Tak - idealny dla rodzin z dziećmi' },
  { value: 'Partially', label: 'Częściowo - toleruje starsze dzieci' },
  { value: 'No', label: 'Nie - niezalecany dla rodzin z dziećmi' },
];

const animalCompatibilityOptions: SelectOption[] = [
  { value: 'Yes', label: 'Tak - przyjazny innym zwierzętom' },
  { value: 'Partially', label: 'Częściowo - toleruje inne zwierzęta' },
  { value: 'No', label: 'Nie - nie toleruje innych zwierząt' },
];

const careTimeOptions: SelectOption[] = [
  { value: 'LessThan1Hour', label: 'Poniżej 1 godziny dziennie' },
  { value: 'OneToThreeHours', label: '1-3 godziny dziennie' },
  { value: 'MoreThan3Hours', label: 'Powyżej 3 godzin dziennie' },
];

const spaceOptions: SelectOption[] = [
  { value: 'Apartment', label: 'Mieszkanie' },
  { value: 'House', label: 'Dom' },
  { value: 'HouseWithGarden', label: 'Dom z ogrodem' },
];

interface FormData {
  species: string;
  breed: string;
  name: string;
  ageYears: string;
  ageMonths: string;
  gender: string;
  size: string;
  color: string;
  chipNumber: string;
  distinguishingMarks: string;
  admissionDate: string;
  admissionCircumstances: string;
  description: string;
  experienceLevel: string;
  childrenCompatibility: string;
  animalCompatibility: string;
  spaceRequirement: string;
  careTime: string;
  // Surrendering person data (optional)
  surrenderedByFirstName: string;
  surrenderedByLastName: string;
  surrenderedByPhone: string;
}

const initialFormData: FormData = {
  species: 'Dog',
  breed: '',
  name: '',
  ageYears: '0',
  ageMonths: '0',
  gender: 'Unknown',
  size: 'Medium',
  color: '',
  chipNumber: '',
  distinguishingMarks: '',
  admissionDate: new Date().toISOString().split('T')[0],
  admissionCircumstances: '',
  description: '',
  experienceLevel: 'None',
  childrenCompatibility: 'Partially',
  animalCompatibility: 'Partially',
  spaceRequirement: 'Apartment',
  careTime: 'OneToThreeHours',
  surrenderedByFirstName: '',
  surrenderedByLastName: '',
  surrenderedByPhone: '',
};

export function AnimalFormModal({ isOpen, onClose, onSuccess, animal }: AnimalFormModalProps) {
  const [formData, setFormData] = useState<FormData>(initialFormData);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedPhoto, setSelectedPhoto] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);

  const isEditing = !!animal;

  const handlePhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedPhoto(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setPhotoPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  useEffect(() => {
    if (animal) {
      const ageInMonths = animal.ageInMonths || 0;
      setFormData({
        species: animal.species,
        breed: animal.breed,
        name: animal.name,
        ageYears: String(Math.floor(ageInMonths / 12)),
        ageMonths: String(ageInMonths % 12),
        gender: animal.gender,
        size: animal.size,
        color: '',
        chipNumber: '',
        distinguishingMarks: '',
        admissionDate: animal.admissionDate.split('T')[0],
        admissionCircumstances: '',
        description: '',
        experienceLevel: 'None',
        childrenCompatibility: 'Partially',
        animalCompatibility: 'Partially',
        spaceRequirement: 'Apartment',
        careTime: 'OneToThreeHours',
        surrenderedByFirstName: '',
        surrenderedByLastName: '',
        surrenderedByPhone: '',
      });
    } else {
      setFormData(initialFormData);
      setSelectedPhoto(null);
      setPhotoPreview(null);
    }
  }, [animal, isOpen]);

  const handleChange = (field: keyof FormData, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      const ageInMonths = parseInt(formData.ageYears) * 12 + parseInt(formData.ageMonths);

      if (isEditing && animal) {
        const updateData: UpdateAnimalRequest = {
          name: formData.name,
          ageInMonths,
          description: formData.description || undefined,
          experienceLevel: formData.experienceLevel,
          childrenCompatibility: formData.childrenCompatibility,
          animalCompatibility: formData.animalCompatibility,
          spaceRequirement: formData.spaceRequirement,
          careTime: formData.careTime,
        };
        await animalsApi.updateAnimal(animal.id, updateData);
      } else {
        const createData: CreateAnimalRequest = {
          species: formData.species,
          breed: formData.breed,
          name: formData.name,
          ageInMonths,
          gender: formData.gender,
          size: formData.size,
          color: formData.color,
          chipNumber: formData.chipNumber || undefined,
          distinguishingMarks: formData.distinguishingMarks || undefined,
          admissionDate: formData.admissionDate,
          admissionCircumstances: formData.admissionCircumstances,
          description: formData.description || undefined,
          experienceLevel: formData.experienceLevel,
          childrenCompatibility: formData.childrenCompatibility,
          animalCompatibility: formData.animalCompatibility,
          spaceRequirement: formData.spaceRequirement,
          careTime: formData.careTime,
          // Surrendering person data (optional)
          surrenderedByFirstName: formData.surrenderedByFirstName || undefined,
          surrenderedByLastName: formData.surrenderedByLastName || undefined,
          surrenderedByPhone: formData.surrenderedByPhone || undefined,
        };
        const createdAnimal = await animalsApi.createAnimal(createData);

        // Upload photo if one was selected
        if (selectedPhoto && createdAnimal.id) {
          try {
            await animalsApi.uploadPhoto(createdAnimal.id, selectedPhoto, true);
          } catch (photoErr) {
            console.error('Failed to upload photo:', photoErr);
            // Don't interrupt - the animal was already created
          }
        }
      }

      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Wystąpił błąd podczas zapisywania');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-25" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-center justify-center p-4">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 scale-95"
              enterTo="opacity-100 scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 scale-100"
              leaveTo="opacity-0 scale-95"
            >
              <Dialog.Panel className="w-full max-w-2xl transform overflow-hidden rounded-2xl bg-white p-6 shadow-xl transition-all">
                <div className="flex items-center justify-between mb-6">
                  <Dialog.Title as="h3" className="text-lg font-semibold text-gray-900">
                    {isEditing ? 'Edytuj zwierzę' : 'Zarejestruj nowe zwierzę'}
                  </Dialog.Title>
                  <button
                    onClick={onClose}
                    className="p-1 text-gray-400 hover:text-gray-600 rounded focus:outline-none focus:ring-2 focus:ring-primary-500"
                    aria-label="Zamknij okno dialogowe"
                  >
                    <XMarkIcon className="h-6 w-6" aria-hidden="true" />
                  </button>
                </div>

                {error && (
                  <div
                    className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm"
                    role="alert"
                    aria-live="assertive"
                  >
                    {error}
                  </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-6">
                  {/* Basic info */}
                  <div>
                    <h4 className="font-medium text-gray-900 mb-3">Podstawowe informacje</h4>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <Input
                        label="Imię"
                        value={formData.name}
                        onChange={(e) => handleChange('name', e.target.value)}
                        required
                      />
                      <Select
                        label="Gatunek"
                        options={speciesOptions}
                        value={formData.species}
                        onChange={(e) => handleChange('species', e.target.value)}
                        required
                        disabled={isEditing}
                      />
                      <Input
                        label="Rasa"
                        value={formData.breed}
                        onChange={(e) => handleChange('breed', e.target.value)}
                        required
                        disabled={isEditing}
                      />
                      <Select
                        label="Płeć"
                        options={genderOptions}
                        value={formData.gender}
                        onChange={(e) => handleChange('gender', e.target.value)}
                        required
                        disabled={isEditing}
                      />
                      <div className="flex gap-2">
                        <Input
                          label="Wiek (lata)"
                          type="number"
                          min="0"
                          max="30"
                          value={formData.ageYears}
                          onChange={(e) => handleChange('ageYears', e.target.value)}
                        />
                        <Input
                          label="Wiek (miesiące)"
                          type="number"
                          min="0"
                          max="11"
                          value={formData.ageMonths}
                          onChange={(e) => handleChange('ageMonths', e.target.value)}
                        />
                      </div>
                      <Select
                        label="Rozmiar"
                        options={sizeOptions}
                        value={formData.size}
                        onChange={(e) => handleChange('size', e.target.value)}
                        required
                        disabled={isEditing}
                      />
                    </div>
                  </div>

                  {/* Identification */}
                  {!isEditing && (
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Identyfikacja</h4>
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <Input
                          label="Kolor/Umaszczenie"
                          value={formData.color}
                          onChange={(e) => handleChange('color', e.target.value)}
                          required
                        />
                        <Input
                          label="Numer chipa"
                          value={formData.chipNumber}
                          onChange={(e) => handleChange('chipNumber', e.target.value)}
                        />
                        <div className="sm:col-span-2">
                          <label htmlFor="distinguishing-marks" className="block text-sm font-medium text-gray-700 mb-1">
                            Znaki szczególne
                          </label>
                          <textarea
                            id="distinguishing-marks"
                            className="block w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:ring-2 focus:ring-opacity-20 focus:border-primary-500 focus:ring-primary-500"
                            rows={2}
                            value={formData.distinguishingMarks}
                            onChange={(e) => handleChange('distinguishingMarks', e.target.value)}
                            placeholder="Np. blizna na lewym uchu, biała łata na piersi..."
                          />
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Admission info */}
                  {!isEditing && (
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Przyjęcie do schroniska</h4>
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <Input
                          label="Data przyjęcia"
                          type="date"
                          value={formData.admissionDate}
                          onChange={(e) => handleChange('admissionDate', e.target.value)}
                          required
                        />
                        <div className="sm:col-span-2">
                          <label htmlFor="admission-circumstances" className="block text-sm font-medium text-gray-700 mb-1">
                            Okoliczności przyjęcia <span className="text-red-500" aria-hidden="true">*</span>
                          </label>
                          <textarea
                            id="admission-circumstances"
                            className="block w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:ring-2 focus:ring-opacity-20 focus:border-primary-500 focus:ring-primary-500"
                            rows={2}
                            value={formData.admissionCircumstances}
                            onChange={(e) => handleChange('admissionCircumstances', e.target.value)}
                            required
                            aria-required="true"
                            placeholder="Np. znaleziony na ulicy, oddany przez właściciela..."
                          />
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Surrendered by - optional */}
                  {!isEditing && (
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Osoba oddająca zwierzę (opcjonalnie)</h4>
                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                        <Input
                          label="Imię"
                          value={formData.surrenderedByFirstName}
                          onChange={(e) => handleChange('surrenderedByFirstName', e.target.value)}
                          placeholder="Jan"
                        />
                        <Input
                          label="Nazwisko"
                          value={formData.surrenderedByLastName}
                          onChange={(e) => handleChange('surrenderedByLastName', e.target.value)}
                          placeholder="Kowalski"
                        />
                        <Input
                          label="Telefon"
                          value={formData.surrenderedByPhone}
                          onChange={(e) => handleChange('surrenderedByPhone', e.target.value)}
                          placeholder="+48 123 456 789"
                        />
                      </div>
                    </div>
                  )}

                  {/* Photo upload */}
                  {!isEditing && (
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Zdjęcie zwierzęcia</h4>
                      <div className="flex items-start gap-4">
                        {photoPreview && (
                          <div className="w-24 h-24 rounded-lg overflow-hidden border border-gray-200">
                            <img src={photoPreview} alt="Podgląd" className="w-full h-full object-cover" />
                          </div>
                        )}
                        <div className="flex-1">
                          <label className="block">
                            <span className="sr-only">Wybierz zdjęcie</span>
                            <input
                              type="file"
                              accept="image/jpeg,image/png,image/webp"
                              onChange={handlePhotoChange}
                              className="block w-full text-sm text-gray-500
                                file:mr-4 file:py-2 file:px-4
                                file:rounded-lg file:border-0
                                file:text-sm file:font-semibold
                                file:bg-primary-50 file:text-primary-700
                                hover:file:bg-primary-100
                                cursor-pointer"
                            />
                          </label>
                          <p className="mt-1 text-xs text-gray-500">JPG, PNG lub WEBP. Maks. 10 MB.</p>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Characteristics */}
                  <div>
                    <h4 className="font-medium text-gray-900 mb-3">Charakterystyka</h4>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <Select
                        label="Wymagane doświadczenie"
                        options={experienceLevelOptions}
                        value={formData.experienceLevel}
                        onChange={(e) => handleChange('experienceLevel', e.target.value)}
                        required
                      />
                      <Select
                        label="Wymagany czas opieki"
                        options={careTimeOptions}
                        value={formData.careTime}
                        onChange={(e) => handleChange('careTime', e.target.value)}
                        required
                      />
                      <Select
                        label="Wymagana przestrzeń"
                        options={spaceOptions}
                        value={formData.spaceRequirement}
                        onChange={(e) => handleChange('spaceRequirement', e.target.value)}
                        required
                      />
                      <Select
                        label="Zgodność z dziećmi"
                        options={childrenCompatibilityOptions}
                        value={formData.childrenCompatibility}
                        onChange={(e) => handleChange('childrenCompatibility', e.target.value)}
                        required
                      />
                      <Select
                        label="Zgodność z innymi zwierzętami"
                        options={animalCompatibilityOptions}
                        value={formData.animalCompatibility}
                        onChange={(e) => handleChange('animalCompatibility', e.target.value)}
                        required
                      />
                    </div>
                  </div>

                  {/* Description */}
                  <div>
                    <label htmlFor="animal-description" className="block text-sm font-medium text-gray-700 mb-1">
                      Opis
                    </label>
                    <textarea
                      id="animal-description"
                      className="block w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:ring-2 focus:ring-opacity-20 focus:border-primary-500 focus:ring-primary-500"
                      rows={3}
                      value={formData.description}
                      onChange={(e) => handleChange('description', e.target.value)}
                      placeholder="Dodatkowe informacje o zwierzęciu..."
                    />
                  </div>

                  {/* Actions */}
                  <div className="flex justify-end gap-3 pt-4 border-t">
                    <Button type="button" variant="ghost" onClick={onClose}>
                      Anuluj
                    </Button>
                    <Button type="submit" isLoading={isSubmitting}>
                      {isEditing ? 'Zapisz zmiany' : 'Zarejestruj'}
                    </Button>
                  </div>
                </form>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
}
