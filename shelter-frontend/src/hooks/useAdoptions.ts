import { useState, useEffect, useCallback } from 'react';
import { adoptionsApi, GetMyAdoptionsParams } from '@/api';
import {
  AdoptionApplicationListItem,
  AdoptionApplicationDetail,
  AdoptionApplicationStatus,
} from '@/types';

interface UseMyAdoptionsReturn {
  applications: AdoptionApplicationListItem[];
  total: number;
  isLoading: boolean;
  error: string | null;
  page: number;
  pageSize: number;
  totalPages: number;
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  statusFilter: AdoptionApplicationStatus | undefined;
  setStatusFilter: (status: AdoptionApplicationStatus | undefined) => void;
  refetch: () => void;
}

export function useMyAdoptions(initialPageSize: number = 10): UseMyAdoptionsReturn {
  const [applications, setApplications] = useState<AdoptionApplicationListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [statusFilter, setStatusFilter] = useState<AdoptionApplicationStatus | undefined>(undefined);

  const fetchApplications = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const params: GetMyAdoptionsParams = {
        page,
        pageSize,
        status: statusFilter,
      };

      const result = await adoptionsApi.getMyApplications(params);
      setApplications(result.items);
      setTotal(result.totalCount);
    } catch (err) {
      console.error('Failed to fetch applications:', err);
      setError('Nie udało się pobrać listy zgłoszeń');
      setApplications([]);
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, statusFilter]);

  useEffect(() => {
    fetchApplications();
  }, [fetchApplications]);

  const totalPages = Math.ceil(total / pageSize);

  return {
    applications,
    total,
    isLoading,
    error,
    page,
    pageSize,
    totalPages,
    setPage,
    setPageSize,
    statusFilter,
    setStatusFilter,
    refetch: fetchApplications,
  };
}

interface UseAdoptionDetailReturn {
  application: AdoptionApplicationDetail | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useAdoptionDetail(id: string | undefined): UseAdoptionDetailReturn {
  const [application, setApplication] = useState<AdoptionApplicationDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchApplication = useCallback(async () => {
    if (!id) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const result = await adoptionsApi.getApplication(id);
      setApplication(result);
    } catch (err) {
      console.error('Failed to fetch application:', err);
      setError('Nie udało się pobrać szczegółów zgłoszenia');
      setApplication(null);
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchApplication();
  }, [fetchApplication]);

  return {
    application,
    isLoading,
    error,
    refetch: fetchApplication,
  };
}
