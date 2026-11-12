export const SUBTITLES_LABEL = 'Subtitles';
export const SUBTITLES_OFF_COPY = 'Off';
export const SUBTITLES_UNAVAILABLE_COPY = 'Subtitles unavailable';
export const SUBTITLES_RETRY_COPY = 'Retry';
export const SUBTITLES_OPTIONS_LABEL = 'Options';
export const SUBTITLES_OPTIONS_EMPTY_COPY = 'Subtitle timing and style options will appear here.';
export const CLOSE_SUBTITLES_PANEL_LABEL = 'Close subtitles panel';

export function subtitleSelectLabel(title: string): string {
  return `Subtitles for ${title}`;
}

export function subtitlePanelLabel(title: string): string {
  return `Subtitle settings for ${title}`;
}

export function subtitlesRetryLabel(title: string): string {
  return `Retry loading subtitles for ${title}`;
}
