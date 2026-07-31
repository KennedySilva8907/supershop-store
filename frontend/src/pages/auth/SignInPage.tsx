import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { useAuth } from "../../features/auth/AuthContext";
import { ApiError } from "../../lib/apiClient";

export function SignInPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const destination = (location.state as { from?: string } | null)?.from ?? "/conta";

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const form = new FormData(event.currentTarget);

    try {
      await signIn({
        email: String(form.get("email")),
        password: String(form.get("password")),
      });
      navigate(destination, { replace: true });
    } catch (caught) {
      setError(caught instanceof ApiError ? caught : new ApiError(0, { detail: "Não foi possível entrar." }));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-md px-6 py-20">
      <h1 className="text-4xl">Entrar</h1>

      <form onSubmit={onSubmit} className="mt-10 space-y-6" noValidate>
        {error && <FormError message={error.message} traceId={error.problem.traceId} />}

        <Field
          label="Email"
          name="email"
          type="email"
          autoComplete="email"
          required
          errors={error?.fieldErrors.email}
        />
        <Field
          label="Password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          errors={error?.fieldErrors.password}
        />

        <button
          type="submit"
          disabled={busy}
          className="w-full bg-ink px-8 py-4 text-sm text-bg transition enabled:hover:opacity-90 disabled:opacity-40"
        >
          {busy ? "A entrar…" : "Entrar"}
        </button>
      </form>

      <div className="mt-8 space-y-2 text-sm text-muted">
        <p>
          Ainda não tens conta?{" "}
          <Link to="/registar" className="text-ink underline underline-offset-4">
            Criar conta
          </Link>
        </p>
        <p>
          <Link to="/recuperar-password" className="underline underline-offset-4 hover:text-ink">
            Esqueci-me da password
          </Link>
        </p>
      </div>
    </div>
  );
}
