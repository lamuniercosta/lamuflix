import { describe, expect, it, vi } from 'vitest';
import { createApiFailureToastQueue } from '../ui/apiFailureToastQueue';
import { markApiTransportFailure, normalizeApiFailure, reportApiFailure } from './reportApiFailure';
import type { components } from './types';

type ProblemDetails = components['schemas']['ProblemDetails'];

function problem(overrides: Partial<ProblemDetails> = {}): ProblemDetails {
  return { status: 500, title: 'Server problem', ...overrides };
}

function response(status: number, body = ''): Response {
  return new Response(body, { status });
}

describe('normalizeApiFailure', () => {
  it('prefers the actual response status over a conflicting problem status', () => {
    const result = normalizeApiFailure({
      response: response(503),
      error: problem({
        status: 429,
        title: 'Degraded',
        reason: 'queue full',
        traceId: 'trace-503',
      }),
    });

    expect(result).toEqual({
      kind: 'http',
      title: 'Degraded',
      status: '503',
      reason: 'queue full',
      traceId: 'trace-503',
    });
  });

  it('falls back to a valid structured problem status when no response is available', () => {
    const result = normalizeApiFailure({
      error: problem({
        status: '429',
        title: 'Too many requests',
        reason: 'quota',
        traceId: 'trace-429',
      }),
    });

    expect(result).toEqual({
      kind: 'http',
      title: 'Too many requests',
      status: '429',
      reason: 'quota',
      traceId: 'trace-429',
    });
  });

  it('omits absent optional fields and uses the generic HTTP title', () => {
    const result = normalizeApiFailure({ error: problem({ title: null }) });

    expect(result).toEqual({
      kind: 'http',
      title: 'Request failed',
      status: '500',
      reason: undefined,
      traceId: 'Unavailable',
    });
  });

  it.each(['', '<html>Bad gateway</html>', '{not json'])(
    'keeps the known HTTP status when the body is unusable (%s)',
    (body) => {
      const result = normalizeApiFailure({ response: response(502, body), error: body });

      expect(result).toEqual({
        kind: 'http',
        title: 'Request failed',
        status: '502',
        reason: undefined,
        traceId: 'Unavailable',
      });
      expect(JSON.stringify(result)).not.toContain('Bad gateway');
    },
  );

  it('classifies an explicitly marked fetch rejection as a network failure', () => {
    const result = normalizeApiFailure(markApiTransportFailure(new TypeError('Failed to fetch')));

    expect(result).toEqual({
      kind: 'network',
      title: 'Network error',
      status: 'Unavailable',
      reason: undefined,
      traceId: 'Unavailable',
    });
    expect(JSON.stringify(result)).not.toContain('Failed to fetch');
  });

  it('ignores unknown throws, unmarked TypeErrors, aborts and non-failure statuses', () => {
    const abort = new DOMException('Aborted', 'AbortError');

    expect(markApiTransportFailure(abort)).toBe(abort);
    for (const value of [
      new TypeError('Failed to fetch'),
      new Error('programming bug'),
      'nope',
      42,
      { status: 500, message: 'programming bug' },
      { error: problem({ status: 200 }) },
      abort,
      null,
      undefined,
    ]) {
      expect(normalizeApiFailure(value)).toBeNull();
    }
  });

  it('does not throw on hostile or malformed diagnostics', () => {
    expect(() =>
      normalizeApiFailure({
        response: response(500),
        error: { title: 12, reason: { nested: true }, traceId: 99, status: 'not-a-status' },
      }),
    ).not.toThrow();
    expect(normalizeApiFailure({ error: { status: 500, title: 12 } })).toEqual({
      kind: 'http',
      title: 'Request failed',
      status: '500',
      reason: undefined,
      traceId: 'Unavailable',
    });
  });
});

describe('reportApiFailure', () => {
  it('publishes one normalized toast to the supplied queue', () => {
    const queue = createApiFailureToastQueue();
    const publish = vi.spyOn(queue, 'publish');

    reportApiFailure(
      {
        error: problem({ status: 403, title: 'Forbidden', reason: 'denied', traceId: 'trace-403' }),
      },
      queue,
    );

    expect(publish).toHaveBeenCalledTimes(1);
    expect(queue.getSnapshot().toasts).toEqual([
      {
        id: 'api-failure-1',
        title: 'Forbidden',
        status: '403',
        reason: 'denied',
        traceId: 'trace-403',
      },
    ]);
    queue.reset();
  });

  it('publishes nothing for an unclassified throw or an abort', () => {
    const queue = createApiFailureToastQueue();
    const publish = vi.spyOn(queue, 'publish');

    reportApiFailure(new TypeError('Failed to fetch'), queue);
    reportApiFailure(new DOMException('Aborted', 'AbortError'), queue);

    expect(publish).not.toHaveBeenCalled();
    expect(queue.getSnapshot().toasts).toEqual([]);
  });
});
