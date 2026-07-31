import { useParams } from "react-router";
import { ProductGrid, ProductGridSkeleton } from "../../components/product/ProductGrid";
import { useCategories, useCollections, useProducts, useSizes } from "../../features/catalog/queries";
import type { ProductSort } from "../../types/catalog";
import { useCatalogFilters } from "./useCatalogFilters";

const SORT_LABELS: Record<ProductSort, string> = {
  newest: "Mais recentes",
  price_asc: "Preço, mais baixo",
  price_desc: "Preço, mais alto",
  name: "Nome",
};

export function CatalogPage() {
  const { categorySlug } = useParams();
  const { query, setFilter, toggleCollection, clearAll, activeChips } = useCatalogFilters(categorySlug);

  const { data, isPending, isFetching } = useProducts(query);
  const { data: categories } = useCategories();
  const { data: collections } = useCollections();
  const { data: sizes } = useSizes();

  const category = categories?.find((c) => c.slug === categorySlug);
  const selectedCollections = (query.collection ?? "").split(",").filter(Boolean);

  return (
    <div className="mx-auto max-w-7xl px-6 py-12">
      <h1 className="text-4xl md:text-5xl">{category?.name ?? "Catálogo"}</h1>
      <p className="mt-3 font-mono text-xs text-muted">
        {data ? `${data.totalItems} ${data.totalItems === 1 ? "produto" : "produtos"}` : " "}
      </p>

      <div className="mt-10 grid gap-10 lg:grid-cols-[16rem_1fr]">
        <aside className="space-y-8">
          <Filter title="Linha">
            {collections?.map((collection) => (
              <label key={collection.slug} className="flex cursor-pointer items-center gap-3 py-1 text-sm">
                <input
                  type="checkbox"
                  checked={selectedCollections.includes(collection.slug)}
                  onChange={() => toggleCollection(collection.slug)}
                  className="size-4 accent-ink"
                />
                {collection.name}
              </label>
            ))}
          </Filter>

          <Filter title="Tamanho">
            <div className="flex flex-wrap gap-2">
              {sizes?.map((size) => (
                <button
                  key={size.id}
                  type="button"
                  onClick={() => setFilter("size", query.size === size.label ? undefined : size.label)}
                  className={`border px-3 py-2 font-mono text-xs transition ${
                    query.size === size.label ? "border-ink bg-ink text-bg" : "border-line hover:border-ink"
                  }`}
                >
                  {size.label}
                </button>
              ))}
            </div>
          </Filter>

          <Filter title="Preço">
            <div className="flex items-center gap-2">
              <input
                type="number"
                inputMode="numeric"
                placeholder="mín"
                defaultValue={query.minPrice ?? ""}
                onBlur={(event) => setFilter("minPrice", event.target.value || undefined)}
                className="w-full border border-line bg-bg px-3 py-2 font-mono text-sm"
              />
              <span className="text-muted">–</span>
              <input
                type="number"
                inputMode="numeric"
                placeholder="máx"
                defaultValue={query.maxPrice ?? ""}
                onBlur={(event) => setFilter("maxPrice", event.target.value || undefined)}
                className="w-full border border-line bg-bg px-3 py-2 font-mono text-sm"
              />
            </div>
          </Filter>
        </aside>

        <section>
          <div className="flex flex-wrap items-center justify-between gap-4 border-b border-line pb-4">
            <div className="flex flex-wrap items-center gap-2">
              {activeChips.map((chip) => (
                <button
                  key={chip.key}
                  type="button"
                  onClick={chip.onRemove}
                  className="flex items-center gap-2 rounded-full border border-line px-3 py-1 font-mono text-xs transition hover:border-ink"
                >
                  {chip.label}
                  <span aria-hidden="true">×</span>
                  <span className="sr-only">Remover filtro</span>
                </button>
              ))}
              {activeChips.length > 0 && (
                <button type="button" onClick={clearAll} className="text-xs text-muted underline underline-offset-4">
                  Limpar tudo
                </button>
              )}
            </div>

            <label className="flex items-center gap-2 text-sm">
              <span className="text-muted">Ordenar</span>
              <select
                value={query.sort ?? "newest"}
                onChange={(event) => setFilter("sort", event.target.value)}
                className="border border-line bg-bg px-3 py-2 text-sm"
              >
                {Object.entries(SORT_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div className="mt-8" aria-busy={isFetching}>
            {isPending ? (
              <ProductGridSkeleton />
            ) : data && data.items.length > 0 ? (
              <ProductGrid products={data.items} />
            ) : (
              <div className="border border-line px-6 py-20 text-center">
                <p className="font-display text-2xl tracking-tight">Sem resultados</p>
                <p className="mt-3 text-sm text-muted">Nenhum produto corresponde a estes filtros.</p>
                <button
                  type="button"
                  onClick={clearAll}
                  className="mt-6 bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90"
                >
                  Limpar filtros
                </button>
              </div>
            )}
          </div>

          {data && data.totalPages > 1 && (
            <nav className="mt-10 flex items-center justify-center gap-4" aria-label="Paginação">
              <button
                type="button"
                disabled={!data.hasPrevious}
                onClick={() => setFilter("page", data.page - 1)}
                className="border border-line px-5 py-3 text-sm transition disabled:opacity-40 enabled:hover:border-ink"
              >
                Anterior
              </button>
              <span className="font-mono text-sm text-muted">
                {data.page} / {data.totalPages}
              </span>
              <button
                type="button"
                disabled={!data.hasNext}
                onClick={() => setFilter("page", data.page + 1)}
                className="border border-line px-5 py-3 text-sm transition disabled:opacity-40 enabled:hover:border-ink"
              >
                Seguinte
              </button>
            </nav>
          )}
        </section>
      </div>
    </div>
  );
}

function Filter({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <h2 className="font-mono text-xs uppercase tracking-widest text-muted">{title}</h2>
      <div className="mt-3">{children}</div>
    </div>
  );
}
