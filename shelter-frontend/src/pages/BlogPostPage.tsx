import { useParams, Link, useNavigate } from 'react-router-dom';
import {
  ArrowLeftIcon,
  CalendarIcon,
  ClockIcon,
  UserIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card } from '@/components/common';
import { getBlogPostById, getRecentBlogPosts } from '@/data/blogPosts';
import { categoryLabels, categoryColors } from '@/types';

function renderMarkdown(content: string): React.ReactNode {
  const lines = content.trim().split('\n');
  const elements: React.ReactNode[] = [];
  let currentList: string[] = [];
  let listType: 'ul' | 'ol' | null = null;
  let key = 0;

  const flushList = () => {
    if (currentList.length > 0 && listType) {
      const ListTag = listType;
      elements.push(
        <ListTag key={key++} className={listType === 'ul' ? 'list-disc list-inside space-y-1 my-4' : 'list-decimal list-inside space-y-1 my-4'}>
          {currentList.map((item, i) => (
            <li key={i} className="text-gray-700">{renderInlineMarkdown(item)}</li>
          ))}
        </ListTag>
      );
      currentList = [];
      listType = null;
    }
  };

  const renderInlineMarkdown = (text: string): React.ReactNode => {
    // Bold
    const parts = text.split(/(\*\*[^*]+\*\*)/g);
    return parts.map((part, i) => {
      if (part.startsWith('**') && part.endsWith('**')) {
        return <strong key={i} className="font-semibold text-gray-900">{part.slice(2, -2)}</strong>;
      }
      return part;
    });
  };

  for (const line of lines) {
    const trimmedLine = line.trim();

    if (!trimmedLine) {
      flushList();
      continue;
    }

    // Headers
    if (trimmedLine.startsWith('## ')) {
      flushList();
      elements.push(
        <h2 key={key++} className="text-2xl font-bold text-gray-900 mt-8 mb-4">
          {trimmedLine.slice(3)}
        </h2>
      );
      continue;
    }

    if (trimmedLine.startsWith('### ')) {
      flushList();
      elements.push(
        <h3 key={key++} className="text-xl font-semibold text-gray-900 mt-6 mb-3">
          {trimmedLine.slice(4)}
        </h3>
      );
      continue;
    }

    // Unordered list
    if (trimmedLine.startsWith('- ')) {
      if (listType !== 'ul') {
        flushList();
        listType = 'ul';
      }
      currentList.push(trimmedLine.slice(2));
      continue;
    }

    // Ordered list
    if (/^\d+\.\s/.test(trimmedLine)) {
      if (listType !== 'ol') {
        flushList();
        listType = 'ol';
      }
      currentList.push(trimmedLine.replace(/^\d+\.\s/, ''));
      continue;
    }

    // Horizontal rule
    if (trimmedLine === '---') {
      flushList();
      elements.push(<hr key={key++} className="my-8 border-gray-200" />);
      continue;
    }

    // Regular paragraph
    flushList();
    elements.push(
      <p key={key++} className="text-gray-700 my-4 leading-relaxed">
        {renderInlineMarkdown(trimmedLine)}
      </p>
    );
  }

  flushList();
  return elements;
}

export function BlogPostPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const post = id ? getBlogPostById(id) : undefined;

  if (!post) {
    return (
      <PageContainer className="py-16">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900 mb-4">
            Nie znaleziono artykulu
          </h1>
          <p className="text-gray-600 mb-8">
            Przepraszamy, ale szukany artykul nie istnieje.
          </p>
          <Button onClick={() => navigate('/')}>Wroc na strone glowna</Button>
        </div>
      </PageContainer>
    );
  }

  const recentPosts = getRecentBlogPosts(4).filter((p) => p.id !== post.id);

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <>
      {/* Hero section with image */}
      <section className="relative h-[400px] bg-gray-900">
        <img
          src={post.imageUrl}
          alt={post.title}
          className="w-full h-full object-cover opacity-60"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-gray-900/80 to-transparent" />
        <div className="absolute bottom-0 left-0 right-0 p-8">
          <PageContainer>
            <Link
              to="/"
              className="inline-flex items-center text-white/80 hover:text-white mb-4 transition-colors"
            >
              <ArrowLeftIcon className="h-4 w-4 mr-2" />
              Powrot do strony glownej
            </Link>
            <div className="mb-4">
              <span
                className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${categoryColors[post.category]}`}
              >
                {categoryLabels[post.category]}
              </span>
            </div>
            <h1 className="text-3xl md:text-4xl font-bold text-white mb-4">
              {post.title}
            </h1>
            <div className="flex flex-wrap items-center gap-4 text-white/80 text-sm">
              <span className="flex items-center">
                <UserIcon className="h-4 w-4 mr-1" />
                {post.author}
              </span>
              <span className="flex items-center">
                <CalendarIcon className="h-4 w-4 mr-1" />
                {formatDate(post.date)}
              </span>
              <span className="flex items-center">
                <ClockIcon className="h-4 w-4 mr-1" />
                {post.readTime} min czytania
              </span>
            </div>
          </PageContainer>
        </div>
      </section>

      {/* Content */}
      <section className="bg-warm-50 py-12">
        <PageContainer>
          <div className="grid lg:grid-cols-3 gap-8">
            {/* Main content */}
            <article className="lg:col-span-2">
              <Card className="p-8">
                <div className="max-w-none">
                  {renderMarkdown(post.content)}
                </div>
              </Card>
            </article>

            {/* Sidebar */}
            <aside className="space-y-6">
              {/* Author card */}
              <Card className="p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">
                  O autorze
                </h3>
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center">
                    <UserIcon className="h-6 w-6 text-primary-600" />
                  </div>
                  <div>
                    <p className="font-medium text-gray-900">{post.author}</p>
                    <p className="text-sm text-gray-500">
                      Zespol Schroniska "Bezpieczna Przystan"
                    </p>
                  </div>
                </div>
              </Card>

              {/* Recent posts */}
              {recentPosts.length > 0 && (
                <Card className="p-6">
                  <h3 className="text-lg font-semibold text-gray-900 mb-4">
                    Inne artykuly
                  </h3>
                  <div className="space-y-4">
                    {recentPosts.slice(0, 3).map((recentPost) => (
                      <Link
                        key={recentPost.id}
                        to={`/blog/${recentPost.id}`}
                        className="block group"
                      >
                        <div className="flex gap-3">
                          <img
                            src={recentPost.imageUrl}
                            alt={recentPost.title}
                            className="w-16 h-16 rounded-lg object-cover flex-shrink-0"
                          />
                          <div>
                            <h4 className="text-sm font-medium text-gray-900 group-hover:text-primary-600 transition-colors line-clamp-2">
                              {recentPost.title}
                            </h4>
                            <p className="text-xs text-gray-500 mt-1">
                              {formatDate(recentPost.date)}
                            </p>
                          </div>
                        </div>
                      </Link>
                    ))}
                  </div>
                </Card>
              )}

              {/* CTA */}
              <Card className="p-6 bg-primary-600 text-white">
                <h3 className="text-lg font-semibold mb-2">
                  Gotowy na adopcje?
                </h3>
                <p className="text-primary-100 text-sm mb-4">
                  Poznaj nasze podopieczne i znajdz swojego nowego przyjaciela.
                </p>
                <Button
                  as={Link}
                  to="/animals"
                  variant="outline"
                  className="w-full border-white text-white hover:bg-white hover:text-primary-600"
                >
                  Zobacz zwierzeta
                </Button>
              </Card>
            </aside>
          </div>
        </PageContainer>
      </section>
    </>
  );
}
