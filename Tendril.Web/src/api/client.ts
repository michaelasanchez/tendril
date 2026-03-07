// const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

// // This flag prevents multiple simultaneous refreshes if 5 calls fail at once
// let isRefreshing = false;

// async function handleResponse<T>(res: Response, retryOriginalRequest: () => Promise<T>): Promise<T> {
//   // If we get a 401, try to refresh the session
//   if (res.status === 401 && !isRefreshing) {
//     isRefreshing = true;
//     try {
//       const refreshRes = await fetch(`${BASE_URL}/api/user/refresh`, {
//         method: 'POST',
//         credentials: 'include'
//       });

//       if (refreshRes.ok) {
//         isRefreshing = false;
//         // The cookie is now updated! Retry the original call
//         return await retryOriginalRequest();
//       }
//     } catch (err) {
//       console.error("Refresh flow failed", err);
//     } finally {
//       isRefreshing = false;
//     }

//     // If refresh fails, throw the original error so the UI can redirect to login
//     throw new Error("Session expired");
//   }

//   if (!res.ok) {
//     const text = await res.text();
//     throw new Error(text || res.statusText);
//   }

//   if (res.status === 204) return undefined as unknown as T;
//   return res.json() as Promise<T>;
// }

// // Updated apiGet with retry logic
// export async function apiGet<T>(path: string, signal?: AbortSignal): Promise<T> {
//   const sendRequest = () => fetch(`${BASE_URL}${path}`, { credentials: 'include', signal });
//   const res = await sendRequest();
//   return handleResponse<T>(res, () => apiGet<T>(path, signal));
// }

// // ... Repeat the logic for apiPost, apiPut, etc.

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  if (res.status === 204) return undefined as unknown as T;
  return res.json() as Promise<T>;
}

export async function apiGet<T>(
  path: string,
  signal?: AbortSignal,
): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    credentials: 'include',
    signal,
  });
  return handleResponse<T>(res);
}

export async function apiPatch<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: body ? JSON.stringify(body) : undefined,
  });
  return handleResponse<T>(res);
}

export async function apiPost<T>(
  path: string,
  body?: unknown,
  signal?: AbortSignal,
): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: body ? JSON.stringify(body) : undefined,
    signal,
  });
  return handleResponse<T>(res);
}

export async function apiPut<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: body ? JSON.stringify(body) : undefined,
  });
  return handleResponse<T>(res);
}

export async function apiDelete<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'DELETE',
    credentials: 'include',
  });
  return handleResponse<T>(res);
}
