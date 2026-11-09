/**
 * Deterministic media element behaviour for player tests: jsdom does not
 * implement play/pause/load, paused or seekable, and the player tests need to
 * observe the exact order of teardown and attach calls.
 */

export interface DeferredPlay {
  resolve: () => void;
  reject: (error: unknown) => void;
}

export interface MediaStubHandle {
  calls: string[];
  deferNextPlay(): DeferredPlay;
  setSeekable(video: HTMLMediaElement, ranges: Array<[number, number]>): void;
  restore(): void;
}

type Mutable = Record<string, unknown>;

interface Deferred {
  promise: Promise<void>;
  resolve: () => void;
  reject: (error: unknown) => void;
}

function deferred(): Deferred {
  let resolveFn: () => void = () => {};
  let rejectFn: (error: unknown) => void = () => {};
  const promise = new Promise<void>((resolve, reject) => {
    resolveFn = resolve;
    rejectFn = reject;
  });
  return { promise, resolve: resolveFn, reject: rejectFn };
}

export function installMediaStubs(): MediaStubHandle {
  const calls: string[] = [];
  const pending: Deferred[] = [];
  const proto = HTMLMediaElement.prototype as unknown as Mutable;
  const elementProto = Element.prototype as unknown as Mutable;

  const originals = {
    play: proto.play,
    pause: proto.pause,
    load: proto.load,
    pausedDescriptor: Object.getOwnPropertyDescriptor(HTMLMediaElement.prototype, 'paused'),
    setAttribute: elementProto.setAttribute,
    removeAttribute: elementProto.removeAttribute,
  };

  Object.defineProperty(HTMLMediaElement.prototype, 'paused', {
    configurable: true,
    get(this: HTMLMediaElement) {
      return (this as unknown as Mutable).__paused !== false;
    },
  });

  const settle = (element: HTMLMediaElement) => {
    (element as unknown as Mutable).__paused = false;
    element.dispatchEvent(new Event('play'));
    element.dispatchEvent(new Event('playing'));
  };

  proto.play = function playElement(this: HTMLMediaElement) {
    calls.push('play');
    const next = pending.shift();
    if (!next) {
      settle(this);
      return Promise.resolve();
    }
    return next.promise.then(
      () => settle(this),
      (error: unknown) => {
        (this as unknown as Mutable).__paused = true;
        throw error;
      },
    );
  };
  proto.pause = function pauseElement(this: HTMLMediaElement) {
    calls.push('pause');
    (this as unknown as Mutable).__paused = true;
    this.dispatchEvent(new Event('pause'));
  };

  proto.load = function loadElement(this: HTMLMediaElement) {
    calls.push('load');
    this.dispatchEvent(new Event('loadstart'));
  };

  elementProto.setAttribute = function setAttribute(name: string, value: string) {
    if (this instanceof HTMLMediaElement && name === 'src') {
      calls.push(`src:${value}`);
    }
    return (originals.setAttribute as (n: string, v: string) => void).call(this, name, value);
  };

  elementProto.removeAttribute = function removeAttribute(name: string) {
    if (this instanceof HTMLMediaElement && name === 'src') {
      calls.push('clear:src');
    }
    return (originals.removeAttribute as (n: string) => void).call(this, name);
  };

  return {
    calls,
    deferNextPlay() {
      const next = deferred();
      pending.push(next);
      return { resolve: next.resolve, reject: next.reject };
    },
    setSeekable(video, ranges) {
      Object.defineProperty(video, 'seekable', {
        configurable: true,
        get: () => ({
          length: ranges.length,
          start: (index: number) => ranges[index][0],
          end: (index: number) => ranges[index][1],
        }),
      });
    },
    restore() {
      proto.play = originals.play;
      proto.pause = originals.pause;
      proto.load = originals.load;
      elementProto.setAttribute = originals.setAttribute;
      elementProto.removeAttribute = originals.removeAttribute;
      if (originals.pausedDescriptor) {
        Object.defineProperty(HTMLMediaElement.prototype, 'paused', originals.pausedDescriptor);
      } else {
        delete (HTMLMediaElement.prototype as unknown as Mutable).paused;
      }
    },
  };
}

export function autoplayDenial(): Error {
  const error = new Error('play() failed because the user did not interact with the document');
  error.name = 'NotAllowedError';
  return error;
}
