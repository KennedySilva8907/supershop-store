const KEY = "supershop.cart";

export interface GuestCartLine {
  productVariantId: number;
  quantity: number;
}

export function readGuestCart(): GuestCartLine[] {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return [];

    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];

    return parsed
      .filter(
        (line): line is GuestCartLine =>
          typeof line === "object" &&
          line !== null &&
          Number.isInteger((line as GuestCartLine).productVariantId) &&
          Number.isInteger((line as GuestCartLine).quantity),
      )
      .filter((line) => line.quantity > 0);
  } catch {
    return [];
  }
}

export function addToGuestCart(productVariantId: number, quantity: number) {
  const lines = readGuestCart();
  const existing = lines.find((line) => line.productVariantId === productVariantId);

  if (existing) {
    existing.quantity += quantity;
  } else {
    lines.push({ productVariantId, quantity });
  }

  write(lines);
  return lines;
}

export function setGuestQuantity(productVariantId: number, quantity: number) {
  const lines = readGuestCart().filter(
    (line) => line.productVariantId !== productVariantId || quantity > 0,
  );

  const existing = lines.find((line) => line.productVariantId === productVariantId);

  if (existing) {
    existing.quantity = quantity;
  }

  write(lines);
  return lines;
}

export function removeFromGuestCart(productVariantId: number) {
  const lines = readGuestCart().filter((line) => line.productVariantId !== productVariantId);
  write(lines);
  return lines;
}

export function clearGuestCart() {
  localStorage.removeItem(KEY);
}

export function guestCartCount(): number {
  return readGuestCart().reduce((total, line) => total + line.quantity, 0);
}

function write(lines: GuestCartLine[]) {
  localStorage.setItem(KEY, JSON.stringify(lines));
  window.dispatchEvent(new Event("supershop:cart"));
}
