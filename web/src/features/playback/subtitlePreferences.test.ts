import { describe, expect, it } from 'vitest';
import {
  SUBTITLE_DELAY_KEY_PREFIX,
  readSubtitleDelay,
  removeSubtitleDelay,
  sanitizeSubtitleDelay,
  subtitleDelayKey,
  writeSubtitleDelay,
} from './subtitlePreferences';

const STYLE_KEY = 'lamuflix:subtitle-style:v1';

function memoryStorage(initial: Record<string, string> = {}) {
  const entries = new Map(Object.entries(initial));
  return {
    entries,
    getItem: (key: string) => entries.get(key) ?? null,
    setItem: (key: string, value: string) => {
      entries.set(key, value);
    },
    removeItem: (key: string) => {
      entries.delete(key);
    },
  };
}

function brokenStorage() {
  return {
    getItem: (): string | null => {
      throw new Error('read failed');
    },
    setItem: (): void => {
      throw new Error('write failed');
    },
    removeItem: (): void => {
      throw new Error('remove failed');
    },
  };
}

describe('subtitleDelayKey', () => {
  it('builds the versioned JSON-array pair key', () => {
    expect(SUBTITLE_DELAY_KEY_PREFIX).toBe('lamuflix:subtitle-delay:v1:');
    expect(subtitleDelayKey(7, 'token-en')).toBe(
      `lamuflix:subtitle-delay:v1:${JSON.stringify([7, 'token-en'])}`,
    );
    expect(subtitleDelayKey(7, 'token-en')).toBe('lamuflix:subtitle-delay:v1:[7,"token-en"]');
  });

  it('encodes the whole pair unambiguously for delimiter-bearing track IDs', () => {
    const keys = [
      subtitleDelayKey(1, '2,3'),
      subtitleDelayKey(12, '3'),
      subtitleDelayKey(1, '2'),
      subtitleDelayKey(1, '2:3'),
      subtitleDelayKey(1, '2 3'),
      subtitleDelayKey(1, '2]3'),
      subtitleDelayKey(1, '2"3'),
      subtitleDelayKey(1, '[]'),
      subtitleDelayKey(1, 'null'),
      subtitleDelayKey(1, ''),
    ];
    expect(new Set(keys).size).toBe(keys.length);
    expect(subtitleDelayKey(1, '2,3')).toBe('lamuflix:subtitle-delay:v1:[1,"2,3"]');
    expect(subtitleDelayKey(1, '2"3')).toBe('lamuflix:subtitle-delay:v1:[1,"2\\"3"]');
  });

  it('keeps track IDs exact for Unicode and case', () => {
    expect(subtitleDelayKey(1, 'EN')).not.toBe(subtitleDelayKey(1, 'en'));
    expect(subtitleDelayKey(1, 'é')).not.toBe(subtitleDelayKey(1, 'e\u0301'));
    expect(subtitleDelayKey(1, 'é')).toBe(`lamuflix:subtitle-delay:v1:${JSON.stringify([1, 'é'])}`);
    expect(subtitleDelayKey(1, '\u0000')).toBe(
      `lamuflix:subtitle-delay:v1:${JSON.stringify([1, '\u0000'])}`,
    );
  });

  it('supports empty and very long track IDs', () => {
    const long = 'x'.repeat(4096);
    expect(subtitleDelayKey(1, '')).toBe('lamuflix:subtitle-delay:v1:[1,""]');
    expect(subtitleDelayKey(1, long)).toBe(
      `lamuflix:subtitle-delay:v1:${JSON.stringify([1, long])}`,
    );
    expect(subtitleDelayKey(1, long.length > 0 ? long : '')).not.toBe(subtitleDelayKey(1, 'x'));
  });

  it('normalizes equivalent numeric movie representations to one key', () => {
    const expected = subtitleDelayKey(7, 'token-en');
    expect(subtitleDelayKey('7', 'token-en')).toBe(expected);
    expect(subtitleDelayKey(7.0, 'token-en')).toBe(expected);
    expect(subtitleDelayKey('007', 'token-en')).toBe(expected);
    expect(subtitleDelayKey(' 7 ', 'token-en')).toBe(expected);
  });

  it('rejects movie IDs that are not safe integers', () => {
    expect(subtitleDelayKey(Number.NaN, 'token-en')).toBeNull();
    expect(subtitleDelayKey(Number.POSITIVE_INFINITY, 'token-en')).toBeNull();
    expect(subtitleDelayKey(7.5, 'token-en')).toBeNull();
    expect(subtitleDelayKey('not-a-number', 'token-en')).toBeNull();
    expect(subtitleDelayKey('', 'token-en')).toBeNull();
    expect(subtitleDelayKey(Number.MAX_SAFE_INTEGER + 1, 'token-en')).toBeNull();
    expect(subtitleDelayKey(Number.MAX_SAFE_INTEGER, 'token-en')).toBe(
      `lamuflix:subtitle-delay:v1:${JSON.stringify([Number.MAX_SAFE_INTEGER, 'token-en'])}`,
    );
  });
});

describe('sanitizeSubtitleDelay', () => {
  it('accepts safe-integer milliseconds without clamping', () => {
    expect(sanitizeSubtitleDelay(100)).toBe(100);
    expect(sanitizeSubtitleDelay(-100)).toBe(-100);
    expect(sanitizeSubtitleDelay(0)).toBe(0);
    expect(sanitizeSubtitleDelay(-0)).toBe(0);
    expect(sanitizeSubtitleDelay(9_000_000)).toBe(9_000_000);
    expect(sanitizeSubtitleDelay(-9_000_000)).toBe(-9_000_000);
    expect(sanitizeSubtitleDelay(Number.MAX_SAFE_INTEGER)).toBe(Number.MAX_SAFE_INTEGER);
  });

  it('defaults to zero for anything that is not a safe-integer millisecond value', () => {
    expect(sanitizeSubtitleDelay(1.5)).toBe(0);
    expect(sanitizeSubtitleDelay(Number.NaN)).toBe(0);
    expect(sanitizeSubtitleDelay(Number.POSITIVE_INFINITY)).toBe(0);
    expect(sanitizeSubtitleDelay(Number.MAX_SAFE_INTEGER + 1)).toBe(0);
    expect(sanitizeSubtitleDelay('100')).toBe(0);
    expect(sanitizeSubtitleDelay(null)).toBe(0);
    expect(sanitizeSubtitleDelay(undefined)).toBe(0);
    expect(sanitizeSubtitleDelay({ delay: 100 })).toBe(0);
  });
});

describe('readSubtitleDelay', () => {
  it('returns zero for a never-saved pair', () => {
    expect(readSubtitleDelay(memoryStorage(), 7, 'token-en')).toBe(0);
  });

  it('restores the saved value for its own pair only', () => {
    const storage = memoryStorage();
    expect(writeSubtitleDelay(storage, 7, 'token-en', 500)).toBe(true);
    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(500);
    expect(readSubtitleDelay(storage, 7, 'token-pob')).toBe(0);
    expect(readSubtitleDelay(storage, 8, 'token-en')).toBe(0);
  });

  it('restores the same value when the pair is read again after a reload', () => {
    const storage = memoryStorage();
    writeSubtitleDelay(storage, 42, 'token-en', -1500);
    const reloaded = memoryStorage(Object.fromEntries(storage.entries));
    expect(readSubtitleDelay(reloaded, 42, 'token-en')).toBe(-1500);
    expect(readSubtitleDelay(reloaded, 42, 'token-pob')).toBe(0);
  });

  it('shares one stored value across equivalent numeric movie representations', () => {
    const storage = memoryStorage();
    writeSubtitleDelay(storage, 7, 'token-en', 300);
    expect(readSubtitleDelay(storage, '7', 'token-en')).toBe(300);
    expect(readSubtitleDelay(storage, '007', 'token-en')).toBe(300);
    expect(readSubtitleDelay(storage, 7.0, 'token-en')).toBe(300);
    expect(storage.entries.size).toBe(1);
  });

  it('round-trips empty, Unicode, case-sensitive and long track IDs', () => {
    const storage = memoryStorage();
    const trackIds = ['', 'EN', 'en', 'é', 'e\u0301', 'a:b, c', 'x'.repeat(4096)];
    trackIds.forEach((trackId, index) => {
      writeSubtitleDelay(storage, 3, trackId, (index + 1) * 100);
    });
    trackIds.forEach((trackId, index) => {
      expect(readSubtitleDelay(storage, 3, trackId)).toBe((index + 1) * 100);
    });
    expect(storage.entries.size).toBe(trackIds.length);
  });

  it('defaults to zero for invalid stored delay data', () => {
    const cases = ['abc', '', ' ', '1.5', '1e3', 'NaN', 'Infinity', '0x10', '9007199254740993'];
    for (const raw of cases) {
      const storage = memoryStorage({ [subtitleDelayKey(7, 'token-en') as string]: raw });
      expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(0);
    }
  });

  it('defaults to zero when the movie ID cannot form a key', () => {
    const storage = memoryStorage();
    expect(readSubtitleDelay(storage, Number.NaN, 'token-en')).toBe(0);
    expect(readSubtitleDelay(storage, 7.5, 'token-en')).toBe(0);
  });

  it('returns zero when storage reads fail', () => {
    expect(readSubtitleDelay(brokenStorage(), 7, 'token-en')).toBe(0);
  });
});

describe('writeSubtitleDelay', () => {
  it('persists integer milliseconds under the pair key', () => {
    const storage = memoryStorage();
    expect(writeSubtitleDelay(storage, 7, 'token-en', 100)).toBe(true);
    expect(storage.entries.get(subtitleDelayKey(7, 'token-en') as string)).toBe('100');
  });

  it('rejects invalid delays instead of clamping them', () => {
    const storage = memoryStorage();
    expect(writeSubtitleDelay(storage, 7, 'token-en', 1.5)).toBe(false);
    expect(writeSubtitleDelay(storage, 7, 'token-en', Number.MAX_SAFE_INTEGER + 2)).toBe(false);
    expect(storage.entries.size).toBe(0);
    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(0);
  });

  it('reports failure without throwing when storage writes fail', () => {
    expect(writeSubtitleDelay(brokenStorage(), 7, 'token-en', 100)).toBe(false);
  });

  it('reports failure for an unusable movie ID', () => {
    const storage = memoryStorage();
    expect(writeSubtitleDelay(storage, Number.NaN, 'token-en', 100)).toBe(false);
    expect(storage.entries.size).toBe(0);
  });

  it('keeps each pair isolated when several pairs are saved', () => {
    const storage = memoryStorage();
    writeSubtitleDelay(storage, 7, 'token-en', 100);
    writeSubtitleDelay(storage, 7, 'token-pob', -200);
    writeSubtitleDelay(storage, 8, 'token-en', 300);
    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(100);
    expect(readSubtitleDelay(storage, 7, 'token-pob')).toBe(-200);
    expect(readSubtitleDelay(storage, 8, 'token-en')).toBe(300);
  });
});

describe('removeSubtitleDelay', () => {
  it('removes only the active pair delay key', () => {
    const storage = memoryStorage({ [STYLE_KEY]: '{"font":"serif"}' });
    writeSubtitleDelay(storage, 7, 'token-en', 500);
    writeSubtitleDelay(storage, 7, 'token-pob', 200);
    writeSubtitleDelay(storage, 8, 'token-en', 100);

    expect(removeSubtitleDelay(storage, 7, 'token-en')).toBe(true);

    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(0);
    expect(readSubtitleDelay(storage, 7, 'token-pob')).toBe(200);
    expect(readSubtitleDelay(storage, 8, 'token-en')).toBe(100);
    expect(storage.entries.get(STYLE_KEY)).toBe('{"font":"serif"}');
    expect(storage.entries.size).toBe(3);
  });

  it('succeeds when the pair was never saved', () => {
    const storage = memoryStorage();
    expect(removeSubtitleDelay(storage, 7, 'token-en')).toBe(true);
    expect(storage.entries.size).toBe(0);
  });

  it('reports failure without throwing when storage removal fails', () => {
    expect(removeSubtitleDelay(brokenStorage(), 7, 'token-en')).toBe(false);
  });

  it('reports failure for an unusable movie ID without touching other entries', () => {
    const storage = memoryStorage();
    writeSubtitleDelay(storage, 7, 'token-en', 500);
    expect(removeSubtitleDelay(storage, 7.5, 'token-en')).toBe(false);
    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(500);
  });
});
