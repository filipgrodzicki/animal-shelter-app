import { Link } from 'react-router-dom';
import {
  ClipboardDocumentListIcon,
  FunnelIcon,
  XMarkIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Spinner, Pagination } from '@/components/common';
import { AdoptionCard } from '@/components/adoptions';
import { useMyAdoptions } from '@/hooks';
import {
  AdoptionApplicationStatus,
  getAdoptionStatusLabel,
} from '@/types';

const statusOptions: { value: AdoptionApplicationStatus | 'all'; label: string }[] = [
  { value: 'all', label: 'Wszystkie' },
  { value: 'Submitted', label: 'Złożone' },
  { value: 'UnderReview', label: 'W rozpatrzeniu' },
  { value: 'Accepted', label: 'Zaakceptowane' },
  { value: 'VisitScheduled', label: 'Wizyta zaplanowana' },
  { value: 'VisitCompleted', label: 'Wizyta odbyta' },
  { value: 'PendingFinalization', label: 'Do finalizacji' },
  { value: 'Completed', label: 'Zrealizowane' },
  { value: 'Rejected', label: 'Odrzucone' },
  { value: 'Cancelled', label: 'Anulowane' },
];

export function MyAdoptionsPage() {
  const {
    applications,
    total,
    isLoading,
    error,
    page,
    totalPages,
    setPage,
    statusFilter,
    setStatusFilter,
  } = useMyAdoptions(10);

  const handleStatusChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setStatusFilter(value === 'all' ? undefined : (value as AdoptionApplicationStatus));
    setPage(1);
  };

  const clearFilter = () => {
    setStatusFilter(undefined);
    setPage(1);
  };

  // Count by status for quick filters
  const activeCount = applications.filter(
    (a) =>
      a.status !== 'Completed' &&
      a.status !== 'Rejected' &&
      a.status !== 'Cancelled'
  ).length;

  return (
    <PageContainer>
      <div className="max-w-4xl mx-auto py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center gap-3 mb-2">
            <ClipboardDocumentListIcon className="w-8 h-8 text-primary-600" />
            <h1 className="text-3xl font-bold text-gray-900">Moje adopcje</h1>
          </div>
          <p className="text-gray-600">
            Tutaj znajdziesz wszystkie swoje wnioski adopcyjne i ich status.
          </p>
        </div>

        {/* Filters */}
        <Card className="p-4 mb-6">
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
            <div className="flex items-center gap-2">
              <FunnelIcon className="w-5 h-5 text-gray-400" />
              <span className="text-sm font-medium text-gray-700">Filtruj:</span>
              <select
                value={statusFilter || 'all'}
                onChange={handleStatusChange}
                className="text-sm border-gray-300 rounded-md focus:ring-primary-500 focus:border-primary-500"
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              {statusFilter && (
                <button
                  onClick={clearFilter}
                  className="p-1 text-gray-400 hover:text-gray-600"
                  title="Wyczyść filtr"
                >
                  <XMarkIcon className="w-4 h-4" />
                </button>
              )}
            </div>
            <p className="text-sm text-gray-500">
              {statusFilter
                ? `Znaleziono: ${total}`
                : `Wszystkich: ${total} (aktywnych: ${activeCount})`}
            </p>
          </div>
        </Card>

        {/* Loading state */}
        {isLoading && (
          <div className="flex flex-col items-center justify-center py-24">
            <Spinner size="lg" />
            <p className="mt-4 text-gray-500">Ładowanie zgłoszeń...</p>
          </div>
        )}

        {/* Error state */}
        {error && !isLoading && (
          <Card className="p-8 text-center">
            <div className="text-red-500 mb-4">
              <ClipboardDocumentListIcon className="w-12 h-12 mx-auto" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              Wystąpił błąd
            </h3>
            <p className="text-gray-600 mb-4">{error}</p>
            <Button onClick={() => window.location.reload()}>
              Spróbuj ponownie
            </Button>
          </Card>
        )}

        {/* Empty state */}
        {!isLoading && !error && applications.length === 0 && (
          <Card className="p-8 text-center">
            <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <ClipboardDocumentListIcon className="w-8 h-8 text-gray-400" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              {statusFilter ? 'Brak wyników' : 'Nie masz jeszcze żadnych zgłoszeń'}
            </h3>
            <p className="text-gray-600 mb-6">
              {statusFilter
                ? `Nie znaleziono zgłoszeń ze statusem "${getAdoptionStatusLabel(statusFilter)}".`
                : 'Przeglądaj zwierzęta i złóż wniosek o adopcję!'}
            </p>
            {statusFilter ? (
              <Button variant="outline" onClick={clearFilter}>
                Pokaż wszystkie
              </Button>
            ) : (
              <Button as={Link} to="/animals">
                Zobacz zwierzęta
              </Button>
            )}
          </Card>
        )}

        {/* Applications list */}
        {!isLoading && !error && applications.length > 0 && (
          <>
            <div className="space-y-4">
              {applications.map((application) => (
                <AdoptionCard key={application.id} application={application} />
              ))}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-8">
                <Pagination
                  currentPage={page}
                  totalPages={totalPages}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        )}
      </div>
    </PageContainer>
  );
}
