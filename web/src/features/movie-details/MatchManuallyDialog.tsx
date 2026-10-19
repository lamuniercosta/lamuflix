import { useId, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import type { QueryClient } from '@tanstack/react-query';
import { AlertTriangleIcon } from '../../ui/icons';
import { extractImdbId } from './imdbMatch';
import { MatchImdbIdError, useMatchImdbId, type ActorId } from './movieDetailsQueries';

const INVALID_IMDB_INPUT_MESSAGE = 'Enter an IMDb id like tt0083658, or an IMDb title URL.';

type MatchManuallyDialogProps = {
  movieId: ActorId;
  libraryClient: QueryClient;
  onClose: () => void;
};

function focusableElements(root: HTMLElement): HTMLElement[] {
  return [
    ...root.querySelectorAll<HTMLElement>('button, input, select, textarea, a[href], [tabindex]'),
  ].filter((element) => !element.hasAttribute('disabled') && element.tabIndex !== -1);
}

function matchErrorMessage(error: unknown): string {
  if (error instanceof MatchImdbIdError) {
    if (error.status === 404) return 'This movie is no longer in the library.';
    if (error.status === 400) return "That IMDb id isn't valid.";
  }
  return "Couldn't match this movie. Try again later.";
}

export default function MatchManuallyDialog({
  movieId,
  libraryClient,
  onClose,
}: MatchManuallyDialogProps) {
  const titleId = useId();
  const inputId = useId();
  const hintId = useId();
  const errorId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const alertRef = useRef<HTMLParagraphElement>(null);
  const [value, setValue] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const matchImdbId = useMatchImdbId(movieId, libraryClient);
  const busy = matchImdbId.isPending;

  function reportError(message: string) {
    setErrorMessage(message);
    queueMicrotask(() => alertRef.current?.focus());
  }

  function trapFocus(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.preventDefault();
      onClose();
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

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;

    const imdbId = extractImdbId(value);
    if (imdbId === null) {
      reportError(INVALID_IMDB_INPUT_MESSAGE);
      return;
    }

    setErrorMessage(null);
    try {
      await matchImdbId.mutateAsync(imdbId);
      onClose();
    } catch (error) {
      reportError(matchErrorMessage(error));
    }
  }

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
          Match manually
        </h2>
        <form onSubmit={onSubmit}>
          <label htmlFor={inputId}>IMDb id or URL</label>
          <input
            id={inputId}
            className="lamu-field-input"
            name="imdbId"
            value={value}
            placeholder="tt0083658"
            autoFocus
            readOnly={busy}
            aria-invalid={errorMessage != null}
            aria-describedby={errorMessage ? `${hintId} ${errorId}` : hintId}
            onChange={(event) => setValue(event.target.value)}
          />
          <p id={hintId} className="lamu-dialog-hint">
            Paste the IMDb id (tt followed by digits) or a title URL from imdb.com.
          </p>
          {errorMessage ? (
            <p ref={alertRef} id={errorId} role="alert" tabIndex={-1} className="lamu-dialog-alert">
              <AlertTriangleIcon size={18} />
              {errorMessage}
            </p>
          ) : null}
          <div className="lamu-dialog-actions">
            <button type="button" className="lamu-action-quiet" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="lamu-action-primary" disabled={busy}>
              Match
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
