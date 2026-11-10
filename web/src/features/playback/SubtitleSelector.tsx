import type { ChangeEvent } from 'react';
import {
  SUBTITLES_LABEL,
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

/**
 * Off plus the server-listed tracks of one movie, in served order. Track labels render as JSX text
 * children only, so a hostile basename can never be interpreted as markup. The selector stays
 * hidden until a successful non-empty list exists; a failed list keeps Off and offers an explicit
 * Retry rather than presenting itself as a successful empty result. A failed track load keeps the
 * video playing, returns to Off and allows retry by reselecting a track.
 */
export default function SubtitleSelector({
  title,
  listStatus,
  contentStatus,
  tracks,
  selectedId,
  onSelect,
  onRetryList,
}: SubtitleSelectorProps) {
  const handleChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value;
    onSelect(value === '' ? null : value);
  };

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

  return (
    <div className="subtitle-selector">
      <label className="subtitle-selector-field">
        <span className="subtitle-selector-caption" aria-hidden="true">
          {SUBTITLES_LABEL}
        </span>
        <select
          className="subtitle-selector-select"
          aria-label={subtitleSelectLabel(title)}
          value={selectedId ?? ''}
          onChange={handleChange}
        >
          <option value="">{SUBTITLES_OFF_COPY}</option>
          {tracks.map((track) => (
            <option key={track.id} value={track.id}>
              {track.label}
            </option>
          ))}
        </select>
      </label>
      {contentStatus === 'error' ? (
        <p className="subtitle-selector-status" role="status">
          {SUBTITLES_UNAVAILABLE_COPY}
        </p>
      ) : null}
    </div>
  );
}
