import type { SubtitleEdgeToken, SubtitleFontToken } from './subtitlePreferences';

export const SUBTITLES_LABEL = 'Subtitles';
export const SUBTITLES_TRIGGER_COPY = 'CC';
export const SUBTITLES_OFF_COPY = 'Off';
export const SUBTITLES_LOADING_COPY = 'Loading subtitles…';
export const SUBTITLES_EMPTY_COPY = 'This movie has no subtitle tracks.';
export const SUBTITLES_UNAVAILABLE_COPY = 'Subtitles unavailable';
export const SUBTITLES_RETRY_COPY = 'Retry';
export const SUBTITLES_OPTIONS_LABEL = 'Options';
export const SUBTITLES_OPTIONS_EMPTY_COPY = 'Subtitle timing and style options will appear here.';
export const CLOSE_SUBTITLES_PANEL_LABEL = 'Close subtitles panel';

export const SUBTITLE_DELAY_LABEL = 'Subtitle delay';
export const SUBTITLE_DELAY_HELP_COPY = 'Negative shows subtitles earlier, positive later.';
export const RESET_TIMING_LABEL = 'Reset timing';
export const CAPTION_STYLE_LABEL = 'Caption style';
export const SUBTITLE_PREVIEW_LABEL = 'Subtitle preview';
export const SUBTITLE_PREVIEW_SAMPLE_COPY = 'The quick brown fox jumps over the lazy dog.';
export const SUBTITLE_FONT_LABEL = 'Font';
export const SUBTITLE_TEXT_SIZE_LABEL = 'Text size (%)';
export const SUBTITLE_TEXT_COLOR_LABEL = 'Text color';
export const SUBTITLE_TEXT_OPACITY_LABEL = 'Text opacity';
export const SUBTITLE_BACKGROUND_COLOR_LABEL = 'Background color';
export const SUBTITLE_BACKGROUND_OPACITY_LABEL = 'Background opacity';
export const SUBTITLE_EDGE_LABEL = 'Edge style';
export const RESET_STYLE_LABEL = 'Reset to default';

export const SUBTITLE_FONT_OPTION_LABELS: Record<SubtitleFontToken, string> = {
  'system-sans': 'System sans-serif',
  serif: 'Serif',
  monospace: 'Monospace',
};

export const SUBTITLE_EDGE_OPTION_LABELS: Record<SubtitleEdgeToken, string> = {
  none: 'None',
  'drop-shadow': 'Drop shadow',
  raised: 'Raised',
  depressed: 'Depressed',
  outline: 'Outline',
};

export function subtitleSelectLabel(title: string): string {
  return `Subtitles for ${title}`;
}

export function subtitlePanelLabel(title: string): string {
  return `Subtitle settings for ${title}`;
}

export function subtitlesRetryLabel(title: string): string {
  return `Retry loading subtitles for ${title}`;
}
