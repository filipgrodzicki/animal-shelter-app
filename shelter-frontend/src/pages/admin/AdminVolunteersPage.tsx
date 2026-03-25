import { useState, useEffect } from 'react';
import {
  UserGroupIcon,
  CalendarDaysIcon,
  ClockIcon,
  CheckIcon,
  XMarkIcon,
  PauseIcon,
  PlayIcon,
  AcademicCapIcon,
  PlusIcon,
  DocumentTextIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Badge, Input, Select, Spinner, Modal } from '@/components/common';
import {
  volunteersApi,
  scheduleApi,
  attendanceApi,
  GetVolunteersParams,
  ApproveVolunteerRequest,
  CompleteTrainingRequest,
  CreateSlotRequest,
  CreateSlotsBulkRequest,
} from '@/api/volunteers';
import { getErrorMessage } from '@/api/client';
import {
  VolunteerListItem,
  VolunteerDetail,
  ScheduleSlot,
  AttendanceListItem,
  VolunteerStatus,
  getVolunteerStatusLabel,
  formatHours,
} from '@/types';
import { useAuth } from '@/context/AuthContext';
import toast from 'react-hot-toast';

type Tab = 'volunteers' | 'schedule' | 'attendance';

const statusFilterOptions = [
  { value: '', label: 'Wszystkie statusy' },
  { value: 'Candidate', label: 'Kandydaci' },
  { value: 'InTraining', label: 'W szkoleniu' },
  { value: 'Active', label: 'Aktywni' },
  { value: 'Suspended', label: 'Zawieszeni' },
  { value: 'Inactive', label: 'Nieaktywni' },
];

function getStatusBadgeVariant(status: VolunteerStatus): 'blue' | 'yellow' | 'green' | 'orange' | 'gray' {
  const variants: Record<VolunteerStatus, 'blue' | 'yellow' | 'green' | 'orange' | 'gray'> = {
    Candidate: 'blue',
    InTraining: 'yellow',
    Active: 'green',
    Suspended: 'orange',
    Inactive: 'gray',
  };
  return variants[status] || 'gray';
}

export function AdminVolunteersPage() {
  const [activeTab, setActiveTab] = useState<Tab>('volunteers');

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Zarzadzanie wolontariuszami</h1>
        <p className="mt-2 text-gray-600">Zarzadzaj wolontariuszami, harmonogramem i obecnosciami</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex gap-6">
          <button
            onClick={() => setActiveTab('volunteers')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'volunteers'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <UserGroupIcon className="h-5 w-5" />
            Wolontariusze
          </button>
          <button
            onClick={() => setActiveTab('schedule')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'schedule'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <CalendarDaysIcon className="h-5 w-5" />
            Harmonogram
          </button>
          <button
            onClick={() => setActiveTab('attendance')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'attendance'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <ClockIcon className="h-5 w-5" />
            Obecnosci
          </button>
        </nav>
      </div>

      {/* Tab content */}
      {activeTab === 'volunteers' && <VolunteersManagement />}
      {activeTab === 'schedule' && <ScheduleManagement />}
      {activeTab === 'attendance' && <AttendanceManagement />}
    </PageContainer>
  );
}

// Volunteers Management Tab
function VolunteersManagement() {
  const { user } = useAuth();
  const [volunteers, setVolunteers] = useState<VolunteerListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  // Modal states
  const [selectedVolunteer, setSelectedVolunteer] = useState<VolunteerDetail | null>(null);
  const [actionModal, setActionModal] = useState<'approve' | 'reject' | 'complete' | 'suspend' | 'resume' | null>(null);

  const fetchVolunteers = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const params: GetVolunteersParams = {
        page,
        pageSize: 10,
        searchTerm: searchTerm || undefined,
        status: statusFilter as VolunteerStatus || undefined,
      };
      const result = await volunteersApi.getVolunteers(params);
      setVolunteers(result.items);
      setTotalPages(result.totalPages);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchVolunteers();
  }, [page, searchTerm, statusFilter]);

  const handleAction = async (volunteer: VolunteerListItem, action: typeof actionModal) => {
    try {
      const detail = await volunteersApi.getVolunteer(volunteer.id);
      setSelectedVolunteer(detail);
      setActionModal(action);
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const closeModal = () => {
    setActionModal(null);
    setSelectedVolunteer(null);
  };

  // Certificate download
  const [certificateModal, setCertificateModal] = useState<VolunteerListItem | null>(null);
  const [certificateFromDate, setCertificateFromDate] = useState(() => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 1);
    return date.toISOString().split('T')[0];
  });
  const [certificateToDate, setCertificateToDate] = useState(() => {
    return new Date().toISOString().split('T')[0];
  });
  const [isDownloadingCertificate, setIsDownloadingCertificate] = useState(false);

  const handleDownloadCertificate = async () => {
    if (!certificateModal) return;
    setIsDownloadingCertificate(true);
    try {
      const blob = await volunteersApi.downloadCertificate(
        certificateModal.id,
        certificateFromDate,
        certificateToDate
      );
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `zaswiadczenie_${certificateModal.fullName.replace(/\s+/g, '_')}_${certificateFromDate}_${certificateToDate}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      toast.success('Zaświadczenie zostało pobrane');
      setCertificateModal(null);
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsDownloadingCertificate(false);
    }
  };

  const handleSuccess = () => {
    closeModal();
    fetchVolunteers();
  };

  // Stats
  const candidateCount = volunteers.filter(v => v.status === 'Candidate').length;
  const inTrainingCount = volunteers.filter(v => v.status === 'InTraining').length;
  const activeCount = volunteers.filter(v => v.status === 'Active').length;

  return (
    <>
      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <Card className="p-4">
          <p className="text-sm text-gray-500">Kandydaci</p>
          <p className="text-2xl font-bold text-blue-600">{candidateCount}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">W szkoleniu</p>
          <p className="text-2xl font-bold text-yellow-600">{inTrainingCount}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Aktywni</p>
          <p className="text-2xl font-bold text-green-600">{activeCount}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Wszystkich</p>
          <p className="text-2xl font-bold text-gray-900">{volunteers.length}</p>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <Input
          placeholder="Szukaj po nazwie lub email..."
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setPage(1); }}
          wrapperClassName="w-72"
        />
        <Select
          options={statusFilterOptions}
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          wrapperClassName="w-48"
        />
      </div>

      {/* Volunteers Table */}
      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : volunteers.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Brak wolontariuszy</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Wolontariusz
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Data zgloszenia
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Przepracowane
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                    Akcje
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {volunteers.map((volunteer) => (
                  <tr key={volunteer.id} className="hover:bg-gray-50">
                    <td className="px-4 py-4">
                      <div>
                        <p className="font-medium text-gray-900">{volunteer.fullName}</p>
                        <p className="text-sm text-gray-500">{volunteer.email}</p>
                      </div>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <Badge variant={getStatusBadgeVariant(volunteer.status)}>
                        {getVolunteerStatusLabel(volunteer.status)}
                      </Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(volunteer.applicationDate).toLocaleDateString('pl-PL')}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatHours(volunteer.totalHoursWorked)}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-right">
                      <div className="flex justify-end gap-1">
                        {volunteer.status === 'Candidate' && (
                          <>
                            <button
                              onClick={() => handleAction(volunteer, 'approve')}
                              className="p-1.5 text-green-600 hover:bg-green-50 rounded"
                              title="Zatwierdz"
                            >
                              <CheckIcon className="h-5 w-5" />
                            </button>
                            <button
                              onClick={() => handleAction(volunteer, 'reject')}
                              className="p-1.5 text-red-600 hover:bg-red-50 rounded"
                              title="Odrzuc"
                            >
                              <XMarkIcon className="h-5 w-5" />
                            </button>
                          </>
                        )}
                        {volunteer.status === 'InTraining' && (
                          <button
                            onClick={() => handleAction(volunteer, 'complete')}
                            className="p-1.5 text-green-600 hover:bg-green-50 rounded"
                            title="Zakoncz szkolenie"
                          >
                            <AcademicCapIcon className="h-5 w-5" />
                          </button>
                        )}
                        {volunteer.status === 'Active' && (
                          <button
                            onClick={() => handleAction(volunteer, 'suspend')}
                            className="p-1.5 text-orange-600 hover:bg-orange-50 rounded"
                            title="Zawies"
                          >
                            <PauseIcon className="h-5 w-5" />
                          </button>
                        )}
                        {volunteer.status === 'Suspended' && (
                          <button
                            onClick={() => handleAction(volunteer, 'resume')}
                            className="p-1.5 text-green-600 hover:bg-green-50 rounded"
                            title="Wznow"
                          >
                            <PlayIcon className="h-5 w-5" />
                          </button>
                        )}
                        {/* Certificate button for volunteers with possible hours */}
                        {(volunteer.status === 'Active' || volunteer.status === 'InTraining' || volunteer.status === 'Suspended') && (
                          <button
                            onClick={() => setCertificateModal(volunteer)}
                            className="p-1.5 text-blue-600 hover:bg-blue-50 rounded"
                            title="Generuj zaświadczenie"
                          >
                            <DocumentTextIcon className="h-5 w-5" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
            <p className="text-sm text-gray-500">
              Strona {page} z {totalPages}
            </p>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
              >
                Poprzednia
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage(page + 1)}
              >
                Nastepna
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Action Modals */}
      <ApproveModal
        isOpen={actionModal === 'approve'}
        onClose={closeModal}
        onSuccess={handleSuccess}
        volunteer={selectedVolunteer}
        userId={user?.id || ''}
        userName={user ? `${user.firstName} ${user.lastName}` : ''}
      />
      <RejectModal
        isOpen={actionModal === 'reject'}
        onClose={closeModal}
        onSuccess={handleSuccess}
        volunteer={selectedVolunteer}
        userName={user ? `${user.firstName} ${user.lastName}` : ''}
      />
      <CompleteTrainingModal
        isOpen={actionModal === 'complete'}
        onClose={closeModal}
        onSuccess={handleSuccess}
        volunteer={selectedVolunteer}
        userName={user ? `${user.firstName} ${user.lastName}` : ''}
      />
      <SuspendModal
        isOpen={actionModal === 'suspend'}
        onClose={closeModal}
        onSuccess={handleSuccess}
        volunteer={selectedVolunteer}
        userName={user ? `${user.firstName} ${user.lastName}` : ''}
      />
      <ResumeModal
        isOpen={actionModal === 'resume'}
        onClose={closeModal}
        onSuccess={handleSuccess}
        volunteer={selectedVolunteer}
        userName={user ? `${user.firstName} ${user.lastName}` : ''}
      />

      {/* Certificate Modal */}
      <Modal
        isOpen={certificateModal !== null}
        onClose={() => setCertificateModal(null)}
        title="Generuj zaświadczenie"
      >
        {certificateModal && (
          <div className="space-y-4">
            <p className="text-gray-600">
              Wygeneruj zaświadczenie o pracy wolontariackiej dla:{' '}
              <strong>{certificateModal.fullName}</strong>
            </p>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Data od
                </label>
                <input
                  type="date"
                  value={certificateFromDate}
                  onChange={(e) => setCertificateFromDate(e.target.value)}
                  className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Data do
                </label>
                <input
                  type="date"
                  value={certificateToDate}
                  onChange={(e) => setCertificateToDate(e.target.value)}
                  className="w-full rounded-md border-gray-300 shadow-sm focus:border-primary-500 focus:ring-primary-500"
                />
              </div>
            </div>

            <p className="text-sm text-gray-500">
              Zaświadczenie będzie zawierało informacje o łącznej liczbie przepracowanych godzin
              w wybranym okresie.
            </p>

            <div className="flex justify-end gap-3 pt-4">
              <Button variant="outline" onClick={() => setCertificateModal(null)}>
                Anuluj
              </Button>
              <Button
                onClick={handleDownloadCertificate}
                disabled={isDownloadingCertificate}
              >
                {isDownloadingCertificate ? 'Generowanie...' : 'Pobierz PDF'}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}

// Schedule Management Tab
function ScheduleManagement() {
  const { user } = useAuth();
  const [slots, setSlots] = useState<ScheduleSlot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [fromDate, setFromDate] = useState(() => {
    const today = new Date();
    return today.toISOString().split('T')[0];
  });
  const [toDate, setToDate] = useState(() => {
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 14);
    return nextWeek.toISOString().split('T')[0];
  });
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isBulkModalOpen, setIsBulkModalOpen] = useState(false);

  const fetchSchedule = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await scheduleApi.getSchedule(fromDate, toDate, false);
      setSlots(data);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchSchedule();
  }, [fromDate, toDate]);

  const handleSuccess = () => {
    setIsCreateModalOpen(false);
    setIsBulkModalOpen(false);
    fetchSchedule();
  };

  // Group slots by date
  const slotsByDate = slots.reduce((acc, slot) => {
    const date = slot.date;
    if (!acc[date]) acc[date] = [];
    acc[date].push(slot);
    return acc;
  }, {} as Record<string, ScheduleSlot[]>);

  return (
    <>
      {/* Actions and filters */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <Button onClick={() => setIsCreateModalOpen(true)} leftIcon={<PlusIcon className="h-5 w-5" />}>
          Dodaj slot
        </Button>
        <Button onClick={() => setIsBulkModalOpen(true)} variant="outline" leftIcon={<CalendarDaysIcon className="h-5 w-5" />}>
          Generuj harmonogram
        </Button>
        <div className="flex-1" />
        <div className="flex gap-2">
          <Input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="w-40"
          />
          <span className="self-center text-gray-500">-</span>
          <Input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="w-40"
          />
        </div>
      </div>

      {/* Schedule Grid */}
      {isLoading ? (
        <div className="p-8 flex justify-center">
          <Spinner size="lg" />
        </div>
      ) : error ? (
        <div className="p-8 text-center text-red-600">{error}</div>
      ) : Object.keys(slotsByDate).length === 0 ? (
        <Card className="p-8 text-center text-gray-500">Brak slotow w wybranym okresie</Card>
      ) : (
        <div className="space-y-4">
          {Object.entries(slotsByDate)
            .sort(([a], [b]) => a.localeCompare(b))
            .map(([date, daySlots]) => (
              <Card key={date} className="p-4">
                <h3 className="font-medium text-gray-900 mb-3">
                  {new Date(date).toLocaleDateString('pl-PL', {
                    weekday: 'long',
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </h3>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {daySlots.map((slot) => (
                    <div
                      key={slot.id}
                      className={`p-3 rounded-lg border ${
                        slot.isActive ? 'bg-white border-gray-200' : 'bg-gray-50 border-gray-300'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <span className="font-medium">
                          {slot.startTime} - {slot.endTime}
                        </span>
                        <Badge variant={slot.hasAvailableSpots ? 'green' : 'gray'}>
                          {slot.currentVolunteers}/{slot.maxVolunteers}
                        </Badge>
                      </div>
                      <p className="text-sm text-gray-600">{slot.description}</p>
                      {slot.assignments && slot.assignments.length > 0 && (
                        <div className="mt-2 pt-2 border-t border-gray-100">
                          <p className="text-xs text-gray-500 mb-1">Przypisani:</p>
                          <div className="flex flex-wrap gap-1">
                            {slot.assignments.map((a) => (
                              <span
                                key={a.id}
                                className="text-xs px-2 py-0.5 bg-blue-50 text-blue-700 rounded"
                              >
                                {a.volunteerName}
                              </span>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </Card>
            ))}
        </div>
      )}

      {/* Create Slot Modal */}
      <CreateSlotModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSuccess={handleSuccess}
        userId={user?.id || ''}
      />

      {/* Bulk Create Modal */}
      <BulkCreateSlotsModal
        isOpen={isBulkModalOpen}
        onClose={() => setIsBulkModalOpen(false)}
        onSuccess={handleSuccess}
        userId={user?.id || ''}
      />
    </>
  );
}

// Attendance Management Tab
function AttendanceManagement() {
  const { user } = useAuth();
  const [attendances, setAttendances] = useState<AttendanceListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [volunteers, setVolunteers] = useState<VolunteerListItem[]>([]);
  const [selectedVolunteerId, setSelectedVolunteerId] = useState('');

  useEffect(() => {
    fetchVolunteers();
  }, []);

  useEffect(() => {
    if (selectedVolunteerId) {
      fetchAttendances();
    }
  }, [selectedVolunteerId]);

  const fetchVolunteers = async () => {
    try {
      const result = await volunteersApi.getVolunteers({ page: 1, pageSize: 100, status: 'Active' });
      setVolunteers(result.items);
      if (result.items.length > 0) {
        setSelectedVolunteerId(result.items[0].id);
      }
    } catch (err) {
      console.error('Failed to fetch volunteers:', err);
    }
  };

  const fetchAttendances = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await attendanceApi.getVolunteerAttendances(selectedVolunteerId, {
        page: 1,
        pageSize: 50,
      });
      setAttendances(result.items);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  const handleApprove = async (attendanceId: string) => {
    try {
      await attendanceApi.approve(attendanceId, user?.id || '');
      toast.success('Obecnosc zatwierdzona');
      fetchAttendances();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const volunteerOptions = [
    { value: '', label: 'Wybierz wolontariusza' },
    ...volunteers.map((v) => ({ value: v.id, label: v.fullName })),
  ];

  return (
    <>
      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <Select
          options={volunteerOptions}
          value={selectedVolunteerId}
          onChange={(e) => setSelectedVolunteerId(e.target.value)}
          className="sm:max-w-[300px]"
        />
      </div>

      {/* Attendances Table */}
      <Card className="overflow-hidden">
        {!selectedVolunteerId ? (
          <div className="p-8 text-center text-gray-500">Wybierz wolontariusza</div>
        ) : isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : attendances.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Brak wpisow obecnosci</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Data
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Wejscie
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Wyjscie
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Godziny
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                    Akcje
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {attendances.map((attendance) => (
                  <tr key={attendance.id} className="hover:bg-gray-50">
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-900">
                      {new Date(attendance.date).toLocaleDateString('pl-PL')}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {attendance.checkInTime.substring(0, 5)}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {attendance.checkOutTime
                        ? attendance.checkOutTime.substring(0, 5)
                        : '-'}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-900">
                      {attendance.hoursWorked ? formatHours(attendance.hoursWorked) : '-'}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <Badge variant={attendance.isApproved ? 'green' : 'yellow'}>
                        {attendance.isApproved ? 'Zatwierdzona' : 'Oczekuje'}
                      </Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-right">
                      {!attendance.isApproved && attendance.checkOutTime && (
                        <button
                          onClick={() => handleApprove(attendance.id)}
                          className="p-1.5 text-green-600 hover:bg-green-50 rounded"
                          title="Zatwierdz"
                        >
                          <CheckIcon className="h-5 w-5" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </>
  );
}

// Modal Components

interface ApproveModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  volunteer: VolunteerDetail | null;
  userId: string;
  userName: string;
}

function ApproveModal({ isOpen, onClose, onSuccess, volunteer, userId, userName }: ApproveModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<ApproveVolunteerRequest>({
    approvedByUserId: userId,
    approvedByName: userName,
    trainingStartDate: new Date().toISOString().split('T')[0],
    notes: '',
  });

  useEffect(() => {
    setFormData({
      approvedByUserId: userId,
      approvedByName: userName,
      trainingStartDate: new Date().toISOString().split('T')[0],
      notes: '',
    });
  }, [isOpen, userId, userName]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!volunteer) return;

    setIsSubmitting(true);
    try {
      await volunteersApi.approve(volunteer.id, formData);
      toast.success('Wolontariusz zostal zatwierdzony');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Zatwierdz wolontariusza">
      {volunteer && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Zatwierdzenie wolontariusza: <strong>{volunteer.fullName}</strong>
          </p>
          <Input
            label="Data rozpoczecia szkolenia"
            type="date"
            value={formData.trainingStartDate}
            onChange={(e) => setFormData({ ...formData, trainingStartDate: e.target.value })}
          />
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Notatki</label>
            <textarea
              value={formData.notes}
              onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500"
              placeholder="Opcjonalne notatki..."
            />
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
            <Button type="submit" isLoading={isSubmitting}>Zatwierdz</Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

interface RejectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  volunteer: VolunteerDetail | null;
  userName: string;
}

function RejectModal({ isOpen, onClose, onSuccess, volunteer, userName }: RejectModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [reason, setReason] = useState('');

  useEffect(() => {
    setReason('');
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!volunteer || !reason.trim()) return;

    setIsSubmitting(true);
    try {
      await volunteersApi.reject(volunteer.id, { rejectedByName: userName, reason });
      toast.success('Wniosek zostal odrzucony');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Odrzuc wniosek">
      {volunteer && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Odrzucenie wniosku: <strong>{volunteer.fullName}</strong>
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Powod odrzucenia <span className="text-red-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500"
              placeholder="Podaj powod odrzucenia..."
            />
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
            <Button type="submit" variant="danger" isLoading={isSubmitting}>Odrzuc</Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

interface CompleteTrainingModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  volunteer: VolunteerDetail | null;
  userName: string;
}

function CompleteTrainingModal({ isOpen, onClose, onSuccess, volunteer, userName }: CompleteTrainingModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CompleteTrainingRequest>({
    completedByName: userName,
    contractNumber: '',
    trainingEndDate: new Date().toISOString().split('T')[0],
  });

  useEffect(() => {
    setFormData({
      completedByName: userName,
      contractNumber: '',
      trainingEndDate: new Date().toISOString().split('T')[0],
    });
  }, [isOpen, userName]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!volunteer || !formData.contractNumber.trim()) return;

    setIsSubmitting(true);
    try {
      await volunteersApi.completeTraining(volunteer.id, formData);
      toast.success('Szkolenie zakonczone, wolontariusz aktywowany');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Zakoncz szkolenie">
      {volunteer && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Zakonczenie szkolenia: <strong>{volunteer.fullName}</strong>
          </p>
          <Input
            label="Numer umowy"
            value={formData.contractNumber}
            onChange={(e) => setFormData({ ...formData, contractNumber: e.target.value })}
            required
            placeholder="np. VOL/2024/001"
          />
          <Input
            label="Data zakonczenia szkolenia"
            type="date"
            value={formData.trainingEndDate}
            onChange={(e) => setFormData({ ...formData, trainingEndDate: e.target.value })}
          />
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
            <Button type="submit" isLoading={isSubmitting}>Zakoncz szkolenie</Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

interface SuspendModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  volunteer: VolunteerDetail | null;
  userName: string;
}

function SuspendModal({ isOpen, onClose, onSuccess, volunteer, userName }: SuspendModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [reason, setReason] = useState('');

  useEffect(() => {
    setReason('');
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!volunteer || !reason.trim()) return;

    setIsSubmitting(true);
    try {
      await volunteersApi.suspend(volunteer.id, { suspendedByName: userName, reason });
      toast.success('Wolontariusz zostal zawieszony');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Zawies wolontariusza">
      {volunteer && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Zawieszenie wolontariusza: <strong>{volunteer.fullName}</strong>
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Powod zawieszenia <span className="text-red-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500"
              placeholder="Podaj powod zawieszenia..."
            />
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
            <Button type="submit" variant="danger" isLoading={isSubmitting}>Zawies</Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

interface ResumeModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  volunteer: VolunteerDetail | null;
  userName: string;
}

function ResumeModal({ isOpen, onClose, onSuccess, volunteer, userName }: ResumeModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notes, setNotes] = useState('');

  useEffect(() => {
    setNotes('');
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!volunteer) return;

    setIsSubmitting(true);
    try {
      await volunteersApi.resume(volunteer.id, { resumedByName: userName, notes: notes || undefined });
      toast.success('Wolontariusz zostal wznowiony');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Wznow wolontariusza">
      {volunteer && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Wznowienie wolontariusza: <strong>{volunteer.fullName}</strong>
          </p>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Notatki</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500"
              placeholder="Opcjonalne notatki..."
            />
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
            <Button type="submit" isLoading={isSubmitting}>Wznow</Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

interface CreateSlotModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  userId: string;
}

function CreateSlotModal({ isOpen, onClose, onSuccess, userId }: CreateSlotModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateSlotRequest>({
    date: new Date().toISOString().split('T')[0],
    startTime: '09:00',
    endTime: '13:00',
    maxVolunteers: 3,
    description: 'Dyzur w schronisku',
    createdByUserId: userId,
  });

  useEffect(() => {
    setFormData({
      date: new Date().toISOString().split('T')[0],
      startTime: '09:00',
      endTime: '13:00',
      maxVolunteers: 3,
      description: 'Dyzur w schronisku',
      createdByUserId: userId,
    });
  }, [isOpen, userId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    setIsSubmitting(true);
    try {
      await scheduleApi.createSlot(formData);
      toast.success('Slot zostal utworzony');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Dodaj slot">
      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Data"
          type="date"
          value={formData.date}
          onChange={(e) => setFormData({ ...formData, date: e.target.value })}
          required
        />
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="Od"
            type="time"
            value={formData.startTime}
            onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
            required
          />
          <Input
            label="Do"
            type="time"
            value={formData.endTime}
            onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
            required
          />
        </div>
        <Input
          label="Maks. wolontariuszy"
          type="number"
          min="1"
          max="10"
          value={formData.maxVolunteers}
          onChange={(e) => setFormData({ ...formData, maxVolunteers: Number(e.target.value) })}
          required
        />
        <Input
          label="Opis"
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          required
        />
        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
          <Button type="submit" isLoading={isSubmitting}>Utworz</Button>
        </div>
      </form>
    </Modal>
  );
}

interface BulkCreateSlotsModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  userId: string;
}

function BulkCreateSlotsModal({ isOpen, onClose, onSuccess, userId }: BulkCreateSlotsModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateSlotsBulkRequest>({
    startDate: new Date().toISOString().split('T')[0],
    endDate: (() => {
      const d = new Date();
      d.setDate(d.getDate() + 14);
      return d.toISOString().split('T')[0];
    })(),
    daysOfWeek: [1, 2, 3, 4, 5],
    startTime: '09:00',
    endTime: '13:00',
    maxVolunteers: 3,
    description: 'Dyzur w schronisku',
    createdByUserId: userId,
  });

  const daysOptions = [
    { value: 0, label: 'Niedziela' },
    { value: 1, label: 'Poniedzialek' },
    { value: 2, label: 'Wtorek' },
    { value: 3, label: 'Sroda' },
    { value: 4, label: 'Czwartek' },
    { value: 5, label: 'Piatek' },
    { value: 6, label: 'Sobota' },
  ];

  useEffect(() => {
    setFormData({
      startDate: new Date().toISOString().split('T')[0],
      endDate: (() => {
        const d = new Date();
        d.setDate(d.getDate() + 14);
        return d.toISOString().split('T')[0];
      })(),
      daysOfWeek: [1, 2, 3, 4, 5],
      startTime: '09:00',
      endTime: '13:00',
      maxVolunteers: 3,
      description: 'Dyzur w schronisku',
      createdByUserId: userId,
    });
  }, [isOpen, userId]);

  const toggleDay = (day: number) => {
    if (formData.daysOfWeek.includes(day)) {
      setFormData({ ...formData, daysOfWeek: formData.daysOfWeek.filter((d) => d !== day) });
    } else {
      setFormData({ ...formData, daysOfWeek: [...formData.daysOfWeek, day].sort() });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (formData.daysOfWeek.length === 0) {
      toast.error('Wybierz przynajmniej jeden dzien');
      return;
    }

    setIsSubmitting(true);
    try {
      await scheduleApi.createSlotsBulk(formData);
      toast.success('Harmonogram zostal wygenerowany');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Generuj harmonogram">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="Data od"
            type="date"
            value={formData.startDate}
            onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
            required
          />
          <Input
            label="Data do"
            type="date"
            value={formData.endDate}
            onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
            required
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Dni tygodnia</label>
          <div className="flex flex-wrap gap-2">
            {daysOptions.map((day) => (
              <button
                key={day.value}
                type="button"
                onClick={() => toggleDay(day.value)}
                className={`px-3 py-1 rounded-full text-sm ${
                  formData.daysOfWeek.includes(day.value)
                    ? 'bg-primary-500 text-white'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                {day.label}
              </button>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <Input
            label="Od"
            type="time"
            value={formData.startTime}
            onChange={(e) => setFormData({ ...formData, startTime: e.target.value })}
            required
          />
          <Input
            label="Do"
            type="time"
            value={formData.endTime}
            onChange={(e) => setFormData({ ...formData, endTime: e.target.value })}
            required
          />
        </div>
        <Input
          label="Maks. wolontariuszy"
          type="number"
          min="1"
          max="10"
          value={formData.maxVolunteers}
          onChange={(e) => setFormData({ ...formData, maxVolunteers: Number(e.target.value) })}
          required
        />
        <Input
          label="Opis"
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          required
        />
        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose}>Anuluj</Button>
          <Button type="submit" isLoading={isSubmitting}>Generuj</Button>
        </div>
      </form>
    </Modal>
  );
}
