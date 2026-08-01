import { Link } from "react-router";
import { cloudinaryUrl } from "../../lib/cloudinary";
import { formatPrice } from "../../lib/format";
import { useAuth } from "../../features/auth/AuthContext";
import { useCart, useCartActions } from "../../features/cart/useCart";

export function CartPage() {
  const { user } = useAuth();
  const { data: cart, isPending } = useCart();
  const { setQuantity, remove } = useCartActions();

  if (!user) {
    return (
      <div className="mx-auto max-w-2xl px-6 py-24 text-center">
        <h1 className="text-4xl">O teu carrinho</h1>
        <p className="mt-6 text-muted">
          Entra na tua conta para veres o carrinho. O que escolheste fica guardado e junta-se
          automaticamente.
        </p>
        <Link to="/entrar" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
          Entrar
        </Link>
      </div>
    );
  }

  if (isPending) {
    return (
      <div className="mx-auto max-w-5xl px-6 py-16">
        <div className="h-10 w-56 animate-pulse bg-surface" />
        <div className="mt-10 h-40 animate-pulse bg-surface" />
      </div>
    );
  }

  if (!cart || cart.isEmpty) {
    return (
      <div className="mx-auto max-w-2xl px-6 py-24 text-center">
        <h1 className="text-4xl">Carrinho vazio</h1>
        <p className="mt-6 text-muted">Ainda não escolheste nada.</p>
        <Link to="/catalogo" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
          Ver catálogo
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-5xl px-6 py-16">
      <h1 className="text-4xl">Carrinho</h1>

      <div className="mt-10 grid gap-12 lg:grid-cols-[1fr_20rem]">
        <div className="space-y-px bg-line">
          {cart.items.map((line) => (
            <article key={line.id} className="flex gap-5 bg-bg p-5">
              {line.imagePublicId && (
                <Link to={`/produto/${line.productSlug}`} className="shrink-0">
                  <img
                    src={cloudinaryUrl(line.imagePublicId, 400)}
                    alt={line.productName}
                    width={96}
                    height={96}
                    loading="lazy"
                    className="size-24 bg-surface object-cover"
                  />
                </Link>
              )}

              <div className="min-w-0 flex-1">
                <p className="font-mono text-[11px] uppercase tracking-widest text-muted">
                  {line.collectionName}
                </p>
                <Link to={`/produto/${line.productSlug}`} className="text-sm underline-offset-4 hover:underline">
                  {line.productName}
                </Link>
                <p className="mt-1 font-mono text-xs text-muted">
                  Tamanho {line.sizeLabel} · {line.sku}
                </p>

                {line.exceedsStock && (
                  <p className="mt-2 font-mono text-xs text-danger">
                    Só restam {line.stockAvailable}. Ajusta a quantidade para continuares.
                  </p>
                )}

                <div className="mt-3 flex items-center gap-4">
                  <div className="flex items-center border border-line">
                    <button
                      type="button"
                      aria-label="Diminuir"
                      onClick={() => setQuantity.mutate({ line, quantity: line.quantity - 1 })}
                      className="px-3 py-2 font-mono text-sm transition hover:bg-surface"
                    >
                      −
                    </button>
                    <span className="min-w-10 text-center font-mono text-sm">{line.quantity}</span>
                    <button
                      type="button"
                      aria-label="Aumentar"
                      disabled={line.quantity >= line.stockAvailable}
                      onClick={() => setQuantity.mutate({ line, quantity: line.quantity + 1 })}
                      className="px-3 py-2 font-mono text-sm transition enabled:hover:bg-surface disabled:opacity-30"
                    >
                      +
                    </button>
                  </div>

                  <button
                    type="button"
                    onClick={() => remove.mutate(line)}
                    className="text-xs text-muted underline underline-offset-4 hover:text-danger"
                  >
                    Remover
                  </button>
                </div>
              </div>

              <p className="shrink-0 font-mono text-sm">{formatPrice(line.lineTotal)}</p>
            </article>
          ))}
        </div>

        <aside className="h-fit border border-line p-6">
          <h2 className="text-xl">Resumo</h2>

          <dl className="mt-6 space-y-3 text-sm">
            <div className="flex justify-between">
              <dt className="text-muted">Subtotal</dt>
              <dd className="font-mono">{formatPrice(cart.subtotal)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted">Portes</dt>
              <dd className="font-mono">
                {cart.shippingCost === 0 ? "Grátis" : formatPrice(cart.shippingCost)}
              </dd>
            </div>
          </dl>

          {cart.freeShippingRemaining > 0 && (
            <p className="mt-4 bg-surface px-3 py-2 text-xs text-muted">
              Faltam {formatPrice(cart.freeShippingRemaining)} para portes grátis.
            </p>
          )}

          <div className="mt-6 flex justify-between border-t border-line pt-4">
            <span>Total</span>
            <span className="font-mono text-lg">{formatPrice(cart.total)}</span>
          </div>

          <Link
            to="/checkout"
            aria-disabled={cart.hasStockProblem}
            className={`mt-6 block w-full px-8 py-4 text-center text-sm ${
              cart.hasStockProblem
                ? "pointer-events-none bg-ink/40 text-bg"
                : "bg-ink text-bg transition hover:opacity-90"
            }`}
          >
            Finalizar compra
          </Link>

          {cart.hasStockProblem && (
            <p className="mt-3 text-xs text-danger">Ajusta as quantidades acima do stock.</p>
          )}
        </aside>
      </div>
    </div>
  );
}
