import { act, renderHook } from '@testing-library/react';
import { StrictMode } from 'react';
import { describe, expect, it } from 'vitest';
import { useSubtitleOptions, type UseSubtitleOptionsOptions } from './useSubtitleOptions';
import {
  DEFAULT_SUBTITLE_STYLE,
  SUBTITLE_STYLE_KEY,
  subtitleDelayKey,
  type SubtitleStorage,
} from './subtitlePreferences';

interface Memory extends SubtitleStorage {
  data: Map<string, string>;
  reads: number;
  writes: number;
  removes: number;
}

function memory(initial: Record<string, string> = {}): Memory {
  const data = new Map(Object.entries(initial));
  const store: Memory = {
    data,
    reads: 0,
    writes: 0,
    removes: 0,
    getItem(key) {
      store.reads += 1;
      return data.has(key) ? (data.get(key) as string) : null;
    },
    setItem(key, value) {
      store.writes += 1;
      data.set(key, value);
    },
    removeItem(key) {
      store.removes += 1;
      data.delete(key);
    },
  };
  return store;
}

function pairKey(movieId: number, trackId: string): string {
  const encoded = subtitleDelayKey(movieId, trackId);
  if (encoded === null) throw new Error('expected an encodable pair');
  return encoded;
}

const STORED_STYLE = JSON.stringify({ ...DEFAULT_SUBTITLE_STYLE, textSize: 150 });

function hookOptions(
  storage: () => SubtitleStorage | null,
  movieId: number | string = 1,
  trackId: string | null = 'token-en',
): UseSubtitleOptionsOptions {
  return { movieId, trackId, storage };
}

describe('useSubtitleOptions delay lifecycle', () => {
  it('starts a never-saved pair at zero with the default style profile', () => {
    const store = memory();
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    expect(result.current.delayMs).toBe(0);
    expect(result.current.style).toEqual(DEFAULT_SUBTITLE_STYLE);
    expect(result.current.delayPersisted).toBe(true);
    expect(result.current.stylePersisted).toBe(true);
  });

  it('restores the saved delay for the active pair and leaves other pairs at zero', () => {
    const store = memory({ [pairKey(1, 'token-en')]: '500' });
    const { result, rerender } = renderHook(
      (props: UseSubtitleOptionsOptions) => useSubtitleOptions(props),
      { initialProps: hookOptions(() => store) },
    );

    expect(result.current.delayMs).toBe(500);

    rerender(hookOptions(() => store, 1, 'token-pob'));
    expect(result.current.delayMs).toBe(0);

    rerender(hookOptions(() => store, 2, 'token-en'));
    expect(result.current.delayMs).toBe(0);

    rerender(hookOptions(() => store, 1, 'token-en'));
    expect(result.current.delayMs).toBe(500);
  });

  it('writes every adjustment immediately under its own pair key', () => {
    const store = memory();
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.adjustDelay('later', false));
    expect(result.current.delayMs).toBe(100);
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('100');

    act(() => result.current.adjustDelay('later', true));
    expect(result.current.delayMs).toBe(600);

    act(() => result.current.adjustDelay('earlier', false));
    expect(result.current.delayMs).toBe(500);
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('500');
    expect(result.current.delayPersisted).toBe(true);
    expect(store.writes).toBe(3);
  });

  it('never writes the outgoing pair value under the incoming identity', () => {
    const store = memory();
    const { result, rerender } = renderHook(
      (props: UseSubtitleOptionsOptions) => useSubtitleOptions(props),
      { initialProps: hookOptions(() => store) },
    );

    act(() => result.current.adjustDelay('later', false));
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('100');

    rerender(hookOptions(() => store, 1, 'token-pob'));
    expect(result.current.delayMs).toBe(0);
    expect(store.data.has(pairKey(1, 'token-pob'))).toBe(false);

    act(() => result.current.adjustDelay('later', false));
    expect(store.data.get(pairKey(1, 'token-pob'))).toBe('100');
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('100');
  });

  it('keeps the latest pair when movie and track switch rapidly', () => {
    const store = memory({
      [pairKey(1, 'token-en')]: '300',
      [pairKey(2, 'token-en')]: '-200',
    });
    const { result, rerender } = renderHook(
      (props: UseSubtitleOptionsOptions) => useSubtitleOptions(props),
      { initialProps: hookOptions(() => store, 1, 'token-en') },
    );

    rerender(hookOptions(() => store, 2, 'token-en'));
    rerender(hookOptions(() => store, 1, 'token-pob'));
    rerender(hookOptions(() => store, 2, 'token-en'));

    expect(result.current.delayMs).toBe(-200);
    expect(store.data.get(pairKey(2, 'token-en'))).toBe('-200');
  });

  it('resets only its own delay key and updates memory', () => {
    const store = memory({
      [pairKey(1, 'token-en')]: '400',
      [pairKey(1, 'token-pob')]: '-900',
      [SUBTITLE_STYLE_KEY]: STORED_STYLE,
    });
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.resetDelay());

    expect(result.current.delayMs).toBe(0);
    expect(result.current.delayPersisted).toBe(true);
    expect(store.data.has(pairKey(1, 'token-en'))).toBe(false);
    expect(store.data.get(pairKey(1, 'token-pob'))).toBe('-900');
    expect(store.data.get(SUBTITLE_STYLE_KEY)).toBe(STORED_STYLE);
    expect(result.current.style.textSize).toBe(150);
  });

  it('adjusts nothing and writes nothing without a selected track', () => {
    const store = memory();
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store, 1, null)));

    act(() => result.current.adjustDelay('later', false));
    act(() => result.current.resetDelay());

    expect(result.current.delayMs).toBe(0);
    expect(store.writes).toBe(0);
    expect(store.removes).toBe(0);
    expect(store.data.size).toBe(0);
  });
});

describe('useSubtitleOptions style lifecycle', () => {
  it('restores a saved global style and applies it to every pair', () => {
    const store = memory({ [SUBTITLE_STYLE_KEY]: STORED_STYLE });
    const { result, rerender } = renderHook(
      (props: UseSubtitleOptionsOptions) => useSubtitleOptions(props),
      { initialProps: hookOptions(() => store) },
    );

    expect(result.current.style.textSize).toBe(150);

    rerender(hookOptions(() => store, 7, 'token-other'));
    expect(result.current.style.textSize).toBe(150);
  });

  it('persists each style change globally and keeps delay untouched', () => {
    const store = memory({ [pairKey(1, 'token-en')]: '250' });
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.updateStyle({ bgOpacity: 50 }));
    expect(result.current.style.bgOpacity).toBe(50);
    expect(JSON.parse(store.data.get(SUBTITLE_STYLE_KEY) ?? '{}').bgOpacity).toBe(50);
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('250');

    act(() => result.current.updateStyle({ font: 'serif', textColor: '#3366FF' }));
    expect(result.current.style.font).toBe('serif');
    expect(result.current.style.textColor).toBe('#3366FF');
    expect(result.current.style.bgOpacity).toBe(50);
    expect(result.current.stylePersisted).toBe(true);
  });

  it('resets only the global style key without clearing the delay', () => {
    const store = memory({
      [SUBTITLE_STYLE_KEY]: STORED_STYLE,
      [pairKey(1, 'token-en')]: '700',
    });
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.resetStyle());

    expect(result.current.style).toEqual(DEFAULT_SUBTITLE_STYLE);
    expect(result.current.stylePersisted).toBe(true);
    expect(store.data.has(SUBTITLE_STYLE_KEY)).toBe(false);
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('700');
    expect(result.current.delayMs).toBe(700);
    expect(store.removes).toBe(1);
  });

  it('falls back to a full default profile for an invalid stored style', () => {
    const store = memory({ [SUBTITLE_STYLE_KEY]: '{"font":"comic-sans","textSize":5000}' });
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    expect(result.current.style).toEqual(DEFAULT_SUBTITLE_STYLE);
  });
});

describe('useSubtitleOptions storage failures', () => {
  it('initializes defaults when the storage accessor getter throws', () => {
    const store = memory({ [pairKey(1, 'token-en')]: '900' });
    const throwing = () => {
      throw new Error('SecurityError');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(throwing)));

    expect(result.current.delayMs).toBe(0);
    expect(result.current.style).toEqual(DEFAULT_SUBTITLE_STYLE);
    expect(result.current.delayPersisted).toBe(false);
    expect(result.current.stylePersisted).toBe(false);
    expect(store.reads).toBe(0);
  });

  it('keeps in-memory adjustments while a throwing accessor getter blocks persistence', () => {
    const throwing = () => {
      throw new Error('SecurityError');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(throwing)));

    act(() => result.current.adjustDelay('later', true));
    act(() => result.current.updateStyle({ edge: 'outline' }));

    expect(result.current.delayMs).toBe(500);
    expect(result.current.delayPersisted).toBe(false);
    expect(result.current.style.edge).toBe('outline');
    expect(result.current.stylePersisted).toBe(false);
  });

  it('initializes defaults when reading throws', () => {
    const store = memory();
    store.getItem = () => {
      throw new Error('denied');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    expect(result.current.delayMs).toBe(0);
    expect(result.current.style).toEqual(DEFAULT_SUBTITLE_STYLE);
  });

  it('retains the in-memory delay when a write fails and makes no persistence claim', () => {
    const store = memory();
    store.setItem = () => {
      throw new Error('QuotaExceededError');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.adjustDelay('earlier', false));

    expect(result.current.delayMs).toBe(-100);
    expect(result.current.delayPersisted).toBe(false);
  });

  it('retains the in-memory style when a style write fails', () => {
    const store = memory();
    store.setItem = () => {
      throw new Error('QuotaExceededError');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.updateStyle({ textSize: 200 }));

    expect(result.current.style.textSize).toBe(200);
    expect(result.current.stylePersisted).toBe(false);
  });

  it('does not promise the reset survives reload when removal fails', () => {
    const store = memory({ [pairKey(1, 'token-en')]: '400' });
    store.removeItem = () => {
      throw new Error('denied');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.resetDelay());

    expect(result.current.delayMs).toBe(0);
    expect(result.current.delayPersisted).toBe(false);
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('400');
  });

  it('does not promise the style reset survives reload when removal fails', () => {
    const store = memory({ [SUBTITLE_STYLE_KEY]: STORED_STYLE });
    store.removeItem = () => {
      throw new Error('denied');
    };
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)));

    act(() => result.current.resetStyle());

    expect(result.current.style).toEqual(DEFAULT_SUBTITLE_STYLE);
    expect(result.current.stylePersisted).toBe(false);
    expect(store.data.get(SUBTITLE_STYLE_KEY)).toBe(STORED_STYLE);
  });

  it('keeps playback-independent state under a StrictMode mount', () => {
    const store = memory({ [pairKey(1, 'token-en')]: '100' });
    const { result } = renderHook(() => useSubtitleOptions(hookOptions(() => store)), {
      wrapper: StrictMode,
    });

    expect(result.current.delayMs).toBe(100);

    act(() => result.current.adjustDelay('later', false));
    expect(result.current.delayMs).toBe(200);
    expect(store.data.get(pairKey(1, 'token-en'))).toBe('200');
    expect(store.writes).toBe(1);
  });
});
