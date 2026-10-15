import { describe, expect, it } from 'vitest';
import { createApiClient } from '../api/client';
import { browseMovies } from '../mocks/fixtures';
import { standingBrowseQuery } from './handlers';

const client = createApiClient('https://test.local');

describe('openapi-msw browse handlers', () => {
  it('answers the A6 standing wire query with the typed browse page', async () => {
    // act
    const { data, error, response } = await client.GET('/api/movies', {
      params: { query: { ...standingBrowseQuery } },
    });

    // assert
    expect(error).toBeUndefined();
    expect(response.status).toBe(200);
    expect(data).toEqual(browseMovies);
    expect(new URL(response.url).search).toBe('?sort=Title&direction=Ascending&page=1&pageSize=20');
  });
});
