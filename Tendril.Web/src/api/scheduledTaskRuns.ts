import type { ScheduledTaskRun } from '../types/api';
import { apiGet } from './client';

export const ScheduledTaskRunsApi = {
  getRecent(limit = 30, signal?: AbortSignal): Promise<ScheduledTaskRun[]> {
    const query = new URLSearchParams({ limit: String(limit) });
    return apiGet(`/scheduled-task-runs/recent?${query}`, signal);
  },
};
