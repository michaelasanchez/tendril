import type { Guid, Venue } from '../types/api';
import { apiDelete, apiGet, apiPost, apiPut } from './client';

export interface CreateVenueRequest {
  name: string;
  address: string;
  website?: string;
}

export interface UpdateVenueRequest extends Partial<CreateVenueRequest> {}

export const VenuesApi = {
  getAll(signal?: AbortSignal): Promise<Venue[]> {
    return apiGet('/venues', signal);
  },

  create(req: CreateVenueRequest): Promise<Venue> {
    return apiPost('/venues', req);
  },

  update(id: Guid, req: UpdateVenueRequest): Promise<void> {
    return apiPut(`/venues/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/venues/${id}`);
  },
};
