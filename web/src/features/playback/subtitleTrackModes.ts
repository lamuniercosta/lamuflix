/**
 * FR-010 track modes at the viewer seam: the selected sidecar track is `showing`, every other
 * rendered track is `disabled`. With no selection (Off) no track is `showing`, so they are all
 * disabled.
 */
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
    const desired = element.dataset.trackId === selectedTrackId ? 'showing' : 'disabled';
    if (textTrack.mode !== desired) textTrack.mode = desired;
  }
}

/**
 * Chromium runs its own text-track selection when the media or the track list settles, which can
 * flip a rendered track to `showing` after this viewer has already applied Off or a switch. The
 * browser-owned track list is therefore watched and the desired modes are re-applied on every
 * change, so every non-selected rendered track stays `disabled` for the life of the element.
 */
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
