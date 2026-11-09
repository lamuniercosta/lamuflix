export const LAUNCH_STARTED_COPY = 'Playback started.';
export const LAUNCH_NOT_FOUND_COPY = 'This movie is no longer in the library.';
export const LAUNCH_UNAVAILABLE_TITLE = 'Playback unavailable';
export const LAUNCH_UNAVAILABLE_COPY = 'Local playback is disabled on this server.';
export const LAUNCH_FAILED_COPY = "Couldn't start playback. Try again later.";

export type LocalLaunchKind = 'started' | 'not-found' | 'unavailable' | 'failed';

export interface LocalLaunchOutcome {
  kind: LocalLaunchKind;
  message: string;
}

export function localLaunchOutcome(status: number | undefined): LocalLaunchOutcome {
  if (status === 204) return { kind: 'started', message: LAUNCH_STARTED_COPY };
  if (status === 404) return { kind: 'not-found', message: LAUNCH_NOT_FOUND_COPY };
  if (status === 403) return { kind: 'unavailable', message: LAUNCH_UNAVAILABLE_COPY };
  return { kind: 'failed', message: LAUNCH_FAILED_COPY };
}
