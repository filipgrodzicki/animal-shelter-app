import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { CheckCircleIcon } from '@heroicons/react/24/outline';
import { Button, Input, Card } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import toast from 'react-hot-toast';

interface ResetPasswordFormData {
  newPassword: string;
  confirmPassword: string;
}

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { resetPassword } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);

  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<ResetPasswordFormData>();

  const newPassword = watch('newPassword');

  // Redirect if missing params
  if (!email || !token) {
    return (
      <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center py-12 px-4">
        <div className="w-full max-w-md">
          <Card className="p-8 text-center">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">
              Nieprawidłowy link
            </h2>
            <p className="text-gray-600 mb-6">
              Link do resetowania hasła jest nieprawidłowy lub wygasł.
            </p>
            <Link to="/forgot-password">
              <Button className="w-full">Poproś o nowy link</Button>
            </Link>
          </Card>
        </div>
      </div>
    );
  }

  const onSubmit = async (data: ResetPasswordFormData) => {
    setIsLoading(true);
    try {
      await resetPassword(email, token, data.newPassword, data.confirmPassword);
      setIsSuccess(true);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Wystąpił błąd';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  if (isSuccess) {
    return (
      <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center py-12 px-4">
        <div className="w-full max-w-md">
          <div className="text-center mb-8">
            <Link to="/" className="inline-flex items-center gap-3">
              <img src="/images/logo-wat.png" alt="WAT Logo" className="w-12 h-12" />
              <span className="font-bold text-2xl text-primary-600">Schronisko</span>
            </Link>
          </div>

          <Card className="p-8 text-center">
            <CheckCircleIcon className="h-16 w-16 text-green-500 mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-900 mb-2">
              Hasło zostało zmienione
            </h2>
            <p className="text-gray-600 mb-6">
              Możesz teraz zalogować się używając nowego hasła.
            </p>
            <Button className="w-full" onClick={() => navigate('/login')}>
              Przejdź do logowania
            </Button>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center py-12 px-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-3">
            <img src="/images/logo-wat.png" alt="WAT Logo" className="w-12 h-12" />
            <span className="font-bold text-2xl text-primary-600">Schronisko</span>
          </Link>
          <h1 className="mt-6 text-3xl font-bold text-gray-900">
            Ustaw nowe hasło
          </h1>
          <p className="mt-2 text-gray-600">
            Wprowadź nowe hasło dla konta {email}
          </p>
        </div>

        <Card className="p-6">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              label="Nowe hasło"
              type="password"
              autoComplete="new-password"
              {...register('newPassword', {
                required: 'Hasło jest wymagane',
                minLength: { value: 8, message: 'Hasło musi mieć min. 8 znaków' },
                pattern: {
                  value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
                  message: 'Hasło musi zawierać małą i wielką literę oraz cyfrę',
                },
              })}
              error={errors.newPassword?.message}
            />

            <Input
              label="Potwierdź hasło"
              type="password"
              autoComplete="new-password"
              {...register('confirmPassword', {
                required: 'Potwierdzenie hasła jest wymagane',
                validate: (value) => value === newPassword || 'Hasła nie są identyczne',
              })}
              error={errors.confirmPassword?.message}
            />

            <Button type="submit" className="w-full" isLoading={isLoading}>
              Zmień hasło
            </Button>
          </form>
        </Card>
      </div>
    </div>
  );
}
