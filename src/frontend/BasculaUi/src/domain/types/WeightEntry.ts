import type { WeightDetail } from "./WeightDetail";

export interface WeightEntry {
  id: number;
  partnerId?: number | null;

  tareWeight: number;   // initial weight
  bruteWeight: number;  // final weight

  concludeDate?: Date | string | null;
  createdAt?: Date | string | null;

  vehiclePlate: string;
  notes?: string | null;
  registeredBy?: string | null;

  weightDetails: WeightDetail[];
}