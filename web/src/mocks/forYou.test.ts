import { afterEach, describe, expect, it, vi } from 'vitest';
import { createApiClient } from '../api/client';
import { forYouRecommendations } from './fixtures';

const client = createApiClient('https://test.local');

afterEach(() => {
  vi.restoreAllMocks();
});

describe('generated for-you recommendations handler', () => {
  it('answers the for-you route with the typed fixture for take 10', async () => {
    // act
    const { data, response } = await client.GET('/api/recommendations/for-you', {
      params: { query: { take: 10 } },
    });

    // assert
    expect(response.status).toBe(200);
    expect(data).toEqual(forYouRecommendations);
  });

  it('preserves the returned order of the typed fixture', async () => {
    // act
    const { data } = await client.GET('/api/recommendations/for-you', {
      params: { query: { take: 10 } },
    });

    // assert
    expect(data?.items.map((movie) => movie.id)).toEqual(
      forYouRecommendations.items.map((movie) => movie.id),
    );
    expect(data?.items.map((movie) => movie.score)).toEqual([0.91, 0.84, 0.72, 0.61]);
  });

  it('carries seeded, evidence, empty-evidence and nullable-poster rows', async () => {
    // act
    const { data } = await client.GET('/api/recommendations/for-you', {
      params: { query: { take: 10 } },
    });

    // assert
    expect(data?.items[0].seed).toEqual({ movieId: 2, title: 'Dune', signal: 'watched' });
    expect(data?.items[0].evidence).toEqual([
      {
        criterion: 'Director',
        text: 'Same director: Denis Villeneuve',
        matchedNames: ['Denis Villeneuve'],
      },
      {
        criterion: 'Cast',
        text: 'Shared cast: Name One, Name Two, Name Three +2',
        matchedNames: ['Name One', 'Name Two', 'Name Three', 'Name Four', 'Name Five'],
      },
    ]);
    expect(data?.items[1].evidence).toEqual([]);
    expect(data?.items[1].seed).toEqual({ movieId: 1, title: 'Dune', signal: 'liked' });
    expect(data?.items[2].evidence).toEqual([
      { criterion: 'Genre', text: 'Same genres: Drama', matchedNames: ['Drama'] },
    ]);
    expect(data?.items[2].seed).toEqual({ movieId: 1, title: 'Dune', signal: 'watchlist' });
    expect(data?.items[3].posterUrl).toBeNull();
    expect(data?.items[3].seed).toEqual({ movieId: 2, title: 'Dune', signal: 'watched' });
  });

  it('serializes multiple exclude IDs as repeated query keys', async () => {
    // arrange
    let excludeValues: string[] = [];
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
      const request = input instanceof Request ? input : new Request(input, init);
      excludeValues = new URL(request.url).searchParams.getAll('exclude');
      return Response.json(forYouRecommendations);
    });

    // act
    const { response } = await client.GET('/api/recommendations/for-you', {
      params: { query: { take: 10, exclude: [7, 9] } },
    });

    // assert
    expect(response.status).toBe(200);
    expect(excludeValues).toEqual(['7', '9']);
  });
});
