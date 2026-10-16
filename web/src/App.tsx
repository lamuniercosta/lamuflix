import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { components } from './api/types';
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
import MovieDetailsView from './features/movie-details/MovieDetailsView';
import ImportDialog from './library/ImportDialog';
import LibraryErrorBoundary from './library/LibraryErrorBoundary';
import { armImportPlaceholder, pendingRow } from './library/libraryQueries';
import LibraryView from './library/LibraryView';

type MovieSummary = components['schemas']['MovieSummaryResponse'];

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
    <div className="lamu-shell-search">
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
    <SearchField
      id={id}
      value={draft}
      onChange={(value) => {
        setDraft(value);
        commit.schedule(value);
      }}
    />
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

export default function App() {
  const [importOpen, setImportOpen] = useState(false);
  const [importedRow, setImportedRow] = useState<MovieSummary | null>(null);
  const importFolderButtonRef = useRef<HTMLButtonElement>(null);
  const { route, navigate } = useAppNavigation();

  const movieId = parseMovieId(route.pathname);
  const isDetails = movieId !== null;
  const libraryQuery = useMemo<FilterQuery>(() => parseFilterQuery(route.search), [route.search]);
  const watchlistActive = useMemo(
    () => new URLSearchParams(route.search).get('watchlist') === 'true',
    [route.search],
  );

  function closeImport() {
    setImportOpen(false);
    importFolderButtonRef.current?.focus();
  }

  function prependImportedMovie(id: string, title: string) {
    setImportedRow(pendingRow(id, title));
    armImportPlaceholder(() => setImportedRow(null));
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
      const params = new URLSearchParams(route.search);
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
    [route.search, navigate],
  );

  const openMovie = useCallback(
    (id: string | number) => {
      const current = `${route.pathname}${route.search}`;
      navigate(`/movies/${String(id)}`, {
        state: { libraryReturnUrl: current },
      });
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
    <div className="lamu-app">
      <header className="lamu-shell-header">
        <div className="lamu-shell-bar">
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
              href="/"
              onClick={(event) => {
                event.preventDefault();
                goLibrary(false);
              }}
            >
              Library
            </a>
            <button
              type="button"
              aria-pressed={watchlistActive}
              onClick={() => goLibrary(!watchlistActive)}
            >
              Watchlist
            </button>
            <button ref={importFolderButtonRef} type="button" onClick={() => setImportOpen(true)}>
              Import folder
            </button>
          </nav>
          {isDetails ? (
            <DetailsSearch
              id={headerSearchId}
              initialText={detailsDraft}
              onSubmit={handleDetailsSearchSubmit}
            />
          ) : (
            <ShellSearch
              id={headerSearchId}
              initialText={libraryQuery.text}
              routeKey={route.routeKey}
              onCommittedText={handleShellSearchText}
            />
          )}
        </div>
      </header>
      {isDetails && movieId !== null ? (
        <main>
          <p>
            <a href={detailsReturnTarget ?? '/'} onClick={handleDetailsBack}>
              Back to library
            </a>
          </p>
          <MovieDetailsView movieId={movieId} />
        </main>
      ) : (
        <main>
          <LibraryErrorBoundary>
            <LibraryView
              leadingItem={importedRow}
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
      <ImportDialog open={importOpen} onClose={closeImport} onImported={prependImportedMovie} />
    </div>
  );
}
