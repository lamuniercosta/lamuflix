import {
  useCallback,
  useEffect,
  useId,
  useMemo,
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
import { MediaPlaybackRateMenu, MediaSettingsMenuButton } from 'media-chrome/react/menu';
import {
  AUTOPLAY_BLOCKED_COPY,
  ENDED_ANNOUNCEMENT,
  ERROR_COPY,
  LOADING_COPY,
  OPEN_LOCAL_COPY,
  PLAYBACK_RATE_OPTIONS,
  RETRY_COPY,
  SETTINGS_LABEL,
  SPEED_LABEL,
  openLocalLabel,
  playbackRateLabel,
  playerRegionLabel,
  retryLabel,
} from './playerCopy';
import { SUBTITLES_LABEL, SUBTITLES_TRIGGER_COPY } from './subtitleCopy';
import CaptionOverlay from './CaptionOverlay';
import SubtitlePanel, { type SubtitlePanelTab } from './SubtitlePanel';
import { applySubtitleTrackModes, watchSubtitleTrackModes } from './subtitleTrackModes';
import type { SubtitleDelayDirection } from './subtitleTiming';
import { useBrowserPlayer, type MediaDiagnostic } from './useBrowserPlayer';
import { useSubtitleCues, type SubtitleCueList } from './useSubtitleCues';
import { useSubtitleOptions } from './useSubtitleOptions';
import { useSubtitlePlayback } from './useSubtitlePlayback';
import type { SubtitleStorage } from './subtitlePreferences';
import './BrowserPlayer.css';

const SEEK_STEP_SECONDS = 10;
const NON_INTERACTIVE_SELECTOR =
  'input, textarea, select, [contenteditable]:not([contenteditable="false"]), [role="dialog"]';

interface CcSurface {
  movieId: number | string;
  open: boolean;
  tab: SubtitlePanelTab;
}

interface SpeedMenuElement extends HTMLElement {
  formatMenuItemText: (text: string, rate: number) => string;
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

const SPACE_OWNING_SELECTOR =
  'button, [role="button"], [role="radio"], [role="tab"], [role="menuitem"], ' +
  '[role="menuitemradio"], [role="menuitemcheckbox"]';

function ownsSpaceActivation(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && target.closest(SPACE_OWNING_SELECTOR) !== null;
}

function isContainedInAny(node: unknown, elements: readonly (Node | null)[]): boolean {
  if (!(node instanceof Node)) return false;
  return elements.some((element) => element !== null && element.contains(node));
}

function exitFullscreenIfActive(): boolean {
  if (!document.fullscreenElement) return false;
  void document.exitFullscreen?.().catch(() => {});
  return true;
}

function enterFullscreen(element: HTMLElement | null): void {
  void element?.requestFullscreen?.().catch(() => {});
}

const DEFAULT_SURFACE_WIDTH_PX = 640;
const TRACK_ELEMENT_SELECTOR = 'track[data-track-id]';

function readLocalStorage(): SubtitleStorage | null {
  try {
    return window.localStorage;
  } catch {
    return null;
  }
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
  const gearTriggerRef = useRef<HTMLElement | null>(null);
  const speedMenuRef = useRef<HTMLElement | null>(null);
  const panelId = useId();
  const gearId = useId();
  const speedMenuId = useId();
  const [ccSurface, setCcSurface] = useState<CcSurface>(() => freshSurface(movieId));
  const surface = ccSurface.movieId === movieId ? ccSurface : freshSurface(movieId);
  const ccOpen = surface.open;
  const ccTab = surface.tab;
  const subtitles = useSubtitlePlayback(movieId);
  const { selection, contentStatus, tracks } = subtitles;
  const showTrack = selection !== null && contentStatus === 'ready';
  const selectionTrackId = selection?.trackId ?? null;
  const options = useSubtitleOptions({
    movieId,
    trackId: showTrack ? selectionTrackId : null,
    storage: readLocalStorage,
  });
  const timingEnabled = showTrack;
  const [mediaElement, setMediaElement] = useState<HTMLVideoElement | null>(null);
  const [surfaceWidthPx, setSurfaceWidthPx] = useState(DEFAULT_SURFACE_WIDTH_PX);
  const [isFullscreen, setIsFullscreen] = useState(false);

  const desiredTrackId = showTrack ? (selection?.trackId ?? null) : null;

  const cueList = useMemo<SubtitleCueList | null>(() => {
    if (!mediaElement || desiredTrackId === null) return null;
    const element = Array.from(
      mediaElement.querySelectorAll<HTMLTrackElement>(TRACK_ELEMENT_SELECTOR),
    ).find((candidate) => candidate.dataset.trackId === desiredTrackId);
    const track = element?.track as unknown as SubtitleCueList | null | undefined;
    if (!track || typeof track.addEventListener !== 'function') return null;
    return track;
  }, [desiredTrackId, mediaElement]);

  const activeCues = useSubtitleCues({
    media: mediaElement,
    cueList,
    hasTrack: timingEnabled,
    delayMs: options.delayMs,
  });
  const focusInsideSurfaceRef = useRef(false);
  const previousMovieRef = useRef(movieId);
  const ccOpenRef = useRef(ccOpen);
  const menuOpenRef = useRef(false);

  useEffect(() => {
    ccOpenRef.current = ccOpen;
  }, [ccOpen]);

  useEffect(() => {
    const previous = previousMovieRef.current;
    previousMovieRef.current = movieId;
    if (previous !== movieId) {
      if (focusInsideSurfaceRef.current) regionRef.current?.focus();
      focusInsideSurfaceRef.current = false;
      return;
    }
    regionRef.current?.focus();
  }, [movieId]);

  useEffect(() => {
    const onFocusIn = (event: Event) => {
      const target = event.target;
      const panel = document.getElementById(panelId);
      const menu = speedMenuRef.current;
      focusInsideSurfaceRef.current =
        (panel !== null && target instanceof Node && panel.contains(target)) ||
        (menu !== null && target instanceof Node && menu.contains(target));
    };
    document.addEventListener('focusin', onFocusIn);
    return () => document.removeEventListener('focusin', onFocusIn);
  }, [panelId]);

  useEffect(() => {
    if (!ccOpen) return;
    const panel = document.getElementById(panelId);
    panel?.querySelector<HTMLElement>('[role="tab"][aria-selected="true"]')?.focus();
  }, [ccOpen, movieId, panelId]);

  const openCcPanel = useCallback(() => {
    if (speedMenuRef.current) speedMenuRef.current.hidden = true;
    setCcSurface({ movieId, open: true, tab: 'subtitles' });
  }, [movieId]);

  const closeCcPanel = useCallback(() => {
    setCcSurface(freshSurface(movieId));
  }, [movieId]);

  const restoreTriggerFocus = useCallback((surface: 'cc' | 'menu') => {
    const trigger = surface === 'cc' ? ccTriggerRef.current : gearTriggerRef.current;
    if (trigger?.isConnected) {
      trigger.focus();
      return;
    }
    regionRef.current?.focus();
  }, []);

  const closeCcPanelAndRestore = useCallback(() => {
    const wasOpen = ccOpenRef.current;
    closeCcPanel();
    if (wasOpen) restoreTriggerFocus('cc');
  }, [closeCcPanel, restoreTriggerFocus]);

  const closeMenu = useCallback(() => {
    const menu = speedMenuRef.current;
    if (menu && !menu.hidden) menu.hidden = true;
  }, []);

  const closeMenuAndRestore = useCallback(() => {
    const wasOpen = menuOpenRef.current;
    closeMenu();
    if (wasOpen) restoreTriggerFocus('menu');
  }, [closeMenu, restoreTriggerFocus]);

  const selectTab = useCallback(
    (tab: SubtitlePanelTab) => {
      setCcSurface((current) => ({ ...freshSurface(movieId), open: current.open, tab }));
    },
    [movieId],
  );

  const toggleCcPanel = useCallback(() => {
    if (ccOpen) {
      closeCcPanelAndRestore();
      return;
    }
    openCcPanel();
  }, [ccOpen, closeCcPanelAndRestore, openCcPanel]);

  const desiredTrackIdRef = useRef<string | null>(null);

  const assignVideo = useCallback(
    (element: HTMLVideoElement | null) => {
      videoRef.current = element;
      setMediaElement(element);
    },
    [videoRef],
  );

  useEffect(() => {
    desiredTrackIdRef.current = desiredTrackId;
    applySubtitleTrackModes(mediaElement, desiredTrackId);
  }, [desiredTrackId, mediaElement]);

  useEffect(
    () => watchSubtitleTrackModes(mediaElement, () => desiredTrackIdRef.current),
    [mediaElement],
  );

  useEffect(() => {
    if (!mediaElement) return;
    const measure = () => {
      const rect = mediaElement.getBoundingClientRect();
      setSurfaceWidthPx(rect.width > 0 ? rect.width : DEFAULT_SURFACE_WIDTH_PX);
      setIsFullscreen(Boolean(document.fullscreenElement));
    };
    measure();
    window.addEventListener('resize', measure);
    document.addEventListener('fullscreenchange', measure);
    return () => {
      window.removeEventListener('resize', measure);
      document.removeEventListener('fullscreenchange', measure);
    };
  }, [mediaElement]);

  const assignChrome = useCallback((element: HTMLElement | null) => {
    if (!element) return;
    element.removeAttribute('role');
    element.removeAttribute('aria-label');
  }, []);

  const assignGear = useCallback((element: HTMLElement | null) => {
    if (!element) return;
    gearTriggerRef.current = element;
    element.setAttribute('aria-label', SETTINGS_LABEL);
  }, []);

  const assignSpeedMenu = useCallback((element: SpeedMenuElement | null) => {
    if (!element) return;
    speedMenuRef.current = element;
    element.formatMenuItemText = (_text: string, rate: number) => playbackRateLabel(rate);
  }, []);

  useEffect(() => {
    menuOpenRef.current = false;
    const menu = speedMenuRef.current;
    if (menu) menu.hidden = true;
  }, [movieId]);

  useEffect(() => {
    const menu = speedMenuRef.current;
    if (!menu) return;
    const onToggle = (event: Event) => {
      const newState = (event as Event & { newState?: string }).newState;
      menuOpenRef.current = newState === 'open';
      if (newState !== 'open') return;
      setCcSurface(freshSurface(movieId));
    };
    menu.addEventListener('toggle', onToggle);
    return () => menu.removeEventListener('toggle', onToggle);
  }, [movieId]);

  useEffect(() => {
    const panel = () => document.getElementById(panelId);
    const withinSurface = (node: unknown): boolean =>
      isContainedInAny(node, [panel(), speedMenuRef.current]);
    const withinTrigger = (node: unknown): boolean =>
      isContainedInAny(node, [ccTriggerRef.current, gearTriggerRef.current]);

    const onPointerDown = (event: Event) => {
      const cc = ccOpenRef.current;
      const menu = menuOpenRef.current;
      if (!cc && !menu) return;
      const path = typeof event.composedPath === 'function' ? event.composedPath() : [];
      if (path.some((node) => withinSurface(node) || withinTrigger(node))) return;
      if (cc) closeCcPanel();
      if (menu) closeMenu();
    };

    let wasFullscreen: boolean | null = null;
    const onFullscreenChange = () => {
      const isFullscreen = document.fullscreenElement !== null;
      const exited = wasFullscreen === null ? !isFullscreen : wasFullscreen && !isFullscreen;
      wasFullscreen = isFullscreen;
      if (!exited) return;
      const cc = ccOpenRef.current;
      const menu = menuOpenRef.current;
      if (!cc && !menu) return;
      if (cc) closeCcPanelAndRestore();
      if (menu) closeMenuAndRestore();
    };

    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('fullscreenchange', onFullscreenChange);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('fullscreenchange', onFullscreenChange);
    };
  }, [closeCcPanel, closeCcPanelAndRestore, closeMenu, closeMenuAndRestore, panelId]);

  const handleEscapeKey = useCallback(
    (event: ReactKeyboardEvent<HTMLElement>): boolean => {
      if (event.key !== 'Escape') return false;
      const cc = ccOpenRef.current;
      const menu = menuOpenRef.current;
      if (cc || menu) {
        event.preventDefault();
        if (cc) closeCcPanelAndRestore();
        if (menu) closeMenuAndRestore();
        return true;
      }
      if (exitFullscreenIfActive()) event.preventDefault();
      return true;
    },
    [closeCcPanelAndRestore, closeMenuAndRestore],
  );

  const handleToggleKey = useCallback(
    (event: ReactKeyboardEvent<HTMLElement>): boolean => {
      switch (event.key) {
        case ' ':
          if (ownsSpaceActivation(event.target)) return true;
          event.preventDefault();
          togglePlay();
          return true;
        case 'k':
          event.preventDefault();
          togglePlay();
          return true;
        case 'm':
          event.preventDefault();
          toggleMute();
          return true;
        default:
          return false;
      }
    },
    [toggleMute, togglePlay],
  );

  const handleDelayKey = useCallback(
    (event: ReactKeyboardEvent<HTMLElement>): boolean => {
      if (event.ctrlKey || event.altKey || event.metaKey) return false;
      let direction: SubtitleDelayDirection | null = null;
      if (event.key === 'h' || event.key === 'H') direction = 'later';
      else if (event.key === 'g' || event.key === 'G') direction = 'earlier';
      if (direction === null) return false;
      event.preventDefault();
      if (!timingEnabled || !mediaElement || mediaElement.paused) return true;
      options.adjustDelay(direction, event.shiftKey);
      return true;
    },
    [mediaElement, options, timingEnabled],
  );

  const handleSeekKey = useCallback(
    (event: ReactKeyboardEvent<HTMLElement>): boolean => {
      switch (event.key) {
        case 'ArrowRight':
          event.preventDefault();
          seekBy(SEEK_STEP_SECONDS);
          return true;
        case 'ArrowLeft':
          event.preventDefault();
          seekBy(-SEEK_STEP_SECONDS);
          return true;
        case 'f':
          event.preventDefault();
          if (!exitFullscreenIfActive()) enterFullscreen(regionRef.current);
          return true;
        default:
          return false;
      }
    },
    [seekBy],
  );

  const handleKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLElement>) => {
      if (event.defaultPrevented) return;
      if (!isPlayerOwned(event.target, regionRef.current)) return;
      if (isExcludedTarget(event.target)) return;
      if (handleEscapeKey(event)) return;
      if (handleToggleKey(event)) return;
      if (handleDelayKey(event)) return;
      handleSeekKey(event);
    },
    [handleDelayKey, handleEscapeKey, handleSeekKey, handleToggleKey],
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
          <video ref={assignVideo} slot="media" className="browser-player-video" playsInline>
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
              {SUBTITLES_TRIGGER_COPY}
            </button>
            <MediaSettingsMenuButton id={gearId} invokeTarget={speedMenuId} ref={assignGear} />
            <MediaFullscreenButton />
          </MediaControlBar>
          <MediaPlaybackRateMenu
            id={speedMenuId}
            hidden
            aria-label={SPEED_LABEL}
            rates={[...PLAYBACK_RATE_OPTIONS]}
            ref={assignSpeedMenu}
            {...{ noautohide: '' }}
          >
            <span slot="header">{SPEED_LABEL}</span>
          </MediaPlaybackRateMenu>
          <CaptionOverlay
            surface={isFullscreen ? 'fullscreen' : 'windowed'}
            cues={timingEnabled ? activeCues : []}
            style={options.style}
            surfaceWidthPx={surfaceWidthPx}
          />
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
              onClose={closeCcPanelAndRestore}
              options={{
                delayMs: options.delayMs,
                timingEnabled,
                style: options.style,
                onAdjustDelay: options.adjustDelay,
                onResetDelay: options.resetDelay,
                onUpdateStyle: options.updateStyle,
                onResetStyle: options.resetStyle,
              }}
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
