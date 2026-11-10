import { describe, expect, it } from 'vitest';
import { applySubtitleTrackModes } from './subtitleTrackModes';

function trackElement(trackId: string): HTMLTrackElement {
  const element = document.createElement('track');
  element.dataset.trackId = trackId;
  Object.defineProperty(element, 'track', {
    configurable: true,
    value: { mode: 'disabled' },
  });
  return element;
}

function modes(elements: HTMLTrackElement[]): Record<string, string> {
  return Object.fromEntries(
    elements.map((element) => [element.dataset.trackId ?? '', element.track?.mode ?? '']),
  );
}

describe('applySubtitleTrackModes', () => {
  it('shows only the selected track and disables every other listed track', () => {
    const english = trackElement('en');
    const portuguese = trackElement('pt');
    const video = document.createElement('video');
    video.append(english, portuguese);

    applySubtitleTrackModes(video, 'pt');

    expect(modes([english, portuguese])).toEqual({ en: 'disabled', pt: 'showing' });
  });

  it('disables every track for Off and tolerates missing containers', () => {
    const english = trackElement('en');
    const video = document.createElement('video');
    video.append(english);

    applySubtitleTrackModes(video, null);

    expect(english.track?.mode).toBe('disabled');
    expect(() => applySubtitleTrackModes(null, 'en')).not.toThrow();
  });

  it('moves the single showing track when the selection changes', () => {
    const english = trackElement('en');
    const portuguese = trackElement('pt');
    const video = document.createElement('video');
    video.append(english, portuguese);

    applySubtitleTrackModes(video, 'en');
    expect(modes([english, portuguese])).toEqual({ en: 'showing', pt: 'disabled' });

    applySubtitleTrackModes(video, 'pt');
    expect(modes([english, portuguese])).toEqual({ en: 'disabled', pt: 'showing' });
  });
});
