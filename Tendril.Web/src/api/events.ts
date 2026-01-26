import type { Event, EventStatus, Guid } from '../types/api';
import { apiGet, apiPatch } from './client';

export interface PatchEventRequest {
  category?: string;
  status?: EventStatus;
}

export const EventsApi = {
  getAll(): Promise<Event[]> {
    return apiGet('/api/events');
  },

  getByScraperId(scraperId: Guid): Promise<Event[]> {
    return apiGet(`/api/events/${scraperId}`);
  },

  patch(eventId: Guid, request: PatchEventRequest): Promise<void> {
    return apiPatch(`/api/events/${eventId}`, request);
  },
};
