import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { AddressForm } from "../../features/account/AddressForm";
import { useCart } from "../../features/cart/useCart";
import { ApiError, apiGet, apiSend } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";
import type { Address, SaveAddress } from "../../types/account";
import { METHOD_LABELS, PaymentMethod, type Order, type PaymentMethodValue } from "../../types/cart";

const STEPS = ["Morada", "Pagamento", "Revisão"];

export function CheckoutPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: cart } = useCart();

  const [step, setStep] = useState(0);
  const [addressId, setAddressId] = useState<number | null>(null);
  const [addingAddress, setAddingAddress] = useState(false);
  const [method, setMethod] = useState<PaymentMethodValue>(PaymentMethod.Multibanco);
  const [phone, setPhone] = useState("");
  const [card, setCard] = useState("");
  const [error, setError] = useState<ApiError | null>(null);
  const [addressError, setAddressError] = useState<ApiError | null>(null);

  const { data: addresses, isPending } = useQuery({
    queryKey: ["addresses"],
    queryFn: ({ signal }) => apiGet<Address[]>("/me/addresses", signal),
  });

  const saveAddress = useMutation({
    mutationFn: (values: SaveAddress) => apiSend<Address>("POST", "/me/addresses", values),
    onSuccess: async (created) => {
      setAddressError(null);
      setAddingAddress(false);
      setAddressId(created.id);
      await queryClient.invalidateQueries({ queryKey: ["addresses"] });
    },
    onError: (caught) => setAddressError(caught instanceof ApiError ? caught : null),
  });

  const place = useMutation({
    mutationFn: () =>
      apiSend<Order>("POST", "/orders", {
        addressId,
        paymentMethod: method,
        mbWayPhone: method === PaymentMethod.MbWay ? phone : null,
        cardNumber: method === PaymentMethod.Card ? card : null,
      }),
    onSuccess: (order) => navigate(`/encomenda/${order.orderNumber}`, { replace: true }),
    onError: (caught) => setError(caught instanceof ApiError ? caught : null),
  });

  if (cart?.isEmpty) {
    return (
      <div className="mx-auto max-w-2xl px-6 py-24 text-center">
        <h1 className="text-4xl">Carrinho vazio</h1>
        <Link to="/catalogo" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
          Ver catálogo
        </Link>
      </div>
    );
  }

  const selected = addresses?.find((a) => a.id === addressId) ?? null;
  const canPay =
    method !== PaymentMethod.MbWay ? (method !== PaymentMethod.Card || card.length >= 12) : phone.length >= 9;

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      <h1 className="text-4xl">Finalizar compra</h1>

      <ol className="mt-8 flex gap-px border border-line bg-line">
        {STEPS.map((label, index) => (
          <li
            key={label}
            aria-current={index === step}
            className={`flex-1 px-4 py-3 text-center font-mono text-xs uppercase tracking-widest ${
              index === step ? "bg-ink text-bg" : index < step ? "bg-surface text-ink" : "bg-bg text-muted"
            }`}
          >
            {index + 1}. {label}
          </li>
        ))}
      </ol>

      {error && <div className="mt-8"><FormError message={error.message} traceId={error.problem.traceId} /></div>}

      {step === 0 && (
        <section className="mt-10">
          <h2 className="text-2xl">Morada de envio</h2>

          {isPending && <div className="mt-6 h-24 animate-pulse bg-surface" />}

          {addresses?.length === 0 && !addingAddress && (
            <div className="mt-6 border border-line p-6">
              <p className="text-sm text-muted">
                Ainda não tens moradas guardadas. Escreve a morada de envio para continuares.
              </p>
              <button
                type="button"
                onClick={() => setAddingAddress(true)}
                className="mt-4 bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90"
              >
                Adicionar morada
              </button>
            </div>
          )}

          {addingAddress && (
            <div className="mt-6">
              <AddressForm
                error={addressError}
                saving={saveAddress.isPending}
                submitLabel="Guardar e continuar"
                onSubmit={(values) => saveAddress.mutate(values)}
                onCancel={
                  addresses?.length === 0
                    ? undefined
                    : () => {
                        setAddingAddress(false);
                        setAddressError(null);
                      }
                }
              />
            </div>
          )}

          <div className="mt-6 space-y-px bg-line">
            {addresses?.map((address) => (
              <label key={address.id} className="flex cursor-pointer gap-4 bg-bg p-5">
                <input
                  type="radio"
                  name="address"
                  checked={addressId === address.id}
                  onChange={() => setAddressId(address.id)}
                  className="mt-1 size-4 accent-ink"
                />
                <div>
                  <p className="text-sm">{address.fullName}</p>
                  <p className="text-sm text-muted">
                    {address.line1}
                    {address.line2 ? `, ${address.line2}` : ""}
                  </p>
                  <p className="font-mono text-xs text-muted">
                    {address.postalCode} {address.city} · {address.phone}
                  </p>
                </div>
              </label>
            ))}
          </div>

          {!addingAddress && addresses !== undefined && addresses.length > 0 && (
            <button
              type="button"
              onClick={() => setAddingAddress(true)}
              className="mt-6 text-sm underline underline-offset-4"
            >
              Enviar para outra morada
            </button>
          )}

          {!addingAddress && (
            <div className="mt-8">
              <button
                type="button"
                disabled={addressId === null}
                onClick={() => setStep(1)}
                className="bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
              >
                Continuar
              </button>
            </div>
          )}
        </section>
      )}

      {step === 1 && (
        <section className="mt-10">
          <h2 className="text-2xl">Pagamento</h2>

          <div className="mt-6 space-y-px bg-line">
            {Object.values(PaymentMethod).map((value) => (
              <label key={value} className="flex cursor-pointer items-center gap-4 bg-bg p-5">
                <input
                  type="radio"
                  name="method"
                  checked={method === value}
                  onChange={() => setMethod(value)}
                  className="size-4 accent-ink"
                />
                <span className="text-sm">{METHOD_LABELS[value]}</span>
              </label>
            ))}
          </div>

          {method === PaymentMethod.MbWay && (
            <div className="mt-6">
              <Field
                label="Telemóvel"
                name="phone"
                inputMode="numeric"
                placeholder="912345678"
                value={phone}
                onChange={(event) => setPhone(event.target.value)}
              />
            </div>
          )}

          {method === PaymentMethod.Card && (
            <div className="mt-6">
              <Field
                label="Número do cartão"
                name="card"
                inputMode="numeric"
                placeholder="4539 5787 6362 1486"
                value={card}
                onChange={(event) => setCard(event.target.value)}
              />
              <p className="mt-2 text-xs text-muted">
                Pagamento simulado. Guardamos apenas os últimos quatro dígitos.
              </p>
            </div>
          )}

          <div className="mt-8 flex gap-3">
            <button
              type="button"
              disabled={!canPay}
              onClick={() => setStep(2)}
              className="bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
            >
              Continuar
            </button>
            <button
              type="button"
              onClick={() => setStep(0)}
              className="border border-line px-6 py-4 text-sm transition hover:border-ink"
            >
              Voltar
            </button>
          </div>
        </section>
      )}

      {step === 2 && cart && (
        <section className="mt-10">
          <h2 className="text-2xl">Revisão</h2>

          <div className="mt-6 space-y-px bg-line">
            {cart.items.map((line) => (
              <div key={line.id} className="flex justify-between gap-4 bg-bg p-4 text-sm">
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
              <dd className="font-mono">{formatPrice(cart.subtotal)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-muted">Portes</dt>
              <dd className="font-mono">
                {cart.shippingCost === 0 ? "Grátis" : formatPrice(cart.shippingCost)}
              </dd>
            </div>
            <div className="flex justify-between border-t border-line pt-2 text-base">
              <dt>Total</dt>
              <dd className="font-mono">{formatPrice(cart.total)}</dd>
            </div>
          </dl>

          <div className="mt-6 border border-line p-4 text-sm">
            <p className="font-mono text-xs uppercase tracking-widest text-muted">Envio</p>
            <p className="mt-2">{selected?.fullName}</p>
            <p className="text-muted">
              {selected?.line1}
              {selected?.line2 ? `, ${selected.line2}` : ""}
            </p>
            <p className="font-mono text-xs text-muted">
              {selected?.postalCode} {selected?.city}
            </p>
            <p className="mt-3 font-mono text-xs uppercase tracking-widest text-muted">Pagamento</p>
            <p className="mt-1">{METHOD_LABELS[method]}</p>
          </div>

          <div className="mt-8 flex gap-3">
            <button
              type="button"
              disabled={place.isPending}
              onClick={() => {
                setError(null);
                place.mutate();
              }}
              className="bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
            >
              {place.isPending ? "A confirmar…" : "Confirmar encomenda"}
            </button>
            <button
              type="button"
              disabled={place.isPending}
              onClick={() => setStep(1)}
              className="border border-line px-6 py-4 text-sm transition enabled:hover:border-ink disabled:opacity-40"
            >
              Voltar
            </button>
          </div>
        </section>
      )}
    </div>
  );
}
