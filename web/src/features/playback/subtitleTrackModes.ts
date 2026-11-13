const TRACK_SELECTOR = 'track[data-track-id]';

const LIST_EVENTS = ['change', 'addtrack', 'removetrack'] as const;

const MEDIA_EVENTS = ['loadstart', 'loadedmetadata'] as const;

export function applySubtitleTrackModes(
  container: ParentNode | null,
  selectedTrackId: string | null,
): void {
  if (!container) return;
  const tracks = Array.from(container.querySelectorAll<HTMLTrackElement>(TRACK_SELECTOR));
  for (const element of tracks) {
    const textTrack = element.track;
    if (!textTrack) continue;
    const desired: TextTrackMode =
      element.dataset.trackId === selectedTrackId ? 'hidden' : 'disabled';
    if (textTrack.mode !== desired) textTrack.mode = desired;
  }
}

export function watchSubtitleTrackModes(
  video: HTMLVideoElement | null,
  selectedTrackId: () => string | null,
): () => void {
  if (!video) return () => {};
  const enforce = () => applySubtitleTrackModes(video, selectedTrackId());
  const list = video.textTracks;
  for (const event of LIST_EVENTS) list.addEventListener(event, enforce);
  for (const event of MEDIA_EVENTS) video.addEventListener(event, enforce);
  return () => {
    for (const event of LIST_EVENTS) list.removeEventListener(event, enforce);
    for (const event of MEDIA_EVENTS) video.removeEventListener(event, enforce);
  };
}
