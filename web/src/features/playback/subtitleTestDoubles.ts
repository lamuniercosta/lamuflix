import type { SubtitleCueList, SubtitleCueClock } from './useSubtitleCues';

export interface FakeCue {
  id: string;
  startTime: number;
  endTime: number;
  text: string;
}

type Listener = EventListener;

interface ListenerBag {
  listeners: Map<string, Set<Listener>>;
  add(type: string, listener: Listener): void;
  remove(type: string, listener: Listener): void;
  emit(type: string): void;
  count(type: string): number;
}

function listenerBag(): ListenerBag {
  const listeners = new Map<string, Set<Listener>>();
  return {
    listeners,
    add(type, listener) {
      const bucket = listeners.get(type) ?? new Set<Listener>();
      bucket.add(listener);
      listeners.set(type, bucket);
    },
    remove(type, listener) {
      listeners.get(type)?.delete(listener);
    },
    emit(type) {
      const event = { type } as Event;
      for (const listener of Array.from(listeners.get(type) ?? [])) listener(event);
    },
    count(type) {
      return listeners.get(type)?.size ?? 0;
    },
  };
}

function totalListenerCount(bag: ListenerBag): number {
  let total = 0;
  for (const bucket of bag.listeners.values()) total += bucket.size;
  return total;
}

export interface FakeCueList extends SubtitleCueList {
  setCues(cues: readonly FakeCue[] | null): void;
  emit(type: string): void;
  listenerCount(type: string): number;
  totalListenerCount(): number;
}

export function createFakeCueList(cues: readonly FakeCue[] | null = null): FakeCueList {
  const bag = listenerBag();
  const list = {
    cues: cues as readonly FakeCue[] | null,
    addEventListener: (type: string, listener: Listener) => bag.add(type, listener),
    removeEventListener: (type: string, listener: Listener) => bag.remove(type, listener),
    setCues(next: readonly FakeCue[] | null) {
      list.cues = next;
    },
    emit: (type: string) => bag.emit(type),
    listenerCount: (type: string) => bag.count(type),
    totalListenerCount: () => totalListenerCount(bag),
  };
  return list;
}

export interface FakeMediaClock extends SubtitleCueClock {
  setTime(seconds: number): void;
  play(): void;
  pause(): void;
  setRate(rate: number): void;
  emit(type: string): void;
  listenerCount(type: string): number;
  totalListenerCount(): number;
}

export function createFakeMediaClock(currentTime = 0): FakeMediaClock {
  const bag = listenerBag();
  const clock = {
    currentTime,
    paused: true,
    playbackRate: 1,
    addEventListener: (type: string, listener: Listener) => bag.add(type, listener),
    removeEventListener: (type: string, listener: Listener) => bag.remove(type, listener),
    setTime(seconds: number) {
      clock.currentTime = seconds;
    },
    play() {
      clock.paused = false;
      bag.emit('play');
    },
    pause() {
      clock.paused = true;
      bag.emit('pause');
    },
    setRate(rate: number) {
      clock.playbackRate = rate;
      bag.emit('ratechange');
    },
    emit: (type: string) => bag.emit(type),
    listenerCount: (type: string) => bag.count(type),
    totalListenerCount: () => totalListenerCount(bag),
  };
  return clock;
}

export interface FakeFrameLoop {
  pending(): number;
  cancelled(): number;
  tick(timestamp?: number): void;
  restore(): void;
}

type FrameCallback = (timestamp: number) => void;

export function installFrameLoop(): FakeFrameLoop {
  const scope = globalThis as unknown as Record<string, unknown>;
  const originalRaf = scope.requestAnimationFrame;
  const originalCancel = scope.cancelAnimationFrame;
  let nextId = 1;
  let cancelledCount = 0;
  const queue = new Map<number, FrameCallback>();

  scope.requestAnimationFrame = (callback: FrameCallback) => {
    const id = nextId;
    nextId += 1;
    queue.set(id, callback);
    return id;
  };
  scope.cancelAnimationFrame = (id: number) => {
    if (queue.delete(id)) cancelledCount += 1;
  };

  return {
    pending: () => queue.size,
    cancelled: () => cancelledCount,
    tick(timestamp = 0) {
      const due = Array.from(queue.entries());
      queue.clear();
      for (const [, callback] of due) callback(timestamp);
    },
    restore() {
      queue.clear();
      if (originalRaf === undefined) delete scope.requestAnimationFrame;
      else scope.requestAnimationFrame = originalRaf;
      if (originalCancel === undefined) delete scope.cancelAnimationFrame;
      else scope.cancelAnimationFrame = originalCancel;
    },
  };
}

export function fakeCues(): readonly FakeCue[] {
  return [
    { id: 'first', startTime: 1, endTime: 2, text: 'First line' },
    { id: 'second', startTime: 2, endTime: 3, text: 'Second line' },
    { id: 'third', startTime: 4, endTime: 5, text: 'Third line' },
  ];
}
