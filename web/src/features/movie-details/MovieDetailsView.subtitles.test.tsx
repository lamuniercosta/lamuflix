import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails } from '../../mocks/fixtures';
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import { playerRegionLabel, playLabel } from '../playback/playerCopy';
import {
  SUBTITLES_OFF_COPY,
  SUBTITLES_RETRY_COPY,
  SUBTITLES_UNAVAILABLE_COPY,
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

describe('MovieDetailsView subtitle selector at the DEV-455 viewer seam', () => {
  it('offers Off plus served tracks once the browser player is open', async () => {
    serveSubtitles();
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;
    expect(Array.from(control.options).map((option) => option.textContent)).toEqual([
      SUBTITLES_OFF_COPY,
      'English',
      'Portuguese (Brazil)',
    ]);
    expect(control.value).toBe('');
  }, 15000);

  it('shows only the chosen track and returns to Off on request', async () => {
    serveSubtitles();
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;

    fireEvent.change(control, { target: { value: 'token-pob' } });
    await waitFor(() => expect(modeOf('token-pob')).toBe('showing'));
    expect(modeOf('token-en')).toBe('disabled');

    fireEvent.change(control, { target: { value: '' } });
    await waitFor(() => expect(modeOf('token-pob')).toBe('disabled'));
    expect(modeOf('token-en')).toBe('disabled');
  });

  it('keeps the browser player and its Play control during a content failure', async () => {
    serveSubtitles();
    server.use(
      http.get(
        '*/api/movies/:id/subtitles/:trackId/content',
        () => new HttpResponse(null, { status: 422 }),
      ),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;

    fireEvent.change(control, { target: { value: 'token-en' } });

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(within(region()).getByRole('button', { name: playLabel('Dune') })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('offers list Retry without hiding the player or pretending the list is empty', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles', () => new HttpResponse(null, { status: 500 })),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: playLabel('Dune') }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });

    expect(await screen.findByText(SUBTITLES_UNAVAILABLE_COPY)).toBeInTheDocument();
    const retry = within(region()).getByRole('button', { name: subtitlesRetryLabel('Dune') });
    expect(retry).toHaveTextContent(SUBTITLES_RETRY_COPY);
    expect(screen.queryByRole('combobox', { name: subtitleSelectLabel('Dune') })).toBeNull();
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

    const control = (await screen.findByRole('combobox', {
      name: subtitleSelectLabel('Dune'),
    })) as HTMLSelectElement;
    fireEvent.change(control, { target: { value: 'token-en' } });
    await waitFor(() => expect(modeOf('token-en')).toBe('showing'));

    fireEvent.change(control, { target: { value: '' } });
    await waitFor(() => expect(modeOf('token-en')).toBe('disabled'));

    expect(media.calls.filter((call) => call === 'pause')).toHaveLength(0);
    expect(media.calls.filter((call) => call.startsWith('src:'))).toHaveLength(0);
  });
});
