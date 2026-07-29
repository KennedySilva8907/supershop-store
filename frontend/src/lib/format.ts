const currency = new Intl.NumberFormat("pt-PT", {
  style: "currency",
  currency: "EUR",
});

const date = new Intl.DateTimeFormat("pt-PT", {
  day: "2-digit",
  month: "long",
  year: "numeric",
});

export const formatPrice = (value: number) => currency.format(value);

export const formatDate = (value: string) => date.format(new Date(value));
