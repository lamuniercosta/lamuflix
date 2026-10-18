import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { createApiClient } from '../api/client';
import { importAcceptedLocation, standingBrowseQuery } from '../test/handlers';
import { server } from '../test/server';
import {
  browseMovies,
  genres,
  movieDetails,
  movieDetailsNullMetadata,
  people,
  problemDetails,
  acceptedLocation,
} from './fixtures';

const client = createApiClient('https://test.local');

function problemResponse(status: number) {
  return new HttpResponse(JSON.stringify(problemDetails(status)), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  });
}

describe('generated MSW handlers', () => {
  it('answers every operation success default', async () => {
    // act
    const browse = await client.GET('/api/movies', {
      params: { query: { ...standingBrowseQuery } },
    });
    const details = await client.GET('/api/movies/{id}', { params: { path: { id: 1 } } });
    const imported = await client.POST('/api/movies/import', {
      body: { folderPath: '/media/dune' },
    });
    const enriched = await client.POST('/api/movies/{id}/enrichment', {
      params: { path: { id: 1 } },
    });
    const added = await client.POST('/api/movies/{id}/watchlist', {
      params: { path: { id: 1 } },
    });
    const removed = await client.DELETE('/api/movies/{id}/watchlist', {
      params: { path: { id: 1 } },
    });
    const played = await client.POST('/api/movies/{id}/play', { params: { path: { id: 1 } } });
    const markedWatched = await client.POST('/api/movies/{id}/watched', {
      params: { path: { id: 1 } },
    });
    const unmarkedWatched = await client.DELETE('/api/movies/{id}/watched', {
      params: { path: { id: 1 } },
    });
    const markedNotInterested = await client.POST('/api/movies/{id}/not-interested', {
      params: { path: { id: 1 } },
    });
    const unmarkedNotInterested = await client.DELETE('/api/movies/{id}/not-interested', {
      params: { path: { id: 1 } },
    });
    const genreList = await client.GET('/api/genres');
    const peopleList = await client.GET('/api/people');

    // assert
    expect(browse.data).toEqual(browseMovies);
    expect(details.data).toEqual(movieDetails);
    expect(imported.response.status).toBe(202);
    expect(imported.response.headers.get('location')).toBe(importAcceptedLocation);
    expect(enriched.response.status).toBe(202);
    expect(enriched.response.headers.get('location')).toBe(acceptedLocation);
    expect(added.response.status).toBe(204);
    expect(removed.response.status).toBe(204);
    expect(played.response.status).toBe(204);
    expect(markedWatched.response.status).toBe(204);
    expect(unmarkedWatched.response.status).toBe(204);
    expect(markedNotInterested.response.status).toBe(204);
    expect(unmarkedNotInterested.response.status).toBe(204);
    expect(genreList.data).toEqual(genres);
    expect(peopleList.data).toEqual(people);
  });

  it('serves the null-metadata details variant through an override', async () => {
    // arrange
    server.use(http.get('*/api/movies/:id', () => HttpResponse.json(movieDetailsNullMetadata)));

    // act
    const { data } = await client.GET('/api/movies/{id}', { params: { path: { id: 1 } } });

    // assert
    expect(data).toEqual(movieDetailsNullMetadata);
    expect(data?.metadata).toBeNull();
  });

  const errorCases = [
    {
      label: 'BrowseMovies 422',
      status: 422,
      register: () => server.use(http.get('*/api/movies', () => problemResponse(422))),
      invoke: () => client.GET('/api/movies', { params: { query: { ...standingBrowseQuery } } }),
    },
    {
      label: 'GetMovieDetails 404',
      status: 404,
      register: () => server.use(http.get('*/api/movies/:id', () => problemResponse(404))),
      invoke: () => client.GET('/api/movies/{id}', { params: { path: { id: 1 } } }),
    },
    {
      label: 'ImportMovie 400',
      status: 400,
      register: () => server.use(http.post('*/api/movies/import', () => problemResponse(400))),
      invoke: () => client.POST('/api/movies/import', { body: { folderPath: '/x' } }),
    },
    {
      label: 'ImportMovie 422',
      status: 422,
      register: () => server.use(http.post('*/api/movies/import', () => problemResponse(422))),
      invoke: () => client.POST('/api/movies/import', { body: { folderPath: '/x' } }),
    },
    {
      label: 'RequestEnrichment 400',
      status: 400,
      register: () =>
        server.use(http.post('*/api/movies/:id/enrichment', () => problemResponse(400))),
      invoke: () => client.POST('/api/movies/{id}/enrichment', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RequestEnrichment 404',
      status: 404,
      register: () =>
        server.use(http.post('*/api/movies/:id/enrichment', () => problemResponse(404))),
      invoke: () => client.POST('/api/movies/{id}/enrichment', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RequestEnrichment 409',
      status: 409,
      register: () =>
        server.use(http.post('*/api/movies/:id/enrichment', () => problemResponse(409))),
      invoke: () => client.POST('/api/movies/{id}/enrichment', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RequestEnrichment 422',
      status: 422,
      register: () =>
        server.use(http.post('*/api/movies/:id/enrichment', () => problemResponse(422))),
      invoke: () => client.POST('/api/movies/{id}/enrichment', { params: { path: { id: 1 } } }),
    },
    {
      label: 'AddToWatchlist 400',
      status: 400,
      register: () =>
        server.use(http.post('*/api/movies/:id/watchlist', () => problemResponse(400))),
      invoke: () => client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'AddToWatchlist 404',
      status: 404,
      register: () =>
        server.use(http.post('*/api/movies/:id/watchlist', () => problemResponse(404))),
      invoke: () => client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'AddToWatchlist 409',
      status: 409,
      register: () =>
        server.use(http.post('*/api/movies/:id/watchlist', () => problemResponse(409))),
      invoke: () => client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'AddToWatchlist 422',
      status: 422,
      register: () =>
        server.use(http.post('*/api/movies/:id/watchlist', () => problemResponse(422))),
      invoke: () => client.POST('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RemoveFromWatchlist 400',
      status: 400,
      register: () =>
        server.use(http.delete('*/api/movies/:id/watchlist', () => problemResponse(400))),
      invoke: () => client.DELETE('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RemoveFromWatchlist 404',
      status: 404,
      register: () =>
        server.use(http.delete('*/api/movies/:id/watchlist', () => problemResponse(404))),
      invoke: () => client.DELETE('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RemoveFromWatchlist 409',
      status: 409,
      register: () =>
        server.use(http.delete('*/api/movies/:id/watchlist', () => problemResponse(409))),
      invoke: () => client.DELETE('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'RemoveFromWatchlist 422',
      status: 422,
      register: () =>
        server.use(http.delete('*/api/movies/:id/watchlist', () => problemResponse(422))),
      invoke: () => client.DELETE('/api/movies/{id}/watchlist', { params: { path: { id: 1 } } }),
    },
    {
      label: 'PlayMovie 400',
      status: 400,
      register: () => server.use(http.post('*/api/movies/:id/play', () => problemResponse(400))),
      invoke: () => client.POST('/api/movies/{id}/play', { params: { path: { id: 1 } } }),
    },
    {
      label: 'PlayMovie 403',
      status: 403,
      register: () => server.use(http.post('*/api/movies/:id/play', () => problemResponse(403))),
      invoke: () => client.POST('/api/movies/{id}/play', { params: { path: { id: 1 } } }),
    },
    {
      label: 'PlayMovie 404',
      status: 404,
      register: () => server.use(http.post('*/api/movies/:id/play', () => problemResponse(404))),
      invoke: () => client.POST('/api/movies/{id}/play', { params: { path: { id: 1 } } }),
    },
    {
      label: 'PlayMovie 422',
      status: 422,
      register: () => server.use(http.post('*/api/movies/:id/play', () => problemResponse(422))),
      invoke: () => client.POST('/api/movies/{id}/play', { params: { path: { id: 1 } } }),
    },
    {
      label: 'GetPeople 422',
      status: 422,
      register: () => server.use(http.get('*/api/people', () => problemResponse(422))),
      invoke: () => client.GET('/api/people'),
    },
  ];

  it.each(errorCases)('serves $label as ProblemDetails', async ({ status, register, invoke }) => {
    // arrange
    register();

    // act
    const result = await invoke();

    // assert
    expect(result.response.status).toBe(status);
    expect(result.response.headers.get('content-type')).toContain('application/problem+json');
    expect(result.error).toEqual(problemDetails(status));
  });

  it('restores the success defaults after an override resets', async () => {
    // act
    const browse = await client.GET('/api/movies', {
      params: { query: { ...standingBrowseQuery } },
    });
    const details = await client.GET('/api/movies/{id}', { params: { path: { id: 1 } } });

    // assert
    expect(browse.data).toEqual(browseMovies);
    expect(details.data).toEqual(movieDetails);
    expect(browseMovies.items).toHaveLength(2);
  });

  it('fails an unhandled /api request without reaching the real network', async () => {
    // act
    const error = await fetch('https://test.local/api/does-not-exist').then(
      () => null,
      (reason: unknown) => reason,
    );

    // assert
    expect(error).toBeInstanceOf(Error);
    expect(String(error)).not.toContain('ENOTFOUND');
  });
});
