export interface CartLine {
  id: number;
  productVariantId: number;
  productId: number;
  productName: string;
  productSlug: string;
  collectionName: string;
  sizeLabel: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  stockAvailable: number;
  imagePublicId: string | null;
  lineTotal: number;
  exceedsStock: boolean;
}

export interface Cart {
  items: CartLine[];
  subtotal: number;
  shippingCost: number;
  total: number;
  freeShippingRemaining: number;
  itemCount: number;
  isEmpty: boolean;
  hasStockProblem: boolean;
}

export const OrderStatus = {
  AwaitingPayment: 1,
  Paid: 2,
  Shipped: 3,
  Delivered: 4,
  Cancelled: 5,
} as const;

export const PaymentMethod = {
  Multibanco: 1,
  MbWay: 2,
  Card: 3,
  CashOnDelivery: 4,
} as const;

export type PaymentMethodValue = (typeof PaymentMethod)[keyof typeof PaymentMethod];

export interface OrderLine {
  productName: string;
  collectionName: string;
  sizeLabel: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  imagePublicId: string | null;
}

export interface Payment {
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

export interface Order {
  orderNumber: string;
  status: number;
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
  items: OrderLine[];
  payment: Payment;
  canCancel: boolean;
}

export interface OrderSummary {
  orderNumber: string;
  status: number;
  total: number;
  itemCount: number;
  createdAt: string;
  firstImagePublicId: string | null;
}

export const STATUS_LABELS: Record<number, string> = {
  1: "A aguardar pagamento",
  2: "Pago",
  3: "Expedida",
  4: "Entregue",
  5: "Cancelada",
};

export const METHOD_LABELS: Record<number, string> = {
  1: "Multibanco",
  2: "MB WAY",
  3: "Cartão",
  4: "Na entrega",
};
