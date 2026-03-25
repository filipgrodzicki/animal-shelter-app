import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { PageContainer } from '@/components/layout';
import { Button, Input, Card } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import { RegisterRequest } from '@/types';
import toast from 'react-hot-toast';

interface RegisterFormData extends Omit<RegisterRequest, 'confirmPassword'> {
  confirmPassword: string;
  terms?: boolean;
}

export function RegisterPage() {
  const navigate = useNavigate();
  const { register: registerUser } = useAuth();
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegisterFormData>();

  const password = watch('password');

  const onSubmit = async (data: RegisterFormData) => {
    setIsLoading(true);
    try {
      const { terms, ...registerData } = data;
      await registerUser(registerData);
      toast.success('Konto zostało utworzone pomyślnie!');
      navigate('/');
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Wystąpił błąd podczas rejestracji';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <PageContainer className="flex items-center justify-center min-h-[calc(100vh-4rem)] py-8">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-3 mb-6">
            <img src="/images/logo-wat.png" alt="WAT Logo" className="w-12 h-12" />
            <span className="font-heading font-bold text-2xl text-primary-600">Schronisko</span>
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">Załóż konto</h1>
          <p className="text-gray-600">
            Masz już konto?{' '}
            <Link to="/login" className="text-primary-600 hover:text-primary-700 font-semibold">
              Zaloguj się
            </Link>
          </p>
        </div>

        <Card className="p-6 sm:p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Imię"
                placeholder="Jan"
                {...register('firstName', {
                  required: 'Imię jest wymagane',
                  minLength: { value: 2, message: 'Imię musi mieć min. 2 znaki' },
                })}
                error={errors.firstName?.message}
              />

              <Input
                label="Nazwisko"
                placeholder="Kowalski"
                {...register('lastName', {
                  required: 'Nazwisko jest wymagane',
                  minLength: { value: 2, message: 'Nazwisko musi mieć min. 2 znaki' },
                })}
                error={errors.lastName?.message}
              />
            </div>

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
              label="Numer telefonu"
              type="tel"
              placeholder="+48 123 456 789"
              {...register('phoneNumber', {
                required: 'Numer telefonu jest wymagany',
                pattern: {
                  value: /^[0-9+\s-]{9,15}$/,
                  message: 'Nieprawidłowy numer telefonu',
                },
              })}
              error={errors.phoneNumber?.message}
            />

            <Input
              label="Hasło"
              type="password"
              autoComplete="new-password"
              placeholder="Min. 8 znaków"
              {...register('password', {
                required: 'Hasło jest wymagane',
                minLength: { value: 8, message: 'Hasło musi mieć min. 8 znaków' },
                pattern: {
                  value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
                  message: 'Hasło musi zawierać małą i wielką literę oraz cyfrę',
                },
              })}
              error={errors.password?.message}
            />

            <Input
              label="Potwierdź hasło"
              type="password"
              autoComplete="new-password"
              placeholder="Powtórz hasło"
              {...register('confirmPassword', {
                required: 'Potwierdzenie hasła jest wymagane',
                validate: (value) => value === password || 'Hasła nie są identyczne',
              })}
              error={errors.confirmPassword?.message}
            />

            <label className="flex items-start gap-3 cursor-pointer pt-2">
              <input
                type="checkbox"
                className="h-4 w-4 mt-0.5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                {...register('terms', {
                  required: 'Musisz zaakceptować regulamin',
                } as const)}
              />
              <span className="text-sm text-gray-600 leading-tight">
                Akceptuję{' '}
                <Link to="/terms" className="text-primary-600 hover:text-primary-700 font-medium">
                  regulamin
                </Link>{' '}
                oraz{' '}
                <Link to="/privacy" className="text-primary-600 hover:text-primary-700 font-medium">
                  politykę prywatności
                </Link>
              </span>
            </label>
            {errors.terms && (
              <p className="text-sm text-red-600">{errors.terms.message}</p>
            )}

            <Button type="submit" className="w-full" size="lg" isLoading={isLoading}>
              Zarejestruj się
            </Button>
          </form>
        </Card>
      </div>
    </PageContainer>
  );
}
