import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import {
  ArrowLeftIcon,
  CalendarDaysIcon,
  DocumentArrowDownIcon,
  XCircleIcon,
  UserIcon,
  HomeIcon,
  PhoneIcon,
  EnvelopeIcon,
  ClockIcon,
  PencilSquareIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Spinner, Badge } from '@/components/common';
import { AdoptionStatusTracker } from '@/components/adoptions';
import { VisitScheduler } from '@/components/visits';
import { useAdoptionDetail } from '@/hooks';
import { adoptionsApi, CancelRequest } from '@/api';
import { useAuth } from '@/context/AuthContext';
import { getAdoptionStatusLabel, getSpeciesLabel, Species } from '@/types';
import toast from 'react-hot-toast';

export function MyAdoptionDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const { application, isLoading, error, refetch } = useAdoptionDetail(id);

  const [showCancelModal, setShowCancelModal] = useState(false);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Check if action is permitted (backend returns Polish trigger names)
  const canCancel = application?.permittedActions?.some(action =>
    ['AnulowanePrzezUzytkownika', 'RezygnacjaPoAkceptacji', 'NiestawienieSieNaWizyte', 'RezygnacjaPrzedPodpisaniem'].includes(action)
  );
  const canScheduleVisit = application?.permittedActions?.includes('RezerwacjaTerminuWizyty') ||
    application?.status === 'Accepted';
  const canDownloadContract = application?.status === 'PendingFinalization' ||
    application?.status === 'Completed';

  const isTerminated = application?.status === 'Rejected' || application?.status === 'Cancelled';

  // Handle cancel application
  const handleCancel = async () => {
    if (!id || !cancelReason.trim()) return;

    setIsSubmitting(true);
    try {
      const data: CancelRequest = {
        reason: cancelReason,
        userName: user ? `${user.firstName} ${user.lastName}` : 'Użytkownik',
      };
      await adoptionsApi.cancel(id, data);
      toast.success('Wniosek został anulowany');
      setShowCancelModal(false);
      refetch();
    } catch (err) {
      console.error('Failed to cancel:', err);
      toast.error('Nie udało się anulować wniosku');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Handle visit scheduled successfully
  const handleVisitScheduled = () => {
    setShowScheduleModal(false);
    refetch();
  };

  // Handle download contract
  const handleDownloadContract = async () => {
    if (!id) return;

    try {
      const blob = await adoptionsApi.getContract(id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `umowa-adopcyjna-${application?.applicationNumber || id}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast.success('Umowa została pobrana');
    } catch (err) {
      console.error('Failed to download contract:', err);
      toast.error('Nie udało się pobrać umowy');
    }
  };

  // Loading state
  if (isLoading) {
    return (
      <PageContainer>
        <div className="flex flex-col items-center justify-center py-24">
          <Spinner size="lg" />
          <p className="mt-4 text-gray-500">Ładowanie szczegółów...</p>
        </div>
      </PageContainer>
    );
  }

  // Error state
  if (error || !application) {
    return (
      <PageContainer>
        <div className="max-w-2xl mx-auto py-12 text-center">
          <div className="text-red-500 mb-4">
            <XCircleIcon className="w-16 h-16 mx-auto" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">
            Nie znaleziono zgłoszenia
          </h2>
          <p className="text-gray-600 mb-6">
            {error || 'To zgłoszenie nie istnieje lub nie masz do niego dostępu.'}
          </p>
          <Button as={Link} to="/profile/adoptions">
            Wróć do moich adopcji
          </Button>
        </div>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <div className="max-w-4xl mx-auto py-8">
        {/* Back link */}
        <Link
          to="/profile/adoptions"
          className="inline-flex items-center text-gray-600 hover:text-primary-600 mb-6"
        >
          <ArrowLeftIcon className="h-4 w-4 mr-2" />
          Wróć do listy
        </Link>

        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-8">
          <div>
            <div className="flex items-center gap-3 mb-2">
              <h1 className="text-2xl font-bold text-gray-900">
                Adopcja: {application.animalName}
              </h1>
              <Badge
                variant={
                  isTerminated
                    ? 'error'
                    : application.status === 'Completed'
                    ? 'success'
                    : 'info'
                }
              >
                {getAdoptionStatusLabel(application.status)}
              </Badge>
            </div>
            <p className="text-gray-500">
              Nr zgłoszenia:{' '}
              <span className="font-mono">{application.applicationNumber}</span>
            </p>
          </div>

          {/* Action buttons */}
          <div className="flex flex-wrap gap-2">
            {canScheduleVisit && (
              <Button
                variant="outline"
                onClick={() => setShowScheduleModal(true)}
              >
                <CalendarDaysIcon className="w-4 h-4 mr-2" />
                Umów wizytę
              </Button>
            )}
            {canDownloadContract && (
              <Button
                variant="outline"
                onClick={handleDownloadContract}
              >
                <DocumentArrowDownIcon className="w-4 h-4 mr-2" />
                Pobierz umowę
              </Button>
            )}
            {canCancel && (
              <Button
                variant="outline"
                onClick={() => setShowCancelModal(true)}
                className="text-red-600 border-red-300 hover:bg-red-50"
              >
                <XCircleIcon className="w-4 h-4 mr-2" />
                Anuluj
              </Button>
            )}
          </div>
        </div>

        {/* Status tracker */}
        <AdoptionStatusTracker
          currentStatus={application.status}
          statusHistory={application.statusHistory}
          scheduledVisitDate={application.scheduledVisitDate}
          className="mb-8"
        />

        {/* Details grid */}
        <div className="grid gap-6 md:grid-cols-2">
          {/* Animal info */}
          <Card className="p-6 border border-warm-200">
            <h3 className="font-semibold text-warm-900 mb-4">
              Dane zwierzęcia
            </h3>
            <dl className="space-y-3">
              <div>
                <dt className="text-sm text-gray-500">Imię</dt>
                <dd className="font-medium text-gray-900">{application.animalName}</dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Gatunek</dt>
                <dd className="font-medium text-gray-900">{getSpeciesLabel(application.animalSpecies as Species)}</dd>
              </div>
              <div className="pt-2">
                <Link
                  to={`/animals/${application.animalId}`}
                  className="text-sm text-primary-600 hover:underline"
                >
                  Zobacz profil zwierzęcia →
                </Link>
              </div>
            </dl>
          </Card>

          {/* Applicant info */}
          <Card className="p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <UserIcon className="w-5 h-5 text-gray-400" />
              Dane wnioskodawcy
            </h3>
            <dl className="space-y-3">
              <div className="flex items-start gap-2">
                <UserIcon className="w-4 h-4 text-gray-400 mt-0.5" />
                <div>
                  <dt className="sr-only">Imię i nazwisko</dt>
                  <dd className="font-medium text-gray-900">{application.adopterName}</dd>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <EnvelopeIcon className="w-4 h-4 text-gray-400 mt-0.5" />
                <div>
                  <dt className="sr-only">Email</dt>
                  <dd className="text-gray-700">{application.adopterEmail}</dd>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <PhoneIcon className="w-4 h-4 text-gray-400 mt-0.5" />
                <div>
                  <dt className="sr-only">Telefon</dt>
                  <dd className="text-gray-700">{application.adopterPhone}</dd>
                </div>
              </div>
            </dl>
          </Card>

          {/* Application details */}
          <Card className="p-6">
            <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <PencilSquareIcon className="w-5 h-5 text-gray-400" />
              Szczegóły wniosku
            </h3>
            <dl className="space-y-3">
              <div className="flex items-start gap-2">
                <ClockIcon className="w-4 h-4 text-gray-400 mt-0.5" />
                <div>
                  <dt className="text-sm text-gray-500">Data złożenia</dt>
                  <dd className="font-medium text-gray-900">
                    {format(new Date(application.applicationDate), 'd MMMM yyyy, HH:mm', { locale: pl })}
                  </dd>
                </div>
              </div>
              {application.scheduledVisitDate && (
                <div className="flex items-start gap-2">
                  <CalendarDaysIcon className="w-4 h-4 text-primary-600 mt-0.5" />
                  <div>
                    <dt className="text-sm text-gray-500">Zaplanowana wizyta</dt>
                    <dd className="font-medium text-primary-600">
                      {format(new Date(application.scheduledVisitDate), 'd MMMM yyyy, HH:mm', { locale: pl })}
                    </dd>
                  </div>
                </div>
              )}
              {application.completionDate && (
                <div className="flex items-start gap-2">
                  <ClockIcon className="w-4 h-4 text-green-600 mt-0.5" />
                  <div>
                    <dt className="text-sm text-gray-500">Data finalizacji</dt>
                    <dd className="font-medium text-green-600">
                      {format(new Date(application.completionDate), 'd MMMM yyyy', { locale: pl })}
                    </dd>
                  </div>
                </div>
              )}
            </dl>
          </Card>

          {/* Living conditions */}
          {application.livingConditions && (
            <Card className="p-6">
              <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
                <HomeIcon className="w-5 h-5 text-gray-400" />
                Warunki mieszkaniowe
              </h3>
              <p className="text-gray-700 whitespace-pre-line">
                {application.livingConditions}
              </p>
            </Card>
          )}

          {/* Motivation */}
          {application.adoptionMotivation && (
            <Card className="p-6 md:col-span-2">
              <h3 className="font-semibold text-gray-900 mb-4">
                Motywacja adopcji
              </h3>
              <p className="text-gray-700 whitespace-pre-line">
                {application.adoptionMotivation}
              </p>
            </Card>
          )}

          {/* Rejection/Cancellation reason */}
          {(application.rejectionReason || application.cancellationReason) && (
            <Card className="p-6 md:col-span-2 bg-red-50 border-red-200">
              <h3 className="font-semibold text-red-800 mb-2">
                {application.status === 'Rejected' ? 'Powód odrzucenia' : 'Powód anulowania'}
              </h3>
              <p className="text-red-700">
                {application.rejectionReason || application.cancellationReason}
              </p>
            </Card>
          )}

          {/* Status history */}
          {application.statusHistory && application.statusHistory.length > 0 && (
            <Card className="p-6 md:col-span-2">
              <h3 className="font-semibold text-gray-900 mb-4">Historia statusów</h3>
              <div className="space-y-3">
                {application.statusHistory.map((change, index) => (
                  <div
                    key={change.id || index}
                    className="flex items-start gap-3 text-sm"
                  >
                    <div className="w-2 h-2 rounded-full bg-primary-600 mt-1.5 flex-shrink-0" />
                    <div className="flex-1">
                      <p className="text-gray-900">
                        {getAdoptionStatusLabel(change.previousStatus)} →{' '}
                        <span className="font-medium">
                          {getAdoptionStatusLabel(change.newStatus)}
                        </span>
                      </p>
                      <p className="text-gray-500">
                        {format(new Date(change.changedAt), 'd MMM yyyy, HH:mm', { locale: pl })}
                        {change.changedBy && ` • ${change.changedBy}`}
                      </p>
                      {change.notes && (
                        <p className="text-gray-600 mt-1">{change.notes}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}
        </div>

        {/* Cancel Modal */}
        <Transition appear show={showCancelModal} as={Fragment}>
          <Dialog
            as="div"
            className="relative z-50"
            onClose={() => setShowCancelModal(false)}
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
                      Anuluj wniosek
                    </Dialog.Title>

                    <p className="text-gray-600 mb-4">
                      Czy na pewno chcesz anulować ten wniosek adopcyjny? Tej operacji
                      nie można cofnąć.
                    </p>

                    <div className="mb-4">
                      <label
                        htmlFor="cancelReason"
                        className="block text-sm font-medium text-gray-700 mb-1"
                      >
                        Powód anulowania
                      </label>
                      <textarea
                        id="cancelReason"
                        value={cancelReason}
                        onChange={(e) => setCancelReason(e.target.value)}
                        className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                        rows={3}
                        placeholder="Opisz powód anulowania..."
                      />
                    </div>

                    <div className="flex justify-end gap-3">
                      <Button
                        variant="outline"
                        onClick={() => setShowCancelModal(false)}
                      >
                        Nie, wróć
                      </Button>
                      <Button
                        onClick={handleCancel}
                        isLoading={isSubmitting}
                        disabled={!cancelReason.trim()}
                        className="bg-red-600 hover:bg-red-700"
                      >
                        Tak, anuluj wniosek
                      </Button>
                    </div>
                  </Dialog.Panel>
                </Transition.Child>
              </div>
            </div>
          </Dialog>
        </Transition>

        {/* Schedule Visit Modal */}
        <Transition appear show={showScheduleModal} as={Fragment}>
          <Dialog
            as="div"
            className="relative z-50"
            onClose={() => setShowScheduleModal(false)}
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
                  <Dialog.Panel className="w-full max-w-4xl transform overflow-hidden rounded-2xl bg-white p-6 shadow-xl transition-all">
                    {id && (
                      <VisitScheduler
                        applicationId={id}
                        animalName={application.animalName}
                        onSuccess={handleVisitScheduled}
                        onCancel={() => setShowScheduleModal(false)}
                      />
                    )}
                  </Dialog.Panel>
                </Transition.Child>
              </div>
            </div>
          </Dialog>
        </Transition>
      </div>
    </PageContainer>
  );
}
