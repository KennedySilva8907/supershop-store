import { useState } from "react";
import { Link, useParams } from "react-router";
import { ImageGallery } from "../../components/product/ImageGallery";
import { SizeSelector } from "../../components/product/SizeSelector";
import { useCartActions } from "../../features/cart/useCart";
import { useProduct } from "../../features/catalog/queries";
import { ApiError } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";

export function ProductPage() {
  const { slug = "" } = useParams();
  const { data: product, isPending, error } = useProduct(slug);
  const [variantId, setVariantId] = useState<number | null>(null);
  const [added, setAdded] = useState(false);
  const [addError, setAddError] = useState<Error | null>(null);
  const { add } = useCartActions();

  if (isPending) {
    return <ProductSkeleton />;
  }

  if (error) {
    const notFound = error instanceof ApiError && error.status === 404;

    return (
      <div className="mx-auto max-w-7xl px-6 py-24 text-center">
        <h1 className="text-4xl">{notFound ? "Produto não encontrado" : "Algo correu mal"}</h1>
        <p className="mt-4 text-muted">
          {notFound ? "Este produto não existe ou já não está disponível." : error.message}
        </p>
        <Link to="/catalogo" className="mt-8 inline-block bg-ink px-6 py-3 text-sm text-bg">
          Voltar ao catálogo
        </Link>
      </div>
    );
  }

  const selected = product.variants.find((v) => v.id === variantId) ?? null;

  return (
    <div className="mx-auto max-w-7xl px-6 py-12">
      <nav className="font-mono text-xs text-muted">
        <Link to="/catalogo" className="underline-offset-4 hover:text-ink hover:underline">
          Catálogo
        </Link>
        <span className="mx-2">/</span>
        <Link
          to={`/catalogo/${product.categorySlug}`}
          className="underline-offset-4 hover:text-ink hover:underline"
        >
          {product.categoryName}
        </Link>
      </nav>

      <div className="mt-8 grid gap-12 lg:grid-cols-2">
        <ImageGallery images={product.images} name={product.name} />

        <div className="lg:pt-4">
          <p className="font-mono text-xs uppercase tracking-widest text-muted">
            {product.collectionName}
          </p>
          <h1 className="mt-3 text-4xl md:text-5xl">{product.name}</h1>

          <p className="mt-5 flex items-baseline gap-3 font-mono text-2xl">
            {formatPrice(product.price)}
            {product.isOnSale && product.compareAtPrice !== null && (
              <span className="text-base text-muted line-through">
                {formatPrice(product.compareAtPrice)}
              </span>
            )}
          </p>

          <p className="mt-6 max-w-prose text-muted">{product.description}</p>

          <div className="mt-10">
            <SizeSelector variants={product.variants} selectedId={variantId} onSelect={setVariantId} />

            {!product.hasStock && (
              <p className="mt-4 border border-danger px-4 py-3 font-mono text-sm text-danger">
                Esgotado em todos os tamanhos.
              </p>
            )}
          </div>

          {selected?.isLowStock && (
            <p className="mt-4 font-mono text-xs text-danger">
              Restam {selected.stock} unidades em {selected.sizeLabel}.
            </p>
          )}

          <button
            type="button"
            disabled={!product.hasStock || selected === null || add.isPending}
            onClick={() => {
              if (selected) {
                setAddError(null);
                add.mutate(
                  { variantId: selected.id, quantity: 1 },
                  { onSuccess: () => setAdded(true), onError: (e) => setAddError(e as Error) },
                );
              }
            }}
            className="mt-8 w-full bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40 sm:w-auto"
          >
            {add.isPending ? "A adicionar…" : "Adicionar ao carrinho"}
          </button>

          {product.hasStock && selected === null && (
            <p className="mt-3 text-sm text-muted">Escolhe um tamanho</p>
          )}

          {addError && <p className="mt-3 text-sm text-danger">{addError.message}</p>}

          {added && !addError && (
            <p className="mt-3 text-sm">
              Adicionado.{" "}
              <Link to="/carrinho" className="underline underline-offset-4">
                Ver carrinho
              </Link>
            </p>
          )}

          {selected && (
            <p className="mt-6 font-mono text-xs text-muted">SKU {selected.sku}</p>
          )}
        </div>
      </div>
    </div>
  );
}

function ProductSkeleton() {
  return (
    <div className="mx-auto max-w-7xl px-6 py-12">
      <div className="mt-8 grid gap-12 lg:grid-cols-2">
        <div className="aspect-square w-full animate-pulse border border-line bg-surface" />
        <div className="space-y-4 lg:pt-4">
          <div className="h-3 w-16 animate-pulse bg-surface" />
          <div className="h-10 w-72 animate-pulse bg-surface" />
          <div className="h-7 w-24 animate-pulse bg-surface" />
          <div className="h-20 w-full animate-pulse bg-surface" />
          <div className="h-12 w-full animate-pulse bg-surface" />
        </div>
      </div>
    </div>
  );
}
