import { useId, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import {
  GENERIC_IMPORT_ERROR,
  ImportFieldError,
  ImportGenericError,
  useImportMovieMutation,
} from './libraryQueries';

type ImportDialogProps = {
  open: boolean;
  onClose: () => void;
  onImported: (id: string, title: string) => void;
};

function focusableElements(root: HTMLElement): HTMLElement[] {
  return [
    ...root.querySelectorAll<HTMLElement>('button, input, select, textarea, a[href], [tabindex]'),
  ].filter((element) => !element.hasAttribute('disabled') && element.tabIndex !== -1);
}

export default function ImportDialog({ open, onClose, onImported }: ImportDialogProps) {
  const titleId = useId();
  const inputId = useId();
  const errorId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const alertRef = useRef<HTMLParagraphElement>(null);
  const [folderPath, setFolderPath] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const importMovie = useImportMovieMutation();

  if (!open) {
    return null;
  }

  function handleClose() {
    setFolderPath('');
    setErrorMessage(null);
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

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);

    try {
      const imported = await importMovie.mutateAsync(folderPath);
      onImported(imported.id, imported.title);
      handleClose();
    } catch (error) {
      const message =
        error instanceof ImportFieldError || error instanceof ImportGenericError
          ? error.message
          : GENERIC_IMPORT_ERROR;
      setErrorMessage(message);
      queueMicrotask(() => alertRef.current?.focus());
    }
  }

  const busy = importMovie.isPending;

  return (
    <div
      ref={dialogRef}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      onKeyDown={trapFocus}
    >
      <h2 id={titleId}>Import a movie folder</h2>
      <form onSubmit={onSubmit}>
        <label htmlFor={inputId}>Folder path</label>
        <input
          id={inputId}
          name="folderPath"
          value={folderPath}
          autoFocus
          readOnly={busy}
          aria-invalid={errorMessage != null}
          aria-describedby={errorMessage ? errorId : undefined}
          onChange={(event) => setFolderPath(event.target.value)}
        />
        {errorMessage ? (
          <p ref={alertRef} id={errorId} role="alert" tabIndex={-1}>
            {errorMessage}
          </p>
        ) : null}
        <button type="button" onClick={handleClose}>
          Cancel
        </button>
        <button type="submit" disabled={busy}>
          Import
        </button>
      </form>
    </div>
  );
}
