export interface Address {
  id: number;
  fullName: string;
  line1: string;
  line2: string | null;
  postalCode: string;
  city: string;
  country: string;
  phone: string;
  isDefault: boolean;
}

export type SaveAddress = Omit<Address, "id">;
