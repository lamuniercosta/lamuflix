import createClient from 'openapi-fetch';
import type { paths } from './types';

export function createApiClient(baseUrl: string) {
  return createClient<paths>({ baseUrl });
}

export const client = createApiClient(import.meta.env.VITE_API_BASE_URL || window.location.origin);
