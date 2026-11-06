import { client } from '../api/client';
import type { paths } from '../api/types';

type WatchedPathParams = paths['/api/movies/{id}/watched']['post']['parameters']['path'];
type NotInterestedPathParams =
  paths['/api/movies/{id}/not-interested']['post']['parameters']['path'];
type ReviewPathParams = paths['/api/movies/{id}/review']['put']['parameters']['path'];

export type FeedbackMovieId = WatchedPathParams['id'] &
  NotInterestedPathParams['id'] &
  ReviewPathParams['id'];

export const REVIEW_STATES = ['none', 'liked', 'disliked'] as const;

export type ReviewState = (typeof REVIEW_STATES)[number];

export interface MovieFeedbackOutcome {
  ok: boolean;
  status: number;
}

type FeedbackPath = '/api/movies/{id}/watched' | '/api/movies/{id}/not-interested';

function outcome(status: number): MovieFeedbackOutcome {
  return { ok: status === 204 || status === 409, status };
}

async function sendFeedback(
  method: 'POST' | 'DELETE',
  path: FeedbackPath,
  movieId: FeedbackMovieId,
): Promise<MovieFeedbackOutcome> {
  try {
    const init = { params: { path: { id: movieId } } };
    const { response } =
      method === 'POST' ? await client.POST(path, init) : await client.DELETE(path, init);
    return outcome(response.status);
  } catch {
    return outcome(0);
  }
}

export async function markMovieWatched(movieId: FeedbackMovieId): Promise<MovieFeedbackOutcome> {
  return sendFeedback('POST', '/api/movies/{id}/watched', movieId);
}

export async function unmarkMovieWatched(movieId: FeedbackMovieId): Promise<MovieFeedbackOutcome> {
  return sendFeedback('DELETE', '/api/movies/{id}/watched', movieId);
}

export async function markMovieNotInterested(
  movieId: FeedbackMovieId,
): Promise<MovieFeedbackOutcome> {
  return sendFeedback('POST', '/api/movies/{id}/not-interested', movieId);
}

export async function unmarkMovieNotInterested(
  movieId: FeedbackMovieId,
): Promise<MovieFeedbackOutcome> {
  return sendFeedback('DELETE', '/api/movies/{id}/not-interested', movieId);
}

export async function setMovieReview(
  movieId: FeedbackMovieId,
  review: ReviewState,
): Promise<MovieFeedbackOutcome> {
  try {
    const { response } = await client.PUT('/api/movies/{id}/review', {
      params: { path: { id: movieId } },
      body: { review },
    });
    return { ok: response.status === 204, status: response.status };
  } catch {
    return { ok: false, status: 0 };
  }
}
