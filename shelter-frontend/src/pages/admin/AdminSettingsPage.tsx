import { Link } from 'react-router-dom';
import {
  UsersIcon,
  Cog6ToothIcon,
  ShieldCheckIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Card } from '@/components/common';

export function AdminSettingsPage() {
  return (
    <PageContainer>
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-2">
          <ShieldCheckIcon className="h-8 w-8 text-red-600" />
          <h1 className="text-3xl font-bold text-gray-900">Panel Administratora</h1>
        </div>
        <p className="text-gray-600">Wylaczne funkcje dostepne tylko dla administratorow systemu</p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {/* User Management */}
        <Link to="/admin/users" className="block">
          <Card className="p-6 hover:shadow-lg transition-shadow cursor-pointer border-2 hover:border-primary-500">
            <div className="flex items-start gap-4">
              <div className="p-3 bg-red-100 rounded-lg">
                <UsersIcon className="h-8 w-8 text-red-600" />
              </div>
              <div className="flex-1">
                <h2 className="text-xl font-semibold text-gray-900 mb-2">Zarzadzanie uzytkownikami</h2>
                <p className="text-gray-600 mb-4">
                  Tworzenie, edycja i dezaktywacja kont uzytkownikow. Zarzadzanie rolami i uprawnieniami.
                </p>
                <ul className="text-sm text-gray-500 space-y-1">
                  <li>• Dodawanie nowych uzytkownikow</li>
                  <li>• Zmiana rol (Admin, Staff, Volunteer, User)</li>
                  <li>• Aktywacja i dezaktywacja kont</li>
                  <li>• Resetowanie hasel</li>
                </ul>
              </div>
            </div>
          </Card>
        </Link>

        {/* AI Configuration */}
        <Link to="/admin/config" className="block">
          <Card className="p-6 hover:shadow-lg transition-shadow cursor-pointer border-2 hover:border-primary-500">
            <div className="flex items-start gap-4">
              <div className="p-3 bg-blue-100 rounded-lg">
                <Cog6ToothIcon className="h-8 w-8 text-blue-600" />
              </div>
              <div className="flex-1">
                <h2 className="text-xl font-semibold text-gray-900 mb-2">Konfiguracja systemu AI</h2>
                <p className="text-gray-600 mb-4">
                  Ustawienia chatbota AI i algorytmu dopasowania zwierzat do adoptujacych.
                </p>
                <ul className="text-sm text-gray-500 space-y-1">
                  <li>• Prompt systemowy chatbota</li>
                  <li>• Dozwolone tematy rozmow</li>
                  <li>• Reguly i wytyczne AI</li>
                  <li>• Wagi algorytmu dopasowania</li>
                </ul>
              </div>
            </div>
          </Card>
        </Link>
      </div>
    </PageContainer>
  );
}
