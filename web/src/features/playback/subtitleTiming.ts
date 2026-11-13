export const SUBTITLE_DELAY_STEP_MS = 100;

export const SUBTITLE_DELAY_SHIFT_STEP_MS = 500;

export type SubtitleDelayDirection = 'earlier' | 'later';

export function subtitleDelayStepMs(shift: boolean): number {
  return shift ? SUBTITLE_DELAY_SHIFT_STEP_MS : SUBTITLE_DELAY_STEP_MS;
}

export function shiftSubtitleDelay(
  delayMs: number,
  direction: SubtitleDelayDirection,
  shift: boolean,
): number {
  const step = subtitleDelayStepMs(shift);
  return direction === 'earlier' ? delayMs - step : delayMs + step;
}

export function formatSubtitleDelay(delayMs: number): string {
  if (!Number.isFinite(delayMs)) return '0.0 s';
  const magnitude = Math.abs(delayMs) / 1000;
  const signed = magnitude < 0.05 ? '' : delayMs > 0 ? '+' : '-';
  return `${signed}${magnitude.toFixed(1)} s`;
}
