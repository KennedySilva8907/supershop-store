import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { Field, FormError } from "../../components/ui/Field";
import { ApiError, apiGet, apiSend } from "../../lib/apiClient";
import type { AdminProductForm, Category, Collection, SaveProduct } from "../../types/admin";
import { ProductImages } from "./ProductImages";

function slugify(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export function AdminProductFormPage() {
  const { id } = useParams();
  const isNew = id === undefined;
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [error, setError] = useState<ApiError | null>(null);
  const [slug, setSlug] = useState("");
  const [slugTouched, setSlugTouched] = useState(false);

  const { data: categories } = useQuery({
    queryKey: ["categories"],
    queryFn: ({ signal }) => apiGet<Category[]>("/categories", signal),
  });

  const { data: collections } = useQuery({
    queryKey: ["collections"],
    queryFn: ({ signal }) => apiGet<Collection[]>("/collections", signal),
  });

  const { data: product, isPending } = useQuery({
    queryKey: ["admin", "product", id],
    queryFn: ({ signal }) => apiGet<AdminProductForm>(`/admin/products/${id}`, signal),
    enabled: !isNew,
  });

  const save = useMutation({
    mutationFn: (body: SaveProduct) =>
      isNew
        ? apiSend<AdminProductForm>("POST", "/admin/products", body)
        : apiSend<AdminProductForm>("PUT", `/admin/products/${id}`, body),
    onSuccess: (saved) => {
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["admin"] });
      if (isNew) navigate(`/admin/produtos/${saved.id}`, { replace: true });
    },
    onError: (caught) => setError(caught instanceof ApiError ? caught : null),
  });

  if (!isNew && isPending) {
    return <div className="px-8 py-6"><div className="h-96 animate-pulse bg-surface" /></div>;
  }

  const current = product ?? null;
  const slugValue = slugTouched || !isNew ? slug || current?.slug || "" : slug;

  function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const compareAt = String(form.get("compareAtPrice")).trim();

    save.mutate({
      name: String(form.get("name")),
      slug: String(form.get("slug")),
      description: String(form.get("description")),
      price: Number(form.get("price")),
      compareAtPrice: compareAt === "" ? null : Number(compareAt),
      categoryId: Number(form.get("categoryId")),
      collectionId: Number(form.get("collectionId")),
      isFeatured: form.get("isFeatured") === "on",
    });
  }

  return (
    <div className="px-8 py-6">
      <Link to="/admin/produtos" className="font-mono text-[11px] uppercase tracking-widest text-muted">
        &larr; Produtos
      </Link>

      <h1 className="mt-3 text-2xl">{isNew ? "Novo produto" : current?.name}</h1>

      <form onSubmit={onSubmit} className="mt-8 max-w-2xl space-y-6">
        {error && <FormError message={error.message} traceId={error.problem.traceId} />}

        <Field
          label="Nome"
          name="name"
          defaultValue={current?.name ?? ""}
          required
          maxLength={120}
          onChange={(event) => {
            if (isNew && !slugTouched) setSlug(slugify(event.target.value));
          }}
        />

        <Field
          label="Endereço"
          name="slug"
          value={slugValue}
          onChange={(event) => {
            setSlugTouched(true);
            setSlug(slugify(event.target.value));
          }}
          required
          maxLength={140}
          errors={error?.fieldErrors.slug}
        />

        <div>
          <label
            htmlFor="description"
            className="font-mono text-xs uppercase tracking-widest text-muted"
          >
            Descrição
          </label>
          <textarea
            id="description"
            name="description"
            defaultValue={current?.description ?? ""}
            required
            rows={5}
            className="mt-2 w-full border border-line bg-bg px-4 py-3 text-sm"
          />
        </div>

        <div className="grid gap-6 sm:grid-cols-2">
          <Field
            label="Preço"
            name="price"
            type="number"
            step="0.01"
            min="0"
            defaultValue={current?.price ?? ""}
            required
          />
          <Field
            label="Preço antes do desconto"
            name="compareAtPrice"
            type="number"
            step="0.01"
            min="0"
            defaultValue={current?.compareAtPrice ?? ""}
          />
        </div>

        <div className="grid gap-6 sm:grid-cols-2">
          <Select label="Categoria" name="categoryId" defaultValue={current?.categoryId}>
            {categories?.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </Select>

          <Select label="Linha" name="collectionId" defaultValue={current?.collectionId}>
            {collections?.map((collection) => (
              <option key={collection.id} value={collection.id}>
                {collection.name}
              </option>
            ))}
          </Select>
        </div>

        <label className="flex items-center gap-3 text-sm">
          <input type="checkbox" name="isFeatured" defaultChecked={current?.isFeatured ?? false} />
          Mostrar nos destaques da página inicial
        </label>

        {isNew && (
          <p className="border border-line bg-surface px-4 py-3 text-xs text-muted">
            Ao gravar, o produto fica com todos os tamanhos da categoria a zero. O stock define-se
            depois, na grelha, e as imagens acrescentam-se aqui.
          </p>
        )}

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={save.isPending}
            className="bg-ink px-6 py-3 text-sm text-bg transition hover:opacity-90 disabled:opacity-50"
          >
            {save.isPending ? "A guardar..." : "Guardar"}
          </button>

          {!isNew && (
            <Link
              to={`/admin/produtos/${id}/stock`}
              className="border border-line px-6 py-3 text-sm transition hover:border-ink"
            >
              Stock
            </Link>
          )}
        </div>
      </form>

      {!isNew && current && <ProductImages productId={current.id} />}
    </div>
  );
}

function Select({
  label,
  name,
  defaultValue,
  children,
}: {
  label: string;
  name: string;
  defaultValue?: number;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label htmlFor={name} className="font-mono text-xs uppercase tracking-widest text-muted">
        {label}
      </label>
      <select
        id={name}
        name={name}
        defaultValue={defaultValue}
        required
        className="mt-2 w-full border border-line bg-bg px-4 py-3 text-sm"
      >
        {children}
      </select>
    </div>
  );
}
