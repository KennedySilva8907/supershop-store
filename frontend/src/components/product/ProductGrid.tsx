import type { ProductListItem } from "../../types/catalog";
import { ProductCard } from "./ProductCard";

export function ProductGrid({ products }: { products: ProductListItem[] }) {
  return (
    <div className="grid grid-cols-2 gap-px border border-line bg-line md:grid-cols-3 lg:grid-cols-4">
      {products.map((product, index) => (
        <ProductCard key={product.id} product={product} priority={index < 4} />
      ))}
    </div>
  );
}

export function ProductGridSkeleton({ count = 12 }: { count?: number }) {
  return (
    <div
      className="grid grid-cols-2 gap-px border border-line bg-line md:grid-cols-3 lg:grid-cols-4"
      aria-busy="true"
      aria-label="A carregar produtos"
    >
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="bg-bg">
          <div className="aspect-square w-full animate-pulse bg-surface" />
          <div className="space-y-2 p-4">
            <div className="h-2 w-12 animate-pulse bg-surface" />
            <div className="h-3 w-32 animate-pulse bg-surface" />
            <div className="h-3 w-16 animate-pulse bg-surface" />
          </div>
        </div>
      ))}
    </div>
  );
}
