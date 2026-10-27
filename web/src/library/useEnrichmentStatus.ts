import { HubConnectionBuilder } from '@microsoft/signalr';
import { useQueryClient, type QueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useState } from 'react';
import { z } from 'zod';
import type { components } from '../api/types';

export interface EnrichmentConnectionOptions {
  readonly url: string;
  readonly withCredentials: false;
}

export interface EnrichmentHubConnection {
  start(): Promise<void>;
  stop(): Promise<void>;
  on(eventName: string, handler: (...args: unknown[]) => void): void;
  off(eventName: string, handler?: (...args: unknown[]) => void): void;
  onreconnected(handler: () => void): void;
  onclose(handler: () => void): void;
}

export type EnrichmentConnectionFactory = (
  options: EnrichmentConnectionOptions,
) => EnrichmentHubConnection;

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
export type EnrichmentStatusEvent = components['schemas']['EnrichmentStatusChangedDto'];

export type EnrichmentStatusNotification =
  | { readonly kind: 'event'; readonly payload: EnrichmentStatusEvent }
  | { readonly kind: 'recovery' };

export type EnrichmentStatusListener = (notification: EnrichmentStatusNotification) => void;

export interface EnrichmentStatusSubscription {
  subscribe(listener: EnrichmentStatusListener): () => void;
}

const ENRICHMENT_STATUS_EVENT = 'EnrichmentStatusChanged';
const ENRICHMENT_HUB_PATH = '/hubs/enrichment';
const BROWSE_MOVIES_QUERY_KEY = ['browseMovies'] as const;
const RETRY_DELAYS_MS = [1000, 2000, 4000, 8000, 16000, 30000] as const;

const enrichmentStatusChangedSchema = z.object({
  id: z.number().int().finite(),
  enrichmentStatus: z.enum(['Pending', 'Enriched', 'NotFound', 'Failed']),
});

function parseEnrichmentStatusEvent(args: readonly unknown[]): EnrichmentStatusEvent | null {
  if (args.length !== 1) {
    return null;
  }
  const parsed = enrichmentStatusChangedSchema.safeParse(args[0]);
  return parsed.success ? parsed.data : null;
}

function patchBrowseEnrichmentStatus(
  current: BrowseMoviesResponse | undefined,
  event: EnrichmentStatusEvent,
): BrowseMoviesResponse | undefined {
  if (!current) {
    return current;
  }
  let changed = false;
  const items = current.items.map((item) => {
    if (String(item.id) !== String(event.id)) {
      return item;
    }
    if (item.enrichmentStatus === event.enrichmentStatus) {
      return item;
    }
    changed = true;
    return { ...item, enrichmentStatus: event.enrichmentStatus };
  });
  return changed ? { ...current, items } : current;
}

function invalidateBrowse(queryClient: QueryClient): void {
  void queryClient.invalidateQueries({ queryKey: BROWSE_MOVIES_QUERY_KEY }).catch(() => {});
}

function enrichmentHubUrl(): string {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || window.location.origin;
  return new URL(ENRICHMENT_HUB_PATH, baseUrl).toString();
}

function stopQuietly(connection: EnrichmentHubConnection): void {
  void connection.stop().catch(() => {});
}

function shutdownConnection(
  target: EnrichmentHubConnection,
  handler: (...args: unknown[]) => void,
): void {
  target.off(ENRICHMENT_STATUS_EVENT, handler);
  stopQuietly(target);
}

function createEnrichmentConnection(options: EnrichmentConnectionOptions): EnrichmentHubConnection {
  return new HubConnectionBuilder()
    .withUrl(options.url, { withCredentials: options.withCredentials })
    .withAutomaticReconnect()
    .build();
}

export function useEnrichmentStatus(
  createConnection: EnrichmentConnectionFactory = createEnrichmentConnection,
): EnrichmentStatusSubscription {
  const queryClient = useQueryClient();
  const [registry] = useState(() => ({ listeners: new Set<EnrichmentStatusListener>() }));
  const [subscription] = useState<EnrichmentStatusSubscription>(() => ({
    subscribe(listener) {
      registry.listeners.add(listener);
      return () => {
        registry.listeners.delete(listener);
      };
    },
  }));

  const publish = useCallback(
    (notification: EnrichmentStatusNotification) => {
      for (const listener of [...registry.listeners]) {
        listener(notification);
      }
    },
    [registry],
  );

  useEffect(() => {
    let disposed = false;
    let connection: EnrichmentHubConnection | null = null;
    let retryTimer: ReturnType<typeof setTimeout> | null = null;
    let retryAttempt = 0;

    const clearRetry = () => {
      if (retryTimer !== null) {
        clearTimeout(retryTimer);
        retryTimer = null;
      }
    };

    const shutdown = (target: EnrichmentHubConnection) => {
      shutdownConnection(target, onEvent);
    };

    const scheduleRetry = () => {
      if (disposed || retryTimer !== null) {
        return;
      }
      const delay = RETRY_DELAYS_MS[Math.min(retryAttempt, RETRY_DELAYS_MS.length - 1)];
      retryAttempt += 1;
      retryTimer = setTimeout(() => {
        retryTimer = null;
        connect();
      }, delay);
    };

    const onEvent = (...args: unknown[]) => {
      if (disposed) {
        return;
      }
      const event = parseEnrichmentStatusEvent(args);
      if (!event) {
        return;
      }
      queryClient.setQueriesData<BrowseMoviesResponse>(
        { queryKey: BROWSE_MOVIES_QUERY_KEY },
        (current) => patchBrowseEnrichmentStatus(current, event),
      );
      invalidateBrowse(queryClient);
      publish({ kind: 'event', payload: event });
    };

    const onReconnected = () => {
      if (disposed) {
        return;
      }
      retryAttempt = 0;
      invalidateBrowse(queryClient);
      publish({ kind: 'recovery' });
    };

    const onClose = () => {
      if (disposed) {
        return;
      }
      const current = connection;
      connection = null;
      if (current) {
        shutdown(current);
      }
      scheduleRetry();
    };

    const connect = () => {
      if (disposed) {
        return;
      }
      const next = createConnection({
        url: enrichmentHubUrl(),
        withCredentials: false,
      });
      connection = next;
      next.on(ENRICHMENT_STATUS_EVENT, onEvent);
      next.onreconnected(onReconnected);
      next.onclose(onClose);
      void next.start().then(
        () => {
          if (disposed) {
            return;
          }
          retryAttempt = 0;
          invalidateBrowse(queryClient);
          publish({ kind: 'recovery' });
        },
        () => {
          if (disposed) {
            return;
          }
          shutdown(next);
          if (connection === next) {
            connection = null;
          }
          scheduleRetry();
        },
      );
    };

    connect();

    return () => {
      disposed = true;
      clearRetry();
      registry.listeners.clear();
      const current = connection;
      connection = null;
      if (current) {
        shutdown(current);
      }
    };
  }, [createConnection, publish, queryClient, registry]);

  return subscription;
}
