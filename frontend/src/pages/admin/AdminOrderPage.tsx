import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { ApiError, apiGet, apiSend } from "../../lib/apiClient";
import { formatDate, formatPrice } from "../../lib/format";
import { METHOD_LABELS, STATUS_LABELS } from "../../types/cart";
import type { AdminOrderDetail } from "../../types/admin";

const PAYMENT_STATUS: Record<number, string> = {
  1: "Por confirmar",
  2: "Confirmado",
  3: "Expirado",
  4: "Falhou",
};

export function AdminOrderPage() {
  const { id } = useParams();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const { data: order, isPending } = useQuery({
    queryKey: ["admin", "order", id],
    queryFn: ({ signal }) => apiGet<AdminOrderDetail>(`/admin/orders/${id}`, signal),
  });

  const move = useMutation({
    mutationFn: (next: number) => apiSend("PATCH", `/admin/orders/${id}/status`, { status: next }),
    onSuccess: () => {
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["admin"] });
    },
    onError: (caught) =>
      setError(caught instanceof ApiError ? caught.message : "Não foi possível mudar o estado."),
  });

  if (isPending) return <div className="px-8 py-6"><div className="h-64 animate-pulse bg-surface" /></div>;
  if (!order) return null;

  return (
    <div className="px-8 py-6">
      <Link to="/admin/encomendas" className="font-mono text-[11px] uppercase tracking-widest text-muted">
        &larr; Encomendas
      </Link>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-4">
        <h1 className="font-mono text-2xl">{order.orderNumber}</h1>

        <div className="flex items-center gap-4">
          <span className={`font-mono text-xs ${order.status === 5 ? "text-danger" : ""}`}>
            {STATUS_LABELS[order.status]}
          </span>

          {order.nextStates.length === 0 ? (
            <span className="font-mono text-[11px] text-muted">final</span>
          ) : (
            order.nextStates.map((next) => (
              <button
                key={next}
                type="button"
                disabled={move.isPending}
                onClick={() => move.mutate(next)}
                className="border border-line px-4 py-2 text-xs transition hover:border-ink disabled:opacity-40"
              >
                Marcar como {STATUS_LABELS[next].toLowerCase()}
              </button>
            ))
          )}
        </div>
      </div>

      {error && <p className="mt-4 border border-danger px-4 py-2 text-sm text-danger">{error}</p>}

      <table className="mt-8 w-full border border-line text-sm">
        <thead className="bg-surface text-left">
          <tr>
            <Th>Produto</Th>
            <Th>Tamanho</Th>
            <Th>SKU</Th>
            <Th right>Preço</Th>
            <Th right>Qtd</Th>
            <Th right>Total</Th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((line) => (
            <tr key={line.sku} className="border-t border-line">
              <td className="px-4 py-2">
                {line.productName}
                <span className="ml-2 font-mono text-[11px] uppercase text-muted">{line.collectionName}</span>
              </td>
              <td className="px-4 py-2 font-mono text-xs">{line.sizeLabel}</td>
              <td className="px-4 py-2 font-mono text-xs text-muted">{line.sku}</td>
              <td className="px-4 py-2 text-right font-mono">{formatPrice(line.unitPrice)}</td>
              <td className="px-4 py-2 text-right font-mono">{line.quantity}</td>
              <td className="px-4 py-2 text-right font-mono">{formatPrice(line.lineTotal)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot className="border-t border-line">
          <Total label="Subtotal" value={formatPrice(order.subtotal)} />
          <Total
            label="Portes"
            value={order.shippingCost === 0 ? "Grátis" : formatPrice(order.shippingCost)}
          />
          <Total label="Total" value={formatPrice(order.total)} strong />
        </tfoot>
      </table>

      <div className="mt-8 grid gap-8 md:grid-cols-3">
        <Panel title="Cliente">
          <p>{order.customerName}</p>
          <p className="break-all font-mono text-xs text-muted">{order.customerEmail}</p>
        </Panel>

        <Panel title="Envio">
          <p>{order.shippingFullName}</p>
          <p className="text-muted">{order.shippingLine1}</p>
          {order.shippingLine2 && <p className="text-muted">{order.shippingLine2}</p>}
          <p className="text-muted">
            {order.shippingPostalCode} {order.shippingCity}, {order.shippingCountry}
          </p>
          <p className="font-mono text-xs text-muted">{order.shippingPhone}</p>
        </Panel>

        <Panel title="Pagamento">
          <p>{METHOD_LABELS[order.payment.method]}</p>
          <p className="font-mono text-xs text-muted">{PAYMENT_STATUS[order.payment.status]}</p>

          {order.payment.mbEntity && (
            <p className="mt-2 font-mono text-xs">
              Entidade {order.payment.mbEntity} · Ref {order.payment.mbReference}
            </p>
          )}
          {order.payment.mbWayPhone && (
            <p className="mt-2 font-mono text-xs">{order.payment.mbWayPhone}</p>
          )}
          {order.payment.cardLast4 && (
            <p className="mt-2 font-mono text-xs">**** **** **** {order.payment.cardLast4}</p>
          )}
        </Panel>
      </div>

      <dl className="mt-8 flex flex-wrap gap-x-10 gap-y-2 border-t border-line pt-6">
        <Moment label="Criada" at={order.createdAt} />
        <Moment label="Paga" at={order.paidAt} />
        <Moment label="Expedida" at={order.shippedAt} />
      </dl>
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

function Total({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <tr>
      <td colSpan={5} className={`px-4 py-2 text-right text-xs ${strong ? "" : "text-muted"}`}>
        {label}
      </td>
      <td className={`px-4 py-2 text-right font-mono ${strong ? "" : "text-muted"}`}>{value}</td>
    </tr>
  );
}

function Panel({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="border border-line px-5 py-4">
      <h2 className="font-mono text-[11px] uppercase tracking-widest text-muted">{title}</h2>
      <div className="mt-3 space-y-1 text-sm">{children}</div>
    </div>
  );
}

function Moment({ label, at }: { label: string; at: string | null }) {
  return (
    <div className="flex items-baseline gap-3">
      <dt className="font-mono text-[11px] uppercase tracking-widest text-muted">{label}</dt>
      <dd className="font-mono text-xs">{at ? formatDate(at) : "—"}</dd>
    </div>
  );
}
