import { useState } from "react";
import { Link } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { ApiError, apiSend } from "../../lib/apiClient";

export function ForgotPasswordPage() {
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const form = new FormData(event.currentTarget);

    try {
      await apiSend("POST", "/auth/forgot-password", { email: String(form.get("email")) });
      setSent(true);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught : new ApiError(0, { detail: "Não foi possível continuar." }));
    } finally {
      setBusy(false);
    }
  }

  if (sent) {
    return (
      <div className="mx-auto max-w-md px-6 py-20">
        <h1 className="text-4xl">Verifica o email</h1>
        <p className="mt-6 text-muted">
          Se existir uma conta com esse endereço, enviámos um link para definires uma nova password.
          É válido durante uma hora.
        </p>
        <Link to="/entrar" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
          Voltar a entrar
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md px-6 py-20">
      <h1 className="text-4xl">Recuperar password</h1>
      <p className="mt-4 text-sm text-muted">
        Escreve o teu email e enviamos um link para definires uma nova.
      </p>

      <form onSubmit={onSubmit} className="mt-10 space-y-6" noValidate>
        {error && <FormError message={error.message} traceId={error.problem.traceId} />}

        <Field label="Email" name="email" type="email" autoComplete="email" required errors={error?.fieldErrors.email} />

        <button
          type="submit"
          disabled={busy}
          className="w-full bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
        >
          {busy ? "A enviar…" : "Enviar link"}
        </button>
      </form>

      <p className="mt-8 text-sm">
        <Link to="/entrar" className="text-muted underline underline-offset-4 hover:text-ink">
          Voltar
        </Link>
      </p>
    </div>
  );
}
