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
  CheckCircleIcon,
  XMarkIcon,
  EyeIcon,
  DocumentCheckIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Spinner, Badge } from '@/components/common';
import { AdoptionStatusTracker } from '@/components/adoptions';
import { useAdoptionDetail } from '@/hooks';
import { adoptionsApi, CancelRequest } from '@/api';
import { useAuth } from '@/context/AuthContext';
import { getAdoptionStatusLabel, getSpeciesLabel, Species } from '@/types';
import toast from 'react-hot-toast';

export function AdminAdoptionDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const { application, isLoading, error, refetch } = useAdoptionDetail(id);

  // Modal states
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [showVisitResultModal, setShowVisitResultModal] = useState(false);

  // Form states
  const [reason, setReason] = useState('');
  const [visitNotes, setVisitNotes] = useState('');
  const [visitAssessment, setVisitAssessment] = useState(5);
  const [isPositiveVisit, setIsPositiveVisit] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isTerminated = application?.status === 'Rejected' || application?.status === 'Cancelled';
  const userName = user ? `${user.firstName} ${user.lastName}` : 'Pracownik';

  // ========================
  // ACTION HANDLERS
  // ========================

  // 1. Take for review (Submitted -> UnderReview)
  const handleTakeForReview = async () => {
    if (!id || !user) return;
    setIsSubmitting(true);
    try {
      await adoptionsApi.takeForReview(id, {
        reviewerUserId: user.id,
        reviewerName: userName
      });
      toast.success('Wniosek został przyjęty do rozpatrzenia');
      refetch();
    } catch (err) {
      console.error('Failed to take for review:', err);
      toast.error('Nie udało się przyjąć wniosku');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 2. Approve application (UnderReview -> Accepted)
  const handleApprove = async () => {
    if (!id) return;
    setIsSubmitting(true);
    try {
      await adoptionsApi.approve(id, { reviewerName: userName });
      toast.success('Wniosek został zatwierdzony - adoptujący otrzyma powiadomienie o możliwości wyboru terminu wizyty');
      refetch();
    } catch (err) {
      console.error('Failed to approve:', err);
      toast.error('Nie udało się zatwierdzić wniosku');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 3. Reject application (UnderReview -> Rejected)
  const handleReject = async () => {
    if (!id || !reason.trim()) return;
    setIsSubmitting(true);
    try {
      await adoptionsApi.reject(id, { reviewerName: userName, reason });
      toast.success('Wniosek został odrzucony');
      setShowRejectModal(false);
      setReason('');
      refetch();
    } catch (err) {
      console.error('Failed to reject:', err);
      toast.error('Nie udało się odrzucić wniosku');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 4. Record visit attendance (VisitScheduled -> VisitCompleted)
  const handleRecordAttendance = async () => {
    if (!id || !user) return;
    setIsSubmitting(true);
    try {
      await adoptionsApi.recordAttendance(id, {
        conductedByUserId: user.id,
        conductedByName: userName
      });
      toast.success('Stawienie na wizytę zostało zarejestrowane');
      refetch();
    } catch (err) {
      console.error('Failed to record attendance:', err);
      toast.error('Nie udało się zarejestrować stawienia');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 6. Record visit result (VisitCompleted -> PendingFinalization or Rejected)
  const handleRecordVisitResult = async () => {
    if (!id) return;
    setIsSubmitting(true);
    try {
      await adoptionsApi.recordVisitResult(id, {
        isPositive: isPositiveVisit,
        assessment: visitAssessment,
        notes: visitNotes || (isPositiveVisit ? 'Wizyta przebiegła pomyślnie' : 'Wizyta negatywna'),
        recordedByName: userName
      });
      toast.success(isPositiveVisit
        ? 'Wizyta zakończona pozytywnie - możesz wygenerować umowę'
        : 'Wizyta zakończona negatywnie - wniosek odrzucony');
      setShowVisitResultModal(false);
      setVisitNotes('');
      setVisitAssessment(5);
      setIsPositiveVisit(true);
      refetch();
    } catch (err) {
      console.error('Failed to record visit result:', err);
      toast.error('Nie udało się zapisać wyniku wizyty');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 7. Generate contract (only in PendingFinalization)
  const handleGenerateContract = async () => {
    if (!id) return;
    setIsSubmitting(true);
    try {
      await adoptionsApi.generateContract(id, { generatedByName: userName });
      toast.success('Umowa została wygenerowana');
      refetch();
    } catch (err) {
      console.error('Failed to generate contract:', err);
      toast.error('Nie udało się wygenerować umowy');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 8. Finalize adoption (PendingFinalization -> Completed)
  const handleFinalize = async () => {
    if (!id || !application?.contractNumber) return;
    setIsSubmitting(true);
    try {
      // Use existing contract path or generate a default one
      const contractPath = application.contractFilePath || `/contracts/${application.contractNumber}.pdf`;
      await adoptionsApi.finalize(id, {
        contractFilePath: contractPath,
        signedByName: userName
      });
      toast.success('Adopcja została sfinalizowana!');
      refetch();
    } catch (err) {
      console.error('Failed to finalize:', err);
      toast.error('Nie udało się sfinalizować adopcji');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 9. Cancel application
  const handleCancel = async () => {
    if (!id || !reason.trim()) return;
    setIsSubmitting(true);
    try {
      const data: CancelRequest = { reason, userName };
      await adoptionsApi.cancel(id, data);
      toast.success('Wniosek został anulowany');
      setShowCancelModal(false);
      setReason('');
      refetch();
    } catch (err) {
      console.error('Failed to cancel:', err);
      toast.error('Nie udało się anulować wniosku');
    } finally {
      setIsSubmitting(false);
    }
  };

  // 10. Download contract
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

  // ========================
  // RENDER
  // ========================

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

  if (error || !application) {
    return (
      <PageContainer>
        <div className="max-w-2xl mx-auto py-12 text-center">
          <div className="text-red-500 mb-4">
            <XCircleIcon className="w-16 h-16 mx-auto" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">
            Nie znaleziono wniosku
          </h2>
          <p className="text-gray-600 mb-6">
            {error || 'Ten wniosek nie istnieje lub nie masz do niego dostępu.'}
          </p>
          <Button as={Link} to="/admin/adoptions">
            Wróć do listy wniosków
          </Button>
        </div>
      </PageContainer>
    );
  }

  // Render action buttons based on current status
  const renderActionButtons = () => {
    const buttons = [];

    switch (application.status) {
      case 'Submitted':
        buttons.push(
          <Button
            key="take-review"
            onClick={handleTakeForReview}
            isLoading={isSubmitting}
            leftIcon={<EyeIcon className="w-4 h-4" />}
          >
            Przyjmij do rozpatrzenia
          </Button>
        );
        buttons.push(
          <Button
            key="cancel"
            variant="outline"
            onClick={() => setShowCancelModal(true)}
            className="text-red-600 border-red-300 hover:bg-red-50"
            leftIcon={<XCircleIcon className="w-4 h-4" />}
          >
            Anuluj wniosek
          </Button>
        );
        break;

      case 'UnderReview':
        buttons.push(
          <Button
            key="approve"
            onClick={handleApprove}
            isLoading={isSubmitting}
            className="bg-green-600 hover:bg-green-700"
            leftIcon={<CheckCircleIcon className="w-4 h-4" />}
          >
            Zatwierdź (pozytywna weryfikacja)
          </Button>
        );
        buttons.push(
          <Button
            key="reject"
            variant="outline"
            onClick={() => setShowRejectModal(true)}
            className="text-red-600 border-red-300 hover:bg-red-50"
            leftIcon={<XMarkIcon className="w-4 h-4" />}
          >
            Odrzuć (negatywna weryfikacja)
          </Button>
        );
        break;

      case 'Accepted':
        // Adopter selects the visit date themselves - staff only waits
        buttons.push(
          <div key="info" className="flex items-center gap-2 text-amber-700 bg-amber-50 px-4 py-2 rounded-lg">
            <CalendarDaysIcon className="w-5 h-5" />
            <span className="text-sm">Oczekiwanie na wybór terminu wizyty przez adoptującego</span>
          </div>
        );
        buttons.push(
          <Button
            key="cancel"
            variant="outline"
            onClick={() => setShowCancelModal(true)}
            className="text-red-600 border-red-300 hover:bg-red-50"
            leftIcon={<XCircleIcon className="w-4 h-4" />}
          >
            Anuluj (rezygnacja adoptującego)
          </Button>
        );
        break;

      case 'VisitScheduled':
        buttons.push(
          <Button
            key="attendance"
            onClick={handleRecordAttendance}
            isLoading={isSubmitting}
            leftIcon={<CheckCircleIcon className="w-4 h-4" />}
          >
            Zarejestruj stawienie na wizytę
          </Button>
        );
        buttons.push(
          <Button
            key="cancel"
            variant="outline"
            onClick={() => setShowCancelModal(true)}
            className="text-red-600 border-red-300 hover:bg-red-50"
            leftIcon={<XCircleIcon className="w-4 h-4" />}
          >
            Anuluj (niestawienie się)
          </Button>
        );
        break;

      case 'VisitCompleted':
        buttons.push(
          <Button
            key="visit-result"
            onClick={() => setShowVisitResultModal(true)}
            leftIcon={<DocumentCheckIcon className="w-4 h-4" />}
          >
            Zapisz wynik wizyty
          </Button>
        );
        break;

      case 'PendingFinalization':
        if (!application.contractNumber) {
          buttons.push(
            <Button
              key="generate-contract"
              onClick={handleGenerateContract}
              isLoading={isSubmitting}
              leftIcon={<DocumentArrowDownIcon className="w-4 h-4" />}
            >
              Generuj umowę
            </Button>
          );
        } else {
          buttons.push(
            <Button
              key="download-contract"
              variant="outline"
              onClick={handleDownloadContract}
              leftIcon={<DocumentArrowDownIcon className="w-4 h-4" />}
            >
              Pobierz umowę
            </Button>
          );
          buttons.push(
            <Button
              key="finalize"
              onClick={handleFinalize}
              isLoading={isSubmitting}
              className="bg-green-600 hover:bg-green-700"
              leftIcon={<CheckCircleIcon className="w-4 h-4" />}
            >
              Sfinalizuj adopcję (podpisanie umowy)
            </Button>
          );
        }
        buttons.push(
          <Button
            key="cancel"
            variant="outline"
            onClick={() => setShowCancelModal(true)}
            className="text-red-600 border-red-300 hover:bg-red-50"
            leftIcon={<XCircleIcon className="w-4 h-4" />}
          >
            Anuluj (rezygnacja przed podpisaniem)
          </Button>
        );
        break;

      case 'Completed':
        buttons.push(
          <Button
            key="download-contract"
            variant="outline"
            onClick={handleDownloadContract}
            leftIcon={<DocumentArrowDownIcon className="w-4 h-4" />}
          >
            Pobierz umowę
          </Button>
        );
        break;

      // Rejected and Cancelled - no actions available
      default:
        break;
    }

    return buttons;
  };

  return (
    <PageContainer>
      <div className="max-w-4xl mx-auto py-8">
        {/* Back link */}
        <Link
          to="/admin/adoptions"
          className="inline-flex items-center text-gray-600 hover:text-primary-600 mb-6"
        >
          <ArrowLeftIcon className="h-4 w-4 mr-2" />
          Wróć do listy wniosków
        </Link>

        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-8">
          <div>
            <div className="flex items-center gap-3 mb-2">
              <h1 className="text-2xl font-bold text-gray-900">
                Wniosek adopcyjny: {application.animalName}
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
              Nr wniosku:{' '}
              <span className="font-mono">{application.applicationNumber}</span>
            </p>
          </div>
        </div>

        {/* Staff action buttons */}
        {!isTerminated && application.status !== 'Completed' && (
          <Card className="p-4 mb-6 bg-blue-50 border-blue-200">
            <h3 className="text-sm font-medium text-blue-800 mb-3">
              Dostępne akcje ({getAdoptionStatusLabel(application.status)})
            </h3>
            <div className="flex flex-wrap gap-2">
              {renderActionButtons()}
            </div>
          </Card>
        )}

        {/* Completed message */}
        {application.status === 'Completed' && (
          <Card className="p-4 mb-6 bg-green-50 border-green-200">
            <h3 className="text-sm font-medium text-green-800 mb-2">
              Adopcja zakończona pomyślnie!
            </h3>
            <p className="text-green-700 text-sm">
              {application.completionDate &&
                `Data finalizacji: ${format(new Date(application.completionDate), 'd MMMM yyyy', { locale: pl })}`
              }
            </p>
            <div className="mt-3">
              {renderActionButtons()}
            </div>
          </Card>
        )}

        {/* Terminated message */}
        {isTerminated && (
          <Card className="p-4 mb-6 bg-red-50 border-red-200">
            <h3 className="text-sm font-medium text-red-800 mb-2">
              Wniosek {application.status === 'Rejected' ? 'odrzucony' : 'anulowany'}
            </h3>
            {(application.rejectionReason || application.cancellationReason) && (
              <p className="text-red-700 text-sm">
                Powód: {application.rejectionReason || application.cancellationReason}
              </p>
            )}
          </Card>
        )}

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

          {/* Match score card */}
          {application.matchScore && (
            <Card className="p-6 md:col-span-2">
              <h3 className="font-semibold text-gray-900 mb-5 flex items-center gap-2">
                <svg className="w-5 h-5 text-primary-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M9 5h-2a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-12a2 2 0 0 0-2-2h-2" />
                  <path d="M9 3m0 2a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v0a2 2 0 0 1-2 2h-2a2 2 0 0 1-2-2z" />
                  <path d="M9 14l2 2l4-4" />
                </svg>
                Dopasowanie adopcyjne
              </h3>
              <div className="flex flex-col md:flex-row gap-6">
                {/* Circular percentage */}
                <div className="flex flex-col items-center justify-center">
                  <div className="relative w-28 h-28">
                    <svg className="w-28 h-28 -rotate-90" viewBox="0 0 120 120">
                      <circle
                        cx="60" cy="60" r="52"
                        fill="none"
                        stroke="#e5e7eb"
                        strokeWidth="10"
                      />
                      <circle
                        cx="60" cy="60" r="52"
                        fill="none"
                        stroke={
                          application.matchScore.totalPercentage >= 70
                            ? '#16a34a'
                            : application.matchScore.totalPercentage >= 40
                            ? '#ca8a04'
                            : '#dc2626'
                        }
                        strokeWidth="10"
                        strokeLinecap="round"
                        strokeDasharray={`${(application.matchScore.totalPercentage / 100) * 2 * Math.PI * 52} ${2 * Math.PI * 52}`}
                      />
                    </svg>
                    <div className="absolute inset-0 flex items-center justify-center">
                      <span className={`text-2xl font-bold ${
                        application.matchScore.totalPercentage >= 70
                          ? 'text-green-600'
                          : application.matchScore.totalPercentage >= 40
                          ? 'text-yellow-600'
                          : 'text-red-600'
                      }`}>
                        {Math.round(application.matchScore.totalPercentage)}%
                      </span>
                    </div>
                  </div>
                  <span className="text-sm text-gray-500 mt-2">Dopasowanie</span>
                </div>

                {/* Progress bars */}
                <div className="flex-1 space-y-3">
                  {[
                    { label: 'Doświadczenie', score: application.matchScore.experienceScore, weight: application.matchScore.experienceWeight },
                    { label: 'Przestrzeń', score: application.matchScore.spaceScore, weight: application.matchScore.spaceWeight },
                    { label: 'Czas opieki', score: application.matchScore.careTimeScore, weight: application.matchScore.careTimeWeight },
                    { label: 'Dzieci', score: application.matchScore.childrenScore, weight: application.matchScore.childrenWeight },
                    { label: 'Inne zwierzęta', score: application.matchScore.otherAnimalsScore, weight: application.matchScore.otherAnimalsWeight },
                  ].map((item) => (
                    <div key={item.label}>
                      <div className="flex justify-between text-sm mb-1">
                        <span className="text-gray-700">{item.label}</span>
                        <span className="text-gray-500">
                          {Math.round(item.score * 100)}%
                          <span className="text-xs text-gray-400 ml-1">(waga: {Math.round(item.weight * 100)}%)</span>
                        </span>
                      </div>
                      <div className="w-full h-2 bg-gray-200 rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full transition-all ${
                            item.score >= 0.7
                              ? 'bg-green-500'
                              : item.score >= 0.4
                              ? 'bg-yellow-500'
                              : 'bg-red-500'
                          }`}
                          style={{ width: `${Math.round(item.score * 100)}%` }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
              <p className="text-xs text-gray-400 mt-4 italic">
                Wynik obliczony automatycznie na podstawie danych z formularza i profilu zwierzęcia. Ostateczna decyzja należy do pracownika schroniska.
              </p>
            </Card>
          )}

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
              {application.contractNumber && (
                <div className="flex items-start gap-2">
                  <DocumentCheckIcon className="w-4 h-4 text-green-600 mt-0.5" />
                  <div>
                    <dt className="text-sm text-gray-500">Nr umowy</dt>
                    <dd className="font-medium text-green-600 font-mono">
                      {application.contractNumber}
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

          {/* Pet experience */}
          {application.petExperience && (
            <Card className="p-6">
              <h3 className="font-semibold text-gray-900 mb-4">
                Doświadczenie ze zwierzętami
              </h3>
              <p className="text-gray-700 whitespace-pre-line">
                {application.petExperience}
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

          {/* Visit notes */}
          {application.visitNotes && (
            <Card className="p-6 md:col-span-2">
              <h3 className="font-semibold text-gray-900 mb-4">
                Notatki z wizyty
              </h3>
              <p className="text-gray-700 whitespace-pre-line">
                {application.visitNotes}
              </p>
              {application.visitAssessment && (
                <p className="mt-2 text-sm text-gray-500">
                  Ocena wizyty: {application.visitAssessment}/5
                </p>
              )}
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

        {/* ======================== */}
        {/* MODALS */}
        {/* ======================== */}

        {/* Reject Modal */}
        <Transition appear show={showRejectModal} as={Fragment}>
          <Dialog
            as="div"
            className="relative z-50"
            onClose={() => setShowRejectModal(false)}
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
                      Odrzuć wniosek
                    </Dialog.Title>

                    <p className="text-gray-600 mb-4">
                      Podaj powód odrzucenia wniosku adopcyjnego. Ta informacja zostanie przekazana wnioskodawcy.
                    </p>

                    <div className="mb-4">
                      <label
                        htmlFor="rejectReason"
                        className="block text-sm font-medium text-gray-700 mb-1"
                      >
                        Powód odrzucenia *
                      </label>
                      <textarea
                        id="rejectReason"
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                        rows={3}
                        placeholder="Np. Niewystarczające warunki mieszkaniowe..."
                      />
                    </div>

                    <div className="flex justify-end gap-3">
                      <Button
                        variant="outline"
                        onClick={() => setShowRejectModal(false)}
                      >
                        Anuluj
                      </Button>
                      <Button
                        onClick={handleReject}
                        isLoading={isSubmitting}
                        disabled={!reason.trim()}
                        className="bg-red-600 hover:bg-red-700"
                      >
                        Odrzuć wniosek
                      </Button>
                    </div>
                  </Dialog.Panel>
                </Transition.Child>
              </div>
            </div>
          </Dialog>
        </Transition>

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
                      Podaj powód anulowania wniosku adopcyjnego.
                    </p>

                    <div className="mb-4">
                      <label
                        htmlFor="cancelReason"
                        className="block text-sm font-medium text-gray-700 mb-1"
                      >
                        Powód anulowania *
                      </label>
                      <textarea
                        id="cancelReason"
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                        rows={3}
                        placeholder="Np. Rezygnacja wnioskodawcy..."
                      />
                    </div>

                    <div className="flex justify-end gap-3">
                      <Button
                        variant="outline"
                        onClick={() => setShowCancelModal(false)}
                      >
                        Wróć
                      </Button>
                      <Button
                        onClick={handleCancel}
                        isLoading={isSubmitting}
                        disabled={!reason.trim()}
                        className="bg-red-600 hover:bg-red-700"
                      >
                        Anuluj wniosek
                      </Button>
                    </div>
                  </Dialog.Panel>
                </Transition.Child>
              </div>
            </div>
          </Dialog>
        </Transition>

        {/* Visit Result Modal */}
        <Transition appear show={showVisitResultModal} as={Fragment}>
          <Dialog
            as="div"
            className="relative z-50"
            onClose={() => setShowVisitResultModal(false)}
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
                      Zapisz wynik wizyty
                    </Dialog.Title>

                    <div className="space-y-4">
                      {/* Positive/Negative selection */}
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          Wynik wizyty *
                        </label>
                        <div className="flex gap-4">
                          <label className="flex items-center">
                            <input
                              type="radio"
                              checked={isPositiveVisit}
                              onChange={() => setIsPositiveVisit(true)}
                              className="h-4 w-4 text-green-600 focus:ring-green-500"
                            />
                            <span className="ml-2 text-green-700">Pozytywna</span>
                          </label>
                          <label className="flex items-center">
                            <input
                              type="radio"
                              checked={!isPositiveVisit}
                              onChange={() => setIsPositiveVisit(false)}
                              className="h-4 w-4 text-red-600 focus:ring-red-500"
                            />
                            <span className="ml-2 text-red-700">Negatywna</span>
                          </label>
                        </div>
                      </div>

                      {/* Assessment */}
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          Ocena wizyty (1-5) *
                        </label>
                        <div className="flex gap-2">
                          {[1, 2, 3, 4, 5].map((value) => (
                            <button
                              key={value}
                              type="button"
                              onClick={() => setVisitAssessment(value)}
                              className={`w-10 h-10 rounded-full font-medium ${
                                visitAssessment === value
                                  ? 'bg-primary-600 text-white'
                                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                              }`}
                            >
                              {value}
                            </button>
                          ))}
                        </div>
                      </div>

                      {/* Notes */}
                      <div>
                        <label
                          htmlFor="visitNotes"
                          className="block text-sm font-medium text-gray-700 mb-1"
                        >
                          Notatki z wizyty
                        </label>
                        <textarea
                          id="visitNotes"
                          value={visitNotes}
                          onChange={(e) => setVisitNotes(e.target.value)}
                          className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                          rows={3}
                          placeholder="Opisz przebieg wizyty..."
                        />
                      </div>

                      {!isPositiveVisit && (
                        <div className="p-3 bg-red-50 rounded-lg">
                          <p className="text-sm text-red-700">
                            Negatywna ocena wizyty spowoduje odrzucenie wniosku adopcyjnego.
                          </p>
                        </div>
                      )}
                    </div>

                    <div className="flex justify-end gap-3 mt-6">
                      <Button
                        variant="outline"
                        onClick={() => setShowVisitResultModal(false)}
                      >
                        Anuluj
                      </Button>
                      <Button
                        onClick={handleRecordVisitResult}
                        isLoading={isSubmitting}
                        className={isPositiveVisit ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'}
                      >
                        {isPositiveVisit ? 'Zatwierdź pozytywnie' : 'Zatwierdź negatywnie'}
                      </Button>
                    </div>
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
