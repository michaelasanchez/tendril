// src/scheduledTasks.ts
import type { Guid, ScheduledTask } from '../types/api';
import { apiDelete, apiGet, apiPost, apiPut } from './client';

export interface CreateScheduledTaskRequest {
  name: string;
  notes?: string | null;
  isDisabled: boolean;
  cronExpression: string;
  selectionStrategy: 'All' | 'Selected';
  scraperIds?: Guid[] | null;
}

export interface UpdateScheduledTaskRequest extends Partial<CreateScheduledTaskRequest> {
  nextRunAtUtc?: string | null; // ISO Date String
  status?: string | null;
}

export const ScheduledTasksApi = {
  getAll(): Promise<ScheduledTask[]> {
    return apiGet('/scheduled-tasks');
  },

  getById(id: Guid): Promise<ScheduledTask> {
    return apiGet(`/scheduled-tasks/${id}`);
  },

  create(req: CreateScheduledTaskRequest): Promise<ScheduledTask> {
    return apiPost('/scheduled-tasks', req);
  },

  update(id: Guid, req: UpdateScheduledTaskRequest): Promise<void> {
    return apiPut(`/scheduled-tasks/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/scheduled-tasks/${id}`);
  },
};
