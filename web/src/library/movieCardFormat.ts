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
