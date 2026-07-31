import type { ProductVariant } from "../../types/catalog";

interface Props {
  variants: ProductVariant[];
  selectedId: number | null;
  onSelect: (variantId: number) => void;
}

export function SizeSelector({ variants, selectedId, onSelect }: Props) {
  return (
    <fieldset>
      <legend className="font-mono text-xs uppercase tracking-widest text-muted">Tamanho</legend>

      <div className="mt-3 flex flex-wrap gap-2">
        {variants.map((variant) => {
          const disabled = !variant.isInStock;
          const selected = variant.id === selectedId;

          return (
            <label
              key={variant.id}
              className={[
                "relative flex min-w-14 cursor-pointer items-center justify-center border px-4 py-3 font-mono text-sm transition",
                disabled
                  ? "cursor-not-allowed border-line text-muted line-through opacity-60"
                  : selected
                    ? "border-ink bg-ink text-bg"
                    : "border-line hover:border-ink",
              ].join(" ")}
            >
              <input
                type="radio"
                name="size"
                value={variant.id}
                checked={selected}
                disabled={disabled}
                onChange={() => onSelect(variant.id)}
                className="sr-only"
              />
              {variant.sizeLabel}
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}
