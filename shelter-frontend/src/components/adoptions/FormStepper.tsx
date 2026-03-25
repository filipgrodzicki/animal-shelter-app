import { CheckIcon } from '@heroicons/react/24/solid';
import { clsx } from 'clsx';

export interface Step {
  id: number;
  name: string;
  description?: string;
}

interface FormStepperProps {
  steps: Step[];
  currentStep: number;
  onStepClick?: (step: number) => void;
  allowNavigation?: boolean;
}

export function FormStepper({
  steps,
  currentStep,
  onStepClick,
  allowNavigation = false,
}: FormStepperProps) {
  return (
    <nav aria-label="Postęp formularza">
      {/* Mobile stepper */}
      <div className="md:hidden mb-6">
        <p className="text-sm text-gray-500 mb-2">
          Krok {currentStep} z {steps.length}
        </p>
        <div className="flex gap-1">
          {steps.map((step) => (
            <div
              key={step.id}
              className={clsx(
                'h-2 flex-1 rounded-full transition-colors',
                step.id < currentStep
                  ? 'bg-primary-600'
                  : step.id === currentStep
                  ? 'bg-primary-400'
                  : 'bg-gray-200'
              )}
            />
          ))}
        </div>
        <p className="text-lg font-medium text-gray-900 mt-2">
          {steps.find((s) => s.id === currentStep)?.name}
        </p>
      </div>

      {/* Desktop stepper */}
      <ol className="hidden md:flex items-center w-full">
        {steps.map((step, index) => {
          const isCompleted = step.id < currentStep;
          const isCurrent = step.id === currentStep;
          const isClickable = allowNavigation && (isCompleted || step.id === currentStep);

          return (
            <li
              key={step.id}
              className={clsx(
                'flex items-center',
                index < steps.length - 1 && 'flex-1'
              )}
            >
              {/* Step circle and content */}
              <button
                type="button"
                onClick={() => isClickable && onStepClick?.(step.id)}
                disabled={!isClickable}
                className={clsx(
                  'flex items-center gap-3 group',
                  isClickable && 'cursor-pointer'
                )}
              >
                {/* Circle */}
                <span
                  className={clsx(
                    'flex items-center justify-center w-10 h-10 rounded-full border-2 transition-all flex-shrink-0',
                    isCompleted
                      ? 'bg-primary-600 border-primary-600 text-white'
                      : isCurrent
                      ? 'border-primary-600 text-primary-600 bg-white'
                      : 'border-gray-300 text-gray-500 bg-white',
                    isClickable && !isCompleted && !isCurrent && 'group-hover:border-gray-400'
                  )}
                >
                  {isCompleted ? (
                    <CheckIcon className="w-5 h-5" />
                  ) : (
                    <span className="text-sm font-medium">{step.id}</span>
                  )}
                </span>

                {/* Text */}
                <span className="hidden lg:block">
                  <span
                    className={clsx(
                      'block text-sm font-medium',
                      isCurrent ? 'text-primary-600' : 'text-gray-900'
                    )}
                  >
                    {step.name}
                  </span>
                  {step.description && (
                    <span className="block text-xs text-gray-500">
                      {step.description}
                    </span>
                  )}
                </span>
              </button>

              {/* Connector line */}
              {index < steps.length - 1 && (
                <div
                  className={clsx(
                    'flex-1 h-0.5 mx-4 transition-colors',
                    isCompleted ? 'bg-primary-600' : 'bg-gray-200'
                  )}
                />
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
