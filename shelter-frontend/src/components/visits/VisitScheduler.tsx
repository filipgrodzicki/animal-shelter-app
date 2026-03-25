import { useState, Fragment } from 'react';
import { format, parse, parseISO } from 'date-fns';
import { pl } from 'date-fns/locale';
import { Dialog, Transition } from '@headlessui/react';
import {
  CalendarDaysIcon,
  ClockIcon,
  CheckCircleIcon,
  XMarkIcon,
  ExclamationTriangleIcon,
} from '@heroicons/react/24/outline';
import { CalendarView } from './CalendarView';
import { TimeSlotPicker } from './TimeSlotPicker';
import { Button, Card } from '@/components/common';
import { useVisitSlots } from '@/hooks';
import { visitsApi } from '@/api';
import { TimeSlot, VisitConfirmation } from '@/types';
import toast from 'react-hot-toast';
import clsx from 'clsx';

// Helper to parse date/time from backend (handles both ISO and simple formats)
function parseDateOrTime(value: string, isTimeOnly: boolean = false): Date {
  if (!value) return new Date();

  // If it's a full ISO string
  if (value.includes('T')) {
    return parseISO(value);
  }

  const today = new Date();

  if (isTimeOnly) {
    // Parse HH:mm:ss or HH:mm format
    const timePart = value.split('.')[0];
    try {
      return parse(timePart, 'HH:mm:ss', today);
    } catch {
      return parse(timePart.substring(0, 5), 'HH:mm', today);
    }
  }

  // Parse date in YYYY-MM-DD format
  try {
    return parseISO(value);
  } catch {
    return new Date(value);
  }
}

interface VisitSchedulerProps {
  applicationId: string;
  animalName: string;
  onSuccess?: (confirmation: VisitConfirmation) => void;
  onCancel?: () => void;
}

type Step = 'calendar' | 'time' | 'confirm';

export function VisitScheduler({
  applicationId,
  animalName,
  onSuccess,
  onCancel,
}: VisitSchedulerProps) {
  const [step, setStep] = useState<Step>('calendar');
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [isBooking, setIsBooking] = useState(false);
  const [notes, setNotes] = useState('');
  const [confirmation, setConfirmation] = useState<VisitConfirmation | null>(null);

  const {
    availability,
    selectedDaySlots,
    isLoadingCalendar,
    isLoadingSlots,
    error,
    selectedDate,
    selectedSlot,
    setSelectedDate,
    setSelectedSlot,
  } = useVisitSlots(applicationId);

  // Handle date selection
  const handleDateSelect = (date: Date) => {
    setSelectedDate(date);
    setStep('time');
  };

  // Handle slot selection
  const handleSlotSelect = (slot: TimeSlot) => {
    setSelectedSlot(slot);
  };

  // Handle back navigation
  const handleBack = () => {
    if (step === 'time') {
      setStep('calendar');
    } else if (step === 'confirm') {
      setStep('time');
    }
  };

  // Handle continue to confirmation
  const handleContinue = () => {
    if (selectedSlot) {
      setShowConfirmDialog(true);
    }
  };

  // Handle booking confirmation
  const handleConfirmBooking = async () => {
    if (!selectedSlot) return;

    setIsBooking(true);

    try {
      const result = await visitsApi.bookSlot({
        applicationId,
        slotId: selectedSlot.id,
        notes: notes.trim() || undefined,
      });

      setConfirmation(result);
      setShowConfirmDialog(false);
      setStep('confirm');
      toast.success('Wizyta została zarezerwowana!');

      if (onSuccess) {
        onSuccess(result);
      }
    } catch (err) {
      console.error('Failed to book visit:', err);
      toast.error('Nie udało się zarezerwować wizyty. Spróbuj ponownie.');
    } finally {
      setIsBooking(false);
    }
  };

  // Render error state
  if (error && !isLoadingCalendar) {
    return (
      <Card className="p-6 text-center">
        <ExclamationTriangleIcon className="w-12 h-12 text-amber-500 mx-auto mb-4" />
        <h3 className="text-lg font-semibold text-gray-900 mb-2">
          Nie udało się załadować kalendarza
        </h3>
        <p className="text-gray-600 mb-4">{error}</p>
        <Button onClick={() => window.location.reload()}>
          Spróbuj ponownie
        </Button>
      </Card>
    );
  }

  // Render success confirmation
  if (step === 'confirm' && confirmation) {
    return (
      <Card className="p-6">
        <div className="text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <CheckCircleIcon className="w-10 h-10 text-green-600" />
          </div>
          <h3 className="text-xl font-bold text-gray-900 mb-2">
            Wizyta zarezerwowana!
          </h3>
          <p className="text-gray-600 mb-6">
            Potwierdzenie zostało wysłane na Twój adres email.
          </p>

          <div className="bg-gray-50 rounded-lg p-4 mb-6 text-left">
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <CalendarDaysIcon className="w-5 h-5 text-primary-600" />
                <div>
                  <p className="text-sm text-gray-500">Data wizyty</p>
                  <p className="font-medium text-gray-900">
                    {format(parseDateOrTime(confirmation.scheduledDate), 'EEEE, d MMMM yyyy', { locale: pl })}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <ClockIcon className="w-5 h-5 text-primary-600" />
                <div>
                  <p className="text-sm text-gray-500">Godzina</p>
                  <p className="font-medium text-gray-900">
                    {format(parseDateOrTime(confirmation.startTime, true), 'HH:mm')} -{' '}
                    {format(parseDateOrTime(confirmation.endTime, true), 'HH:mm')}
                  </p>
                </div>
              </div>
              <div className="pt-2 border-t border-gray-200">
                <p className="text-sm text-gray-500">Numer potwierdzenia</p>
                <p className="font-mono font-medium text-primary-600">
                  {confirmation.confirmationNumber}
                </p>
              </div>
            </div>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 text-left mb-6">
            <h4 className="font-medium text-blue-800 mb-2">Pamiętaj!</h4>
            <ul className="text-sm text-blue-700 space-y-1">
              <li>• Przyjedź 5-10 minut przed wizytą</li>
              <li>• Zabierz ze sobą dokument tożsamości</li>
              <li>• Jeśli musisz odwołać wizytę, zrób to min. 24h wcześniej</li>
            </ul>
          </div>

          <Button onClick={onCancel} className="w-full">
            Zamknij
          </Button>
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-gray-900">
            Umów wizytę w schronisku
          </h2>
          <p className="text-gray-600 mt-1">
            Wybierz termin spotkania z {animalName}
          </p>
        </div>
        {onCancel && (
          <button
            onClick={onCancel}
            className="p-2 text-gray-400 hover:text-gray-600 rounded-full hover:bg-gray-100"
          >
            <XMarkIcon className="w-6 h-6" />
          </button>
        )}
      </div>

      {/* Step indicator */}
      <div className="flex items-center gap-2">
        <StepIndicator
          number={1}
          label="Wybierz dzień"
          isActive={step === 'calendar'}
          isCompleted={step === 'time' || step === 'confirm'}
        />
        <div className="flex-1 h-0.5 bg-gray-200" />
        <StepIndicator
          number={2}
          label="Wybierz godzinę"
          isActive={step === 'time'}
          isCompleted={step === 'confirm'}
        />
      </div>

      {/* Content */}
      <div className="grid md:grid-cols-2 gap-6">
        {/* Calendar */}
        <Card className="p-4">
          <div className="flex items-center gap-2 mb-4">
            <CalendarDaysIcon className="w-5 h-5 text-primary-600" />
            <h3 className="font-semibold text-gray-900">Kalendarz</h3>
          </div>
          <CalendarView
            availability={availability}
            selectedDate={selectedDate}
            onSelectDate={handleDateSelect}
            isLoading={isLoadingCalendar}
          />
        </Card>

        {/* Time slots */}
        <Card className="p-4">
          <div className="flex items-center gap-2 mb-4">
            <ClockIcon className="w-5 h-5 text-primary-600" />
            <h3 className="font-semibold text-gray-900">Dostępne godziny</h3>
          </div>
          <TimeSlotPicker
            slots={selectedDaySlots}
            selectedSlotId={selectedSlot?.id || null}
            onSelectSlot={handleSlotSelect}
            isLoading={isLoadingSlots}
            selectedDate={selectedDate}
          />
        </Card>
      </div>

      {/* Actions */}
      <div className="flex justify-between pt-4 border-t border-gray-200">
        <Button
          variant="outline"
          onClick={step === 'calendar' ? onCancel : handleBack}
        >
          {step === 'calendar' ? 'Anuluj' : 'Wstecz'}
        </Button>
        <Button
          onClick={handleContinue}
          disabled={!selectedSlot}
        >
          Zarezerwuj wizytę
        </Button>
      </div>

      {/* Confirmation Dialog */}
      <Transition appear show={showConfirmDialog} as={Fragment}>
        <Dialog
          as="div"
          className="relative z-50"
          onClose={() => setShowConfirmDialog(false)}
        >
          <Transition.Child
            as={Fragment}
            enter="ease-out duration-300"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="ease-in duration-200"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-black/25" />
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
                  <Dialog.Title className="text-lg font-semibold text-gray-900 mb-4">
                    Potwierdź rezerwację
                  </Dialog.Title>

                  <div className="bg-gray-50 rounded-lg p-4 mb-4">
                    <div className="space-y-2">
                      <div className="flex justify-between">
                        <span className="text-gray-600">Zwierzę:</span>
                        <span className="font-medium text-gray-900">{animalName}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">Data:</span>
                        <span className="font-medium text-gray-900">
                          {selectedDate &&
                            format(selectedDate, 'd MMMM yyyy (EEEE)', { locale: pl })}
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-600">Godzina:</span>
                        <span className="font-medium text-gray-900">
                          {selectedSlot &&
                            `${format(parseDateOrTime(selectedSlot.startTime, true), 'HH:mm')} - ${format(
                              parseDateOrTime(selectedSlot.endTime, true),
                              'HH:mm'
                            )}`}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="mb-4">
                    <label
                      htmlFor="notes"
                      className="block text-sm font-medium text-gray-700 mb-1"
                    >
                      Uwagi (opcjonalnie)
                    </label>
                    <textarea
                      id="notes"
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                      rows={2}
                      placeholder="Np. specjalne pytania, obawy..."
                    />
                  </div>

                  <p className="text-sm text-gray-500 mb-4">
                    Po potwierdzeniu otrzymasz email z szczegółami wizyty.
                  </p>

                  <div className="flex justify-end gap-3">
                    <Button
                      variant="outline"
                      onClick={() => setShowConfirmDialog(false)}
                      disabled={isBooking}
                    >
                      Anuluj
                    </Button>
                    <Button
                      onClick={handleConfirmBooking}
                      isLoading={isBooking}
                    >
                      Potwierdzam rezerwację
                    </Button>
                  </div>
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </Dialog>
      </Transition>
    </div>
  );
}

// Step indicator component
interface StepIndicatorProps {
  number: number;
  label: string;
  isActive: boolean;
  isCompleted: boolean;
}

function StepIndicator({ number, label, isActive, isCompleted }: StepIndicatorProps) {
  return (
    <div className="flex items-center gap-2">
      <div
        className={clsx(
          'w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium transition-colors',
          isCompleted && 'bg-primary-600 text-white',
          isActive && 'bg-primary-100 text-primary-700 ring-2 ring-primary-600',
          !isActive && !isCompleted && 'bg-gray-100 text-gray-400'
        )}
      >
        {isCompleted ? (
          <CheckCircleIcon className="w-5 h-5" />
        ) : (
          number
        )}
      </div>
      <span
        className={clsx(
          'text-sm font-medium hidden sm:block',
          isActive || isCompleted ? 'text-gray-900' : 'text-gray-400'
        )}
      >
        {label}
      </span>
    </div>
  );
}
