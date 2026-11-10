import type { paths } from '../../api/types';

type SubtitleContentPath = keyof paths & '/api/movies/{id}/subtitles/{trackId}/content';

const SUBTITLE_CONTENT_PATH: SubtitleContentPath = '/api/movies/{id}/subtitles/{trackId}/content';

/**
 * Builds the track source from the generated path template, so the only inputs are the movie id
 * and the server-issued opaque token: no filenames, paths, or hand-rolled route strings.
 */
export function subtitleContentUrl(movieId: number | string, trackId: string): string {
  return SUBTITLE_CONTENT_PATH.replace('{id}', String(movieId)).replace(
    '{trackId}',
    encodeURIComponent(trackId),
  );
}
