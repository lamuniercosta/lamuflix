import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
} from 'react';
import {
  MediaController,
  MediaControlBar,
  MediaFullscreenButton,
  MediaLoadingIndicator,
  MediaMuteButton,
  MediaPlayButton,
  MediaTimeDisplay,
  MediaTimeRange,
  MediaVolumeRange,
} from 'media-chrome/react';
import {
  AUTOPLAY_BLOCKED_COPY,
  ENDED_ANNOUNCEMENT,
  ERROR_COPY,
  LOADING_COPY,
  OPEN_LOCAL_COPY,
  RETRY_COPY,
  SETTINGS_LABEL,
  openLocalLabel,
  playerRegionLabel,
  retryLabel,
} from './playerCopy';
import { SUBTITLES_LABEL } from './subtitleCopy';
import SubtitlePanel, { type SubtitlePanelTab } from './SubtitlePanel';
import { applySubtitleTrackModes, watchSubtitleTrackModes } from './subtitleTrackModes';
import { useBrowserPlayer, type MediaDiagnostic } from './useBrowserPlayer';
import { useSubtitlePlayback } from './useSubtitlePlayback';
import './BrowserPlayer.css';

const SEEK_STEP_SECONDS = 10;
const NON_INTERACTIVE_SELECTOR =
  'input, textarea, select, [contenteditable]:not([contenteditable="false"]), [role="dialog"]';

interface CcSurface {
  movieId: number | string;
  open: boolean;
  tab: SubtitlePanelTab;
}

function freshSurface(movieId: number | string): CcSurface {
  return { movieId, open: false, tab: 'subtitles' };
}

export interface BrowserPlayerProps {
  title: string;
  movieId: number | string;
  streamUrl: string;
  localPlayEnabled?: boolean;
  onOpenLocal?: () => void;
  onDiagnostic?: (diagnostic: MediaDiagnostic) => void;
}

function isExcludedTarget(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.closest(NON_INTERACTIVE_SELECTOR) !== null;
}

function isPlayerOwned(target: EventTarget | null, container: HTMLElement | null): boolean {
  return target instanceof Node && container !== null && container.contains(target);
}

function isChromeControl(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.closest('media-control-bar') !== null;
}

function isActivatableControl(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.closest('button, [role="radio"]') !== null;
}

export default function BrowserPlayer({
  title,
  movieId,
  streamUrl,
  localPlayEnabled = false,
  onOpenLocal,
  onDiagnostic,
}: BrowserPlayerProps) {
  const { status, videoRef, retry, togglePlay, seekBy, toggleMute } = useBrowserPlayer({
    streamUrl,
    onDiagnostic,
  });
  const regionRef = useRef<HTMLElement | null>(null);
  const ccTriggerRef = useRef<HTMLButtonElement | null>(null);
  const panelId = useId();
  const [ccSurface, setCcSurface] = useState<CcSurface>(() => freshSurface(movieId));
  const surface = ccSurface.movieId === movieId ? ccSurface : freshSurface(movieId);
  const ccOpen = surface.open;
  const ccTab = surface.tab;
  const subtitles = useSubtitlePlayback(movieId);
  const { selection, contentStatus, tracks } = subtitles;
  const showTrack = selection !== null && contentStatus === 'ready';

  useEffect(() => {
    regionRef.current?.focus();
  }, [movieId]);

  const openCcPanel = useCallback(() => {
    setCcSurface({ movieId, open: true, tab: 'subtitles' });
  }, [movieId]);

  const closeCcPanel = useCallback(() => {
    setCcSurface(freshSurface(movieId));
  }, [movieId]);

  const selectTab = useCallback(
    (tab: SubtitlePanelTab) => {
      setCcSurface((current) => ({ ...freshSurface(movieId), open: current.open, tab }));
    },
    [movieId],
  );

  const toggleCcPanel = useCallback(() => {
    if (ccOpen) {
      closeCcPanel();
      return;
    }
    openCcPanel();
  }, [ccOpen, closeCcPanel, openCcPanel]);

  const desiredTrackId = showTrack ? (selection?.trackId ?? null) : null;
  const desiredTrackIdRef = useRef<string | null>(null);

  useEffect(() => {
    desiredTrackIdRef.current = desiredTrackId;
    applySubtitleTrackModes(videoRef.current, desiredTrackId);
  }, [desiredTrackId, videoRef]);

  useEffect(
    () => watchSubtitleTrackModes(videoRef.current, () => desiredTrackIdRef.current),
    [videoRef],
  );

  const assignChrome = useCallback((element: HTMLElement | null) => {
    if (!element) return;
    element.removeAttribute('role');
    element.removeAttribute('aria-label');
  }, []);

  const handleKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLElement>) => {
      if (event.defaultPrevented) return;
      if (!isPlayerOwned(event.target, regionRef.current)) return;
      if (isExcludedTarget(event.target)) return;
      if (isActivatableControl(event.target)) return;

      switch (event.key) {
        case ' ':
          event.preventDefault();
          if (isChromeControl(event.target)) return;
          togglePlay();
          return;
        case 'k':
          event.preventDefault();
          togglePlay();
          return;
        case 'ArrowRight':
          event.preventDefault();
          seekBy(SEEK_STEP_SECONDS);
          return;
        case 'ArrowLeft':
          event.preventDefault();
          seekBy(-SEEK_STEP_SECONDS);
          return;
        case 'm':
          event.preventDefault();
          toggleMute();
          return;
        case 'f': {
          event.preventDefault();
          if (document.fullscreenElement) {
            void document.exitFullscreen?.().catch(() => {});
            return;
          }
          void regionRef.current?.requestFullscreen?.().catch(() => {});
          return;
        }
        case 'Escape':
          if (document.fullscreenElement) {
            event.preventDefault();
            void document.exitFullscreen?.().catch(() => {});
          }
          return;
        default:
          return;
      }
    },
    [seekBy, toggleMute, togglePlay],
  );

  return (
    <section
      ref={regionRef}
      className="browser-player"
      aria-label={playerRegionLabel(title)}
      tabIndex={-1}
      onKeyDown={handleKeyDown}
    >
      <div className="browser-player-frame">
        <MediaController ref={assignChrome} noHotkeys className="browser-player-chrome">
          <video ref={videoRef} slot="media" className="browser-player-video" playsInline>
            {tracks.map((track) => {
              const active = showTrack && selection?.trackId === track.id;
              return (
                <track
                  key={track.id}
                  kind="subtitles"
                  label={track.label}
                  srcLang={track.language}
                  src={active ? selection?.src : undefined}
                  data-track-id={track.id}
                />
              );
            })}
          </video>
          <MediaLoadingIndicator slot="centered-chrome" noAutohide />
          <MediaControlBar>
            <MediaPlayButton />
            <MediaTimeRange />
            <MediaTimeDisplay showDuration noToggle />
            <MediaMuteButton />
            <MediaVolumeRange />
            <button
              ref={ccTriggerRef}
              type="button"
              className="browser-player-trigger"
              aria-label={SUBTITLES_LABEL}
              aria-expanded={ccOpen}
              aria-controls={panelId}
              onClick={toggleCcPanel}
            >
              CC
            </button>
            <button type="button" className="browser-player-trigger" aria-label={SETTINGS_LABEL}>
              <span aria-hidden="true">⚙</span>
            </button>
            <MediaFullscreenButton />
          </MediaControlBar>
          {ccOpen ? (
            <SubtitlePanel
              title={title}
              panelId={panelId}
              activeTab={ccTab}
              listStatus={subtitles.listStatus}
              contentStatus={subtitles.contentStatus}
              tracks={subtitles.tracks}
              selectedId={selection?.trackId ?? null}
              onTabChange={selectTab}
              onSelect={subtitles.select}
              onRetryList={subtitles.retryList}
              onClose={closeCcPanel}
            />
          ) : null}
        </MediaController>
      </div>

      <div className="browser-player-state" role="status" aria-live="polite">
        {(status === 'loading' || status === 'buffering') && (
          <p className="browser-player-state-line">{LOADING_COPY}</p>
        )}
        {status === 'autoplay-blocked' && (
          <p className="browser-player-state-line">{AUTOPLAY_BLOCKED_COPY}</p>
        )}
        {status === 'ended' && <p className="sr-only">{ENDED_ANNOUNCEMENT}</p>}
      </div>

      {status === 'error' ? (
        <div className="browser-player-actions">
          <p role="alert" className="browser-player-error">
            {ERROR_COPY}
          </p>
          <button
            type="button"
            className="lamu-action-primary"
            aria-label={retryLabel(title)}
            onClick={retry}
          >
            {RETRY_COPY}
          </button>
          {localPlayEnabled && onOpenLocal ? (
            <button
              type="button"
              className="lamu-action-quiet"
              aria-label={openLocalLabel(title)}
              onClick={onOpenLocal}
            >
              {OPEN_LOCAL_COPY}
            </button>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
