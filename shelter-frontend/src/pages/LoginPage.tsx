import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { PageContainer } from '@/components/layout';
import { Button, Input, Card } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import { LoginRequest } from '@/types';
import toast from 'react-hot-toast';

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const [isLoading, setIsLoading] = useState(false);

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/';

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginRequest>();

  const onSubmit = async (data: LoginRequest) => {
    setIsLoading(true);
    try {
      await login(data.email, data.password);
      toast.success('Zalogowano pomyślnie');
      navigate(from, { replace: true });
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Nieprawidłowy email lub hasło';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <PageContainer className="flex items-center justify-center min-h-[calc(100vh-4rem)]">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-3 mb-6">
            <img src="/images/logo-wat.png" alt="WAT Logo" className="w-12 h-12" />
            <span className="font-heading font-bold text-2xl text-primary-600">Schronisko</span>
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Zaloguj się</h1>
          <p className="text-gray-600">
            Nie masz konta?{' '}
            <Link to="/register" className="text-primary-600 hover:text-primary-700 font-semibold">
              Zarejestruj się
            </Link>
          </p>
        </div>

        <Card className="p-6 sm:p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <Input
              label="Email"
              type="email"
              autoComplete="email"
              placeholder="jan@example.com"
              {...register('email', {
                required: 'Email jest wymagany',
                pattern: {
                  value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                  message: 'Nieprawidłowy adres email',
                },
              })}
              error={errors.email?.message}
            />

            <Input
              label="Hasło"
              type="password"
              autoComplete="current-password"
              placeholder="••••••••"
              {...register('password', {
                required: 'Hasło jest wymagane',
              })}
              error={errors.password?.message}
            />

            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                />
                <span className="text-sm text-gray-600">Zapamiętaj mnie</span>
              </label>
              <Link
                to="/forgot-password"
                className="text-sm text-primary-600 hover:text-primary-700 font-medium"
              >
                Zapomniałeś hasła?
              </Link>
            </div>

            <Button type="submit" className="w-full" size="lg" isLoading={isLoading}>
              Zaloguj się
            </Button>
          </form>
        </Card>

        <p className="mt-8 text-center text-sm text-gray-500">
          Logując się, akceptujesz{' '}
          <Link to="/terms" className="text-primary-600 hover:underline">
            regulamin
          </Link>{' '}
          i{' '}
          <Link to="/privacy" className="text-primary-600 hover:underline">
            politykę prywatności
          </Link>
        </p>
      </div>
    </PageContainer>
  );
}
