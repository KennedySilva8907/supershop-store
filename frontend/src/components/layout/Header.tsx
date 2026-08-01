import { Link, NavLink } from "react-router";
import { useAuth } from "../../features/auth/AuthContext";
import { useCartCount } from "../../features/cart/useCart";
import { useCategories } from "../../features/catalog/queries";

export function Header() {
  const { data: categories } = useCategories();
  const { user } = useAuth();
  const count = useCartCount();

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-bg/95 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center gap-8 px-6 py-5">
        <Link to="/" className="font-display text-2xl tracking-tight">
          SUPERSHOP
        </Link>

        <nav className="hidden items-center gap-6 md:flex">
          <NavLink
            to="/catalogo"
            end
            className={({ isActive }) =>
              `text-sm transition ${isActive ? "text-ink" : "text-muted hover:text-ink"}`
            }
          >
            Tudo
          </NavLink>
          {categories?.map((category) => (
            <NavLink
              key={category.slug}
              to={`/catalogo/${category.slug}`}
              className={({ isActive }) =>
                `text-sm transition ${isActive ? "text-ink" : "text-muted hover:text-ink"}`
              }
            >
              {category.name}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-6">
          <Link
            to={user ? "/conta" : "/entrar"}
            className="text-sm text-muted transition hover:text-ink"
          >
            {user ? user.firstName : "Entrar"}
          </Link>

          <Link to="/carrinho" className="flex items-center gap-2 text-sm transition hover:text-ink">
            Carrinho
            {count > 0 && (
              <span className="min-w-5 rounded-full bg-accent px-1.5 py-0.5 text-center font-mono text-[11px] text-ink">
                {count}
              </span>
            )}
          </Link>
        </div>
      </div>
    </header>
  );
}
