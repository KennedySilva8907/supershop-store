const CLOUD_NAME = import.meta.env.VITE_CLOUDINARY_CLOUD_NAME;

export type ImageWidth = 400 | 600 | 800 | 1200;

export function cloudinaryUrl(publicId: string, width: ImageWidth = 600): string {
  const transformations = `f_auto,q_auto,w_${width},c_fill,ar_1:1`;
  return `https://res.cloudinary.com/${CLOUD_NAME}/image/upload/${transformations}/${publicId}`;
}

export function cloudinarySrcSet(publicId: string, widths: ImageWidth[]): string {
  return widths.map((w) => `${cloudinaryUrl(publicId, w)} ${w}w`).join(", ");
}
