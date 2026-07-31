import { useQuery } from "@tanstack/react-query";
import { apiGet, buildQuery } from "../../lib/apiClient";
import type {
  Category,
  Collection,
  PagedResult,
  ProductDetail,
  ProductListItem,
  ProductQuery,
  Size,
} from "../../types/catalog";

const HOUR = 1000 * 60 * 60;

export function useCategories() {
  return useQuery({
    queryKey: ["categories"],
    queryFn: ({ signal }) => apiGet<Category[]>("/categories", signal),
    staleTime: HOUR,
  });
}

export function useCollections() {
  return useQuery({
    queryKey: ["collections"],
    queryFn: ({ signal }) => apiGet<Collection[]>("/collections", signal),
    staleTime: HOUR,
  });
}

export function useSizes(sizeSystem?: string) {
  return useQuery({
    queryKey: ["sizes", sizeSystem ?? "all"],
    queryFn: ({ signal }) =>
      apiGet<Size[]>(`/sizes${buildQuery({ sizeSystem })}`, signal),
    staleTime: HOUR,
  });
}

export function useProducts(query: ProductQuery) {
  return useQuery({
    queryKey: ["products", query],
    queryFn: ({ signal }) =>
      apiGet<PagedResult<ProductListItem>>(`/products${buildQuery({ ...query })}`, signal),
    placeholderData: (previous) => previous,
  });
}

export function useProduct(slug: string) {
  return useQuery({
    queryKey: ["product", slug],
    queryFn: ({ signal }) => apiGet<ProductDetail>(`/products/${slug}`, signal),
    enabled: Boolean(slug),
    retry: (failureCount, error) =>
      !(error instanceof Error && error.name === "ApiError") && failureCount < 2,
  });
}

export function useFeaturedProducts() {
  return useQuery({
    queryKey: ["products", "featured"],
    queryFn: ({ signal }) => apiGet<ProductListItem[]>("/products/featured", signal),
    staleTime: HOUR,
  });
}
