import { act, render } from '@testing-library/react';
import { StrictMode, createElement, type ReactElement } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { useSubtitleCues, type UseSubtitleCuesOptions } from './useSubtitleCues';
import {
  createFakeCueList,
  createFakeMediaClock,
  fakeCues,
  installFrameLoop,
  type FakeCue,
  type FakeCueList,
  type FakeFrameLoop,
  type FakeMediaClock,
} from './subtitleTestDoubles';

let frameLoop: FakeFrameLoop;
let media: FakeMediaClock;

beforeEach(() => {
  frameLoop = installFrameLoop();
  media = createFakeMediaClock(0);
});

afterEach(() => {
  frameLoop.restore();
});

function Harness({ options }: { options: UseSubtitleCuesOptions }): ReactElement {
  const cues = useSubtitleCues(options);
  return createElement(
    'ul',
    { 'data-testid': 'cues' },
    cues.map((cue) => createElement('li', { key: cue.id, 'data-testid': 'cue' }, cue.text)),
  );
}

function texts(container: HTMLElement): string[] {
  return Array.from(container.querySelectorAll('[data-testid="cue"]')).map(
    (node) => node.textContent ?? '',
  );
}

function observeUpdates(container: HTMLElement): { batches: () => number; stop: () => void } {
  let batches = 0;
  const observer = new MutationObserver(() => {
    batches += 1;
  });
  observer.observe(container, { childList: true, subtree: true, characterData: true });
  return {
    batches: () => batches,
    stop: () => observer.disconnect(),
  };
}

interface Fixture {
  list: FakeCueList;
  view: ReturnType<typeof render>;
  update: (patch: Partial<UseSubtitleCuesOptions>) => void;
}

function nativeLikeCueList(initial: readonly FakeCue[] | null): FakeCueList {
  const inner = createFakeCueList(null);
  const nativeCues: Record<string, unknown> = {};
  const sync = (next: readonly FakeCue[] | null) => {
    for (const key of Object.keys(nativeCues)) delete nativeCues[key];
    (next ?? []).forEach((cue, index) => {
      nativeCues[String(index)] = cue;
    });
    nativeCues.length = (next ?? []).length;
  };
  sync(initial);
  return {
    get cues() {
      return nativeCues;
    },
    addEventListener: (type: string, listener: EventListener) =>
      inner.addEventListener(type, listener),
    removeEventListener: (type: string, listener: EventListener) =>
      inner.removeEventListener(type, listener),
    setCues: (next: readonly FakeCue[] | null) => sync(next),
    emit: (type: string) => inner.emit(type),
    listenerCount: (type: string) => inner.listenerCount(type),
    totalListenerCount: () => inner.totalListenerCount(),
  } as unknown as FakeCueList;
}

function mount(initial: Partial<UseSubtitleCuesOptions> = {}): Fixture {
  const list = (initial.cueList as FakeCueList | undefined) ?? createFakeCueList(fakeCues());
  const base: UseSubtitleCuesOptions = {
    media,
    cueList: list,
    hasTrack: true,
    delayMs: 0,
    ...initial,
  };
  const view = render(createElement(Harness, { options: base }));
  const update = (patch: Partial<UseSubtitleCuesOptions>) => {
    Object.assign(base, patch);
    view.rerender(createElement(Harness, { options: base }));
  };
  return { list, view, update };
}

describe('useSubtitleCues', () => {
  it('reports the cues active at the current media time', () => {
    const { view } = mount();
    expect(texts(view.container)).toEqual([]);

    media.setTime(1.5);
    act(() => media.emit('timeupdate'));

    expect(texts(view.container)).toEqual(['First line']);
  });

  it('reads cues from a native TextTrackCueList-shaped container without array methods', () => {
    const list = nativeLikeCueList(fakeCues());
    expect((list.cues as unknown as { forEach?: unknown }).forEach).toBeUndefined();
    const { view } = mount({ cueList: list });
    expect(texts(view.container)).toEqual([]);

    media.setTime(1.5);
    act(() => media.emit('timeupdate'));

    expect(texts(view.container)).toEqual(['First line']);

    act(() => {
      list.setCues([{ id: 'late', startTime: 0, endTime: 10, text: 'Late track' }]);
      list.emit('cuechange');
    });
    expect(texts(view.container)).toEqual(['Late track']);
  });

  it('renders nothing for a native empty-but-loaded cue list instead of throwing', () => {
    const list = nativeLikeCueList([]);
    const { view } = mount({ cueList: list });

    media.setTime(1.5);
    act(() => media.emit('timeupdate'));

    expect(texts(view.container)).toEqual([]);
  });

  it('renders nothing without a usable selected track', () => {
    media.setTime(1.5);
    const { view } = mount({ hasTrack: false });
    expect(texts(view.container)).toEqual([]);
  });

  it('resyncs immediately when the delay changes', () => {
    media.setTime(1.5);
    const { view, update } = mount();
    expect(texts(view.container)).toEqual(['First line']);

    update({ delayMs: -1000 });
    expect(texts(view.container)).toEqual(['Second line']);

    update({ delayMs: -2500 });
    expect(texts(view.container)).toEqual(['Third line']);
  });

  it('resyncs on seek, pause, play and playback-rate changes', () => {
    const { view } = mount();

    media.setTime(2.5);
    act(() => media.emit('seeked'));
    expect(texts(view.container)).toEqual(['Second line']);

    media.setTime(4.5);
    act(() => media.emit('seeking'));
    expect(texts(view.container)).toEqual(['Third line']);

    media.setTime(1.5);
    act(() => media.play());
    expect(texts(view.container)).toEqual(['First line']);

    act(() => media.setRate(2));
    expect(texts(view.container)).toEqual(['First line']);
  });

  it('picks up cues that become ready after the overlay mounted', () => {
    media.setTime(1.5);
    const list = createFakeCueList(null);
    const { view } = mount({ cueList: list });
    expect(texts(view.container)).toEqual([]);

    act(() => {
      list.setCues(fakeCues());
      list.emit('cuechange');
    });

    expect(texts(view.container)).toEqual(['First line']);
  });

  it('runs a frame loop while playing and none while paused', () => {
    mount();
    expect(frameLoop.pending()).toBe(0);

    act(() => media.play());
    expect(frameLoop.pending()).toBeGreaterThan(0);

    media.setTime(1.5);
    act(() => frameLoop.tick(16));
    expect(frameLoop.pending()).toBeGreaterThan(0);

    act(() => media.pause());
    expect(frameLoop.pending()).toBe(0);
    act(() => frameLoop.tick(32));
    expect(frameLoop.pending()).toBe(0);
  });

  it('does not produce observable updates for unchanged active cues', async () => {
    media.setTime(1.5);
    const { view } = mount();
    act(() => media.emit('timeupdate'));
    expect(texts(view.container)).toEqual(['First line']);
    const observed = observeUpdates(view.container);

    act(() => media.play());
    for (let index = 1; index <= 20; index += 1) {
      act(() => frameLoop.tick(index * 16));
    }

    await act(async () => {
      await Promise.resolve();
    });

    expect(texts(view.container)).toEqual(['First line']);
    expect(observed.batches()).toBe(0);
    observed.stop();
  });

  it('updates when the ordered active-cue identity changes', async () => {
    media.setTime(1.5);
    const { view } = mount();
    act(() => media.emit('timeupdate'));
    const observed = observeUpdates(view.container);

    media.setTime(2.5);
    act(() => media.emit('timeupdate'));

    await act(async () => {
      await Promise.resolve();
    });

    expect(texts(view.container)).toEqual(['Second line']);
    expect(observed.batches()).toBeGreaterThan(0);
    observed.stop();
  });

  it('ignores stale cue-readiness events from a replaced track', () => {
    media.setTime(1.5);
    const first = createFakeCueList(fakeCues());
    const second = createFakeCueList(null);
    const { view, update } = mount({ cueList: first });
    expect(texts(view.container)).toEqual(['First line']);

    update({ cueList: second });
    expect(texts(view.container)).toEqual([]);

    act(() => {
      first.setCues(fakeCues());
      first.emit('cuechange');
    });
    expect(texts(view.container)).toEqual([]);

    act(() => {
      second.setCues([{ id: 'late', startTime: 0, endTime: 10, text: 'Late track' }]);
      second.emit('cuechange');
    });
    expect(texts(view.container)).toEqual(['Late track']);
  });

  it('survives rapid cue-list switches and keeps only the latest cues', () => {
    media.setTime(1.5);
    const a = createFakeCueList([{ id: 'a', startTime: 0, endTime: 5, text: 'A' }]);
    const b = createFakeCueList([{ id: 'b', startTime: 0, endTime: 5, text: 'B' }]);
    const { view, update } = mount({ cueList: a });

    update({ cueList: b });
    update({ cueList: a });
    update({ cueList: b });

    expect(texts(view.container)).toEqual(['B']);
  });

  it('releases every acquired listener and frame on unmount', () => {
    media.setTime(1.5);
    const list = createFakeCueList(fakeCues());
    const { view } = mount({ cueList: list });
    act(() => media.play());
    expect(media.totalListenerCount()).toBeGreaterThan(0);
    expect(list.totalListenerCount()).toBeGreaterThan(0);

    view.unmount();

    expect(media.totalListenerCount()).toBe(0);
    expect(list.totalListenerCount()).toBe(0);
    expect(frameLoop.pending()).toBe(0);
  });

  it('does not duplicate subscriptions when remounted under StrictMode', () => {
    media.setTime(1.5);
    const list = createFakeCueList(fakeCues());
    const options: UseSubtitleCuesOptions = {
      media,
      cueList: list,
      hasTrack: true,
      delayMs: 0,
    };
    const view = render(createElement(StrictMode, null, createElement(Harness, { options })));

    expect(texts(view.container)).toEqual(['First line']);
    expect(list.listenerCount('cuechange')).toBe(1);

    view.unmount();
    expect(list.totalListenerCount()).toBe(0);
    expect(media.totalListenerCount()).toBe(0);
  });
});
