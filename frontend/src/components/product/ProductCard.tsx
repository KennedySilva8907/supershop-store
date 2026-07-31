import { Link } from "react-router";
import { cloudinaryUrl } from "../../lib/cloudinary";
import { formatPrice } from "../../lib/format";
import type { ProductListItem } from "../../types/catalog";

export function ProductCard({ product, priority = false }: { product: ProductListItem; priority?: boolean }) {
  const image = product.primaryImage;

  return (
    <Link to={`/produto/${product.slug}`} className="group block bg-bg">
      <div className="relative">
        {image ? (
          <img
            src={cloudinaryUrl(image.publicId, 400)}
            alt={image.altText}
            width={400}
            height={400}
            loading={priority ? "eager" : "lazy"}
            fetchPriority={priority ? "high" : "auto"}
            className="aspect-square w-full bg-surface object-cover transition duration-200 group-hover:brightness-95"
          />
        ) : (
          <div className="aspect-square w-full bg-surface" />
        )}

        {product.isOnSale && (
          <span className="absolute left-3 top-3 rounded-full bg-accent px-3 py-1 font-mono text-[11px] font-medium text-ink">
            PROMO
          </span>
        )}

        {!product.hasStock && (
          <div className="absolute inset-0 flex items-center justify-center bg-bg/70">
            <span className="border border-danger px-3 py-1 font-mono text-xs uppercase tracking-widest text-danger">
              Esgotado
            </span>
          </div>
        )}
      </div>

      <div className="p-4">
        <p className="font-mono text-[11px] uppercase tracking-widest text-muted">
          {product.collectionName}
        </p>
        <h3 className="mt-1 font-sans text-sm normal-case tracking-normal decoration-accent decoration-2 underline-offset-4 group-hover:underline">
          {product.name}
        </h3>
        <p className="mt-2 flex items-baseline gap-2 font-mono text-sm">
          {formatPrice(product.price)}
          {product.isOnSale && product.compareAtPrice !== null && (
            <span className="text-xs text-muted line-through">{formatPrice(product.compareAtPrice)}</span>
          )}
        </p>
      </div>
    </Link>
  );
}
