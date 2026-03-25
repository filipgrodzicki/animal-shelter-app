import { Link } from 'react-router-dom';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';
import {
  CalendarIcon,
  ChevronRightIcon,
  ClockIcon,
  HeartIcon,
} from '@heroicons/react/24/outline';
import { Card, Badge } from '@/components/common';
import {
  AdoptionApplicationListItem,
  getAdoptionStatusLabel,
  getAdoptionStatusStep,
} from '@/types';
import clsx from 'clsx';

interface AdoptionCardProps {
  application: AdoptionApplicationListItem;
}

export function AdoptionCard({ application }: AdoptionCardProps) {
  const {
    id,
    applicationNumber,
    animalName,
    status,
    applicationDate,
    scheduledVisitDate,
  } = application;

  const step = getAdoptionStatusStep(status);
  const isActive = step > 0 && step < 7;
  const isCompleted = status === 'Completed';
  const isTerminated = status === 'Rejected' || status === 'Cancelled';

  const getStatusBadgeVariant = (): 'success' | 'warning' | 'error' | 'info' | 'default' => {
    switch (status) {
      case 'Submitted':
      case 'UnderReview':
        return 'info';
      case 'Accepted':
      case 'VisitScheduled':
      case 'VisitCompleted':
        return 'warning';
      case 'PendingFinalization':
        return 'warning';
      case 'Completed':
        return 'success';
      case 'Rejected':
      case 'Cancelled':
        return 'error';
      default:
        return 'default';
    }
  };

  return (
    <Link to={`/profile/adoptions/${id}`}>
      <Card
        className={clsx(
          'p-4 transition-all duration-200 hover:shadow-warm hover:border-primary-300 border border-warm-200',
          isCompleted && 'bg-green-50/50 border-status-available/30',
          isTerminated && 'bg-warm-50 border-warm-200 opacity-75'
        )}
      >
        <div className="flex items-center gap-4">
          {/* Icon */}
          <div
            className={clsx(
              'w-14 h-14 rounded-full flex items-center justify-center flex-shrink-0',
              isCompleted ? 'bg-green-100' : isTerminated ? 'bg-warm-100' : 'bg-primary-100'
            )}
          >
            <HeartIcon className={clsx(
              'w-7 h-7',
              isCompleted ? 'text-status-available' : isTerminated ? 'text-warm-400' : 'text-primary-500'
            )} />
          </div>

          {/* Content */}
          <div className="flex-1 min-w-0">
            {/* Animal name and status */}
            <div className="flex items-center gap-2 mb-1">
              <h3 className="font-semibold text-warm-900 truncate">
                {animalName}
              </h3>
              <Badge variant={getStatusBadgeVariant()} size="sm">
                {getAdoptionStatusLabel(status)}
              </Badge>
            </div>

            {/* Application number */}
            <p className="text-sm text-warm-700 mb-2">
              Nr zgłoszenia: <span className="font-mono">{applicationNumber}</span>
            </p>

            {/* Dates */}
            <div className="flex flex-wrap gap-4 text-xs text-warm-700">
              <div className="flex items-center gap-1">
                <ClockIcon className="w-3.5 h-3.5" />
                <span>
                  Złożono: {format(new Date(applicationDate), 'd MMM yyyy', { locale: pl })}
                </span>
              </div>
              {scheduledVisitDate && (
                <div className="flex items-center gap-1 text-primary-600">
                  <CalendarIcon className="w-3.5 h-3.5" />
                  <span>
                    Wizyta: {format(new Date(scheduledVisitDate), 'd MMM yyyy, HH:mm', { locale: pl })}
                  </span>
                </div>
              )}
            </div>

            {/* Progress bar for active applications */}
            {isActive && (
              <div className="mt-3">
                <div className="flex items-center gap-2">
                  <div className="flex-1 h-1.5 bg-warm-200 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-primary-500 rounded-full transition-all duration-300"
                      style={{ width: `${(step / 7) * 100}%` }}
                    />
                  </div>
                  <span className="text-xs text-warm-700 tabular-nums">
                    {step}/7
                  </span>
                </div>
              </div>
            )}
          </div>

          {/* Arrow */}
          <ChevronRightIcon className="w-5 h-5 text-warm-400 flex-shrink-0" />
        </div>
      </Card>
    </Link>
  );
}
