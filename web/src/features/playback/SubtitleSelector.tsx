import { useRef, type KeyboardEvent as ReactKeyboardEvent } from 'react';
import {
  SUBTITLES_OFF_COPY,
  SUBTITLES_RETRY_COPY,
  SUBTITLES_UNAVAILABLE_COPY,
  subtitleSelectLabel,
  subtitlesRetryLabel,
} from './subtitleCopy';
import type {
  SubtitleContentStatus,
  SubtitleListStatus,
  SubtitleTrack,
} from './useSubtitlePlayback';

export interface SubtitleSelectorProps {
  title: string;
  listStatus: SubtitleListStatus;
  contentStatus: SubtitleContentStatus;
  tracks: readonly SubtitleTrack[];
  selectedId: string | null;
  onSelect: (trackId: string | null) => void;
  onRetryList: () => void;
}

interface TrackChoice {
  id: string | null;
  label: string;
}

const OFF_CHOICE: TrackChoice = { id: null, label: SUBTITLES_OFF_COPY };

export default function SubtitleSelector({
  title,
  listStatus,
  contentStatus,
  tracks,
  selectedId,
  onSelect,
  onRetryList,
}: SubtitleSelectorProps) {
  const groupRef = useRef<HTMLDivElement | null>(null);

  if (listStatus === 'error') {
    return (
      <div className="subtitle-selector">
        <p className="subtitle-selector-status" role="status">
          {SUBTITLES_UNAVAILABLE_COPY}
        </p>
        <button
          type="button"
          className="lamu-action-quiet"
          aria-label={subtitlesRetryLabel(title)}
          onClick={onRetryList}
        >
          {SUBTITLES_RETRY_COPY}
        </button>
      </div>
    );
  }

  if (listStatus !== 'ready' || tracks.length === 0) return null;

  const choices: TrackChoice[] = [
    OFF_CHOICE,
    ...tracks.map((track) => ({ id: track.id, label: track.label })),
  ];
  const activeIndex = Math.max(
    0,
    choices.findIndex((choice) => choice.id === selectedId),
  );

  const focusChoice = (index: number) => {
    const next = groupRef.current?.querySelectorAll<HTMLElement>('[role="radio"]')[index];
    next?.focus();
  };

  const move = (index: number, delta: number) => {
    const nextIndex = (index + delta + choices.length) % choices.length;
    onSelect(choices[nextIndex].id);
    focusChoice(nextIndex);
  };

  const handleKeyDown = (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowRight':
        event.preventDefault();
        event.stopPropagation();
        move(index, 1);
        break;
      case 'ArrowUp':
      case 'ArrowLeft':
        event.preventDefault();
        event.stopPropagation();
        move(index, -1);
        break;
      case 'Home':
        event.preventDefault();
        event.stopPropagation();
        onSelect(choices[0].id);
        focusChoice(0);
        break;
      case 'End':
        event.preventDefault();
        event.stopPropagation();
        onSelect(choices[choices.length - 1].id);
        focusChoice(choices.length - 1);
        break;
      case ' ':
      case 'Enter':
        event.preventDefault();
        event.stopPropagation();
        onSelect(choices[index].id);
        break;
      default:
        break;
    }
  };

  return (
    <div className="subtitle-selector">
      <div
        ref={groupRef}
        className="subtitle-selector-list"
        role="radiogroup"
        aria-label={subtitleSelectLabel(title)}
      >
        {choices.map((choice, index) => {
          const checked = choice.id === selectedId || (index === 0 && selectedId === null);
          return (
            <button
              key={choice.id ?? 'off'}
              type="button"
              role="radio"
              aria-checked={checked}
              aria-label={choice.label}
              data-track-id={choice.id ?? ''}
              tabIndex={index === activeIndex ? 0 : -1}
              className="subtitle-selector-option"
              onClick={() => onSelect(choice.id)}
              onKeyDown={(event) => handleKeyDown(event, index)}
            >
              <span className="subtitle-selector-check" aria-hidden="true">
                {checked ? '✓' : ''}
              </span>
              <span className="subtitle-selector-option-label">{choice.label}</span>
            </button>
          );
        })}
      </div>
      {contentStatus === 'error' ? (
        <p className="subtitle-selector-status" role="status">
          {SUBTITLES_UNAVAILABLE_COPY}
        </p>
      ) : null}
    </div>
  );
}
