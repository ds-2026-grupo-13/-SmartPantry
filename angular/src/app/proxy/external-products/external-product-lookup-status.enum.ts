import { mapEnumToOptions } from '@abp/ng.core';

export enum ExternalProductLookupStatus {
  Found = 0,
  NotFound = 1,
  RateLimited = 2,
  ProviderUnavailable = 3,
}

export const externalProductLookupStatusOptions = mapEnumToOptions(ExternalProductLookupStatus);
