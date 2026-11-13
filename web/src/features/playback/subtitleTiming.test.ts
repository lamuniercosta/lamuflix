import { describe, expect, it } from 'vitest';
import {
  SUBTITLE_DELAY_SHIFT_STEP_MS,
  SUBTITLE_DELAY_STEP_MS,
  formatSubtitleDelay,
  shiftSubtitleDelay,
  subtitleDelayStepMs,
} from './subtitleTiming';

describe('subtitleDelayStepMs', () => {
  it('uses a 100 ms ordinary step and a 500 ms Shift step', () => {
    expect(SUBTITLE_DELAY_STEP_MS).toBe(100);
    expect(SUBTITLE_DELAY_SHIFT_STEP_MS).toBe(500);
    expect(subtitleDelayStepMs(false)).toBe(100);
    expect(subtitleDelayStepMs(true)).toBe(500);
  });
});

describe('shiftSubtitleDelay', () => {
  it('moves earlier as a negative offset and later as a positive offset', () => {
    expect(shiftSubtitleDelay(0, 'later', false)).toBe(100);
    expect(shiftSubtitleDelay(0, 'earlier', false)).toBe(-100);
    expect(shiftSubtitleDelay(0, 'later', true)).toBe(500);
    expect(shiftSubtitleDelay(0, 'earlier', true)).toBe(-500);
  });

  it('keeps integer milliseconds exact across repeated ordinary and Shift steps', () => {
    let delay = 0;
    for (let index = 0; index < 10; index += 1) {
      delay = shiftSubtitleDelay(delay, 'later', false);
    }
    expect(delay).toBe(1000);
    for (let index = 0; index < 2; index += 1) {
      delay = shiftSubtitleDelay(delay, 'later', true);
    }
    expect(delay).toBe(2000);
    for (let index = 0; index < 10; index += 1) {
      delay = shiftSubtitleDelay(delay, 'earlier', false);
    }
    expect(delay).toBe(1000);
    for (let index = 0; index < 2; index += 1) {
      delay = shiftSubtitleDelay(delay, 'earlier', true);
    }
    expect(delay).toBe(0);
    expect(Number.isInteger(delay)).toBe(true);
  });

  it('returns to the exact starting value after a mirrored ordinary step', () => {
    for (const start of [0, 100, -500, 12_300, -987_654]) {
      const later = shiftSubtitleDelay(start, 'later', false);
      const earlier = shiftSubtitleDelay(start, 'earlier', false);
      expect(shiftSubtitleDelay(later, 'earlier', false)).toBe(start);
      expect(shiftSubtitleDelay(earlier, 'later', false)).toBe(start);
    }
  });

  it('never applies an arbitrary clamp in either direction', () => {
    expect(shiftSubtitleDelay(5_000_000, 'later', true)).toBe(5_000_500);
    expect(shiftSubtitleDelay(-5_000_000, 'earlier', true)).toBe(-5_000_500);
    expect(shiftSubtitleDelay(0, 'earlier', false)).toBe(-100);
    expect(shiftSubtitleDelay(-100, 'earlier', false)).toBe(-200);
  });
});

describe('formatSubtitleDelay', () => {
  it('shows zero as 0.0 s with no sign', () => {
    expect(formatSubtitleDelay(0)).toBe('0.0 s');
    expect(formatSubtitleDelay(-0)).toBe('0.0 s');
  });

  it('shows signed seconds with one decimal place', () => {
    expect(formatSubtitleDelay(100)).toBe('+0.1 s');
    expect(formatSubtitleDelay(500)).toBe('+0.5 s');
    expect(formatSubtitleDelay(1000)).toBe('+1.0 s');
    expect(formatSubtitleDelay(12_300)).toBe('+12.3 s');
    expect(formatSubtitleDelay(-100)).toBe('-0.1 s');
    expect(formatSubtitleDelay(-500)).toBe('-0.5 s');
    expect(formatSubtitleDelay(-1000)).toBe('-1.0 s');
    expect(formatSubtitleDelay(-12_300)).toBe('-12.3 s');
  });

  it('keeps the displayed value in step with repeated adjustments', () => {
    const displayed: string[] = [];
    let delay = 0;
    for (let index = 0; index < 5; index += 1) {
      delay = shiftSubtitleDelay(delay, 'earlier', false);
      displayed.push(formatSubtitleDelay(delay));
    }
    for (let index = 0; index < 3; index += 1) {
      delay = shiftSubtitleDelay(delay, 'later', true);
      displayed.push(formatSubtitleDelay(delay));
    }
    expect(displayed).toEqual([
      '-0.1 s',
      '-0.2 s',
      '-0.3 s',
      '-0.4 s',
      '-0.5 s',
      '0.0 s',
      '+0.5 s',
      '+1.0 s',
    ]);
  });

  it('does not depend on prior formatting of the same value', () => {
    expect(formatSubtitleDelay(1500)).toBe(formatSubtitleDelay(1500));
    expect(formatSubtitleDelay(1500)).toBe('+1.5 s');
  });
});
