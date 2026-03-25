import { useState, useEffect } from 'react';
import { animalsApi } from '@/api/animals';
import { AnimalNote, getAnimalNoteTypeLabel, getAnimalNoteTypeColor } from '@/types';

interface AnimalNotesPanelProps {
  animalId: string;
}

export function AnimalNotesPanel({ animalId }: AnimalNotesPanelProps) {
  const [notes, setNotes] = useState<AnimalNote[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadNotes();
  }, [animalId]);

  const loadNotes = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await animalsApi.getAnimalNotes(animalId, { pageSize: 10 });
      setNotes(result.items);
    } catch (err) {
      console.error('Error loading notes:', err);
      setError('Nie udalo sie zaladowac notatek');
    } finally {
      setIsLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('pl-PL', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getColorClasses = (color: string) => {
    const colorMap: Record<string, string> = {
      blue: 'bg-blue-100 text-blue-800',
      red: 'bg-red-100 text-red-800',
      green: 'bg-green-100 text-green-800',
      purple: 'bg-purple-100 text-purple-800',
      yellow: 'bg-yellow-100 text-yellow-800',
      cyan: 'bg-cyan-100 text-cyan-800',
      pink: 'bg-pink-100 text-pink-800',
      orange: 'bg-orange-100 text-orange-800',
      gray: 'bg-gray-100 text-gray-800',
    };
    return colorMap[color] || 'bg-gray-100 text-gray-800';
  };

  if (isLoading) {
    return (
      <div className="p-4 text-center">
        <div className="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary-600 mx-auto" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 text-center text-red-600">
        <p>{error}</p>
      </div>
    );
  }

  if (notes.length === 0) {
    return (
      <div className="p-4 text-center text-gray-500">
        <svg
          className="w-8 h-8 mx-auto mb-2 text-gray-300"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <p className="text-sm">Brak notatek dla tego zwierzecia</p>
      </div>
    );
  }

  return (
    <div className="p-4 space-y-3 max-h-96 overflow-y-auto">
      <h4 className="text-sm font-medium text-gray-700 mb-2">
        Ostatnie notatki ({notes.length})
      </h4>
      {notes.map((note) => (
        <div
          key={note.id}
          className={`p-3 rounded-lg border ${
            note.isImportant ? 'border-red-300 bg-red-50' : 'border-gray-200 bg-gray-50'
          }`}
        >
          <div className="flex items-start justify-between mb-2">
            <div className="flex items-center gap-2">
              <span
                className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getColorClasses(
                  getAnimalNoteTypeColor(note.noteType)
                )}`}
              >
                {getAnimalNoteTypeLabel(note.noteType)}
              </span>
              {note.isImportant && (
                <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800">
                  Wazne
                </span>
              )}
            </div>
          </div>
          <h5 className="font-medium text-gray-900 text-sm mb-1">{note.title}</h5>
          <p className="text-sm text-gray-600 mb-2">{note.content}</p>
          <div className="flex items-center justify-between text-xs text-gray-500">
            <span>{note.authorName}</span>
            <span>{formatDate(note.observationDate)}</span>
          </div>
        </div>
      ))}
    </div>
  );
}
