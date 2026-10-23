import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';
import type { paths } from '../api/types';
import { server } from '../test/server';
import { markMovieNotInterested, markMovieWatched } from './movieFeedback';

type WatchedPathId = paths['/api/movies/{id}/watched']['post']['parameters']['path']['id'];

interface CapturedRequest {
  method: string;
  path: string;
}

function capture(requests: CapturedRequest[], request: Request): void {
  requests.push({ method: request.method, path: new URL(request.url).pathname });
}

describe('markMovieWatched', () => {
  it('posts to the watched route for the given movie and reports the 204 success', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.post('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const movieId: WatchedPathId = 7;

    // act
    const outcome = await markMovieWatched(movieId);

    // assert
    expect(requests).toEqual([{ method: 'POST', path: '/api/movies/7/watched' }]);
    expect(outcome).toEqual({ ok: true, status: 204 });
  });

  it('treats a 409 repeat transition as already marked without another request', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.post('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 409 });
      }),
    );

    // act
    const outcome = await markMovieWatched(3);

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: true, status: 409 });
  });

  it('reports a failed mark without retrying the request', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.post('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 500 });
      }),
    );

    // act
    const outcome = await markMovieWatched(4);

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: false, status: 500 });
  });

  it('reports a network failure without retrying the request', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.post('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return HttpResponse.error();
      }),
    );

    // act
    const outcome = await markMovieWatched(5);

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: false, status: 0 });
  });
});

describe('markMovieNotInterested', () => {
  it('posts to the not-interested route for the given movie and reports the 204 success', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.post('*/api/movies/:id/not-interested', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 204 });
      }),
    );

    // act
    const outcome = await markMovieNotInterested(11);

    // assert
    expect(requests).toEqual([{ method: 'POST', path: '/api/movies/11/not-interested' }]);
    expect(outcome).toEqual({ ok: true, status: 204 });
  });

  it('reports a failed mark with the responding status and no retry', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.post('*/api/movies/:id/not-interested', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 404 });
      }),
    );

    // act
    const outcome = await markMovieNotInterested(12);

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: false, status: 404 });
  });
});
