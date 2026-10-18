export interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  setPage: (page: number) => void;
}

export default function Pagination({ page, pageSize, totalCount, setPage }: PaginationProps) {
  const safePageSize = Math.max(1, pageSize);
  const safeTotal = Math.max(0, totalCount);
  const currentPage = Math.max(1, page);
  const totalPages = Math.max(1, Math.ceil(safeTotal / safePageSize));
  const isPastEnd = safeTotal > 0 && currentPage > totalPages;
  const isFirst = currentPage <= 1;
  const isLast = currentPage >= totalPages;

  const start = safeTotal === 0 ? 0 : (currentPage - 1) * safePageSize + 1;
  const end = Math.min(currentPage * safePageSize, safeTotal);

  return (
    <nav className="pagination" aria-label="Pagination">
      <p className="pagination-summary" role="status" aria-live="polite">
        <span className="sr-only">{`Page ${currentPage} of ${totalPages}. `}</span>
        <span>{`Showing ${start}–${end} of ${safeTotal}`}</span>
      </p>

      <div className="pagination-buttons">
        <button
          type="button"
          className="lamu-pill-button pagination-previous"
          onClick={() => {
            if (!isFirst) {
              setPage(currentPage - 1);
            }
          }}
          disabled={isFirst}
          aria-disabled={isFirst}
        >
          Previous
        </button>
        <button
          type="button"
          className="lamu-pill-button pagination-next"
          onClick={() => {
            if (!isLast) {
              setPage(currentPage + 1);
            }
          }}
          disabled={isLast}
          aria-disabled={isLast}
        >
          Next
        </button>
      </div>

      {isPastEnd ? (
        <section className="pagination-past-end">
          <p>This page is past the end of the results.</p>
          <button type="button" className="lamu-pill-button" onClick={() => setPage(1)}>
            Go to page 1
          </button>
        </section>
      ) : null}
    </nav>
  );
}
