import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  PlusIcon,
  PencilIcon,
  EyeIcon,
  DocumentTextIcon,
  ArrowPathIcon,
  ClockIcon,
  HeartIcon,
  ArchiveBoxIcon,
  ClipboardDocumentListIcon,
  NewspaperIcon,
  BellIcon,
  UserGroupIcon,
  Cog6ToothIcon,
  ChartBarIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Badge, Input, Select, Pagination, Spinner } from '@/components/common';
import { animalsApi } from '@/api/animals';
import {
  AnimalListItem,
  PagedResult,
  getSpeciesLabel,
  getGenderLabel,
  getStatusLabel,
  formatAge,
  AnimalStatus,
  Species,
  SelectOption,
  isAdmin,
} from '@/types';
import { useAuth } from '@/context/AuthContext';
import { AnimalFormModal } from './components/AnimalFormModal';
import { StatusChangeModal } from './components/StatusChangeModal';
import { AnimalDetailsModal } from './components/AnimalDetailsModal';

const speciesOptions: SelectOption[] = [
  { value: '', label: 'Wszystkie gatunki' },
  { value: 'Dog', label: 'Pies' },
  { value: 'Cat', label: 'Kot' },
];

const statusOptions: SelectOption[] = [
  { value: '', label: 'Wszystkie statusy' },
  { value: 'Quarantine', label: 'Kwarantanna' },
  { value: 'Treatment', label: 'Leczenie' },
  { value: 'Available', label: 'Dostępny' },
  { value: 'Reserved', label: 'Zarezerwowany' },
  { value: 'InAdoptionProcess', label: 'W procesie adopcji' },
  { value: 'Adopted', label: 'Adoptowany' },
  { value: 'Deceased', label: 'Zmarły' },
];

function getStatusBadgeVariant(status: AnimalStatus): 'green' | 'yellow' | 'red' | 'blue' | 'gray' | 'orange' {
  const variants: Record<string, 'green' | 'yellow' | 'red' | 'blue' | 'gray' | 'orange'> = {
    Admitted: 'gray',
    Quarantine: 'yellow',
    Treatment: 'orange',
    Available: 'green',
    Reserved: 'blue',
    InAdoptionProcess: 'blue',
    Adopted: 'green',
    Deceased: 'gray',
  };
  return variants[status] || 'gray';
}

export function AdminPage() {
  const { user } = useAuth();
  const [animals, setAnimals] = useState<PagedResult<AnimalListItem> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [searchTerm, setSearchTerm] = useState('');
  const [speciesFilter, setSpeciesFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  // Modal states
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [selectedAnimalId, setSelectedAnimalId] = useState<string | null>(null);
  const [editingAnimal, setEditingAnimal] = useState<AnimalListItem | null>(null);

  const fetchAnimals = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await animalsApi.getAnimals({
        page,
        pageSize: 10,
        searchTerm: searchTerm || undefined,
        species: (speciesFilter as Species) || undefined,
        status: (statusFilter as AnimalStatus) || undefined,
      });
      setAnimals(result);
    } catch (err) {
      setError('Nie udało się pobrać listy zwierząt');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchAnimals();
  }, [page, speciesFilter, statusFilter]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setPage(1);
      fetchAnimals();
    }, 300);
    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  const handleOpenForm = (animal?: AnimalListItem) => {
    setEditingAnimal(animal || null);
    setIsFormModalOpen(true);
  };

  const handleOpenStatusChange = (animalId: string) => {
    setSelectedAnimalId(animalId);
    setIsStatusModalOpen(true);
  };

  const handleOpenDetails = (animalId: string) => {
    setSelectedAnimalId(animalId);
    setIsDetailsModalOpen(true);
  };

  const handleFormSuccess = () => {
    setIsFormModalOpen(false);
    setEditingAnimal(null);
    fetchAnimals();
  };

  const handleStatusSuccess = () => {
    setIsStatusModalOpen(false);
    setSelectedAnimalId(null);
    fetchAnimals();
  };

  // Calculate statistics
  const stats = animals ? {
    total: animals.totalCount,
    available: animals.items.filter(a => a.status === 'Available').length,
    inProcess: animals.items.filter(a => a.status === 'InAdoptionProcess' || a.status === 'Reserved').length,
    quarantine: animals.items.filter(a => a.status === 'Quarantine' || a.status === 'Treatment').length,
  } : { total: 0, available: 0, inProcess: 0, quarantine: 0 };

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Panel administracyjny</h1>
        <p className="mt-2 text-gray-600">Zarządzaj zwierzętami w schronisku</p>
      </div>

      {/* Quick stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary-100 rounded-lg">
              <HeartIcon className="h-6 w-6 text-primary-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stats.total}</p>
              <p className="text-sm text-gray-500">Wszystkie zwierzęta</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-green-100 rounded-lg">
              <EyeIcon className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stats.available}</p>
              <p className="text-sm text-gray-500">Dostępne do adopcji</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <ClockIcon className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stats.inProcess}</p>
              <p className="text-sm text-gray-500">W procesie adopcji</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-yellow-100 rounded-lg">
              <DocumentTextIcon className="h-6 w-6 text-yellow-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stats.quarantine}</p>
              <p className="text-sm text-gray-500">Kwarantanna / Leczenie</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Navigation buttons */}
      <div className="flex flex-wrap gap-2 mb-4">
        <Button onClick={() => handleOpenForm()} leftIcon={<PlusIcon className="h-5 w-5" />}>
          Zarejestruj zwierzę
        </Button>
        <Button
          as={Link}
          to="/admin/adoptions"
          variant="outline"
          leftIcon={<ClipboardDocumentListIcon className="h-5 w-5" />}
        >
          Wnioski adopcyjne
        </Button>
        <Button
          as={Link}
          to="/admin/cms"
          variant="outline"
          leftIcon={<NewspaperIcon className="h-5 w-5" />}
        >
          CMS
        </Button>
        <Button
          as={Link}
          to="/admin/notifications"
          variant="outline"
          leftIcon={<BellIcon className="h-5 w-5" />}
        >
          Powiadomienia
        </Button>
        <Button
          as={Link}
          to="/admin/volunteers"
          variant="outline"
          leftIcon={<UserGroupIcon className="h-5 w-5" />}
        >
          Wolontariusze
        </Button>
        <Button
          as={Link}
          to="/admin/reports"
          variant="outline"
          leftIcon={<ChartBarIcon className="h-5 w-5" />}
        >
          Raporty
        </Button>
        {isAdmin(user) && (
          <Button
            as={Link}
            to="/admin/settings"
            variant="outline"
            leftIcon={<Cog6ToothIcon className="h-5 w-5" />}
            className="text-red-600 border-red-300 hover:bg-red-50"
          >
            Panel Administratora
          </Button>
        )}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <Input
          placeholder="Szukaj po imieniu lub numerze..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          wrapperClassName="w-64"
        />
        <Select
          options={speciesOptions}
          value={speciesFilter}
          onChange={(e) => setSpeciesFilter(e.target.value)}
          wrapperClassName="w-52"
        />
        <Select
          options={statusOptions}
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          wrapperClassName="w-52"
        />
        <Button
          variant={statusFilter === 'Deceased' ? 'primary' : 'outline'}
          onClick={() => setStatusFilter(statusFilter === 'Deceased' ? '' : 'Deceased')}
          leftIcon={<ArchiveBoxIcon className="h-5 w-5" />}
        >
          Zmarłe
        </Button>
      </div>

      {/* Animals table */}
      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : !animals || animals.items.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            Nie znaleziono zwierząt
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Zwierzę
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Numer ewidencyjny
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Gatunek
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Wiek
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Data przyjęcia
                    </th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Akcje
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {animals.items.map((animal) => (
                    <tr key={animal.id} className="hover:bg-gray-50">
                      <td className="px-4 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-3">
                          {animal.mainPhotoUrl ? (
                            <img
                              src={animal.mainPhotoUrl}
                              alt={animal.name}
                              className="h-10 w-10 rounded-full object-cover"
                            />
                          ) : (
                            <div className="h-10 w-10 rounded-full bg-gray-200 flex items-center justify-center">
                              <span className="text-gray-500 text-xs">
                                {animal.name.charAt(0)}
                              </span>
                            </div>
                          )}
                          <div>
                            <p className="font-medium text-gray-900">{animal.name}</p>
                            <p className="text-sm text-gray-500">
                              {animal.breed} • {getGenderLabel(animal.gender)}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                        {animal.registrationNumber}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-900">
                        {getSpeciesLabel(animal.species)}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                        {formatAge(animal.ageInMonths)}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap">
                        <Badge variant={getStatusBadgeVariant(animal.status)}>
                          {getStatusLabel(animal.status)}
                        </Badge>
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                        {new Date(animal.admissionDate).toLocaleDateString('pl-PL')}
                      </td>
                      <td className="px-4 py-4 whitespace-nowrap text-right">
                        <div className="flex justify-end gap-2">
                          <button
                            onClick={() => handleOpenDetails(animal.id)}
                            className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                            title="Szczegóły"
                          >
                            <EyeIcon className="h-5 w-5" />
                          </button>
                          <button
                            onClick={() => handleOpenForm(animal)}
                            className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                            title="Edytuj"
                          >
                            <PencilIcon className="h-5 w-5" />
                          </button>
                          <button
                            onClick={() => handleOpenStatusChange(animal.id)}
                            className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                            title="Zmień status"
                          >
                            <ArrowPathIcon className="h-5 w-5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {animals.totalPages > 1 && (
              <div className="px-4 py-3 border-t border-gray-200">
                <Pagination
                  currentPage={page}
                  totalPages={animals.totalPages}
                  onPageChange={setPage}
                />
              </div>
            )}
          </>
        )}
      </Card>

      {/* Modals */}
      <AnimalFormModal
        isOpen={isFormModalOpen}
        onClose={() => {
          setIsFormModalOpen(false);
          setEditingAnimal(null);
        }}
        onSuccess={handleFormSuccess}
        animal={editingAnimal}
      />

      {selectedAnimalId && (
        <>
          <StatusChangeModal
            isOpen={isStatusModalOpen}
            onClose={() => {
              setIsStatusModalOpen(false);
              setSelectedAnimalId(null);
            }}
            onSuccess={handleStatusSuccess}
            animalId={selectedAnimalId}
          />
          <AnimalDetailsModal
            isOpen={isDetailsModalOpen}
            onClose={() => {
              setIsDetailsModalOpen(false);
              setSelectedAnimalId(null);
            }}
            animalId={selectedAnimalId}
          />
        </>
      )}
    </PageContainer>
  );
}
