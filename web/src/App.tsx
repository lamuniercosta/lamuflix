import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  currentReturnUrl,
  libraryReturnSearch,
  parseMovieId,
  readReturnUrl,
  useAppNavigation,
} from './features/app-navigation/useAppNavigation';
import {
  DEBOUNCE_MS,
  DEFAULT_PAGE,
  parseFilterQuery,
  toUrlQuery,
} from './features/library/filterQuery';
import type { FilterQuery } from './features/library/filterQuery';
import { useDebouncedSearch } from './features/library/useDebouncedSearch';
import DetailsBackLink from './features/movie-details/DetailsBackLink';
import MovieDetailsView from './features/movie-details/MovieDetailsView';
import ForYouPage from './features/for-you/ForYouPage';
import BulkImportDialog from './library/BulkImportDialog';
import LibraryErrorBoundary from './library/LibraryErrorBoundary';
import LibraryView from './library/LibraryView';
import type { EnrichmentStatusSubscription } from './library/useEnrichmentStatus';
import { AmbientProvider } from './ui/AmbientProvider';
import { RetryIcon, SearchIcon } from './ui/icons';

function shellSearchId(prefix: string): string {
  return `${prefix}-shell-search`;
}

function SearchField({
  id,
  value,
  onChange,
}: {
  id: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="lamu-search-field">
      <SearchIcon size={20} />
      <label className="sr-only" htmlFor={id}>
        Search titles
      </label>
      <input
        id={id}
        type="search"
        placeholder="Search titles"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  );
}

function ShellSearch({
  id,
  initialText,
  routeKey,
  onCommittedText,
}: {
  id: string;
  initialText: string;
  routeKey: number;
  onCommittedText: (text: string) => void;
}) {
  const [draft, setDraft] = useState(initialText);
  const [prevText, setPrevText] = useState(initialText);
  const [prevKey, setPrevKey] = useState(routeKey);
  if (initialText !== prevText || routeKey !== prevKey) {
    setPrevText(initialText);
    setPrevKey(routeKey);
    setDraft(initialText);
  }
  const target = useMemo(() => onCommittedText, [onCommittedText]);
  const commit = useDebouncedSearch(
    useCallback((value: string) => target(value), [target]),
    DEBOUNCE_MS,
  );
  const commitRef = useRef(commit);
  useEffect(() => {
    commitRef.current = commit;
  });
  const prevRouteKeyRef = useRef(routeKey);
  useEffect(() => {
    if (prevRouteKeyRef.current !== routeKey) {
      prevRouteKeyRef.current = routeKey;
      commitRef.current.cancel();
    }
  });
  useEffect(() => {
    const onPopState = () => {
      commitRef.current.cancel();
    };
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  return (
    <div className="lamu-shell-search">
      <SearchField
        id={id}
        value={draft}
        onChange={(value) => {
          setDraft(value);
          commit.schedule(value);
        }}
      />
    </div>
  );
}

function DetailsSearch({
  id,
  initialText,
  onSubmit,
}: {
  id: string;
  initialText: string;
  onSubmit: (text: string) => void;
}) {
  const [draft, setDraft] = useState(initialText);
  const [prev, setPrev] = useState(initialText);
  if (initialText !== prev) {
    setPrev(initialText);
    setDraft(initialText);
  }
  return (
    <div className="lamu-shell-search">
      <form
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit(draft);
        }}
      >
        <SearchField id={id} value={draft} onChange={setDraft} />
      </form>
    </div>
  );
}

export interface AppProps {
  enrichmentStatus?: EnrichmentStatusSubscription;
}

export default function App({ enrichmentStatus }: AppProps) {
  const [importOpen, setImportOpen] = useState(false);
  const bulkImportButtonRef = useRef<HTMLButtonElement>(null);
  const { route, navigate } = useAppNavigation();

  const movieId = parseMovieId(route.pathname);
  const isDetails = movieId !== null;
  const isForYou = route.pathname === '/for-you';
  const libraryQuery = useMemo<FilterQuery>(() => parseFilterQuery(route.search), [route.search]);
  const watchlistActive = useMemo(
    () => new URLSearchParams(route.search).get('watchlist') === 'true',
    [route.search],
  );

  function closeImport() {
    setImportOpen(false);
    bulkImportButtonRef.current?.focus();
  }

  const applyLibraryQuery = useCallback(
    (next: FilterQuery, options?: { replace?: boolean }) => {
      const search = toUrlQuery(next);
      navigate(search ? `/?${search}` : '/', {
        replace: options?.replace ?? false,
      });
    },
    [navigate],
  );

  const handleShellSearchText = useCallback(
    (text: string) => {
      const next = parseFilterQuery(libraryQuery);
      next.text = text;
      next.page = DEFAULT_PAGE;
      applyLibraryQuery(next, { replace: true });
    },
    [libraryQuery, applyLibraryQuery],
  );

  const goLibrary = useCallback(
    (watchlist: boolean) => {
      const params = new URLSearchParams(route.pathname === '/' ? route.search : '');
      if (watchlist) {
        params.set('watchlist', 'true');
      } else {
        params.delete('watchlist');
      }
      if (params.has('page')) {
        params.set('page', '1');
      }
      const search = params.toString();
      navigate(search ? `/?${search}` : '/', {});
    },
    [route.pathname, route.search, navigate],
  );

  const openMovie = useCallback(
    (id: string | number, options?: { playerIntent?: boolean }) => {
      const current = `${route.pathname}${route.search}`;
      const state: { libraryReturnUrl: string; playerIntent?: true } = {
        libraryReturnUrl: current,
      };
      if (options?.playerIntent) {
        state.playerIntent = true;
      }
      navigate(`/movies/${String(id)}`, { state });
    },
    [route.pathname, route.search, navigate],
  );

  const detailsReturnTarget = isDetails ? (currentReturnUrl() ?? '/') : null;

  const handleDetailsBack = useCallback(
    (event: React.MouseEvent) => {
      event.preventDefault();
      navigate(detailsReturnTarget ?? '/', {});
    },
    [detailsReturnTarget, navigate],
  );

  const handleDetailsSearchSubmit = useCallback(
    (text: string) => {
      const base = libraryReturnSearch(readReturnUrl(window.history.state));
      const next = parseFilterQuery(base);
      next.text = text;
      next.page = DEFAULT_PAGE;
      const search = toUrlQuery(next);
      navigate(search ? `/?${search}` : '/', {});
    },
    [navigate],
  );

  const headerSearchId = useMemo(
    () => shellSearchId(isDetails ? 'details' : 'library'),
    [isDetails],
  );
  const detailsReturnSearch = isDetails
    ? libraryReturnSearch(readReturnUrl(window.history.state))
    : '';
  const detailsDraft = detailsReturnSearch ? parseFilterQuery(detailsReturnSearch).text : '';

  return (
    <AmbientProvider>
      <div className="lamu-app">
        <div className="lamu-shell">
          <div className="lamu-frame">
            <header className="lamu-shell-header">
              <a
                className="lamu-wordmark"
                href="/"
                onClick={(event) => {
                  event.preventDefault();
                  goLibrary(false);
                }}
              >
                LamuFlix
              </a>
              <nav className="lamu-shell-nav" aria-label="Primary">
                <a
                  className="lamu-nav-tab"
                  href="/"
                  aria-current={isDetails || isForYou || watchlistActive ? undefined : 'page'}
                  onClick={(event) => {
                    event.preventDefault();
                    goLibrary(false);
                  }}
                >
                  Library
                </a>
                <button
                  type="button"
                  className="lamu-nav-tab"
                  aria-pressed={isForYou ? false : watchlistActive}
                  onClick={() => goLibrary(isForYou ? true : !watchlistActive)}
                >
                  Watchlist
                </button>
                <a
                  className="lamu-nav-tab"
                  href="/for-you"
                  aria-current={isForYou ? 'page' : undefined}
                  onClick={(event) => {
                    event.preventDefault();
                    navigate('/for-you', {});
                  }}
                >
                  For You
                </a>
                {isDetails ? (
                  <DetailsSearch
                    id={headerSearchId}
                    initialText={detailsDraft}
                    onSubmit={handleDetailsSearchSubmit}
                  />
                ) : null}
              </nav>
              <button
                ref={bulkImportButtonRef}
                type="button"
                className="lamu-pill-button"
                onClick={() => setImportOpen(true)}
              >
                <RetryIcon size={18} />
                Bulk import
              </button>
            </header>
            {!isDetails && !isForYou ? (
              <div className="lamu-query">
                <ShellSearch
                  id={headerSearchId}
                  initialText={libraryQuery.text}
                  routeKey={route.routeKey}
                  onCommittedText={handleShellSearchText}
                />
              </div>
            ) : null}
            {isDetails && movieId !== null ? (
              <main>
                <DetailsBackLink href={detailsReturnTarget ?? '/'} onClick={handleDetailsBack} />
                <MovieDetailsView movieId={movieId} enrichmentStatus={enrichmentStatus} />
              </main>
            ) : isForYou ? (
              <main>
                <ForYouPage onOpenMovie={openMovie} />
              </main>
            ) : (
              <main>
                <LibraryErrorBoundary>
                  <LibraryView
                    inWatchlist={watchlistActive}
                    query={libraryQuery}
                    routeKey={route.routeKey}
                    routePath={route.pathname}
                    onQueryChange={applyLibraryQuery}
                    onOpenMovie={openMovie}
                  />
                </LibraryErrorBoundary>
              </main>
            )}
          </div>
        </div>
        <BulkImportDialog open={importOpen} onClose={closeImport} />
      </div>
    </AmbientProvider>
  );
}
