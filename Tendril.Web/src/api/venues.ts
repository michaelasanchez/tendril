// src/api/venues.ts
import { apiGet, apiPost, apiPut, apiDelete } from "./client";
import type { Venue, Guid } from "../types/api";

export interface CreateVenueRequest {
  name: string;
  address: string;
  website?: string;
}

export interface UpdateVenueRequest extends Partial<CreateVenueRequest> {}

export const VenuesApi = {
  getAll(): Promise<Venue[]> {
    return apiGet("/api/venues");
  },

  create(req: CreateVenueRequest): Promise<Venue> {
    return apiPost("/api/venues", req);
  },

  update(id: Guid, req: UpdateVenueRequest): Promise<void> {
    return apiPut(`/api/venues/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/api/venues/${id}`);
  }
};
