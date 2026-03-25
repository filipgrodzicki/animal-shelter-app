import { useState, useCallback, useEffect } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import {
  XMarkIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ArrowsPointingOutIcon,
  PhotoIcon,
} from '@heroicons/react/24/outline';
import { AnimalPhoto } from '@/types';

interface AnimalPhotoGalleryProps {
  photos: AnimalPhoto[];
  animalName: string;
}

export function AnimalPhotoGallery({ photos, animalName }: AnimalPhotoGalleryProps) {
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [isLightboxOpen, setIsLightboxOpen] = useState(false);

  // Sort photos by displayOrder, main photo first
  const sortedPhotos = [...photos].sort((a, b) => {
    if (a.isMain) return -1;
    if (b.isMain) return 1;
    return a.displayOrder - b.displayOrder;
  });

  const currentPhoto = sortedPhotos[selectedIndex];

  const goToPrevious = useCallback(() => {
    setSelectedIndex((prev) => (prev === 0 ? sortedPhotos.length - 1 : prev - 1));
  }, [sortedPhotos.length]);

  const goToNext = useCallback(() => {
    setSelectedIndex((prev) => (prev === sortedPhotos.length - 1 ? 0 : prev + 1));
  }, [sortedPhotos.length]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!isLightboxOpen) return;
      if (e.key === 'ArrowLeft') goToPrevious();
      if (e.key === 'ArrowRight') goToNext();
      if (e.key === 'Escape') setIsLightboxOpen(false);
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isLightboxOpen, goToPrevious, goToNext]);

  // Placeholder when no photos
  if (sortedPhotos.length === 0) {
    return (
      <div className="aspect-[4/3] bg-gradient-to-br from-warm-100 to-warm-200 rounded-xl flex items-center justify-center">
        <div className="text-center">
          <PhotoIcon className="w-20 h-20 mx-auto text-primary-300 opacity-50" />
          <p className="mt-4 text-warm-700">Brak zdjęć</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Main photo */}
      <div className="relative group">
        <div
          className="aspect-[4/3] overflow-hidden rounded-xl bg-warm-100 cursor-pointer"
          onClick={() => setIsLightboxOpen(true)}
        >
          <img
            src={currentPhoto?.url}
            alt={`${animalName} - zdjęcie ${selectedIndex + 1}`}
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
          />
        </div>

        {/* Expand button */}
        <button
          onClick={() => setIsLightboxOpen(true)}
          className="absolute top-4 right-4 p-2 bg-black/50 rounded-lg text-white opacity-0 group-hover:opacity-100 transition-opacity hover:bg-black/70"
          aria-label="Powiększ zdjęcie"
        >
          <ArrowsPointingOutIcon className="h-5 w-5" aria-hidden="true" />
        </button>

        {/* Navigation arrows for main view */}
        {sortedPhotos.length > 1 && (
          <>
            <button
              onClick={(e) => {
                e.stopPropagation();
                goToPrevious();
              }}
              className="absolute left-4 top-1/2 -translate-y-1/2 p-2 bg-black/50 rounded-full text-white opacity-0 group-hover:opacity-100 transition-opacity hover:bg-black/70"
              aria-label="Poprzednie zdjęcie"
            >
              <ChevronLeftIcon className="h-6 w-6" aria-hidden="true" />
            </button>
            <button
              onClick={(e) => {
                e.stopPropagation();
                goToNext();
              }}
              className="absolute right-4 top-1/2 -translate-y-1/2 p-2 bg-black/50 rounded-full text-white opacity-0 group-hover:opacity-100 transition-opacity hover:bg-black/70"
              aria-label="Następne zdjęcie"
            >
              <ChevronRightIcon className="h-6 w-6" aria-hidden="true" />
            </button>
          </>
        )}

        {/* Photo counter */}
        {sortedPhotos.length > 1 && (
          <div className="absolute bottom-4 left-1/2 -translate-x-1/2 px-3 py-1 bg-black/50 rounded-full text-white text-sm">
            {selectedIndex + 1} / {sortedPhotos.length}
          </div>
        )}
      </div>

      {/* Thumbnails */}
      {sortedPhotos.length > 1 && (
        <div className="flex gap-2 overflow-x-auto pb-2 scrollbar-thin">
          {sortedPhotos.map((photo, index) => (
            <button
              key={photo.id}
              onClick={() => setSelectedIndex(index)}
              className={`flex-shrink-0 w-20 h-20 rounded-lg overflow-hidden border-2 transition-all ${
                index === selectedIndex
                  ? 'border-primary-500 ring-2 ring-primary-200'
                  : 'border-transparent hover:border-warm-300'
              }`}
            >
              <img
                src={photo.thumbnailUrl || photo.url}
                alt={`${animalName} - miniatura ${index + 1}`}
                className="h-full w-full object-cover"
              />
            </button>
          ))}
        </div>
      )}

      {/* Lightbox */}
      <Transition show={isLightboxOpen} as={Fragment}>
        <Dialog onClose={() => setIsLightboxOpen(false)} className="relative z-50">
          {/* Backdrop */}
          <Transition.Child
            as={Fragment}
            enter="ease-out duration-300"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="ease-in duration-200"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-black/90" />
          </Transition.Child>

          {/* Full-screen container */}
          <div className="fixed inset-0 overflow-y-auto">
            <div className="flex min-h-full items-center justify-center p-4">
              <Transition.Child
                as={Fragment}
                enter="ease-out duration-300"
                enterFrom="opacity-0 scale-95"
                enterTo="opacity-100 scale-100"
                leave="ease-in duration-200"
                leaveFrom="opacity-100 scale-100"
                leaveTo="opacity-0 scale-95"
              >
                <Dialog.Panel className="relative max-w-5xl w-full">
                  {/* Close button */}
                  <button
                    onClick={() => setIsLightboxOpen(false)}
                    className="absolute -top-12 right-0 p-2 text-white hover:text-gray-300 transition-colors"
                    aria-label="Zamknij"
                  >
                    <XMarkIcon className="h-8 w-8" aria-hidden="true" />
                  </button>

                  {/* Image */}
                  <div className="relative">
                    <img
                      src={currentPhoto?.url}
                      alt={`${animalName} - zdjęcie ${selectedIndex + 1}`}
                      className="w-full h-auto max-h-[80vh] object-contain rounded-lg"
                    />

                    {/* Navigation arrows */}
                    {sortedPhotos.length > 1 && (
                      <>
                        <button
                          onClick={goToPrevious}
                          className="absolute left-4 top-1/2 -translate-y-1/2 p-3 bg-black/50 rounded-full text-white hover:bg-black/70 transition-colors"
                          aria-label="Poprzednie zdjęcie"
                        >
                          <ChevronLeftIcon className="h-8 w-8" aria-hidden="true" />
                        </button>
                        <button
                          onClick={goToNext}
                          className="absolute right-4 top-1/2 -translate-y-1/2 p-3 bg-black/50 rounded-full text-white hover:bg-black/70 transition-colors"
                          aria-label="Następne zdjęcie"
                        >
                          <ChevronRightIcon className="h-8 w-8" aria-hidden="true" />
                        </button>
                      </>
                    )}
                  </div>

                  {/* Photo description and counter */}
                  <div className="mt-4 text-center text-white">
                    {currentPhoto?.description && (
                      <p className="text-gray-300 mb-2">{currentPhoto.description}</p>
                    )}
                    <p className="text-sm text-gray-400">
                      {selectedIndex + 1} / {sortedPhotos.length}
                    </p>
                  </div>

                  {/* Thumbnails in lightbox */}
                  {sortedPhotos.length > 1 && (
                    <div className="flex justify-center gap-2 mt-4 overflow-x-auto pb-2">
                      {sortedPhotos.map((photo, index) => (
                        <button
                          key={photo.id}
                          onClick={() => setSelectedIndex(index)}
                          className={`flex-shrink-0 w-16 h-16 rounded-lg overflow-hidden border-2 transition-all ${
                            index === selectedIndex
                              ? 'border-white'
                              : 'border-transparent opacity-50 hover:opacity-100'
                          }`}
                        >
                          <img
                            src={photo.thumbnailUrl || photo.url}
                            alt={`Miniatura ${index + 1}`}
                            className="h-full w-full object-cover"
                          />
                        </button>
                      ))}
                    </div>
                  )}
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </Dialog>
      </Transition>
    </div>
  );
}
