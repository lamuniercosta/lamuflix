import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails } from '../../mocks/fixtures';
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import {
  ERROR_COPY,
  OPEN_LOCAL_COPY,
  playerRegionLabel,
  openLocalLabel,
  retryLabel,
} from '../playback/playerCopy';
import {
  LAUNCH_FAILED_COPY,
  LAUNCH_NOT_FOUND_COPY,
  LAUNCH_STARTED_COPY,
  LAUNCH_UNAVAILABLE_COPY,
} from './movieDetailsLaunch';
import MovieDetailsView from './MovieDetailsView';

const fullMovie = {
  ...movieDetails,
  metadata: { ...movieDetails.metadata!, posterUrl: null, actors: [], directors: [] },
};

function renderView(movieId: number | string = 1) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MovieDetailsView movieId={movieId} />
    </QueryClientProvider>,
  );
}

function capabilities(enabled: boolean) {
  return http.get('*/api/playback/capabilities', () =>
    HttpResponse.json({ localPlayEnabled: enabled }),
  );
}

function recordLaunchPosts(posts: string[]) {
  return http.post('*/api/movies/:id/play', ({ request }) => {
    posts.push(request.url);
    return new HttpResponse(null, { status: 204 });
  });
}

function secondaryButton(title = 'Dune') {
  return screen.queryByRole('button', { name: openLocalLabel(title) });
}

let media: MediaStubHandle;

beforeEach(() => {
  media = installMediaStubs();
});

afterEach(() => {
  media.restore();
  window.history.replaceState({}, '', '/');
});

describe('MovieDetailsView Open in BS Player visibility', () => {
  it('offers the secondary action when capability is true', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
    );
    renderView();

    const secondary = await screen.findByRole('button', { name: openLocalLabel('Dune') });
    expect(secondary).toHaveTextContent(OPEN_LOCAL_COPY);
    expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument();
  });

  it('hides the secondary action when capability is false', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(false),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    await waitFor(() => expect(secondaryButton()).toBeNull());
    expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument();
  });

  it('hides the secondary action while capability is loading and keeps browser Play', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.get('*/api/playback/capabilities', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100));
        return HttpResponse.json({ localPlayEnabled: true });
      }),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    expect(secondaryButton()).toBeNull();
    expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: openLocalLabel('Dune') })).toBeInTheDocument();
  });

  it('hides the secondary action when the capability request fails', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      http.get('*/api/playback/capabilities', () => new HttpResponse(null, { status: 500 })),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    await waitFor(() => expect(secondaryButton()).toBeNull());
    expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument();
  });
});

describe('MovieDetailsView Open in BS Player launches local playback', () => {
  it('posts the launch contract only from the secondary action', async () => {
    const posts: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      recordLaunchPosts(posts),
    );
    renderView();

    await screen.findByRole('heading', { name: 'Dune' });
    fireEvent.click(screen.getByRole('button', { name: 'Play' }));
    await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    expect(posts).toEqual([]);

    fireEvent.click(screen.getByRole('button', { name: openLocalLabel('Dune') }));
    await waitFor(() => expect(posts).toEqual(['http://localhost:3000/api/movies/1/play']));
  });

  it('reports playback started on 204', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 204 })),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: openLocalLabel('Dune') }));

    expect(await screen.findByText(LAUNCH_STARTED_COPY)).toBeInTheDocument();
  });
});

describe('MovieDetailsView Open in BS Player preserves launch status meanings', () => {
  it.each([
    [403, LAUNCH_UNAVAILABLE_COPY],
    [404, LAUNCH_NOT_FOUND_COPY],
    [409, LAUNCH_FAILED_COPY],
    [422, LAUNCH_FAILED_COPY],
    [500, LAUNCH_FAILED_COPY],
  ])('reports the preserved meaning for %i', async (status, copy) => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status })),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: openLocalLabel('Dune') }));

    expect(await screen.findByText(copy)).toBeInTheDocument();
  });

  it('reports a generic failure on a network error', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      http.post('*/api/movies/:id/play', () => HttpResponse.error()),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: openLocalLabel('Dune') }));

    expect(await screen.findByText(LAUNCH_FAILED_COPY)).toBeInTheDocument();
  });

  it('does not send a second launch request while one is in flight', async () => {
    let calls = 0;
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      http.post('*/api/movies/:id/play', async () => {
        calls += 1;
        await new Promise((resolve) => setTimeout(resolve, 100));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderView();

    const secondary = await screen.findByRole('button', { name: openLocalLabel('Dune') });
    fireEvent.click(secondary);
    fireEvent.click(secondary);

    expect(await screen.findByText(LAUNCH_STARTED_COPY)).toBeInTheDocument();
    expect(calls).toBe(1);
  });
});

describe('MovieDetailsView browser failure offers explicit local recovery', () => {
  it('offers the local action inside the terminal error when capability is true', async () => {
    const posts: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      recordLaunchPosts(posts),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Play' }));
    const region = await screen.findByRole('region', { name: playerRegionLabel('Dune') });

    await act(async () => {
      region.querySelector('video')?.dispatchEvent(new Event('error'));
    });

    expect(await screen.findByText(ERROR_COPY)).toBeInTheDocument();
    expect(posts).toEqual([]);

    const recovery = within(region).getByRole('button', { name: openLocalLabel('Dune') });
    fireEvent.click(recovery);
    await waitFor(() => expect(posts).toEqual(['http://localhost:3000/api/movies/1/play']));
  });

  it('hides the local recovery action when capability is false', async () => {
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(false),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Play' }));
    const region = await screen.findByRole('region', { name: playerRegionLabel('Dune') });

    await act(async () => {
      region.querySelector('video')?.dispatchEvent(new Event('error'));
    });

    expect(await screen.findByText(ERROR_COPY)).toBeInTheDocument();
    expect(within(region).queryByRole('button', { name: openLocalLabel('Dune') })).toBeNull();
  });

  it('never auto-invokes local playback from a browser failure', async () => {
    const posts: string[] = [];
    server.use(
      http.get('*/api/movies/:id', () => HttpResponse.json(fullMovie)),
      capabilities(true),
      recordLaunchPosts(posts),
    );
    renderView();

    fireEvent.click(await screen.findByRole('button', { name: 'Play' }));
    const region = await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    await act(async () => {
      region.querySelector('video')?.dispatchEvent(new Event('error'));
    });
    await screen.findByText(ERROR_COPY);

    await new Promise((resolve) => setTimeout(resolve, 30));
    expect(posts).toEqual([]);
    expect(within(region).getByRole('button', { name: retryLabel('Dune') })).toBeInTheDocument();
  });
});
