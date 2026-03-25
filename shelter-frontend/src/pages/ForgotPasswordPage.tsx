import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { ArrowLeftIcon, CheckCircleIcon } from '@heroicons/react/24/outline';
import { Button, Input, Card } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import toast from 'react-hot-toast';

interface ForgotPasswordFormData {
  email: string;
}

export function ForgotPasswordPage() {
  const { forgotPassword } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormData>();

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setIsLoading(true);
    try {
      await forgotPassword(data.email);
      setSubmittedEmail(data.email);
      setIsSubmitted(true);
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Wystąpił błąd';
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  if (isSubmitted) {
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
              Sprawdź swoją skrzynkę
            </h2>
            <p className="text-gray-600 mb-6">
              Wysłaliśmy link do resetowania hasła na adres{' '}
              <span className="font-medium text-gray-900">{submittedEmail}</span>
            </p>
            <p className="text-sm text-gray-500 mb-6">
              Jeśli nie widzisz wiadomości, sprawdź folder spam.
            </p>
            <Link to="/login">
              <Button variant="outline" className="w-full">
                <ArrowLeftIcon className="h-4 w-4 mr-2" />
                Wróć do logowania
              </Button>
            </Link>
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
            Zapomniałeś hasła?
          </h1>
          <p className="mt-2 text-gray-600">
            Podaj swój adres email, a wyślemy Ci link do resetowania hasła.
          </p>
        </div>

        <Card className="p-6">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              label="Email"
              type="email"
              autoComplete="email"
              {...register('email', {
                required: 'Email jest wymagany',
                pattern: {
                  value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                  message: 'Nieprawidłowy adres email',
                },
              })}
              error={errors.email?.message}
            />

            <Button type="submit" className="w-full" isLoading={isLoading}>
              Wyślij link resetujący
            </Button>

            <div className="text-center">
              <Link
                to="/login"
                className="text-sm text-primary-600 hover:text-primary-700 inline-flex items-center gap-1"
              >
                <ArrowLeftIcon className="h-4 w-4" />
                Wróć do logowania
              </Link>
            </div>
          </form>
        </Card>
      </div>
    </div>
  );
}
