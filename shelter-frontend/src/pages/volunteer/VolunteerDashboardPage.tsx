import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import { volunteersApi, attendanceApi } from '@/api/volunteers';
import { animalsApi } from '@/api/animals';
import {
  VolunteerDetail,
  Attendance,
  AttendanceListItem,
  AnimalListItem,
  formatHours,
  getVolunteerStatusLabel,
  getVolunteerStatusColor,
} from '@/types';
import { TimeTracker } from './components/TimeTracker';
import { AttendanceHistory } from './components/AttendanceHistory';
import { VolunteerAnimalList } from './components/VolunteerAnimalList';
import { VolunteerSchedule } from './components/VolunteerSchedule';

export function VolunteerDashboardPage() {
  const [volunteer, setVolunteer] = useState<VolunteerDetail | null>(null);
  const [currentAttendance, setCurrentAttendance] = useState<Attendance | null>(null);
  const [attendanceHistory, setAttendanceHistory] = useState<AttendanceListItem[]>([]);
  const [animals, setAnimals] = useState<AnimalListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'time' | 'schedule' | 'animals'>('time');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      // Load volunteer data
      const volunteerData = await volunteersApi.getMyVolunteer();
      setVolunteer(volunteerData);

      // Load current attendance
      try {
        const attendance = await attendanceApi.getCurrentAttendance(volunteerData.id);
        setCurrentAttendance(attendance);
      } catch {
        // No active attendance - that's okay
        setCurrentAttendance(null);
      }

      // Load attendance history
      const historyResult = await attendanceApi.getVolunteerAttendances(volunteerData.id, {
        pageSize: 10,
      });
      setAttendanceHistory(historyResult.items);

      // Load available animals
      const animalsResult = await animalsApi.getAnimals({
        status: 'Available',
        page: 1,
        pageSize: 20,
      });
      setAnimals(animalsResult.items);
    } catch (err) {
      console.error('Error loading volunteer data:', err);
      setError('Nie udalo sie zaladowac danych wolontariusza');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCheckIn = async () => {
    if (!volunteer) return;
    try {
      await attendanceApi.checkIn({ volunteerId: volunteer.id });
      toast.success('Zameldowano pomyslnie!');
      loadData();
    } catch (err) {
      console.error('Error checking in:', err);
      toast.error('Blad podczas meldowania');
    }
  };

  const handleCheckOut = async (workDescription?: string) => {
    if (!currentAttendance) return;
    try {
      await attendanceApi.checkOut({
        attendanceId: currentAttendance.id,
        workDescription,
      });
      toast.success('Wymeldowano pomyslnie!');
      loadData();
    } catch (err) {
      console.error('Error checking out:', err);
      toast.error('Blad podczas wymeldowania');
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary-600" />
      </div>
    );
  }

  if (error || !volunteer) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
          <h2 className="text-xl font-semibold text-red-800 mb-2">
            {error || 'Nie znaleziono danych wolontariusza'}
          </h2>
          <p className="text-red-600 mb-4">
            Upewnij sie, ze jestes zarejestrowany jako wolontariusz.
          </p>
          <Link to="/volunteer" className="btn btn-primary">
            Zostac wolontariuszem
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="bg-white rounded-lg shadow-sm p-6 mb-6">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              Witaj, {volunteer.firstName}!
            </h1>
            <p className="text-gray-600 mt-1">Panel wolontariusza</p>
          </div>
          <div className="mt-4 md:mt-0 flex items-center gap-4">
            <span
              className={`px-3 py-1 rounded-full text-sm font-medium ${getVolunteerStatusColor(
                volunteer.status
              ).replace('badge-', 'bg-').replace('green', 'green-100 text-green-800').replace('yellow', 'yellow-100 text-yellow-800').replace('blue', 'blue-100 text-blue-800')}`}
            >
              {getVolunteerStatusLabel(volunteer.status)}
            </span>
            <div className="text-right">
              <p className="text-sm text-gray-500">Przepracowane godziny</p>
              <p className="text-xl font-semibold text-primary-600">
                {formatHours(volunteer.totalHoursWorked)}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab('time')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'time'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Ewidencja czasu
          </button>
          <button
            onClick={() => setActiveTab('schedule')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'schedule'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Grafik dyzurow
          </button>
          <button
            onClick={() => setActiveTab('animals')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'animals'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Zwierzeta
          </button>
        </nav>
      </div>

      {/* Tab content */}
      {activeTab === 'time' ? (
        <div className="space-y-6">
          <TimeTracker
            currentAttendance={currentAttendance}
            onCheckIn={handleCheckIn}
            onCheckOut={handleCheckOut}
          />
          <AttendanceHistory attendances={attendanceHistory} />
        </div>
      ) : activeTab === 'schedule' ? (
        <VolunteerSchedule volunteerId={volunteer.id} />
      ) : (
        <VolunteerAnimalList animals={animals} volunteerId={volunteer.id} />
      )}
    </div>
  );
}
