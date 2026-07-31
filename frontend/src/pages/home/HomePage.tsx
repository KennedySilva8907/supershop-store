import { Link } from "react-router";
import { ProductGrid, ProductGridSkeleton } from "../../components/product/ProductGrid";
import { useCategories, useFeaturedProducts } from "../../features/catalog/queries";

export function HomePage() {
  const { data: featured, isPending } = useFeaturedProducts();
  const { data: categories } = useCategories();

  return (
    <>
      <section className="mx-auto max-w-7xl px-6 py-20 md:py-28">
        <p className="font-mono text-xs uppercase tracking-widest text-muted">AXIS · CORE</p>
        <h1 className="mt-4 max-w-3xl text-5xl/[1.15] md:text-7xl/[1.1]">
          Streetwear feito
          <br />
          para durar.
        </h1>
        <p className="mt-6 max-w-lg text-muted">
          Duas linhas. A AXIS é técnica, a CORE são os essenciais. Tudo desenhado e
          produzido pela SuperShop.
        </p>
        <Link
          to="/catalogo"
          className="mt-8 inline-block bg-ink px-8 py-4 text-sm text-bg transition hover:opacity-90"
        >
          Ver catálogo
        </Link>
      </section>

      <section className="mx-auto max-w-7xl px-6 pb-20">
        <div className="flex items-end justify-between">
          <h2 className="text-2xl md:text-3xl">Em destaque</h2>
          <Link to="/catalogo" className="text-sm text-muted underline-offset-4 hover:text-ink hover:underline">
            Ver tudo
          </Link>
        </div>

        <div className="mt-8">
          {isPending ? <ProductGridSkeleton count={8} /> : <ProductGrid products={featured ?? []} />}
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-6 pb-8">
        <h2 className="text-2xl md:text-3xl">Categorias</h2>
        <div className="mt-8 grid grid-cols-2 gap-px border border-line bg-line md:grid-cols-5">
          {categories?.map((category) => (
            <Link
              key={category.slug}
              to={`/catalogo/${category.slug}`}
              className="group bg-bg px-6 py-10 transition hover:bg-surface"
            >
              <span className="font-display text-xl tracking-tight">{category.name}</span>
            </Link>
          ))}
        </div>
      </section>
    </>
  );
}
