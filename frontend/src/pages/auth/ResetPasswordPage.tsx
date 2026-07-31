import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { ApiError, apiSend } from "../../lib/apiClient";

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const email = params.get("email");
  const token = params.get("token");

  if (!email || !token) {
    return (
      <div className="mx-auto max-w-md px-6 py-24 text-center">
        <h1 className="text-4xl">Link inválido</h1>
        <p className="mt-6 text-muted">Este link está incompleto. Pede um novo.</p>
        <Link to="/recuperar-password" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
          Pedir novo link
        </Link>
      </div>
    );
  }

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const form = new FormData(event.currentTarget);
    const password = String(form.get("password"));

    if (password !== String(form.get("confirm"))) {
      setError(new ApiError(0, { detail: "As passwords não coincidem." }));
      setBusy(false);
      return;
    }

    try {
      await apiSend("POST", "/auth/reset-password", { email, token, newPassword: password });
      navigate("/entrar", { replace: true });
    } catch (caught) {
      setError(caught instanceof ApiError ? caught : new ApiError(0, { detail: "Não foi possível continuar." }));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-6 py-20">
      <h1 className="text-4xl">Nova password</h1>
      <p className="mt-4 text-sm text-muted">
        Para <span className="font-mono text-ink">{email}</span>
      </p>

      <form onSubmit={onSubmit} className="mt-10 space-y-6" noValidate>
        {error && <FormError message={error.message} traceId={error.problem.traceId} />}

        <Field
          label="Nova password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
          errors={error?.fieldErrors.newPassword}
        />
        <Field label="Repetir password" name="confirm" type="password" autoComplete="new-password" required />

        <p className="text-xs text-muted">Mínimo de 8 caracteres.</p>

        <button
          type="submit"
          disabled={busy}
          className="w-full bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
        >
          {busy ? "A guardar…" : "Definir password"}
        </button>
      </form>
    </div>
  );
}
