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

  it('carries personal, structural, empty and nullable-poster rows', async () => {
    // act
    const { data } = await client.GET('/api/recommendations/for-you', {
      params: { query: { take: 10 } },
    });

    // assert
    expect(data?.items[0].reasons).toEqual([
      { kind: 'personal', text: 'Because you watched Dune' },
      { kind: 'structural', text: 'Same director' },
    ]);
    expect(data?.items[1].reasons).toEqual([]);
    expect(data?.items[2].reasons).toEqual([{ kind: 'structural', text: 'Same genre' }]);
    expect(data?.items[3].posterUrl).toBeNull();
    expect(data?.items[3].reasons).toEqual([
      { kind: 'personal', text: 'Matches your Thriller interest' },
    ]);
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
