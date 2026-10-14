import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { createApiClient, client as defaultClient } from './client';
import type { components } from './types';
import { server } from '../test/server';

const BASE_URL = 'https://test.local';
const client = createApiClient(BASE_URL);

const browseResponse = {
  items: [
    {
      id: '1',
      title: 'Dune',
      posterUrl: '/dune.jpg',
      releaseYear: null,
      runtime: null,
      imdbRating: null,
      enrichmentStatus: 'Pending',
    },
  ],
  totalCount: '1',
} satisfies components['schemas']['BrowseMoviesResponse'];

const movieDetails = {
  id: 1,
  title: 'Dune',
  path: '/media/dune.mkv',
  format: 'mkv',
  metadata: {
    title: 'Dune',
    synopsis: 'A synopsis',
    releaseYear: 2021,
    runtime: 155,
    imdbRating: 8.1,
    imdbId: 'tt1160419',
  },
} satisfies components['schemas']['MovieDetailsResponse'];

const genres = [{ id: 1, name: 'Action' }] satisfies components['schemas']['GenreDto'][];
const people = [
  { id: 1, name: 'Timothée Chalamet' },
] satisfies components['schemas']['PersonDto'][];

function problem(status: number): components['schemas']['ProblemDetails'] {
  return {
    type: null,
    title: 'Problem',
    status,
    detail: `status ${status}`,
    instance: null,
  };
}

function problemResponse(status: number) {
  return new HttpResponse(JSON.stringify(problem(status)), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  });
}

describe('typed API client', () => {
  it('serializes browse query parameters and returns the typed page', async () => {
    // arrange
    let search = '';
    server.use(
      http.get('*/api/movies', ({ request }) => {
        search = new URL(request.url).search;
        return HttpResponse.json(browseResponse);
      }),
    );

    // act
    const { data, error, response } = await client.GET('/api/movies', {
      params: { query: { Text: 'dune', GenreIds: ['1', '2'], Page: '2' } },
    });

    // assert
    expect(error).toBeUndefined();
    expect(response.status).toBe(200);
    expect(data).toEqual(browseResponse);
    expect(search).toContain('Text=dune');
    expect(search).toContain('GenreIds=1');
    expect(search).toContain('GenreIds=2');
    expect(search).toContain('Page=2');
  });

  it('serializes the details path parameter and returns one movie', async () => {
    // arrange
    let pathname = '';
    server.use(
      http.get('*/api/movies/:id', ({ request }) => {
        pathname = new URL(request.url).pathname;
        return HttpResponse.json(movieDetails);
      }),
    );

    // act
    const { data, error } = await client.GET('/api/movies/{id}', {
      params: { path: { id: 42 } },
    });

    // assert
    expect(error).toBeUndefined();
    expect(pathname).toBe('/api/movies/42');
    expect(data).toEqual(movieDetails);
  });

  it('surfaces a generated ProblemDetails error from a declared status', async () => {
    // arrange
    server.use(http.get('*/api/movies/:id', () => problemResponse(404)));

    // act
    const { data, error, response } = await client.GET('/api/movies/{id}', {
      params: { path: { id: 7 } },
    });

    // assert
    expect(data).toBeUndefined();
    expect(response.status).toBe(404);
    expect(error).toEqual(problem(404));
  });

  it('serializes the import body and returns a bodyless 202 with a raw Location header', async () => {
    // arrange
    let body: unknown;
    server.use(
      http.post('*/api/movies/import', async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, {
          status: 202,
          headers: { Location: '/api/movies/9', 'Content-Length': '0' },
        });
      }),
    );

    // act
    const { data, error, response } = await client.POST('/api/movies/import', {
      body: { folderPath: '/media/dune' },
    });

    // assert
    expect(error).toBeUndefined();
    expect(response.status).toBe(202);
    expect(data).toEqual({});
    expect(body).toEqual({ folderPath: '/media/dune' });
    expect(response.headers.get('location')).toBe('/api/movies/9');
  });

  it('returns a bodyless 202 with the raw Location for enrichment using a union id', async () => {
    // arrange
    server.use(
      http.post(
        '*/api/movies/:id/enrichment',
        () =>
          new HttpResponse(null, {
            status: 202,
            headers: { Location: '/api/movies/1', 'Content-Length': '0' },
          }),
      ),
    );

    // act
    const { data, error, response } = await client.POST('/api/movies/{id}/enrichment', {
      params: { path: { id: '1' } },
    });

    // assert
    expect(error).toBeUndefined();
    expect(response.status).toBe(202);
    expect(data).toEqual({});
    expect(response.headers.get('location')).toBe('/api/movies/1');
  });

  it('returns bodyless 204 for watchlist add and remove', async () => {
    // arrange
    server.use(
      http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
      http.delete('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );

    // act
    const added = await client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 3 } } });
    const removed = await client.DELETE('/api/movies/{id}/watchlist', {
      params: { path: { id: 3 } },
    });

    // assert
    expect(added.response.status).toBe(204);
    expect(added.error).toBeUndefined();
    expect(removed.response.status).toBe(204);
    expect(removed.error).toBeUndefined();
  });

  it('returns bodyless 204 for play and surfaces a declared 403', async () => {
    // arrange
    server.use(http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 204 })));

    // act
    const played = await client.POST('/api/movies/{id}/play', { params: { path: { id: 5 } } });

    // assert
    expect(played.error).toBeUndefined();
    expect(played.response.status).toBe(204);

    // arrange
    server.use(http.post('*/api/movies/:id/play', () => problemResponse(403)));

    // act
    const forbidden = await client.POST('/api/movies/{id}/play', { params: { path: { id: 5 } } });

    // assert
    expect(forbidden.response.status).toBe(403);
    expect(forbidden.error).toEqual(problem(403));
  });

  it('returns the genres array', async () => {
    // arrange
    server.use(http.get('*/api/genres', () => HttpResponse.json(genres)));

    // act
    const { data, error } = await client.GET('/api/genres');

    // assert
    expect(error).toBeUndefined();
    expect(data).toEqual(genres);
  });

  it('returns the people array and surfaces a declared 422', async () => {
    // arrange
    server.use(http.get('*/api/people', () => HttpResponse.json(people)));

    // act
    const ok = await client.GET('/api/people');

    // assert
    expect(ok.error).toBeUndefined();
    expect(ok.data).toEqual(people);

    // arrange
    server.use(http.get('*/api/people', () => problemResponse(422)));

    // act
    const failed = await client.GET('/api/people');

    // assert
    expect(failed.response.status).toBe(422);
    expect(failed.error).toEqual(problem(422));
  });

  it('default client targets the browser origin', async () => {
    // arrange
    let origin = '';
    server.use(
      http.get('*/api/genres', ({ request }) => {
        origin = new URL(request.url).origin;
        return HttpResponse.json(genres);
      }),
    );

    // act
    const { data, error } = await defaultClient.GET('/api/genres');

    // assert
    expect(error).toBeUndefined();
    expect(data).toEqual(genres);
    expect(origin).toBe(window.location.origin);
  });
});
