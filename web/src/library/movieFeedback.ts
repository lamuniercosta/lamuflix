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

export async function markMovieWatched(movieId: FeedbackMovieId): Promise<MovieFeedbackOutcome> {
  try {
    const { response } = await client.POST('/api/movies/{id}/watched', {
      params: { path: { id: movieId } },
    });
    return outcome(response.status);
  } catch {
    return outcome(0);
  }
}

export async function markMovieNotInterested(
  movieId: FeedbackMovieId,
): Promise<MovieFeedbackOutcome> {
  try {
    const { response } = await client.POST('/api/movies/{id}/not-interested', {
      params: { path: { id: movieId } },
    });
    return outcome(response.status);
  } catch {
    return outcome(0);
  }
}
