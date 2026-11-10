import { useCallback, useEffect, useRef, type KeyboardEvent as ReactKeyboardEvent } from 'react';
import {
  MediaController,
  MediaControlBar,
  MediaDurationDisplay,
  MediaFullscreenButton,
  MediaLoadingIndicator,
  MediaMuteButton,
  MediaPlaybackRateButton,
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
  PLAY_COPY,
  PLAYBACK_SPEEDS,
  RETRY_COPY,
  openLocalLabel,
  playLabel,
  playerRegionLabel,
  retryLabel,
} from './playerCopy';
import { useBrowserPlayer, type MediaDiagnostic } from './useBrowserPlayer';
import './BrowserPlayer.css';

const SEEK_STEP_SECONDS = 10;
const NON_INTERACTIVE_SELECTOR =
  'input, textarea, select, [contenteditable]:not([contenteditable="false"]), [role="dialog"]';

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

  useEffect(() => {
    regionRef.current?.focus();
  }, [movieId]);

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
          <video ref={videoRef} slot="media" className="browser-player-video" playsInline />
          <MediaLoadingIndicator slot="centered-chrome" noAutohide />
          <MediaControlBar>
            <MediaPlayButton />
            <MediaTimeRange />
            <MediaTimeDisplay />
            <MediaDurationDisplay />
            <MediaMuteButton />
            <MediaVolumeRange />
            <MediaPlaybackRateButton rates={PLAYBACK_SPEEDS} />
            <MediaFullscreenButton />
          </MediaControlBar>
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
      ) : (
        <div className="browser-player-actions">
          <button
            type="button"
            className="lamu-action-primary"
            aria-label={playLabel(title)}
            onClick={togglePlay}
          >
            {PLAY_COPY}
          </button>
        </div>
      )}
    </section>
  );
}
