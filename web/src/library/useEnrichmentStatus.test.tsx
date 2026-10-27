import { act, renderHook } from '@testing-library/react';
import { StrictMode, type ReactNode } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  useEnrichmentStatus,
  type EnrichmentConnectionFactory,
  type EnrichmentConnectionOptions,
  type EnrichmentHubConnection,
  type EnrichmentStatusNotification,
} from './useEnrichmentStatus';

type EventHandler = (...args: unknown[]) => void;

interface FakeConnection extends EnrichmentHubConnection {
  options: EnrichmentConnectionOptions;
  startCalls: number;
  stopCalls: number;
  handlers: Map<string, EventHandler>;
  reconnect: (() => void) | null;
  close: (() => void) | null;
}

function createFakeHub(start: (connection: FakeConnection) => Promise<void> = async () => {}) {
  const connections: FakeConnection[] = [];
  const factory: EnrichmentConnectionFactory = (options) => {
    const connection: FakeConnection = {
      options,
      startCalls: 0,
      stopCalls: 0,
      handlers: new Map(),
      reconnect: null,
      close: null,
      start() {
        connection.startCalls += 1;
        return start(connection);
      },
      stop() {
        connection.stopCalls += 1;
        return Promise.resolve();
      },
      on(name, handler) {
        connection.handlers.set(name, handler);
      },
      off(name, handler) {
        if (connection.handlers.get(name) === handler) {
          connection.handlers.delete(name);
        }
      },
      onreconnected(handler) {
        connection.reconnect = handler;
      },
      onclose(handler) {
        connection.close = handler;
      },
    };
    connections.push(connection);
    return connection;
  };
  return { factory, connections };
}

function liveConnections(connections: FakeConnection[]) {
  return connections.filter(
    (connection) => connection.startCalls > 0 && connection.stopCalls === 0,
  );
}

async function advance(ms: number) {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
}

function expectCleanTeardown(connections: FakeConnection[]) {
  expect(liveConnections(connections)).toHaveLength(0);
  expect(vi.getTimerCount()).toBe(0);
  vi.useRealTimers();
}

afterEach(() => {
  vi.unstubAllEnvs();
  vi.useRealTimers();
});

describe('useEnrichmentStatus connection lifecycle', () => {
  it('opens one hub connection against the API base URL with credentials disabled and no auth headers', async () => {
    vi.useFakeTimers();
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.example');
    const { factory, connections } = createFakeHub();

    const { unmount } = renderHook(() => useEnrichmentStatus(factory));
    await advance(0);

    expect(connections).toHaveLength(1);
    expect(connections[0].options).toEqual({
      url: 'http://api.example/hubs/enrichment',
      withCredentials: false,
    });
    expect(Object.keys(connections[0].options).sort()).toEqual(['url', 'withCredentials']);
    expect(connections[0].startCalls).toBe(1);
    expect(liveConnections(connections)).toHaveLength(1);

    unmount();
    await advance(0);
    expect(connections[0].stopCalls).toBe(1);
    expectCleanTeardown(connections);
  });

  it('falls back to the window origin when no API base URL is configured', async () => {
    vi.useFakeTimers();
    vi.stubEnv('VITE_API_BASE_URL', '');
    const { factory, connections } = createFakeHub();

    const { unmount } = renderHook(() => useEnrichmentStatus(factory));
    await advance(0);

    expect(connections[0].options.url).toBe(`${window.location.origin}/hubs/enrichment`);

    unmount();
    await advance(0);
    expectCleanTeardown(connections);
  });

  it('registers event and lifecycle handlers before start and disposes them on cleanup', async () => {
    vi.useFakeTimers();
    let handlersAtStart: { event: boolean; reconnect: boolean; close: boolean } | null = null;
    const { factory, connections } = createFakeHub(async (connection) => {
      handlersAtStart = {
        event: connection.handlers.has('EnrichmentStatusChanged'),
        reconnect: connection.reconnect !== null,
        close: connection.close !== null,
      };
    });

    const { unmount } = renderHook(() => useEnrichmentStatus(factory));
    await advance(0);

    expect(handlersAtStart).toEqual({ event: true, reconnect: true, close: true });

    unmount();
    await advance(0);
    expect(connections[0].handlers.size).toBe(0);
    expect(connections[0].stopCalls).toBe(1);
    expectCleanTeardown(connections);
  });

  it('keeps a single live connection across a StrictMode remount', async () => {
    vi.useFakeTimers();
    const { factory, connections } = createFakeHub();
    const wrapper = ({ children }: { children: ReactNode }) => <StrictMode>{children}</StrictMode>;

    const first = renderHook(() => useEnrichmentStatus(factory), { wrapper });
    await advance(0);
    expect(connections).toHaveLength(1);
    expect(liveConnections(connections)).toHaveLength(1);

    first.unmount();
    await advance(0);
    expect(liveConnections(connections)).toHaveLength(0);

    const second = renderHook(() => useEnrichmentStatus(factory), { wrapper });
    await advance(0);
    expect(connections).toHaveLength(2);
    expect(liveConnections(connections)).toHaveLength(1);

    second.unmount();
    await advance(0);
    expectCleanTeardown(connections);
  });

  it('retries a failed initial start on 1s, 2s, 4s, 8s, 16s then a 30s cap', async () => {
    vi.useFakeTimers();
    const { factory, connections } = createFakeHub(() => Promise.reject(new Error('start failed')));

    const { unmount } = renderHook(() => useEnrichmentStatus(factory));
    await advance(0);
    expect(connections).toHaveLength(1);

    const delays = [1000, 2000, 4000, 8000, 16000, 30000, 30000];
    let attempt = 1;
    for (const delay of delays) {
      await advance(delay - 1);
      expect(connections).toHaveLength(attempt);

      await advance(1);
      attempt += 1;
      expect(connections).toHaveLength(attempt);
    }

    expect(connections).toHaveLength(8);

    unmount();
    await advance(0);
    expectCleanTeardown(connections);
  });

  it('retries the connection after automatic reconnect is exhausted', async () => {
    vi.useFakeTimers();
    const { factory, connections } = createFakeHub();

    const { unmount } = renderHook(() => useEnrichmentStatus(factory));
    await advance(0);
    expect(connections).toHaveLength(1);

    await act(async () => {
      connections[0].close?.();
    });
    await advance(999);
    expect(connections).toHaveLength(1);

    await advance(1);
    expect(connections).toHaveLength(2);
    expect(liveConnections(connections)).toHaveLength(1);

    unmount();
    await advance(0);
    expectCleanTeardown(connections);
  });

  it('runs a single start and retry owner across a StrictMode remount', async () => {
    vi.useFakeTimers();
    const { factory, connections } = createFakeHub(() => Promise.reject(new Error('start failed')));
    const wrapper = ({ children }: { children: ReactNode }) => <StrictMode>{children}</StrictMode>;

    const first = renderHook(() => useEnrichmentStatus(factory), { wrapper });
    await advance(0);
    expect(connections).toHaveLength(1);

    first.unmount();
    await advance(0);
    expect(vi.getTimerCount()).toBe(0);

    const second = renderHook(() => useEnrichmentStatus(factory), { wrapper });
    await advance(0);
    expect(connections).toHaveLength(2);
    expect(vi.getTimerCount()).toBe(1);

    await advance(1000);
    expect(connections).toHaveLength(3);

    second.unmount();
    await advance(0);
    expectCleanTeardown(connections);
  });

  it('writes nothing and schedules nothing when a start resolves after disposal', async () => {
    vi.useFakeTimers();
    let resolveStart: (() => void) | null = null;
    const { factory, connections } = createFakeHub(
      () =>
        new Promise<void>((resolve) => {
          resolveStart = resolve;
        }),
    );
    const listener = vi.fn<(notification: EnrichmentStatusNotification) => void>();

    const { result, unmount } = renderHook(() => useEnrichmentStatus(factory));
    act(() => {
      result.current.subscribe(listener);
    });
    await advance(0);
    expect(connections).toHaveLength(1);

    unmount();
    await advance(0);
    expect(resolveStart).not.toBeNull();
    await act(async () => {
      resolveStart?.();
      await Promise.resolve();
    });

    expect(listener).not.toHaveBeenCalled();
    expect(connections).toHaveLength(1);
    expect(connections[0].stopCalls).toBe(1);
    expectCleanTeardown(connections);
  });

  it('cancels a pending retry timer on cleanup', async () => {
    vi.useFakeTimers();
    const { factory, connections } = createFakeHub(() => Promise.reject(new Error('start failed')));

    const { unmount } = renderHook(() => useEnrichmentStatus(factory));
    await advance(0);
    expect(vi.getTimerCount()).toBe(1);

    unmount();
    await advance(0);
    expect(vi.getTimerCount()).toBe(0);
    expect(connections).toHaveLength(1);
    expectCleanTeardown(connections);
  });

  it('publishes valid events, connect recovery, and reconnect recovery to subscribers', async () => {
    vi.useFakeTimers();
    const { factory, connections } = createFakeHub();
    const listener = vi.fn<(notification: EnrichmentStatusNotification) => void>();

    const { result, unmount } = renderHook(() => useEnrichmentStatus(factory));
    act(() => {
      result.current.subscribe(listener);
    });
    await advance(0);

    expect(listener).toHaveBeenCalledWith({ kind: 'recovery' });

    await act(async () => {
      connections[0].handlers.get('EnrichmentStatusChanged')?.({
        id: 7,
        enrichmentStatus: 'Enriched',
      });
    });
    expect(listener).toHaveBeenCalledWith({
      kind: 'event',
      args: [{ id: 7, enrichmentStatus: 'Enriched' }],
    });

    listener.mockClear();
    await act(async () => {
      connections[0].reconnect?.();
    });
    expect(listener).toHaveBeenCalledWith({ kind: 'recovery' });

    const second = vi.fn<(notification: EnrichmentStatusNotification) => void>();
    const unsubscribe = result.current.subscribe(second);
    unsubscribe();
    listener.mockClear();
    await act(async () => {
      connections[0].reconnect?.();
    });
    expect(listener).toHaveBeenCalledWith({ kind: 'recovery' });
    expect(second).not.toHaveBeenCalled();

    unmount();
    await advance(0);
    expectCleanTeardown(connections);
  });
});
