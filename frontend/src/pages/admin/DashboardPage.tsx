import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { apiGet } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";
import type { Dashboard } from "../../types/admin";

export function DashboardPage() {
  const { data, isPending } = useQuery({
    queryKey: ["admin", "dashboard"],
    queryFn: ({ signal }) => apiGet<Dashboard>("/admin/dashboard", signal),
  });

  return (
    <div className="px-8 py-6">
      <h1 className="text-2xl">Painel</h1>

      {isPending && <div className="mt-6 h-24 animate-pulse bg-surface" />}

      {data && (
        <>
          <div className="mt-6 grid grid-cols-2 gap-px border border-line bg-line lg:grid-cols-3">
            <Stat label="Vendas" value={formatPrice(data.salesTotal)} />
            <Stat label="Encomendas pagas" value={String(data.paidOrders)} />
            <Stat label="Por pagar" value={String(data.pendingOrders)} highlight={data.pendingOrders > 0} />
            <Stat label="Produtos" value={String(data.totalProducts)} />
            <Stat label="Inativos" value={String(data.inactiveProducts)} />
            <Stat label="Esgotados" value={String(data.outOfStockProducts)} highlight={data.outOfStockProducts > 0} />
          </div>

          <section className="mt-10">
            <h2 className="text-lg">Stock baixo</h2>
            <p className="mt-1 text-xs text-muted">
              {data.lowStockTotal > data.lowStock.length
                ? `${data.lowStockTotal} variantes ativas com menos de 5 unidades. Mostram-se as ${data.lowStock.length} com menos stock.`
                : `${data.lowStockTotal} variantes ativas com menos de 5 unidades.`}
            </p>

            {data.lowStock.length === 0 ? (
              <p className="mt-4 border border-line px-4 py-8 text-center text-sm text-muted">
                Nenhuma variante em risco.
              </p>
            ) : (
              <table className="mt-4 w-full border border-line text-sm">
                <thead className="bg-surface text-left">
                  <tr>
                    <th className="px-4 py-2 font-mono text-[11px] uppercase tracking-widest text-muted">Produto</th>
                    <th className="px-4 py-2 font-mono text-[11px] uppercase tracking-widest text-muted">Tamanho</th>
                    <th className="px-4 py-2 font-mono text-[11px] uppercase tracking-widest text-muted">SKU</th>
                    <th className="px-4 py-2 text-right font-mono text-[11px] uppercase tracking-widest text-muted">Stock</th>
                  </tr>
                </thead>
                <tbody>
                  {data.lowStock.map((row) => (
                    <tr key={row.variantId} className="border-t border-line">
                      <td className="px-4 py-2">
                        <Link
                          to={`/admin/produtos/${row.productId}/stock`}
                          className="underline-offset-4 hover:underline"
                        >
                          {row.productName}
                        </Link>
                      </td>
                      <td className="px-4 py-2 font-mono">{row.sizeLabel}</td>
                      <td className="px-4 py-2 font-mono text-xs text-muted">{row.sku}</td>
                      <td className={`px-4 py-2 text-right font-mono ${row.stock === 0 ? "text-danger" : ""}`}>
                        {row.stock}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <Link
              to="/admin/produtos"
              className="mt-6 inline-block border border-line px-5 py-2 text-sm transition hover:border-ink"
            >
              Gerir produtos
            </Link>
          </section>
        </>
      )}
    </div>
  );
}

function Stat({ label, value, highlight = false }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="bg-bg px-5 py-4">
      <p className="font-mono text-[11px] uppercase tracking-widest text-muted">{label}</p>
      <p className={`mt-2 font-mono text-2xl ${highlight ? "text-danger" : ""}`}>{value}</p>
    </div>
  );
}
