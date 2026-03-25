import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { PageContainer, PageHeader } from '@/components/layout';
import { AnimalGrid, AnimalFilters, AnimalFiltersState } from '@/components/animals';
import { Pagination } from '@/components/common';
import { useAnimals, SortOption } from '@/hooks';
import { Species, Gender, Size, AnimalStatus } from '@/types';

export function AnimalsPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  // Initialize filters from URL
  const [filters, setFilters] = useState<AnimalFiltersState>(() => ({
    searchTerm: searchParams.get('searchTerm') || undefined,
    species: (searchParams.get('species') as Species) || undefined,
    gender: (searchParams.get('gender') as Gender) || undefined,
    size: (searchParams.get('size') as Size) || undefined,
    status: (searchParams.get('status') as AnimalStatus) || undefined,
    ageMin: searchParams.get('ageMin') ? Number(searchParams.get('ageMin')) : undefined,
    ageMax: searchParams.get('ageMax') ? Number(searchParams.get('ageMax')) : undefined,
  }));

  // Initialize sort from URL
  const initialSortBy = (searchParams.get('sortBy') as SortOption) || 'AdmissionDate';
  const initialSortDesc = searchParams.get('sortDesc') !== 'false';

  const {
    animals,
    totalCount,
    totalPages,
    page,
    sortBy,
    sortDescending,
    isLoading,
    error,
    goToPage,
    setSort,
    refetch,
    updateFilters: hookUpdateFilters,
    clearFilters: hookClearFilters,
  } = useAnimals({
    initialFilters: filters,
    pageSize: 12,
    publicOnly: true,
    initialSortBy,
    initialSortDescending: initialSortDesc,
  });

  // Sync filters and sort with URL
  useEffect(() => {
    const params = new URLSearchParams();

    // Add filters to URL
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== '') {
        params.set(key, String(value));
      }
    });

    // Add sort to URL
    if (sortBy !== 'AdmissionDate') {
      params.set('sortBy', sortBy);
    }
    if (!sortDescending) {
      params.set('sortDesc', 'false');
    }

    // Add page to URL if not first page
    if (page > 1) {
      params.set('page', String(page));
    }

    setSearchParams(params, { replace: true });
  }, [filters, sortBy, sortDescending, page, setSearchParams]);

  const handleFiltersChange = (newFilters: AnimalFiltersState) => {
    setFilters(newFilters);
    hookUpdateFilters(newFilters);
  };

  const handleClearFilters = () => {
    setFilters({});
    hookClearFilters();
  };

  const handleSortChange = (newSortBy: SortOption, descending: boolean) => {
    setSort(newSortBy, descending);
  };

  // Generate description based on filters and count
  const getDescription = () => {
    if (isLoading) return 'Ładowanie...';
    if (error) return 'Wystąpił błąd podczas ładowania';

    const parts: string[] = [];

    if (filters.species) {
      const speciesNames: Record<string, string> = {
        Dog: 'psów',
        Cat: 'kotów',
        Rabbit: 'królików',
        Hamster: 'chomików',
        GuineaPig: 'świnek morskich',
        Bird: 'ptaków',
        Reptile: 'gadów',
        Other: 'innych zwierząt',
      };
      parts.push(speciesNames[filters.species] || 'zwierząt');
    }

    if (totalCount === 0) {
      return 'Nie znaleziono zwierząt spełniających wybrane kryteria';
    }

    const animalWord = totalCount === 1 ? 'zwierzę' :
                       totalCount < 5 ? 'zwierzęta' : 'zwierząt';

    if (parts.length > 0) {
      return `Znaleziono ${totalCount} ${parts.join(', ')}`;
    }

    return `${totalCount} ${animalWord} czeka na adopcję`;
  };

  return (
    <PageContainer>
      <PageHeader
        title="Zwierzęta do adopcji"
        description={getDescription()}
      />

      {/* Filters */}
      <div className="mb-8">
        <AnimalFilters
          filters={filters}
          onFiltersChange={handleFiltersChange}
          onClear={handleClearFilters}
          sortBy={sortBy}
          sortDescending={sortDescending}
          onSortChange={handleSortChange}
          showSorting={true}
        />
      </div>

      {/* Results info */}
      {!isLoading && !error && animals.length > 0 && (
        <div className="mb-4 flex items-center justify-between text-sm text-gray-600">
          <span>
            Wyświetlanie {((page - 1) * 12) + 1}-{Math.min(page * 12, totalCount)} z {totalCount}
          </span>
          {totalPages > 1 && (
            <span>Strona {page} z {totalPages}</span>
          )}
        </div>
      )}

      {/* Animal grid */}
      <AnimalGrid
        animals={animals}
        isLoading={isLoading}
        error={error}
        onRetry={refetch}
        emptyMessage="Brak zwierząt"
        emptyDescription="Nie znaleziono zwierząt spełniających wybrane kryteria. Spróbuj zmienić filtry lub wyczyść je, aby zobaczyć wszystkie dostępne zwierzęta."
      />

      {/* Pagination */}
      {!isLoading && !error && totalPages > 1 && (
        <div className="mt-8 flex justify-center">
          <Pagination
            currentPage={page}
            totalPages={totalPages}
            onPageChange={goToPage}
          />
        </div>
      )}
    </PageContainer>
  );
}
