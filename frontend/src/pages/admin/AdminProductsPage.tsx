import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router";
import { apiGet, apiSend } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";
import type { AdminProduct } from "../../types/admin";

export function AdminProductsPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");

  const { data: products, isPending } = useQuery({
    queryKey: ["admin", "products", search],
    queryFn: ({ signal }) => apiGet<AdminProduct[]>(`/admin/products${search ? `?search=${encodeURIComponent(search)}` : ""}`, signal),
    placeholderData: (previous) => previous,
  });

  const toggle = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      apiSend<AdminProduct>("PATCH", `/admin/products/${id}/status`, { isActive }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin", "products"] }),
  });

  return (
    <div className="px-8 py-6">
      <div className="flex items-center justify-between gap-6">
        <h1 className="text-2xl">Produtos</h1>
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Procurar por nome ou endereço"
          className="w-72 border border-line bg-bg px-3 py-2 text-sm"
        />
      </div>

      {isPending && <div className="mt-6 h-40 animate-pulse bg-surface" />}

      {products && (
        <table className="mt-6 w-full border border-line text-sm">
          <thead className="bg-surface text-left">
            <tr>
              <Th>Produto</Th>
              <Th>Categoria</Th>
              <Th>Linha</Th>
              <Th right>Preço</Th>
              <Th right>Stock</Th>
              <Th right>Imagens</Th>
              <Th>Estado</Th>
              <Th right>Ações</Th>
            </tr>
          </thead>
          <tbody>
            {products.map((product) => (
              <tr key={product.id} className={`border-t border-line ${product.isActive ? "" : "opacity-50"}`}>
                <td className="px-4 py-2">
                  <Link to={`/produto/${product.slug}`} className="underline-offset-4 hover:underline">
                    {product.name}
                  </Link>
                  {product.isFeatured && (
                    <span className="ml-2 rounded-full bg-accent px-2 py-0.5 font-mono text-[10px] text-ink">
                      destaque
                    </span>
                  )}
                </td>
                <td className="px-4 py-2 text-muted">{product.categoryName}</td>
                <td className="px-4 py-2 font-mono text-xs text-muted">{product.collectionName}</td>
                <td className="px-4 py-2 text-right font-mono">{formatPrice(product.price)}</td>
                <td className={`px-4 py-2 text-right font-mono ${product.totalStock === 0 ? "text-danger" : ""}`}>
                  {product.totalStock}
                </td>
                <td className="px-4 py-2 text-right font-mono text-muted">{product.imageCount}</td>
                <td className="px-4 py-2">
                  <span className={`font-mono text-xs ${product.isActive ? "" : "text-danger"}`}>
                    {product.isActive ? "ativo" : "inativo"}
                  </span>
                </td>
                <td className="px-4 py-2 text-right">
                  <Link
                    to={`/admin/produtos/${product.id}/stock`}
                    className="mr-4 text-xs underline underline-offset-4"
                  >
                    Stock
                  </Link>
                  <button
                    type="button"
                    onClick={() => toggle.mutate({ id: product.id, isActive: !product.isActive })}
                    className="text-xs underline underline-offset-4"
                  >
                    {product.isActive ? "Desativar" : "Ativar"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {products?.length === 0 && (
        <p className="mt-6 border border-line px-4 py-10 text-center text-sm text-muted">
          Nenhum produto corresponde à procura.
        </p>
      )}
    </div>
  );
}

function Th({ children, right = false }: { children: React.ReactNode; right?: boolean }) {
  return (
    <th
      className={`px-4 py-2 font-mono text-[11px] uppercase tracking-widest text-muted ${right ? "text-right" : ""}`}
    >
      {children}
    </th>
  );
}
