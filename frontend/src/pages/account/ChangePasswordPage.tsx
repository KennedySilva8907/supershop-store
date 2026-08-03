import { useState } from "react";
import { Link } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { useAuth } from "../../features/auth/AuthContext";
import { ApiError } from "../../lib/apiClient";

export function ChangePasswordPage() {
  const { changePassword } = useAuth();
  const [saving, setSaving] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [mismatch, setMismatch] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = event.currentTarget;
    const data = new FormData(form);
    const newPassword = String(data.get("newPassword"));

    if (newPassword !== String(data.get("confirmPassword"))) {
      setMismatch(true);
      setError(null);
      return;
    }

    setMismatch(false);
    setSaving(true);
    setError(null);

    try {
      await changePassword({
        currentPassword: String(data.get("currentPassword")),
        newPassword,
      });

      form.reset();
      setDone(true);
    } catch (caught) {
      if (caught instanceof ApiError) setError(caught);
      else throw caught;
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-xl px-6 py-16">
      <h1 className="text-4xl">Mudar password</h1>

      {done && (
        <p className="mt-6 border border-line bg-surface px-4 py-3 text-sm">
          Password alterada. As sessões abertas noutros sítios foram terminadas.
        </p>
      )}

      <form onSubmit={onSubmit} className="mt-10 space-y-6">
        {error && <FormError message={error.message} traceId={error.problem.traceId} />}

        <Field
          label="Password atual"
          name="currentPassword"
          type="password"
          autoComplete="current-password"
          required
        />

        <Field
          label="Password nova"
          name="newPassword"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
        />

        <Field
          label="Repetir a nova"
          name="confirmPassword"
          type="password"
          autoComplete="new-password"
          required
          errors={mismatch ? ["As duas passwords não são iguais."] : undefined}
        />

        <p className="text-xs text-muted">
          Ao mudares a password, quem estiver com a tua sessão aberta noutro lado é
          desligado. Tu continuas ligado aqui.
        </p>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={saving}
            className="bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90 disabled:opacity-50"
          >
            {saving ? "A guardar..." : "Mudar password"}
          </button>
          <Link to="/conta" className="border border-line px-6 py-3 text-sm transition hover:border-ink">
            Voltar
          </Link>
        </div>
      </form>
    </div>
  );
}
