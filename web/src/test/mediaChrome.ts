class ResizeObserverStub {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

function stubTrackList() {
  const events = new EventTarget();
  return {
    length: 0,
    selectedIndex: -1,
    addEventListener: events.addEventListener.bind(events),
    removeEventListener: events.removeEventListener.bind(events),
    dispatchEvent: events.dispatchEvent.bind(events),
    item: () => null,
    [Symbol.iterator]: function* emptyList() {},
  };
}

const scope = globalThis as unknown as Record<string, unknown>;

if (typeof scope.ResizeObserver !== 'function') {
  scope.ResizeObserver = ResizeObserverStub;
}

if (typeof scope.matchMedia !== 'function') {
  scope.matchMedia = (media: string) => ({
    matches: false,
    media,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  });
}

for (const name of ['textTracks', 'audioTracks', 'videoRenditions'] as const) {
  Object.defineProperty(HTMLMediaElement.prototype, name, {
    configurable: true,
    get(this: HTMLMediaElement) {
      const bag = ((this as unknown as Record<string, unknown>).__trackLists ??= {}) as Record<
        string,
        unknown
      >;
      bag[name] ??= stubTrackList();
      return bag[name];
    },
  });
}

/**
 * jsdom exposes `HTMLTrackElement.track` as `undefined` and never loads sidecar text, so the tests
 * get a minimal TextTrack-shaped object carrying the attribute-derived identity and a writable
 * `mode`, which is exactly what the viewer sets and asserts.
 */
interface StubTextTrack {
  kind: string | null;
  label: string | null;
  language: string | null;
  mode: TextTrackMode;
}

if (typeof HTMLTrackElement === 'function') {
  Object.defineProperty(HTMLTrackElement.prototype, 'track', {
    configurable: true,
    get(this: HTMLTrackElement): StubTextTrack {
      const element = this as HTMLTrackElement & { __stubTrack?: StubTextTrack };
      element.__stubTrack ??= {
        kind: this.getAttribute('kind'),
        label: this.getAttribute('label'),
        language: this.getAttribute('srclang'),
        mode: 'disabled',
      };
      return element.__stubTrack;
    },
  });
}
