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

export interface SubtitleCueSeed {
  id: string;
  startMs: number;
  endMs: number;
  text: string;
}

export function cueTimeToMs(seconds: number): number {
  return Math.round(seconds * 1000);
}

export function copySubtitleCueBaseline(
  cues: readonly SubtitleCueSeed[],
): readonly SubtitleCueSeed[] {
  return cues.map((cue) => ({
    id: cue.id,
    startMs: cue.startMs,
    endMs: cue.endMs,
    text: cue.text,
  }));
}

export function shiftSubtitleCueIntervals(
  baseline: readonly SubtitleCueSeed[],
  delayMs: number,
): readonly SubtitleCueSeed[] {
  return baseline.map((cue) => ({
    id: cue.id,
    startMs: cue.startMs + delayMs,
    endMs: cue.endMs + delayMs,
    text: cue.text,
  }));
}

export function activeSubtitleCuesAt(
  intervals: readonly SubtitleCueSeed[],
  timeMs: number,
): readonly SubtitleCueSeed[] {
  return intervals.filter((cue) => cue.startMs <= timeMs && timeMs < cue.endMs);
}

export function subtitleCueIds(cues: readonly { id: string }[]): string {
  return cues.map((cue) => cue.id).join('|');
}

export function sameSubtitleCueIds(
  previous: readonly { id: string }[],
  next: readonly { id: string }[],
): boolean {
  if (previous.length !== next.length) return false;
  return previous.every((cue, index) => cue.id === next[index].id);
}
