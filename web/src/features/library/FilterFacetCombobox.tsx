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

function nextIndex(key: string, current: number, length: number): number {
  if (current < 0) {
    return 0;
  }
  if (key === 'ArrowDown') {
    return Math.min(current + 1, length - 1);
  }
  return Math.max(current - 1, 0);
}

function SuggestionList({
  visible,
  listLabel,
  onPick,
}: {
  visible: readonly FacetOption[];
  listLabel?: string;
  onPick: (option: FacetOption) => void;
}) {
  return (
    <ul className="lamu-filter-options" aria-label={listLabel}>
      {visible.map((option) => (
        <li key={String(option.id)}>
          <button type="button" className="lamu-filter-option" onClick={() => onPick(option)}>
            {option.name}
          </button>
        </li>
      ))}
    </ul>
  );
}

function OptionCount({ count }: { count: string | number | null | undefined }) {
  if (count === undefined || count === null) {
    return null;
  }
  return (
    <>
      <span className="lamu-filter-row-count" aria-hidden="true">
        {count}
      </span>
      <span className="sr-only">, {count} movies</span>
    </>
  );
}

function OptionList({
  visible,
  listId,
  optionId,
  activeIndex,
  onPick,
}: {
  visible: readonly FacetOption[];
  listId?: string;
  optionId?: (index: number) => string;
  activeIndex: number;
  onPick: (option: FacetOption) => void;
}) {
  return (
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
          onClick={() => onPick(option)}
        >
          <span className="lamu-filter-row-name">{option.name}</span>
          <OptionCount count={option.count} />
        </li>
      ))}
    </ul>
  );
}

interface FacetBodyInput {
  kind: FacetComboboxKind;
  needle: string;
  visible: readonly FacetOption[];
  optionsOpen: boolean;
  draftOpen: boolean;
  note: ReactNode;
  listId?: string;
  listLabel?: string;
  optionId?: (index: number) => string;
  activeIndex: number;
  onPick: (option: FacetOption) => void;
}

function FacetBody(props: FacetBodyInput): ReactNode {
  const { kind, needle, visible, optionsOpen, draftOpen, note } = props;
  if (kind === 'suggestions') {
    if (needle === '') {
      return null;
    }
    if (visible.length === 0) {
      return note;
    }
    return <SuggestionList visible={visible} listLabel={props.listLabel} onPick={props.onPick} />;
  }
  if (optionsOpen) {
    return (
      <OptionList
        visible={visible}
        listId={props.listId}
        optionId={props.optionId}
        activeIndex={props.activeIndex}
        onPick={props.onPick}
      />
    );
  }
  return draftOpen ? note : null;
}

function matchOptions(options: readonly FacetOption[], needle: string): FacetOption[] {
  if (needle === '') {
    return [];
  }
  return options.filter((option) =>
    String(option.name ?? '')
      .toLowerCase()
      .includes(needle),
  );
}

function activeOptionIdOf(
  open: boolean,
  optionId: ((index: number) => string) | undefined,
  activeIndex: number,
  length: number,
): string | undefined {
  if (!open || optionId === undefined || activeIndex < 0 || activeIndex >= length) {
    return undefined;
  }
  return optionId(activeIndex);
}

interface InputAria {
  role?: 'combobox';
  'aria-autocomplete'?: 'list';
  'aria-expanded'?: boolean;
  'aria-controls'?: string;
}

function listControls(
  draftOpen: boolean,
  optionsOpen: boolean,
  listId?: string,
): string | undefined {
  if (!draftOpen || !optionsOpen) {
    return undefined;
  }
  return listId;
}

function inputAria(
  kind: FacetComboboxKind,
  optionsOpen: boolean,
  draftOpen: boolean,
  listId?: string,
): InputAria {
  if (kind !== 'combobox') {
    return {};
  }
  return {
    role: 'combobox',
    'aria-autocomplete': 'list',
    'aria-expanded': optionsOpen,
    'aria-controls': listControls(draftOpen, optionsOpen, listId),
  };
}

function keepsDraft(kind: FacetComboboxKind, event: FocusEvent<HTMLInputElement>): boolean {
  if (kind !== 'combobox') {
    return false;
  }
  const group = event.currentTarget.closest('fieldset');
  return Boolean(
    group && event.relatedTarget instanceof Node && group.contains(event.relatedTarget),
  );
}

interface FacetView {
  needle: string;
  visible: FacetOption[];
  draftOpen: boolean;
  optionsOpen: boolean;
}

function facetView(
  kind: FacetComboboxKind,
  draft: string,
  options: readonly FacetOption[],
  limit: number | undefined,
): FacetView {
  const needle = draft.trim().toLowerCase();
  const matched = matchOptions(options, needle);
  const visible = limit === undefined ? matched : matched.slice(0, limit);
  return {
    needle,
    visible,
    draftOpen: needle !== '' && kind !== 'filter',
    optionsOpen: kind === 'combobox' && visible.length > 0,
  };
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

  const view = facetView(kind, draft, options, limit);
  const { needle, visible, draftOpen, optionsOpen } = view;
  const activeOptionId = activeOptionIdOf(optionsOpen, optionId, activeIndex, visible.length);
  const onEnter = () => {
    if (activeOptionId === undefined) {
      return;
    }
    const option = visible[activeIndex];
    if (option) {
      commit(option);
    }
  };

  const keyHandlers: Record<string, (() => void) | undefined> = {
    ArrowDown: () => setActiveIndex((current) => nextIndex('ArrowDown', current, visible.length)),
    ArrowUp: () => setActiveIndex((current) => nextIndex('ArrowUp', current, visible.length)),
    Enter: onEnter,
    Escape: () => {
      reset();
      inputRef.current?.focus();
    },
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (kind !== 'combobox' || visible.length === 0) {
      return;
    }
    const handler = keyHandlers[event.key];
    if (handler === undefined) {
      return;
    }
    event.preventDefault();
    handler();
  };

  const onBlur = (event: FocusEvent<HTMLInputElement>) => {
    if (keepsDraft(kind, event)) {
      return;
    }
    if (kind === 'combobox') {
      reset();
    }
  };

  const note =
    emptyMessage === undefined ? null : <p className="lamu-filter-note">{emptyMessage}</p>;
  const aria = inputAria(kind, optionsOpen, draftOpen, listId);
  const input = (
    <input
      ref={inputRef}
      id={inputId}
      type="search"
      aria-label={ariaLabel}
      className="lamu-filter-find"
      placeholder={placeholder}
      autoComplete="off"
      {...aria}
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
        <FacetBody
          kind={kind}
          needle={needle}
          visible={visible}
          optionsOpen={optionsOpen}
          draftOpen={draftOpen}
          note={note}
          listId={listId}
          listLabel={listLabel}
          optionId={optionId}
          activeIndex={activeIndex}
          onPick={commit}
        />
      </div>
    </>
  );
}

export default FilterFacetCombobox;
