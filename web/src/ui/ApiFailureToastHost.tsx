import { useSyncExternalStore } from 'react';
import { apiFailureToastQueue, type ApiFailureToastQueue } from './apiFailureToastQueue';

interface ApiFailureToastHostProps {
  queue?: ApiFailureToastQueue;
}

export default function ApiFailureToastHost({
  queue = apiFailureToastQueue,
}: ApiFailureToastHostProps) {
  const { toasts, announcements } = useSyncExternalStore(queue.subscribe, queue.getSnapshot);

  return (
    <section className="lamu-toast-host" aria-label="API failure notifications">
      <div
        className="sr-only lamu-toast-announcements"
        role="status"
        aria-live="polite"
        aria-atomic="false"
      >
        {announcements.map((message, index) => (
          <p key={`announcement-${index + 1}`}>{message}</p>
        ))}
      </div>
      <div className="lamu-toast-stack">
        {toasts.map((toast) => {
          const titleId = `api-failure-title-${toast.id}`;
          return (
            <article
              key={toast.id}
              className="lamu-toast"
              aria-labelledby={titleId}
              onMouseEnter={() => queue.pause(toast.id, 'hover')}
              onMouseLeave={() => queue.resume(toast.id, 'hover')}
              onFocus={() => queue.pause(toast.id, 'focus')}
              onBlur={(event) => {
                if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
                  queue.resume(toast.id, 'focus');
                }
              }}
            >
              <div className="lamu-toast-copy">
                <h2 className="lamu-toast-title" id={titleId}>
                  {toast.title}
                </h2>
                <p className="lamu-toast-meta">
                  Status: <span className="lamu-toast-status">{toast.status}</span>
                </p>
                {toast.reason ? <p className="lamu-toast-reason">{toast.reason}</p> : null}
                <p className="lamu-toast-meta">
                  Trace ID: <span className="lamu-toast-trace">{toast.traceId}</span>
                </p>
              </div>
              <button
                type="button"
                className="lamu-toast-dismiss"
                onClick={() => queue.dismiss(toast.id)}
              >
                Dismiss
              </button>
            </article>
          );
        })}
      </div>
    </section>
  );
}
