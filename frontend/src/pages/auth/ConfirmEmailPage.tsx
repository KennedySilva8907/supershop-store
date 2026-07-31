import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { apiSend } from "../../lib/apiClient";

type State = "confirming" | "done" | "failed";

export function ConfirmEmailPage() {
  const [params] = useSearchParams();
  const [state, setState] = useState<State>("confirming");
  const started = useRef(false);

  const userId = params.get("userId");
  const token = params.get("token");

  useEffect(() => {
    if (started.current) return;
    started.current = true;

    if (!userId || !token) {
      setState("failed");
      return;
    }

    apiSend("POST", "/auth/confirm-email", { userId, token })
      .then(() => setState("done"))
      .catch(() => setState("failed"));
  }, [userId, token]);

  return (
    <div className="mx-auto max-w-md px-6 py-24 text-center">
      {state === "confirming" && (
        <>
          <h1 className="text-3xl">A confirmar</h1>
          <div className="mx-auto mt-8 h-2 w-40 animate-pulse bg-surface" />
        </>
      )}

      {state === "done" && (
        <>
          <h1 className="text-4xl">Conta confirmada</h1>
          <p className="mt-6 text-muted">Já podes entrar e comprar.</p>
          <Link to="/entrar" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
            Entrar
          </Link>
        </>
      )}

      {state === "failed" && (
        <>
          <h1 className="text-4xl">Link inválido</h1>
          <p className="mt-6 text-muted">
            Este link já foi usado ou expirou. Pede um novo a partir da página de entrada.
          </p>
          <Link to="/entrar" className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg">
            Ir para entrar
          </Link>
        </>
      )}
    </div>
  );
}
