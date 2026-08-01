export interface AdminProduct {
  id: number;
  name: string;
  slug: string;
  price: number;
  compareAtPrice: number | null;
  categoryName: string;
  collectionName: string;
  isActive: boolean;
  isFeatured: boolean;
  totalStock: number;
  imageCount: number;
  createdAt: string;
}

export interface AdminVariant {
  id: number;
  sizeLabel: string;
  sizeSortOrder: number;
  sku: string;
  stock: number;
}

export interface AdminImage {
  id: number;
  publicId: string;
  altText: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface AdminOrder {
  id: number;
  orderNumber: string;
  status: number;
  total: number;
  itemCount: number;
  customerName: string;
  shippingCity: string;
  paymentMethod: number;
  paymentStatus: number;
  createdAt: string;
  nextStates: number[];
}

export interface LowStock {
  variantId: number;
  productName: string;
  sizeLabel: string;
  sku: string;
  stock: number;
}

export interface Dashboard {
  salesTotal: number;
  paidOrders: number;
  pendingOrders: number;
  totalProducts: number;
  inactiveProducts: number;
  outOfStockProducts: number;
  lowStock: LowStock[];
}

export interface SaveProduct {
  name: string;
  slug: string;
  description: string;
  price: number;
  compareAtPrice: number | null;
  categoryId: number;
  collectionId: number;
  isFeatured: boolean;
}
