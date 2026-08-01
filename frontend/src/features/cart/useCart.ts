import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { apiGet, apiSend } from "../../lib/apiClient";
import type { Cart } from "../../types/cart";
import {
  addToGuestCart,
  clearGuestCart,
  guestCartCount,
  readGuestCart,
  removeFromGuestCart,
  setGuestQuantity,
} from "./guestCart";

export function useCart() {
  const { user } = useAuth();

  return useQuery({
    queryKey: ["cart"],
    queryFn: ({ signal }) => apiGet<Cart>("/cart", signal),
    enabled: Boolean(user),
  });
}

export function useCartActions() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const invalidate = useCallback(
    () => queryClient.invalidateQueries({ queryKey: ["cart"] }),
    [queryClient],
  );

  const add = useMutation({
    mutationFn: async ({ variantId, quantity }: { variantId: number; quantity: number }) => {
      if (!user) {
        addToGuestCart(variantId, quantity);
        return null;
      }

      return apiSend<Cart>("POST", "/cart/items", {
        productVariantId: variantId,
        quantity,
      });
    },
    onSuccess: invalidate,
  });

  const setQuantity = useMutation({
    mutationFn: async ({ line, quantity }: { line: { id: number; productVariantId: number }; quantity: number }) => {
      if (!user) {
        setGuestQuantity(line.productVariantId, quantity);
        return null;
      }

      return quantity <= 0
        ? apiSend<Cart>("DELETE", `/cart/items/${line.id}`)
        : apiSend<Cart>("PUT", `/cart/items/${line.id}`, { quantity });
    },
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: async (line: { id: number; productVariantId: number }) => {
      if (!user) {
        removeFromGuestCart(line.productVariantId);
        return null;
      }

      return apiSend<Cart>("DELETE", `/cart/items/${line.id}`);
    },
    onSuccess: invalidate,
  });

  return { add, setQuantity, remove };
}

export function useCartCount(): number {
  const { user } = useAuth();
  const { data } = useCart();
  const [guestCount, setGuestCount] = useState(() => guestCartCount());

  useEffect(() => {
    const update = () => setGuestCount(guestCartCount());

    window.addEventListener("supershop:cart", update);
    window.addEventListener("storage", update);

    return () => {
      window.removeEventListener("supershop:cart", update);
      window.removeEventListener("storage", update);
    };
  }, []);

  return user ? (data?.itemCount ?? 0) : guestCount;
}

export function useGuestCartMerge() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const merged = useRef<string | null>(null);

  useEffect(() => {
    if (!user || merged.current === user.id) {
      return;
    }

    merged.current = user.id;
    const items = readGuestCart();

    if (items.length === 0) {
      return;
    }

    apiSend<Cart>("POST", "/cart/merge", { items })
      .then(() => {
        clearGuestCart();
        queryClient.invalidateQueries({ queryKey: ["cart"] });
      })
      .catch(() => undefined);
  }, [user, queryClient]);
}
