import { useCallback, useMemo } from "react";
import { useSearchParams } from "react-router";
import type { ProductQuery, ProductSort } from "../../types/catalog";

const SORTS: ProductSort[] = ["newest", "price_asc", "price_desc", "name"];

export function useCatalogFilters(categoryFromRoute?: string) {
  const [searchParams, setSearchParams] = useSearchParams();

  const query = useMemo<ProductQuery>(() => {
    const sort = searchParams.get("sort");
    const page = Number(searchParams.get("page"));
    const minPrice = Number(searchParams.get("minPrice"));
    const maxPrice = Number(searchParams.get("maxPrice"));

    return {
      category: categoryFromRoute ?? searchParams.get("category") ?? undefined,
      collection: searchParams.get("collection") ?? undefined,
      size: searchParams.get("size") ?? undefined,
      search: searchParams.get("search") ?? undefined,
      minPrice: Number.isFinite(minPrice) && minPrice > 0 ? minPrice : undefined,
      maxPrice: Number.isFinite(maxPrice) && maxPrice > 0 ? maxPrice : undefined,
      sort: SORTS.includes(sort as ProductSort) ? (sort as ProductSort) : undefined,
      page: Number.isFinite(page) && page > 1 ? page : undefined,
    };
  }, [searchParams, categoryFromRoute]);

  const setFilter = useCallback(
    (key: string, value: string | number | undefined) => {
      setSearchParams(
        (previous) => {
          const next = new URLSearchParams(previous);

          if (value === undefined || value === "" || value === null) {
            next.delete(key);
          } else {
            next.set(key, String(value));
          }

          if (key !== "page") {
            next.delete("page");
          }

          return next;
        },
        { replace: false },
      );
    },
    [setSearchParams],
  );

  const toggleCollection = useCallback(
    (slug: string) => {
      const current = (searchParams.get("collection") ?? "")
        .split(",")
        .filter(Boolean);

      const next = current.includes(slug)
        ? current.filter((s) => s !== slug)
        : [...current, slug];

      setFilter("collection", next.join(",") || undefined);
    },
    [searchParams, setFilter],
  );

  const clearAll = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  const activeChips = useMemo(() => {
    const chips: { key: string; label: string; onRemove: () => void }[] = [];

    if (!categoryFromRoute && query.category) {
      chips.push({ key: "category", label: query.category, onRemove: () => setFilter("category", undefined) });
    }

    for (const slug of (query.collection ?? "").split(",").filter(Boolean)) {
      chips.push({ key: `collection-${slug}`, label: slug.toUpperCase(), onRemove: () => toggleCollection(slug) });
    }

    if (query.size) {
      chips.push({ key: "size", label: `Tamanho ${query.size}`, onRemove: () => setFilter("size", undefined) });
    }

    if (query.search) {
      chips.push({ key: "search", label: `"${query.search}"`, onRemove: () => setFilter("search", undefined) });
    }

    if (query.minPrice) {
      chips.push({ key: "minPrice", label: `desde ${query.minPrice} €`, onRemove: () => setFilter("minPrice", undefined) });
    }

    if (query.maxPrice) {
      chips.push({ key: "maxPrice", label: `até ${query.maxPrice} €`, onRemove: () => setFilter("maxPrice", undefined) });
    }

    return chips;
  }, [query, categoryFromRoute, setFilter, toggleCollection]);

  return { query, setFilter, toggleCollection, clearAll, activeChips, searchParams };
}
