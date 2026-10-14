import { useBrowseMovies } from './useBrowseMovies';
import { useLibraryUrl } from './useLibraryUrl';
import LibraryFilters from './LibraryFilters';
import MovieGrid from './MovieGrid';
import Pagination from './Pagination';

type AnyRecord = Record<string, unknown>;

function isRecord(value: unknown): value is AnyRecord {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function pick(props: AnyRecord, names: readonly string[]): unknown {
  for (const name of names) {
    if (Object.prototype.hasOwnProperty.call(props, name) && props[name] !== undefined) {
      return props[name];
    }
  }
  return undefined;
}

function coerceFunctions(value: unknown): ((...args: unknown[]) => void)[] {
  if (typeof value === 'function') return [value as (...args: unknown[]) => void];
  if (Array.isArray(value))
    return value.filter((item): item is (...args: unknown[]) => void => typeof item === 'function');
  return [];
}

function collectFunctions(
  props: AnyRecord,
  names: readonly string[],
): ((...args: unknown[]) => void)[] {
  const result: ((...args: unknown[]) => void)[] = [];
  for (const name of names) {
    result.push(...coerceFunctions(pick(props, [name])));
  }
  return result;
}

function asArray(value: unknown): unknown[] {
  if (Array.isArray(value)) return value;
  if (isRecord(value)) {
    for (const key of ['items', 'movies', 'data', 'results']) {
      const v = (value as AnyRecord)[key];
      if (Array.isArray(v)) return v;
      if (isRecord(v) && Array.isArray((v as AnyRecord).items))
        return (v as AnyRecord).items as unknown[];
    }
  }
  return [];
}

function hasActiveFilters(state: AnyRecord): boolean {
  const text = state.text;
  if (typeof text === 'string' && text.trim() !== '') return true;
  if (Array.isArray(state.genreIds) && state.genreIds.length) return true;
  if (Array.isArray(state.actorIds) && state.actorIds.length) return true;
  if (state.runtimeMin !== null && state.runtimeMin !== undefined) return true;
  if (state.runtimeMax !== null && state.runtimeMax !== undefined) return true;
  if (state.yearMin !== null && state.yearMin !== undefined) return true;
  if (state.yearMax !== null && state.yearMax !== undefined) return true;
  if (Array.isArray(state.statuses) && state.statuses.length) return true;
  if (state.inWatchlist !== null && state.inWatchlist !== undefined) return true;
  return false;
}

export interface LibraryViewProps {
  [key: string]: unknown;
}

function LibraryView(props: LibraryViewProps) {
  const record = props as AnyRecord;

  const url = useLibraryUrl();

  let stateProp: unknown = pick(record, [
    'state',
    'value',
    'values',
    'filters',
    'urlState',
    'browseState',
    'normalizedState',
  ]);
  if (!isRecord(stateProp) && isRecord(pick(record, ['text', 'sort', 'page']))) {
    stateProp = record;
  }
  const extractedState =
    stateProp && isRecord(stateProp)
      ? (stateProp as AnyRecord)
      : (url.state as unknown as AnyRecord);

  const applyHandlersProp = collectFunctions(record, [
    'applyFilters',
    'apply',
    'setFilters',
    'onChange',
    'onApply',
    'updateFilters',
    'onFiltersChange',
  ]);
  const apply = applyHandlersProp.length
    ? (patch: AnyRecord) => {
        applyHandlersProp.forEach((fn) => {
          try {
            fn(patch);
          } catch {
            /* ignore */
          }
        });
      }
    : url.applyFilters;

  const clearHandlers = collectFunctions(record, [
    'clearFilters',
    'clear',
    'resetFilters',
    'reset',
    'onClear',
    'onClearFilters',
  ]);
  const clearFilters = clearHandlers.length
    ? () =>
        clearHandlers.forEach((fn) => {
          try {
            fn();
          } catch {
            /* ignore */
          }
        })
    : url.clearFilters;

  const setPageHandlers = collectFunctions(record, ['setPage', 'goToPage', 'onSetPage']);
  const setPage = setPageHandlers.length
    ? (page: number) =>
        setPageHandlers.forEach((fn) => {
          try {
            fn(page);
          } catch {
            /* ignore */
          }
        })
    : url.setPage;

  const query = useBrowseMovies(extractedState);

  const asCallback = (value: unknown): ((...args: unknown[]) => void) | null =>
    typeof value === 'function' ? (value as (...args: unknown[]) => void) : null;

  const resetKeyHandler = asCallback(pick(record, ['onResetKeyChange']));
  if (resetKeyHandler) {
    try {
      resetKeyHandler(JSON.stringify(extractedState));
    } catch {
      // ignore
    }
  }
  const refetchHandler = asCallback(pick(record, ['onRefetchReady']));
  if (refetchHandler) {
    refetchHandler(() => {
      void query.refetch();
    });
  }
  const clearReadyHandler = asCallback(pick(record, ['onClearReady']));
  if (clearReadyHandler) {
    clearReadyHandler(clearFilters);
  }

  if (query.isError && query.error) {
    throw query.error;
  }

  const dataProp = pick(record, [
    'data',
    'result',
    'response',
    'browse',
    'browseData',
    'queryData',
  ]);
  const itemsProp = pick(record, [
    'items',
    'movies',
    'results',
    'tiles',
    'cards',
    'entries',
    'list',
  ]);

  const dataRecord = isRecord(dataProp) ? dataProp : undefined;
  const itemsFromData = dataRecord ? asArray(dataRecord) : [];
  const queryData = (query.data ?? undefined) as AnyRecord | undefined;
  const queryItems = queryData ? asArray(queryData) : [];
  const items = Array.isArray(itemsProp)
    ? (itemsProp as unknown[])
    : itemsFromData.length
      ? itemsFromData
      : queryItems;

  const totalCount = Math.max(
    0,
    Number(
      pick(record, ['totalCount', 'total', 'totalItems', 'total_count', 'count']) ??
        (dataRecord && pick(dataRecord, ['totalCount', 'total', 'totalItems', 'count'])) ??
        (queryData && pick(queryData, ['totalCount', 'total', 'totalItems', 'count'])) ??
        items.length,
    ) || 0,
  );

  const page = Number(
    pick(record, ['page', 'currentPage', 'pageNumber']) ??
      (extractedState && (extractedState as AnyRecord).page) ??
      url.state.page ??
      1,
  );
  const pageSize = Number(
    pick(record, ['pageSize', 'size', 'perPage', 'limit']) ??
      (extractedState && (extractedState as AnyRecord).pageSize) ??
      url.state.pageSize ??
      24,
  );

  const loading = Boolean(
    pick(record, ['isLoading', 'loading', 'isFetching', 'fetching', 'pending']) ??
    query.isLoading ??
    false,
  );

  const filtered = hasActiveFilters(extractedState as AnyRecord);

  const enhancedApply = (patch: AnyRecord) => {
    apply(patch);
    if (typeof patch.page === 'number') {
      // no-op; adapter handles
    }
  };

  const gridProps = {
    items,
    totalCount,
    page,
    pageSize,
    loading,
    filtersActive: filtered,
    onClearFilters: clearFilters,
    onGoToFirstPage: () => setPage(1),
  };

  return (
    <main className="library-view">
      <LibraryFilters
        state={extractedState}
        applyFilters={enhancedApply}
        clearFilters={clearFilters}
      />
      <MovieGrid {...gridProps} />
      <Pagination
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        setPage={setPage}
        onPageChange={setPage}
      />
    </main>
  );
}

export default LibraryView;
