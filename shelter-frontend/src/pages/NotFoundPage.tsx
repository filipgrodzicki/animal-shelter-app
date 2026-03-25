import { Link } from 'react-router-dom';
import { HomeIcon, MagnifyingGlassIcon } from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button } from '@/components/common';

export function NotFoundPage() {
  return (
    <PageContainer className="flex items-center justify-center min-h-[calc(100vh-4rem)]">
      <div className="text-center max-w-md">
        <div className="text-8xl font-bold text-primary-200 mb-4">404</div>
        <h1 className="text-2xl font-bold text-gray-900 mb-4">
          Strona nie znaleziona
        </h1>
        <p className="text-gray-600 mb-8">
          Przepraszamy, ale strona której szukasz nie istnieje lub została przeniesiona.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Button as={Link} to="/">
            <HomeIcon className="h-5 w-5 mr-2" />
            Strona główna
          </Button>
          <Button as={Link} to="/animals" variant="outline">
            <MagnifyingGlassIcon className="h-5 w-5 mr-2" />
            Zobacz zwierzęta
          </Button>
        </div>
      </div>
    </PageContainer>
  );
}
