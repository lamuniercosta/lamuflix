import { afterEach, describe, expect, it, vi } from 'vitest';
import { createApiClient } from './client';
import { normalizeApiFailure } from './reportApiFailure';

const client = createApiClient('https://test.local');

function okResponseWhoseBodyFails(error: Error): Response {
  const response = new Response('{}', {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
  vi.spyOn(response, 'json').mockRejectedValue(error);
  vi.spyOn(response, 'text').mockRejectedValue(error);
  return response;
}

async function captureRejection(): Promise<unknown> {
  try {
    await client.GET('/api/movies', { params: { query: {} as never } });
  } catch (thrown: unknown) {
    return thrown;
  }
  throw new Error('expected the call to reject');
}

describe('client transport provenance', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('marks a fetch rejection before any response as a network failure', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'));

    const thrown = await captureRejection();

    expect(normalizeApiFailure(thrown)?.kind).toBe('network');
  });

  it('leaves a post-response body-decoding TypeError unmarked', async () => {
    const decodeError = new TypeError('terminated', { cause: { code: 'Z_DATA_ERROR' } });
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(okResponseWhoseBodyFails(decodeError));

    const thrown = await captureRejection();

    expect(thrown).toBe(decodeError);
    expect(normalizeApiFailure(thrown)).toBeNull();
  });

  it('leaves a post-response SyntaxError unmarked', async () => {
    const parseError = new SyntaxError('Unexpected token <');
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(okResponseWhoseBodyFails(parseError));

    const thrown = await captureRejection();

    expect(thrown).toBe(parseError);
    expect(normalizeApiFailure(thrown)).toBeNull();
  });

  it('passes an abort through unmarked', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(
      new DOMException('The operation was aborted.', 'AbortError'),
    );

    const thrown = await captureRejection();

    expect(normalizeApiFailure(thrown)).toBeNull();
  });
});
