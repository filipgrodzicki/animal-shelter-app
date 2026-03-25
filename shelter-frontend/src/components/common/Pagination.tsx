import { ChevronLeftIcon, ChevronRightIcon } from '@heroicons/react/24/outline';
import { clsx } from 'clsx';
import { usePagination } from '@/hooks';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  siblingCount?: number;
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  siblingCount = 1,
}: PaginationProps) {
  const paginationRange = usePagination({ totalPages, currentPage, siblingCount });

  if (totalPages <= 1) {
    return null;
  }

  const onPrevious = () => {
    if (currentPage > 1) {
      onPageChange(currentPage - 1);
    }
  };

  const onNext = () => {
    if (currentPage < totalPages) {
      onPageChange(currentPage + 1);
    }
  };

  return (
    <nav className="inline-flex items-center gap-2 rounded-2xl bg-white border border-gray-100 p-2 shadow-sm" aria-label="Pagination">
      <button
        onClick={onPrevious}
        disabled={currentPage === 1}
        className={clsx(
          'p-2 rounded-xl transition-colors',
          currentPage === 1
            ? 'text-gray-300 cursor-not-allowed'
            : 'text-gray-500 hover:bg-gray-100 hover:text-gray-700'
        )}
        aria-label="Poprzednia strona"
      >
        <ChevronLeftIcon className="h-5 w-5" />
      </button>

      {paginationRange.map((pageNumber, index) => {
        if (pageNumber === 'dots') {
          return (
            <span key={`dots-${index}`} className="px-2 py-2 text-gray-400">
              ...
            </span>
          );
        }

        return (
          <button
            key={pageNumber}
            onClick={() => onPageChange(pageNumber as number)}
            className={clsx(
              'min-w-[40px] px-3 py-2 rounded-xl text-sm font-medium transition-colors',
              pageNumber === currentPage
                ? 'bg-primary-600 text-white'
                : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
            )}
            aria-current={pageNumber === currentPage ? 'page' : undefined}
          >
            {pageNumber}
          </button>
        );
      })}

      <button
        onClick={onNext}
        disabled={currentPage === totalPages}
        className={clsx(
          'p-2 rounded-xl transition-colors',
          currentPage === totalPages
            ? 'text-gray-300 cursor-not-allowed'
            : 'text-gray-500 hover:bg-gray-100 hover:text-gray-700'
        )}
        aria-label="Następna strona"
      >
        <ChevronRightIcon className="h-5 w-5" />
      </button>
    </nav>
  );
}
