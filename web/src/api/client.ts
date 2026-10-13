import createClient from 'openapi-fetch';
import type { paths } from './types';

function runtimeFetch(request: Request): ReturnType<typeof fetch> {
  return globalThis.fetch(request);
}

export function createApiClient(baseUrl: string) {
  return createClient<paths>({ baseUrl, fetch: runtimeFetch });
}

export const client = createApiClient(import.meta.env.VITE_API_BASE_URL || window.location.origin);
