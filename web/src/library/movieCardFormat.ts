import type { components } from '../api/types';
import { PlayMovieError } from './libraryQueries';

type MovieSummaryResponse = components['schemas']['MovieSummaryResponse'];

export const PLAYBACK_FORBIDDEN_MESSAGE = 'Playback is not available for this title.';
export const PLAYBACK_FAILED_MESSAGE = 'Playback could not start. Try again.';

export function playErrorMessage(error: unknown): string {
  return error instanceof PlayMovieError && error.status === 403
    ? PLAYBACK_FORBIDDEN_MESSAGE
    : PLAYBACK_FAILED_MESSAGE;
}

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
