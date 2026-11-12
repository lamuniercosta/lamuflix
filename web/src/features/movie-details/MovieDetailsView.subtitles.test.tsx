import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails } from '../../mocks/fixtures';
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import { playerRegionLabel, playLabel } from '../playback/playerCopy';
import {
  SUBTITLES_LABEL,
  SUBTITLES_OFF_COPY,
  SUBTITLES_RETRY_COPY,
  SUBTITLES_UNAVAILABLE_COPY,
  subtitlePanelLabel,
  subtitleSelectLabel,
  subtitlesRetryLabel,
} from '../playback/subtitleCopy';
import MovieDetailsView from './MovieDetailsView';

const fullMovie = {
  ...movieDetails,
  metadata: { ...movieDetails.metadata!, posterUrl: null, actors: [], directors: [] },
};

const VTT = 'WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello.\n';

const TRACKS = [
  { id: 'token-en', label: 'English', language: 'en', isDefault: true },
  { id: 'token-pob', label: 'Portuguese (Brazil)', language: 'pt-BR', isDefault: false },
];

let media: MediaStubHandle;

function renderView() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
}

function region(): HTMLElement {
  return screen.getByRole('region', { name: playerRegionLabel('Dune') });
}

function video(): HTMLVideoElement {
  const element = region().querySelector('video');
  if (!element) throw new Error('expected the player to render one video');
  return element;
}

function trackGroup(): HTMLElement | null {
  return screen.queryByRole('radiogroup', { name: subtitleSelectLabel('Dune') });
}

function ccTrigger(): HTMLElement {
  return within(region()).getByRole('button', { name: SUBTITLES_LABEL });
}

async function openCcPanel(): Promise<HTMLElement> {
  const trigger = ccTrigger();
  if (trigger.getAttribute('aria-expanded') !== 'true') fireEvent.click(trigger);
  const panel = await screen.findByRole('region', { name: subtitlePanelLabel('Dune') });
  await waitFor(() => expect(panel.querySelector('[aria-busy="true"]')).toBeNull());
  return panel;
}

function trackLabels(): string[] {
  const group = trackGroup();
  if (!group) return [];
  return within(group)
    .getAllByRole('radio')
    .map((radio) => radio.getAttribute('aria-label') ?? '');
}

function checkedLabel(): string | null {
  const group = trackGroup();
  if (!group) return null;
  const checked = within(group)
    .getAllByRole('radio')
    .find((radio) => radio.getAttribute('aria-checked') === 'true');
  return checked?.getAttribute('aria-label') ?? null;
}

function choose(label: string): void {
  const group = trackGroup();
  if (!group) throw new Error('expected the subtitle radiogroup to be rendered');
  fireEvent.click(within(group).getByRole('radio', { name: label }));
}

function modeOf(trackId: string): TextTrackMode | undefined {
  const element = video().querySelector<HTMLTrackElement>(`track[data-track-id="${trackId}"]`);
  return element?.track?.mode;
}

function serveSubtitles() {
  server.use(
    http.get('*/api/movies/:id/subtitles', () => HttpResponse.json(TRACKS, { status: 200 })),
    http.get(
      '*/api/movies/:id/subtitles/:trackId/content',
      () =>
        new HttpResponse(VTT, {
          status: 200,
          headers: { 'Content-Type': 'text/vtt; charset=utf-8' },
        }),
    ),
  );
}

beforeEach(() => {
  media = installMediaStubs();
  server.use(http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)));
});

afterEach(() => {
  media.restore();
  window.history.replaceState({}, '', '/');
});

describe('MovieDetailsView subtitle track list at the DEV-455 viewer seam', () => {
  it('offers Off plus served tracks once the browser player is open', async () => {
    serveSubtitles();
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });

    await openCcPanel();
    expect(trackLabels()).toEqual([SUBTITLES_OFF_COPY, 'English', 'Portuguese (Brazil)']);
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
  }, 15000);

  it('shows only the chosen track and returns to Off on request', async () => {
    serveSubtitles();
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await openCcPanel();

    choose('Portuguese (Brazil)');
    await waitFor(() => expect(modeOf('token-pob')).toBe('showing'));
    expect(modeOf('token-en')).toBe('disabled');
    expect(checkedLabel()).toBe('Portuguese (Brazil)');

    choose(SUBTITLES_OFF_COPY);
    await waitFor(() => expect(modeOf('token-pob')).toBe('disabled'));
    expect(modeOf('token-en')).toBe('disabled');
    expect(checkedLabel()).toBe(SUBTITLES_OFF_COPY);
  });

  it('keeps the browser player and its in-bar Play control during a content failure', async () => {
    serveSubtitles();
    server.use(
      http.get(
        '*/api/movies/:id/subtitles/:trackId/content',
        () => new HttpResponse(null, { status: 422 }),
      ),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await openCcPanel();

    choose('English');

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(within(region()).getByRole('button', { name: SUBTITLES_LABEL })).toBeInTheDocument();
    expect(region().querySelectorAll('media-play-button')).toHaveLength(1);
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('offers list Retry without hiding the player or pretending the list is empty', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles', () => new HttpResponse(null, { status: 500 })),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    await openCcPanel();

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    const retry = within(region()).getByRole('button', { name: subtitlesRetryLabel('Dune') });
    expect(retry).toHaveTextContent(SUBTITLES_RETRY_COPY);
    expect(trackGroup()).toBeNull();
  });

  it('does not disturb video playback when a track is chosen or turned off', async () => {
    serveSubtitles();
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    await act(async () => {
      video().dispatchEvent(new Event('playing'));
    });
    media.calls.length = 0;

    await openCcPanel();
    choose('English');
    await waitFor(() => expect(modeOf('token-en')).toBe('showing'));

    choose(SUBTITLES_OFF_COPY);
    await waitFor(() => expect(modeOf('token-en')).toBe('disabled'));

    expect(media.calls.filter((call) => call === 'pause')).toHaveLength(0);
    expect(media.calls.filter((call) => call.startsWith('src:'))).toHaveLength(0);
  });
});
