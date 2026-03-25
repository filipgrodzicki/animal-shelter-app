import { CheckIcon } from '@heroicons/react/24/solid';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';
import {
  AdoptionApplicationStatus,
  AdoptionStatusChange,
  getAdoptionStatusStep,
} from '@/types';
import clsx from 'clsx';

interface StatusStep {
  id: number;
  status: AdoptionApplicationStatus;
  name: string;
  description: string;
}

const statusSteps: StatusStep[] = [
  { id: 1, status: 'Submitted', name: 'Złożone', description: 'Wniosek został złożony' },
  { id: 2, status: 'UnderReview', name: 'W rozpatrzeniu', description: 'Analizujemy Twój wniosek' },
  { id: 3, status: 'Accepted', name: 'Zaakceptowane', description: 'Wniosek został zaakceptowany' },
  { id: 4, status: 'VisitScheduled', name: 'Wizyta umówiona', description: 'Wizyta w schronisku zaplanowana' },
  { id: 5, status: 'VisitCompleted', name: 'Wizyta odbyta', description: 'Wizyta zakończona pomyślnie' },
  { id: 6, status: 'PendingFinalization', name: 'Do finalizacji', description: 'Przygotowujemy dokumenty' },
  { id: 7, status: 'Completed', name: 'Zrealizowane', description: 'Adopcja zakończona' },
];

interface AdoptionStatusTrackerProps {
  currentStatus: AdoptionApplicationStatus;
  statusHistory?: AdoptionStatusChange[];
  scheduledVisitDate?: string;
  className?: string;
}

export function AdoptionStatusTracker({
  currentStatus,
  statusHistory = [],
  scheduledVisitDate,
  className,
}: AdoptionStatusTrackerProps) {
  const currentStep = getAdoptionStatusStep(currentStatus);
  const isRejected = currentStatus === 'Rejected';
  const isCancelled = currentStatus === 'Cancelled';
  const isTerminated = isRejected || isCancelled;

  // Get date for a specific status from history
  const getStatusDate = (status: AdoptionApplicationStatus): string | undefined => {
    const historyItem = statusHistory.find((h) => h.newStatus === status);
    return historyItem?.changedAt;
  };

  // Format date
  const formatDate = (dateString: string | undefined): string | null => {
    if (!dateString) return null;
    try {
      return format(new Date(dateString), 'd MMM yyyy', { locale: pl });
    } catch {
      return null;
    }
  };

  if (isTerminated) {
    return (
      <div className={clsx('bg-white rounded-lg border p-6', className)}>
        <div className="flex items-center gap-4 mb-4">
          <div
            className={clsx(
              'w-12 h-12 rounded-full flex items-center justify-center',
              isRejected ? 'bg-red-100' : 'bg-gray-100'
            )}
          >
            <span className="text-2xl">{isRejected ? '✗' : '🚫'}</span>
          </div>
          <div>
            <h3 className="font-semibold text-lg text-gray-900">
              {isRejected ? 'Wniosek odrzucony' : 'Wniosek anulowany'}
            </h3>
            <p className="text-sm text-gray-500">
              {isRejected
                ? 'Niestety, Twój wniosek nie został zaakceptowany'
                : 'Wniosek został anulowany'}
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={clsx('bg-white rounded-lg border p-6', className)}>
      <h3 className="font-semibold text-lg text-gray-900 mb-6">
        Status wniosku
      </h3>

      {/* Desktop tracker */}
      <div className="hidden lg:block">
        <div className="relative">
          {/* Progress line */}
          <div className="absolute top-5 left-0 right-0 h-0.5 bg-gray-200" />
          <div
            className="absolute top-5 left-0 h-0.5 bg-primary-600 transition-all duration-500"
            style={{
              width: `${((currentStep - 1) / (statusSteps.length - 1)) * 100}%`,
            }}
          />

          {/* Steps */}
          <div className="relative flex justify-between">
            {statusSteps.map((step) => {
              const isCompleted = step.id < currentStep;
              const isCurrent = step.id === currentStep;
              const isPending = step.id > currentStep;
              const date = formatDate(getStatusDate(step.status));
              const isVisitStep = step.status === 'VisitScheduled';
              const visitDate = isVisitStep && scheduledVisitDate
                ? formatDate(scheduledVisitDate)
                : null;

              return (
                <div key={step.id} className="flex flex-col items-center w-32">
                  {/* Circle */}
                  <div
                    className={clsx(
                      'w-10 h-10 rounded-full flex items-center justify-center border-2 transition-all duration-300',
                      isCompleted && 'bg-primary-600 border-primary-600',
                      isCurrent && 'bg-white border-primary-600 ring-4 ring-primary-100',
                      isPending && 'bg-white border-gray-300'
                    )}
                  >
                    {isCompleted ? (
                      <CheckIcon className="w-5 h-5 text-white" />
                    ) : (
                      <span
                        className={clsx(
                          'text-sm font-medium',
                          isCurrent ? 'text-primary-600' : 'text-gray-400'
                        )}
                      >
                        {step.id}
                      </span>
                    )}
                  </div>

                  {/* Label */}
                  <div className="mt-3 text-center">
                    <p
                      className={clsx(
                        'text-sm font-medium',
                        isCompleted || isCurrent ? 'text-gray-900' : 'text-gray-400'
                      )}
                    >
                      {step.name}
                    </p>
                    {(date || visitDate) && (
                      <p className="text-xs text-gray-500 mt-1">
                        {visitDate || date}
                      </p>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Mobile tracker */}
      <div className="lg:hidden">
        <div className="relative">
          {statusSteps.map((step, index) => {
            const isCompleted = step.id < currentStep;
            const isCurrent = step.id === currentStep;
            const isPending = step.id > currentStep;
            const date = formatDate(getStatusDate(step.status));
            const isVisitStep = step.status === 'VisitScheduled';
            const visitDate = isVisitStep && scheduledVisitDate
              ? formatDate(scheduledVisitDate)
              : null;
            const isLast = index === statusSteps.length - 1;

            return (
              <div key={step.id} className="flex">
                {/* Left side with circle and line */}
                <div className="flex flex-col items-center mr-4">
                  {/* Circle */}
                  <div
                    className={clsx(
                      'w-8 h-8 rounded-full flex items-center justify-center border-2 flex-shrink-0',
                      isCompleted && 'bg-primary-600 border-primary-600',
                      isCurrent && 'bg-white border-primary-600 ring-2 ring-primary-100',
                      isPending && 'bg-white border-gray-300'
                    )}
                  >
                    {isCompleted ? (
                      <CheckIcon className="w-4 h-4 text-white" />
                    ) : (
                      <span
                        className={clsx(
                          'text-xs font-medium',
                          isCurrent ? 'text-primary-600' : 'text-gray-400'
                        )}
                      >
                        {step.id}
                      </span>
                    )}
                  </div>

                  {/* Line */}
                  {!isLast && (
                    <div
                      className={clsx(
                        'w-0.5 h-12 my-1',
                        isCompleted ? 'bg-primary-600' : 'bg-gray-200'
                      )}
                    />
                  )}
                </div>

                {/* Right side with content */}
                <div className={clsx('pb-8', isLast && 'pb-0')}>
                  <p
                    className={clsx(
                      'text-sm font-medium',
                      isCompleted || isCurrent ? 'text-gray-900' : 'text-gray-400'
                    )}
                  >
                    {step.name}
                  </p>
                  {(isCurrent || isCompleted) && (
                    <p className="text-xs text-gray-500 mt-0.5">
                      {step.description}
                    </p>
                  )}
                  {(date || visitDate) && (
                    <p className="text-xs text-primary-600 mt-1">
                      {visitDate || date}
                    </p>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
