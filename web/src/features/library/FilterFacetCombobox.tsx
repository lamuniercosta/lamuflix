import {
  useEffect,
  useRef,
  useState,
  type FocusEvent,
  type KeyboardEvent,
  type ReactNode,
} from 'react';

export interface FacetOption {
  id: string | number;
  name: string;
  count?: string | number | null;
}

export type FacetComboboxKind = 'combobox' | 'suggestions' | 'filter';

export interface FilterFacetComboboxProps {
  kind: FacetComboboxKind;
  inputId: string;
  label: string;
  placeholder: string;
  ariaLabel?: string;
  listId?: string;
  listLabel?: string;
  optionId?: (index: number) => string;
  /** Candidate options, already ordered by the shell; the draft narrows them. */
  options: readonly FacetOption[];
  limit?: number;
  emptyMessage?: string;
  status?: ReactNode;
  onSelect: (option: FacetOption) => void;
  /** Reported only from interaction handlers; the shell may mirror it. */
  onDraftChange?: (draft: string) => void;
  focusSignal?: number;
}

/**
 * Owns one facet's local interaction state: the search draft, the active option
 * index and the input ref. Every mounted instance keeps that state to itself, so
 * the cast, collection and director facets never share a draft or a highlight.
 * Filtering happens here from the drafts; the shell supplies ordered candidates
 * and receives selections. Nothing here touches history or the URL.
 */
export function FilterFacetCombobox({
  kind,
  inputId,
  label,
  placeholder,
  ariaLabel,
  listId,
  listLabel,
  optionId,
  options,
  limit,
  emptyMessage,
  status,
  onSelect,
  onDraftChange,
  focusSignal,
}: FilterFacetComboboxProps) {
  const [draft, setDraft] = useState('');
  const [activeIndex, setActiveIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (focusSignal === undefined) {
      return;
    }
    inputRef.current?.focus();
  }, [focusSignal]);

  const reset = () => {
    setDraft('');
    setActiveIndex(-1);
  };

  const commit = (option: FacetOption) => {
    onSelect(option);
    reset();
    inputRef.current?.focus();
  };

  const needle = draft.trim().toLowerCase();
  const matched =
    needle === ''
      ? []
      : options.filter((option) =>
          String(option.name ?? '')
            .toLowerCase()
            .includes(needle),
        );
  const visible = limit === undefined ? matched : matched.slice(0, limit);
  const draftOpen = needle !== '' && kind !== 'filter';
  const optionsOpen = kind === 'combobox' && visible.length > 0;
  const activeOptionId =
    optionsOpen && optionId && activeIndex >= 0 && activeIndex < visible.length
      ? optionId(activeIndex)
      : undefined;

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (kind !== 'combobox' || visible.length === 0) {
      return;
    }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      setActiveIndex((current) => {
        if (current < 0) {
          return 0;
        }
        if (event.key === 'ArrowDown') {
          return Math.min(current + 1, visible.length - 1);
        }
        return Math.max(current - 1, 0);
      });
      return;
    }
    if (event.key === 'Enter') {
      if (activeOptionId === undefined) {
        return;
      }
      event.preventDefault();
      const option = visible[activeIndex];
      if (option) {
        commit(option);
      }
      return;
    }
    if (event.key === 'Escape') {
      event.preventDefault();
      reset();
      inputRef.current?.focus();
    }
  };

  const onBlur = (event: FocusEvent<HTMLInputElement>) => {
    if (kind !== 'combobox') {
      return;
    }
    const group = event.currentTarget.closest('fieldset');
    if (group && event.relatedTarget instanceof Node && group.contains(event.relatedTarget)) {
      return;
    }
    reset();
  };

  const input = (
    <input
      ref={inputRef}
      id={inputId}
      type="search"
      role={kind === 'combobox' ? 'combobox' : undefined}
      aria-label={ariaLabel}
      className="lamu-filter-find"
      placeholder={placeholder}
      autoComplete="off"
      aria-autocomplete={kind === 'combobox' ? 'list' : undefined}
      aria-expanded={kind === 'combobox' ? optionsOpen : undefined}
      aria-controls={kind === 'filter' ? undefined : draftOpen && optionsOpen ? listId : undefined}
      aria-activedescendant={activeOptionId}
      value={draft}
      onChange={(event) => {
        const value = event.target.value;
        setDraft(value);
        setActiveIndex(-1);
        onDraftChange?.(value);
      }}
      onKeyDown={onKeyDown}
      onBlur={onBlur}
    />
  );

  if (kind === 'filter') {
    return input;
  }

  return (
    <>
      <label htmlFor={inputId} className="sr-only">
        {label}
      </label>
      {input}
      <div id={kind === 'suggestions' ? listId : undefined} aria-live="polite">
        {status}
        {kind === 'suggestions' ? (
          needle === '' ? null : visible.length > 0 ? (
            <ul className="lamu-filter-options" aria-label={listLabel}>
              {visible.map((option) => (
                <li key={String(option.id)}>
                  <button
                    type="button"
                    className="lamu-filter-option"
                    onClick={() => commit(option)}
                  >
                    {option.name}
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="lamu-filter-note">{emptyMessage}</p>
          )
        ) : optionsOpen ? (
          <ul id={listId} role="listbox" className="lamu-filter-options">
            {visible.map((option, index) => (
              <li
                key={String(option.id)}
                id={optionId ? optionId(index) : undefined}
                role="option"
                aria-selected={activeIndex === index}
                className={
                  activeIndex === index
                    ? 'lamu-filter-option lamu-filter-option-active'
                    : 'lamu-filter-option'
                }
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => commit(option)}
              >
                <span className="lamu-filter-row-name">{option.name}</span>
                {option.count === undefined || option.count === null ? null : (
                  <>
                    <span className="lamu-filter-row-count" aria-hidden="true">
                      {option.count}
                    </span>
                    <span className="sr-only">, {option.count} movies</span>
                  </>
                )}
              </li>
            ))}
          </ul>
        ) : draftOpen && emptyMessage !== undefined ? (
          <p className="lamu-filter-note">{emptyMessage}</p>
        ) : null}
      </div>
    </>
  );
}

export default FilterFacetCombobox;
