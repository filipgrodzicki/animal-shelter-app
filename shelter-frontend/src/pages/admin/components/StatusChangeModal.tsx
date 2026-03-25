import { useState, useEffect } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { XMarkIcon, ArrowPathIcon } from '@heroicons/react/24/outline';
import { Button, Select, Spinner } from '@/components/common';
import { animalsApi } from '@/api/animals';
import { AnimalDetail, getStatusLabel, SelectOption } from '@/types';
import { useAuth } from '@/context/AuthContext';

interface StatusChangeModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  animalId: string;
}

const allStatusOptions: SelectOption[] = [
  { value: 'Admitted', label: 'Przyjęte' },
  { value: 'Quarantine', label: 'Kwarantanna' },
  { value: 'Treatment', label: 'Leczenie' },
  { value: 'Available', label: 'Dostępny' },
  { value: 'Reserved', label: 'Zarezerwowany' },
  { value: 'InAdoptionProcess', label: 'W procesie adopcji' },
  { value: 'Adopted', label: 'Adoptowany' },
  { value: 'Deceased', label: 'Zmarły' },
];

export function StatusChangeModal({ isOpen, onClose, onSuccess, animalId }: StatusChangeModalProps) {
  const { user } = useAuth();
  const [animal, setAnimal] = useState<AnimalDetail | null>(null);
  const [permittedActions, setPermittedActions] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [newStatus, setNewStatus] = useState<string>('');
  const [reason, setReason] = useState('');

  useEffect(() => {
    if (isOpen && animalId) {
      fetchAnimalData();
    }
  }, [isOpen, animalId]);

  const fetchAnimalData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [animalData, actions] = await Promise.all([
        animalsApi.getAnimal(animalId),
        animalsApi.getPermittedActions(animalId),
      ]);
      setAnimal(animalData);
      setPermittedActions(actions);
      setNewStatus('');
      setReason('');
    } catch (err) {
      setError('Nie udało się pobrać danych zwierzęcia');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newStatus || !user) return;

    setIsSubmitting(true);
    setError(null);

    try {
      await animalsApi.changeStatus(animalId, {
        newStatus,
        changedBy: `${user.firstName} ${user.lastName}`,
        reason: reason || undefined,
      });
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Nie udało się zmienić statusu');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Filter available status options based on permitted actions
  const availableStatusOptions = allStatusOptions.filter((option) =>
    permittedActions.some((action) => action.toLowerCase().includes(option.value.toLowerCase()))
  );

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
              <Dialog.Panel className="w-full max-w-md transform overflow-hidden rounded-2xl bg-white p-6 shadow-xl transition-all">
                <div className="flex items-center justify-between mb-6">
                  <Dialog.Title as="h3" className="text-lg font-semibold text-gray-900">
                    Zmień status zwierzęcia
                  </Dialog.Title>
                  <button
                    onClick={onClose}
                    className="p-1 text-gray-400 hover:text-gray-600 rounded"
                  >
                    <XMarkIcon className="h-6 w-6" />
                  </button>
                </div>

                {isLoading ? (
                  <div className="py-8 flex justify-center">
                    <Spinner size="lg" />
                  </div>
                ) : error ? (
                  <div className="py-4 text-center text-red-600">{error}</div>
                ) : animal ? (
                  <form onSubmit={handleSubmit} className="space-y-4">
                    {/* Animal info */}
                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="flex items-center gap-3">
                        {animal.photos[0]?.url ? (
                          <img
                            src={animal.photos[0].url}
                            alt={animal.name}
                            className="h-12 w-12 rounded-full object-cover"
                          />
                        ) : (
                          <div className="h-12 w-12 rounded-full bg-gray-200 flex items-center justify-center">
                            <span className="text-gray-500">{animal.name.charAt(0)}</span>
                          </div>
                        )}
                        <div>
                          <p className="font-medium text-gray-900">{animal.name}</p>
                          <p className="text-sm text-gray-500">
                            Aktualny status: <span className="font-medium">{getStatusLabel(animal.status)}</span>
                          </p>
                        </div>
                      </div>
                    </div>

                    {/* New status selection */}
                    {availableStatusOptions.length > 0 ? (
                      <>
                        <Select
                          label="Nowy status"
                          options={availableStatusOptions}
                          value={newStatus}
                          onChange={(e) => setNewStatus(e.target.value)}
                          placeholder="Wybierz nowy status"
                          required
                        />

                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">
                            Powód zmiany
                          </label>
                          <textarea
                            className="block w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:ring-2 focus:ring-opacity-20 focus:border-primary-500 focus:ring-primary-500"
                            rows={3}
                            value={reason}
                            onChange={(e) => setReason(e.target.value)}
                            placeholder="Opcjonalny powód zmiany statusu..."
                          />
                        </div>

                        <div className="flex justify-end gap-3 pt-4 border-t">
                          <Button type="button" variant="ghost" onClick={onClose}>
                            Anuluj
                          </Button>
                          <Button
                            type="submit"
                            isLoading={isSubmitting}
                            disabled={!newStatus}
                            leftIcon={<ArrowPathIcon className="h-4 w-4" />}
                          >
                            Zmień status
                          </Button>
                        </div>
                      </>
                    ) : (
                      <div className="py-4 text-center text-gray-500">
                        <p>Brak dostępnych zmian statusu dla tego zwierzęcia.</p>
                        <p className="text-sm mt-1">
                          Aktualny status nie pozwala na dalsze zmiany lub wymaga specjalnych uprawnień.
                        </p>
                        <Button type="button" variant="ghost" onClick={onClose} className="mt-4">
                          Zamknij
                        </Button>
                      </div>
                    )}
                  </form>
                ) : null}
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
}
