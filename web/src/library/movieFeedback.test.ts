import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';
import type { paths } from '../api/types';
import { server } from '../test/server';
import {
  markMovieWatched,
  setMovieReview,
  unmarkMovieWatched,
  type ReviewState,
} from './movieFeedback';

type WatchedPathId = paths['/api/movies/{id}/watched']['post']['parameters']['path']['id'];

interface CapturedRequest {
  method: string;
  path: string;
  body: unknown;
}

function capture(requests: CapturedRequest[], request: Request): void {
  requests.push({ method: request.method, path: new URL(request.url).pathname, body: null });
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
    expect(requests).toEqual([{ method: 'POST', path: '/api/movies/7/watched', body: null }]);
    expect(outcome).toEqual({ ok: true, status: 204 });
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

describe('unmarkMovieWatched', () => {
  it('deletes the watched mark for the given movie and reports the 204 success', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.delete('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 204 });
      }),
    );

    // act
    const outcome = await unmarkMovieWatched(21);

    // assert
    expect(requests).toEqual([{ method: 'DELETE', path: '/api/movies/21/watched', body: null }]);
    expect(outcome).toEqual({ ok: true, status: 204 });
  });

  it('treats a 409 repeat transition as already unmarked without another request', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.delete('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return new HttpResponse(null, { status: 409 });
      }),
    );

    // act
    const outcome = await unmarkMovieWatched(22);

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: true, status: 409 });
  });

  it('reports a network failure without retrying the request', async () => {
    // arrange
    const requests: CapturedRequest[] = [];
    server.use(
      http.delete('*/api/movies/:id/watched', ({ request }) => {
        capture(requests, request);
        return HttpResponse.error();
      }),
    );

    // act
    const outcome = await unmarkMovieWatched(24);

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: false, status: 0 });
  });
});

describe('setMovieReview', () => {
  it.each(['none', 'liked', 'disliked'] as const)(
    'puts the %s review on the review route and reports the 204 success',
    async (review) => {
      // arrange
      const requests: Array<{ method: string; path: string; review: unknown }> = [];
      server.use(
        http.put('*/api/movies/:id/review', async ({ request }) => {
          const body = (await request.json()) as { review: unknown };
          requests.push({
            method: request.method,
            path: new URL(request.url).pathname,
            review: body.review,
          });
          return new HttpResponse(null, { status: 204 });
        }),
      );

      // act
      const outcome = await setMovieReview(11, review);

      // assert
      expect(requests).toEqual([
        { method: 'PUT', path: '/api/movies/11/review', review: review satisfies ReviewState },
      ]);
      expect(outcome).toEqual({ ok: true, status: 204 });
    },
  );

  it.each([400, 404, 422, 500])(
    'reports the %i failure with no retry and no success',
    async (status) => {
      // arrange
      const requests: string[] = [];
      server.use(
        http.put('*/api/movies/:id/review', ({ request }) => {
          requests.push(new URL(request.url).pathname);
          return new HttpResponse(null, { status });
        }),
      );

      // act
      const outcome = await setMovieReview(12, 'liked');

      // assert
      expect(requests).toHaveLength(1);
      expect(outcome).toEqual({ ok: false, status });
    },
  );

  it('reports a network failure without retrying the request', async () => {
    // arrange
    const requests: string[] = [];
    server.use(
      http.put('*/api/movies/:id/review', ({ request }) => {
        requests.push(new URL(request.url).pathname);
        return HttpResponse.error();
      }),
    );

    // act
    const outcome = await setMovieReview(13, 'disliked');

    // assert
    expect(requests).toHaveLength(1);
    expect(outcome).toEqual({ ok: false, status: 0 });
  });
});
