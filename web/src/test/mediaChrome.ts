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
