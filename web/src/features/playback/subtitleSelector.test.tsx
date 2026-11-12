import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ComponentProps } from 'react';
import BrowserPlayer from './BrowserPlayer';
import { playerRegionLabel } from './playerCopy';
import {
  SUBTITLES_EMPTY_COPY,
  SUBTITLES_LABEL,
  SUBTITLES_LOADING_COPY,
  SUBTITLES_OFF_COPY,
  SUBTITLES_RETRY_COPY,
  SUBTITLES_UNAVAILABLE_COPY,
  subtitlePanelLabel,
  subtitleSelectLabel,
  subtitlesRetryLabel,
} from './subtitleCopy';
import { server } from '../../test/server';
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import type { components } from '../../api/types';

type SubtitleTrackDto = components['schemas']['SubtitleTrackDto'];

const ENGLISH: SubtitleTrackDto = {
  id: 'token-en',
  label: 'English',
  language: 'en',
  isDefault: true,
};

const PORTUGUESE: SubtitleTrackDto = {
  id: 'token-pob',
  label: 'Portuguese (Brazil)',
  language: 'pt-BR',
  isDefault: false,
};

const HOSTILE: SubtitleTrackDto = {
  id: 'token-hostile',
  label: '<img src=x onerror=alert(1)> & "Dune.srt"',
  language: 'und',
  isDefault: false,
};

const VTT = 'WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello.\n';

let media: MediaStubHandle;
let listRequests = 0;
let contentRequests: string[] = [];
let owned: string[] = [];

function renderPlayer(props: Partial<ComponentProps<typeof BrowserPlayer>> = {}) {
  return render(
    <BrowserPlayer title="Dune" movieId={1} streamUrl="/api/movies/1/stream" {...props} />,
  );
}

function region(title = 'Dune'): HTMLElement {
  return screen.getByRole('region', { name: playerRegionLabel(title) });
}

function video(): HTMLVideoElement {
  const element = region().querySelector('video');
  if (!element) throw new Error('expected the player to render one video');
  return element;
}

function group(title = 'Dune'): HTMLElement | null {
  return screen.queryByRole('radiogroup', { name: subtitleSelectLabel(title) });
}

function ccTrigger(title = 'Dune'): HTMLElement {
  return within(region(title)).getByRole('button', { name: SUBTITLES_LABEL });
}

async function openPanel(title = 'Dune'): Promise<HTMLElement> {
  const trigger = ccTrigger(title);
  if (trigger.getAttribute('aria-expanded') !== 'true') fireEvent.click(trigger);
  const panel = await screen.findByRole('region', { name: subtitlePanelLabel(title) });
  await waitFor(() => expect(panel.querySelector('[aria-busy="true"]')).toBeNull());
  return panel;
}

function radios(title = 'Dune'): HTMLElement[] {
  const element = group(title);
  if (!element) return [];
  return within(element).getAllByRole('radio');
}

function trackLabels(title = 'Dune'): string[] {
  return radios(title).map((radio) => radio.getAttribute('aria-label') ?? '');
}

function trackIds(title = 'Dune'): string[] {
  return radios(title).map((radio) => radio.getAttribute('data-track-id') ?? '');
}

function checkedLabel(title = 'Dune'): string | null {
  const checked = radios(title).find((radio) => radio.getAttribute('aria-checked') === 'true');
  return checked?.getAttribute('aria-label') ?? null;
}

function choose(label: string, title = 'Dune'): void {
  const element = group(title);
  if (!element) throw new Error(`expected a subtitle radiogroup for ${title}`);
  fireEvent.click(within(element).getByRole('radio', { name: label }));
}

async function serveTracks(next: SubtitleTrackDto[], delayMs = 0) {
  server.use(
    http.get('*/api/movies/:id/subtitles', async () => {
      listRequests += 1;
      if (delayMs > 0) await new Promise((resolve) => setTimeout(resolve, delayMs));
      return HttpResponse.json(next, { status: 200 });
    }),
  );
}

function failTrackList(status = 500) {
  server.use(
    http.get('*/api/movies/:id/subtitles', () => {
      listRequests += 1;
      return new HttpResponse(null, { status });
    }),
  );
}

function serveContent(
  options: { slowTrackId?: string; slowMs?: number; failTrackId?: string } = {},
) {
  server.use(
    http.get('*/api/movies/:id/subtitles/:trackId/content', async ({ request, params }) => {
      contentRequests.push(String(params.trackId));
      if (options.failTrackId && params.trackId === options.failTrackId) {
        return new HttpResponse(null, { status: 422 });
      }
      if (options.slowTrackId && params.trackId === options.slowTrackId) {
        await new Promise((resolve) => setTimeout(resolve, options.slowMs ?? 80));
      }
      void request;
      return new HttpResponse(VTT, {
        status: 200,
        headers: { 'Content-Type': 'text/vtt; charset=utf-8' },
      });
    }),
  );
}

function trackElement(trackId: string): HTMLTrackElement {
  const element = video().querySelector<HTMLTrackElement>(`track[data-track-id="${trackId}"]`);
  if (!element) throw new Error(`expected a rendered track for ${trackId}`);
  return element;
}

function modeOf(trackId: string): TextTrackMode {
  return trackElement(trackId).track?.mode as TextTrackMode;
}

beforeEach(() => {
  media = installMediaStubs();
  listRequests = 0;
  contentRequests = [];
  owned = [];
  serveTracks([ENGLISH, PORTUGUESE]);
  serveContent();

  let counter = 0;
  Object.defineProperty(URL, 'createObjectURL', {
    configurable: true,
    value: vi.fn(() => {
      counter += 1;
      const url = `blob:owned-${counter}`;
      owned.push(url);
      return url;
    }),
  });
  Object.defineProperty(URL, 'revokeObjectURL', {
    configurable: true,
    value: vi.fn((url: string) => {
      owned = owned.filter((candidate) => candidate !== url);
    }),
  });
});

afterEach(() => {
  Reflect.deleteProperty(URL, 'createObjectURL');
  Reflect.deleteProperty(URL, 'revokeObjectURL');
  media.restore();
  vi.restoreAllMocks();
});

describe('subtitle track list states', () => {
  it('hides the track list when the served list is a successful empty result', async () => {
    serveTracks([]);
    renderPlayer();

    await waitFor(() => expect(listRequests).toBeGreaterThan(0));
    await new Promise((resolve) => setTimeout(resolve, 20));
    await openPanel();
    expect(group()).toBeNull();
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(screen.queryByRole('button', { name: subtitlesRetryLabel('Dune') })).toBeNull();
  });

  it('shows honest loading copy while the served list is pending', async () => {
    serveTracks([ENGLISH, PORTUGUESE], 90);
    renderPlayer();

    fireEvent.click(ccTrigger());
    expect(await screen.findByText(SUBTITLES_LOADING_COPY)).toBeInTheDocument();
    await screen.findByRole('radiogroup', { name: subtitleSelectLabel('Dune') });
  });

  it('shows honest empty copy for a successful empty result', async () => {
    serveTracks([]);
    renderPlayer();

    await openPanel();
    expect(group()).toBeNull();
    expect(screen.getByText(SUBTITLES_EMPTY_COPY)).toBeInTheDocument();
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });

  it('offers Off plus served labels in served order, with duplicate counters preserved', async () => {
    serveTracks([
      { id: 'default', label: 'Default', language: 'und', isDefault: true },
      { id: 'dup-a', label: 'English (2)', language: 'en', isDefault: false },
      { id: 'dup-b', label: 'English (3)', language: 'en', isDefault: false },
    ]);
    renderPlayer();

    await openPanel();
    expect(trackLabels()).toEqual([SUBTITLES_OFF_COPY, 'Default', 'English (2)', 'English (3)']);
    expect(trackIds()).toEqual(['', 'default', 'dup-a', 'dup-b']);
  });

  it('starts Off even when the served list carries a default marker', async () => {
    renderPlayer();

    await openPanel();
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
    expect(modeOf(ENGLISH.id)).toBe('disabled');
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(contentRequests).toEqual([]);
  });

  it('keeps exactly one radio in the tab order', async () => {
    renderPlayer();

    await openPanel();
    expect(radios().filter((radio) => radio.getAttribute('tabindex') === '0')).toHaveLength(1);
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
  });

  it('never persists a selection and never auto-selects from the browser language', async () => {
    const getItem = vi.spyOn(Storage.prototype, 'getItem');
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    expect(getItem).not.toHaveBeenCalledWith(expect.stringMatching(/subtitle/i));
    const subtitleWrites = setItem.mock.calls.filter(([key]) => /subtitle/i.test(String(key)));
    expect(subtitleWrites).toEqual([]);
  });
});

describe('subtitle track list text-only labels', () => {
  it('renders a hostile basename literally without injecting markup', async () => {
    serveTracks([ENGLISH, HOSTILE]);
    renderPlayer();

    await openPanel();
    const option = within(group() as HTMLElement).getByRole('radio', { name: HOSTILE.label });
    expect(option).toHaveAttribute('data-track-id', HOSTILE.id);
    expect(option.querySelector('img')).toBeNull();
    expect((group() as HTMLElement).querySelector('img')).toBeNull();
    expect(within(region()).queryByRole('img')).toBeNull();
  });
});

describe('subtitle track modes', () => {
  it('shows only the selected track and keeps the others disabled', async () => {
    renderPlayer();

    await openPanel();
    choose(PORTUGUESE.label);

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    expect(modeOf(ENGLISH.id)).toBe('disabled');
    expect(checkedLabel()).toBe(PORTUGUESE.label);
    expect(video().querySelectorAll('track')).toHaveLength(2);
  });

  it('disables every track on Off', async () => {
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    choose(SUBTITLES_OFF_COPY);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
  });

  it('selects a track and returns to Off through keyboard activation alone', async () => {
    renderPlayer();

    await openPanel();
    const off = within(group() as HTMLElement).getByRole('radio', { name: SUBTITLES_OFF_COPY });
    off.focus();
    fireEvent.keyDown(off, { key: 'ArrowDown' });

    await waitFor(() => expect(checkedLabel()).toBe(ENGLISH.label));
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    const english = within(group() as HTMLElement).getByRole('radio', { name: ENGLISH.label });
    fireEvent.keyDown(english, { key: 'ArrowUp' });

    await waitFor(() => expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY));
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
  });

  it('re-disables a track the browser selects after Off', async () => {
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    choose(SUBTITLES_OFF_COPY);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));

    act(() => {
      trackElement(ENGLISH.id).track.mode = 'showing';
      video().textTracks.dispatchEvent(new Event('change'));
    });

    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
  });

  it('re-disables every non-selected track the browser selects after a switch', async () => {
    renderPlayer();

    await openPanel();
    choose(PORTUGUESE.label);
    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));

    act(() => {
      trackElement(ENGLISH.id).track.mode = 'showing';
      video().textTracks.dispatchEvent(new Event('addtrack'));
    });

    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
    expect(modeOf(PORTUGUESE.id)).toBe('showing');
  });

  it('re-disables the tracks when the browser re-selects on media load', async () => {
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    choose(SUBTITLES_OFF_COPY);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));

    act(() => {
      trackElement(PORTUGUESE.id).track.mode = 'showing';
      video().dispatchEvent(new Event('loadedmetadata'));
    });

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('disabled'));
    expect(modeOf(ENGLISH.id)).toBe('disabled');
  });

  it('requests content from the movie id and the opaque token only', async () => {
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);

    await waitFor(() => expect(contentRequests).toEqual([ENGLISH.id]));
    expect(contentRequests.join(' ')).not.toMatch(/dune|\.srt|\.vtt/i);
    await waitFor(() => expect(trackElement(ENGLISH.id).getAttribute('src')).toMatch(/^blob:/));
  });

  it('revokes its owned object URL on Off and on unmount', async () => {
    const { unmount } = renderPlayer();
    await openPanel();

    choose(ENGLISH.label);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));
    expect(owned).toHaveLength(1);

    choose(SUBTITLES_OFF_COPY);
    await waitFor(() => expect(owned).toEqual([]));

    choose(PORTUGUESE.label);
    await waitFor(() => expect(owned).toHaveLength(1));
    unmount();
    expect(owned).toEqual([]);
  });
});

describe('subtitle selection lifetime', () => {
  it('resets the selection to Off and revokes the owned URL when the movie changes', async () => {
    const { rerender } = renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));
    expect(owned).toHaveLength(1);

    serveTracks([ENGLISH]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    await openPanel('Heat');
    expect(checkedLabel('Heat')).toBe(SUBTITLES_OFF_COPY);
    const heatVideo = region('Heat').querySelector('video');
    expect(heatVideo?.querySelectorAll('track')).toHaveLength(1);
    expect(heatVideo?.querySelector('track')?.getAttribute('src')).toBeNull();
    expect(owned).toEqual([]);
  });
});

describe('subtitle content failure', () => {
  it('keeps the video playing, returns to Off, and allows retry by reselecting', async () => {
    serveContent({ failTrackId: ENGLISH.id });
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });

    await openPanel();
    media.calls.length = 0;
    choose(ENGLISH.label);

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
    expect(modeOf(ENGLISH.id)).toBe('disabled');
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(screen.queryByRole('alert')).toBeNull();
    expect(media.calls).not.toContain('pause');

    serveContent();
    choose(PORTUGUESE.label);
    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });

  it('keeps Off and releases every owned URL through a rapid track A/B/Off sequence', async () => {
    serveContent({ slowTrackId: ENGLISH.id, slowMs: 60 });
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    choose(PORTUGUESE.label);
    choose(SUBTITLES_OFF_COPY);

    await waitFor(() => expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY));
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(owned).toEqual([]);

    await new Promise((resolve) => setTimeout(resolve, 120));
    expect(owned).toEqual([]);
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });

  it('ignores a stale content success that resolves after a newer selection', async () => {
    serveContent({ slowTrackId: ENGLISH.id, slowMs: 90 });
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    choose(PORTUGUESE.label);

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    await new Promise((resolve) => setTimeout(resolve, 140));

    expect(checkedLabel()).toBe(PORTUGUESE.label);
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(owned).toHaveLength(1);
  });

  it('ignores a stale content failure from a replaced selection', async () => {
    serveContent({ failTrackId: ENGLISH.id, slowTrackId: ENGLISH.id, slowMs: 90 });
    renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    choose(PORTUGUESE.label);

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    await new Promise((resolve) => setTimeout(resolve, 140));

    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(checkedLabel()).toBe(PORTUGUESE.label);
  });
});

describe('subtitle list failure', () => {
  it('keeps the video playing, keeps Off, and offers explicit Retry instead of an empty list', async () => {
    failTrackList();
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });

    await openPanel();
    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(group()).toBeNull();
    expect(screen.getByRole('button', { name: subtitlesRetryLabel('Dune') })).toHaveTextContent(
      SUBTITLES_RETRY_COPY,
    );
    expect(media.calls).not.toContain('pause');
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('recovers the track list through Retry after a list failure', async () => {
    failTrackList();
    renderPlayer();

    await openPanel();
    await screen.findByText(SUBTITLES_UNAVAILABLE_COPY);
    serveTracks([ENGLISH, PORTUGUESE]);

    fireEvent.click(screen.getByRole('button', { name: subtitlesRetryLabel('Dune') }));

    await screen.findByRole('radiogroup', { name: subtitleSelectLabel('Dune') });
    expect(trackLabels()).toEqual([SUBTITLES_OFF_COPY, 'English', 'Portuguese (Brazil)']);
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });
});

describe('subtitle generation guard', () => {
  it('suppresses a stale list success from the replaced movie', async () => {
    await serveTracks([ENGLISH], 90);
    const { rerender } = renderPlayer();

    await serveTracks([PORTUGUESE]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    await openPanel('Heat');
    await new Promise((resolve) => setTimeout(resolve, 140));
    expect(trackIds('Heat')).toEqual(['', PORTUGUESE.id]);
    expect(within(region('Heat')).queryByText('English')).toBeNull();
  });

  it('suppresses a stale list error from the replaced movie', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles', async () => {
        await new Promise((resolve) => setTimeout(resolve, 90));
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const { rerender } = renderPlayer();

    await serveTracks([PORTUGUESE]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    await openPanel('Heat');
    await new Promise((resolve) => setTimeout(resolve, 160));
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });

  it('suppresses a stale list error after unmount', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles', async () => {
        await new Promise((resolve) => setTimeout(resolve, 40));
        return new HttpResponse(null, { status: 500 });
      }),
    );
    const { unmount } = renderPlayer();
    unmount();

    await new Promise((resolve) => setTimeout(resolve, 80));
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });

  it('drops a late list success that resolves after unmount', async () => {
    await serveTracks([ENGLISH, PORTUGUESE], 40);
    const { unmount } = renderPlayer();
    unmount();

    await new Promise((resolve) => setTimeout(resolve, 80));
    expect(group()).toBeNull();
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });
});

describe('subtitle stale content', () => {
  it('drops a late content failure after the movie changes', async () => {
    serveContent({ failTrackId: ENGLISH.id, slowTrackId: ENGLISH.id, slowMs: 90 });
    const { rerender } = renderPlayer();

    await openPanel();
    choose(ENGLISH.label);

    serveTracks([PORTUGUESE]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    await openPanel('Heat');
    expect(checkedLabel('Heat')).toBe(SUBTITLES_OFF_COPY);
    await new Promise((resolve) => setTimeout(resolve, 160));
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(checkedLabel('Heat')).toBe(SUBTITLES_OFF_COPY);
  });

  it('releases a late content success that resolves after unmount', async () => {
    serveContent({ slowTrackId: ENGLISH.id, slowMs: 90 });
    const { unmount } = renderPlayer();

    await openPanel();
    choose(ENGLISH.label);
    unmount();

    await new Promise((resolve) => setTimeout(resolve, 160));
    expect(owned).toEqual([]);
  });
});

describe('subtitle track keyboard operation inside the player surface', () => {
  it('selects a track then Off with the keyboard alone and never seeks', async () => {
    media = installMediaStubs();
    render(<BrowserPlayer title="Dune" movieId={1} streamUrl="/api/movies/1/stream" />);
    media.setSeekable(video(), [[0, 100]]);
    const panel = await openPanel();
    const rows = await within(panel).findAllByRole('radio');
    const names = rows.map((row) => row.getAttribute('aria-label'));
    expect(names).toEqual([SUBTITLES_OFF_COPY, 'English', 'Portuguese (Brazil)']);

    rows[1].focus();
    fireEvent.keyDown(rows[1], { key: 'Enter' });

    await waitFor(() => expect(modeOf('token-en')).toBe('showing'));
    expect(video().currentTime).toBe(0);

    const refreshed = await within(panel).findAllByRole('radio');
    refreshed[2].focus();
    fireEvent.keyDown(refreshed[2], { key: ' ' });

    await waitFor(() => expect(modeOf('token-pob')).toBe('showing'));
    expect(modeOf('token-en')).toBe('disabled');
    expect(video().currentTime).toBe(0);

    const off = (await within(panel).findAllByRole('radio'))[0];
    off.focus();
    fireEvent.keyDown(off, { key: ' ' });

    await waitFor(() => expect(modeOf('token-pob')).toBe('disabled'));
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
    expect(video().currentTime).toBe(0);
  });

  it('moves and selects with the arrow keys while the video keeps its position', async () => {
    media = installMediaStubs();
    render(<BrowserPlayer title="Dune" movieId={1} streamUrl="/api/movies/1/stream" />);
    media.setSeekable(video(), [[0, 100]]);
    const panel = await openPanel();
    const rows = await within(panel).findAllByRole('radio');

    rows[0].focus();
    fireEvent.keyDown(rows[0], { key: 'ArrowDown' });
    await waitFor(() => expect(checkedLabel()).toBe('English'));
    expect(video().currentTime).toBe(0);

    const moved = (await within(panel).findAllByRole('radio'))[1];
    fireEvent.keyDown(moved, { key: 'ArrowDown' });
    await waitFor(() => expect(checkedLabel()).toBe('Portuguese (Brazil)'));
    expect(video().currentTime).toBe(0);
  });
});
