import { useState } from 'react';
import toast from 'react-hot-toast';
import { animalsApi } from '@/api/animals';
import { AnimalListItem, AnimalNoteType, getAnimalNoteTypeLabel } from '@/types';

interface AddAnimalNoteModalProps {
  animal: AnimalListItem;
  volunteerId: string;
  onClose: () => void;
  onSuccess: () => void;
}

const NOTE_TYPES: AnimalNoteType[] = [
  'BehaviorObservation',
  'HealthObservation',
  'Feeding',
  'WalkActivity',
  'AnimalInteraction',
  'HumanInteraction',
  'Grooming',
  'Training',
  'General',
  'Urgent',
];

export function AddAnimalNoteModal({
  animal,
  volunteerId,
  onClose,
  onSuccess,
}: AddAnimalNoteModalProps) {
  const [noteType, setNoteType] = useState<AnimalNoteType>('General');
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [isImportant, setIsImportant] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!title.trim() || !content.trim()) {
      toast.error('Wypelnij wszystkie wymagane pola');
      return;
    }

    setIsSubmitting(true);
    try {
      await animalsApi.addAnimalNote(animal.id, {
        volunteerId,
        noteType,
        title: title.trim(),
        content: content.trim(),
        isImportant,
        observationDate: new Date().toISOString(),
      });
      toast.success('Notatka zostala dodana');
      onSuccess();
    } catch (err) {
      console.error('Error adding note:', err);
      toast.error('Blad podczas dodawania notatki');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900">
              Dodaj notatke - {animal.name}
            </h3>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600"
              disabled={isSubmitting}
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Note type */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Typ notatki *
              </label>
              <select
                value={noteType}
                onChange={(e) => setNoteType(e.target.value as AnimalNoteType)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                disabled={isSubmitting}
              >
                {NOTE_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {getAnimalNoteTypeLabel(type)}
                  </option>
                ))}
              </select>
            </div>

            {/* Title */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tytul *</label>
              <input
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="Krotki tytul notatki"
                maxLength={100}
                disabled={isSubmitting}
              />
            </div>

            {/* Content */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tresc *</label>
              <textarea
                value={content}
                onChange={(e) => setContent(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                rows={5}
                placeholder="Opisz swoje obserwacje..."
                disabled={isSubmitting}
              />
            </div>

            {/* Important flag */}
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isImportant"
                checked={isImportant}
                onChange={(e) => setIsImportant(e.target.checked)}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                disabled={isSubmitting}
              />
              <label htmlFor="isImportant" className="text-sm text-gray-700">
                Oznacz jako wazne
              </label>
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-3 pt-4">
              <button
                type="button"
                onClick={onClose}
                className="btn btn-secondary"
                disabled={isSubmitting}
              >
                Anuluj
              </button>
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? 'Zapisywanie...' : 'Dodaj notatke'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
