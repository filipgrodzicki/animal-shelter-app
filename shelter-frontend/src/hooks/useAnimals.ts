import { useState, useEffect, useCallback } from 'react';
import { animalsApi, GetAnimalsParams } from '@/api';
import { AnimalListItem, AnimalDetail, PagedResult, AnimalFilters } from '@/types';
import toast from 'react-hot-toast';
import { getErrorMessage } from '@/api';

export type SortOption = 'AdmissionDate' | 'Name' | 'Age';

interface UseAnimalsOptions {
  initialFilters?: AnimalFilters;
  initialPage?: number;
  pageSize?: number;
  publicOnly?: boolean;
  initialSortBy?: SortOption;
  initialSortDescending?: boolean;
}

interface UseAnimalsReturn {
  animals: AnimalListItem[];
  totalCount: number;
  totalPages: number;
  page: number;
  filters: AnimalFilters;
  sortBy: SortOption;
  sortDescending: boolean;
  isLoading: boolean;
  error: string | null;
  updateFilters: (newFilters: Partial<AnimalFilters>) => void;
  clearFilters: () => void;
  goToPage: (page: number) => void;
  setSort: (sortBy: SortOption, descending?: boolean) => void;
  refetch: () => Promise<void>;
}

export function useAnimals(options: UseAnimalsOptions = {}): UseAnimalsReturn {
  const {
    initialFilters = {},
    initialPage = 1,
    pageSize = 12,
    publicOnly = true,
    initialSortBy = 'AdmissionDate',
    initialSortDescending = true,
  } = options;

  const [animals, setAnimals] = useState<AnimalListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [page, setPage] = useState(initialPage);
  const [filters, setFilters] = useState<AnimalFilters>(initialFilters);
  const [sortBy, setSortBy] = useState<SortOption>(initialSortBy);
  const [sortDescending, setSortDescending] = useState(initialSortDescending);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchAnimals = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const params: GetAnimalsParams = {
        ...filters,
        page,
        pageSize,
        publicOnly,
        sortBy,
        sortDescending,
      };

      const result: PagedResult<AnimalListItem> = publicOnly
        ? await animalsApi.getAvailableAnimals(params)
        : await animalsApi.getAnimals(params);

      setAnimals(result.items);
      setTotalCount(result.totalCount);
      setTotalPages(result.totalPages);
    } catch (err) {
      const message = getErrorMessage(err);
      setError(message);
      // Don't show toast on every error - let the UI handle it
      console.error('Failed to fetch animals:', message);
    } finally {
      setIsLoading(false);
    }
  }, [filters, page, pageSize, publicOnly, sortBy, sortDescending]);

  useEffect(() => {
    fetchAnimals();
  }, [fetchAnimals]);

  const updateFilters = useCallback((newFilters: Partial<AnimalFilters>) => {
    setFilters(prev => ({ ...prev, ...newFilters }));
    setPage(1);
  }, []);

  const clearFilters = useCallback(() => {
    setFilters({});
    setPage(1);
  }, []);

  const goToPage = useCallback((newPage: number) => {
    setPage(newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, []);

  const setSort = useCallback((newSortBy: SortOption, descending?: boolean) => {
    setSortBy(newSortBy);
    if (descending !== undefined) {
      setSortDescending(descending);
    } else {
      // Toggle direction if same sort field
      if (newSortBy === sortBy) {
        setSortDescending(prev => !prev);
      } else {
        // Default directions for each sort type
        setSortDescending(newSortBy === 'AdmissionDate' ? true : false);
      }
    }
    setPage(1);
  }, [sortBy]);

  return {
    animals,
    totalCount,
    totalPages,
    page,
    filters,
    sortBy,
    sortDescending,
    isLoading,
    error,
    updateFilters,
    clearFilters,
    goToPage,
    setSort,
    refetch: fetchAnimals,
  };
}

export function useAnimal(id: string | undefined) {
  const [animal, setAnimal] = useState<AnimalDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchAnimal = useCallback(async () => {
    if (!id) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const result = await animalsApi.getAnimal(id);
      setAnimal(result);
    } catch (err) {
      const message = getErrorMessage(err);
      setError(message);
      toast.error('Nie udało się załadować danych zwierzęcia');
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchAnimal();
  }, [fetchAnimal]);

  return {
    animal,
    isLoading,
    error,
    refetch: fetchAnimal,
  };
}
