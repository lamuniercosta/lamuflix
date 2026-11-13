import { describe, expect, it } from 'vitest';
import {
  SUBTITLE_DELAY_SHIFT_STEP_MS,
  SUBTITLE_DELAY_STEP_MS,
  activeSubtitleCuesAt,
  copySubtitleCueBaseline,
  cueTimeToMs,
  formatSubtitleDelay,
  sameSubtitleCueIds,
  shiftSubtitleCueIntervals,
  shiftSubtitleDelay,
  subtitleCueIds,
  subtitleDelayStepMs,
  type SubtitleCueSeed,
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

function seed(id: string, startMs: number, endMs: number, text = id): SubtitleCueSeed {
  return { id, startMs, endMs, text };
}

describe('cueTimeToMs', () => {
  it('converts cue clock seconds to exact integer milliseconds', () => {
    expect(cueTimeToMs(0)).toBe(0);
    expect(cueTimeToMs(1)).toBe(1000);
    expect(cueTimeToMs(1.5)).toBe(1500);
    expect(cueTimeToMs(0.1 + 0.2)).toBe(300);
    expect(cueTimeToMs(-0.25)).toBe(-250);
  });
});

describe('copySubtitleCueBaseline', () => {
  it('copies primitive cue data so later mutation of the originals cannot change the snapshot', () => {
    const source = [seed('a', 1000, 2000, 'first'), seed('b', 3000, 4000, 'second')];
    const baseline = copySubtitleCueBaseline(source);

    source[0].startMs = 9_000;
    source[0].text = 'mutated';
    source.push(seed('c', 5000, 6000));

    expect(baseline).toEqual([seed('a', 1000, 2000, 'first'), seed('b', 3000, 4000, 'second')]);
  });

  it('returns an independent copy that sharing callers cannot alter', () => {
    const source = [seed('a', 1000, 2000)];
    const baseline = copySubtitleCueBaseline(source);
    const other = copySubtitleCueBaseline(baseline);
    expect(other).not.toBe(baseline);
    expect(other).toEqual(baseline);
  });
});

describe('shiftSubtitleCueIntervals', () => {
  it('derives half-open effective intervals from the baseline plus delay', () => {
    const baseline = [seed('a', 1000, 2000)];
    expect(shiftSubtitleCueIntervals(baseline, 500)).toEqual([seed('a', 1500, 2500)]);
    expect(shiftSubtitleCueIntervals(baseline, -500)).toEqual([seed('a', 500, 1500)]);
    expect(shiftSubtitleCueIntervals(baseline, 0)).toEqual([seed('a', 1000, 2000)]);
  });

  it('never accumulates drift across repeated derivation from the same baseline', () => {
    const baseline = [seed('a', 1000, 2000)];
    for (let index = 0; index < 25; index += 1) {
      shiftSubtitleCueIntervals(baseline, 100);
      shiftSubtitleCueIntervals(baseline, -100);
    }
    const shifted = shiftSubtitleCueIntervals(baseline, 100);
    expect(shifted).toEqual([seed('a', 1100, 2100)]);
    expect(baseline).toEqual([seed('a', 1000, 2000)]);
  });

  it('keeps negative effective starts meaningful instead of dropping or clamping them', () => {
    const baseline = [seed('a', 500, 2500)];
    expect(shiftSubtitleCueIntervals(baseline, -2000)).toEqual([seed('a', -1500, 500)]);
  });

  it('preserves order and does not mutate the copied baseline', () => {
    const baseline = copySubtitleCueBaseline([
      seed('a', 0, 1000),
      seed('b', 1000, 2000),
      seed('c', 2000, 3000),
    ]);
    const shifted = shiftSubtitleCueIntervals(baseline, 500);
    expect(shifted.map((cue) => cue.id)).toEqual(['a', 'b', 'c']);
    expect(baseline.map((cue) => cue.startMs)).toEqual([0, 1000, 2000]);
  });
});

describe('activeSubtitleCuesAt', () => {
  const baseline = copySubtitleCueBaseline([
    seed('a', 1000, 2000),
    seed('b', 2000, 3000),
    seed('c', 4000, 5000),
  ]);

  it('treats effective intervals as start-inclusive and end-exclusive', () => {
    const intervals = shiftSubtitleCueIntervals(baseline, 0);
    expect(activeSubtitleCuesAt(intervals, 1000).map((cue) => cue.id)).toEqual(['a']);
    expect(activeSubtitleCuesAt(intervals, 1999).map((cue) => cue.id)).toEqual(['a']);
    expect(activeSubtitleCuesAt(intervals, 2000).map((cue) => cue.id)).toEqual(['b']);
    expect(activeSubtitleCuesAt(intervals, 3999)).toEqual([]);
    expect(activeSubtitleCuesAt(intervals, 5000)).toEqual([]);
  });

  it('omits a cue ending at time zero and keeps a cue starting at time zero active', () => {
    const intervals = shiftSubtitleCueIntervals(
      copySubtitleCueBaseline([seed('ends', -2000, 0), seed('starts', 0, 1000)]),
      0,
    );
    expect(activeSubtitleCuesAt(intervals, 0).map((cue) => cue.id)).toEqual(['starts']);
  });

  it('shows the portion of a negative-start cue overlapping time zero', () => {
    const intervals = shiftSubtitleCueIntervals(
      copySubtitleCueBaseline([seed('a', 600, 1600)]),
      -1000,
    );
    expect(activeSubtitleCuesAt(intervals, 0).map((cue) => cue.id)).toEqual(['a']);
    expect(activeSubtitleCuesAt(intervals, 599).map((cue) => cue.id)).toEqual(['a']);
    expect(activeSubtitleCuesAt(intervals, 600)).toEqual([]);
  });

  it('omits cues already ended and never reports an adjacent cue twice', () => {
    const intervals = shiftSubtitleCueIntervals(
      copySubtitleCueBaseline([seed('a', 0, 1000), seed('b', 1000, 2000)]),
      0,
    );
    expect(activeSubtitleCuesAt(intervals, 500).map((cue) => cue.id)).toEqual(['a']);
    expect(activeSubtitleCuesAt(intervals, 1000).map((cue) => cue.id)).toEqual(['b']);
  });

  it('reports every concurrently active cue in baseline order', () => {
    const intervals = shiftSubtitleCueIntervals(
      copySubtitleCueBaseline([
        seed('a', 1000, 4000),
        seed('b', 1500, 5000),
        seed('c', 2000, 2500),
      ]),
      0,
    );
    expect(activeSubtitleCuesAt(intervals, 2100).map((cue) => cue.id)).toEqual(['a', 'b', 'c']);
  });

  it('returns an empty set without crashing for a large delay', () => {
    const intervals = shiftSubtitleCueIntervals(baseline, 5_000_000);
    expect(activeSubtitleCuesAt(intervals, 0)).toEqual([]);
    expect(activeSubtitleCuesAt(intervals, 5_001_500).map((cue) => cue.id)).toEqual(['a']);
  });

  it('stays exact across long repeated +/-100 ms delay sequences', () => {
    let delay = 0;
    for (let index = 0; index < 40; index += 1) {
      delay = shiftSubtitleDelay(delay, 'earlier', false);
    }
    for (let index = 0; index < 40; index += 1) {
      delay = shiftSubtitleDelay(delay, 'later', false);
    }
    const intervals = shiftSubtitleCueIntervals(baseline, delay);
    expect(delay).toBe(0);
    expect(intervals.map((cue) => cue.startMs)).toEqual([1000, 2000, 4000]);
  });
});

describe('subtitle cue identity comparison', () => {
  it('compares ordered active-cue identity so unchanged membership produces no update', () => {
    const first = [seed('a', 0, 1000), seed('b', 0, 1000)];
    const same = [seed('a', 0, 1000), seed('b', 0, 1000)];
    const reordered = [seed('b', 0, 1000), seed('a', 0, 1000)];
    const removed = [seed('a', 0, 1000)];

    expect(subtitleCueIds(first)).toBe('a|b');
    expect(sameSubtitleCueIds(first, same)).toBe(true);
    expect(sameSubtitleCueIds(first, reordered)).toBe(false);
    expect(sameSubtitleCueIds(first, removed)).toBe(false);
    expect(subtitleCueIds([])).toBe('');
  });
});
