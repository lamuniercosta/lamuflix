export const MAX_VISIBLE_TOASTS = 3;
export const TOAST_LIFETIME_MS = 5000;

export type ToastPauseReason = 'hover' | 'focus';

export interface ApiFailureToastInput {
  title: string;
  status: string;
  traceId: string;
  reason?: string | undefined;
}

export interface ApiFailureToast extends ApiFailureToastInput {
  id: string;
}

export interface ApiFailureToastQueueSnapshot {
  toasts: readonly ApiFailureToast[];
  announcements: readonly string[];
}

export interface ApiFailureToastQueue {
  publish: (input: ApiFailureToastInput) => void;
  dismiss: (id: string) => void;
  pause: (id: string, reason: ToastPauseReason) => void;
  resume: (id: string, reason: ToastPauseReason) => void;
  subscribe: (listener: () => void) => () => void;
  getSnapshot: () => ApiFailureToastQueueSnapshot;
  reset: () => void;
}

interface Entry extends ApiFailureToast {
  remainingMs: number;
  startedAt: number | null;
  timer: ReturnType<typeof setTimeout> | null;
  pausedByHover: boolean;
  pausedByFocus: boolean;
}

function announcementFor(input: ApiFailureToastInput): string {
  const reason = input.reason ? ` Reason: ${input.reason}.` : '';
  return `API failure: ${input.title}. Status ${input.status}.${reason} Trace ID: ${input.traceId}.`;
}

export function createApiFailureToastQueue(): ApiFailureToastQueue {
  const entries: Entry[] = [];
  const announcements: string[] = [];
  const listeners = new Set<() => void>();
  let nextId = 0;
  let snapshot: ApiFailureToastQueueSnapshot = { toasts: [], announcements: [] };

  function publishSnapshot(): void {
    snapshot = {
      toasts: entries.map(({ id, title, status, reason, traceId }) => ({
        id,
        title,
        status,
        reason,
        traceId,
      })),
      announcements: [...announcements],
    };
    for (const listener of listeners) {
      listener();
    }
  }

  function clearTimer(entry: Entry): void {
    if (entry.timer !== null) {
      clearTimeout(entry.timer);
      entry.timer = null;
    }
    entry.startedAt = null;
  }

  function schedule(entry: Entry): void {
    entry.startedAt = Date.now();
    entry.timer = setTimeout(() => {
      entry.timer = null;
      remove(entry.id);
    }, entry.remainingMs);
  }

  function remove(id: string): void {
    const index = entries.findIndex((entry) => entry.id === id);
    if (index === -1) {
      return;
    }
    const [entry] = entries.splice(index, 1);
    clearTimer(entry);
    publishSnapshot();
  }

  function find(id: string): Entry | undefined {
    return entries.find((entry) => entry.id === id);
  }

  return {
    publish(input) {
      while (entries.length >= MAX_VISIBLE_TOASTS) {
        const evicted = entries.shift();
        if (evicted) {
          clearTimer(evicted);
        }
      }

      const entry: Entry = {
        id: `api-failure-${(nextId += 1)}`,
        title: input.title,
        status: input.status,
        reason: input.reason,
        traceId: input.traceId,
        remainingMs: TOAST_LIFETIME_MS,
        startedAt: null,
        timer: null,
        pausedByHover: false,
        pausedByFocus: false,
      };
      entries.push(entry);
      announcements.push(announcementFor(input));
      schedule(entry);
      publishSnapshot();
    },
    dismiss(id) {
      remove(id);
    },
    pause(id, reason) {
      const entry = find(id);
      if (!entry) {
        return;
      }
      const wasPaused = entry.pausedByHover || entry.pausedByFocus;
      if (reason === 'hover') {
        entry.pausedByHover = true;
      } else {
        entry.pausedByFocus = true;
      }
      if (wasPaused) {
        return;
      }
      if (entry.startedAt !== null) {
        entry.remainingMs = Math.max(0, entry.remainingMs - (Date.now() - entry.startedAt));
      }
      clearTimer(entry);
    },
    resume(id, reason) {
      const entry = find(id);
      if (!entry || (!entry.pausedByHover && !entry.pausedByFocus)) {
        return;
      }
      if (reason === 'hover') {
        entry.pausedByHover = false;
      } else {
        entry.pausedByFocus = false;
      }
      if (entry.pausedByHover || entry.pausedByFocus) {
        return;
      }
      schedule(entry);
    },
    subscribe(listener) {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    getSnapshot() {
      return snapshot;
    },
    reset() {
      for (const entry of entries) {
        clearTimer(entry);
      }
      entries.length = 0;
      announcements.length = 0;
      publishSnapshot();
    },
  };
}

export const apiFailureToastQueue: ApiFailureToastQueue = createApiFailureToastQueue();
