import { useState, useEffect, useCallback } from 'react';
import {
  ChartBarIcon,
  DocumentArrowDownIcon,
  CalendarIcon,
  UserGroupIcon,
  HeartIcon,
  HomeModernIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Card, Button, Spinner } from '@/components/common';
import {
  getStatistics,
  getGovernmentReport,
  downloadAdmissionsCsv,
  downloadAdoptionsCsv,
  downloadVolunteerHoursCsv,
  downloadGovernmentReportPdf,
  downloadFile,
  type ShelterStatistics,
  type GovernmentReport,
} from '@/api/reports';

// Default to last 30 days
const getDefaultDates = () => {
  const toDate = new Date();
  const fromDate = new Date();
  fromDate.setDate(fromDate.getDate() - 30);
  return {
    fromDate: fromDate.toISOString().split('T')[0],
    toDate: toDate.toISOString().split('T')[0],
  };
};

export function AdminReportsPage() {
  const defaultDates = getDefaultDates();
  const [fromDate, setFromDate] = useState(defaultDates.fromDate);
  const [toDate, setToDate] = useState(defaultDates.toDate);
  const [activeTab, setActiveTab] = useState<'statistics' | 'government'>('statistics');
  const [isDownloading, setIsDownloading] = useState<string | null>(null);

  // Statistics state
  const [statistics, setStatistics] = useState<ShelterStatistics | null>(null);
  const [statsLoading, setStatsLoading] = useState(false);
  const [statsError, setStatsError] = useState<string | null>(null);

  // Government report state
  const [governmentReport, setGovernmentReport] = useState<GovernmentReport | null>(null);
  const [reportLoading, setReportLoading] = useState(false);
  const [reportError, setReportError] = useState<string | null>(null);

  // Load statistics
  const loadStatistics = useCallback(async () => {
    if (activeTab !== 'statistics') return;

    setStatsLoading(true);
    setStatsError(null);
    try {
      const data = await getStatistics(fromDate, toDate);
      setStatistics(data);
    } catch (err: unknown) {
      const error = err as { response?: { status?: number } };
      if (error.response?.status === 401) {
        setStatsError('Brak autoryzacji. Zaloguj się ponownie.');
      } else if (error.response?.status === 403) {
        setStatsError('Brak uprawnień do przeglądania raportów.');
      } else {
        setStatsError('Wystąpił błąd podczas ładowania statystyk');
      }
      console.error(err);
    } finally {
      setStatsLoading(false);
    }
  }, [fromDate, toDate, activeTab]);

  // Load government report
  const loadGovernmentReport = useCallback(async () => {
    if (activeTab !== 'government') return;

    setReportLoading(true);
    setReportError(null);
    try {
      const data = await getGovernmentReport(fromDate, toDate, 'json') as GovernmentReport;
      setGovernmentReport(data);
    } catch (err: unknown) {
      const error = err as { response?: { status?: number } };
      if (error.response?.status === 401) {
        setReportError('Brak autoryzacji. Zaloguj się ponownie.');
      } else if (error.response?.status === 403) {
        setReportError('Brak uprawnień do przeglądania raportów.');
      } else {
        setReportError('Wystąpił błąd podczas ładowania raportu');
      }
      console.error(err);
    } finally {
      setReportLoading(false);
    }
  }, [fromDate, toDate, activeTab]);

  useEffect(() => {
    if (activeTab === 'statistics') {
      loadStatistics();
    } else {
      loadGovernmentReport();
    }
  }, [activeTab, fromDate, toDate, loadStatistics, loadGovernmentReport]);

  const handleDownload = async (type: 'admissions' | 'adoptions' | 'volunteer-hours' | 'full-pdf') => {
    setIsDownloading(type);
    try {
      let blob: Blob;
      let fileName: string;

      switch (type) {
        case 'admissions':
          blob = await downloadAdmissionsCsv(fromDate, toDate);
          fileName = `przyjecia_${fromDate}_${toDate}.csv`;
          break;
        case 'adoptions':
          blob = await downloadAdoptionsCsv(fromDate, toDate);
          fileName = `adopcje_${fromDate}_${toDate}.csv`;
          break;
        case 'volunteer-hours':
          blob = await downloadVolunteerHoursCsv(fromDate, toDate);
          fileName = `godziny_wolontariuszy_${fromDate}_${toDate}.csv`;
          break;
        case 'full-pdf':
          blob = await downloadGovernmentReportPdf(fromDate, toDate);
          fileName = `raport_samorzadowy_${fromDate}_${toDate}.pdf`;
          break;
      }

      downloadFile(blob, fileName);
    } catch (error) {
      console.error('Download error:', error);
    } finally {
      setIsDownloading(null);
    }
  };

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Raporty i statystyki</h1>
        <p className="mt-2 text-gray-600">Generuj raporty i przeglądaj statystyki schroniska</p>
      </div>

      {/* Date range selector */}
      <Card className="p-4 mb-6">
        <div className="flex flex-wrap items-center gap-4">
          <div className="flex items-center gap-2">
            <CalendarIcon className="h-5 w-5 text-gray-400" />
            <span className="text-sm font-medium text-gray-700">Okres:</span>
          </div>
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
            <span className="text-gray-500">-</span>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>
          <div className="flex gap-2 ml-auto">
            <button
              onClick={() => {
                const d = getDefaultDates();
                setFromDate(d.fromDate);
                setToDate(d.toDate);
              }}
              className="text-sm text-primary-600 hover:text-primary-700"
            >
              Ostatnie 30 dni
            </button>
            <button
              onClick={() => {
                const now = new Date();
                const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
                setFromDate(firstDay.toISOString().split('T')[0]);
                setToDate(now.toISOString().split('T')[0]);
              }}
              className="text-sm text-primary-600 hover:text-primary-700"
            >
              Ten miesiąc
            </button>
            <button
              onClick={() => {
                const now = new Date();
                const firstDay = new Date(now.getFullYear(), 0, 1);
                setFromDate(firstDay.toISOString().split('T')[0]);
                setToDate(now.toISOString().split('T')[0]);
              }}
              className="text-sm text-primary-600 hover:text-primary-700"
            >
              Ten rok
            </button>
          </div>
        </div>
      </Card>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab('statistics')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'statistics'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <ChartBarIcon className="h-5 w-5 inline mr-2" />
            Statystyki
          </button>
          <button
            onClick={() => setActiveTab('government')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'government'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <DocumentArrowDownIcon className="h-5 w-5 inline mr-2" />
            Raporty samorządowe
          </button>
        </nav>
      </div>

      {/* Statistics Tab */}
      {activeTab === 'statistics' && (
        <StatisticsView
          statistics={statistics}
          isLoading={statsLoading}
          error={statsError}
        />
      )}

      {/* Government Reports Tab */}
      {activeTab === 'government' && (
        <GovernmentReportView
          report={governmentReport}
          isLoading={reportLoading}
          error={reportError}
          onDownload={handleDownload}
          isDownloading={isDownloading}
        />
      )}
    </PageContainer>
  );
}

// Statistics View Component
function StatisticsView({
  statistics,
  isLoading,
  error,
}: {
  statistics: ShelterStatistics | null;
  isLoading: boolean;
  error: string | null;
}) {
  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12 text-red-600">
        {error}
      </div>
    );
  }

  if (!statistics) {
    return null;
  }

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          icon={HeartIcon}
          title="Adopcje"
          value={statistics.adoptions.completedAdoptions}
          subtitle={`${statistics.adoptions.totalApplications} zgłoszeń łącznie`}
          color="green"
        />
        <StatCard
          icon={HomeModernIcon}
          title="Zwierzęta w schronisku"
          value={statistics.animals.totalAnimalsInShelter}
          subtitle={`${statistics.animals.admissionsInPeriod} przyjęć w okresie`}
          color="blue"
        />
        <StatCard
          icon={UserGroupIcon}
          title="Aktywni wolontariusze"
          value={statistics.volunteers.totalActiveVolunteers}
          subtitle={`${statistics.volunteers.newVolunteersInPeriod} nowych`}
          color="purple"
        />
        <StatCard
          icon={CalendarIcon}
          title="Godziny wolontariatu"
          value={Math.round(statistics.volunteers.totalHoursWorked)}
          subtitle={`Średnio ${statistics.volunteers.averageHoursPerVolunteer.toFixed(1)}h/os.`}
          color="orange"
        />
      </div>

      {/* Adoptions Section */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Statystyki adopcji</h3>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6">
          <div className="text-center p-3 bg-gray-50 rounded-lg">
            <div className="text-2xl font-bold text-gray-900">{statistics.adoptions.totalApplications}</div>
            <div className="text-xs text-gray-500">Zgłoszenia</div>
          </div>
          <div className="text-center p-3 bg-green-50 rounded-lg">
            <div className="text-2xl font-bold text-green-600">{statistics.adoptions.completedAdoptions}</div>
            <div className="text-xs text-gray-500">Zakończone</div>
          </div>
          <div className="text-center p-3 bg-yellow-50 rounded-lg">
            <div className="text-2xl font-bold text-yellow-600">{statistics.adoptions.pendingApplications}</div>
            <div className="text-xs text-gray-500">W trakcie</div>
          </div>
          <div className="text-center p-3 bg-red-50 rounded-lg">
            <div className="text-2xl font-bold text-red-600">{statistics.adoptions.rejectedApplications}</div>
            <div className="text-xs text-gray-500">Odrzucone</div>
          </div>
          <div className="text-center p-3 bg-gray-50 rounded-lg">
            <div className="text-2xl font-bold text-gray-600">{statistics.adoptions.cancelledApplications}</div>
            <div className="text-xs text-gray-500">Anulowane</div>
          </div>
        </div>

        {statistics.adoptions.byMonth.length > 0 && (
          <div className="mb-6">
            <h4 className="text-sm font-medium text-gray-700 mb-3">Adopcje w czasie</h4>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Miesiąc</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">Zgłoszenia</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">Zakończone</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {statistics.adoptions.byMonth.map((month) => (
                    <tr key={`${month.year}-${month.month}`}>
                      <td className="px-4 py-2 text-sm text-gray-900">{month.monthName}</td>
                      <td className="px-4 py-2 text-sm text-gray-600 text-right">{month.applicationsCount}</td>
                      <td className="px-4 py-2 text-sm text-green-600 text-right font-medium">{month.completedCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {statistics.adoptions.bySpecies.length > 0 && (
          <div>
            <h4 className="text-sm font-medium text-gray-700 mb-3">Adopcje wg gatunku</h4>
            <div className="flex flex-wrap gap-4">
              {statistics.adoptions.bySpecies.map((species) => (
                <div key={species.species} className="flex items-center gap-2 bg-gray-50 px-4 py-2 rounded-lg">
                  <span className="text-sm text-gray-700">{species.speciesLabel}:</span>
                  <span className="text-lg font-semibold text-primary-600">{species.count}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </Card>

      {/* Volunteers Section */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Aktywność wolontariuszy</h3>

        {statistics.volunteers.topVolunteers.length > 0 && (
          <div className="mb-6">
            <h4 className="text-sm font-medium text-gray-700 mb-3">Top 10 wolontariuszy</h4>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">#</th>
                    <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Wolontariusz</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">Godziny</th>
                    <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">Dni</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {statistics.volunteers.topVolunteers.map((volunteer, index) => (
                    <tr key={volunteer.volunteerId}>
                      <td className="px-4 py-2 text-sm text-gray-500">{index + 1}</td>
                      <td className="px-4 py-2 text-sm text-gray-900 font-medium">{volunteer.name}</td>
                      <td className="px-4 py-2 text-sm text-primary-600 text-right font-medium">
                        {volunteer.hoursWorked.toFixed(1)}h
                      </td>
                      <td className="px-4 py-2 text-sm text-gray-600 text-right">{volunteer.daysWorked}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </Card>

      {/* Animals Section */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Stan zwierząt</h3>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          <div className="text-center p-3 bg-blue-50 rounded-lg">
            <div className="text-2xl font-bold text-blue-600">{statistics.animals.totalAnimalsInShelter}</div>
            <div className="text-xs text-gray-500">W schronisku</div>
          </div>
          <div className="text-center p-3 bg-gray-50 rounded-lg">
            <div className="text-2xl font-bold text-gray-600">{statistics.animals.admissionsInPeriod}</div>
            <div className="text-xs text-gray-500">Przyjęcia</div>
          </div>
          <div className="text-center p-3 bg-green-50 rounded-lg">
            <div className="text-2xl font-bold text-green-600">{statistics.animals.adoptionsInPeriod}</div>
            <div className="text-xs text-gray-500">Adopcje</div>
          </div>
          <div className="text-center p-3 bg-red-50 rounded-lg">
            <div className="text-2xl font-bold text-red-600">{statistics.animals.deceasedInPeriod}</div>
            <div className="text-xs text-gray-500">Zgony</div>
          </div>
        </div>

        <div className="grid md:grid-cols-2 gap-6">
          {statistics.animals.bySpecies.length > 0 && (
            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-3">Wg gatunku</h4>
              <div className="space-y-2">
                {statistics.animals.bySpecies.map((species) => (
                  <div key={species.species} className="flex justify-between items-center">
                    <span className="text-sm text-gray-700">{species.speciesLabel}</span>
                    <span className="text-sm font-medium text-gray-900">{species.count}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {statistics.animals.byStatus.length > 0 && (
            <div>
              <h4 className="text-sm font-medium text-gray-700 mb-3">Wg statusu</h4>
              <div className="space-y-2">
                {statistics.animals.byStatus.map((status) => (
                  <div key={status.status} className="flex justify-between items-center">
                    <span className="text-sm text-gray-700">{status.statusLabel}</span>
                    <span className="text-sm font-medium text-gray-900">{status.count}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}

// Government Report View Component
function GovernmentReportView({
  report,
  isLoading,
  error,
  onDownload,
  isDownloading,
}: {
  report: GovernmentReport | null;
  isLoading: boolean;
  error: string | null;
  onDownload: (type: 'admissions' | 'adoptions' | 'volunteer-hours' | 'full-pdf') => void;
  isDownloading: string | null;
}) {
  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12 text-red-600">
        {error}
      </div>
    );
  }

  if (!report) {
    return null;
  }

  return (
    <div className="space-y-6">
      {/* Download buttons */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Pobierz raporty</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Button
            onClick={() => onDownload('full-pdf')}
            disabled={isDownloading !== null}
            className="flex items-center justify-center gap-2"
          >
            <DocumentArrowDownIcon className="h-5 w-5" />
            {isDownloading === 'full-pdf' ? 'Pobieranie...' : 'Pełny raport (PDF)'}
          </Button>
          <Button
            variant="secondary"
            onClick={() => onDownload('admissions')}
            disabled={isDownloading !== null}
            className="flex items-center justify-center gap-2"
          >
            <DocumentArrowDownIcon className="h-5 w-5" />
            {isDownloading === 'admissions' ? 'Pobieranie...' : 'Przyjęcia (CSV)'}
          </Button>
          <Button
            variant="secondary"
            onClick={() => onDownload('adoptions')}
            disabled={isDownloading !== null}
            className="flex items-center justify-center gap-2"
          >
            <DocumentArrowDownIcon className="h-5 w-5" />
            {isDownloading === 'adoptions' ? 'Pobieranie...' : 'Adopcje (CSV)'}
          </Button>
          <Button
            variant="secondary"
            onClick={() => onDownload('volunteer-hours')}
            disabled={isDownloading !== null}
            className="flex items-center justify-center gap-2"
          >
            <DocumentArrowDownIcon className="h-5 w-5" />
            {isDownloading === 'volunteer-hours' ? 'Pobieranie...' : 'Godziny wolontariuszy (CSV)'}
          </Button>
        </div>
      </Card>

      {/* Summary */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Podsumowanie: {report.reportPeriod}
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Kategoria</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Ogółem</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Psy</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Koty</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Inne</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              <tr>
                <td className="px-4 py-3 text-sm font-medium text-gray-900">Przyjęcia</td>
                <td className="px-4 py-3 text-sm text-gray-900 text-right">{report.summary.totalAdmissions}</td>
                <td className="px-4 py-3 text-sm text-gray-600 text-right">{report.summary.admissionsDogs}</td>
                <td className="px-4 py-3 text-sm text-gray-600 text-right">{report.summary.admissionsCats}</td>
                <td className="px-4 py-3 text-sm text-gray-600 text-right">{report.summary.admissionsOther}</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-sm font-medium text-gray-900">Adopcje</td>
                <td className="px-4 py-3 text-sm text-gray-900 text-right">{report.summary.totalAdoptions}</td>
                <td className="px-4 py-3 text-sm text-gray-600 text-right">{report.summary.adoptionsDogs}</td>
                <td className="px-4 py-3 text-sm text-gray-600 text-right">{report.summary.adoptionsCats}</td>
                <td className="px-4 py-3 text-sm text-gray-600 text-right">{report.summary.adoptionsOther}</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-sm font-medium text-gray-900">Zgony</td>
                <td className="px-4 py-3 text-sm text-gray-900 text-right" colSpan={4}>{report.summary.totalDeceased}</td>
              </tr>
              <tr className="bg-gray-50">
                <td className="px-4 py-3 text-sm font-bold text-gray-900">Aktualna populacja</td>
                <td className="px-4 py-3 text-sm font-bold text-gray-900 text-right" colSpan={4}>{report.summary.currentPopulation}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </Card>

      {/* Admissions */}
      {report.admissions.length > 0 && (
        <Card className="p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Przyjęcia ({report.admissions.length})
          </h3>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Nr ewid.</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Imię</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Gatunek</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Rasa</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Data</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {report.admissions.slice(0, 10).map((admission) => (
                  <tr key={admission.registrationNumber}>
                    <td className="px-3 py-2 text-sm text-gray-900">{admission.registrationNumber}</td>
                    <td className="px-3 py-2 text-sm text-gray-900 font-medium">{admission.name}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">{admission.species}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">{admission.breed}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">
                      {new Date(admission.admissionDate).toLocaleDateString('pl-PL')}
                    </td>
                    <td className="px-3 py-2 text-sm text-gray-600">{admission.currentStatus}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {report.admissions.length > 10 && (
              <p className="text-sm text-gray-500 mt-2 text-center">
                ... i {report.admissions.length - 10} więcej. Pobierz CSV, aby zobaczyć wszystkie.
              </p>
            )}
          </div>
        </Card>
      )}

      {/* Adoptions */}
      {report.adoptions.length > 0 && (
        <Card className="p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Adopcje ({report.adoptions.length})
          </h3>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Nr ewid.</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Imię</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Gatunek</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Data adopcji</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Nr umowy</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Miasto</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {report.adoptions.slice(0, 10).map((adoption, index) => (
                  <tr key={`${adoption.registrationNumber}-${index}`}>
                    <td className="px-3 py-2 text-sm text-gray-900">{adoption.registrationNumber}</td>
                    <td className="px-3 py-2 text-sm text-gray-900 font-medium">{adoption.animalName}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">{adoption.species}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">
                      {new Date(adoption.adoptionDate).toLocaleDateString('pl-PL')}
                    </td>
                    <td className="px-3 py-2 text-sm text-gray-600">{adoption.contractNumber}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">{adoption.adopterCity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {report.adoptions.length > 10 && (
              <p className="text-sm text-gray-500 mt-2 text-center">
                ... i {report.adoptions.length - 10} więcej. Pobierz CSV, aby zobaczyć wszystkie.
              </p>
            )}
          </div>
        </Card>
      )}

      {/* Volunteer Hours */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Ewidencja godzin wolontariuszy
        </h3>
        <div className="grid grid-cols-3 gap-4 mb-6">
          <div className="text-center p-3 bg-gray-50 rounded-lg">
            <div className="text-2xl font-bold text-gray-900">{report.volunteerHours.totalVolunteers}</div>
            <div className="text-xs text-gray-500">Wolontariuszy</div>
          </div>
          <div className="text-center p-3 bg-primary-50 rounded-lg">
            <div className="text-2xl font-bold text-primary-600">{report.volunteerHours.totalHoursWorked.toFixed(1)}h</div>
            <div className="text-xs text-gray-500">Godzin łącznie</div>
          </div>
          <div className="text-center p-3 bg-gray-50 rounded-lg">
            <div className="text-2xl font-bold text-gray-900">{report.volunteerHours.totalDaysWorked}</div>
            <div className="text-xs text-gray-500">Dni pracy</div>
          </div>
        </div>

        {report.volunteerHours.records.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Wolontariusz</th>
                  <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                  <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Godziny</th>
                  <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Dni</th>
                  <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Obecności</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {report.volunteerHours.records.slice(0, 10).map((record, index) => (
                  <tr key={`${record.email}-${index}`}>
                    <td className="px-3 py-2 text-sm text-gray-900 font-medium">{record.volunteerName}</td>
                    <td className="px-3 py-2 text-sm text-gray-600">{record.email}</td>
                    <td className="px-3 py-2 text-sm text-primary-600 text-right font-medium">
                      {record.hoursWorked.toFixed(1)}h
                    </td>
                    <td className="px-3 py-2 text-sm text-gray-600 text-right">{record.daysWorked}</td>
                    <td className="px-3 py-2 text-sm text-gray-600 text-right">{record.attendanceCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {report.volunteerHours.records.length > 10 && (
              <p className="text-sm text-gray-500 mt-2 text-center">
                ... i {report.volunteerHours.records.length - 10} więcej. Pobierz CSV, aby zobaczyć wszystkie.
              </p>
            )}
          </div>
        )}
      </Card>
    </div>
  );
}

// Stat Card Component
function StatCard({
  icon: Icon,
  title,
  value,
  subtitle,
  color,
}: {
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  value: number;
  subtitle: string;
  color: 'green' | 'blue' | 'purple' | 'orange';
}) {
  const colorClasses = {
    green: 'bg-green-100 text-green-600',
    blue: 'bg-blue-100 text-blue-600',
    purple: 'bg-purple-100 text-purple-600',
    orange: 'bg-orange-100 text-orange-600',
  };

  return (
    <Card className="p-4">
      <div className="flex items-center gap-4">
        <div className={`p-3 rounded-lg ${colorClasses[color]}`}>
          <Icon className="h-6 w-6" />
        </div>
        <div>
          <p className="text-sm text-gray-500">{title}</p>
          <p className="text-2xl font-bold text-gray-900">{value}</p>
          <p className="text-xs text-gray-400">{subtitle}</p>
        </div>
      </div>
    </Card>
  );
}
