import { apiGet } from './client';

export type WorkerAvailability = 'Online' | 'Offline' | 'Stopped' | 'Unknown';

export interface WorkerStatus {
  status: WorkerAvailability;
  isOnline: boolean;
  sessionId: string | null;
  machineName: string | null;
  processId: number | null;
  startedAtUtc: string | null;
  lastHeartbeatAtUtc: string | null;
  stoppedAtUtc: string | null;
  heartbeatAgeSeconds: number | null;
}

export const WorkerApi = {
  getStatus(signal?: AbortSignal): Promise<WorkerStatus> {
    return apiGet('/worker/status', signal);
  },
};
