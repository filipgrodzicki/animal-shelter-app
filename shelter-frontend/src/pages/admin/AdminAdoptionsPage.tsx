import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { format } from 'date-fns';
import { pl } from 'date-fns/locale';
import {
  ClipboardDocumentListIcon,
  EyeIcon,
  CheckIcon,
  ArrowLeftIcon,
  UserPlusIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Card, Badge, Select, Pagination, Spinner, Button } from '@/components/common';
import { adoptionsApi } from '@/api';
import {
  AdoptionApplicationListItem,
  PagedResult,
  AdoptionApplicationStatus,
  getAdoptionStatusLabel,
  SelectOption,
} from '@/types';
import { WalkInApplicationModal } from './components/WalkInApplicationModal';

const statusOptions: SelectOption[] = [
  { value: '', label: 'Wszystkie statusy' },
  { value: 'Submitted', label: 'Zlożone' },
  { value: 'UnderReview', label: 'W rozpatrzeniu' },
  { value: 'Accepted', label: 'Zaakceptowane' },
  { value: 'VisitScheduled', label: 'Wizyta zaplanowana' },
  { value: 'VisitCompleted', label: 'Wizyta odbyta' },
  { value: 'PendingFinalization', label: 'Do finalizacji' },
  { value: 'Completed', label: 'Zrealizowane' },
  { value: 'Rejected', label: 'Odrzucone' },
  { value: 'Cancelled', label: 'Anulowane' },
];

function getStatusBadgeVariant(status: AdoptionApplicationStatus): 'success' | 'warning' | 'error' | 'info' | 'default' {
  switch (status) {
    case 'Submitted':
    case 'UnderReview':
      return 'info';
    case 'Accepted':
    case 'VisitScheduled':
    case 'VisitCompleted':
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
}

export function AdminAdoptionsPage() {
  const [applications, setApplications] = useState<PagedResult<AdoptionApplicationListItem> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [showWalkInModal, setShowWalkInModal] = useState(false);

  const fetchApplications = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await adoptionsApi.getApplications({
        page,
        pageSize: 15,
        status: (statusFilter as AdoptionApplicationStatus) || undefined,
      });
      setApplications(result);
    } catch (err) {
      setError('Nie udalo sie pobrac listy wnioskow');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchApplications();
  }, [page, statusFilter]);

  // Calculate statistics
  const stats = {
    submitted: applications?.items.filter(a => a.status === 'Submitted').length || 0,
    inProgress: applications?.items.filter(a =>
      ['UnderReview', 'Accepted', 'VisitScheduled', 'VisitCompleted', 'PendingFinalization'].includes(a.status)
    ).length || 0,
  };

  return (
    <PageContainer>
      <div className="mb-8">
        <Link
          to="/admin"
          className="inline-flex items-center text-gray-600 hover:text-primary-600 mb-4"
        >
          <ArrowLeftIcon className="h-4 w-4 mr-2" />
          Wróć do panelu
        </Link>
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Wnioski adopcyjne</h1>
            <p className="mt-2 text-gray-600">Zarządzaj wnioskami adopcyjnymi</p>
          </div>
          <Button
            onClick={() => setShowWalkInModal(true)}
            leftIcon={<UserPlusIcon className="h-5 w-5" />}
          >
            Nowe zgloszenie stacjonarne
          </Button>
        </div>
      </div>

      {/* Quick stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary-100 rounded-lg">
              <ClipboardDocumentListIcon className="h-6 w-6 text-primary-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{applications?.totalCount || 0}</p>
              <p className="text-sm text-gray-500">Wszystkie wnioski</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <ClipboardDocumentListIcon className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stats.submitted}</p>
              <p className="text-sm text-gray-500">Nowe do rozpatrzenia</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-yellow-100 rounded-lg">
              <ClipboardDocumentListIcon className="h-6 w-6 text-yellow-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stats.inProgress}</p>
              <p className="text-sm text-gray-500">W trakcie</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <Select
          options={statusOptions}
          value={statusFilter}
          onChange={(e) => {
            setStatusFilter(e.target.value);
            setPage(1);
          }}
          className="sm:min-w-[220px]"
        />
      </div>

      {/* Applications table */}
      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : !applications || applications.items.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            Nie znaleziono wniosków
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Nr wniosku
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Wnioskodawca
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Zwierzę
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Data złożenia
                    </th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Akcje
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {applications.items.map((app) => (
                    <tr key={app.id} className="hover:bg-gray-50">
                      <td className="px-4 py-4 whitespace-nowrap">
                        <span className="font-mono text-sm">{app.applicationNumber}</span>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap">
                        <div>
                          <p className="font-medium text-gray-900">{app.adopterName}</p>
                          <p className="text-sm text-gray-500">{app.adopterEmail}</p>
                        </div>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap">
                        <p className="font-medium text-gray-900">{app.animalName}</p>
                        <p className="text-sm text-gray-500">{app.animalSpecies === 'Dog' ? 'Pies' : app.animalSpecies === 'Cat' ? 'Kot' : app.animalSpecies}</p>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap">
                        <Badge variant={getStatusBadgeVariant(app.status)}>
                          {getAdoptionStatusLabel(app.status)}
                        </Badge>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                        {format(new Date(app.applicationDate), 'd MMM yyyy', { locale: pl })}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-right">
                        <div className="flex justify-end gap-2">
                          <Link
                            to={`/admin/adoptions/${app.id}`}
                            className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                            title="Szczegóły"
                          >
                            <EyeIcon className="h-5 w-5" />
                          </Link>
                          {app.status === 'Submitted' && (
                            <button
                              className="p-1.5 text-green-500 hover:text-green-600 hover:bg-green-50 rounded"
                              title="Rozpatrz"
                            >
                              <CheckIcon className="h-5 w-5" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {applications.totalPages > 1 && (
              <div className="px-4 py-3 border-t border-gray-200">
                <Pagination
                  currentPage={page}
                  totalPages={applications.totalPages}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        )}
      </Card>

      {/* Walk-in Application Modal */}
      <WalkInApplicationModal
        isOpen={showWalkInModal}
        onClose={() => setShowWalkInModal(false)}
        onSuccess={() => {
          setShowWalkInModal(false);
          fetchApplications();
        }}
      />
    </PageContainer>
  );
}
