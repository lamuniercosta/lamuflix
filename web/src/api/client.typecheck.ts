import { createApiClient } from './client';

const client = createApiClient('https://test.local');

export async function positiveCalls() {
  await client.GET('/api/movies', {
    params: {
      query: {
        Text: 'dune',
        GenreIds: ['1'],
        sort: 'Title',
        direction: 'Ascending',
        page: 1,
        pageSize: 20,
      },
    },
  });
  await client.GET('/api/movies/{id}', { params: { path: { id: 1 } } });
  await client.GET('/api/movies/{id}/recommendations', {
    params: { path: { id: 1 }, query: { take: 10 } },
  });
  await client.POST('/api/movies/import', { body: { folderPath: '/media' } });
  await client.POST('/api/movies/{id}/enrichment', { params: { path: { id: '1' } } });
  await client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } });
  await client.DELETE('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } });
  await client.POST('/api/movies/{id}/play', { params: { path: { id: 1 } } });
  await client.POST('/api/movies/{id}/watched', { params: { path: { id: 1 } } });
  await client.DELETE('/api/movies/{id}/watched', { params: { path: { id: 1 } } });
  await client.POST('/api/movies/{id}/not-interested', { params: { path: { id: 1 } } });
  await client.DELETE('/api/movies/{id}/not-interested', { params: { path: { id: 1 } } });
  await client.GET('/api/genres');
  await client.GET('/api/people');
}

export async function idUnionDistinction() {
  await client.GET('/api/movies/{id}', { params: { path: { id: 1 } } });
  await client.POST('/api/movies/{id}/play', { params: { path: { id: '1' } } });
}

export async function negativeCalls() {
  // @ts-expect-error the path does not exist in the generated contract
  await client.GET('/api/unknown');
  // @ts-expect-error the required path parameter is missing
  await client.GET('/api/movies/{id}');
  // @ts-expect-error GetMovieDetails id is integer-only, not the number|string union
  await client.GET('/api/movies/{id}', { params: { path: { id: '1' } } });
  // @ts-expect-error the required request body is missing
  await client.POST('/api/movies/import');
  // @ts-expect-error the request body shape does not match ImportMovieRequest
  await client.POST('/api/movies/import', { body: { path: '/media' } });
  // @ts-expect-error standing browse query keys are required by the generated contract
  await client.GET('/api/movies', { params: { query: { Text: 'dune' } } });
}
