import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ComponentProps } from 'react';
import BrowserPlayer from './BrowserPlayer';
import { autoplayDenial, installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import {
  AUTOPLAY_BLOCKED_COPY,
  ENDED_ANNOUNCEMENT,
  ERROR_COPY,
  LOADING_COPY,
  OPEN_LOCAL_COPY,
  SETTINGS_LABEL,
  playerRegionLabel,
  playLabel,
  openLocalLabel,
  retryLabel,
} from './playerCopy';
import { SUBTITLES_LABEL } from './subtitleCopy';

const STREAM = '/api/movies/1/stream';
let media: MediaStubHandle;

function region(title = 'Dune'): HTMLElement {
  return screen.getByRole('region', { name: playerRegionLabel(title) });
}

function video(container: HTMLElement = region()): HTMLVideoElement {
  const element = container.querySelector('video');
  if (!element) throw new Error('expected a video element inside the player region');
  return element;
}

function controlBar(container: HTMLElement = region()): HTMLElement {
  const element = container.querySelector<HTMLElement>('media-control-bar');
  if (!element) throw new Error('expected a media control bar inside the player region');
  return element;
}

function playControl(container: HTMLElement = region()): HTMLElement {
  const element = container.querySelector<HTMLElement>('media-play-button');
  if (!element) throw new Error('expected an in-bar play control inside the player region');
  return element;
}

function renderPlayer(props: Partial<ComponentProps<typeof BrowserPlayer>> = {}) {
  return render(<BrowserPlayer title="Dune" movieId={1} streamUrl={STREAM} {...props} />);
}

beforeEach(() => {
  media = installMediaStubs();
});

afterEach(() => {
  media.restore();
  vi.restoreAllMocks();
});

describe('BrowserPlayer region', () => {
  it('renders one labelled region with a single video element and labelled chrome', async () => {
    media.deferNextPlay();
    renderPlayer();

    const playerRegion = region();
    expect(playerRegion.querySelectorAll('video')).toHaveLength(1);
    expect(within(playerRegion).getByText(LOADING_COPY)).toBeInTheDocument();

    const labels = Array.from(playerRegion.querySelectorAll('media-control-bar *'))
      .map((element) => element.getAttribute('aria-label') ?? '')
      .join(' ');
    expect(labels).toMatch(/play|pause/i);
    expect(labels).toMatch(/mute/i);
    expect(labels).toMatch(/fullscreen/i);

    const seekRange = playerRegion.querySelector('media-time-range') as HTMLElement & {
      shadowRoot: ShadowRoot;
    };
    const seekInput = seekRange.shadowRoot.querySelector('input');
    expect(seekInput?.getAttribute('aria-label')).toBe('seek');
    expect(seekInput?.getAttribute('type')).toBe('range');
  });

  it('moves focus into the region when it opens', async () => {
    renderPlayer();

    await waitFor(() => expect(region()).toHaveFocus());
  });

  it('requests the stream and reflects native playing state', async () => {
    renderPlayer();

    expect(media.calls).toContain(`src:${STREAM}`);
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    expect(screen.queryByText(LOADING_COPY)).toBeNull();
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('does not treat buffering as a terminal failure', async () => {
    renderPlayer();

    await act(async () => {
      video().dispatchEvent(new Event('waiting'));
    });
    expect(screen.queryByText(ERROR_COPY)).toBeNull();
    expect(screen.queryByRole('alert')).toBeNull();
    expect(screen.getByText(LOADING_COPY)).toBeInTheDocument();
  });
});

describe('BrowserPlayer autoplay denial', () => {
  it('keeps an explicit in-bar Play control and shows no terminal error', async () => {
    const play = media.deferNextPlay();
    renderPlayer();

    await act(async () => {
      play.reject(autoplayDenial());
    });

    expect(await screen.findByText(AUTOPLAY_BLOCKED_COPY)).toBeInTheDocument();
    expect(screen.queryByText(ERROR_COPY)).toBeNull();
    expect(region().querySelectorAll('media-play-button')).toHaveLength(1);

    media.calls.length = 0;
    fireEvent.click(playControl());
    await waitFor(() => expect(media.calls).toEqual(['play']));
  });
});

describe('BrowserPlayer terminal failure', () => {
  it('shows generic copy and retries the same source from the beginning', async () => {
    renderPlayer();

    await act(async () => {
      const element = video();
      element.currentTime = 90;
      element.dispatchEvent(new Event('error'));
    });

    expect(await screen.findByRole('alert')).toHaveTextContent(ERROR_COPY);
    expect(region().textContent).not.toMatch(/http|status|codec|\.mp4/i);

    media.calls.length = 0;
    fireEvent.click(screen.getByRole('button', { name: retryLabel('Dune') }));
    await waitFor(() => expect(media.calls).toContain(`src:${STREAM}`));
    expect(media.calls.indexOf('pause')).toBeLessThan(media.calls.indexOf(`src:${STREAM}`));
    expect(media.calls.indexOf(`src:${STREAM}`)).toBeLessThan(media.calls.indexOf('play'));
    expect(video().currentTime).toBe(0);
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('never retries automatically and reports only safe diagnostics', async () => {
    const onDiagnostic = vi.fn();
    renderPlayer({ onDiagnostic });

    await act(async () => {
      video().dispatchEvent(new Event('error'));
    });

    expect(await screen.findByRole('alert')).toHaveTextContent(ERROR_COPY);
    await new Promise((resolve) => setTimeout(resolve, 30));
    expect(media.calls.filter((call) => call === 'play')).toHaveLength(1);
    expect(onDiagnostic).toHaveBeenCalled();
    const diagnostic = onDiagnostic.mock.calls.at(-1)?.[0];
    expect(Object.keys(diagnostic).sort()).toEqual(['mediaErrorCode', 'networkState']);
    expect(JSON.stringify(diagnostic)).not.toMatch(/stream|\/api|http/i);
  });

  it('offers the explicit local action only when capability is true and never invokes it', async () => {
    const onOpenLocal = vi.fn();
    const first = renderPlayer({ localPlayEnabled: true, onOpenLocal });

    await act(async () => {
      video().dispatchEvent(new Event('error'));
    });

    const localAction = await screen.findByRole('button', { name: openLocalLabel('Dune') });
    expect(localAction).toHaveTextContent(OPEN_LOCAL_COPY);
    expect(onOpenLocal).not.toHaveBeenCalled();

    fireEvent.click(localAction);
    expect(onOpenLocal).toHaveBeenCalledTimes(1);

    first.unmount();
    renderPlayer({ localPlayEnabled: false, onOpenLocal });
    await act(async () => {
      video().dispatchEvent(new Event('error'));
    });
    expect(await screen.findByRole('alert')).toHaveTextContent(ERROR_COPY);
    expect(screen.queryByRole('button', { name: openLocalLabel('Dune') })).toBeNull();
    expect(onOpenLocal).toHaveBeenCalledTimes(1);
  });

  it('offers no automatic local fallback on failure', async () => {
    const onOpenLocal = vi.fn();
    renderPlayer({ localPlayEnabled: true, onOpenLocal });

    await act(async () => {
      video().dispatchEvent(new Event('error'));
    });

    await new Promise((resolve) => setTimeout(resolve, 30));
    expect(onOpenLocal).not.toHaveBeenCalled();
  });
});

describe('BrowserPlayer source lifecycle', () => {
  it('pauses and clears the outgoing source before attaching the incoming one', async () => {
    const { rerender } = renderPlayer();

    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    media.calls.length = 0;

    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    expect(media.calls).toEqual([
      'pause',
      'clear:src',
      'load',
      'src:/api/movies/2/stream',
      'load',
      'play',
    ]);
    expect(region('Heat')).toBeInTheDocument();
  });

  it('ignores stale play rejections from the replaced source', async () => {
    const play = media.deferNextPlay();
    const { rerender } = renderPlayer();

    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);
    await act(async () => {
      play.reject(autoplayDenial());
    });

    await act(async () => {
      video(region('Heat')).dispatchEvent(new Event('playing'));
    });
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(screen.queryByText(ERROR_COPY)).toBeNull();
    expect(screen.queryByText(AUTOPLAY_BLOCKED_COPY)).toBeNull();
  });

  it('tears the player down on unmount', async () => {
    const { unmount } = renderPlayer();
    media.calls.length = 0;

    unmount();

    expect(media.calls).toEqual(['pause', 'clear:src', 'load']);
  });
});

describe('BrowserPlayer control bar composition', () => {
  it('shows current time with duration in one display and no separate duration display', async () => {
    renderPlayer();

    const timeDisplays = region().querySelectorAll('media-time-display');
    expect(timeDisplays).toHaveLength(1);
    expect(timeDisplays[0]).toHaveAttribute('showduration');
    expect(region().querySelector('media-duration-display')).toBeNull();

    const element = timeDisplays[0] as HTMLElement & { shadowRoot: ShadowRoot | null };
    element.setAttribute('mediaduration', '4625');
    element.setAttribute('mediacurrenttime', '12');

    const rendered = element.shadowRoot?.querySelector('slot')?.innerHTML ?? '';
    expect(rendered.replace(/&nbsp;|\u00a0/g, ' ')).toBe('0:12 / 1:17:05');
  });

  it('keeps exactly one in-bar play control and drops the duplicate Play action', () => {
    renderPlayer();

    const playControls = region().querySelectorAll('media-play-button');
    expect(playControls).toHaveLength(1);
    expect(controlBar().querySelectorAll('media-play-button')).toHaveLength(1);

    const named = screen
      .queryAllByRole('button')
      .filter((button) => button.getAttribute('aria-label') === playLabel('Dune'));
    expect(named).toHaveLength(0);
  });

  it('renders play, seek, combined time, volume, subtitles, settings and real fullscreen controls', () => {
    renderPlayer();

    const bar = controlBar();
    expect(bar.querySelectorAll('media-play-button')).toHaveLength(1);
    expect(bar.querySelectorAll('media-time-range')).toHaveLength(1);
    expect(bar.querySelectorAll('media-time-display')).toHaveLength(1);
    expect(bar.querySelectorAll('media-mute-button')).toHaveLength(1);
    expect(bar.querySelectorAll('media-volume-range')).toHaveLength(1);
    expect(bar.querySelectorAll('media-fullscreen-button')).toHaveLength(1);
    expect(within(region()).getByRole('button', { name: SUBTITLES_LABEL })).toBeInTheDocument();
    expect(within(region()).getByRole('button', { name: SETTINGS_LABEL })).toBeInTheDocument();
    expect(bar.querySelector('media-playback-rate-button')).toBeNull();
  });

  it('renders no subtitle select element', () => {
    renderPlayer();

    expect(region().querySelector('select')).toBeNull();
    expect(screen.queryByRole('combobox')).toBeNull();
  });
});

describe('BrowserPlayer seeking', () => {
  it('ignores seeks until valid seekable bounds exist', async () => {
    renderPlayer();

    fireEvent.keyDown(region(), { key: 'ArrowRight' });
    expect(video().currentTime).toBe(0);
    expect(screen.queryByRole('alert')).toBeNull();
    expect(media.calls.filter((call) => call === 'play')).toHaveLength(1);

    media.setSeekable(video(), []);
    fireEvent.keyDown(region(), { key: 'ArrowRight' });
    expect(video().currentTime).toBe(0);
  });

  it('seeks by ten seconds and clamps to the seekable bounds', async () => {
    renderPlayer();
    media.setSeekable(video(), [[0, 100]]);

    fireEvent.keyDown(region(), { key: 'ArrowRight' });
    expect(video().currentTime).toBe(10);
    fireEvent.keyDown(region(), { key: 'ArrowLeft' });
    expect(video().currentTime).toBe(0);
    fireEvent.keyDown(region(), { key: 'ArrowLeft' });
    expect(video().currentTime).toBe(0);

    video().currentTime = 95;
    fireEvent.keyDown(region(), { key: 'ArrowRight' });
    expect(video().currentTime).toBe(100);

    media.setSeekable(video(), [[5, 12]]);
    video().currentTime = 11;
    fireEvent.keyDown(region(), { key: 'ArrowRight' });
    expect(video().currentTime).toBe(12);
  });
});

describe('BrowserPlayer keyboard scope', () => {
  it('toggles playback once for Space and K with region focus and prevents scrolling', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    media.calls.length = 0;

    expect(fireEvent.keyDown(region(), { key: ' ' })).toBe(false);
    await waitFor(() => expect(media.calls).toEqual(['pause']));

    media.calls.length = 0;
    fireEvent.keyDown(region(), { key: 'k' });
    await waitFor(() => expect(media.calls).toEqual(['play']));
  });

  it('toggles exactly once when Space is pressed on a focused control', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    media.calls.length = 0;

    const playButton = region().querySelector('media-play-button') as HTMLElement;
    playButton.focus();
    playButton.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    playButton.dispatchEvent(new KeyboardEvent('keyup', { key: ' ', bubbles: true }));

    await waitFor(() => expect(media.calls).toEqual(['pause']));
  });

  it('toggles exactly once when K is pressed on a focused control', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    media.calls.length = 0;

    const playButton = region().querySelector('media-play-button') as HTMLElement;
    playButton.focus();
    playButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'k', bubbles: true }));

    await waitFor(() => expect(media.calls).toEqual(['pause']));
  });

  it('mutes with M and toggles fullscreen with F and Escape', async () => {
    const requestFullscreen = vi.fn().mockResolvedValue(undefined);
    const exitFullscreen = vi.fn().mockResolvedValue(undefined);
    const fullscreenDescriptor = Object.getOwnPropertyDescriptor(document, 'fullscreenElement');
    HTMLElement.prototype.requestFullscreen = requestFullscreen;
    Object.defineProperty(document, 'exitFullscreen', {
      configurable: true,
      value: exitFullscreen,
    });
    Object.defineProperty(document, 'fullscreenElement', { configurable: true, get: () => null });

    renderPlayer();
    fireEvent.keyDown(region(), { key: 'm' });
    expect(video().muted).toBe(true);
    fireEvent.keyDown(region(), { key: 'm' });
    expect(video().muted).toBe(false);

    fireEvent.keyDown(region(), { key: 'f' });
    expect(requestFullscreen).toHaveBeenCalledTimes(1);

    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => region(),
    });
    fireEvent.keyDown(region(), { key: 'f' });
    expect(exitFullscreen).toHaveBeenCalledTimes(1);

    fireEvent.keyDown(region(), { key: 'Escape' });
    expect(exitFullscreen).toHaveBeenCalledTimes(2);

    if (fullscreenDescriptor) {
      Object.defineProperty(document, 'fullscreenElement', fullscreenDescriptor);
    }
  });

  it('cycles no ad-hoc rate control in the bar while the settings trigger owns speed', () => {
    renderPlayer();

    expect(region().querySelector('media-playback-rate-button')).toBeNull();
    expect(video().playbackRate).toBe(1);
    expect(within(region()).getByRole('button', { name: SETTINGS_LABEL })).toBeInTheDocument();
  });

  it('ignores shortcuts from editable fields, dialogs and outside the player', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    media.calls.length = 0;

    const input = document.createElement('input');
    document.body.append(input);
    fireEvent.keyDown(input, { key: ' ', bubbles: true });

    const dialog = document.createElement('div');
    dialog.setAttribute('role', 'dialog');
    const dialogButton = document.createElement('button');
    dialog.append(dialogButton);
    document.body.append(dialog);
    fireEvent.keyDown(dialogButton, { key: ' ' });
    fireEvent.keyDown(dialogButton, { key: 'm' });

    fireEvent.keyDown(document.body, { key: ' ' });
    fireEvent.keyDown(window, { key: 'k' });

    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(media.calls).toEqual([]);
    expect(video().muted).toBe(false);

    input.remove();
    dialog.remove();
  });
});

describe('BrowserPlayer terminal states', () => {
  it('announces the ended state politely and offers replay from the beginning', async () => {
    renderPlayer();

    await act(async () => {
      video().dispatchEvent(new Event('ended'));
    });
    expect(screen.getByText(ENDED_ANNOUNCEMENT)).toBeInTheDocument();
    media.calls.length = 0;

    fireEvent.keyDown(region(), { key: 'k' });
    await waitFor(() => expect(media.calls).toContain('play'));
    expect(video().currentTime).toBe(0);
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('exposes a single in-bar play control and no separate labelled Play action', async () => {
    renderPlayer();
    expect(region().querySelectorAll('media-play-button')).toHaveLength(1);
    expect(within(region()).queryByRole('button', { name: playLabel('Dune') })).toBeNull();
  });
});
