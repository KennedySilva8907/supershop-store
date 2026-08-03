import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router";
import { ApiError, apiGet, apiSend } from "../../lib/apiClient";
import { formatDate, formatPrice } from "../../lib/format";
import { METHOD_LABELS, STATUS_LABELS } from "../../types/cart";
import type { AdminOrder } from "../../types/admin";

const FILTERS = [
  { value: "", label: "Todas" },
  { value: "1", label: "A aguardar pagamento" },
  { value: "2", label: "Pagas" },
  { value: "3", label: "Expedidas" },
  { value: "4", label: "Entregues" },
  { value: "5", label: "Canceladas" },
];

export function AdminOrdersPage() {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState("");
  const [error, setError] = useState<string | null>(null);

  const { data: orders, isPending } = useQuery({
    queryKey: ["admin", "orders", status],
    queryFn: ({ signal }) => apiGet<AdminOrder[]>(`/admin/orders${status ? `?status=${status}` : ""}`, signal),
    placeholderData: (previous) => previous,
  });

  const move = useMutation({
    mutationFn: ({ id, next }: { id: number; next: number }) =>
      apiSend<AdminOrder>("PATCH", `/admin/orders/${id}/status`, { status: next }),
    onSuccess: () => {
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["admin"] });
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught.message : "Não foi possível mudar o estado."),
  });

  return (
    <div className="px-8 py-6">
      <div className="flex items-center justify-between gap-6">
        <h1 className="text-2xl">Encomendas</h1>
        <select
          value={status}
          onChange={(event) => setStatus(event.target.value)}
          className="border border-line bg-bg px-3 py-2 text-sm"
        >
          {FILTERS.map((filter) => (
            <option key={filter.value} value={filter.value}>
              {filter.label}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="mt-4 border border-danger px-4 py-2 text-sm text-danger">{error}</p>}

      {isPending && <div className="mt-6 h-40 animate-pulse bg-surface" />}

      {orders && (
        <div className="mt-6 overflow-x-auto">
        <table className="w-full min-w-[52rem] border border-line text-sm">
          <thead className="bg-surface text-left">
            <tr>
              <Th>Número</Th>
              <Th>Cliente</Th>
              <Th>Destino</Th>
              <Th>Pagamento</Th>
              <Th right>Total</Th>
              <Th>Estado</Th>
              <Th>Data</Th>
              <Th right>Mover para</Th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr key={order.id} className="border-t border-line">
                <td className="whitespace-nowrap px-4 py-2 font-mono text-xs">
                  <Link
                    to={`/admin/encomendas/${order.id}`}
                    className="underline underline-offset-4 transition hover:text-muted"
                  >
                    {order.orderNumber}
                  </Link>
                </td>
                <td className="px-4 py-2">{order.customerName}</td>
                <td className="px-4 py-2 text-muted">{order.shippingCity}</td>
                <td className="px-4 py-2 font-mono text-xs text-muted">
                  {METHOD_LABELS[order.paymentMethod]}
                </td>
                <td className="px-4 py-2 text-right font-mono">{formatPrice(order.total)}</td>
                <td className="px-4 py-2">
                  <span
                    className={`font-mono text-xs ${
                      order.status === 5 ? "text-danger" : order.status === 1 ? "text-muted" : ""
                    }`}
                  >
                    {STATUS_LABELS[order.status]}
                  </span>
                </td>
                <td className="px-4 py-2 text-xs text-muted">{formatDate(order.createdAt)}</td>
                <td className="px-4 py-2 text-right">
                  {order.nextStates.length === 0 ? (
                    <span className="font-mono text-[11px] text-muted">final</span>
                  ) : (
                    order.nextStates.map((next) => (
                      <button
                        key={next}
                        type="button"
                        disabled={move.isPending}
                        onClick={() => move.mutate({ id: order.id, next })}
                        className="ml-3 text-xs underline underline-offset-4 disabled:opacity-40"
                      >
                        {STATUS_LABELS[next]}
                      </button>
                    ))
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      )}

      {orders?.length === 0 && (
        <p className="mt-6 border border-line px-4 py-10 text-center text-sm text-muted">
          Nenhuma encomenda neste estado.
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
