/**
 * FR-010 track modes at the viewer seam: the selected sidecar track is `showing`, every other
 * rendered track is `disabled`. With no selection (Off) no track is `showing`, so they are all
 * disabled.
 */
export function applySubtitleTrackModes(
  container: ParentNode | null,
  selectedTrackId: string | null,
): void {
  if (!container) return;
  const tracks = Array.from(container.querySelectorAll<HTMLTrackElement>('track[data-track-id]'));
  for (const element of tracks) {
    const textTrack = element.track;
    if (!textTrack) continue;
    textTrack.mode = element.dataset.trackId === selectedTrackId ? 'showing' : 'disabled';
  }
}
