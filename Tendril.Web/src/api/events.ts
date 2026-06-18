import type { Event, EventResponse, EventStatus, Guid, PendingEventReviewDto } from '../types/api';
import { apiGet, apiPatch } from './client';

export interface PatchEventRequest {
  categoryId?: Guid;
  status?: EventStatus;
  requiresReview?: boolean;
}

export interface EventFilter {
  title?: string;
  startDate?: string;
  endDate?: string;
  categoryIds?: Guid[];
  venueIds?: Guid[];
}

export const EventsApi = {
  search(
    filter: EventFilter | null,
    cursor: Guid | null,
    signal?: AbortSignal,
  ): Promise<EventResponse> {
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

    filter?.categoryIds?.forEach((c) => params.append('category', c));
    filter?.venueIds?.forEach((v) => params.append('venue', v));

    if (!!cursor) {
      params.append('cursor', cursor);
    }

    const queryString = !!params.size ? `?${params.toString()}` : '';

    return apiGet(`/events${queryString}`, signal);
  },

  getById(evendId: Guid): Promise<Event> {
    return apiGet(`/events/${evendId}`);
  },

  getByScraperId(scraperId: Guid): Promise<Event[]> {
    return apiGet(`/events/scraper/${scraperId}`);
  },

  getPending(signal?: AbortSignal): Promise<PendingEventReviewDto[]> {
    return apiGet('/events/pending', signal);
  },

  patch(eventId: Guid, request: PatchEventRequest): Promise<void> {
    return apiPatch(`/events/${eventId}`, request);
  },
};
