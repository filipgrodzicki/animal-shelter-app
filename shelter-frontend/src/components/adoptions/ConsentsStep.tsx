import { useState } from 'react';
import { UseFormRegister, FieldErrors } from 'react-hook-form';
import { ShieldCheckIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { AdoptionFormData } from './adoptionFormSchema';

interface ConsentsStepProps {
  register: UseFormRegister<AdoptionFormData>;
  errors: FieldErrors<AdoptionFormData>;
}

export function ConsentsStep({ register, errors }: ConsentsStepProps) {
  const [showRulesModal, setShowRulesModal] = useState(false);

  return (
    <>
    {/* Rules Modal */}
    {showRulesModal && (
      <div className="fixed inset-0 z-50 overflow-y-auto">
        <div className="flex min-h-full items-center justify-center p-4">
          <div className="fixed inset-0 bg-black/50" onClick={() => setShowRulesModal(false)} />
          <div className="relative bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[80vh] overflow-y-auto">
            <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
              <h2 className="text-xl font-semibold text-gray-900">Regulamin Adopcji</h2>
              <button
                onClick={() => setShowRulesModal(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <XMarkIcon className="w-6 h-6" />
              </button>
            </div>
            <div className="px-6 py-4 space-y-4 text-sm text-gray-700">
              <h3 className="font-semibold text-gray-900">§1. Postanowienia ogólne</h3>
              <p>1. Niniejszy regulamin określa zasady adopcji zwierząt ze Schroniska dla Bezdomnych Zwierząt.</p>
              <p>2. Adopcja ma na celu zapewnienie zwierzętom bezpiecznego i kochającego domu.</p>

              <h3 className="font-semibold text-gray-900 pt-2">§2. Warunki adopcji</h3>
              <p>1. Adoptować może osoba pełnoletnia (ukończone 18 lat).</p>
              <p>2. Adoptujący musi posiadać odpowiednie warunki do opieki nad zwierzęciem.</p>
              <p>3. Adopcja jest bezpłatna, jednak zachęcamy do dobrowolnych darowizn na rzecz schroniska.</p>

              <h3 className="font-semibold text-gray-900 pt-2">§3. Proces adopcji</h3>
              <p>1. Złożenie wniosku adopcyjnego przez stronę internetową lub osobiście.</p>
              <p>2. Weryfikacja wniosku przez pracownika schroniska.</p>
              <p>3. Wizyta przedadopcyjna (opcjonalnie, na życzenie schroniska).</p>
              <p>4. Podpisanie umowy adopcyjnej.</p>
              <p>5. Odbiór zwierzęcia.</p>

              <h3 className="font-semibold text-gray-900 pt-2">§4. Obowiązki adoptującego</h3>
              <p>1. Zapewnienie zwierzęciu odpowiednich warunków bytowych.</p>
              <p>2. Zapewnienie regularnej opieki weterynaryjnej.</p>
              <p>3. Traktowanie zwierzęcia z szacunkiem i troską.</p>
              <p>4. Nieprzekazywanie zwierzęcia osobom trzecim bez zgody schroniska.</p>
              <p>5. Informowanie schroniska o zmianie adresu zamieszkania.</p>

              <h3 className="font-semibold text-gray-900 pt-2">§5. Wizyty kontrolne</h3>
              <p>1. Schronisko zastrzega sobie prawo do przeprowadzenia wizyt kontrolnych.</p>
              <p>2. W przypadku stwierdzenia zaniedbania zwierzęcia, schronisko może żądać jego zwrotu.</p>

              <h3 className="font-semibold text-gray-900 pt-2">§6. Zwrot zwierzęcia</h3>
              <p>1. W przypadku niemożności dalszej opieki nad zwierzęciem, adoptujący zobowiązuje się do kontaktu ze schroniskiem.</p>
              <p>2. Zwierzę może zostać przyjęte z powrotem do schroniska.</p>

              <h3 className="font-semibold text-gray-900 pt-2">§7. Postanowienia końcowe</h3>
              <p>1. Schronisko zastrzega sobie prawo do odmowy adopcji bez podania przyczyny.</p>
              <p>2. Regulamin wchodzi w życie z dniem opublikowania.</p>
            </div>
            <div className="sticky bottom-0 bg-gray-50 border-t border-gray-200 px-6 py-4">
              <button
                onClick={() => setShowRulesModal(false)}
                className="w-full px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Zamknij
              </button>
            </div>
          </div>
        </div>
      </div>
    )}
    <div className="space-y-6">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
          <ShieldCheckIcon className="w-5 h-5 text-primary-600" />
        </div>
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Zgody i oświadczenia</h2>
          <p className="text-sm text-gray-500">Zapoznaj się z poniższymi informacjami</p>
        </div>
      </div>

      {/* GDPR Consent */}
      <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
        <label className="flex items-start gap-3 cursor-pointer">
          <input
            type="checkbox"
            {...register('gdprConsent')}
            className="w-5 h-5 mt-0.5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          <div className="flex-1">
            <span className="font-medium text-gray-900">
              Zgoda na przetwarzanie danych osobowych
              <span className="text-red-500 ml-1">*</span>
            </span>
            <p className="text-sm text-gray-600 mt-2">
              Wyrażam zgodę na przetwarzanie moich danych osobowych przez Schronisko
              w celu przeprowadzenia procesu adopcyjnego, zgodnie z Rozporządzeniem Parlamentu
              Europejskiego i Rady (UE) 2016/679 z dnia 27 kwietnia 2016 r. (RODO).
            </p>
            <p className="text-sm text-gray-600 mt-2">
              Dane będą przetwarzane w celu:
            </p>
            <ul className="text-sm text-gray-600 mt-1 list-disc list-inside ml-2">
              <li>rozpatrzenia wniosku adopcyjnego</li>
              <li>kontaktu w sprawie adopcji</li>
              <li>weryfikacji warunków mieszkaniowych (jeśli wymagane)</li>
              <li>prowadzenia dokumentacji adopcyjnej</li>
            </ul>
            <p className="text-sm text-gray-600 mt-2">
              Administratorem danych jest Schronisko. Masz prawo do dostępu,
              sprostowania, usunięcia danych, ograniczenia przetwarzania oraz przenoszenia danych.
              Dane będą przechowywane przez okres niezbędny do realizacji procesu adopcyjnego,
              a następnie przez okres wymagany przepisami prawa.
            </p>
          </div>
        </label>
        {errors.gdprConsent && (
          <p className="mt-2 text-sm text-red-600 ml-8">{errors.gdprConsent.message}</p>
        )}
      </div>

      {/* Rules Consent */}
      <div className="bg-gray-50 rounded-lg p-4 border border-gray-200">
        <label className="flex items-start gap-3 cursor-pointer">
          <input
            type="checkbox"
            {...register('rulesConsent')}
            className="w-5 h-5 mt-0.5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          <div className="flex-1">
            <span className="font-medium text-gray-900">
              Akceptacja regulaminu adopcji
              <span className="text-red-500 ml-1">*</span>
            </span>
            <p className="text-sm text-gray-600 mt-2">
              Oświadczam, że zapoznałem/am się z{' '}
              <button
                type="button"
                onClick={() => setShowRulesModal(true)}
                className="text-primary-600 hover:underline font-medium"
              >
                regulaminem adopcji
              </button>{' '}
              i akceptuję jego warunki.
            </p>
            <p className="text-sm text-gray-600 mt-2">
              W szczególności potwierdzam, że:
            </p>
            <ul className="text-sm text-gray-600 mt-1 list-disc list-inside ml-2">
              <li>wszystkie podane przeze mnie informacje są prawdziwe</li>
              <li>rozumiem odpowiedzialność związaną z adopcją zwierzęcia</li>
              <li>zobowiązuję się do zapewnienia zwierzęciu odpowiedniej opieki</li>
              <li>wyrażam zgodę na ewentualną wizytę przedadopcyjną</li>
              <li>
                w przypadku niemożności dalszej opieki, zobowiązuję się do kontaktu ze schroniskiem
              </li>
            </ul>
          </div>
        </label>
        {errors.rulesConsent && (
          <p className="mt-2 text-sm text-red-600 ml-8">{errors.rulesConsent.message}</p>
        )}
      </div>
    </div>
    </>
  );
}
