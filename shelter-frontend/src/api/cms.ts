import { get, post, put, del, buildQueryString } from './client';

// Types
export interface BlogPost {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  category: BlogCategory;
  categoryLabel: string;
  mainImageUrl: string | null;
  isPublished: boolean;
  publishedAt: string | null;
  authorName: string | null;
  viewCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export type BlogCategory =
  | 'Adopcja'
  | 'Porady'
  | 'Zdrowie'
  | 'Historie'
  | 'Wydarzenia'
  | 'Aktualnosci';

export interface FaqItem {
  id: string;
  question: string;
  answer: string;
  category: FaqCategory;
  categoryLabel: string;
  displayOrder: number;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export type FaqCategory =
  | 'Adopcja'
  | 'OpikaZwierzat'
  | 'Wolontariat'
  | 'Darowizny'
  | 'Kontakt'
  | 'ProceduraAdopcji';

export interface ContentPage {
  id: string;
  title: string;
  slug: string;
  content: string;
  metaDescription: string | null;
  metaKeywords: string | null;
  isPublished: boolean;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Blog API
interface GetBlogPostsParams {
  page?: number;
  pageSize?: number;
  category?: BlogCategory;
  publishedOnly?: boolean;
}

interface CreateBlogPostDto {
  title: string;
  excerpt: string;
  content: string;
  category: BlogCategory;
  mainImageUrl?: string;
  isPublished?: boolean;
}

interface UpdateBlogPostDto {
  title?: string;
  excerpt?: string;
  content?: string;
  category?: BlogCategory;
  mainImageUrl?: string;
}

export const blogApi = {
  getAll: (params: GetBlogPostsParams = {}) =>
    get<PagedResult<BlogPost>>(`/cms/blog${buildQueryString(params)}`),

  getById: (id: string) =>
    get<BlogPost>(`/cms/blog/${id}`),

  getBySlug: (slug: string) =>
    get<BlogPost>(`/cms/blog/slug/${slug}`),

  create: (data: CreateBlogPostDto) =>
    post<BlogPost>('/cms/blog', data),

  update: (id: string, data: UpdateBlogPostDto) =>
    put<BlogPost>(`/cms/blog/${id}`, data),

  delete: (id: string) =>
    del<void>(`/cms/blog/${id}`),

  publish: (id: string) =>
    post<BlogPost>(`/cms/blog/${id}/publish`),

  unpublish: (id: string) =>
    post<BlogPost>(`/cms/blog/${id}/unpublish`),
};

// FAQ API
interface GetFaqParams {
  category?: FaqCategory;
  publishedOnly?: boolean;
}

interface CreateFaqDto {
  question: string;
  answer: string;
  category: FaqCategory;
  displayOrder?: number;
  isPublished?: boolean;
}

interface UpdateFaqDto {
  question?: string;
  answer?: string;
  category?: FaqCategory;
  displayOrder?: number;
}

export const faqApi = {
  getAll: (params: GetFaqParams = {}) =>
    get<FaqItem[]>(`/cms/faq${buildQueryString(params)}`),

  getById: (id: string) =>
    get<FaqItem>(`/cms/faq/${id}`),

  create: (data: CreateFaqDto) =>
    post<FaqItem>('/cms/faq', data),

  update: (id: string, data: UpdateFaqDto) =>
    put<FaqItem>(`/cms/faq/${id}`, data),

  delete: (id: string) =>
    del<void>(`/cms/faq/${id}`),

  publish: (id: string) =>
    post<FaqItem>(`/cms/faq/${id}/publish`),

  unpublish: (id: string) =>
    post<FaqItem>(`/cms/faq/${id}/unpublish`),

  reorder: (items: { id: string; displayOrder: number }[]) =>
    post<void>('/cms/faq/reorder', { items }),
};

// Pages API
interface GetPagesParams {
  publishedOnly?: boolean;
}

interface CreatePageDto {
  title: string;
  slug: string;
  content: string;
  metaDescription?: string;
  metaKeywords?: string;
  isPublished?: boolean;
}

interface UpdatePageDto {
  title?: string;
  content?: string;
  metaDescription?: string;
  metaKeywords?: string;
}

export const pagesApi = {
  getAll: (params: GetPagesParams = {}) =>
    get<ContentPage[]>(`/cms/pages${buildQueryString(params)}`),

  getById: (id: string) =>
    get<ContentPage>(`/cms/pages/${id}`),

  getBySlug: (slug: string) =>
    get<ContentPage>(`/cms/pages/slug/${slug}`),

  create: (data: CreatePageDto) =>
    post<ContentPage>('/cms/pages', data),

  update: (id: string, data: UpdatePageDto) =>
    put<ContentPage>(`/cms/pages/${id}`, data),

  delete: (id: string) =>
    del<void>(`/cms/pages/${id}`),

  publish: (id: string) =>
    post<ContentPage>(`/cms/pages/${id}/publish`),

  unpublish: (id: string) =>
    post<ContentPage>(`/cms/pages/${id}/unpublish`),
};

// Category labels
export const blogCategoryLabels: Record<BlogCategory, string> = {
  Adopcja: 'Adopcja',
  Porady: 'Porady',
  Zdrowie: 'Zdrowie',
  Historie: 'Historie sukcesu',
  Wydarzenia: 'Wydarzenia',
  Aktualnosci: 'Aktualności',
};

export const faqCategoryLabels: Record<FaqCategory, string> = {
  Adopcja: 'Adopcja',
  OpikaZwierzat: 'Opieka nad zwierzętami',
  Wolontariat: 'Wolontariat',
  Darowizny: 'Darowizny',
  Kontakt: 'Kontakt',
  ProceduraAdopcji: 'Procedura adopcji',
};
