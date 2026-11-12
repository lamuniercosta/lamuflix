import type { components } from '../../api/types';

export type SubtitleQuota = components['schemas']['SubtitleQuotaDto'];
export type SubtitleCandidate = components['schemas']['SubtitleSearchCandidateDto'];
export type SubtitleDownloadAvailability = components['schemas']['SubtitleDownloadAvailabilityDto'];
export type SubtitleLanguageOption = components['schemas']['SubtitleLanguageOptionDto'];

export type SubtitleProblemReason =
  | 'invalid_request'
  | 'stale_candidate'
  | 'access_denied'
  | 'movie_not_found'
  | 'read_only_library'
  | 'provider_not_configured'
  | 'search_rate_limited'
  | 'download_quota_exhausted'
  | 'provider_unavailable'
  | 'provider_timeout';

export interface SubtitleProblem {
  status: number | null;
  reason: SubtitleProblemReason | null;
  quota: SubtitleQuota | null;
}

const REASONS: readonly SubtitleProblemReason[] = [
  'invalid_request',
  'stale_candidate',
  'access_denied',
  'movie_not_found',
  'read_only_library',
  'provider_not_configured',
  'search_rate_limited',
  'download_quota_exhausted',
  'provider_unavailable',
  'provider_timeout',
];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function toNumberOrString(value: unknown): number | string | null {
  return typeof value === 'number' || typeof value === 'string' ? value : null;
}

function readQuota(value: unknown): SubtitleQuota | null {
  if (!isRecord(value)) return null;
  return {
    remainingDownloads: toNumberOrString(value.remainingDownloads),
    resetAtUtc: typeof value.resetAtUtc === 'string' ? value.resetAtUtc : null,
  };
}

export function readSubtitleProblem(body: unknown): SubtitleProblem {
  if (!isRecord(body)) return { status: null, reason: null, quota: null };
  const reason = REASONS.find((candidate) => candidate === body.reason) ?? null;
  const status = typeof body.status === 'number' ? body.status : null;
  return { status, reason, quota: readQuota(body.quota) };
}

export function numericValue(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

const integerFormat = new Intl.NumberFormat();

export function formatCount(value: number | string | null | undefined): string {
  const parsed = numericValue(value);
  return parsed === null ? '—' : integerFormat.format(parsed);
}

export function formatMatchPercent(value: number | string | null | undefined): string {
  const parsed = numericValue(value);
  return parsed === null ? '—' : `${integerFormat.format(parsed)}%`;
}

const resetTimeFormat = new Intl.DateTimeFormat(undefined, {
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

export function formatResetTime(resetAtUtc: string | null | undefined): string | null {
  if (!resetAtUtc) return null;
  const parsed = new Date(resetAtUtc);
  if (Number.isNaN(parsed.getTime())) return null;
  return resetTimeFormat.format(parsed);
}
