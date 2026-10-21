import { describe, expect, it } from 'vitest';
import { createApiClient } from '../api/client';
import { recommendations } from './fixtures';

const client = createApiClient('https://test.local');

describe('generated recommendations handler', () => {
  it('answers the recommendations route with the typed empty fixture', async () => {
    // act
    const { data, response } = await client.GET('/api/movies/{id}/recommendations', {
      params: { path: { id: 1 } },
    });

    // assert
    expect(response.status).toBe(200);
    expect(data).toEqual(recommendations);
    expect(data).toEqual([]);
  });
});
