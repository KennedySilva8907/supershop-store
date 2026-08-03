import { Field, FormError } from "../../components/ui/Field";
import type { ApiError } from "../../lib/apiClient";
import type { Address, SaveAddress } from "../../types/account";

interface Props {
  address?: Address | null;
  error?: ApiError | null;
  saving?: boolean;
  submitLabel?: string;
  onSubmit: (values: SaveAddress) => void;
  onCancel?: () => void;
}

export function AddressForm({
  address = null,
  error = null,
  saving = false,
  submitLabel = "Guardar",
  onSubmit,
  onCancel,
}: Props) {
  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    onSubmit({
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

  return (
    <form onSubmit={handleSubmit} className="space-y-6 border border-line p-6" noValidate>
      {error && <FormError message={error.message} traceId={error.problem.traceId} />}

      <Field label="Quem recebe" name="fullName" defaultValue={address?.fullName} required />
      <Field label="Morada" name="line1" defaultValue={address?.line1} required />
      <Field label="Complemento" name="line2" defaultValue={address?.line2 ?? ""} />

      <div className="grid grid-cols-2 gap-4">
        <Field
          label="Código postal"
          name="postalCode"
          placeholder="1234-567"
          defaultValue={address?.postalCode}
          required
        />
        <Field label="Localidade" name="city" defaultValue={address?.city} required />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Field label="País" name="country" maxLength={2} defaultValue={address?.country ?? "PT"} required />
        <Field label="Telemóvel" name="phone" defaultValue={address?.phone} required />
      </div>

      <label className="flex items-center gap-3 text-sm">
        <input
          type="checkbox"
          name="isDefault"
          defaultChecked={address?.isDefault ?? true}
          className="size-4 accent-ink"
        />
        Usar como morada principal
      </label>

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={saving}
          className="bg-ink px-6 py-3 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
        >
          {saving ? "A guardar…" : submitLabel}
        </button>

        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="border border-line px-6 py-3 text-sm transition hover:border-ink"
          >
            Cancelar
          </button>
        )}
      </div>
    </form>
  );
}
