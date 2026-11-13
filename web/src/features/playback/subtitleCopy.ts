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
export const SUBTITLE_RETRY_COPY = 'Retry';
export const SUBTITLE_RETRY_SEARCH_LABEL = 'Retry subtitle search';

export const SUBTITLE_LANGUAGE_LABEL = 'Search by language';
export const SUBTITLE_CHOOSE_LANGUAGE_COPY = 'Choose a language to search for subtitles';
export const SUBTITLE_NO_RESULTS_COPY = 'No subtitles found for this language';
export const SUBTITLE_SEARCHING_COPY = 'Searching…';
export const SUBTITLE_READY_COPY = 'Ready';
export const SUBTITLE_GET_COPY = 'Get';
export const SUBTITLE_GETTING_COPY = 'Getting…';
export const SUBTITLE_LOCAL_TRACKS_LABEL = 'Local tracks';
export const SUBTITLE_QUOTA_UNAVAILABLE_COPY = 'Downloads left: unavailable';
export const SUBTITLE_READ_ONLY_COPY = 'Downloads unavailable for this library';
export const SUBTITLE_SEARCH_RATE_LIMITED_COPY =
  'Subtitle search is temporarily rate-limited. Try again in a moment.';
export const SUBTITLE_QUOTA_REACHED_COPY = 'Download quota reached.';
export const SUBTITLE_STALE_CANDIDATE_COPY = 'That result expired. Search again.';
export const SUBTITLE_PROVIDER_TIMEOUT_COPY = 'The subtitle provider timed out.';
export const SUBTITLE_PROVIDER_FAILURE_COPY = 'The subtitle provider is unavailable.';
export const SUBTITLE_INVALID_REQUEST_COPY = 'That subtitle request was not accepted.';
export const SUBTITLE_SEARCH_FAILURE_COPY = 'The subtitle search failed.';

export const SUBTITLE_DELAY_LABEL = 'Subtitle delay';
export const SUBTITLE_DELAY_EARLIER_LABEL = 'Subtitles earlier';
export const SUBTITLE_DELAY_LATER_LABEL = 'Subtitles later';
export const SUBTITLE_TIMING_LABEL = 'Subtitle timing';
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

export function subtitleGetLabel(releaseName: string): string {
  return `Get subtitle ${releaseName}`;
}

export function subtitleQuotaReachedResetCopy(localReset: string): string {
  return `Download quota reached. Resets at ${localReset}.`;
}
