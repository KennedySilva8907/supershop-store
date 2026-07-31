import { useState } from "react";
import { cloudinaryUrl } from "../../lib/cloudinary";
import type { ProductImage } from "../../types/catalog";

export function ImageGallery({ images, name }: { images: ProductImage[]; name: string }) {
  const [active, setActive] = useState(0);

  if (images.length === 0) {
    return <div className="aspect-square w-full border border-line bg-surface" />;
  }

  const current = images[active];

  return (
    <div className="flex flex-col gap-3">
      <img
        src={cloudinaryUrl(current.publicId, 800)}
        alt={current.altText || name}
        width={800}
        height={800}
        fetchPriority="high"
        className="aspect-square w-full border border-line bg-surface object-cover"
      />

      {images.length > 1 && (
        <div className="flex gap-3">
          {images.map((image, index) => (
            <button
              key={image.publicId}
              type="button"
              onClick={() => setActive(index)}
              aria-label={`Ver imagem ${index + 1} de ${images.length}`}
              aria-current={index === active}
              className={`w-20 border ${index === active ? "border-ink" : "border-line"}`}
            >
              <img
                src={cloudinaryUrl(image.publicId, 400)}
                alt=""
                width={80}
                height={80}
                loading="lazy"
                className="aspect-square w-full bg-surface object-cover"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
