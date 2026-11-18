import type { components } from '../api/types';

type MovieSummaryResponse = components['schemas']['MovieSummaryResponse'];

export function canRetryEnrichment(status: MovieSummaryResponse['enrichmentStatus']): boolean {
  return status === 'NotFound' || status === 'Failed';
}

export function displayValue(value: null | number | string | undefined): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }
  return String(value);
}

export function formatRating(value: null | number | string | undefined): string | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }
  const number = Number(value);
  return Number.isFinite(number) ? number.toFixed(1) : null;
}

/**
 * The caller-safe failure reason to show beside Failed. Non-Failed states and absent details yield
 * null so the view keeps its existing Failed label without inventing cause wording (DEV-522).
 */
export function failureDetail(
  status: MovieSummaryResponse['enrichmentStatus'],
  failureMessage: null | undefined | string,
): string | null {
  if (status !== 'Failed') {
    return null;
  }
  const text = failureMessage?.trim();
  return text ? text : null;
}
