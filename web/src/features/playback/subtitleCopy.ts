export const SUBTITLES_LABEL = 'Subtitles';
export const SUBTITLES_OFF_COPY = 'Off';
export const SUBTITLES_UNAVAILABLE_COPY = 'Subtitles unavailable';
export const SUBTITLES_RETRY_COPY = 'Retry';

export function subtitleSelectLabel(title: string): string {
  return `Subtitles for ${title}`;
}

export function subtitlesRetryLabel(title: string): string {
  return `Retry loading subtitles for ${title}`;
}
