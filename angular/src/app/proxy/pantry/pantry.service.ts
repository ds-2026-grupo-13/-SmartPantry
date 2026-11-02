import type { CreatePantryItemDto, GetPantryItemsInput, PantryItemDto, UpdatePantryItemDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PantryService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreatePantryItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/pantry',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/pantry/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetPantryItemsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PantryItemDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/pantry',
      params: { status: input.status, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  markAsConsumed = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry/${id}/mark-as-consumed`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdatePantryItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PantryItemDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/pantry/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}