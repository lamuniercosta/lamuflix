import type { paths } from '../../api/types';

type SubtitleContentPath = keyof paths & '/api/movies/{id}/subtitles/{trackId}/content';

const SUBTITLE_CONTENT_PATH: SubtitleContentPath = '/api/movies/{id}/subtitles/{trackId}/content';

export function subtitleContentUrl(movieId: number | string, trackId: string): string {
  return SUBTITLE_CONTENT_PATH.replace('{id}', String(movieId)).replace(
    '{trackId}',
    encodeURIComponent(trackId),
  );
}
