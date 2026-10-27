import { QueryClient, QueryClientProvider, QueryObserver } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import { StrictMode, type ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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

interface TestBrowseRow {
  id: number | string;
  title: string;
  enrichmentStatus: string;
}

interface TestBrowsePage {
  items: TestBrowseRow[];
  totalCount: number;
}

const standingPageKey = [
  'browseMovies',
  { sort: 'Title', direction: 'Ascending', page: 1, pageSize: 20 },
] as const;
const filteredPageKey = [
  'browseMovies',
  { sort: 'Title', direction: 'Ascending', page: 1, pageSize: 20, text: 'dune' },
] as const;

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

function createClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
}

function renderHub(
  factory: EnrichmentConnectionFactory,
  client: QueryClient = createClient(),
  strict = false,
) {
  function Wrapper({ children }: { children: ReactNode }) {
    const content = <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    return strict ? <StrictMode>{content}</StrictMode> : content;
  }
  return renderHook(() => useEnrichmentStatus(factory), { wrapper: Wrapper });
}

function freezeBrowseRefetch(client: QueryClient) {
  client.setQueryDefaults(['browseMovies'], {
    queryFn: () => new Promise<never>(() => {}),
  });
}

function browseRow(id: number, enrichmentStatus: string): TestBrowseRow {
  return { id, title: `Movie ${id}`, enrichmentStatus };
}

function browsePage(rows: TestBrowseRow[]): TestBrowsePage {
  return { items: rows, totalCount: rows.length };
}

function readPage(client: QueryClient, key: readonly unknown[]): TestBrowsePage | undefined {
  return client.getQueryData<TestBrowsePage>(key);
}

function isInvalidated(client: QueryClient, key: readonly unknown[]): boolean {
  return client.getQueryCache().find({ queryKey: key })?.state.isInvalidated ?? false;
}

function emitEvent(connections: FakeConnection[], ...args: unknown[]) {
  const handler = connections[0].handlers.get('EnrichmentStatusChanged');
  if (!handler) {
    throw new Error('EnrichmentStatusChanged handler is not registered');
  }
  return handler(...args);
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

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.useRealTimers();
});

describe('useEnrichmentStatus connection lifecycle', () => {
  it('opens one hub connection against the API base URL with credentials disabled and no auth headers', async () => {
    vi.useFakeTimers();
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.example');
    const { factory, connections } = createFakeHub();

    const { unmount } = renderHub(factory);
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

    const { unmount } = renderHub(factory);
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

    const { unmount } = renderHub(factory);
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

    const first = renderHub(factory, createClient(), true);
    await advance(0);
    expect(connections).toHaveLength(1);
    expect(liveConnections(connections)).toHaveLength(1);

    first.unmount();
    await advance(0);
    expect(liveConnections(connections)).toHaveLength(0);

    const second = renderHub(factory, createClient(), true);
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

    const { unmount } = renderHub(factory);
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

    const { unmount } = renderHub(factory);
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

    const first = renderHub(factory, createClient(), true);
    await advance(0);
    expect(connections).toHaveLength(1);

    first.unmount();
    await advance(0);
    expect(vi.getTimerCount()).toBe(0);

    const second = renderHub(factory, createClient(), true);
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

    const { result, unmount } = renderHub(factory);
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

    const { unmount } = renderHub(factory);
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

    const { result, unmount } = renderHub(factory);
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
      payload: { id: 7, enrichmentStatus: 'Enriched' },
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

describe('useEnrichmentStatus event cache updates', () => {
  it('patches every matching browse variant for each status and invalidates the browse caches', async () => {
    const client = createClient();
    freezeBrowseRefetch(client);
    client.setQueryData(
      standingPageKey,
      browsePage([browseRow(1, 'Pending'), browseRow(2, 'Enriched')]),
    );
    client.setQueryData(filteredPageKey, browsePage([browseRow(1, 'Pending')]));
    const hub = createFakeHub();
    const listener = vi.fn<(notification: EnrichmentStatusNotification) => void>();
    const { result, unmount } = renderHub(hub.factory, client);
    act(() => {
      result.current.subscribe(listener);
    });
    await advance(0);

    for (const status of ['Pending', 'Enriched', 'NotFound', 'Failed'] as const) {
      await act(async () => {
        emitEvent(hub.connections, { id: 1, enrichmentStatus: status });
      });

      expect(readPage(client, standingPageKey)?.items[0].enrichmentStatus).toBe(status);
      expect(readPage(client, filteredPageKey)?.items[0].enrichmentStatus).toBe(status);
      expect(readPage(client, standingPageKey)?.items[1].enrichmentStatus).toBe('Enriched');
      expect(isInvalidated(client, standingPageKey)).toBe(true);
      expect(isInvalidated(client, filteredPageKey)).toBe(true);
      expect(listener).toHaveBeenCalledWith({
        kind: 'event',
        payload: { id: 1, enrichmentStatus: status },
      });
    }

    unmount();
    await advance(0);
  });

  it('ignores the malformed payload matrix without patching or invalidating browse caches', async () => {
    const client = createClient();
    freezeBrowseRefetch(client);
    const hub = createFakeHub();
    const listener = vi.fn<(notification: EnrichmentStatusNotification) => void>();
    const { result, unmount } = renderHub(hub.factory, client);
    act(() => {
      result.current.subscribe(listener);
    });
    await advance(0);
    listener.mockClear();
    const seeded = browsePage([browseRow(1, 'Pending')]);
    client.setQueryData(standingPageKey, seeded);

    const malformed: unknown[][] = [
      [null],
      [{}],
      [{ enrichmentStatus: 'Enriched' }],
      [{ id: '1', enrichmentStatus: 'Enriched' }],
      [{ id: 1.5, enrichmentStatus: 'Enriched' }],
      [{ id: Number.POSITIVE_INFINITY, enrichmentStatus: 'Enriched' }],
      [{ id: Number.NaN, enrichmentStatus: 'Enriched' }],
      [{ id: 1, enrichmentStatus: 'Unknown' }],
      [],
      [
        { id: 1, enrichmentStatus: 'Enriched' },
        { id: 1, enrichmentStatus: 'Enriched' },
      ],
    ];

    for (const args of malformed) {
      await act(async () => {
        emitEvent(hub.connections, ...args);
      });
    }

    expect(readPage(client, standingPageKey)).toBe(seeded);
    expect(isInvalidated(client, standingPageKey)).toBe(false);
    expect(listener).not.toHaveBeenCalled();

    unmount();
    await advance(0);
  });

  it('never fabricates rows for absent ids and keeps unchanged rows untouched for duplicates', async () => {
    const client = createClient();
    freezeBrowseRefetch(client);
    const seeded = browsePage([browseRow(1, 'Pending')]);
    client.setQueryData(standingPageKey, seeded);
    const hub = createFakeHub();
    const { unmount } = renderHub(hub.factory, client);
    await advance(0);

    await act(async () => {
      emitEvent(hub.connections, { id: 99, enrichmentStatus: 'Enriched' });
    });
    expect(readPage(client, standingPageKey)).toBe(seeded);

    await act(async () => {
      emitEvent(hub.connections, { id: 1, enrichmentStatus: 'Pending' });
    });
    expect(readPage(client, standingPageKey)).toBe(seeded);

    await act(async () => {
      emitEvent(hub.connections, { id: 1, enrichmentStatus: 'Failed' });
    });
    await act(async () => {
      emitEvent(hub.connections, { id: 1, enrichmentStatus: 'Failed' });
    });
    const page = readPage(client, standingPageKey);
    expect(page?.items).toHaveLength(1);
    expect(page?.items[0]).toEqual({ id: 1, title: 'Movie 1', enrichmentStatus: 'Failed' });

    unmount();
    await advance(0);
  });

  it('invalidates browse including Pending rows on a successful initial start', async () => {
    const client = createClient();
    freezeBrowseRefetch(client);
    client.setQueryData(standingPageKey, browsePage([browseRow(1, 'Pending')]));
    const hub = createFakeHub();

    const { unmount } = renderHub(hub.factory, client);
    await advance(0);

    expect(isInvalidated(client, standingPageKey)).toBe(true);
    expect(readPage(client, standingPageKey)?.items[0].enrichmentStatus).toBe('Pending');

    unmount();
    await advance(0);
  });

  it('invalidates browse including Pending rows on reconnect with no event', async () => {
    const client = createClient();
    freezeBrowseRefetch(client);
    const hub = createFakeHub();
    const { unmount } = renderHub(hub.factory, client);
    await advance(0);

    client.setQueryData(standingPageKey, browsePage([browseRow(1, 'Pending')]));
    expect(isInvalidated(client, standingPageKey)).toBe(false);

    await act(async () => {
      hub.connections[0].reconnect?.();
    });

    expect(isInvalidated(client, standingPageKey)).toBe(true);
    expect(readPage(client, standingPageKey)?.items[0].enrichmentStatus).toBe('Pending');

    unmount();
    await advance(0);
  });

  it('keeps a valid event that arrives during an older refetch and converges on a fresh pass', async () => {
    vi.useFakeTimers();
    const client = createClient();
    const pendingResponses: Array<(page: TestBrowsePage) => void> = [];
    const queryFn = () =>
      new Promise<TestBrowsePage>((resolve) => {
        pendingResponses.push(resolve);
      });
    const stalePage = browsePage([browseRow(1, 'Pending')]);
    const freshPage = browsePage([browseRow(1, 'Enriched')]);
    const hub = createFakeHub();
    const { unmount } = renderHub(hub.factory, client);
    await advance(0);

    client.setQueryData(standingPageKey, stalePage);
    const observer = new QueryObserver(client, {
      queryKey: standingPageKey,
      queryFn,
      retry: false,
    });
    const unsubscribeObserver = observer.subscribe(() => {});
    expect(pendingResponses).toHaveLength(1);

    await act(async () => {
      emitEvent(hub.connections, { id: 1, enrichmentStatus: 'Enriched' });
    });
    expect(pendingResponses).toHaveLength(2);

    await act(async () => {
      pendingResponses[0](stalePage);
      pendingResponses[1](freshPage);
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(readPage(client, standingPageKey)?.items[0].enrichmentStatus).toBe('Enriched');

    unsubscribeObserver();
    unmount();
    await advance(0);
    expectCleanTeardown(hub.connections);
  });

  it('retains the in-place event patch when the authoritative refetch fails', async () => {
    const client = createClient();
    const observer = new QueryObserver(client, {
      queryKey: standingPageKey,
      queryFn: () => Promise.reject(new Error('offline')),
      retry: false,
    });
    const unsubscribeObserver = observer.subscribe(() => {});
    const hub = createFakeHub();
    const { unmount } = renderHub(hub.factory, client);
    await advance(0);

    client.setQueryData(standingPageKey, browsePage([browseRow(1, 'Pending')]));
    await act(async () => {
      emitEvent(hub.connections, { id: 1, enrichmentStatus: 'Enriched' });
    });
    await advance(0);

    expect(readPage(client, standingPageKey)?.items[0].enrichmentStatus).toBe('Enriched');
    expect(client.getQueryState(standingPageKey)?.status).toBe('error');

    unsubscribeObserver();
    unmount();
    await advance(0);
  });
});

describe('useEnrichmentStatus subscription seam', () => {
  it('publishes valid events for ids absent from the browse cache without fabricating rows', async () => {
    const client = createClient();
    const hub = createFakeHub();
    const listener = vi.fn<(notification: EnrichmentStatusNotification) => void>();
    const { result } = renderHub(hub.factory, client);
    const unsubscribe = result.current.subscribe(listener);
    await advance(0);
    listener.mockClear();

    await act(async () => {
      emitEvent(hub.connections, { id: 500, enrichmentStatus: 'NotFound' });
    });

    expect(listener).toHaveBeenCalledWith({
      kind: 'event',
      payload: { id: 500, enrichmentStatus: 'NotFound' },
    });
    expect(client.getQueryCache().find({ queryKey: standingPageKey })).toBeUndefined();

    unsubscribe();
  });
});
