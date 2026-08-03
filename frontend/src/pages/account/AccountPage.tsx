import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { useAuth } from "../../features/auth/AuthContext";
import { ApiError } from "../../lib/apiClient";

export function AccountPage() {
  const { user, isAdmin, updateProfile, signOut } = useAuth();
  const navigate = useNavigate();
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  if (!user) return null;

  async function onSignOut() {
    await signOut();
    navigate("/", { replace: true });
  }

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const phone = String(form.get("phoneNumber")).trim();

    setSaving(true);
    setError(null);

    try {
      await updateProfile({
        firstName: String(form.get("firstName")),
        lastName: String(form.get("lastName")),
        phoneNumber: phone === "" ? null : phone,
      });

      setEditing(false);
      setSaved(true);
    } catch (caught) {
      if (caught instanceof ApiError) setError(caught);
      else throw caught;
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      <div className="flex items-center justify-between gap-6">
        <h1 className="text-4xl">A minha conta</h1>
        {!editing && (
          <button
            type="button"
            onClick={() => {
              setEditing(true);
              setSaved(false);
              setError(null);
            }}
            className="border border-line px-5 py-2 text-sm transition hover:border-ink"
          >
            Editar
          </button>
        )}
      </div>

      {saved && !editing && (
        <p className="mt-6 border border-line bg-surface px-4 py-3 text-sm">Dados guardados.</p>
      )}

      {editing ? (
        <form onSubmit={onSubmit} className="mt-10 space-y-6">
          {error && <FormError message={error.message} traceId={error.problem.traceId} />}

          <div className="grid gap-6 sm:grid-cols-2">
            <Field
              label="Nome"
              name="firstName"
              defaultValue={user.firstName}
              required
              maxLength={60}
              errors={error?.fieldErrors.firstName}
            />
            <Field
              label="Apelido"
              name="lastName"
              defaultValue={user.lastName}
              required
              maxLength={60}
              errors={error?.fieldErrors.lastName}
            />
          </div>

          <Field
            label="Telemóvel"
            name="phoneNumber"
            type="tel"
            defaultValue={user.phoneNumber ?? ""}
            maxLength={20}
            errors={error?.fieldErrors.phoneNumber}
          />

          <p className="text-xs text-muted">
            O email não se altera aqui. Fica ligado à conta e é por onde entras.
          </p>

          <div className="flex gap-3">
            <button
              type="submit"
              disabled={saving}
              className="bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90 disabled:opacity-50"
            >
              {saving ? "A guardar..." : "Guardar"}
            </button>
            <button
              type="button"
              onClick={() => {
                setEditing(false);
                setError(null);
              }}
              className="border border-line px-6 py-3 text-sm transition hover:border-ink"
            >
              Cancelar
            </button>
          </div>
        </form>
      ) : (
        <dl className="mt-10 divide-y divide-line border-y border-line">
          <Row label="Nome" value={`${user.firstName} ${user.lastName}`} />
          <Row label="Email" value={user.email} mono />
          <Row label="Telemóvel" value={user.phoneNumber ?? "—"} mono />
          <Row label="Email confirmado" value={user.emailConfirmed ? "Sim" : "Não"} />
          <Row label="Perfil" value={user.roles.join(", ")} />
        </dl>
      )}

      {!editing && isAdmin && (
        <div className="mt-10 border border-line bg-surface px-5 py-4">
          <p className="font-mono text-[11px] uppercase tracking-widest text-muted">Backoffice</p>
          <p className="mt-2 text-sm">
            Esta conta gere a loja: produtos, stock e encomendas.
          </p>
          <Link
            to="/admin"
            className="mt-4 inline-block bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90"
          >
            Abrir o backoffice
          </Link>
        </div>
      )}

      {!editing && (
        <>
          <div className="mt-10 flex flex-wrap gap-3">
            <Link
              to="/conta/encomendas"
              className="bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90"
            >
              As minhas encomendas
            </Link>

            <Link
              to="/conta/moradas"
              className="border border-line px-6 py-3 text-sm transition hover:border-ink"
            >
              Gerir moradas
            </Link>

            <Link
              to="/conta/password"
              className="border border-line px-6 py-3 text-sm transition hover:border-ink"
            >
              Mudar password
            </Link>

            <button
              type="button"
              onClick={onSignOut}
              className="border border-line px-6 py-3 text-sm transition hover:border-ink"
            >
              Terminar sessão
            </button>
          </div>
        </>
      )}
    </div>
  );
}

function Row({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-6 py-4">
      <dt className="font-mono text-xs uppercase tracking-widest text-muted">{label}</dt>
      <dd className={mono ? "font-mono text-sm" : "text-sm"}>{value}</dd>
    </div>
  );
}
