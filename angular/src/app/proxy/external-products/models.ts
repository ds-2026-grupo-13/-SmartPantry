import type { ExternalProductLookupStatus } from './external-product-lookup-status.enum';

export interface ExternalProductLookupResultDto {
  status?: ExternalProductLookupStatus;
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  quantity?: string | null;
  imageUrl?: string | null;
}

export interface GetExternalProductInput {
  barcode: string;
}
