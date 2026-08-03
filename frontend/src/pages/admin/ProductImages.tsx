import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { ApiError, apiGet, apiSend, apiUpload } from "../../lib/apiClient";
import { cloudinaryUrl } from "../../lib/cloudinary";
import type { AdminImage } from "../../types/admin";

export function ProductImages({ productId }: { productId: number }) {
  const queryClient = useQueryClient();
  const fileInput = useRef<HTMLInputElement>(null);
  const [error, setError] = useState<string | null>(null);

  const { data: images } = useQuery({
    queryKey: ["admin", "images", productId],
    queryFn: ({ signal }) => apiGet<AdminImage[]>(`/admin/products/${productId}/images`, signal),
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["admin", "images", productId] });
    queryClient.invalidateQueries({ queryKey: ["admin", "products"] });
  };

  const fail = (caught: unknown, fallback: string) =>
    setError(caught instanceof ApiError ? caught.message : fallback);

  const upload = useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append("file", file);
      form.append("altText", file.name.replace(/\.[^.]+$/, ""));

      return apiUpload<AdminImage>(`/admin/products/${productId}/images`, form);
    },
    onSuccess: () => {
      setError(null);
      invalidate();
      if (fileInput.current) fileInput.current.value = "";
    },
    onError: (caught) => fail(caught, "Não foi possível carregar a imagem."),
  });

  const makePrimary = useMutation({
    mutationFn: (imageId: number) =>
      apiSend("PATCH", `/admin/products/${productId}/images/${imageId}/primary`),
    onSuccess: () => {
      setError(null);
      invalidate();
    },
    onError: (caught) => fail(caught, "Não foi possível mudar a imagem principal."),
  });

  const remove = useMutation({
    mutationFn: (imageId: number) =>
      apiSend("DELETE", `/admin/products/${productId}/images/${imageId}`),
    onSuccess: () => {
      setError(null);
      invalidate();
    },
    onError: (caught) => fail(caught, "Não foi possível apagar a imagem."),
  });

  return (
    <section className="mt-12 max-w-2xl border-t border-line pt-8">
      <div className="flex items-center justify-between gap-6">
        <h2 className="text-xl">Imagens</h2>
        <input
          ref={fileInput}
          type="file"
          accept="image/png,image/jpeg,image/webp,image/avif"
          disabled={upload.isPending}
          onChange={(event) => {
            const file = event.target.files?.[0];
            if (file) upload.mutate(file);
          }}
          className="text-xs file:mr-3 file:border file:border-line file:bg-bg file:px-4 file:py-2 file:text-xs"
        />
      </div>

      <p className="mt-2 text-xs text-muted">
        PNG, JPEG, WebP ou AVIF, até 5 MB. A primeira que carregares fica a principal.
      </p>

      {error && <p className="mt-4 border border-danger px-4 py-2 text-sm text-danger">{error}</p>}
      {upload.isPending && <p className="mt-4 text-sm text-muted">A carregar...</p>}

      {images?.length === 0 && (
        <p className="mt-6 border border-line px-4 py-10 text-center text-sm text-muted">
          Este produto ainda não tem imagens. No catálogo aparece sem fotografia.
        </p>
      )}

      <ul className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-3">
        {images?.map((image) => (
          <li key={image.id} className="border border-line">
            <img
              src={cloudinaryUrl(image.publicId, 400)}
              alt={image.altText}
              width={400}
              height={400}
              loading="lazy"
              className="aspect-square w-full object-cover"
            />

            <div className="flex items-center justify-between gap-2 px-3 py-2">
              {image.isPrimary ? (
                <span className="bg-accent px-2 py-0.5 font-mono text-[10px] text-ink">principal</span>
              ) : (
                <button
                  type="button"
                  disabled={makePrimary.isPending}
                  onClick={() => makePrimary.mutate(image.id)}
                  className="text-xs underline underline-offset-4 disabled:opacity-40"
                >
                  Tornar principal
                </button>
              )}

              <button
                type="button"
                disabled={remove.isPending}
                onClick={() => remove.mutate(image.id)}
                className="text-xs text-danger underline underline-offset-4 disabled:opacity-40"
              >
                Apagar
              </button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
