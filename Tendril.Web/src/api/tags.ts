import type { Guid, Tag } from '../types/api';
import { apiDelete, apiGet, apiPost, apiPut } from './client';

export interface CreateTagRequest {
  name: string;
}

export interface UpdateTagRequest extends Partial<CreateTagRequest> {}

export const TagApi = {
  getAll(signal?: AbortSignal): Promise<Tag[]> {
    return apiGet('/tags', signal);
  },

  create(req: CreateTagRequest): Promise<Tag> {
    return apiPost('/tags', req);
  },

  update(id: Guid, req: UpdateTagRequest): Promise<void> {
    return apiPut(`/tags/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/tags/${id}`);
  },
};
