import { useState, useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { MagnifyingGlassIcon, CalendarIcon, ClockIcon } from '@heroicons/react/24/outline';
import { PageContainer, PageHeader } from '@/components/layout';
import { Card } from '@/components/common';
import { blogPosts } from '@/data/blogPosts';
import { BlogCategory, categoryLabels, categoryColors } from '@/types';

const categories: { value: BlogCategory | 'all'; label: string }[] = [
  { value: 'all', label: 'Wszystkie' },
  { value: 'adopcja', label: 'Adopcja' },
  { value: 'porady', label: 'Porady' },
  { value: 'zdrowie', label: 'Zdrowie' },
  { value: 'historie', label: 'Historie' },
  { value: 'wydarzenia', label: 'Wydarzenia' },
];

export function BlogPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialCategory = (searchParams.get('category') as BlogCategory | 'all') || 'all';
  const initialSearch = searchParams.get('search') || '';

  const [selectedCategory, setSelectedCategory] = useState<BlogCategory | 'all'>(initialCategory);
  const [searchQuery, setSearchQuery] = useState(initialSearch);

  const filteredPosts = useMemo(() => {
    return blogPosts.filter((post) => {
      const matchesCategory = selectedCategory === 'all' || post.category === selectedCategory;
      const matchesSearch =
        searchQuery === '' ||
        post.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        post.excerpt.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesCategory && matchesSearch;
    });
  }, [selectedCategory, searchQuery]);

  const handleCategoryChange = (category: BlogCategory | 'all') => {
    setSelectedCategory(category);
    const params = new URLSearchParams(searchParams);
    if (category === 'all') {
      params.delete('category');
    } else {
      params.set('category', category);
    }
    setSearchParams(params, { replace: true });
  };

  const handleSearchChange = (value: string) => {
    setSearchQuery(value);
    const params = new URLSearchParams(searchParams);
    if (value === '') {
      params.delete('search');
    } else {
      params.set('search', value);
    }
    setSearchParams(params, { replace: true });
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('pl-PL', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <PageContainer>
      <PageHeader
        title="Blog edukacyjny"
        description="Porady dotyczące adopcji, opieki nad zwierzętami i inspirujące historie naszych podopiecznych."
      />

      {/* Filters */}
      <div className="mb-8 space-y-4">
        {/* Search */}
        <div className="max-w-md relative">
          <MagnifyingGlassIcon className="w-5 h-5 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Szukaj artykułów..."
            value={searchQuery}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
          />
        </div>

        {/* Category filters */}
        <div className="flex flex-wrap gap-2">
          {categories.map((category) => (
            <button
              key={category.value}
              onClick={() => handleCategoryChange(category.value)}
              className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                selectedCategory === category.value
                  ? 'bg-primary-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {category.label}
            </button>
          ))}
        </div>
      </div>

      {/* Results count */}
      <p className="text-gray-600 mb-6">
        {filteredPosts.length === 0
          ? 'Nie znaleziono artykułów'
          : filteredPosts.length === 1
          ? '1 artykuł'
          : `${filteredPosts.length} artykułów`}
      </p>

      {/* Blog grid */}
      {filteredPosts.length > 0 ? (
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
          {filteredPosts.map((post) => (
            <Link key={post.id} to={`/blog/${post.id}`} className="group">
              <Card className="overflow-hidden hover:shadow-lg transition-shadow h-full">
                <div className="aspect-video overflow-hidden">
                  <img
                    src={post.imageUrl}
                    alt={post.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                </div>
                <div className="p-6">
                  <span
                    className={`inline-block px-3 py-1 rounded-full text-xs font-medium mb-3 ${categoryColors[post.category]}`}
                  >
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
      ) : (
        <div className="text-center py-12">
          <p className="text-gray-500 mb-4">
            Nie znaleziono artykułów spełniających kryteria wyszukiwania.
          </p>
          <button
            onClick={() => {
              setSelectedCategory('all');
              setSearchQuery('');
              setSearchParams({}, { replace: true });
            }}
            className="text-primary-600 font-medium hover:text-primary-700"
          >
            Wyczyść filtry
          </button>
        </div>
      )}
    </PageContainer>
  );
}
