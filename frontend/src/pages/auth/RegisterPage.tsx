import { useState } from "react";
import { Link } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { useAuth } from "../../features/auth/AuthContext";
import { ApiError } from "../../lib/apiClient";

export function RegisterPage() {
  const { register } = useAuth();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const form = new FormData(event.currentTarget);
    const email = String(form.get("email"));

    try {
      await register({
        email,
        password: String(form.get("password")),
        firstName: String(form.get("firstName")),
        lastName: String(form.get("lastName")),
      });
      setRegisteredEmail(email);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught : new ApiError(0, { detail: "Não foi possível criar a conta." }));
    } finally {
      setBusy(false);
    }
  }

  if (registeredEmail) {
    return (
      <div className="mx-auto max-w-md px-6 py-20">
        <h1 className="text-4xl">Confirma o email</h1>
        <p className="mt-6 text-muted">
          Enviámos uma mensagem para <span className="font-mono text-ink">{registeredEmail}</span>. Abre o
          link para ativares a conta.
        </p>
        <p className="mt-4 text-sm text-muted">
          Sem confirmação não é possível encomendar. Se não chegar em alguns minutos, verifica a
          pasta de spam.
        </p>
        <Link to="/entrar" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
          Ir para entrar
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md px-6 py-20">
      <h1 className="text-4xl">Criar conta</h1>

      <form onSubmit={onSubmit} className="mt-10 space-y-6" noValidate>
        {error && <FormError message={error.message} traceId={error.problem.traceId} />}

        <div className="grid grid-cols-2 gap-4">
          <Field label="Nome" name="firstName" autoComplete="given-name" required errors={error?.fieldErrors.firstName} />
          <Field label="Apelido" name="lastName" autoComplete="family-name" required errors={error?.fieldErrors.lastName} />
        </div>

        <Field label="Email" name="email" type="email" autoComplete="email" required errors={error?.fieldErrors.email} />
        <Field
          label="Password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
          errors={error?.fieldErrors.password}
        />

        <p className="text-xs text-muted">Mínimo de 8 caracteres.</p>

        <button
          type="submit"
          disabled={busy}
          className="w-full bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
        >
          {busy ? "A criar…" : "Criar conta"}
        </button>
      </form>

      <p className="mt-8 text-sm text-muted">
        Já tens conta?{" "}
        <Link to="/entrar" className="text-ink underline underline-offset-4">
          Entrar
        </Link>
      </p>
    </div>
  );
}
