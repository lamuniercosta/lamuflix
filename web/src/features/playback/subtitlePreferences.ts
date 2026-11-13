export const SUBTITLE_DELAY_KEY_PREFIX = 'lamuflix:subtitle-delay:v1:';

const SAFE_INTEGER_PATTERN = /^-?\d+$/;

export interface SubtitleStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

function normalizeMovieId(movieId: number | string): number | null {
  if (typeof movieId === 'number') {
    return Number.isSafeInteger(movieId) ? movieId : null;
  }
  if (typeof movieId !== 'string') return null;
  const trimmed = movieId.trim();
  if (!SAFE_INTEGER_PATTERN.test(trimmed)) return null;
  const parsed = Number(trimmed);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

export function subtitleDelayKey(movieId: number | string, trackId: string): string | null {
  const normalized = normalizeMovieId(movieId);
  if (normalized === null || typeof trackId !== 'string') return null;
  return `${SUBTITLE_DELAY_KEY_PREFIX}${JSON.stringify([normalized, trackId])}`;
}

export function sanitizeSubtitleDelay(value: unknown): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value)) return 0;
  return value === 0 ? 0 : value;
}

function parseStoredDelay(raw: string | null): number {
  if (raw === null) return 0;
  const trimmed = raw.trim();
  if (!SAFE_INTEGER_PATTERN.test(trimmed)) return 0;
  const parsed = Number(trimmed);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}

export function readSubtitleDelay(
  storage: SubtitleStorage,
  movieId: number | string,
  trackId: string,
): number {
  const key = subtitleDelayKey(movieId, trackId);
  if (key === null) return 0;
  try {
    return parseStoredDelay(storage.getItem(key));
  } catch {
    return 0;
  }
}

export function writeSubtitleDelay(
  storage: SubtitleStorage,
  movieId: number | string,
  trackId: string,
  delayMs: number,
): boolean {
  const key = subtitleDelayKey(movieId, trackId);
  if (key === null || !Number.isSafeInteger(delayMs)) return false;
  try {
    storage.setItem(key, String(delayMs));
    return true;
  } catch {
    return false;
  }
}

export function removeSubtitleDelay(
  storage: SubtitleStorage,
  movieId: number | string,
  trackId: string,
): boolean {
  const key = subtitleDelayKey(movieId, trackId);
  if (key === null) return false;
  try {
    storage.removeItem(key);
    return true;
  } catch {
    return false;
  }
}
