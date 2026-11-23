import { useCallback, useEffect, useId, useRef, useState } from 'react';
import MovieCard from '../../library/MovieCard';
import '../../library/library.css';
import { MovieCarousel } from '../../ui/MovieCarousel';
import { RetryIcon } from '../../ui/icons';
import { FOR_YOU_TAKE, fetchForYouSlice, type ForYouRecommendation } from './forYouApi';
import { MORE_FOR_YOU_LABEL, groupForYou, type ForYouRow } from './forYouGrouping';
import './ForYouPage.css';

export interface ForYouPageProps {
  onOpenMovie?: (id: number | string) => void;
}

interface ForYouVisit {
  items: ForYouRecommendation[];
  themed: ForYouRow[];
  hasSeeds: boolean;
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
const EMPTY_WITHOUT_SEEDS = 'No recommendations yet. Mark movies as watched or liked to get some.';
const EMPTY_WITH_SEEDS =
  'No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.';
const BROWSE_LIBRARY_LABEL = 'Browse the library';
const LIBRARY_HREF = '/';

function seenIds(items: ReadonlyArray<ForYouRecommendation>): Set<number> {
  return new Set(items.map((item) => Number(item.id)));
}

function resolveRows(visit: ForYouVisit): ForYouRow[] {
  const byId = new Map<number, ForYouRecommendation>();
  const unique: ForYouRecommendation[] = [];
  for (const movie of visit.items) {
    const id = Number(movie.id);
    if (byId.has(id)) {
      continue;
    }
    byId.set(id, movie);
    unique.push(movie);
  }
  const themed = visit.themed
    .map((row) => ({
      ...row,
      movies: row.movies
        .map((movie) => byId.get(Number(movie.id)))
        .filter((movie): movie is ForYouRecommendation => movie !== undefined),
    }))
    .filter((row) => row.movies.length > 0);
  const themedIds = new Set(themed.flatMap((row) => row.movies.map((movie) => Number(movie.id))));
  const more = unique.filter((movie) => !themedIds.has(Number(movie.id)));
  const showMore =
    visit.items.length > 0 && (more.length > 0 || visit.hasMore || visit.buffer.length > 0);
  return showMore
    ? [...themed, { key: 'more', kind: 'more', label: MORE_FOR_YOU_LABEL, movies: more }]
    : themed;
}

export default function ForYouPage({ onOpenMovie }: ForYouPageProps) {
  const pageHeadingId = useId();
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
        themed: groupForYou(slice.items),
        hasSeeds: slice.hasSeeds,
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
    identityRef.current += 1;
    busyRef.current = false;
    setPending(null);
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
        themed: visit.themed,
        hasSeeds: visit.hasSeeds,
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
          themed: visit.themed,
          hasSeeds: visit.hasSeeds,
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

  const rows = visit ? resolveRows(visit) : [];
  const canLoadMore = Boolean(visit && (visit.hasMore || visit.buffer.length > 0));
  const busy = pending !== null;
  const empty = phase === 'ready' && (visit?.items.length ?? 0) === 0;

  return (
    <section className="for-you-page" aria-labelledby={pageHeadingId}>
      <h1 id={pageHeadingId} className="lamu-section-heading">
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
      {empty ? (
        <div className="for-you-empty">
          <p>{visit?.hasSeeds === true ? EMPTY_WITH_SEEDS : EMPTY_WITHOUT_SEEDS}</p>
          <a className="lamu-action-quiet" href={LIBRARY_HREF}>
            {BROWSE_LIBRARY_LABEL}
          </a>
        </div>
      ) : null}
      {rows.map((row) => {
        const rowHeadingId = `${pageHeadingId}-${row.key}`;
        const isMore = row.kind === 'more';
        return (
          <section key={row.key} className="for-you-row" aria-labelledby={rowHeadingId}>
            <h2 id={rowHeadingId} className="for-you-row-heading">
              {row.label}
            </h2>
            {row.movies.length > 0 ? (
              <MovieCarousel label={row.label}>
                {row.movies.map((movie) => (
                  <MovieCard
                    key={String(movie.id)}
                    movie={{ ...movie, failureCategory: null, failureMessage: null }}
                    seed={movie.seed}
                    evidence={movie.evidence}
                    hideSeed
                    onOpenMovie={onOpenMovie}
                    action={
                      isMore ? (
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
                      ) : undefined
                    }
                  />
                ))}
              </MovieCarousel>
            ) : null}
            {isMore && canLoadMore ? (
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
      })}
    </section>
  );
}
