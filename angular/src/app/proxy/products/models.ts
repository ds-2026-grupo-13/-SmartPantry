import type { EntityDto } from '@abp/ng.core';

export interface CreateProductDto {
  name?: string;
  brand?: string;
  barcode?: string;
}

export interface ProductDto extends EntityDto<string> {
  name?: string;
  brand?: string;
  barcode?: string;
}

export interface UpdateProductDto {
  name: string;
  brand: string;
  barcode: string;
}
