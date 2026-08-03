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

export interface AdminProductForm {
  id: number;
  name: string;
  slug: string;
  description: string;
  price: number;
  compareAtPrice: number | null;
  categoryId: number;
  collectionId: number;
  isActive: boolean;
  isFeatured: boolean;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
}

export interface Collection {
  id: number;
  name: string;
  slug: string;
}

export interface AdminOrderLine {
  productName: string;
  collectionName: string;
  sizeLabel: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  imagePublicId: string | null;
}

export interface AdminOrderPayment {
  method: number;
  status: number;
  amount: number;
  mbEntity: string | null;
  mbReference: string | null;
  mbWayPhone: string | null;
  cardLast4: string | null;
  expiresAt: string | null;
  confirmedAt: string | null;
}

export interface AdminOrderDetail {
  id: number;
  orderNumber: string;
  status: number;
  customerName: string;
  customerEmail: string;
  subtotal: number;
  shippingCost: number;
  total: number;
  shippingFullName: string;
  shippingLine1: string;
  shippingLine2: string | null;
  shippingPostalCode: string;
  shippingCity: string;
  shippingCountry: string;
  shippingPhone: string;
  createdAt: string;
  paidAt: string | null;
  shippedAt: string | null;
  items: AdminOrderLine[];
  payment: AdminOrderPayment;
  nextStates: number[];
}

export interface LowStock {
  variantId: number;
  productId: number;
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
  lowStockTotal: number;
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
