import type { UserProfile } from '../types/auth';
import { apiGet, apiPost } from './client';

// Define the interface to match your C# User return type
// export interface UserProfile {
//   name: string;
//   pictureUrl: string;
//   email: string;
// }

export const UserApi = {
  // GET /api/user/me
  getMe(signal?: AbortSignal): Promise<UserProfile> {
    return apiGet<UserProfile>('/api/user/me', signal);
  },

  // POST /api/user/login
  // Sending the code as a string directly
  login(code: string, signal?: AbortSignal): Promise<UserProfile> {
    return apiPost<UserProfile>('/api/user/login', code);
  },

  // POST /api/user/logout
  logout(): Promise<void> {
    return apiPost<void>('/api/user/logout');
  },

  // POST /api/user/refresh
  refresh(): Promise<void> {
    return apiPost<void>('/api/user/refresh');
  }
};