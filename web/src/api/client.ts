import createClient from 'openapi-fetch';
import { markApiTransportFailure } from './reportApiFailure';
import type { paths } from './types';

// The only transport boundary: a rejection here happens before any response exists.
// Errors raised later by openapi-fetch while decoding or parsing the body stay unmarked.
function runtimeFetch(request: Request): ReturnType<typeof fetch> {
  return globalThis.fetch(request).catch((thrown: unknown) => {
    throw markApiTransportFailure(thrown);
  });
}

export function createApiClient(baseUrl: string) {
  return createClient<paths>({ baseUrl, fetch: runtimeFetch });
}

export const client = createApiClient(import.meta.env.VITE_API_BASE_URL || window.location.origin);
