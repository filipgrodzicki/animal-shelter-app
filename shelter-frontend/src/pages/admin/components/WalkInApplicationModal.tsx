import { useState, useEffect } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import {
  XMarkIcon,
  UserPlusIcon,
  MagnifyingGlassIcon,
  CheckCircleIcon,
} from '@heroicons/react/24/outline';
import { Button, Spinner } from '@/components/common';
import { animalsApi, adoptionsApi, WalkInApplicationRequest } from '@/api';
import { AnimalListItem } from '@/types';
import { useAuth } from '@/context/AuthContext';
import toast from 'react-hot-toast';

interface WalkInApplicationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  preselectedAnimalId?: string;
}

interface FormData {
  // Personal data
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  address: string;
  city: string;
  postalCode: string;
  documentNumber: string;
  // Application data
  motivation: string;
  livingConditions: string;
  experience: string;
  otherPetsInfo: string;
  // Consent
  rodoConsent: boolean;
  skipEmailConfirmation: boolean;
}

const initialFormData: FormData = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  dateOfBirth: '',
  address: '',
  city: '',
  postalCode: '',
  documentNumber: '',
  motivation: '',
  livingConditions: '',
  experience: '',
  otherPetsInfo: '',
  rodoConsent: false,
  skipEmailConfirmation: false,
};

export function WalkInApplicationModal({
  isOpen,
  onClose,
  onSuccess,
  preselectedAnimalId,
}: WalkInApplicationModalProps) {
  const { user } = useAuth();
  const [step, setStep] = useState<'animal' | 'form' | 'success'>(preselectedAnimalId ? 'form' : 'animal');
  const [formData, setFormData] = useState<FormData>(initialFormData);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({});

  // Animal selection
  const [selectedAnimal, setSelectedAnimal] = useState<AnimalListItem | null>(null);
  const [animalSearch, setAnimalSearch] = useState('');
  const [availableAnimals, setAvailableAnimals] = useState<AnimalListItem[]>([]);
  const [isLoadingAnimals, setIsLoadingAnimals] = useState(false);

  // Result
  const [resultMessage, setResultMessage] = useState('');

  // Reset form when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setFormData(initialFormData);
      setErrors({});
      setSelectedAnimal(null);
      setStep(preselectedAnimalId ? 'form' : 'animal');
      if (preselectedAnimalId) {
        loadPreselectedAnimal(preselectedAnimalId);
      }
    }
  }, [isOpen, preselectedAnimalId]);

  // Load preselected animal
  const loadPreselectedAnimal = async (animalId: string) => {
    try {
      const animal = await animalsApi.getAnimal(animalId);
      setSelectedAnimal({
        id: animal.id,
        name: animal.name,
        species: animal.species,
        breed: animal.breed,
        gender: animal.gender,
        ageInMonths: animal.ageInMonths,
        status: animal.status,
        mainPhotoUrl: animal.photos?.[0]?.url,
        registrationNumber: animal.registrationNumber,
        size: animal.size,
        admissionDate: animal.admissionDate,
      });
    } catch (err) {
      console.error('Failed to load animal:', err);
      toast.error('Nie udalo sie wczytac danych zwierzecia');
    }
  };

  // Search available animals
  const searchAnimals = async () => {
    setIsLoadingAnimals(true);
    try {
      const result = await animalsApi.getAnimals({
        page: 1,
        pageSize: 20,
        status: 'Available',
        searchTerm: animalSearch || undefined,
      });
      setAvailableAnimals(result.items);
    } catch (err) {
      console.error('Failed to search animals:', err);
      toast.error('Nie udalo sie wyszukac zwierzat');
    } finally {
      setIsLoadingAnimals(false);
    }
  };

  // Load animals on step change
  useEffect(() => {
    if (step === 'animal' && isOpen) {
      searchAnimals();
    }
  }, [step, isOpen]);

  // Handle input change
  const handleChange = (field: keyof FormData, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    // Clear error when user types
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    }
  };

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Partial<Record<keyof FormData, string>> = {};

    if (!formData.firstName.trim()) newErrors.firstName = 'Imie jest wymagane';
    if (!formData.lastName.trim()) newErrors.lastName = 'Nazwisko jest wymagane';
    if (!formData.email.trim()) {
      newErrors.email = 'Email jest wymagany';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Nieprawidlowy format email';
    }
    if (!formData.phone.trim()) {
      newErrors.phone = 'Telefon jest wymagany';
    } else if (!/^[\d\s+\-()]{9,20}$/.test(formData.phone)) {
      newErrors.phone = 'Nieprawidlowy format telefonu';
    }
    if (!formData.dateOfBirth) {
      newErrors.dateOfBirth = 'Data urodzenia jest wymagana';
    } else {
      const birthDate = new Date(formData.dateOfBirth);
      const today = new Date();
      let age = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();
      if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        age--;
      }
      if (age < 18) {
        newErrors.dateOfBirth = 'Adoptujacy musi miec ukonczonych 18 lat';
      }
    }
    if (formData.postalCode && !/^\d{2}-\d{3}$/.test(formData.postalCode)) {
      newErrors.postalCode = 'Nieprawidlowy format (XX-XXX)';
    }
    if (!formData.rodoConsent) {
      newErrors.rodoConsent = 'Zgoda RODO jest wymagana';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Submit form
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm() || !selectedAnimal || !user) return;

    setIsSubmitting(true);

    try {
      const requestData: WalkInApplicationRequest = {
        animalId: selectedAnimal.id,
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        phone: formData.phone,
        dateOfBirth: formData.dateOfBirth,
        address: formData.address || undefined,
        city: formData.city || undefined,
        postalCode: formData.postalCode || undefined,
        rodoConsent: formData.rodoConsent,
        motivation: formData.motivation || undefined,
        livingConditions: formData.livingConditions || undefined,
        experience: formData.experience || undefined,
        otherPetsInfo: formData.otherPetsInfo || undefined,
        staffUserId: user.id,
        staffName: `${user.firstName} ${user.lastName}`,
        skipEmailConfirmation: formData.skipEmailConfirmation,
      };

      const result = await adoptionsApi.submitWalkInApplication(requestData);
      setResultMessage(result.message);
      setStep('success');
      toast.success('Zgloszenie zostalo zarejestrowane');
    } catch (err: any) {
      console.error('Failed to submit application:', err);
      const errorMessage = err?.response?.data?.detail || err?.message || 'Wystapil blad podczas rejestracji zgloszenia';
      toast.error(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  // Handle close with success callback
  const handleClose = () => {
    if (step === 'success') {
      onSuccess();
    }
    onClose();
  };

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={handleClose}>
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
                {/* Header */}
                <div className="flex items-center justify-between mb-6">
                  <Dialog.Title as="h3" className="text-lg font-semibold text-gray-900">
                    {step === 'success' ? 'Zgloszenie zarejestrowane' : 'Nowe zgloszenie stacjonarne'}
                  </Dialog.Title>
                  <button
                    onClick={handleClose}
                    className="p-1 text-gray-400 hover:text-gray-600 rounded"
                  >
                    <XMarkIcon className="h-6 w-6" />
                  </button>
                </div>

                {/* Step: Animal Selection */}
                {step === 'animal' && (
                  <div className="space-y-4">
                    <p className="text-gray-600">
                      Wybierz zwierze, dla ktorego chcesz zarejestrowac zgloszenie adopcyjne.
                    </p>

                    {/* Search */}
                    <div className="relative">
                      <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
                      <input
                        type="text"
                        placeholder="Szukaj po imieniu lub numerze..."
                        className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                        value={animalSearch}
                        onChange={(e) => setAnimalSearch(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && searchAnimals()}
                      />
                    </div>

                    {/* Animals list */}
                    <div className="max-h-80 overflow-y-auto border border-gray-200 rounded-lg">
                      {isLoadingAnimals ? (
                        <div className="p-8 flex justify-center">
                          <Spinner />
                        </div>
                      ) : availableAnimals.length === 0 ? (
                        <div className="p-8 text-center text-gray-500">
                          Brak dostepnych zwierzat
                        </div>
                      ) : (
                        <div className="divide-y divide-gray-200">
                          {availableAnimals.map((animal) => (
                            <button
                              key={animal.id}
                              type="button"
                              className="w-full p-4 flex items-center gap-4 hover:bg-gray-50 transition-colors text-left"
                              onClick={() => {
                                setSelectedAnimal(animal);
                                setStep('form');
                              }}
                            >
                              {animal.mainPhotoUrl ? (
                                <img
                                  src={animal.mainPhotoUrl}
                                  alt={animal.name}
                                  className="w-12 h-12 rounded-full object-cover"
                                />
                              ) : (
                                <div className="w-12 h-12 rounded-full bg-gray-200 flex items-center justify-center">
                                  <span className="text-gray-500 font-medium">
                                    {animal.name.charAt(0)}
                                  </span>
                                </div>
                              )}
                              <div className="flex-1 min-w-0">
                                <p className="font-medium text-gray-900">{animal.name}</p>
                                <p className="text-sm text-gray-500">
                                  {animal.species === 'Dog' ? 'Pies' : animal.species === 'Cat' ? 'Kot' : animal.species}
                                  {' • '}
                                  {animal.breed}
                                  {' • '}
                                  <span className="font-mono text-xs">{animal.registrationNumber}</span>
                                </p>
                              </div>
                            </button>
                          ))}
                        </div>
                      )}
                    </div>

                    <div className="flex justify-end pt-4 border-t">
                      <Button variant="ghost" onClick={handleClose}>
                        Anuluj
                      </Button>
                    </div>
                  </div>
                )}

                {/* Step: Form */}
                {step === 'form' && selectedAnimal && (
                  <form onSubmit={handleSubmit} className="space-y-6">
                    {/* Selected animal */}
                    <div className="p-4 bg-primary-50 rounded-lg flex items-center gap-4">
                      {selectedAnimal.mainPhotoUrl ? (
                        <img
                          src={selectedAnimal.mainPhotoUrl}
                          alt={selectedAnimal.name}
                          className="w-16 h-16 rounded-lg object-cover"
                        />
                      ) : (
                        <div className="w-16 h-16 rounded-lg bg-primary-100 flex items-center justify-center">
                          <span className="text-primary-600 font-bold text-xl">
                            {selectedAnimal.name.charAt(0)}
                          </span>
                        </div>
                      )}
                      <div>
                        <p className="font-semibold text-gray-900">{selectedAnimal.name}</p>
                        <p className="text-sm text-gray-600">
                          {selectedAnimal.species === 'Dog' ? 'Pies' : selectedAnimal.species === 'Cat' ? 'Kot' : selectedAnimal.species}
                          {' • '}{selectedAnimal.breed}
                        </p>
                        <p className="text-xs text-gray-500 font-mono">{selectedAnimal.registrationNumber}</p>
                      </div>
                      {!preselectedAnimalId && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => setStep('animal')}
                          className="ml-auto"
                        >
                          Zmien
                        </Button>
                      )}
                    </div>

                    {/* Personal data */}
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Dane osobowe adoptujacego</h4>
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Imie *
                          </label>
                          <input
                            type="text"
                            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                              errors.firstName ? 'border-red-500' : 'border-gray-300'
                            }`}
                            value={formData.firstName}
                            onChange={(e) => handleChange('firstName', e.target.value)}
                          />
                          {errors.firstName && (
                            <p className="mt-1 text-sm text-red-600">{errors.firstName}</p>
                          )}
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Nazwisko *
                          </label>
                          <input
                            type="text"
                            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                              errors.lastName ? 'border-red-500' : 'border-gray-300'
                            }`}
                            value={formData.lastName}
                            onChange={(e) => handleChange('lastName', e.target.value)}
                          />
                          {errors.lastName && (
                            <p className="mt-1 text-sm text-red-600">{errors.lastName}</p>
                          )}
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Email *
                          </label>
                          <input
                            type="email"
                            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                              errors.email ? 'border-red-500' : 'border-gray-300'
                            }`}
                            value={formData.email}
                            onChange={(e) => handleChange('email', e.target.value)}
                          />
                          {errors.email && (
                            <p className="mt-1 text-sm text-red-600">{errors.email}</p>
                          )}
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Telefon *
                          </label>
                          <input
                            type="tel"
                            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                              errors.phone ? 'border-red-500' : 'border-gray-300'
                            }`}
                            value={formData.phone}
                            onChange={(e) => handleChange('phone', e.target.value)}
                            placeholder="+48 123 456 789"
                          />
                          {errors.phone && (
                            <p className="mt-1 text-sm text-red-600">{errors.phone}</p>
                          )}
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Data urodzenia *
                          </label>
                          <input
                            type="date"
                            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                              errors.dateOfBirth ? 'border-red-500' : 'border-gray-300'
                            }`}
                            value={formData.dateOfBirth}
                            onChange={(e) => handleChange('dateOfBirth', e.target.value)}
                          />
                          {errors.dateOfBirth && (
                            <p className="mt-1 text-sm text-red-600">{errors.dateOfBirth}</p>
                          )}
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Nr dokumentu
                          </label>
                          <input
                            type="text"
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                            value={formData.documentNumber}
                            onChange={(e) => handleChange('documentNumber', e.target.value)}
                            placeholder="ABC 123456"
                          />
                        </div>
                      </div>
                    </div>

                    {/* Address */}
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Adres</h4>
                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                        <div className="sm:col-span-3">
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Ulica i numer
                          </label>
                          <input
                            type="text"
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                            value={formData.address}
                            onChange={(e) => handleChange('address', e.target.value)}
                          />
                        </div>
                        <div className="sm:col-span-2">
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Miasto
                          </label>
                          <input
                            type="text"
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                            value={formData.city}
                            onChange={(e) => handleChange('city', e.target.value)}
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Kod pocztowy
                          </label>
                          <input
                            type="text"
                            className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                              errors.postalCode ? 'border-red-500' : 'border-gray-300'
                            }`}
                            value={formData.postalCode}
                            onChange={(e) => handleChange('postalCode', e.target.value)}
                            placeholder="00-000"
                          />
                          {errors.postalCode && (
                            <p className="mt-1 text-sm text-red-600">{errors.postalCode}</p>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Additional info */}
                    <div>
                      <h4 className="font-medium text-gray-900 mb-3">Dodatkowe informacje (opcjonalne)</h4>
                      <div className="space-y-4">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Motywacja do adopcji
                          </label>
                          <textarea
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                            rows={2}
                            value={formData.motivation}
                            onChange={(e) => handleChange('motivation', e.target.value)}
                            placeholder="Dlaczego chce adoptowac to zwierze..."
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Warunki mieszkaniowe
                          </label>
                          <textarea
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                            rows={2}
                            value={formData.livingConditions}
                            onChange={(e) => handleChange('livingConditions', e.target.value)}
                            placeholder="Mieszkanie/dom, ogrod, dzieci..."
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Doswiadczenie ze zwierzetami
                          </label>
                          <textarea
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                            rows={2}
                            value={formData.experience}
                            onChange={(e) => handleChange('experience', e.target.value)}
                            placeholder="Czy mial/a wczesniej zwierzeta..."
                          />
                        </div>
                      </div>
                    </div>

                    {/* Consents */}
                    <div className="space-y-3 p-4 bg-gray-50 rounded-lg">
                      <label className="flex items-start gap-3 cursor-pointer">
                        <input
                          type="checkbox"
                          className={`mt-1 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500 ${
                            errors.rodoConsent ? 'border-red-500' : ''
                          }`}
                          checked={formData.rodoConsent}
                          onChange={(e) => handleChange('rodoConsent', e.target.checked)}
                        />
                        <span className="text-sm text-gray-700">
                          <span className="font-medium">Zgoda RODO *</span> - Potwierdzam uzyskanie zgody od adoptujacego na przetwarzanie danych osobowych w celu realizacji procesu adopcji.
                        </span>
                      </label>
                      {errors.rodoConsent && (
                        <p className="text-sm text-red-600 ml-7">{errors.rodoConsent}</p>
                      )}

                      <label className="flex items-start gap-3 cursor-pointer">
                        <input
                          type="checkbox"
                          className="mt-1 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                          checked={formData.skipEmailConfirmation}
                          onChange={(e) => handleChange('skipEmailConfirmation', e.target.checked)}
                        />
                        <span className="text-sm text-gray-700">
                          Nie wysylaj emaila z potwierdzeniem (adoptujacy zostal poinformowany ustnie)
                        </span>
                      </label>
                    </div>

                    {/* Actions */}
                    <div className="flex justify-end gap-3 pt-4 border-t">
                      <Button type="button" variant="ghost" onClick={handleClose}>
                        Anuluj
                      </Button>
                      <Button
                        type="submit"
                        isLoading={isSubmitting}
                        leftIcon={<UserPlusIcon className="h-4 w-4" />}
                      >
                        Zarejestruj zgloszenie
                      </Button>
                    </div>
                  </form>
                )}

                {/* Step: Success */}
                {step === 'success' && (
                  <div className="text-center py-6">
                    <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                      <CheckCircleIcon className="w-10 h-10 text-green-600" />
                    </div>
                    <h3 className="text-xl font-bold text-gray-900 mb-2">
                      Zgloszenie zarejestrowane!
                    </h3>
                    <p className="text-gray-600 mb-6">
                      {resultMessage || 'Zgloszenie adopcyjne zostalo pomyslnie zarejestrowane przez pracownika.'}
                    </p>
                    <Button onClick={handleClose}>
                      Zamknij
                    </Button>
                  </div>
                )}
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
}
