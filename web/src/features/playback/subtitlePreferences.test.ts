import { describe, expect, it } from 'vitest';
import {
  DEFAULT_SUBTITLE_STYLE,
  SUBTITLE_DELAY_KEY_PREFIX,
  SUBTITLE_EDGE_TOKENS,
  SUBTITLE_FONT_TOKENS,
  SUBTITLE_OPACITY_MAX,
  SUBTITLE_OPACITY_MIN,
  SUBTITLE_OPACITY_STEP,
  SUBTITLE_STYLE_KEY,
  SUBTITLE_TEXT_SIZE_MAX,
  SUBTITLE_TEXT_SIZE_MIN,
  SUBTITLE_TEXT_SIZE_STEP,
  isHexColor,
  readSubtitleDelay,
  readSubtitleStyle,
  removeSubtitleDelay,
  removeSubtitleStyle,
  sanitizeSubtitleDelay,
  sanitizeSubtitleStyle,
  subtitleDelayKey,
  writeSubtitleDelay,
  writeSubtitleStyle,
  type SubtitleStyleProfile,
} from './subtitlePreferences';
import {
  CAPTION_STYLE_LABEL,
  RESET_STYLE_LABEL,
  RESET_TIMING_LABEL,
  SUBTITLE_BACKGROUND_COLOR_LABEL,
  SUBTITLE_BACKGROUND_OPACITY_LABEL,
  SUBTITLE_DELAY_HELP_COPY,
  SUBTITLE_DELAY_LABEL,
  SUBTITLE_EDGE_LABEL,
  SUBTITLE_EDGE_OPTION_LABELS,
  SUBTITLE_FONT_LABEL,
  SUBTITLE_FONT_OPTION_LABELS,
  SUBTITLE_PREVIEW_LABEL,
  SUBTITLE_PREVIEW_SAMPLE_COPY,
  SUBTITLE_TEXT_COLOR_LABEL,
  SUBTITLE_TEXT_OPACITY_LABEL,
  SUBTITLE_TEXT_SIZE_LABEL,
} from './subtitleCopy';

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

function customProfile(overrides: Partial<SubtitleStyleProfile> = {}): SubtitleStyleProfile {
  return {
    font: 'serif',
    textSize: 140,
    textColor: '#FFEEDD',
    textOpacity: 90,
    bgColor: '#112233',
    bgOpacity: 40,
    edge: 'outline',
    ...overrides,
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

describe('subtitle style profile constants', () => {
  it('uses the versioned global style key', () => {
    expect(SUBTITLE_STYLE_KEY).toBe('lamuflix:subtitle-style:v1');
  });

  it('exposes the ratified internal font and edge tokens only', () => {
    expect(SUBTITLE_FONT_TOKENS).toEqual(['system-sans', 'serif', 'monospace']);
    expect(SUBTITLE_EDGE_TOKENS).toEqual(['none', 'drop-shadow', 'raised', 'depressed', 'outline']);
  });

  it('exposes the permitted ranges and steps', () => {
    expect(SUBTITLE_TEXT_SIZE_MIN).toBe(50);
    expect(SUBTITLE_TEXT_SIZE_MAX).toBe(200);
    expect(SUBTITLE_TEXT_SIZE_STEP).toBe(10);
    expect(SUBTITLE_OPACITY_MIN).toBe(0);
    expect(SUBTITLE_OPACITY_MAX).toBe(100);
    expect(SUBTITLE_OPACITY_STEP).toBe(5);
  });

  it('matches the provisional Q3 default profile', () => {
    expect(DEFAULT_SUBTITLE_STYLE).toEqual({
      font: 'system-sans',
      textSize: 100,
      textColor: '#FFFFFF',
      textOpacity: 100,
      bgColor: '#000000',
      bgOpacity: 75,
      edge: 'none',
    });
  });

  it('returns a fresh default profile that cannot corrupt the shared default', () => {
    const first = sanitizeSubtitleStyle(null);
    first.textColor = '#123456';
    expect(DEFAULT_SUBTITLE_STYLE.textColor).toBe('#FFFFFF');
    expect(sanitizeSubtitleStyle(null)).toEqual(DEFAULT_SUBTITLE_STYLE);
  });
});

describe('isHexColor', () => {
  it('accepts six-digit hex colors in either case', () => {
    expect(isHexColor('#FFFFFF')).toBe(true);
    expect(isHexColor('#000000')).toBe(true);
    expect(isHexColor('#a1b2C3')).toBe(true);
  });

  it('rejects anything that is not a six-digit hex color', () => {
    expect(isHexColor('#FFF')).toBe(false);
    expect(isHexColor('FFFFFF')).toBe(false);
    expect(isHexColor('#GGGGGG')).toBe(false);
    expect(isHexColor('red')).toBe(false);
    expect(isHexColor('#FFFFFF; color: red')).toBe(false);
    expect(isHexColor('')).toBe(false);
    expect(isHexColor(null)).toBe(false);
    expect(isHexColor(123456)).toBe(false);
  });
});

describe('sanitizeSubtitleStyle', () => {
  it('accepts a fully valid stored profile unchanged', () => {
    expect(sanitizeSubtitleStyle(customProfile())).toEqual(customProfile());
  });

  it('accepts every ratified token and range boundary', () => {
    for (const font of SUBTITLE_FONT_TOKENS) {
      for (const edge of SUBTITLE_EDGE_TOKENS) {
        const profile = customProfile({
          font,
          edge,
          textSize: SUBTITLE_TEXT_SIZE_MIN,
          textOpacity: SUBTITLE_OPACITY_MIN,
          bgOpacity: SUBTITLE_OPACITY_MAX,
        });
        expect(sanitizeSubtitleStyle(profile)).toEqual(profile);
      }
    }
  });

  it('falls back to the whole default profile for malformed values', () => {
    const invalidProfiles: unknown[] = [
      null,
      undefined,
      'not-an-object',
      [],
      {},
      { ...customProfile(), font: 'comic-sans' },
      { ...customProfile(), edge: 'glow' },
      { ...customProfile(), textSize: 45 },
      { ...customProfile(), textSize: 205 },
      { ...customProfile(), textSize: 145 },
      { ...customProfile(), textSize: '100' },
      { ...customProfile(), textOpacity: -5 },
      { ...customProfile(), textOpacity: 98 },
      { ...customProfile(), bgOpacity: 105 },
      { ...customProfile(), textColor: 'white' },
      { ...customProfile(), bgColor: '#12345' },
      { ...customProfile(), textColor: '#fff; background: red' },
    ];

    for (const value of invalidProfiles) {
      expect(sanitizeSubtitleStyle(value)).toEqual(DEFAULT_SUBTITLE_STYLE);
    }
  });

  it('does not partially merge a profile with one bad field', () => {
    const partial = sanitizeSubtitleStyle({ ...customProfile(), edge: 'glow' });
    expect(partial).toEqual(DEFAULT_SUBTITLE_STYLE);
    expect(partial.font).toBe('system-sans');
    expect(partial.textSize).toBe(100);
  });
});

describe('readSubtitleStyle', () => {
  it('returns the default profile when nothing is stored', () => {
    expect(readSubtitleStyle(memoryStorage())).toEqual(DEFAULT_SUBTITLE_STYLE);
  });

  it('round-trips a stored profile', () => {
    const storage = memoryStorage();
    expect(writeSubtitleStyle(storage, customProfile())).toBe(true);
    expect(readSubtitleStyle(storage)).toEqual(customProfile());
  });

  it('restores the global style across movie and track changes', () => {
    const storage = memoryStorage();
    writeSubtitleStyle(storage, customProfile({ font: 'monospace', bgOpacity: 20 }));
    writeSubtitleDelay(storage, 7, 'token-en', 500);
    expect(readSubtitleStyle(storage).font).toBe('monospace');
    expect(readSubtitleStyle(storage).bgOpacity).toBe(20);
    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(500);
  });

  it('falls back to the whole default profile for invalid stored data', () => {
    const cases = [
      'not json',
      '',
      ' ',
      'null',
      '[]',
      '"serif"',
      '{}',
      JSON.stringify({ ...customProfile(), font: 'comic-sans' }),
      JSON.stringify({ ...customProfile(), textSize: 500 }),
    ];
    for (const raw of cases) {
      expect(readSubtitleStyle(memoryStorage({ [SUBTITLE_STYLE_KEY]: raw }))).toEqual(
        DEFAULT_SUBTITLE_STYLE,
      );
    }
  });

  it('returns the default profile when storage reads fail', () => {
    expect(readSubtitleStyle(brokenStorage())).toEqual(DEFAULT_SUBTITLE_STYLE);
  });
});

describe('writeSubtitleStyle', () => {
  it('persists the validated profile under the global key', () => {
    const storage = memoryStorage();
    expect(writeSubtitleStyle(storage, customProfile())).toBe(true);
    expect(JSON.parse(storage.entries.get(SUBTITLE_STYLE_KEY) as string)).toEqual(customProfile());
  });

  it('rejects an invalid profile instead of writing it', () => {
    const storage = memoryStorage();
    expect(writeSubtitleStyle(storage, customProfile({ font: 'comic-sans' as never }))).toBe(false);
    expect(storage.entries.size).toBe(0);
  });

  it('reports failure without throwing when storage writes fail', () => {
    expect(writeSubtitleStyle(brokenStorage(), customProfile())).toBe(false);
  });
});

describe('removeSubtitleStyle', () => {
  it('removes only the style key and leaves delay values intact', () => {
    const storage = memoryStorage();
    writeSubtitleStyle(storage, customProfile());
    writeSubtitleDelay(storage, 7, 'token-en', 500);
    writeSubtitleDelay(storage, 7, 'token-pob', -200);

    expect(removeSubtitleStyle(storage)).toBe(true);

    expect(readSubtitleStyle(storage)).toEqual(DEFAULT_SUBTITLE_STYLE);
    expect(storage.entries.has(SUBTITLE_STYLE_KEY)).toBe(false);
    expect(readSubtitleDelay(storage, 7, 'token-en')).toBe(500);
    expect(readSubtitleDelay(storage, 7, 'token-pob')).toBe(-200);
  });

  it('succeeds when no style was ever saved', () => {
    const storage = memoryStorage();
    expect(removeSubtitleStyle(storage)).toBe(true);
    expect(storage.entries.size).toBe(0);
  });

  it('reports failure without throwing when storage removal fails', () => {
    expect(removeSubtitleStyle(brokenStorage())).toBe(false);
  });
});

describe('subtitle ticket copy', () => {
  it('keeps the authoritative label and help text', () => {
    expect(SUBTITLE_DELAY_LABEL).toBe('Subtitle delay');
    expect(SUBTITLE_DELAY_HELP_COPY).toBe('Negative shows subtitles earlier, positive later.');
    expect(RESET_TIMING_LABEL).toBe('Reset timing');
    expect(CAPTION_STYLE_LABEL).toBe('Caption style');
    expect(SUBTITLE_PREVIEW_LABEL).toBe('Subtitle preview');
    expect(RESET_STYLE_LABEL).toBe('Reset to default');
  });

  it('keeps the four color and opacity labels separate', () => {
    expect(SUBTITLE_TEXT_COLOR_LABEL).toBe('Text color');
    expect(SUBTITLE_TEXT_OPACITY_LABEL).toBe('Text opacity');
    expect(SUBTITLE_BACKGROUND_COLOR_LABEL).toBe('Background color');
    expect(SUBTITLE_BACKGROUND_OPACITY_LABEL).toBe('Background opacity');
    expect(
      new Set([
        SUBTITLE_TEXT_COLOR_LABEL,
        SUBTITLE_TEXT_OPACITY_LABEL,
        SUBTITLE_BACKGROUND_COLOR_LABEL,
        SUBTITLE_BACKGROUND_OPACITY_LABEL,
      ]).size,
    ).toBe(4);
  });

  it('keeps the exact Text size and Edge style labels', () => {
    expect(SUBTITLE_TEXT_SIZE_LABEL).toBe('Text size (%)');
    expect(SUBTITLE_EDGE_LABEL).toBe('Edge style');
    expect(SUBTITLE_FONT_LABEL).toBe('Font');
  });

  it('settles the preview sample copy', () => {
    expect(SUBTITLE_PREVIEW_SAMPLE_COPY).toBe('The quick brown fox jumps over the lazy dog.');
  });

  it('labels every ratified font and edge token', () => {
    expect(SUBTITLE_FONT_OPTION_LABELS).toEqual({
      'system-sans': 'System sans-serif',
      serif: 'Serif',
      monospace: 'Monospace',
    });
    expect(SUBTITLE_EDGE_OPTION_LABELS).toEqual({
      none: 'None',
      'drop-shadow': 'Drop shadow',
      raised: 'Raised',
      depressed: 'Depressed',
      outline: 'Outline',
    });
  });
});
