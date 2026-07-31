export type SizeSystem = "Footwear" | "Apparel";

export interface Category {
  id: number;
  name: string;
  slug: string;
  sizeSystem: SizeSystem;
  displayOrder: number;
}

export interface Collection {
  id: number;
  name: string;
  slug: string;
}

export interface Size {
  id: number;
  sizeSystem: SizeSystem;
  label: string;
  sortOrder: number;
}

export interface ProductImage {
  publicId: string;
  altText: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface ProductVariant {
  id: number;
  sizeLabel: string;
  sizeSortOrder: number;
  sku: string;
  stock: number;
  isInStock: boolean;
  isLowStock: boolean;
}

export interface ProductListItem {
  id: number;
  name: string;
  slug: string;
  price: number;
  compareAtPrice: number | null;
  categoryName: string;
  categorySlug: string;
  collectionName: string;
  isFeatured: boolean;
  hasStock: boolean;
  primaryImage: ProductImage | null;
  isOnSale: boolean;
}

export interface ProductDetail {
  id: number;
  name: string;
  slug: string;
  description: string;
  price: number;
  compareAtPrice: number | null;
  categoryName: string;
  categorySlug: string;
  sizeSystem: SizeSystem;
  collectionName: string;
  collectionSlug: string;
  isFeatured: boolean;
  variants: ProductVariant[];
  images: ProductImage[];
  isOnSale: boolean;
  hasStock: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export type ProductSort = "newest" | "price_asc" | "price_desc" | "name";

export interface ProductQuery {
  category?: string;
  collection?: string;
  size?: string;
  minPrice?: number;
  maxPrice?: number;
  search?: string;
  sort?: ProductSort;
  page?: number;
  pageSize?: number;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
