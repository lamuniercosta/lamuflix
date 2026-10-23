import { client } from '../api/client';
import type { paths } from '../api/types';

type WatchedPathParams = paths['/api/movies/{id}/watched']['post']['parameters']['path'];
type NotInterestedPathParams =
  paths['/api/movies/{id}/not-interested']['post']['parameters']['path'];

export type FeedbackMovieId = WatchedPathParams['id'] & NotInterestedPathParams['id'];

export interface MovieFeedbackOutcome {
  ok: boolean;
  status: number;
}

function outcome(status: number): MovieFeedbackOutcome {
  return { ok: status === 204 || status === 409, status };
}

async function postFeedback(
  path: '/api/movies/{id}/watched' | '/api/movies/{id}/not-interested',
  movieId: FeedbackMovieId,
): Promise<MovieFeedbackOutcome> {
  try {
    const { response } = await client.POST(path, {
      params: { path: { id: movieId } },
    });
    return outcome(response.status);
  } catch {
    return outcome(0);
  }
}

export async function markMovieWatched(movieId: FeedbackMovieId): Promise<MovieFeedbackOutcome> {
  return postFeedback('/api/movies/{id}/watched', movieId);
}

export async function markMovieNotInterested(
  movieId: FeedbackMovieId,
): Promise<MovieFeedbackOutcome> {
  return postFeedback('/api/movies/{id}/not-interested', movieId);
}
