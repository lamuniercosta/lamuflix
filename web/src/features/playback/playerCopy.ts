export const LOADING_COPY = 'Loading player…';
export const ERROR_COPY = "Couldn't play this movie. Check your connection and try again.";
export const AUTOPLAY_BLOCKED_COPY = 'Playback is ready. Choose Play to start watching.';
export const ENDED_ANNOUNCEMENT = 'Playback finished.';
export const RETRY_COPY = 'Retry';
export const OPEN_LOCAL_COPY = 'Open in BS Player';
export const SPEED_LABEL = 'Playback speed';
export const SETTINGS_LABEL = 'Settings';
export const PLAYBACK_SPEEDS = [0.5, 1, 1.5, 2] as const;

export type PlaybackSpeed = (typeof PLAYBACK_SPEEDS)[number];

export function playerRegionLabel(title: string): string {
  return `Movie player: ${title}`;
}

export function playLabel(title: string): string {
  return `Play ${title} in browser`;
}

export function retryLabel(title: string): string {
  return `Retry playing ${title} from the beginning`;
}

export function openLocalLabel(title: string): string {
  return `Open ${title} in BS Player (local playback)`;
}
