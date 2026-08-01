import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { apiGet } from "../../lib/apiClient";
import { cloudinaryUrl } from "../../lib/cloudinary";
import { formatDate, formatPrice } from "../../lib/format";
import { STATUS_LABELS, type OrderSummary } from "../../types/cart";

export function OrdersPage() {
  const { data: orders, isPending } = useQuery({
    queryKey: ["orders"],
    queryFn: ({ signal }) => apiGet<OrderSummary[]>("/orders", signal),
  });

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      <h1 className="text-4xl">As minhas encomendas</h1>

      {isPending && <div className="mt-10 h-28 animate-pulse bg-surface" />}

      {orders?.length === 0 && !isPending && (
        <div className="mt-10 border border-line px-6 py-16 text-center">
          <p className="text-sm text-muted">Ainda não fizeste nenhuma encomenda.</p>
          <Link to="/catalogo" className="mt-6 inline-block bg-ink px-6 py-3 text-sm text-bg">
            Ver catálogo
          </Link>
        </div>
      )}

      <div className="mt-10 space-y-px bg-line">
        {orders?.map((order) => (
          <Link
            key={order.orderNumber}
            to={`/encomenda/${order.orderNumber}`}
            className="flex items-center gap-5 bg-bg p-5 transition hover:bg-surface"
          >
            {order.firstImagePublicId && (
              <img
                src={cloudinaryUrl(order.firstImagePublicId, 400)}
                alt=""
                width={64}
                height={64}
                loading="lazy"
                className="size-16 bg-surface object-cover"
              />
            )}

            <div className="min-w-0 flex-1">
              <p className="font-mono text-sm">{order.orderNumber}</p>
              <p className="mt-1 text-xs text-muted">
                {formatDate(order.createdAt)} · {order.itemCount}{" "}
                {order.itemCount === 1 ? "artigo" : "artigos"}
              </p>
              <p className="mt-1 font-mono text-[11px] uppercase tracking-widest text-muted">
                {STATUS_LABELS[order.status]}
              </p>
            </div>

            <p className="font-mono text-sm">{formatPrice(order.total)}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
