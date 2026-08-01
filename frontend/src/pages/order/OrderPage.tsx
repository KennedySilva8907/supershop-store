import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { apiGet, apiSend } from "../../lib/apiClient";
import { formatDate, formatPrice } from "../../lib/format";
import { METHOD_LABELS, OrderStatus, PaymentMethod, STATUS_LABELS, type Order } from "../../types/cart";

export function OrderPage() {
  const { orderNumber = "" } = useParams();
  const queryClient = useQueryClient();

  const { data: order, isPending } = useQuery({
    queryKey: ["order", orderNumber],
    queryFn: ({ signal }) => apiGet<Order>(`/orders/${orderNumber}`, signal),
    refetchInterval: (query) => {
      const current = query.state.data;
      const waitingMbWay =
        current?.status === OrderStatus.AwaitingPayment &&
        current.payment.method === PaymentMethod.MbWay;

      return waitingMbWay ? 3000 : false;
    },
  });

  const confirm = useMutation({
    mutationFn: () => apiSend<Order>("POST", `/payments/${orderNumber}/confirm`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["order", orderNumber] }),
  });

  const cancel = useMutation({
    mutationFn: () => apiSend<Order>("POST", `/orders/${orderNumber}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["order", orderNumber] }),
  });

  if (isPending) {
    return (
      <div className="mx-auto max-w-3xl px-6 py-16">
        <div className="h-10 w-64 animate-pulse bg-surface" />
        <div className="mt-8 h-48 animate-pulse bg-surface" />
      </div>
    );
  }

  if (!order) return null;

  const paid = order.status !== OrderStatus.AwaitingPayment && order.status !== OrderStatus.Cancelled;

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      <p className="font-mono text-xs uppercase tracking-widest text-muted">
        {formatDate(order.createdAt)}
      </p>
      <h1 className="mt-3 text-4xl">Encomenda {order.orderNumber}</h1>

      <p className="mt-4 inline-block border border-line px-3 py-1 font-mono text-xs uppercase tracking-widest">
        {STATUS_LABELS[order.status]}
      </p>

      {order.status === OrderStatus.AwaitingPayment && (
        <section className="mt-10 border border-line p-6">
          {order.payment.method === PaymentMethod.Multibanco && (
            <MultibancoPanel order={order} />
          )}

          {order.payment.method === PaymentMethod.MbWay && (
            <div>
              <h2 className="text-2xl">Confirma no telemóvel</h2>
              <p className="mt-3 text-sm text-muted">
                Enviámos um pedido para {order.payment.mbWayPhone}. Esta página atualiza sozinha.
              </p>
            </div>
          )}

          {order.payment.method === PaymentMethod.CashOnDelivery && (
            <div>
              <h2 className="text-2xl">Pagamento na entrega</h2>
              <p className="mt-3 text-sm text-muted">
                Pagas {formatPrice(order.total)} quando receberes a encomenda.
              </p>
            </div>
          )}

          <button
            type="button"
            disabled={confirm.isPending}
            onClick={() => confirm.mutate()}
            className="mt-6 bg-ink px-6 py-3 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
          >
            {confirm.isPending ? "A confirmar…" : "Simular pagamento"}
          </button>
          <p className="mt-2 text-xs text-muted">
            Todos os pagamentos são simulados. Nenhum dinheiro muda de mãos.
          </p>
        </section>
      )}

      {paid && (
        <section className="mt-10 border border-line p-6">
          <h2 className="text-2xl">Pagamento confirmado</h2>
          <p className="mt-3 text-sm text-muted">
            {METHOD_LABELS[order.payment.method]}
            {order.payment.cardLast4 && ` terminado em ${order.payment.cardLast4}`}
            {order.paidAt && ` · ${formatDate(order.paidAt)}`}
          </p>
        </section>
      )}

      <section className="mt-10">
        <h2 className="text-2xl">Artigos</h2>
        <div className="mt-6 space-y-px bg-line">
          {order.items.map((line) => (
            <div key={line.sku} className="flex justify-between gap-4 bg-bg p-4 text-sm">
              <span>
                {line.productName}
                <span className="text-muted"> · {line.sizeLabel} · {line.quantity}</span>
              </span>
              <span className="font-mono">{formatPrice(line.lineTotal)}</span>
            </div>
          ))}
        </div>

        <dl className="mt-6 space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted">Subtotal</dt>
            <dd className="font-mono">{formatPrice(order.subtotal)}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted">Portes</dt>
            <dd className="font-mono">
              {order.shippingCost === 0 ? "Grátis" : formatPrice(order.shippingCost)}
            </dd>
          </div>
          <div className="flex justify-between border-t border-line pt-2 text-base">
            <dt>Total</dt>
            <dd className="font-mono">{formatPrice(order.total)}</dd>
          </div>
        </dl>
      </section>

      <section className="mt-10 border border-line p-6 text-sm">
        <p className="font-mono text-xs uppercase tracking-widest text-muted">Envio</p>
        <p className="mt-2">{order.shippingFullName}</p>
        <p className="text-muted">
          {order.shippingLine1}
          {order.shippingLine2 ? `, ${order.shippingLine2}` : ""}
        </p>
        <p className="font-mono text-xs text-muted">
          {order.shippingPostalCode} {order.shippingCity} · {order.shippingCountry} · {order.shippingPhone}
        </p>
      </section>

      <div className="mt-10 flex gap-4">
        <Link to="/conta/encomendas" className="border border-line px-6 py-3 text-sm transition hover:border-ink">
          As minhas encomendas
        </Link>

        {order.canCancel && (
          <button
            type="button"
            disabled={cancel.isPending}
            onClick={() => cancel.mutate()}
            className="px-6 py-3 text-sm text-danger underline underline-offset-4 disabled:opacity-40"
          >
            {cancel.isPending ? "A cancelar…" : "Cancelar encomenda"}
          </button>
        )}
      </div>
    </div>
  );
}

function MultibancoPanel({ order }: { order: Order }) {
  const remaining = useCountdown(order.payment.expiresAt);

  return (
    <div>
      <h2 className="text-2xl">Referência Multibanco</h2>

      <dl className="mt-6 space-y-3">
        <Row label="Entidade" value={order.payment.mbEntity ?? ""} />
        <Row label="Referência" value={order.payment.mbReference ?? ""} />
        <Row label="Valor" value={formatPrice(order.payment.amount)} />
      </dl>

      {remaining && (
        <p className="mt-6 font-mono text-xs text-muted">
          Válida durante mais {remaining}.
        </p>
      )}
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  const [copied, setCopied] = useState(false);

  return (
    <div className="flex items-center justify-between gap-4 border-b border-line pb-3">
      <dt className="font-mono text-xs uppercase tracking-widest text-muted">{label}</dt>
      <dd className="flex items-center gap-3">
        <span className="font-mono text-lg">{value}</span>
        <button
          type="button"
          onClick={() => {
            navigator.clipboard.writeText(value.replace(/\s/g, ""));
            setCopied(true);
            setTimeout(() => setCopied(false), 1500);
          }}
          className="font-mono text-[11px] uppercase tracking-widest text-muted underline underline-offset-4 hover:text-ink"
        >
          {copied ? "copiado" : "copiar"}
        </button>
      </dd>
    </div>
  );
}

function useCountdown(expiresAt: string | null): string | null {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!expiresAt) return;

    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [expiresAt]);

  if (!expiresAt) return null;

  const milliseconds = new Date(expiresAt).getTime() - now;

  if (milliseconds <= 0) return null;

  const totalMinutes = Math.floor(milliseconds / 60000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  const seconds = Math.floor((milliseconds % 60000) / 1000);

  return `${hours}h ${String(minutes).padStart(2, "0")}m ${String(seconds).padStart(2, "0")}s`;
}
