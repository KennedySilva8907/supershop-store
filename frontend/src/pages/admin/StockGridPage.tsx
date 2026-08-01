import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { apiGet, apiSend } from "../../lib/apiClient";
import type { AdminVariant } from "../../types/admin";

export function StockGridPage() {
  const { id = "" } = useParams();
  const queryClient = useQueryClient();
  const [saved, setSaved] = useState<number | null>(null);

  const { data: variants, isPending } = useQuery({
    queryKey: ["admin", "variants", id],
    queryFn: ({ signal }) => apiGet<AdminVariant[]>(`/admin/products/${id}/variants`, signal),
  });

  const save = useMutation({
    mutationFn: ({ variantId, stock }: { variantId: number; stock: number }) =>
      apiSend<AdminVariant>("PUT", `/admin/variants/${variantId}/stock`, { stock }),
    onSuccess: (updated) => {
      setSaved(updated.id);
      setTimeout(() => setSaved(null), 1200);
      queryClient.invalidateQueries({ queryKey: ["admin", "variants", id] });
      queryClient.invalidateQueries({ queryKey: ["admin", "products"] });
      queryClient.invalidateQueries({ queryKey: ["admin", "dashboard"] });
    },
  });

  return (
    <div className="px-8 py-6">
      <Link to="/admin/produtos" className="font-mono text-xs text-muted underline underline-offset-4">
        Produtos
      </Link>
      <h1 className="mt-2 text-2xl">Stock</h1>
      <p className="mt-1 text-xs text-muted">Cada linha grava ao sair do campo.</p>

      {isPending && <div className="mt-6 h-40 animate-pulse bg-surface" />}

      {variants && (
        <table className="mt-6 w-full max-w-2xl border border-line text-sm">
          <thead className="bg-surface text-left">
            <tr>
              <th className="px-4 py-2 font-mono text-[11px] uppercase tracking-widest text-muted">Tamanho</th>
              <th className="px-4 py-2 font-mono text-[11px] uppercase tracking-widest text-muted">SKU</th>
              <th className="px-4 py-2 text-right font-mono text-[11px] uppercase tracking-widest text-muted">Stock</th>
              <th className="w-24 px-4 py-2" />
            </tr>
          </thead>
          <tbody>
            {variants.map((variant) => (
              <tr key={variant.id} className="border-t border-line">
                <td className="px-4 py-2 font-mono">{variant.sizeLabel}</td>
                <td className="px-4 py-2 font-mono text-xs text-muted">{variant.sku}</td>
                <td className="px-4 py-2 text-right">
                  <input
                    type="number"
                    min={0}
                    defaultValue={variant.stock}
                    onBlur={(event) => {
                      const stock = Number(event.target.value);

                      if (Number.isFinite(stock) && stock !== variant.stock) {
                        save.mutate({ variantId: variant.id, stock });
                      }
                    }}
                    className={`w-24 border bg-bg px-3 py-1.5 text-right font-mono text-sm ${
                      variant.stock === 0 ? "border-danger" : "border-line"
                    }`}
                  />
                </td>
                <td className="px-4 py-2 font-mono text-[11px] text-muted">
                  {saved === variant.id && "gravado"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
