import { useState, useEffect } from 'react';
import { Dialog, Transition, Tab } from '@headlessui/react';
import { Fragment } from 'react';
import {
  XMarkIcon,
  ClockIcon,
  DocumentTextIcon,
  HeartIcon,
  PlusIcon,
  PaperClipIcon,
  ArrowDownTrayIcon,
} from '@heroicons/react/24/outline';
import { Button, Badge, Spinner, Input, Select } from '@/components/common';
import { animalsApi, AddMedicalRecordRequest } from '@/api/animals';
import { useAuth } from '@/context/AuthContext';
import { getUserFullName } from '@/types/auth';
import {
  AnimalDetail,
  AnimalStatusChange,
  getStatusLabel,
  getSpeciesLabel,
  getGenderLabel,
  getSizeLabel,
  formatAge,
  getExperienceLevelLabel,
  getSpaceRequirementLabel,
  getCareTimeLabel,
  getChildrenCompatibilityLabel,
  getAnimalCompatibilityLabel,
  getMedicalRecordTypeLabel,
  SelectOption,
} from '@/types';
import { clsx } from 'clsx';

interface AnimalDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  animalId: string;
}

const medicalRecordTypeOptions: SelectOption[] = [
  { value: 'Examination', label: 'Badanie' },
  { value: 'Vaccination', label: 'Szczepienie' },
  { value: 'Treatment', label: 'Leczenie' },
  { value: 'Surgery', label: 'Operacja' },
  { value: 'Deworming', label: 'Odrobaczanie' },
  { value: 'Sterilization', label: 'Sterylizacja/Kastracja' },
  { value: 'Microchipping', label: 'Czipowanie' },
  { value: 'DentalCare', label: 'Stomatologia' },
  { value: 'Laboratory', label: 'Badania laboratoryjne' },
  { value: 'Other', label: 'Inne' },
];

export function AnimalDetailsModal({ isOpen, onClose, animalId }: AnimalDetailsModalProps) {
  const { user } = useAuth();
  const [animal, setAnimal] = useState<AnimalDetail | null>(null);
  const [statusHistory, setStatusHistory] = useState<AnimalStatusChange[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState(0);

  // Medical record form state
  const [showMedicalForm, setShowMedicalForm] = useState(false);
  const [medicalFormData, setMedicalFormData] = useState({
    type: 'Examination',
    title: '',
    description: '',
    recordDate: new Date().toISOString().split('T')[0],
    veterinarianName: '',
    notes: '',
    enteredBy: '', // WF-06
  });
  const [isSubmittingMedical, setIsSubmittingMedical] = useState(false);

  // Initialize enteredBy when user changes
  useEffect(() => {
    if (user) {
      setMedicalFormData((prev) => ({
        ...prev,
        enteredBy: getUserFullName(user) || user.email || '',
      }));
    }
  }, [user]);

  useEffect(() => {
    if (isOpen && animalId) {
      fetchData();
    }
  }, [isOpen, animalId]);

  const fetchData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [animalData, historyData] = await Promise.all([
        animalsApi.getAnimal(animalId),
        animalsApi.getStatusHistory(animalId),
      ]);
      setAnimal(animalData);
      setStatusHistory(historyData as AnimalStatusChange[]);
    } catch (err) {
      setError('Nie udało się pobrać danych zwierzęcia');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddMedicalRecord = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmittingMedical(true);

    try {
      const request: AddMedicalRecordRequest = {
        type: medicalFormData.type,
        title: medicalFormData.title,
        description: medicalFormData.description,
        recordDate: medicalFormData.recordDate,
        veterinarianName: medicalFormData.veterinarianName || undefined,
        notes: medicalFormData.notes || undefined,
        enteredBy: medicalFormData.enteredBy, // WF-06
        enteredByUserId: user?.id, // WF-06
      };
      await animalsApi.addMedicalRecord(animalId, request);
      await fetchData();
      setShowMedicalForm(false);
      setMedicalFormData({
        type: 'Examination',
        title: '',
        description: '',
        recordDate: new Date().toISOString().split('T')[0],
        veterinarianName: '',
        notes: '',
        enteredBy: user ? (getUserFullName(user) || user.email) : '', // WF-06
      });
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmittingMedical(false);
    }
  };

  const tabs = [
    { name: 'Szczegóły', icon: HeartIcon },
    { name: 'Historia statusów', icon: ClockIcon },
    { name: 'Dokumentacja medyczna', icon: DocumentTextIcon },
  ];

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
              <Dialog.Panel className="w-full max-w-3xl transform overflow-hidden rounded-2xl bg-white shadow-xl transition-all">
                {/* Header */}
                <div className="flex items-center justify-between p-6 border-b">
                  <Dialog.Title as="h3" className="text-lg font-semibold text-gray-900">
                    {animal ? `${animal.name} - Szczegóły` : 'Szczegóły zwierzęcia'}
                  </Dialog.Title>
                  <button
                    onClick={onClose}
                    className="p-1 text-gray-400 hover:text-gray-600 rounded"
                  >
                    <XMarkIcon className="h-6 w-6" />
                  </button>
                </div>

                {isLoading ? (
                  <div className="p-8 flex justify-center">
                    <Spinner size="lg" />
                  </div>
                ) : error ? (
                  <div className="p-8 text-center text-red-600">{error}</div>
                ) : animal ? (
                  <div className="max-h-[70vh] overflow-y-auto">
                    {/* Animal header */}
                    <div className="p-6 bg-gray-50 border-b">
                      <div className="flex items-center gap-4">
                        {animal.photos[0]?.url ? (
                          <img
                            src={animal.photos[0].url}
                            alt={animal.name}
                            className="h-20 w-20 rounded-lg object-cover"
                          />
                        ) : (
                          <div className="h-20 w-20 rounded-lg bg-gray-200 flex items-center justify-center">
                            <span className="text-2xl text-gray-500">{animal.name.charAt(0)}</span>
                          </div>
                        )}
                        <div>
                          <h4 className="text-xl font-semibold text-gray-900">{animal.name}</h4>
                          <p className="text-gray-600">
                            {animal.breed} • {getGenderLabel(animal.gender)} • {formatAge(animal.ageInMonths)}
                          </p>
                          <p className="text-sm text-gray-500 mt-1">
                            Numer ewidencyjny: {animal.registrationNumber}
                          </p>
                        </div>
                        <div className="ml-auto">
                          <Badge variant={
                            animal.status === 'Available' ? 'green' :
                            animal.status === 'InAdoptionProcess' || animal.status === 'Reserved' ? 'blue' :
                            animal.status === 'Quarantine' ? 'yellow' :
                            animal.status === 'Treatment' ? 'orange' : 'gray'
                          }>
                            {getStatusLabel(animal.status)}
                          </Badge>
                        </div>
                      </div>
                    </div>

                    {/* Tabs */}
                    <Tab.Group selectedIndex={activeTab} onChange={setActiveTab}>
                      <Tab.List className="flex border-b px-6">
                        {tabs.map((tab) => (
                          <Tab
                            key={tab.name}
                            className={({ selected }) =>
                              clsx(
                                'flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 -mb-px outline-none',
                                selected
                                  ? 'border-primary-600 text-primary-600'
                                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                              )
                            }
                          >
                            <tab.icon className="h-4 w-4" />
                            {tab.name}
                          </Tab>
                        ))}
                      </Tab.List>

                      <Tab.Panels className="p-6">
                        {/* Details tab */}
                        <Tab.Panel>
                          <div className="grid grid-cols-2 gap-6">
                            {/* Podstawowe informacje */}
                            <div>
                              <h5 className="font-medium text-gray-900 mb-3">Podstawowe informacje</h5>
                              <dl className="space-y-2 text-sm">
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Gatunek:</dt>
                                  <dd className="text-gray-900">{getSpeciesLabel(animal.species)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Rasa:</dt>
                                  <dd className="text-gray-900">{animal.breed || 'Brak'}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Płeć:</dt>
                                  <dd className="text-gray-900">{getGenderLabel(animal.gender)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Maść:</dt>
                                  <dd className="text-gray-900">{animal.color || 'Brak'}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Znaki szczególne:</dt>
                                  <dd className="text-gray-900">{animal.distinguishingMarks || 'Brak'}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Numer identyfikacyjny (chip):</dt>
                                  <dd className="text-gray-900">{animal.chipNumber || 'Brak'}</dd>
                                </div>
                              </dl>
                            </div>

                            {/* Charakterystyka */}
                            <div>
                              <h5 className="font-medium text-gray-900 mb-3">Charakterystyka</h5>
                              <dl className="space-y-2 text-sm">
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Rozmiar:</dt>
                                  <dd className="text-gray-900">{getSizeLabel(animal.size)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Doświadczenie:</dt>
                                  <dd className="text-gray-900">{getExperienceLevelLabel(animal.experienceLevel)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Wymagany czas opieki:</dt>
                                  <dd className="text-gray-900">{getCareTimeLabel(animal.careTime)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Przestrzeń:</dt>
                                  <dd className="text-gray-900">{getSpaceRequirementLabel(animal.spaceRequirement)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Zgodność z dziećmi:</dt>
                                  <dd className="text-gray-900">{getChildrenCompatibilityLabel(animal.childrenCompatibility)}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Zgodność ze zwierzętami:</dt>
                                  <dd className="text-gray-900">{getAnimalCompatibilityLabel(animal.animalCompatibility)}</dd>
                                </div>
                              </dl>
                            </div>

                            {/* Przyjęcie do schroniska */}
                            <div className="col-span-2">
                              <h5 className="font-medium text-gray-900 mb-3">Przyjęcie do schroniska</h5>
                              <dl className="space-y-2 text-sm">
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Data przyjęcia:</dt>
                                  <dd className="text-gray-900">
                                    {new Date(animal.admissionDate).toLocaleDateString('pl-PL')}
                                  </dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Okoliczności:</dt>
                                  <dd className="text-gray-900">{animal.admissionCircumstances || 'Brak'}</dd>
                                </div>
                                <div className="flex justify-between">
                                  <dt className="text-gray-500">Osoba przekazująca:</dt>
                                  <dd className="text-gray-900">
                                    {animal.surrenderedBy
                                      ? `${animal.surrenderedBy.firstName} ${animal.surrenderedBy.lastName}${animal.surrenderedBy.phone ? ` (tel. ${animal.surrenderedBy.phone})` : ''}`
                                      : 'Brak (zwierzę znalezione)'}
                                  </dd>
                                </div>
                              </dl>
                            </div>

                            {/* Wydanie zwierzęcia (dla adoptowanych) */}
                            {animal.status === 'Adopted' && (
                              <div className="col-span-2 bg-green-50 p-4 rounded-lg">
                                <h5 className="font-medium text-green-900 mb-3">Wydanie zwierzęcia</h5>
                                <dl className="space-y-2 text-sm">
                                  <div className="flex justify-between">
                                    <dt className="text-green-700">Data wydania:</dt>
                                    <dd className="text-green-900">
                                      {animal.adoptionInfo?.adoptionDate
                                        ? new Date(animal.adoptionInfo.adoptionDate).toLocaleDateString('pl-PL')
                                        : 'Brak danych'}
                                    </dd>
                                  </div>
                                  <div className="flex justify-between">
                                    <dt className="text-green-700">Okoliczności wydania:</dt>
                                    <dd className="text-green-900">
                                      {animal.adoptionInfo?.releaseCircumstances || 'Adopcja'}
                                    </dd>
                                  </div>
                                  <div className="flex justify-between">
                                    <dt className="text-green-700">Osoba odbierająca:</dt>
                                    <dd className="text-green-900">
                                      {animal.adoptionInfo?.adopter
                                        ? `${animal.adoptionInfo.adopter.firstName} ${animal.adoptionInfo.adopter.lastName}${animal.adoptionInfo.adopter.phone ? ` (tel. ${animal.adoptionInfo.adopter.phone})` : ''}`
                                        : 'Brak danych'}
                                    </dd>
                                  </div>
                                  {animal.adoptionInfo?.adopter?.address && (
                                    <div className="flex justify-between">
                                      <dt className="text-green-700">Adres:</dt>
                                      <dd className="text-green-900">{animal.adoptionInfo.adopter.address}</dd>
                                    </div>
                                  )}
                                </dl>
                              </div>
                            )}

                            {/* Opis */}
                            {animal.description && (
                              <div className="col-span-2">
                                <h5 className="font-medium text-gray-900 mb-3">Opis</h5>
                                <p className="text-sm text-gray-600">{animal.description}</p>
                              </div>
                            )}
                          </div>
                        </Tab.Panel>

                        {/* Status history tab */}
                        <Tab.Panel>
                          {statusHistory.length > 0 ? (
                            <div className="space-y-4">
                              {statusHistory.map((change) => (
                                <div
                                  key={change.id}
                                  className="flex items-start gap-4 p-4 bg-gray-50 rounded-lg"
                                >
                                  <div className="flex-shrink-0 w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center">
                                    <ClockIcon className="h-4 w-4 text-primary-600" />
                                  </div>
                                  <div className="flex-1">
                                    <div className="flex items-center gap-2 flex-wrap">
                                      <Badge variant="gray">{getStatusLabel(change.previousStatus)}</Badge>
                                      <span className="text-gray-400">→</span>
                                      <Badge variant="blue">{getStatusLabel(change.newStatus)}</Badge>
                                    </div>
                                    <p className="text-sm text-gray-600 mt-1">
                                      {change.reason || 'Brak podanego powodu'}
                                    </p>
                                    <p className="text-xs text-gray-500 mt-2">
                                      {change.changedBy} • {new Date(change.changedAt).toLocaleString('pl-PL')}
                                    </p>
                                  </div>
                                </div>
                              ))}
                            </div>
                          ) : (
                            <p className="text-center text-gray-500 py-8">
                              Brak historii zmian statusu
                            </p>
                          )}
                        </Tab.Panel>

                        {/* Medical records tab */}
                        <Tab.Panel>
                          <div className="space-y-4">
                            <div className="flex justify-between items-center">
                              <h5 className="font-medium text-gray-900">Dokumentacja medyczna</h5>
                              <Button
                                size="sm"
                                onClick={() => setShowMedicalForm(!showMedicalForm)}
                                leftIcon={<PlusIcon className="h-4 w-4" />}
                              >
                                Dodaj wpis
                              </Button>
                            </div>

                            {showMedicalForm && (
                              <form onSubmit={handleAddMedicalRecord} className="p-4 bg-gray-50 rounded-lg space-y-4">
                                <div className="grid grid-cols-2 gap-4">
                                  <Select
                                    label="Typ wpisu"
                                    options={medicalRecordTypeOptions}
                                    value={medicalFormData.type}
                                    onChange={(e) =>
                                      setMedicalFormData((prev) => ({ ...prev, type: e.target.value }))
                                    }
                                    required
                                  />
                                  <Input
                                    label="Data"
                                    type="date"
                                    value={medicalFormData.recordDate}
                                    onChange={(e) =>
                                      setMedicalFormData((prev) => ({ ...prev, recordDate: e.target.value }))
                                    }
                                    required
                                  />
                                  <Input
                                    label="Tytuł"
                                    value={medicalFormData.title}
                                    onChange={(e) =>
                                      setMedicalFormData((prev) => ({ ...prev, title: e.target.value }))
                                    }
                                    required
                                  />
                                  <Input
                                    label="Weterynarz"
                                    value={medicalFormData.veterinarianName}
                                    onChange={(e) =>
                                      setMedicalFormData((prev) => ({ ...prev, veterinarianName: e.target.value }))
                                    }
                                  />
                                  <Input
                                    label="Wprowadzający (WF-06)"
                                    value={medicalFormData.enteredBy}
                                    onChange={(e) =>
                                      setMedicalFormData((prev) => ({ ...prev, enteredBy: e.target.value }))
                                    }
                                    required
                                    className="col-span-2"
                                  />
                                </div>
                                <div>
                                  <label className="block text-sm font-medium text-gray-700 mb-1">
                                    Opis <span className="text-red-500">*</span>
                                  </label>
                                  <textarea
                                    className="block w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:ring-2 focus:ring-opacity-20 focus:border-primary-500 focus:ring-primary-500"
                                    rows={3}
                                    value={medicalFormData.description}
                                    onChange={(e) =>
                                      setMedicalFormData((prev) => ({ ...prev, description: e.target.value }))
                                    }
                                    required
                                  />
                                </div>
                                <div className="flex justify-end gap-2">
                                  <Button
                                    type="button"
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => setShowMedicalForm(false)}
                                  >
                                    Anuluj
                                  </Button>
                                  <Button type="submit" size="sm" isLoading={isSubmittingMedical}>
                                    Zapisz
                                  </Button>
                                </div>
                              </form>
                            )}

                            {animal.medicalRecords && animal.medicalRecords.length > 0 ? (
                              <div className="space-y-3">
                                {animal.medicalRecords.map((record) => {
                                  // Backward compatibility
                                  const recordType = record.type || record.recordType || 'Other';
                                  const veterinarian = record.veterinarianName || record.veterinarian;
                                  return (
                                    <div
                                      key={record.id}
                                      className="p-4 border border-gray-200 rounded-lg"
                                    >
                                      <div className="flex items-center justify-between mb-2">
                                        <Badge variant="blue">
                                          {getMedicalRecordTypeLabel(recordType)}
                                        </Badge>
                                        <span className="text-sm text-gray-500">
                                          {new Date(record.recordDate).toLocaleDateString('pl-PL')}
                                        </span>
                                      </div>
                                      {record.title && (
                                        <h6 className="font-medium text-gray-900 mb-1">{record.title}</h6>
                                      )}
                                      <p className="text-sm text-gray-700">{record.description}</p>
                                      {record.diagnosis && (
                                        <p className="text-xs text-gray-600 mt-1">
                                          <span className="font-medium">Diagnoza:</span> {record.diagnosis}
                                        </p>
                                      )}
                                      {record.treatment && (
                                        <p className="text-xs text-gray-600 mt-1">
                                          <span className="font-medium">Leczenie:</span> {record.treatment}
                                        </p>
                                      )}
                                      {record.medications && (
                                        <p className="text-xs text-gray-600 mt-1">
                                          <span className="font-medium">Leki:</span> {record.medications}
                                        </p>
                                      )}
                                      {veterinarian && (
                                        <p className="text-xs text-gray-500 mt-2">
                                          Weterynarz: {veterinarian}
                                        </p>
                                      )}
                                      {record.cost !== undefined && record.cost !== null && (
                                        <p className="text-xs text-gray-500 mt-1">
                                          Koszt: {record.cost.toFixed(2)} zł
                                        </p>
                                      )}
                                      {record.notes && (
                                        <p className="text-xs text-gray-500 mt-1">
                                          Uwagi: {record.notes}
                                        </p>
                                      )}
                                      {/* WF-06: Entered by */}
                                      {record.enteredBy && (
                                        <p className="text-xs text-gray-400 mt-2">
                                          Wprowadził: {record.enteredBy}
                                        </p>
                                      )}
                                      {/* WF-06: Attachments */}
                                      {record.attachments && record.attachments.length > 0 && (
                                        <div className="mt-3 pt-2 border-t border-gray-100">
                                          <p className="text-xs font-medium text-gray-600 flex items-center gap-1 mb-1">
                                            <PaperClipIcon className="h-3 w-3" />
                                            Załączniki ({record.attachments.length})
                                          </p>
                                          <div className="space-y-1">
                                            {record.attachments.map((attachment) => (
                                              <a
                                                key={attachment.id}
                                                href={attachment.url}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="flex items-center gap-1 text-xs text-primary-600 hover:text-primary-800"
                                              >
                                                <ArrowDownTrayIcon className="h-3 w-3" />
                                                {attachment.fileName}
                                              </a>
                                            ))}
                                          </div>
                                        </div>
                                      )}
                                    </div>
                                  );
                                })}
                              </div>
                            ) : (
                              <p className="text-center text-gray-500 py-8">
                                Brak dokumentacji medycznej
                              </p>
                            )}
                          </div>
                        </Tab.Panel>
                      </Tab.Panels>
                    </Tab.Group>
                  </div>
                ) : null}

                {/* Footer */}
                <div className="p-6 border-t bg-gray-50">
                  <div className="flex justify-end">
                    <Button variant="ghost" onClick={onClose}>
                      Zamknij
                    </Button>
                  </div>
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
}
