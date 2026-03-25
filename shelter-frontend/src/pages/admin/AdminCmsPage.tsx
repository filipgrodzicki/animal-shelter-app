import { useState, useEffect } from 'react';
import {
  PlusIcon,
  PencilIcon,
  TrashIcon,
  EyeIcon,
  EyeSlashIcon,
  NewspaperIcon,
  QuestionMarkCircleIcon,
  DocumentTextIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Badge, Input, Select, Spinner, Modal } from '@/components/common';
import {
  blogApi,
  faqApi,
  pagesApi,
  BlogPost,
  FaqItem,
  ContentPage,
  BlogCategory,
  FaqCategory,
  blogCategoryLabels,
  faqCategoryLabels,
} from '@/api/cms';
import { getErrorMessage } from '@/api/client';

type Tab = 'blog' | 'faq' | 'pages';

const blogCategoryOptions = [
  { value: '', label: 'Wszystkie kategorie' },
  ...Object.entries(blogCategoryLabels).map(([value, label]) => ({ value, label })),
];

const faqCategoryOptions = [
  { value: '', label: 'Wszystkie kategorie' },
  ...Object.entries(faqCategoryLabels).map(([value, label]) => ({ value, label })),
];

export function AdminCmsPage() {
  const [activeTab, setActiveTab] = useState<Tab>('blog');

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Zarządzanie treścią (CMS)</h1>
        <p className="mt-2 text-gray-600">Zarządzaj blogiem, FAQ i stronami statycznymi</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex gap-6">
          <button
            onClick={() => setActiveTab('blog')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'blog'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <NewspaperIcon className="h-5 w-5" />
            Blog
          </button>
          <button
            onClick={() => setActiveTab('faq')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'faq'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <QuestionMarkCircleIcon className="h-5 w-5" />
            FAQ
          </button>
          <button
            onClick={() => setActiveTab('pages')}
            className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'pages'
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <DocumentTextIcon className="h-5 w-5" />
            Strony
          </button>
        </nav>
      </div>

      {/* Tab content */}
      {activeTab === 'blog' && <BlogManagement />}
      {activeTab === 'faq' && <FaqManagement />}
      {activeTab === 'pages' && <PagesManagement />}
    </PageContainer>
  );
}

// Blog Management Component
function BlogManagement() {
  const [posts, setPosts] = useState<BlogPost[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPost, setEditingPost] = useState<BlogPost | null>(null);

  const fetchPosts = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await blogApi.getAll({
        category: categoryFilter as BlogCategory || undefined,
        publishedOnly: false,
      });
      setPosts(result.items);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchPosts();
  }, [categoryFilter]);

  const handleTogglePublish = async (post: BlogPost) => {
    try {
      if (post.isPublished) {
        await blogApi.unpublish(post.id);
      } else {
        await blogApi.publish(post.id);
      }
      fetchPosts();
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Czy na pewno chcesz usunąć ten wpis?')) return;
    try {
      await blogApi.delete(id);
      fetchPosts();
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const handleEdit = (post: BlogPost) => {
    setEditingPost(post);
    setIsModalOpen(true);
  };

  const handleCreate = () => {
    setEditingPost(null);
    setIsModalOpen(true);
  };

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <Button onClick={handleCreate} leftIcon={<PlusIcon className="h-5 w-5" />}>
          Dodaj wpis
        </Button>
        <Select
          options={blogCategoryOptions}
          value={categoryFilter}
          onChange={(e) => setCategoryFilter(e.target.value)}
          wrapperClassName="w-52"
        />
      </div>

      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : posts.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Brak wpisów</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Tytuł
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Kategoria
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Data
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                    Akcje
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {posts.map((post) => (
                  <tr key={post.id} className="hover:bg-gray-50">
                    <td className="px-4 py-4">
                      <div>
                        <p className="font-medium text-gray-900">{post.title}</p>
                        <p className="text-sm text-gray-500 truncate max-w-xs">{post.excerpt}</p>
                      </div>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <Badge variant="blue">{post.categoryLabel}</Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <Badge variant={post.isPublished ? 'green' : 'gray'}>
                        {post.isPublished ? 'Opublikowany' : 'Szkic'}
                      </Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(post.createdAt).toLocaleDateString('pl-PL')}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleTogglePublish(post)}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title={post.isPublished ? 'Cofnij publikację' : 'Opublikuj'}
                        >
                          {post.isPublished ? (
                            <EyeSlashIcon className="h-5 w-5" />
                          ) : (
                            <EyeIcon className="h-5 w-5" />
                          )}
                        </button>
                        <button
                          onClick={() => handleEdit(post)}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title="Edytuj"
                        >
                          <PencilIcon className="h-5 w-5" />
                        </button>
                        <button
                          onClick={() => handleDelete(post.id)}
                          className="p-1.5 text-gray-500 hover:text-red-600 hover:bg-gray-100 rounded"
                          title="Usuń"
                        >
                          <TrashIcon className="h-5 w-5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <BlogFormModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingPost(null);
        }}
        onSuccess={() => {
          setIsModalOpen(false);
          setEditingPost(null);
          fetchPosts();
        }}
        post={editingPost}
      />
    </>
  );
}

// FAQ Management Component
function FaqManagement() {
  const [items, setItems] = useState<FaqItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<FaqItem | null>(null);

  const fetchItems = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await faqApi.getAll({
        category: categoryFilter as FaqCategory || undefined,
        publishedOnly: false,
      });
      setItems(result);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchItems();
  }, [categoryFilter]);

  const handleTogglePublish = async (item: FaqItem) => {
    try {
      if (item.isPublished) {
        await faqApi.unpublish(item.id);
      } else {
        await faqApi.publish(item.id);
      }
      fetchItems();
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Czy na pewno chcesz usunąć to pytanie?')) return;
    try {
      await faqApi.delete(id);
      fetchItems();
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  return (
    <>
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <Button
          onClick={() => {
            setEditingItem(null);
            setIsModalOpen(true);
          }}
          leftIcon={<PlusIcon className="h-5 w-5" />}
        >
          Dodaj pytanie
        </Button>
        <Select
          options={faqCategoryOptions}
          value={categoryFilter}
          onChange={(e) => setCategoryFilter(e.target.value)}
          wrapperClassName="w-52"
        />
      </div>

      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : items.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Brak pytań FAQ</div>
        ) : (
          <div className="divide-y divide-gray-200">
            {items.map((item) => (
              <div key={item.id} className="p-4 hover:bg-gray-50">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <Badge variant="blue">{item.categoryLabel}</Badge>
                      <Badge variant={item.isPublished ? 'green' : 'gray'}>
                        {item.isPublished ? 'Opublikowane' : 'Szkic'}
                      </Badge>
                      <span className="text-xs text-gray-400">#{item.displayOrder}</span>
                    </div>
                    <h3 className="font-medium text-gray-900">{item.question}</h3>
                    <p className="text-sm text-gray-600 mt-1 line-clamp-2">{item.answer}</p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleTogglePublish(item)}
                      className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                      title={item.isPublished ? 'Cofnij publikację' : 'Opublikuj'}
                    >
                      {item.isPublished ? (
                        <EyeSlashIcon className="h-5 w-5" />
                      ) : (
                        <EyeIcon className="h-5 w-5" />
                      )}
                    </button>
                    <button
                      onClick={() => {
                        setEditingItem(item);
                        setIsModalOpen(true);
                      }}
                      className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                      title="Edytuj"
                    >
                      <PencilIcon className="h-5 w-5" />
                    </button>
                    <button
                      onClick={() => handleDelete(item.id)}
                      className="p-1.5 text-gray-500 hover:text-red-600 hover:bg-gray-100 rounded"
                      title="Usuń"
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      <FaqFormModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingItem(null);
        }}
        onSuccess={() => {
          setIsModalOpen(false);
          setEditingItem(null);
          fetchItems();
        }}
        item={editingItem}
      />
    </>
  );
}

// Pages Management Component
function PagesManagement() {
  const [pages, setPages] = useState<ContentPage[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPage, setEditingPage] = useState<ContentPage | null>(null);

  const fetchPages = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await pagesApi.getAll({ publishedOnly: false });
      setPages(result);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchPages();
  }, []);

  const handleTogglePublish = async (page: ContentPage) => {
    try {
      if (page.isPublished) {
        await pagesApi.unpublish(page.id);
      } else {
        await pagesApi.publish(page.id);
      }
      fetchPages();
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Czy na pewno chcesz usunąć tę stronę?')) return;
    try {
      await pagesApi.delete(id);
      fetchPages();
    } catch (err) {
      alert(getErrorMessage(err));
    }
  };

  return (
    <>
      <div className="flex flex-col sm:flex-row gap-4 mb-6">
        <Button
          onClick={() => {
            setEditingPage(null);
            setIsModalOpen(true);
          }}
          leftIcon={<PlusIcon className="h-5 w-5" />}
        >
          Dodaj stronę
        </Button>
      </div>

      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : pages.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Brak stron</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Tytuł
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Slug
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Data
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                    Akcje
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {pages.map((page) => (
                  <tr key={page.id} className="hover:bg-gray-50">
                    <td className="px-4 py-4">
                      <p className="font-medium text-gray-900">{page.title}</p>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      /{page.slug}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <Badge variant={page.isPublished ? 'green' : 'gray'}>
                        {page.isPublished ? 'Opublikowana' : 'Szkic'}
                      </Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(page.createdAt).toLocaleDateString('pl-PL')}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleTogglePublish(page)}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title={page.isPublished ? 'Cofnij publikację' : 'Opublikuj'}
                        >
                          {page.isPublished ? (
                            <EyeSlashIcon className="h-5 w-5" />
                          ) : (
                            <EyeIcon className="h-5 w-5" />
                          )}
                        </button>
                        <button
                          onClick={() => {
                            setEditingPage(page);
                            setIsModalOpen(true);
                          }}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title="Edytuj"
                        >
                          <PencilIcon className="h-5 w-5" />
                        </button>
                        <button
                          onClick={() => handleDelete(page.id)}
                          className="p-1.5 text-gray-500 hover:text-red-600 hover:bg-gray-100 rounded"
                          title="Usuń"
                        >
                          <TrashIcon className="h-5 w-5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <PageFormModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingPage(null);
        }}
        onSuccess={() => {
          setIsModalOpen(false);
          setEditingPage(null);
          fetchPages();
        }}
        page={editingPage}
      />
    </>
  );
}

// Form Modals
interface BlogFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  post: BlogPost | null;
}

function BlogFormModal({ isOpen, onClose, onSuccess, post }: BlogFormModalProps) {
  const [formData, setFormData] = useState({
    title: '',
    excerpt: '',
    content: '',
    category: 'Aktualnosci' as BlogCategory,
    mainImageUrl: '',
    isPublished: false,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (post) {
      setFormData({
        title: post.title,
        excerpt: post.excerpt,
        content: post.content,
        category: post.category,
        mainImageUrl: post.mainImageUrl || '',
        isPublished: post.isPublished,
      });
    } else {
      setFormData({
        title: '',
        excerpt: '',
        content: '',
        category: 'Aktualnosci',
        mainImageUrl: '',
        isPublished: false,
      });
    }
  }, [post, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      if (post) {
        await blogApi.update(post.id, formData);
      } else {
        await blogApi.create(formData);
      }
      onSuccess();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={post ? 'Edytuj wpis' : 'Dodaj wpis'} size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && <div className="p-3 bg-red-50 text-red-600 rounded">{error}</div>}

        <Input
          label="Tytuł"
          value={formData.title}
          onChange={(e) => setFormData({ ...formData, title: e.target.value })}
          required
        />

        <Select
          label="Kategoria"
          options={Object.entries(blogCategoryLabels).map(([value, label]) => ({ value, label }))}
          value={formData.category}
          onChange={(e) => setFormData({ ...formData, category: e.target.value as BlogCategory })}
        />

        <Input
          label="Zajawka"
          value={formData.excerpt}
          onChange={(e) => setFormData({ ...formData, excerpt: e.target.value })}
          required
        />

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Treść</label>
          <textarea
            value={formData.content}
            onChange={(e) => setFormData({ ...formData, content: e.target.value })}
            rows={10}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            required
          />
        </div>

        <Input
          label="URL zdjęcia głównego"
          value={formData.mainImageUrl}
          onChange={(e) => setFormData({ ...formData, mainImageUrl: e.target.value })}
          placeholder="https://..."
        />

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="isPublished"
            checked={formData.isPublished}
            onChange={(e) => setFormData({ ...formData, isPublished: e.target.checked })}
            className="h-4 w-4 text-primary-600 rounded"
          />
          <label htmlFor="isPublished" className="text-sm text-gray-700">
            Opublikuj od razu
          </label>
        </div>

        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose}>
            Anuluj
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Zapisuję...' : post ? 'Zapisz zmiany' : 'Dodaj wpis'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

interface FaqFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  item: FaqItem | null;
}

function FaqFormModal({ isOpen, onClose, onSuccess, item }: FaqFormModalProps) {
  const [formData, setFormData] = useState({
    question: '',
    answer: '',
    category: 'Adopcja' as FaqCategory,
    displayOrder: 0,
    isPublished: false,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (item) {
      setFormData({
        question: item.question,
        answer: item.answer,
        category: item.category,
        displayOrder: item.displayOrder,
        isPublished: item.isPublished,
      });
    } else {
      setFormData({
        question: '',
        answer: '',
        category: 'Adopcja',
        displayOrder: 0,
        isPublished: false,
      });
    }
  }, [item, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      if (item) {
        await faqApi.update(item.id, formData);
      } else {
        await faqApi.create(formData);
      }
      onSuccess();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={item ? 'Edytuj pytanie' : 'Dodaj pytanie'}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && <div className="p-3 bg-red-50 text-red-600 rounded">{error}</div>}

        <Select
          label="Kategoria"
          options={Object.entries(faqCategoryLabels).map(([value, label]) => ({ value, label }))}
          value={formData.category}
          onChange={(e) => setFormData({ ...formData, category: e.target.value as FaqCategory })}
        />

        <Input
          label="Pytanie"
          value={formData.question}
          onChange={(e) => setFormData({ ...formData, question: e.target.value })}
          required
        />

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Odpowiedź</label>
          <textarea
            value={formData.answer}
            onChange={(e) => setFormData({ ...formData, answer: e.target.value })}
            rows={5}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            required
          />
        </div>

        <Input
          label="Kolejność wyświetlania"
          type="number"
          value={formData.displayOrder.toString()}
          onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
        />

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="faqIsPublished"
            checked={formData.isPublished}
            onChange={(e) => setFormData({ ...formData, isPublished: e.target.checked })}
            className="h-4 w-4 text-primary-600 rounded"
          />
          <label htmlFor="faqIsPublished" className="text-sm text-gray-700">
            Opublikuj od razu
          </label>
        </div>

        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose}>
            Anuluj
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Zapisuję...' : item ? 'Zapisz zmiany' : 'Dodaj pytanie'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

interface PageFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  page: ContentPage | null;
}

function PageFormModal({ isOpen, onClose, onSuccess, page }: PageFormModalProps) {
  const [formData, setFormData] = useState({
    title: '',
    slug: '',
    content: '',
    metaDescription: '',
    metaKeywords: '',
    isPublished: false,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (page) {
      setFormData({
        title: page.title,
        slug: page.slug,
        content: page.content,
        metaDescription: page.metaDescription || '',
        metaKeywords: page.metaKeywords || '',
        isPublished: page.isPublished,
      });
    } else {
      setFormData({
        title: '',
        slug: '',
        content: '',
        metaDescription: '',
        metaKeywords: '',
        isPublished: false,
      });
    }
  }, [page, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      if (page) {
        await pagesApi.update(page.id, {
          title: formData.title,
          content: formData.content,
          metaDescription: formData.metaDescription,
          metaKeywords: formData.metaKeywords,
        });
      } else {
        await pagesApi.create(formData);
      }
      onSuccess();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  const generateSlug = () => {
    const slug = formData.title
      .toLowerCase()
      .replace(/[ąä]/g, 'a')
      .replace(/[ćç]/g, 'c')
      .replace(/[ęé]/g, 'e')
      .replace(/[łl]/g, 'l')
      .replace(/[ńñ]/g, 'n')
      .replace(/[óö]/g, 'o')
      .replace(/[śß]/g, 's')
      .replace(/[źżz]/g, 'z')
      .replace(/[üú]/g, 'u')
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .trim();
    setFormData({ ...formData, slug });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={page ? 'Edytuj stronę' : 'Dodaj stronę'} size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && <div className="p-3 bg-red-50 text-red-600 rounded">{error}</div>}

        <Input
          label="Tytuł"
          value={formData.title}
          onChange={(e) => setFormData({ ...formData, title: e.target.value })}
          onBlur={() => !page && !formData.slug && generateSlug()}
          required
        />

        {!page && (
          <div className="flex gap-2">
            <div className="flex-1">
              <Input
                label="Slug (URL)"
                value={formData.slug}
                onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                required
              />
            </div>
            <div className="pt-6">
              <Button type="button" variant="outline" onClick={generateSlug}>
                Generuj
              </Button>
            </div>
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Treść</label>
          <textarea
            value={formData.content}
            onChange={(e) => setFormData({ ...formData, content: e.target.value })}
            rows={10}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            required
          />
        </div>

        <Input
          label="Meta Description (SEO)"
          value={formData.metaDescription}
          onChange={(e) => setFormData({ ...formData, metaDescription: e.target.value })}
        />

        <Input
          label="Meta Keywords (SEO)"
          value={formData.metaKeywords}
          onChange={(e) => setFormData({ ...formData, metaKeywords: e.target.value })}
        />

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="pageIsPublished"
            checked={formData.isPublished}
            onChange={(e) => setFormData({ ...formData, isPublished: e.target.checked })}
            className="h-4 w-4 text-primary-600 rounded"
          />
          <label htmlFor="pageIsPublished" className="text-sm text-gray-700">
            Opublikuj od razu
          </label>
        </div>

        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose}>
            Anuluj
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Zapisuję...' : page ? 'Zapisz zmiany' : 'Dodaj stronę'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
