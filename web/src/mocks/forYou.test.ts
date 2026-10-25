import { describe, expect, it } from 'vitest';
import { createApiClient } from '../api/client';
import { forYouRecommendations } from './fixtures';

const client = createApiClient('https://test.local');

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
    expect(data?.map((movie) => movie.id)).toEqual(forYouRecommendations.map((movie) => movie.id));
    expect(data?.map((movie) => movie.score)).toEqual([0.91, 0.84, 0.72, 0.61]);
  });

  it('carries personal, structural, empty and nullable-poster rows', async () => {
    // act
    const { data } = await client.GET('/api/recommendations/for-you', {
      params: { query: { take: 10 } },
    });

    // assert
    expect(data?.[0].reasons).toEqual([
      { kind: 'personal', text: 'Because you watched Dune' },
      { kind: 'structural', text: 'Same director' },
    ]);
    expect(data?.[1].reasons).toEqual([]);
    expect(data?.[2].reasons).toEqual([{ kind: 'structural', text: 'Same genre' }]);
    expect(data?.[3].posterUrl).toBeNull();
    expect(data?.[3].reasons).toEqual([
      { kind: 'personal', text: 'Matches your Thriller interest' },
    ]);
  });
});
