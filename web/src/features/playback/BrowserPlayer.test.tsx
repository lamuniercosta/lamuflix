import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ComponentProps } from 'react';
import BrowserPlayer from './BrowserPlayer';
import { autoplayDenial, installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import { server } from '../../test/server';
import {
  AUTOPLAY_BLOCKED_COPY,
  ENDED_ANNOUNCEMENT,
  ERROR_COPY,
  LOADING_COPY,
  OPEN_LOCAL_COPY,
  SETTINGS_LABEL,
  playbackRateLabel,
  playerRegionLabel,
  playLabel,
  openLocalLabel,
  retryLabel,
} from './playerCopy';
import {
  CLOSE_SUBTITLES_PANEL_LABEL,
  SUBTITLES_LABEL,
  SUBTITLES_OPTIONS_EMPTY_COPY,
  SUBTITLES_OPTIONS_LABEL,
  SUBTITLES_TRIGGER_COPY,
  SUBTITLES_UNAVAILABLE_COPY,
  subtitlePanelLabel,
} from './subtitleCopy';

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

function ccTrigger(): HTMLElement {
  return within(region()).getByRole('button', { name: SUBTITLES_LABEL });
}

function panelId(): string {
  return ccTrigger().getAttribute('aria-controls') ?? '';
}

async function openPanel(): Promise<HTMLElement> {
  await screen.findByRole('tablist');
  return screen.getByRole('region', { name: subtitlePanelLabel('Dune') });
}

interface SpeedMenuElement extends HTMLElement {
  shadowRoot: ShadowRoot;
}

function gearTrigger(title = 'Dune'): HTMLElement {
  return within(region(title)).getByRole('button', { name: SETTINGS_LABEL });
}

function speedMenu(title = 'Dune'): SpeedMenuElement {
  const element = region(title).querySelector('media-playback-rate-menu');
  if (!element || !element.shadowRoot) {
    throw new Error('expected the settings speed menu inside the player region');
  }
  return element as SpeedMenuElement;
}

function speedOptions(title = 'Dune'): HTMLElement[] {
  return Array.from(
    speedMenu(title).shadowRoot.querySelectorAll<HTMLElement>('[role="menuitemradio"]'),
  );
}

function speedLabels(title = 'Dune'): string[] {
  return speedOptions(title).map((option) => (option.textContent ?? '').trim());
}

function checkedSpeed(title = 'Dune'): string {
  const index = speedOptions(title).findIndex(
    (option) => option.getAttribute('aria-checked') === 'true',
  );
  return speedLabels(title)[index];
}

async function openGear(title = 'Dune'): Promise<void> {
  fireEvent.click(gearTrigger(title));
  await waitFor(() => expect(speedMenu(title).hasAttribute('hidden')).toBe(false));
}

async function setLiveRate(rate: number): Promise<void> {
  await act(async () => {
    const element = video();
    element.playbackRate = rate;
    element.dispatchEvent(new Event('ratechange'));
  });
}

beforeEach(() => {
  media = installMediaStubs();
});

afterEach(() => {
  media.restore();
  vi.restoreAllMocks();
  Reflect.deleteProperty(document, 'fullscreenElement');
  Reflect.deleteProperty(document, 'exitFullscreen');
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
    } else {
      Reflect.deleteProperty(document, 'fullscreenElement');
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

describe('BrowserPlayer CC panel', () => {
  it('keeps the CC trigger visible when the movie serves no local tracks', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles', () => HttpResponse.json([], { status: 200 })),
    );
    renderPlayer();

    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false');
    fireEvent.click(ccTrigger());

    const surface = await openPanel();
    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'true');
    expect(within(surface).getByRole('tablist')).toBeInTheDocument();
    expect(screen.queryByRole('radiogroup')).toBeNull();
  });

  it('keeps the CC trigger visible and polite when the track list fails', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles', () => new HttpResponse(null, { status: 500 })),
    );
    renderPlayer();

    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    expect(await within(surface).findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(within(surface).getByRole('status')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).toBeNull();
    expect(ccTrigger()).toBeInTheDocument();
  });

  it('opens a non-modal labelled panel with the Subtitles tab active and never a dialog', async () => {
    renderPlayer();

    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false');
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    expect(screen.queryByRole('dialog')).toBeNull();
    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'true');
    expect(ccTrigger()).toHaveAttribute('aria-controls', panelId());

    const tabs = within(surface).getAllByRole('tab');
    expect(tabs.map((tab) => tab.textContent)).toEqual([SUBTITLES_LABEL, SUBTITLES_OPTIONS_LABEL]);
    expect(within(surface).getByRole('tab', { name: SUBTITLES_LABEL })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    expect(within(surface).getByRole('tabpanel')).toBeInTheDocument();
  });

  it('renders the CC trigger text from the approved copy module', () => {
    renderPlayer();

    expect(ccTrigger()).toHaveTextContent(SUBTITLES_TRIGGER_COPY);
  });

  it('keeps both tabs wired to existing panels in either tab state', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    const controlsResolve = () =>
      within(surface)
        .getAllByRole('tab')
        .every(
          (tab) =>
            surface.ownerDocument.getElementById(tab.getAttribute('aria-controls') ?? '') !== null,
        );
    expect(controlsResolve()).toBe(true);

    fireEvent.click(within(surface).getByRole('tab', { name: SUBTITLES_OPTIONS_LABEL }));
    expect(controlsResolve()).toBe(true);
    expect(within(surface).getAllByRole('tabpanel')).toHaveLength(1);
  });

  it('shows only honest empty-shell copy on the Options tab', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    fireEvent.click(within(surface).getByRole('tab', { name: SUBTITLES_OPTIONS_LABEL }));

    expect(within(surface).getByRole('tab', { name: SUBTITLES_OPTIONS_LABEL })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    expect(within(surface).getByText(SUBTITLES_OPTIONS_EMPTY_COPY)).toBeInTheDocument();
    expect(within(surface).queryByRole('radiogroup')).toBeNull();
    expect(within(surface).queryByRole('slider')).toBeNull();
    expect(within(surface).queryByRole('switch')).toBeNull();
  });

  it('closes through the labelled close control and returns to collapsed state', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    fireEvent.click(within(surface).getByRole('button', { name: CLOSE_SUBTITLES_PANEL_LABEL }));

    await waitFor(() => expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false'));
    expect(screen.queryByRole('tablist')).toBeNull();
  });

  it('toggles the panel from the CC trigger and returns to the Subtitles tab each open', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();
    fireEvent.click(within(surface).getByRole('tab', { name: SUBTITLES_OPTIONS_LABEL }));

    fireEvent.click(ccTrigger());
    await waitFor(() => expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false'));

    fireEvent.click(ccTrigger());
    const reopened = await openPanel();
    expect(within(reopened).getByRole('tab', { name: SUBTITLES_LABEL })).toHaveAttribute(
      'aria-selected',
      'true',
    );
  });

  it('keeps the playback failure alert distinct from the polite subtitle status', async () => {
    renderPlayer();

    await act(async () => {
      video().dispatchEvent(new Event('error'));
    });

    expect(await screen.findByRole('alert')).toHaveTextContent(ERROR_COPY);
    expect(ccTrigger()).toBeInTheDocument();
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });
});

describe('BrowserPlayer gear speed menu', () => {
  it('offers exactly the seven quarter-step speeds with normal labelled once', async () => {
    renderPlayer();
    await openGear();

    expect(speedMenu().getAttribute('rates')).toBe('0.5 0.75 1 1.25 1.5 1.75 2');
    expect(speedLabels()).toEqual(['0.5x', '0.75x', '1x (Normal)', '1.25x', '1.5x', '1.75x', '2x']);
  });

  it('keeps the open speed menu exempt from controller autohide', async () => {
    renderPlayer();
    await openGear();

    expect(speedMenu().hasAttribute('noautohide')).toBe(true);
  });

  it('opens the speed menu from the gear trigger and closes it again', async () => {
    renderPlayer();

    expect(gearTrigger()).toHaveAttribute('aria-label', SETTINGS_LABEL);
    expect(speedMenu().hasAttribute('hidden')).toBe(true);

    await openGear();
    expect(gearTrigger()).toHaveAttribute('aria-expanded', 'true');

    fireEvent.click(gearTrigger());
    await waitFor(() => expect(speedMenu().hasAttribute('hidden')).toBe(true));
    expect(gearTrigger()).toHaveAttribute('aria-expanded', 'false');
  });

  it('checks the speed matching the live media rate and changes playback rate', async () => {
    renderPlayer();
    await openGear();

    expect(checkedSpeed()).toBe('1x (Normal)');

    fireEvent.click(speedOptions()[4]);
    await waitFor(() => expect(video().playbackRate).toBe(1.5));
    expect(checkedSpeed()).toBe('1.5x');

    fireEvent.click(speedOptions()[0]);
    await waitFor(() => expect(video().playbackRate).toBe(0.5));
    expect(checkedSpeed()).toBe('0.5x');
  });

  it('follows rate changes from outside the menu without resetting to 1x', async () => {
    renderPlayer();
    await openGear();

    await setLiveRate(0.75);
    expect(checkedSpeed()).toBe('0.75x');

    await setLiveRate(2);
    expect(checkedSpeed()).toBe('2x');
    expect(video().playbackRate).toBe(2);
    expect(checkedSpeed()).not.toBe('1x (Normal)');
  });

  it('resynchronizes the checked speed after a movie change', async () => {
    const { rerender } = renderPlayer({ movieId: 1 });
    await setLiveRate(1.75);
    await openGear();
    expect(checkedSpeed()).toBe('1.75x');

    await act(async () => {
      rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);
    });

    await openGear('Heat');
    const liveRate = video(region('Heat')).playbackRate;
    expect(checkedSpeed('Heat')).toBe(playbackRateLabel(liveRate));
  });

  it('keeps no saved speed preference and no forced reset beyond live state', async () => {
    renderPlayer();
    await openGear();
    fireEvent.click(speedOptions()[5]);
    await waitFor(() => expect(video().playbackRate).toBe(1.75));
    await waitFor(() => expect(speedMenu().hasAttribute('hidden')).toBe(true));

    await setLiveRate(1);
    await openGear();

    expect(checkedSpeed()).toBe('1x (Normal)');
    expect(video().playbackRate).toBe(1);
    expect(window.localStorage.getItem('playbackRate')).toBeNull();
    expect(window.sessionStorage.length).toBe(0);
  });

  it('closes the CC panel when the gear opens and closes the gear when CC opens', async () => {
    renderPlayer();

    fireEvent.click(ccTrigger());
    const surface = await openPanel();
    expect(surface).toBeInTheDocument();

    await openGear();
    expect(screen.queryByRole('tablist')).toBeNull();
    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false');

    fireEvent.click(ccTrigger());
    const reopened = await openPanel();
    expect(reopened).toBeInTheDocument();
    await waitFor(() => expect(speedMenu().hasAttribute('hidden')).toBe(true));
    expect(gearTrigger()).toHaveAttribute('aria-expanded', 'false');
  });
});

describe('BrowserPlayer focus and dismissal', () => {
  it('moves focus into the panel when the CC trigger opens it', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());

    const surface = await openPanel();
    const activeTab = within(surface).getByRole('tab', { name: SUBTITLES_LABEL });
    await waitFor(() => expect(activeTab).toHaveFocus());
  });

  it('closes on Escape and returns focus to the CC trigger before exiting fullscreen', async () => {
    const requestFullscreen = vi.fn().mockResolvedValue(undefined);
    const exitFullscreen = vi.fn().mockResolvedValue(undefined);
    const descriptor = Object.getOwnPropertyDescriptor(document, 'fullscreenElement');
    HTMLElement.prototype.requestFullscreen = requestFullscreen;
    Object.defineProperty(document, 'exitFullscreen', {
      configurable: true,
      value: exitFullscreen,
    });
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => region(),
    });

    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();
    const row = within(surface).getAllByRole('tab')[1];
    row.focus();

    fireEvent.keyDown(document.activeElement ?? surface, { key: 'Escape' });

    await waitFor(() => expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false'));
    expect(screen.queryByRole('tablist')).toBeNull();
    expect(ccTrigger()).toHaveFocus();
    expect(exitFullscreen).not.toHaveBeenCalled();

    if (descriptor) {
      Object.defineProperty(document, 'fullscreenElement', descriptor);
    }
  });

  it('exits fullscreen on Escape only when no surface is open', async () => {
    const requestFullscreen = vi.fn().mockResolvedValue(undefined);
    const exitFullscreen = vi.fn().mockResolvedValue(undefined);
    const descriptor = Object.getOwnPropertyDescriptor(document, 'fullscreenElement');
    HTMLElement.prototype.requestFullscreen = requestFullscreen;
    Object.defineProperty(document, 'exitFullscreen', {
      configurable: true,
      value: exitFullscreen,
    });
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => region(),
    });

    renderPlayer();
    fireEvent.keyDown(region(), { key: 'Escape' });

    await waitFor(() => expect(exitFullscreen).toHaveBeenCalledTimes(1));

    if (descriptor) {
      Object.defineProperty(document, 'fullscreenElement', descriptor);
    }
  });

  it('closes a stale CC panel when fullscreen exits and restores trigger focus', async () => {
    const descriptor = Object.getOwnPropertyDescriptor(document, 'fullscreenElement');
    let current: Element | null = null;
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => current,
    });

    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();
    within(surface).getAllByRole('tab')[1].focus();

    current = region();
    document.dispatchEvent(new Event('fullscreenchange'));
    current = null;
    document.dispatchEvent(new Event('fullscreenchange'));

    await waitFor(() => expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false'));
    expect(screen.queryByRole('tablist')).toBeNull();
    expect(ccTrigger()).toHaveFocus();

    if (descriptor) {
      Object.defineProperty(document, 'fullscreenElement', descriptor);
    }
  });

  it('closes a stale gear menu when fullscreen exits and restores trigger focus', async () => {
    const descriptor = Object.getOwnPropertyDescriptor(document, 'fullscreenElement');
    let current: Element | null = null;
    Object.defineProperty(document, 'fullscreenElement', {
      configurable: true,
      get: () => current,
    });

    renderPlayer();
    await openGear();
    speedOptions()[0].focus();

    current = region();
    document.dispatchEvent(new Event('fullscreenchange'));
    current = null;
    document.dispatchEvent(new Event('fullscreenchange'));

    await waitFor(() => expect(speedMenu().hasAttribute('hidden')).toBe(true));
    expect(gearTrigger()).toHaveFocus();

    if (descriptor) {
      Object.defineProperty(document, 'fullscreenElement', descriptor);
    }
  });

  it('dismisses the panel on an outside pointerdown and keeps destination focus', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    await openPanel();

    const destination = document.createElement('a');
    destination.href = '#elsewhere';
    destination.textContent = 'elsewhere';
    document.body.append(destination);
    destination.focus();

    fireEvent.pointerDown(destination);

    await waitFor(() => expect(ccTrigger()).toHaveAttribute('aria-expanded', 'false'));
    expect(destination).toHaveFocus();
    destination.remove();
  });

  it('never dismisses the panel from inside the surface or pointercancel alone', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    fireEvent.pointerDown(surface);
    fireEvent.pointerCancel(video());

    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('tablist')).toBeInTheDocument();
  });

  it('switches surfaces once when the other trigger is pressed', async () => {
    renderPlayer();
    fireEvent.click(ccTrigger());
    await openPanel();

    fireEvent.pointerDown(gearTrigger());
    fireEvent.click(gearTrigger());

    await waitFor(() => expect(speedMenu().hasAttribute('hidden')).toBe(false));
    expect(gearTrigger()).toHaveAttribute('aria-expanded', 'true');
    expect(screen.queryByRole('tablist')).toBeNull();
  });

  it('closes the gear menu on Escape and restores trigger focus', async () => {
    renderPlayer();
    await openGear();
    const item = speedOptions()[2];
    item.focus();

    fireEvent.keyDown(item, { key: 'Escape' });

    await waitFor(() => expect(speedMenu().hasAttribute('hidden')).toBe(true));
    expect(gearTrigger()).toHaveAttribute('aria-expanded', 'false');
  });
});

describe('BrowserPlayer hotkey isolation on panel and menu controls', () => {
  it('keeps K, M and F working while a panel control holds focus', async () => {
    const requestFullscreen = vi.fn().mockResolvedValue(undefined);
    HTMLElement.prototype.requestFullscreen = requestFullscreen;

    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    media.calls.length = 0;
    const tab = within(surface).getByRole('tab', { name: SUBTITLES_LABEL });
    tab.focus();
    fireEvent.keyDown(tab, { key: 'k' });
    await waitFor(() => expect(media.calls).toEqual(['pause']));

    const close = within(surface).getByRole('button', { name: CLOSE_SUBTITLES_PANEL_LABEL });
    close.focus();
    fireEvent.keyDown(close, { key: 'm' });
    expect(video().muted).toBe(true);

    fireEvent.keyDown(close, { key: 'f' });
    expect(requestFullscreen).toHaveBeenCalledTimes(1);
  });

  it('never seeks when a tab or a track row consumes the arrow keys', async () => {
    renderPlayer();
    media.setSeekable(video(), [[0, 100]]);
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    const rows = await within(surface).findAllByRole('radio');
    rows[0].focus();
    fireEvent.keyDown(rows[0], { key: 'ArrowRight' });
    expect(video().currentTime).toBe(0);
    fireEvent.keyDown(rows[0], { key: 'ArrowLeft' });
    expect(video().currentTime).toBe(0);

    const subtitlesTab = within(surface).getByRole('tab', { name: SUBTITLES_LABEL });
    subtitlesTab.focus();
    fireEvent.keyDown(subtitlesTab, { key: 'ArrowRight' });
    expect(video().currentTime).toBe(0);
    expect(within(surface).getByRole('tab', { name: SUBTITLES_OPTIONS_LABEL })).toHaveAttribute(
      'aria-selected',
      'true',
    );
  });

  it('applies exactly one action when panel controls consume Space or Enter', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    media.calls.length = 0;
    const rows = await within(surface).findAllByRole('radio');
    rows[0].focus();
    fireEvent.keyDown(rows[0], { key: ' ' });
    fireEvent.keyUp(rows[0], { key: ' ' });

    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(media.calls).toEqual([]);
    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'true');
  });

  it('leaves the activation of a focused tab to the control without toggling playback', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    media.calls.length = 0;
    const optionsTab = within(surface).getByRole('tab', { name: SUBTITLES_OPTIONS_LABEL });
    optionsTab.focus();
    fireEvent.keyDown(optionsTab, { key: ' ' });
    fireEvent.keyUp(optionsTab, { key: ' ' });
    fireEvent.keyDown(optionsTab, { key: 'Enter' });

    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(media.calls).toEqual([]);
    expect(ccTrigger()).toHaveAttribute('aria-expanded', 'true');
  });

  it('preserves the ten-second arrow seek from the close control', async () => {
    renderPlayer();
    media.setSeekable(video(), [[0, 100]]);
    fireEvent.click(ccTrigger());
    const surface = await openPanel();

    const close = within(surface).getByRole('button', { name: CLOSE_SUBTITLES_PANEL_LABEL });
    close.focus();

    fireEvent.keyDown(close, { key: 'ArrowRight' });
    expect(video().currentTime).toBe(10);
    fireEvent.keyDown(close, { key: 'ArrowLeft' });
    expect(video().currentTime).toBe(0);
  });

  it('preserves the ten-second arrow seek from the speed menu trigger', async () => {
    renderPlayer();
    media.setSeekable(video(), [[0, 100]]);

    gearTrigger().focus();
    fireEvent.keyDown(gearTrigger(), { key: 'ArrowRight' });
    expect(video().currentTime).toBe(10);
  });

  it('keeps hotkeys working on a speed menu item without double activation', async () => {
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    await openGear();

    const item = speedOptions()[1];
    item.focus();
    media.calls.length = 0;

    item.dispatchEvent(new KeyboardEvent('keydown', { key: 'k', bubbles: true, composed: true }));

    await waitFor(() => expect(media.calls).toEqual(['pause']));
    expect(video().playbackRate).toBe(1);
  });

  it('changes speed through keyboard alone while the menu consumes the keys', async () => {
    renderPlayer();
    await openGear();

    const first = speedOptions()[0];
    first.focus();
    first.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, composed: true }),
    );

    const focused = speedMenu().shadowRoot.activeElement as HTMLElement | null;
    expect(focused).not.toBeNull();
    focused?.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, composed: true }),
    );

    await waitFor(() => expect(video().playbackRate).toBe(0.75));
    expect(checkedSpeed()).toBe('0.75x');
    expect(media.calls.filter((call) => call === 'pause')).toHaveLength(0);
  });
});

describe('BrowserPlayer surface lifecycle', () => {
  it('resets the surface on movie change and restores focus lost with it', async () => {
    const { rerender } = renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();
    const rows = await within(surface).findAllByRole('radio');
    rows[0].focus();

    await act(async () => {
      rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);
    });

    expect(screen.queryByRole('tablist')).toBeNull();
    await waitFor(() => expect(region('Heat')).toHaveFocus());
  });

  it('leaves outside focus untouched on movie change', async () => {
    const outside = document.createElement('button');
    outside.textContent = 'outside';
    document.body.append(outside);

    const { rerender } = renderPlayer();
    fireEvent.click(ccTrigger());
    await openPanel();

    outside.focus();
    await act(async () => {
      rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);
    });

    expect(outside).toHaveFocus();
    outside.remove();
  });

  it('unmounts without focusing detached nodes or stealing destination focus', async () => {
    const outside = document.createElement('button');
    outside.textContent = 'outside';
    document.body.append(outside);

    const first = renderPlayer();
    fireEvent.click(ccTrigger());
    const surface = await openPanel();
    within(surface).getAllByRole('tab')[1].focus();

    outside.focus();
    await act(async () => {
      first.unmount();
    });

    expect(outside).toHaveFocus();
    expect(document.activeElement?.isConnected).toBe(true);
    outside.remove();
  });
});
