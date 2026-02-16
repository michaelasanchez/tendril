import type { Event, EventStatus, Guid, PagedResponse } from '../types/api';
import { apiGet, apiPatch } from './client';

export interface PatchEventRequest {
  categoryId?: Guid;
  status?: EventStatus;
}

export interface EventFilter {
  title?: string;
  startDate?: string;
  endDate?: string;
  categories?: string[];
  venueIds?: string[];
}

export const EventsApi = {
  get(
    filter: EventFilter | null,
    cursor: Guid | null,
    signal?: AbortSignal,
  ): Promise<PagedResponse> {
    const params = new URLSearchParams();

    if (!!filter?.title) {
      params.append('title', filter.title);
    }

    if (!!filter?.startDate) {
      params.append('startDate', filter.startDate);
    }

    if (!!filter?.endDate) {
      params.append('endDate', filter.endDate);
    }

    filter?.categories?.forEach((c) => params.append('category', c));
    filter?.venueIds?.forEach((v) => params.append('venue', v));

    if (!!cursor) {
      params.append('cursor', cursor);
    }

    const queryString = !!params.size ? `?${params.toString()}` : '';

    return apiGet(`/events${queryString}`, signal);
  },

  getByScraperId(scraperId: Guid): Promise<Event[]> {
    return apiGet(`/events/${scraperId}`);
  },

  patch(eventId: Guid, request: PatchEventRequest): Promise<void> {
    return apiPatch(`/events/${eventId}`, request);
  },
};
