import { useCallback, useEffect, useId, useRef, useState } from 'react';
import MovieCard from '../../library/MovieCard';
import '../../library/library.css';
import { RetryIcon } from '../../ui/icons';
import { FOR_YOU_TAKE, fetchForYouSlice, type ForYouRecommendation } from './forYouApi';
import './ForYouPage.css';

export interface ForYouPageProps {
  onOpenMovie?: (id: number | string) => void;
}

interface ForYouVisit {
  items: ForYouRecommendation[];
  seen: ReadonlySet<number>;
  committed: number[];
  cursor: number;
  buffer: ForYouRecommendation[];
  hasMore: boolean;
}

type VisitPhase = 'loading' | 'ready' | 'error';
type PendingAction = 'append' | 'replace' | null;

const INITIAL_ERROR = 'Could not load recommendations.';
const APPEND_ERROR = 'Could not load more recommendations. Try again.';
const REPLACE_ERROR = 'Could not replace this suggestion. Try again.';
const EXHAUSTED_NOTICE = 'No new suggestions to swap in right now.';

function seenIds(items: ReadonlyArray<ForYouRecommendation>): Set<number> {
  return new Set(items.map((item) => Number(item.id)));
}

export default function ForYouPage({ onOpenMovie }: ForYouPageProps) {
  const headingId = useId();
  const [visit, setVisit] = useState<ForYouVisit | null>(null);
  const [phase, setPhase] = useState<VisitPhase>('loading');
  const [pending, setPending] = useState<PendingAction>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const identityRef = useRef(0);
  const controllerRef = useRef<AbortController | null>(null);
  const busyRef = useRef(false);

  const beginRequest = useCallback(() => {
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;
    return controller;
  }, []);

  const loadVisit = useCallback(async () => {
    const identity = identityRef.current;
    const controller = beginRequest();
    try {
      const slice = await fetchForYouSlice({
        take: FOR_YOU_TAKE,
        offset: 0,
        exclude: [],
        signal: controller.signal,
      });
      if (identity !== identityRef.current) {
        return;
      }
      setVisit({
        items: slice.items,
        seen: seenIds(slice.items),
        committed: [],
        cursor: FOR_YOU_TAKE,
        buffer: [],
        hasMore: slice.hasMore,
      });
      setPhase('ready');
    } catch {
      if (identity !== identityRef.current || controller.signal.aborted) {
        return;
      }
      setPhase('error');
      setError(INITIAL_ERROR);
    }
  }, [beginRequest]);

  useEffect(() => {
    identityRef.current += 1;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- mount-only visit seeding (plan: visit state survives query invalidation)
    void loadVisit();
    return () => {
      identityRef.current += 1;
      controllerRef.current?.abort();
      controllerRef.current = null;
    };
  }, [loadVisit]);

  const retryInitialLoad = useCallback(() => {
    setVisit(null);
    setError(null);
    setNotice(null);
    setPhase('loading');
    void loadVisit();
  }, [loadVisit]);

  const loadMore = useCallback(async () => {
    if (!visit || busyRef.current || (!visit.hasMore && visit.buffer.length === 0)) {
      return;
    }
    busyRef.current = true;
    setPending('append');
    setError(null);
    setNotice(null);
    const identity = identityRef.current;
    const controller = beginRequest();
    try {
      const staged: ForYouRecommendation[] = [];
      const seen = new Set(visit.seen);
      let cursor = visit.cursor;
      let hasMore = visit.hasMore;
      const collect = (rows: ReadonlyArray<ForYouRecommendation>) => {
        const leftover: ForYouRecommendation[] = [];
        for (const row of rows) {
          const id = Number(row.id);
          if (seen.has(id)) {
            continue;
          }
          if (staged.length < FOR_YOU_TAKE) {
            staged.push(row);
            seen.add(id);
          } else {
            leftover.push(row);
          }
        }
        return leftover;
      };
      let buffer = collect(visit.buffer);
      while (staged.length < FOR_YOU_TAKE && hasMore) {
        if (identity !== identityRef.current || controller.signal.aborted) {
          return;
        }
        const slice = await fetchForYouSlice({
          take: FOR_YOU_TAKE,
          offset: cursor,
          exclude: visit.committed,
          signal: controller.signal,
        });
        cursor += FOR_YOU_TAKE;
        hasMore = slice.hasMore;
        buffer = collect(slice.items);
      }
      if (identity !== identityRef.current) {
        return;
      }
      setVisit({
        items: [...visit.items, ...staged],
        seen,
        committed: visit.committed,
        cursor,
        buffer,
        hasMore,
      });
    } catch {
      if (identity !== identityRef.current || controller.signal.aborted) {
        return;
      }
      setError(APPEND_ERROR);
    } finally {
      busyRef.current = false;
      if (identity === identityRef.current) {
        setPending(null);
      }
    }
  }, [beginRequest, visit]);

  const replaceCard = useCallback(
    async (target: number | string) => {
      if (!visit || busyRef.current) {
        return;
      }
      const targetId = Number(target);
      busyRef.current = true;
      setPending('replace');
      setError(null);
      setNotice(null);
      const identity = identityRef.current;
      const controller = beginRequest();
      const tentative = visit.committed.includes(targetId)
        ? [...visit.committed]
        : [...visit.committed, targetId];
      try {
        let cursor = 0;
        let hasMore = true;
        const found = await (async () => {
          while (hasMore) {
            if (identity !== identityRef.current || controller.signal.aborted) {
              return null;
            }
            const slice = await fetchForYouSlice({
              take: FOR_YOU_TAKE,
              offset: cursor,
              exclude: tentative,
              signal: controller.signal,
            });
            cursor += FOR_YOU_TAKE;
            hasMore = slice.hasMore;
            const index = slice.items.findIndex((row) => !visit.seen.has(Number(row.id)));
            if (index >= 0) {
              return {
                candidate: slice.items[index],
                buffer: slice.items
                  .slice(index + 1)
                  .filter((row) => !visit.seen.has(Number(row.id))),
              };
            }
          }
          return null;
        })();
        if (identity !== identityRef.current) {
          return;
        }
        if (found === null) {
          setNotice(EXHAUSTED_NOTICE);
          return;
        }
        const seen = new Set(visit.seen);
        seen.add(Number(found.candidate.id));
        setVisit({
          items: visit.items.map((row) => (Number(row.id) === targetId ? found.candidate : row)),
          seen,
          committed: tentative,
          cursor,
          buffer: found.buffer,
          hasMore,
        });
      } catch {
        if (identity !== identityRef.current || controller.signal.aborted) {
          return;
        }
        setError(REPLACE_ERROR);
      } finally {
        busyRef.current = false;
        if (identity === identityRef.current) {
          setPending(null);
        }
      }
    },
    [beginRequest, visit],
  );

  const rows = visit?.items ?? [];
  const canLoadMore = Boolean(visit && (visit.hasMore || visit.buffer.length > 0));
  const busy = pending !== null;

  return (
    <section className="for-you-page" aria-labelledby={headingId}>
      <h1 id={headingId} className="lamu-section-heading">
        For You
      </h1>
      {phase === 'loading' ? <p role="status">Loading recommendations</p> : null}
      {error ? <p role="alert">{error}</p> : null}
      {phase === 'error' ? (
        <button type="button" className="lamu-action-quiet" onClick={retryInitialLoad}>
          Retry
        </button>
      ) : null}
      {notice ? <p role="status">{notice}</p> : null}
      {phase === 'ready' && rows.length === 0 ? (
        <p>No recommendations yet. Mark movies as watched or liked to get some.</p>
      ) : null}
      {rows.length > 0 ? (
        <div className="library-grid" role="list" aria-label="Recommended movies">
          {rows.map((movie) => (
            <div key={String(movie.id)} role="listitem">
              <MovieCard
                movie={{ ...movie, failureCategory: null, failureMessage: null }}
                seed={movie.seed}
                evidence={movie.evidence}
                onOpenMovie={onOpenMovie}
                action={
                  <button
                    type="button"
                    className="movie-card-retry"
                    aria-label={`Replace ${movie.title}`}
                    disabled={busy}
                    onClick={() => void replaceCard(movie.id)}
                  >
                    <RetryIcon size={14} />
                    <span>Replace</span>
                  </button>
                }
              />
            </div>
          ))}
        </div>
      ) : null}
      {canLoadMore ? (
        <button
          type="button"
          className="lamu-action-quiet"
          disabled={busy}
          onClick={() => void loadMore()}
        >
          Load more
        </button>
      ) : null}
    </section>
  );
}
