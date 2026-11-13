export const SUBTITLE_DELAY_KEY_PREFIX = 'lamuflix:subtitle-delay:v1:';

export const SUBTITLE_STYLE_KEY = 'lamuflix:subtitle-style:v1';

const SAFE_INTEGER_PATTERN = /^-?\d+$/;

const HEX_COLOR_PATTERN = /^#[0-9a-fA-F]{6}$/;

export const SUBTITLE_FONT_TOKENS = ['system-sans', 'serif', 'monospace'] as const;

export const SUBTITLE_EDGE_TOKENS = [
  'none',
  'drop-shadow',
  'raised',
  'depressed',
  'outline',
] as const;

export type SubtitleFontToken = (typeof SUBTITLE_FONT_TOKENS)[number];

export type SubtitleEdgeToken = (typeof SUBTITLE_EDGE_TOKENS)[number];

export const SUBTITLE_TEXT_SIZE_MIN = 50;
export const SUBTITLE_TEXT_SIZE_MAX = 200;
export const SUBTITLE_TEXT_SIZE_STEP = 10;

export const SUBTITLE_OPACITY_MIN = 0;
export const SUBTITLE_OPACITY_MAX = 100;
export const SUBTITLE_OPACITY_STEP = 5;

export interface SubtitleStyleProfile {
  font: SubtitleFontToken;
  textSize: number;
  textColor: string;
  textOpacity: number;
  bgColor: string;
  bgOpacity: number;
  edge: SubtitleEdgeToken;
}

export const DEFAULT_SUBTITLE_STYLE: Readonly<SubtitleStyleProfile> = {
  font: 'system-sans',
  textSize: 100,
  textColor: '#FFFFFF',
  textOpacity: 100,
  bgColor: '#000000',
  bgOpacity: 75,
  edge: 'none',
};

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

export function isHexColor(value: unknown): value is string {
  return typeof value === 'string' && HEX_COLOR_PATTERN.test(value);
}

function isSteppedValue(value: unknown, min: number, max: number, step: number): value is number {
  if (typeof value !== 'number' || !Number.isInteger(value)) return false;
  if (value < min || value > max) return false;
  return (value - min) % step === 0;
}

function isFontToken(value: unknown): value is SubtitleFontToken {
  return SUBTITLE_FONT_TOKENS.includes(value as SubtitleFontToken);
}

function isEdgeToken(value: unknown): value is SubtitleEdgeToken {
  return SUBTITLE_EDGE_TOKENS.includes(value as SubtitleEdgeToken);
}

function asSteppedNumber(value: unknown, min: number, max: number, step: number): number | null {
  return isSteppedValue(value, min, max, step) ? value : null;
}

function validateSubtitleStyle(value: unknown): SubtitleStyleProfile | null {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) return null;
  const candidate = value as Record<string, unknown>;
  if (!isFontToken(candidate.font)) return null;
  if (!isEdgeToken(candidate.edge)) return null;
  const textSize = asSteppedNumber(
    candidate.textSize,
    SUBTITLE_TEXT_SIZE_MIN,
    SUBTITLE_TEXT_SIZE_MAX,
    SUBTITLE_TEXT_SIZE_STEP,
  );
  const textOpacity = asSteppedNumber(
    candidate.textOpacity,
    SUBTITLE_OPACITY_MIN,
    SUBTITLE_OPACITY_MAX,
    SUBTITLE_OPACITY_STEP,
  );
  const bgOpacity = asSteppedNumber(
    candidate.bgOpacity,
    SUBTITLE_OPACITY_MIN,
    SUBTITLE_OPACITY_MAX,
    SUBTITLE_OPACITY_STEP,
  );
  if (textSize === null || textOpacity === null || bgOpacity === null) return null;
  if (!isHexColor(candidate.textColor)) return null;
  if (!isHexColor(candidate.bgColor)) return null;
  return {
    font: candidate.font,
    textSize,
    textColor: candidate.textColor,
    textOpacity,
    bgColor: candidate.bgColor,
    bgOpacity,
    edge: candidate.edge,
  };
}

function defaultSubtitleStyle(): SubtitleStyleProfile {
  return { ...DEFAULT_SUBTITLE_STYLE };
}

export function sanitizeSubtitleStyle(value: unknown): SubtitleStyleProfile {
  return validateSubtitleStyle(value) ?? defaultSubtitleStyle();
}

function parseStoredStyle(raw: string | null): SubtitleStyleProfile {
  if (raw === null) return defaultSubtitleStyle();
  try {
    return sanitizeSubtitleStyle(JSON.parse(raw));
  } catch {
    return defaultSubtitleStyle();
  }
}

export function readSubtitleStyle(storage: SubtitleStorage): SubtitleStyleProfile {
  try {
    return parseStoredStyle(storage.getItem(SUBTITLE_STYLE_KEY));
  } catch {
    return defaultSubtitleStyle();
  }
}

export function writeSubtitleStyle(storage: SubtitleStorage, style: unknown): boolean {
  const validated = validateSubtitleStyle(style);
  if (validated === null) return false;
  try {
    storage.setItem(SUBTITLE_STYLE_KEY, JSON.stringify(validated));
    return true;
  } catch {
    return false;
  }
}

export function removeSubtitleStyle(storage: SubtitleStorage): boolean {
  try {
    storage.removeItem(SUBTITLE_STYLE_KEY);
    return true;
  } catch {
    return false;
  }
}
