export interface WeightDetail {
  id: number;
  fk_WeightEntryId: number;

  tare: number;
  weight: number;

  fk_WeightedProductId?: number | null;

  weightedBy?: string | null;

  secondaryTare?: number | null;
  requiredAmount?: number | null;
  productPrice?: number | null;
}