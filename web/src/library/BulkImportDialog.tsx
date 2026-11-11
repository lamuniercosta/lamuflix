import { useQueryClient } from '@tanstack/react-query';
import { useId, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { AlertTriangleIcon } from '../ui/icons';
import {
  BULK_IMPORT_REASON_TEXT,
  GENERIC_BULK_IMPORT_ERROR,
  invalidateBrowseMovies,
  invalidateImportSources,
  useFailedEnrichmentsQuery,
  useImportCandidatesQuery,
  useImportLibraryFoldersMutation,
  type BulkImportSummary,
} from './libraryQueries';

type BulkImportDialogProps = {
  open: boolean;
  onClose: () => void;
};

type BulkImportProgress = {
  done: number;
  total: number;
  folder: string;
};

function focusableElements(root: HTMLElement): HTMLElement[] {
  return [
    ...root.querySelectorAll<HTMLElement>('button, input, select, textarea, a[href], [tabindex]'),
  ].filter((element) => !element.hasAttribute('disabled') && element.tabIndex !== -1);
}

export default function BulkImportDialog({ open, onClose }: BulkImportDialogProps) {
  const titleId = useId();
  const statusId = useId();
  const errorId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const alertRef = useRef<HTMLParagraphElement>(null);
  const stopRequested = useRef(false);
  const queryClient = useQueryClient();
  const candidates = useImportCandidatesQuery(open);
  const failed = useFailedEnrichmentsQuery(open);
  const importLibrary = useImportLibraryFoldersMutation();
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set());
  const [retryFailed, setRetryFailed] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [summary, setSummary] = useState<BulkImportSummary | null>(null);
  const [progress, setProgress] = useState<BulkImportProgress | null>(null);
  const [running, setRunning] = useState(false);

  if (!open) {
    return null;
  }

  const folders = candidates.data?.newFolders ?? [];
  const failedItems = failed.data?.items ?? [];
  const selectedFolders = folders.filter((folder) => selected.has(folder));

  function handleClose() {
    stopRequested.current = true;
    setSelected(new Set());
    setRetryFailed(false);
    setErrorMessage(null);
    setSummary(null);
    setProgress(null);
    onClose();
  }

  function trapFocus(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault();
      handleClose();
      return;
    }

    if (event.key !== 'Tab' || !dialogRef.current) {
      return;
    }

    const nodes = focusableElements(dialogRef.current);
    if (nodes.length === 0) {
      return;
    }

    const first = nodes[0];
    const last = nodes[nodes.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  function toggleFolder(folder: string) {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(folder)) {
        next.delete(folder);
      } else {
        next.add(folder);
      }
      return next;
    });
  }

  function toggleSelectAll() {
    setSelected((current) =>
      folders.every((folder) => current.has(folder)) ? new Set() : new Set(folders),
    );
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);
    setSummary(null);

    const targets = selectedFolders;
    const totals = { imported: 0, skipped: 0, retried: 0 };
    const failures: BulkImportSummary['failures'] = [];
    stopRequested.current = false;
    setRunning(true);

    try {
      for (let index = 0; index < targets.length; index++) {
        if (stopRequested.current) {
          return;
        }

        setProgress({ done: index, total: targets.length, folder: targets[index] });
        const result = await importLibrary.mutateAsync({
          folders: [targets[index]],
          retryFailed: false,
        });
        totals.imported += Number(result.imported);
        totals.skipped += Number(result.skipped);
        totals.retried += Number(result.retried);
        failures.push(...result.failures);
        setProgress({ done: index + 1, total: targets.length, folder: targets[index] });
      }

      if (retryFailed) {
        const result = await importLibrary.mutateAsync({ folders: [], retryFailed: true });
        totals.imported += Number(result.imported);
        totals.skipped += Number(result.skipped);
        totals.retried += Number(result.retried);
        failures.push(...result.failures);
      }

      setSummary({
        imported: totals.imported,
        skipped: totals.skipped,
        retried: totals.retried,
        failures,
      });
    } catch {
      setErrorMessage(GENERIC_BULK_IMPORT_ERROR);
      queueMicrotask(() => alertRef.current?.focus());
      if (failures.length > 0 || totals.imported + totals.skipped + totals.retried > 0) {
        setSummary({
          imported: totals.imported,
          skipped: totals.skipped,
          retried: totals.retried,
          failures,
        });
      }
    } finally {
      invalidateBrowseMovies(queryClient);
      invalidateImportSources(queryClient);
      setRunning(false);
      setProgress(null);
    }
  }

  const busy = running;
  const inFlight = progress !== null && progress.done < progress.total;
  const nothingToDo = selectedFolders.length === 0 && !retryFailed;

  return (
    <div className="lamu-dialog-scrim">
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="lamu-glass lamu-dialog"
        onKeyDown={trapFocus}
      >
        <h2 id={titleId} className="lamu-section-heading">
          Bulk import
        </h2>
        <form onSubmit={onSubmit}>
          <div>
            <button
              type="button"
              className="lamu-action-quiet"
              onClick={toggleSelectAll}
              disabled={busy || folders.length === 0}
            >
              {folders.length > 0 && selectedFolders.length === folders.length
                ? 'Clear all'
                : 'Select all'}
            </button>
            {candidates.isError ? (
              <p role="alert" className="lamu-dialog-alert">
                <AlertTriangleIcon size={18} />
                Could not scan the library.{' '}
                <button
                  type="button"
                  className="lamu-action-quiet"
                  onClick={() => void candidates.refetch()}
                >
                  Retry scan
                </button>
              </p>
            ) : (
              <ul>
                {folders.map((folder) => (
                  <li key={folder}>
                    <label>
                      <input
                        type="checkbox"
                        checked={selected.has(folder)}
                        disabled={busy}
                        onChange={() => toggleFolder(folder)}
                      />
                      {folder}
                    </label>
                  </li>
                ))}
              </ul>
            )}
          </div>
          <fieldset>
            <legend>Failed enrichments</legend>
            {failed.isError ? (
              <p role="alert" className="lamu-dialog-alert">
                <AlertTriangleIcon size={18} />
                Could not load failed enrichments.{' '}
                <button
                  type="button"
                  className="lamu-action-quiet"
                  onClick={() => void failed.refetch()}
                >
                  Retry
                </button>
              </p>
            ) : failedItems.length === 0 ? (
              <p>No movies are waiting in Failed enrichment.</p>
            ) : (
              <>
                <p>{failedItems.length} movie(s) are waiting in Failed enrichment.</p>
                <label>
                  <input
                    type="checkbox"
                    checked={retryFailed}
                    disabled={busy}
                    onChange={(event) => setRetryFailed(event.target.checked)}
                  />
                  Retry failed enrichments
                </label>
              </>
            )}
          </fieldset>
          {errorMessage ? (
            <p ref={alertRef} id={errorId} role="alert" tabIndex={-1} className="lamu-dialog-alert">
              <AlertTriangleIcon size={18} />
              {errorMessage}
            </p>
          ) : null}
          {inFlight && progress !== null ? (
            <div
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={progress.total}
              aria-valuenow={progress.done}
              aria-label="Bulk import progress"
              className="lamu-dialog-progress"
            >
              Importing {progress.done + 1} of {progress.total}: {progress.folder}
            </div>
          ) : null}
          {summary ? (
            <div role="status" id={statusId}>
              <p>
                Imported {Number(summary.imported)} · Skipped {Number(summary.skipped)} · Retried{' '}
                {Number(summary.retried)}
              </p>
              {summary.failures.length > 0 ? (
                <ul>
                  {summary.failures.map((failure) => (
                    <li key={failure.folder}>
                      {failure.folder}:{' '}
                      {BULK_IMPORT_REASON_TEXT[failure.reason] ??
                        BULK_IMPORT_REASON_TEXT['import-failed']}
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          ) : null}
          <div className="lamu-dialog-actions">
            <button type="button" className="lamu-action-quiet" onClick={handleClose}>
              Cancel
            </button>
            <button type="submit" className="lamu-action-primary" disabled={busy || nothingToDo}>
              Import
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
