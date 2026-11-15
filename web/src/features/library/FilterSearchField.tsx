import { useRef, useState } from 'react';
import { CloseIcon } from '../../ui/icons';
import { DEBOUNCE_MS } from './filterQuery';
import { useDebouncedSearch } from './useDebouncedSearch';

export interface FilterSearchFieldProps {
  inputId: string;
  text: string;
  onCommit: (text: string) => void;
}

export function FilterSearchField({ inputId, text, onCommit }: FilterSearchFieldProps) {
  const [draft, setDraft] = useState(text);
  const [prevText, setPrevText] = useState(text);
  if (text !== prevText) {
    setPrevText(text);
    setDraft(text);
  }

  const commitDraft = useDebouncedSearch((value: string) => {
    onCommit(value);
  }, DEBOUNCE_MS);

  const inputRef = useRef<HTMLInputElement>(null);

  const clear = () => {
    commitDraft.cancel();
    setDraft('');
    onCommit('');
    inputRef.current?.focus();
  };

  return (
    <div className="lamu-filter-group">
      <label htmlFor={inputId} className="lamu-filter-sublabel">
        Search titles
      </label>
      <div className="lamu-filter-search">
        <input
          ref={inputRef}
          id={inputId}
          type="search"
          className="lamu-field-input"
          placeholder="Search titles"
          value={draft}
          onChange={(event) => {
            setDraft(event.target.value);
            commitDraft.schedule(event.target.value);
          }}
        />
        {draft === '' ? null : (
          <button
            type="button"
            className="lamu-filter-search-clear"
            aria-label="Clear search"
            onClick={clear}
          >
            <CloseIcon size={14} />
          </button>
        )}
      </div>
    </div>
  );
}

export default FilterSearchField;
