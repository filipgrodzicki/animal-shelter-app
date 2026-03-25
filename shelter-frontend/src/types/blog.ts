export interface BlogPost {
  id: string;
  title: string;
  excerpt: string;
  content: string;
  author: string;
  date: string;
  category: BlogCategory;
  imageUrl: string;
  readTime: number;
}

export type BlogCategory =
  | 'adopcja'
  | 'porady'
  | 'zdrowie'
  | 'historie'
  | 'wydarzenia';

export const categoryLabels: Record<BlogCategory, string> = {
  adopcja: 'Adopcja',
  porady: 'Porady',
  zdrowie: 'Zdrowie',
  historie: 'Historie',
  wydarzenia: 'Wydarzenia',
};

export const categoryColors: Record<BlogCategory, string> = {
  adopcja: 'bg-rose-100 text-rose-700',
  porady: 'bg-blue-100 text-blue-700',
  zdrowie: 'bg-green-100 text-green-700',
  historie: 'bg-amber-100 text-amber-700',
  wydarzenia: 'bg-purple-100 text-purple-700',
};
