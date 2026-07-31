import { Link, useNavigate } from "react-router";
import { useAuth } from "../../features/auth/AuthContext";

export function AccountPage() {
  const { user, signOut } = useAuth();
  const navigate = useNavigate();

  if (!user) return null;

  async function onSignOut() {
    await signOut();
    navigate("/", { replace: true });
  }

  return (
    <div className="mx-auto max-w-3xl px-6 py-16">
      <h1 className="text-4xl">A minha conta</h1>

      <dl className="mt-10 divide-y divide-line border-y border-line">
        <Row label="Nome" value={`${user.firstName} ${user.lastName}`} />
        <Row label="Email" value={user.email} mono />
        <Row label="Telemóvel" value={user.phoneNumber ?? "—"} mono />
        <Row label="Email confirmado" value={user.emailConfirmed ? "Sim" : "Não"} />
        <Row label="Perfil" value={user.roles.join(", ")} />
      </dl>

      <Link
        to="/conta/moradas"
        className="mt-10 inline-block bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90"
      >
        Gerir moradas
      </Link>

      <button
        type="button"
        onClick={onSignOut}
        className="ml-3 mt-10 border border-line px-6 py-3 text-sm transition hover:border-ink"
      >
        Terminar sessão
      </button>
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
