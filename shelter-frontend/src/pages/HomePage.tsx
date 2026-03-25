import { Link } from 'react-router-dom';
import {
  UserGroupIcon,
  CalendarIcon,
  ClockIcon,
  GiftIcon,
  ArrowRightIcon,
} from '@heroicons/react/24/outline';


import { PageContainer } from '@/components/layout';
import { Button, Card } from '@/components/common';
import { useAuth } from '@/context/AuthContext';
import { isVolunteer, isStaff, categoryLabels, categoryColors } from '@/types';
import { getRecentBlogPosts } from '@/data/blogPosts';

function TablerHomeIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5 12l-2 0l9-9l9 9l-2 0" />
      <path d="M5 12v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-7" />
      <path d="M9 21v-6a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v6" />
    </svg>
  );
}

const baseFeatures = [
  {
    icon: TablerHomeIcon,
    title: 'Adopcja',
    description: 'Znajdź swojego nowego przyjaciela wśród naszych podopiecznych.',
    link: '/animals',
    linkText: 'Zobacz zwierzęta',
  },
  {
    icon: GiftIcon,
    title: 'Wesprzyj nas',
    description: 'Twoja pomoc finansowa pozwala nam opiekować się zwierzętami.',
    link: '/donate',
    linkText: 'Przekaż darowiznę',
  },
];

const volunteerFeature = {
  icon: UserGroupIcon,
  title: 'Wolontariat',
  description: 'Dołącz do naszego zespołu wolontariuszy i pomagaj zwierzętom.',
  link: '/volunteer',
  linkText: 'Zostań wolontariuszem',
};

export function HomePage() {
  const { user } = useAuth();
  const recentPosts = getRecentBlogPosts(3);

  const showVolunteerContent = !isVolunteer(user) && !isStaff(user);
  const features = showVolunteerContent
    ? [baseFeatures[0], volunteerFeature, ...baseFeatures.slice(1)]
    : baseFeatures;

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <div>
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-primary-50 to-primary-100 py-16 md:py-24">
        <PageContainer>
          <div className="grid md:grid-cols-2 gap-12 items-center">
            <div>
              <h1 className="text-4xl md:text-5xl font-bold text-gray-900 leading-tight mb-6">
                Znajdź swojego{' '}
                <span className="text-primary-600">przyjaciela</span> na całe życie
              </h1>
              <p className="text-lg text-gray-600 mb-8">
                W naszym schronisku czekają zwierzęta pełne miłości, które szukają kochającego domu. Daj im drugą szansę.
              </p>
              <div className="flex flex-col sm:flex-row gap-4">
                <Button as={Link} to="/animals" size="lg">
                  <svg className="w-5 h-5 mr-2" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M14.7 13.5c-1.1-2-1.441-2.5-2.7-2.5c-1.259 0-1.736.755-2.836 2.747c-.942 1.703-2.846 1.845-3.321 3.291c-.097.265-.145.677-.143.962c0 1.176.787 2 1.8 2c1.259 0 3-1 4.5-1s3.241 1 4.5 1c1.013 0 1.8-.823 1.8-2c0-.285-.049-.697-.146-.962c-.475-1.451-2.512-1.835-3.454-3.538"/>
                    <path d="M20.188 8.082a1.039 1.039 0 0 0-.406-.082h-.015c-.735.012-1.56.75-1.993 1.866c-.519 1.335-.28 2.7.538 3.052c.129.055.267.082.406.082c.739 0 1.575-.742 2.011-1.866c.516-1.335.273-2.7-.54-3.052"/>
                    <path d="M9.474 9c.055 0 .109 0 .163-.011c.944-.128 1.533-1.346 1.32-2.722c-.203-1.297-1.047-2.267-1.932-2.267c-.055 0-.109 0-.163.011c-.944.128-1.533 1.346-1.32 2.722c.204 1.293 1.048 2.267 1.932 2.267"/>
                    <path d="M14.526 9c.055 0 .109 0 .163-.011c.944-.128 1.533-1.346 1.32-2.722c-.203-1.297-1.047-2.267-1.932-2.267c-.055 0-.109 0-.163.011c-.944.128-1.533 1.346-1.32 2.722c.204 1.293 1.048 2.267 1.932 2.267"/>
                    <path d="M3.812 8.082a1.039 1.039 0 0 1 .406-.082h.015c.735.012 1.56.75 1.993 1.866c.519 1.335.28 2.7-.538 3.052a1.039 1.039 0 0 1-.406.082c-.739 0-1.575-.742-2.011-1.866c-.516-1.335-.273-2.7.54-3.052"/>
                  </svg>
                  Zobacz zwierzęta
                </Button>
                {showVolunteerContent && (
                  <Button as={Link} to="/volunteer" variant="outline" size="lg">
                    <UserGroupIcon className="w-5 h-5 mr-2" />
                    Zostań wolontariuszem
                  </Button>
                )}
              </div>
            </div>
            <div>
              <img
                src="https://images.unsplash.com/photo-1587300003388-59208cc962cb?w=800&h=600&fit=crop"
                alt="Szczęśliwy pies czekający na adopcję"
                className="rounded-2xl shadow-2xl w-full"
              />
            </div>
          </div>
        </PageContainer>
      </section>

      {/* Blog Section */}
      <section className="py-16 md:py-24 bg-white">
        <PageContainer>
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Blog edukacyjny</h2>
            <p className="text-gray-600 max-w-xl mx-auto">
              Porady dotyczące adopcji, opieki nad zwierzętami i inspirujące historie naszych podopiecznych.
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-8">
            {recentPosts.map((post) => (
              <Link key={post.id} to={`/blog/${post.id}`} className="group">
                <Card className="overflow-hidden hover:shadow-lg transition-shadow">
                  <div className="aspect-video overflow-hidden">
                    <img
                      src={post.imageUrl}
                      alt={post.title}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                    />
                  </div>
                  <div className="p-6">
                    <span className={`inline-block px-3 py-1 rounded-full text-xs font-medium mb-3 ${categoryColors[post.category]}`}>
                      {categoryLabels[post.category]}
                    </span>
                    <h3 className="font-semibold text-gray-900 mb-2 group-hover:text-primary-600 transition-colors">
                      {post.title}
                    </h3>
                    <p className="text-gray-600 text-sm mb-4 line-clamp-2">{post.excerpt}</p>
                    <div className="flex items-center gap-4 text-xs text-gray-400">
                      <span className="flex items-center gap-1">
                        <CalendarIcon className="w-4 h-4" />
                        {formatDate(post.date)}
                      </span>
                      <span className="flex items-center gap-1">
                        <ClockIcon className="w-4 h-4" />
                        {post.readTime} min
                      </span>
                    </div>
                  </div>
                </Card>
              </Link>
            ))}
          </div>
          <div className="text-center mt-8">
            <Button as={Link} to="/blog" variant="outline">
              Zobacz wszystkie wpisy
            </Button>
          </div>
        </PageContainer>
      </section>

      {/* Features Section */}
      <section className="py-16 md:py-24 bg-gray-50">
        <PageContainer>
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Jak możesz pomóc?</h2>
            <p className="text-gray-600 max-w-xl mx-auto">
              Każda forma wsparcia ma ogromne znaczenie dla naszych podopiecznych.
            </p>
          </div>
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-8">
            {features.map((feature) => (
              <Card key={feature.title} className="p-6 hover:shadow-lg transition-shadow bg-white">
                <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center mb-4">
                  <feature.icon className="w-6 h-6 text-primary-600" />
                </div>
                <h3 className="text-xl font-semibold text-gray-900 mb-2">{feature.title}</h3>
                <p className="text-gray-600 mb-4">{feature.description}</p>
                <Link
                  to={feature.link}
                  className="inline-flex items-center text-primary-600 font-semibold hover:text-primary-700"
                >
                  {feature.linkText}
                  <ArrowRightIcon className="w-4 h-4 ml-2" />
                </Link>
              </Card>
            ))}
          </div>
        </PageContainer>
      </section>
    </div>
  );
}
