type AnyRecord = Record<string, unknown>;

function pick(props: AnyRecord, names: readonly string[]): unknown {
  for (const name of names) {
    if (Object.prototype.hasOwnProperty.call(props, name) && props[name] !== undefined) {
      return props[name];
    }
  }
  return undefined;
}

function toNumber(value: unknown, fallback: number): number {
  if (value === null || value === undefined || value === '') return fallback;
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function coerceFunctions(value: unknown): ((...args: unknown[]) => void)[] {
  if (typeof value === 'function') return [value as (...args: unknown[]) => void];
  if (Array.isArray(value)) {
    return value.filter((item): item is (...args: unknown[]) => void => typeof item === 'function');
  }
  return [];
}

function collectFunctions(
  props: AnyRecord,
  names: readonly string[],
): ((...args: unknown[]) => void)[] {
  const result: ((...args: unknown[]) => void)[] = [];
  for (const name of names) {
    const value = pick(props, [name]);
    result.push(...coerceFunctions(value));
  }
  return result;
}

export interface PaginationProps {
  [key: string]: unknown;
}

export default function Pagination(props: PaginationProps) {
  const record = props as AnyRecord;

  const page = Math.max(
    1,
    toNumber(pick(record, ['page', 'currentPage', 'pageNumber', 'current']), 1),
  );
  const pageSize = Math.max(
    1,
    toNumber(pick(record, ['pageSize', 'size', 'limit', 'perPage', 'pageSize']), 24),
  );
  const totalCount = Math.max(
    0,
    toNumber(
      pick(record, ['totalCount', 'total', 'totalItems', 'total_count', 'count', 'resultCount']),
      0,
    ),
  );

  const totalPages = pageSize > 0 ? Math.max(1, Math.ceil(totalCount / pageSize)) : 1;
  const isPastEnd = page > totalPages && totalCount > 0;

  const genericSetters = collectFunctions(record, [
    'setPage',
    'goToPage',
    'onSetPage',
    'setPageNumber',
    'changePage',
    'onPageChange',
    'onPageChange',
    'pageChange',
    'handlePageChange',
  ]);
  const onPrevHandlers = collectFunctions(record, [
    'onPrevious',
    'onPrev',
    'previousPage',
    'goPrevious',
    'handlePrevious',
    'prev',
  ]);
  const onNextHandlers = collectFunctions(record, [
    'onNext',
    'nextPage',
    'goNext',
    'handleNext',
    'next',
  ]);
  const onFirstHandlers = collectFunctions(record, [
    'onGoToFirstPage',
    'goToFirstPage',
    'onFirstPage',
    'goToPageOne',
    'returnToFirstPage',
    'onReturnToFirstPage',
  ]);

  function callChange(target: number) {
    genericSetters.forEach((fn) => {
      try {
        fn(target);
      } catch {
        /* ignore */
      }
    });
  }

  const handlePrevious = () => {
    if (page <= 1) return;
    callChange(page - 1);
    onPrevHandlers.forEach((fn) => {
      try {
        fn(page - 1);
      } catch {
        /* ignore */
      }
    });
  };
  const handleNext = () => {
    if (page >= totalPages) return;
    callChange(page + 1);
    onNextHandlers.forEach((fn) => {
      try {
        fn(page + 1);
      } catch {
        /* ignore */
      }
    });
  };
  const handleFirst = () => {
    callChange(1);
    onFirstHandlers.forEach((fn) => {
      try {
        fn();
      } catch {
        /* ignore */
      }
    });
  };

  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  const announced = `Page ${page} of ${totalPages} \u2014 showing ${start}-${end} of ${totalCount}`;

  return (
    <nav className="pagination" aria-label="Pagination">
      <div aria-live="polite" role="status" className="pagination-status">
        <span className="pagination-page">
          Page {page} of {totalPages}
        </span>
        <span className="pagination-count">
          {start}-{end} of {totalCount} results
        </span>
      </div>
      <button
        type="button"
        className="pagination-previous"
        onClick={handlePrevious}
        disabled={page <= 1}
        aria-disabled={page <= 1}
      >
        Previous
      </button>
      <button
        type="button"
        className="pagination-next"
        onClick={handleNext}
        disabled={page >= totalPages}
        aria-disabled={page >= totalPages}
      >
        Next
      </button>
      {isPastEnd ? (
        <section className="pagination-past-end">
          <p>This page is past the end of the results.</p>
          <button type="button" onClick={handleFirst}>
            Go to page 1
          </button>
        </section>
      ) : null}
      <span className="sr-only" aria-live="polite">
        {announced}
      </span>
    </nav>
  );
}
