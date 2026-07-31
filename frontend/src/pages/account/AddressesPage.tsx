import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Field, FormError } from "../../components/ui/Field";
import { ApiError, apiGet, apiSend } from "../../lib/apiClient";

interface Address {
  id: number;
  fullName: string;
  line1: string;
  line2: string | null;
  postalCode: string;
  city: string;
  country: string;
  phone: string;
  isDefault: boolean;
}

export function AddressesPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<Address | "new" | null>(null);
  const [error, setError] = useState<ApiError | null>(null);

  const { data: addresses, isPending } = useQuery({
    queryKey: ["addresses"],
    queryFn: ({ signal }) => apiGet<Address[]>("/me/addresses", signal),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["addresses"] });

  const save = useMutation({
    mutationFn: (body: Omit<Address, "id">) =>
      editing === "new"
        ? apiSend<Address>("POST", "/me/addresses", body)
        : apiSend<Address>("PUT", `/me/addresses/${(editing as Address).id}`, body),
    onSuccess: () => {
      setEditing(null);
      setError(null);
      invalidate();
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught : null),
  });

  const remove = useMutation({
    mutationFn: (id: number) => apiSend("DELETE", `/me/addresses/${id}`),
    onSuccess: invalidate,
  });

  function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    save.mutate({
      fullName: String(form.get("fullName")),
      line1: String(form.get("line1")),
      line2: String(form.get("line2")) || null,
      postalCode: String(form.get("postalCode")),
      city: String(form.get("city")),
      country: String(form.get("country") || "PT"),
      phone: String(form.get("phone")),
      isDefault: form.get("isDefault") === "on",
    });
  }

  const current = editing === "new" ? null : editing;

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      <div className="flex items-center justify-between gap-6">
        <h1 className="text-4xl">Moradas</h1>
        {editing === null && (
          <button
            type="button"
            onClick={() => setEditing("new")}
            className="bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90"
          >
            Adicionar
          </button>
        )}
      </div>

      {editing !== null && (
        <form onSubmit={onSubmit} className="mt-10 space-y-6 border border-line p-6" noValidate>
          {error && <FormError message={error.message} traceId={error.problem.traceId} />}

          <Field label="Quem recebe" name="fullName" defaultValue={current?.fullName} required />
          <Field label="Morada" name="line1" defaultValue={current?.line1} required />
          <Field label="Complemento" name="line2" defaultValue={current?.line2 ?? ""} />

          <div className="grid grid-cols-2 gap-4">
            <Field label="Código postal" name="postalCode" placeholder="1234-567" defaultValue={current?.postalCode} required />
            <Field label="Localidade" name="city" defaultValue={current?.city} required />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="País" name="country" maxLength={2} defaultValue={current?.country ?? "PT"} required />
            <Field label="Telemóvel" name="phone" defaultValue={current?.phone} required />
          </div>

          <label className="flex items-center gap-3 text-sm">
            <input type="checkbox" name="isDefault" defaultChecked={current?.isDefault} className="size-4 accent-ink" />
            Usar como morada principal
          </label>

          <div className="flex gap-3">
            <button
              type="submit"
              disabled={save.isPending}
              className="bg-ink px-6 py-3 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
            >
              {save.isPending ? "A guardar…" : "Guardar"}
            </button>
            <button
              type="button"
              onClick={() => {
                setEditing(null);
                setError(null);
              }}
              className="border border-line px-6 py-3 text-sm transition hover:border-ink"
            >
              Cancelar
            </button>
          </div>
        </form>
      )}

      <div className="mt-10 space-y-px bg-line">
        {isPending && <div className="h-28 animate-pulse bg-surface" />}

        {addresses?.length === 0 && !isPending && editing === null && (
          <p className="bg-bg py-16 text-center text-sm text-muted">Ainda não tens moradas guardadas.</p>
        )}

        {addresses?.map((address) => (
          <article key={address.id} className="flex items-start justify-between gap-6 bg-bg p-5">
            <div>
              <p className="flex items-center gap-3 text-sm">
                {address.fullName}
                {address.isDefault && (
                  <span className="rounded-full bg-accent px-2 py-0.5 font-mono text-[10px] uppercase text-ink">
                    Principal
                  </span>
                )}
              </p>
              <p className="mt-1 text-sm text-muted">
                {address.line1}
                {address.line2 ? `, ${address.line2}` : ""}
              </p>
              <p className="font-mono text-xs text-muted">
                {address.postalCode} {address.city} · {address.country} · {address.phone}
              </p>
            </div>

            <div className="flex shrink-0 gap-4 text-sm">
              <button type="button" onClick={() => setEditing(address)} className="underline underline-offset-4">
                Editar
              </button>
              <button
                type="button"
                onClick={() => remove.mutate(address.id)}
                className="text-danger underline underline-offset-4"
              >
                Remover
              </button>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}
