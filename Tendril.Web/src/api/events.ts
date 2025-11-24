// src/api/events.ts
import { apiGet } from "./client";
import type { Event } from '../types/api';

export const EventsApi = {
  getAll(): Promise<Event[]> {
    return apiGet("/api/events");
  }
};
