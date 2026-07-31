import { Link, NavLink } from "react-router";
import { useCategories } from "../../features/catalog/queries";

export function Header() {
  const { data: categories } = useCategories();

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
      </div>
    </header>
  );
}
