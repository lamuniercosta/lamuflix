import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ComponentProps } from 'react';
import BrowserPlayer from './BrowserPlayer';
import { playerRegionLabel } from './playerCopy';
import {
  SUBTITLES_OFF_COPY,
  SUBTITLES_RETRY_COPY,
  SUBTITLES_UNAVAILABLE_COPY,
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

function selector(title = 'Dune'): HTMLSelectElement | null {
  return screen.queryByRole('combobox', {
    name: subtitleSelectLabel(title),
  }) as HTMLSelectElement | null;
}

function serveTracks(next: SubtitleTrackDto[], delayMs = 0) {
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

/** Serves WebVTT for every content request, optionally delaying one specific track. */
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

describe('subtitle selector list states', () => {
  it('hides the selector when the served list is a successful empty result', async () => {
    serveTracks([]);
    renderPlayer();

    await waitFor(() => expect(listRequests).toBeGreaterThan(0));
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(selector()).toBeNull();
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(screen.queryByRole('button', { name: subtitlesRetryLabel('Dune') })).toBeNull();
  });

  it('offers Off plus served labels in served order, with duplicate counters preserved', async () => {
    serveTracks([
      { id: 'default', label: 'Default', language: 'und', isDefault: true },
      { id: 'dup-a', label: 'English (2)', language: 'en', isDefault: false },
      { id: 'dup-b', label: 'English (3)', language: 'en', isDefault: false },
    ]);
    renderPlayer();

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;
    expect(Array.from(control.options).map((option) => option.textContent)).toEqual([
      SUBTITLES_OFF_COPY,
      'Default',
      'English (2)',
      'English (3)',
    ]);
    expect(Array.from(control.options).map((option) => option.value)).toEqual([
      '',
      'default',
      'dup-a',
      'dup-b',
    ]);
  });

  it('starts Off even when the served list carries a default marker', async () => {
    renderPlayer();

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;
    expect(control.value).toBe('');
    expect(modeOf(ENGLISH.id)).toBe('disabled');
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(contentRequests).toEqual([]);
  });

  it('never persists a selection and never auto-selects from the browser language', async () => {
    const getItem = vi.spyOn(Storage.prototype, 'getItem');
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    expect(getItem).not.toHaveBeenCalledWith(expect.stringMatching(/subtitle/i));
    const subtitleWrites = setItem.mock.calls.filter(([key]) => /subtitle/i.test(String(key)));
    expect(subtitleWrites).toEqual([]);
  });
});

describe('subtitle selector text-only labels', () => {
  it('renders a hostile basename literally without injecting markup', async () => {
    serveTracks([ENGLISH, HOSTILE]);
    renderPlayer();

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;
    const option = Array.from(control.options).find((item) => item.value === HOSTILE.id);
    expect(option?.textContent).toBe(HOSTILE.label);
    expect(option?.querySelector('img')).toBeNull();
    expect(control.querySelector('img')).toBeNull();
    expect(within(region()).queryByRole('img')).toBeNull();
  });
});

describe('subtitle track modes', () => {
  it('shows only the selected track and keeps the others disabled', async () => {
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: PORTUGUESE.id } });

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    expect(modeOf(ENGLISH.id)).toBe('disabled');
    expect(video().querySelectorAll('track')).toHaveLength(2);
  });

  it('disables every track on Off', async () => {
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    fireEvent.change(selector() as HTMLSelectElement, { target: { value: '' } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(selector()?.value).toBe('');
  });

  it('re-disables a track the browser selects after Off', async () => {
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    fireEvent.change(selector() as HTMLSelectElement, { target: { value: '' } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));

    act(() => {
      trackElement(ENGLISH.id).track.mode = 'showing';
      video().textTracks.dispatchEvent(new Event('change'));
    });

    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('disabled'));
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(selector()?.value).toBe('');
  });

  it('re-disables every non-selected track the browser selects after a switch', async () => {
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: PORTUGUESE.id } });
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

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));

    fireEvent.change(selector() as HTMLSelectElement, { target: { value: '' } });
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

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });

    await waitFor(() => expect(contentRequests).toEqual([ENGLISH.id]));
    expect(contentRequests.join(' ')).not.toMatch(/dune|\.srt|\.vtt/i);
    await waitFor(() => expect(trackElement(ENGLISH.id).getAttribute('src')).toMatch(/^blob:/));
  });

  it('revokes its owned object URL on Off and on unmount', async () => {
    const { unmount } = renderPlayer();
    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });

    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));
    expect(owned).toHaveLength(1);

    fireEvent.change(selector() as HTMLSelectElement, { target: { value: '' } });
    await waitFor(() => expect(owned).toEqual([]));

    fireEvent.change(selector() as HTMLSelectElement, { target: { value: PORTUGUESE.id } });
    await waitFor(() => expect(owned).toHaveLength(1));
    unmount();
    expect(owned).toEqual([]);
  });
});

describe('subtitle selection lifetime', () => {
  it('resets the selection to Off and revokes the owned URL when the movie changes', async () => {
    const { rerender } = renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    await waitFor(() => expect(modeOf(ENGLISH.id)).toBe('showing'));
    expect(owned).toHaveLength(1);

    serveTracks([ENGLISH]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Heat'),
    })) as HTMLSelectElement;
    expect(control.value).toBe('');
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

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    media.calls.length = 0;
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(selector()?.value).toBe('');
    expect(modeOf(ENGLISH.id)).toBe('disabled');
    expect(modeOf(PORTUGUESE.id)).toBe('disabled');
    expect(screen.queryByRole('alert')).toBeNull();
    expect(media.calls).not.toContain('pause');

    serveContent();
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: PORTUGUESE.id } });
    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });

  it('ignores a stale content success that resolves after a newer selection', async () => {
    serveContent({ slowTrackId: ENGLISH.id, slowMs: 90 });
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: PORTUGUESE.id } });

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    await new Promise((resolve) => setTimeout(resolve, 140));

    expect(selector()?.value).toBe(PORTUGUESE.id);
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(owned).toHaveLength(1);
  });

  it('ignores a stale content failure from a replaced selection', async () => {
    serveContent({ failTrackId: ENGLISH.id, slowTrackId: ENGLISH.id, slowMs: 90 });
    renderPlayer();

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Dune') });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: ENGLISH.id } });
    fireEvent.change(selector() as HTMLSelectElement, { target: { value: PORTUGUESE.id } });

    await waitFor(() => expect(modeOf(PORTUGUESE.id)).toBe('showing'));
    await new Promise((resolve) => setTimeout(resolve, 140));

    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
    expect(selector()?.value).toBe(PORTUGUESE.id);
  });
});

describe('subtitle list failure', () => {
  it('keeps the video playing, keeps Off, and offers explicit Retry instead of an empty selector', async () => {
    failTrackList();
    renderPlayer();
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(selector()).toBeNull();
    expect(screen.getByRole('button', { name: subtitlesRetryLabel('Dune') })).toHaveTextContent(
      SUBTITLES_RETRY_COPY,
    );
    expect(media.calls).not.toContain('pause');
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('recovers the selector through Retry after a list failure', async () => {
    failTrackList();
    renderPlayer();

    await screen.findByText(SUBTITLES_UNAVAILABLE_COPY);
    serveTracks([ENGLISH, PORTUGUESE]);

    fireEvent.click(screen.getByRole('button', { name: subtitlesRetryLabel('Dune') }));

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;
    expect(Array.from(control.options).map((option) => option.textContent)).toEqual([
      SUBTITLES_OFF_COPY,
      'English',
      'Portuguese (Brazil)',
    ]);
    expect(control.value).toBe('');
    expect(screen.queryByText(SUBTITLES_UNAVAILABLE_COPY)).toBeNull();
  });
});

describe('subtitle generation guard', () => {
  it('suppresses a stale list success from the replaced movie', async () => {
    serveTracks([ENGLISH], 90);
    const { rerender } = renderPlayer();

    serveTracks([PORTUGUESE]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Heat'),
    })) as HTMLSelectElement;
    await new Promise((resolve) => setTimeout(resolve, 140));
    expect(Array.from(control.options).map((option) => option.value)).toEqual(['', PORTUGUESE.id]);
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

    serveTracks([PORTUGUESE]);
    rerender(<BrowserPlayer title="Heat" movieId={2} streamUrl="/api/movies/2/stream" />);

    await screen.findByRole('combobox', { name: subtitleSelectLabel('Heat') });
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
});
