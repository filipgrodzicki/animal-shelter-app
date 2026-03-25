import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import {
  UserCircleIcon,
  KeyIcon,
  BellIcon,
  ClipboardDocumentListIcon,
  ChevronRightIcon,
} from '@heroicons/react/24/outline';
import { PageContainer, PageHeader } from '@/components/layout';
import { Button, Input, Card } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import toast from 'react-hot-toast';

interface ProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

interface PasswordFormData {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export function ProfilePage() {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<'profile' | 'password' | 'notifications'>('profile');
  const [isLoading, setIsLoading] = useState(false);

  const {
    register: registerProfile,
    handleSubmit: handleSubmitProfile,
    formState: { errors: profileErrors },
  } = useForm<ProfileFormData>({
    defaultValues: {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      email: user?.email || '',
      phoneNumber: user?.phoneNumber || '',
    },
  });

  const {
    register: registerPassword,
    handleSubmit: handleSubmitPassword,
    watch,
    reset: resetPassword,
    formState: { errors: passwordErrors },
  } = useForm<PasswordFormData>();

  const newPassword = watch('newPassword');

  const onSubmitProfile = async (data: ProfileFormData) => {
    setIsLoading(true);
    try {
      // TODO: API call to update profile
      console.log('Profile data:', data);
      await new Promise((resolve) => setTimeout(resolve, 1000));
      toast.success('Profil został zaktualizowany');
    } catch {
      toast.error('Wystąpił błąd podczas aktualizacji profilu');
    } finally {
      setIsLoading(false);
    }
  };

  const onSubmitPassword = async (data: PasswordFormData) => {
    setIsLoading(true);
    try {
      // TODO: API call to change password
      console.log('Password data:', data);
      await new Promise((resolve) => setTimeout(resolve, 1000));
      toast.success('Hasło zostało zmienione');
      resetPassword();
    } catch {
      toast.error('Wystąpił błąd podczas zmiany hasła');
    } finally {
      setIsLoading(false);
    }
  };

  const tabs = [
    { id: 'profile' as const, name: 'Profil', icon: UserCircleIcon },
    { id: 'password' as const, name: 'Hasło', icon: KeyIcon },
    { id: 'notifications' as const, name: 'Powiadomienia', icon: BellIcon },
  ];

  return (
    <PageContainer>
      <PageHeader title="Ustawienia konta" />

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
        {/* Sidebar */}
        <div className="lg:col-span-1">
          <Card className="p-2">
            {/* My adoptions link */}
            <Link
              to="/profile/adoptions"
              className="flex items-center justify-between gap-3 px-4 py-3 rounded-lg text-gray-700 hover:bg-primary-50 hover:text-primary-600 transition-colors mb-2"
            >
              <div className="flex items-center gap-3">
                <ClipboardDocumentListIcon className="h-5 w-5" />
                <span className="font-medium">Moje adopcje</span>
              </div>
              <ChevronRightIcon className="h-4 w-4" />
            </Link>

            <div className="border-t border-gray-200 my-2" />

            <nav className="space-y-1">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left transition-colors ${
                    activeTab === tab.id
                      ? 'bg-primary-50 text-primary-600'
                      : 'text-gray-700 hover:bg-gray-50'
                  }`}
                >
                  <tab.icon className="h-5 w-5" />
                  <span className="font-medium">{tab.name}</span>
                </button>
              ))}
            </nav>
          </Card>
        </div>

        {/* Content */}
        <div className="lg:col-span-3">
          {activeTab === 'profile' && (
            <Card className="p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Dane osobowe</h2>
              <form onSubmit={handleSubmitProfile(onSubmitProfile)} className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <Input
                    label="Imię"
                    {...registerProfile('firstName', {
                      required: 'Imię jest wymagane',
                    })}
                    error={profileErrors.firstName?.message}
                  />
                  <Input
                    label="Nazwisko"
                    {...registerProfile('lastName', {
                      required: 'Nazwisko jest wymagane',
                    })}
                    error={profileErrors.lastName?.message}
                  />
                </div>

                <Input
                  label="Email"
                  type="email"
                  {...registerProfile('email', {
                    required: 'Email jest wymagany',
                    pattern: {
                      value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                      message: 'Nieprawidłowy adres email',
                    },
                  })}
                  error={profileErrors.email?.message}
                />

                <Input
                  label="Numer telefonu"
                  type="tel"
                  {...registerProfile('phoneNumber', {
                    required: 'Numer telefonu jest wymagany',
                  })}
                  error={profileErrors.phoneNumber?.message}
                />

                <div className="pt-4">
                  <Button type="submit" isLoading={isLoading}>
                    Zapisz zmiany
                  </Button>
                </div>
              </form>
            </Card>
          )}

          {activeTab === 'password' && (
            <Card className="p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Zmień hasło</h2>
              <form onSubmit={handleSubmitPassword(onSubmitPassword)} className="space-y-4 max-w-md">
                <Input
                  label="Obecne hasło"
                  type="password"
                  {...registerPassword('currentPassword', {
                    required: 'Obecne hasło jest wymagane',
                  })}
                  error={passwordErrors.currentPassword?.message}
                />

                <Input
                  label="Nowe hasło"
                  type="password"
                  {...registerPassword('newPassword', {
                    required: 'Nowe hasło jest wymagane',
                    minLength: {
                      value: 8,
                      message: 'Hasło musi mieć min. 8 znaków',
                    },
                    pattern: {
                      value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
                      message: 'Hasło musi zawierać małą i wielką literę oraz cyfrę',
                    },
                  })}
                  error={passwordErrors.newPassword?.message}
                />

                <Input
                  label="Potwierdź nowe hasło"
                  type="password"
                  {...registerPassword('confirmPassword', {
                    required: 'Potwierdzenie hasła jest wymagane',
                    validate: (value) =>
                      value === newPassword || 'Hasła nie są identyczne',
                  })}
                  error={passwordErrors.confirmPassword?.message}
                />

                <div className="pt-4">
                  <Button type="submit" isLoading={isLoading}>
                    Zmień hasło
                  </Button>
                </div>
              </form>
            </Card>
          )}

          {activeTab === 'notifications' && (
            <Card className="p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Powiadomienia</h2>
              <div className="space-y-4">
                <label className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                  <div>
                    <div className="font-medium text-gray-900">Powiadomienia email</div>
                    <div className="text-sm text-gray-600">
                      Otrzymuj powiadomienia o statusie adopcji i nowościach
                    </div>
                  </div>
                  <input
                    type="checkbox"
                    defaultChecked
                    className="h-5 w-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                  />
                </label>

                <label className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                  <div>
                    <div className="font-medium text-gray-900">Newsletter</div>
                    <div className="text-sm text-gray-600">
                      Bądź na bieżąco z aktualnościami schroniska
                    </div>
                  </div>
                  <input
                    type="checkbox"
                    defaultChecked
                    className="h-5 w-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                  />
                </label>

                <label className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                  <div>
                    <div className="font-medium text-gray-900">Przypomnienia o wolontariacie</div>
                    <div className="text-sm text-gray-600">
                      Otrzymuj przypomnienia o zaplanowanych dyżurach
                    </div>
                  </div>
                  <input
                    type="checkbox"
                    defaultChecked
                    className="h-5 w-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                  />
                </label>

                <div className="pt-4">
                  <Button onClick={() => toast.success('Ustawienia zapisane')}>
                    Zapisz preferencje
                  </Button>
                </div>
              </div>
            </Card>
          )}
        </div>
      </div>
    </PageContainer>
  );
}
