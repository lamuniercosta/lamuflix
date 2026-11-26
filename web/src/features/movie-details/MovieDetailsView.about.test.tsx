import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import { browseMovies, movieDetails, movieDetailsNullMetadata, people } from '../../mocks/fixtures';
import { __resetMovieFeedbackStore } from '../../library/movieFeedbackStore';
import { openLocalLabel, playerRegionLabel, playLabel } from '../playback/playerCopy';
import { LAUNCH_STARTED_COPY } from './movieDetailsLaunch';
import MovieDetailsView from './MovieDetailsView';

function enrichedMetadata(overrides: Record<string, unknown> = {}) {
  return {
    ...movieDetails.metadata!,
    directors: ['Denis Villeneuve', 'Jon Spaihts'],
    actors: ['Timothée Chalamet', 'Rebecca Ferguson'],
    writers: ['Frank Herbert', 'Jon Spaihts'],
    genres: ['Science fiction', 'Adventure'],
    collectionName: 'Dune Saga',
    ...overrides,
  };
}

function useDetails(metadata: object | null) {
  server.use(http.get('*/api/movies/:id', () => HttpResponse.json({ ...movieDetails, metadata })));
}

function renderView() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
}

async function aboutRegion() {
  return screen.findByRole('region', { name: 'About Dune' });
}

function rows(section: HTMLElement) {
  const list = section.querySelector('dl') as HTMLElement;
  expect(list.tagName).toBe('DL');
  expect(list.getAttribute('aria-labelledby')).toBe(
    within(section).getByRole('heading', { name: 'About Dune' }).id,
  );
  return {
    list,
    labels: within(list)
      .getAllByRole('term')
      .map((term) => term.textContent),
    values: within(list)
      .getAllByRole('definition')
      .map((value) => value.textContent),
    definitions: within(list).getAllByRole('definition'),
  };
}

function capabilities(enabled: boolean) {
  return http.get('*/api/playback/capabilities', () =>
    HttpResponse.json({ localPlayEnabled: enabled }),
  );
}

let media: MediaStubHandle;

beforeEach(() => {
  __resetMovieFeedbackStore();
  media = installMediaStubs();
});

afterEach(() => {
  media.restore();
  window.history.replaceState({}, '', '/');
});

describe('MovieDetailsView About section', () => {
  it('renders every eligible row in ticket order with plural crew labels', async () => {
    useDetails(enrichedMetadata());
    renderView();

    const section = await aboutRegion();
    expect(within(section).getByRole('heading', { name: 'About Dune', level: 2 })).toBeVisible();
    const { labels, values } = rows(section);
    expect(labels).toEqual(['Directors', 'Cast', 'Writers', 'Genres', 'Collection']);
    expect(values).toEqual([
      'Denis Villeneuve, Jon Spaihts',
      'Timothée Chalamet, Rebecca Ferguson',
      'Frank Herbert, Jon Spaihts',
      'Science fiction, Adventure',
      'Dune Saga',
    ]);
  });

  it('uses singular Director and Writer labels for one displayed name each', async () => {
    useDetails(
      enrichedMetadata({
        directors: ['Denis Villeneuve'],
        writers: ['Frank Herbert'],
      }),
    );
    renderView();

    const { labels, values } = rows(await aboutRegion());
    expect(labels).toEqual(['Director', 'Cast', 'Writer', 'Genres', 'Collection']);
    expect(values[0]).toBe('Denis Villeneuve');
    expect(values[2]).toBe('Frank Herbert');
  });

  it('hides missing writers and absent collection while keeping other rows', async () => {
    useDetails(
      enrichedMetadata({
        writers: [],
        genres: ['Drama'],
        collectionName: null,
      }),
    );
    renderView();

    const { labels, values } = rows(await aboutRegion());
    expect(labels).toEqual(['Directors', 'Cast', 'Genres']);
    expect(values[2]).toBe('Drama');
  });

  it('omits blank list members and counts only displayed names', async () => {
    useDetails(
      enrichedMetadata({
        directors: ['  ', 'Denis Villeneuve', ''],
        writers: ['', 'Frank Herbert', '   '],
        actors: ['  ', 'Timothée Chalamet'],
        genres: ['', '   '],
        collectionName: null,
      }),
    );
    renderView();

    const { labels, values } = rows(await aboutRegion());
    expect(labels).toEqual(['Director', 'Cast', 'Writer']);
    expect(values).toEqual(['Denis Villeneuve', 'Timothée Chalamet', 'Frank Herbert']);
  });

  it('omits the whole section when no About row is eligible', async () => {
    useDetails(
      enrichedMetadata({
        directors: ['', '  '],
        actors: [],
        writers: [],
        genres: [],
        collectionName: null,
      }),
    );
    renderView();

    expect(await screen.findByRole('heading', { name: 'Dune', level: 1 })).toBeVisible();
    expect(screen.queryByRole('region', { name: 'About Dune' })).toBeNull();
  });

  it('leaves the existing no-metadata notice in place and shows no About', async () => {
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));
    renderView();

    expect(await screen.findByText('No metadata available yet.')).toBeVisible();
    expect(screen.queryByRole('region', { name: 'About Dune' })).toBeNull();
  });

  it('opens the existing More with panel from a mapped About cast name only', async () => {
    useDetails(
      enrichedMetadata({
        actors: ['Timothée Chalamet', 'Unknown Actor'],
        collectionName: null,
      }),
    );
    server.use(http.get('*/api/people', () => HttpResponse.json(people)));
    renderView();

    const section = await aboutRegion();
    const mapped = within(section).getByRole('button', { name: 'Timothée Chalamet' });
    expect(within(section).queryByRole('button', { name: 'Unknown Actor' })).toBeNull();
    expect(within(section).getByText('Unknown Actor')).toBeVisible();

    fireEvent.click(mapped);

    expect(
      await screen.findByRole('heading', {
        name: 'More with Timothée Chalamet in your library',
      }),
    ).toBeVisible();
  });

  it('keeps hero cast selection opening the same panel', async () => {
    useDetails(
      enrichedMetadata({
        actors: ['Timothée Chalamet'],
        collectionName: null,
      }),
    );
    server.use(http.get('*/api/people', () => HttpResponse.json(people)));
    const { container } = renderView();

    await screen.findByRole('heading', { name: 'Dune', level: 1 });
    const hero = container.querySelector('.details-hero') as HTMLElement;
    const heroButton = await within(hero).findByRole('button', { name: 'Timothée Chalamet' });
    fireEvent.click(heroButton);

    expect(
      await screen.findByRole('heading', {
        name: 'More with Timothée Chalamet in your library',
      }),
    ).toBeVisible();
  });

  it('places About after actions, player and launch feedback, before the trailer', async () => {
    window.history.replaceState({ playerIntent: true }, '', '/movies/1');
    useDetails(enrichedMetadata());
    server.use(
      capabilities(true),
      http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 204 })),
    );
    renderView();

    const actions = await screen.findByRole('button', { name: playLabel('Dune') });
    const player = await screen.findByRole('region', { name: playerRegionLabel('Dune') });
    fireEvent.click(screen.getByRole('button', { name: openLocalLabel('Dune') }));
    const launchFeedback = await screen.findByText(LAUNCH_STARTED_COPY);
    const section = await aboutRegion();
    const trailer = await screen.findByRole('region', { name: 'Trailer' });

    const follows = (before: Element, after: Element) =>
      Boolean(before.compareDocumentPosition(after) & Node.DOCUMENT_POSITION_FOLLOWING);
    expect(follows(actions, player)).toBe(true);
    expect(follows(player, launchFeedback)).toBe(true);
    expect(follows(launchFeedback, section)).toBe(true);
    expect(follows(section, trailer)).toBe(true);
  });

  it('keeps the existing error path for malformed metadata fields', async () => {
    useDetails(enrichedMetadata({ writers: 'not an array' }));
    renderView();

    expect(await screen.findByText("Couldn't load this movie. Try again later.")).toBeVisible();
    expect(screen.queryByRole('region', { name: 'About Dune' })).toBeNull();
  });
});

describe('MovieDetailsView About selection isolation', () => {
  it('does not affect the browse filter when an About cast name is selected', async () => {
    const sent: string[][] = [];
    useDetails(
      enrichedMetadata({
        actors: ['Timothée Chalamet'],
        collectionName: null,
      }),
    );
    server.use(
      http.get('*/api/people', () => HttpResponse.json(people)),
      http.get('*/api/movies', ({ request }) => {
        sent.push(new URL(request.url).searchParams.getAll('ActorIds'));
        return HttpResponse.json(browseMovies);
      }),
    );
    renderView();

    const section = await aboutRegion();
    fireEvent.click(within(section).getByRole('button', { name: 'Timothée Chalamet' }));

    await screen.findByRole('heading', {
      name: 'More with Timothée Chalamet in your library',
    });
    expect(sent).toContainEqual(['1']);
  });
});
