import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import {
  AnimalListItem,
  getSpeciesLabel,
  getGenderLabel,
  getSizeLabel,
  formatAge,
} from '@/types';
import { AddAnimalNoteModal } from './AddAnimalNoteModal';
import { AnimalNotesPanel } from './AnimalNotesPanel';

interface VolunteerAnimalListProps {
  animals: AnimalListItem[];
  volunteerId: string;
}

export function VolunteerAnimalList({ animals, volunteerId }: VolunteerAnimalListProps) {
  const [selectedAnimalId, setSelectedAnimalId] = useState<string | null>(null);
  const [showAddNoteModal, setShowAddNoteModal] = useState(false);
  const [animalForNote, setAnimalForNote] = useState<AnimalListItem | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  // Filtrowanie zwierzat po imieniu
  const filteredAnimals = useMemo(() => {
    if (!searchTerm.trim()) return animals;
    const term = searchTerm.toLowerCase().trim();
    return animals.filter((animal) => animal.name.toLowerCase().includes(term));
  }, [animals, searchTerm]);

  const handleAddNote = (animal: AnimalListItem) => {
    setAnimalForNote(animal);
    setShowAddNoteModal(true);
  };

  const handleViewNotes = (animalId: string) => {
    setSelectedAnimalId(selectedAnimalId === animalId ? null : animalId);
  };

  if (animals.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-sm p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Zwierzeta w schronisku</h2>
        <div className="text-center py-8 text-gray-500">
          <svg
            className="w-12 h-12 mx-auto mb-4 text-gray-300"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <p>Brak dostepnych zwierzat</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-lg shadow-sm p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-4">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">
              Zwierzeta w schronisku ({filteredAnimals.length})
            </h2>
            <p className="text-sm text-gray-600">
              Przegladaj zwierzeta i dodawaj notatki o ich zachowaniu, zdrowiu lub innych obserwacjach.
            </p>
          </div>
          <div className="w-full sm:w-64">
            <input
              type="text"
              placeholder="Szukaj po imieniu..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
        </div>
      </div>

      {filteredAnimals.length === 0 ? (
        <div className="bg-white rounded-lg shadow-sm p-8 text-center text-gray-500">
          <p>Nie znaleziono zwierzat o imieniu "{searchTerm}"</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredAnimals.map((animal) => (
          <div key={animal.id} className="bg-white rounded-lg shadow-sm overflow-hidden">
            {/* Animal image */}
            <div className="aspect-w-16 aspect-h-9 bg-gray-100">
              {animal.mainPhotoUrl ? (
                <img
                  src={animal.mainPhotoUrl}
                  alt={animal.name}
                  className="w-full h-72 object-cover object-center"
                />
              ) : (
                <div className="w-full h-72 flex items-center justify-center bg-gray-100">
                  <svg
                    className="w-16 h-16 text-gray-300"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                    />
                  </svg>
                </div>
              )}
            </div>

            {/* Animal info */}
            <div className="p-4">
              <div className="flex items-start justify-between mb-2">
                <h3 className="text-lg font-semibold text-gray-900">{animal.name}</h3>
                <span className="text-xs text-gray-500">{animal.registrationNumber}</span>
              </div>

              <div className="space-y-1 text-sm text-gray-600 mb-4">
                <p>
                  <span className="font-medium">Gatunek:</span> {getSpeciesLabel(animal.species)}
                </p>
                <p>
                  <span className="font-medium">Rasa:</span> {animal.breed}
                </p>
                <p>
                  <span className="font-medium">Plec:</span> {getGenderLabel(animal.gender)}
                </p>
                <p>
                  <span className="font-medium">Wiek:</span> {formatAge(animal.ageInMonths)}
                </p>
                <p>
                  <span className="font-medium">Rozmiar:</span> {getSizeLabel(animal.size)}
                </p>
              </div>

              {/* Actions */}
              <div className="flex flex-col gap-2">
                <div className="flex gap-2">
                  <button
                    onClick={() => handleViewNotes(animal.id)}
                    className="flex-1 btn btn-secondary text-sm py-2"
                  >
                    {selectedAnimalId === animal.id ? 'Ukryj notatki' : 'Zobacz notatki'}
                  </button>
                  <button
                    onClick={() => handleAddNote(animal)}
                    className="flex-1 btn btn-primary text-sm py-2"
                  >
                    Dodaj notatke
                  </button>
                </div>
                <Link
                  to={`/animals/${animal.id}`}
                  className="btn btn-secondary text-sm py-2 text-center"
                >
                  Szczegoly
                </Link>
              </div>
            </div>

            {/* Notes panel (expandable) */}
            {selectedAnimalId === animal.id && (
              <div className="border-t border-gray-200">
                <AnimalNotesPanel animalId={animal.id} />
              </div>
            )}
          </div>
        ))}
        </div>
      )}

      {/* Add note modal */}
      {showAddNoteModal && animalForNote && (
        <AddAnimalNoteModal
          animal={animalForNote}
          volunteerId={volunteerId}
          onClose={() => {
            setShowAddNoteModal(false);
            setAnimalForNote(null);
          }}
          onSuccess={() => {
            setShowAddNoteModal(false);
            setAnimalForNote(null);
            // Refresh notes if the animal panel is open
            if (selectedAnimalId === animalForNote.id) {
              setSelectedAnimalId(null);
              setTimeout(() => setSelectedAnimalId(animalForNote.id), 100);
            }
          }}
        />
      )}
    </div>
  );
}
