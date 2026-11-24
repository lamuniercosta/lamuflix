import { apiFailureToastQueue, type ApiFailureToastQueue } from '../ui/apiFailureToastQueue';
import type { components } from './types';

type ProblemDetails = components['schemas']['ProblemDetails'];

const UNAVAILABLE = 'Unavailable';
const HTTP_TITLE = 'Request failed';
const NETWORK_TITLE = 'Network error';
const TRANSPORT_MARKER = Symbol('apiTransportFailure');
const PROBLEM_KEYS = ['type', 'title', 'detail', 'instance', 'reason', 'traceId'] as const;

export interface NormalizedApiFailure {
  readonly kind: 'http' | 'network';
  readonly title: string;
  readonly status: string;
  readonly reason: string | undefined;
  readonly traceId: string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isAbort(value: unknown): boolean {
  return isRecord(value) && (value['name'] === 'AbortError' || value['reason'] === 'AbortError');
}

function isMarkedTransport(value: unknown): boolean {
  return isRecord(value) && (value as Record<symbol, unknown>)[TRANSPORT_MARKER] === true;
}

function isResponseLike(value: unknown): value is { status: number; ok: boolean } {
  return isRecord(value) && typeof value['status'] === 'number' && typeof value['ok'] === 'boolean';
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return isRecord(value) && 'status' in value && PROBLEM_KEYS.some((key) => key in value);
}

function validFailureStatus(value: unknown): number | null {
  const status =
    typeof value === 'string' && /^\d+$/.test(value.trim()) ? Number(value.trim()) : value;
  if (typeof status !== 'number' || !Number.isInteger(status) || status < 400 || status > 599) {
    return null;
  }
  return status;
}

function optionalText(value: unknown): string | undefined {
  if (typeof value !== 'string') {
    return undefined;
  }
  const text = value.trim();
  return text.length > 0 ? text : undefined;
}

function shortTitle(value: unknown): string {
  const title = optionalText(value)?.split('\n')[0]?.trim();
  return title ? title.slice(0, 80) : HTTP_TITLE;
}

function normalizeHttp(response: unknown, error: unknown): NormalizedApiFailure | null {
  const responseStatus = isResponseLike(response) && !response.ok ? response.status : null;
  const problem = isProblemDetails(error) ? error : null;
  const status = validFailureStatus(responseStatus) ?? validFailureStatus(problem?.status);
  if (status === null) {
    return null;
  }

  return {
    kind: 'http',
    title: shortTitle(problem?.title),
    status: String(status),
    reason: optionalText(problem?.reason),
    traceId: optionalText(problem?.traceId) ?? UNAVAILABLE,
  };
}

export function markApiTransportFailure(error: unknown): unknown {
  if (isAbort(error) || isMarkedTransport(error)) {
    return error;
  }

  const marked = new Error('API transport failure', { cause: error }) as unknown as Record<
    string,
    unknown
  >;
  Object.defineProperty(marked, TRANSPORT_MARKER, { value: true });
  return marked;
}

export function normalizeApiFailure(value: unknown): NormalizedApiFailure | null {
  if (isAbort(value)) {
    return null;
  }
  if (isMarkedTransport(value)) {
    return {
      kind: 'network',
      title: NETWORK_TITLE,
      status: UNAVAILABLE,
      reason: undefined,
      traceId: UNAVAILABLE,
    };
  }
  if (isRecord(value) && ('response' in value || 'error' in value)) {
    return normalizeHttp(value['response'], value['error']);
  }
  if (isProblemDetails(value)) {
    return normalizeHttp(undefined, value);
  }
  if (isResponseLike(value)) {
    return normalizeHttp(value, undefined);
  }
  return null;
}

export function reportApiFailure(
  failure: unknown,
  queue: ApiFailureToastQueue = apiFailureToastQueue,
): boolean {
  const normalized = normalizeApiFailure(failure);
  if (!normalized) {
    return false;
  }

  queue.publish({
    title: normalized.title,
    status: normalized.status,
    reason: normalized.reason,
    traceId: normalized.traceId,
  });
  return true;
}
