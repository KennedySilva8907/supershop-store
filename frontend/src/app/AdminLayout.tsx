import { Link, NavLink, Outlet } from "react-router";
import { useAuth } from "../features/auth/AuthContext";

const LINKS = [
  { to: "/admin", label: "Painel", end: true },
  { to: "/admin/produtos", label: "Produtos", end: false },
  { to: "/admin/encomendas", label: "Encomendas", end: false },
];

export function AdminLayout() {
  const { user } = useAuth();

  return (
    <div className="flex min-h-screen">
      <aside className="w-56 shrink-0 border-r border-line bg-surface">
        <div className="border-b border-line px-5 py-4">
          <Link to="/" className="font-display text-lg tracking-tight">
            SUPERSHOP
          </Link>
          <p className="mt-1 font-mono text-[10px] uppercase tracking-widest text-muted">
            Backoffice
          </p>
        </div>

        <nav className="flex flex-col py-2">
          {LINKS.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.end}
              className={({ isActive }) =>
                `border-l-2 px-5 py-2 text-sm transition ${
                  isActive
                    ? "border-accent bg-bg text-ink"
                    : "border-transparent text-muted hover:text-ink"
                }`
              }
            >
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto border-t border-line px-5 py-4">
          <p className="font-mono text-[10px] text-muted">{user?.email}</p>
          <Link to="/" className="mt-2 block text-xs text-muted underline underline-offset-4 hover:text-ink">
            Voltar à loja
          </Link>
        </div>
      </aside>

      <main className="min-w-0 flex-1 bg-bg">
        <Outlet />
      </main>
    </div>
  );
}
