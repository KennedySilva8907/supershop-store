import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { AddressForm } from "../../features/account/AddressForm";
import { ApiError, apiGet, apiSend } from "../../lib/apiClient";
import type { Address, SaveAddress } from "../../types/account";

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
    mutationFn: (body: SaveAddress) =>
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
        <div className="mt-10">
          <AddressForm
            address={current}
            error={error}
            saving={save.isPending}
            onSubmit={(values) => save.mutate(values)}
            onCancel={() => {
              setEditing(null);
              setError(null);
            }}
          />
        </div>
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
