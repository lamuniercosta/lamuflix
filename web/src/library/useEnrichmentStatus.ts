import { HubConnectionBuilder } from '@microsoft/signalr';
import { useCallback, useEffect, useState } from 'react';

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

export type EnrichmentStatusNotification =
  { readonly kind: 'event'; readonly args: readonly unknown[] } | { readonly kind: 'recovery' };

export type EnrichmentStatusListener = (notification: EnrichmentStatusNotification) => void;

export interface EnrichmentStatusSubscription {
  subscribe(listener: EnrichmentStatusListener): () => void;
}

const ENRICHMENT_STATUS_EVENT = 'EnrichmentStatusChanged';
const ENRICHMENT_HUB_PATH = '/hubs/enrichment';
const RETRY_DELAYS_MS = [1000, 2000, 4000, 8000, 16000, 30000] as const;

function enrichmentHubUrl(): string {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || window.location.origin;
  return new URL(ENRICHMENT_HUB_PATH, baseUrl).toString();
}

function stopQuietly(connection: EnrichmentHubConnection): void {
  void connection.stop().catch(() => {});
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

    const detach = (target: EnrichmentHubConnection) => {
      target.off(ENRICHMENT_STATUS_EVENT, onEvent);
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
      publish({ kind: 'event', args });
    };

    const onReconnected = () => {
      if (disposed) {
        return;
      }
      retryAttempt = 0;
      publish({ kind: 'recovery' });
    };

    const onClose = () => {
      if (disposed) {
        return;
      }
      const current = connection;
      connection = null;
      if (current) {
        detach(current);
        stopQuietly(current);
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
          publish({ kind: 'recovery' });
        },
        () => {
          if (disposed) {
            return;
          }
          detach(next);
          stopQuietly(next);
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
        detach(current);
        stopQuietly(current);
      }
    };
  }, [createConnection, publish, registry]);

  return subscription;
}
