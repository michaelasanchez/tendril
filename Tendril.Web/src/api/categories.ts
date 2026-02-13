import type { Guid, Category } from '../types/api';
import { apiDelete, apiGet, apiPost, apiPut } from './client';

export interface CreateCategoryRequest {
  name: string;
  description: string;
}

export interface UpdateCategoryRequest extends Partial<CreateCategoryRequest> {}

export const CategoriesApi = {
  getAll(signal?: AbortSignal): Promise<Category[]> {
    return apiGet('/categories', signal);
  },

  create(req: CreateCategoryRequest): Promise<Category> {
    return apiPost('/categories', req);
  },

  update(id: Guid, req: UpdateCategoryRequest): Promise<void> {
    return apiPut(`/categories/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/categories/${id}`);
  },
};
