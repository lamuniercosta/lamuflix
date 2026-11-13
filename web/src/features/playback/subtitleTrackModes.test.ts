import { describe, expect, it } from 'vitest';
import { applySubtitleTrackModes, watchSubtitleTrackModes } from './subtitleTrackModes';

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
  it('loads only the selected track as hidden and disables every other listed track', () => {
    const english = trackElement('en');
    const portuguese = trackElement('pt');
    const video = document.createElement('video');
    video.append(english, portuguese);

    applySubtitleTrackModes(video, 'pt');

    expect(modes([english, portuguese])).toEqual({ en: 'disabled', pt: 'hidden' });
  });

  it('disables every track for Off and tolerates missing containers', () => {
    const english = trackElement('en');
    const video = document.createElement('video');
    video.append(english);

    applySubtitleTrackModes(video, null);

    expect(english.track?.mode).toBe('disabled');
    expect(() => applySubtitleTrackModes(null, 'en')).not.toThrow();
  });

  it('moves the single hidden track when the selection changes', () => {
    const english = trackElement('en');
    const portuguese = trackElement('pt');
    const video = document.createElement('video');
    video.append(english, portuguese);

    applySubtitleTrackModes(video, 'en');
    expect(modes([english, portuguese])).toEqual({ en: 'hidden', pt: 'disabled' });

    applySubtitleTrackModes(video, 'pt');
    expect(modes([english, portuguese])).toEqual({ en: 'disabled', pt: 'hidden' });
  });

  it('re-applies hidden mode when the browser switches the selected track to showing', () => {
    const english = trackElement('en');
    const video = document.createElement('video');
    video.append(english);

    const stop = watchSubtitleTrackModes(video, () => 'en');
    english.track!.mode = 'showing';
    video.textTracks.dispatchEvent(new Event('change'));

    expect(english.track?.mode).toBe('hidden');
    stop();
  });

  it('releases every media and track-list listener it acquired', () => {
    const video = document.createElement('video');
    const added: string[] = [];
    const removed: string[] = [];
    type Listener = EventListenerOrEventListenerObject;
    type Options = boolean | AddEventListenerOptions | undefined;
    const trackList = video.textTracks;
    const addToList = trackList.addEventListener.bind(trackList);
    const removeFromList = trackList.removeEventListener.bind(trackList);
    trackList.addEventListener = (type: string, listener: Listener, options?: Options) => {
      added.push(`list:${type}`);
      addToList(type, listener, options);
    };
    trackList.removeEventListener = (type: string, listener: Listener, options?: Options) => {
      removed.push(`list:${type}`);
      removeFromList(type, listener, options);
    };
    const addToVideo = video.addEventListener.bind(video);
    const removeFromVideo = video.removeEventListener.bind(video);
    video.addEventListener = (type: string, listener: Listener, options?: Options) => {
      added.push(`media:${type}`);
      addToVideo(type, listener, options);
    };
    video.removeEventListener = (type: string, listener: Listener, options?: Options) => {
      removed.push(`media:${type}`);
      removeFromVideo(type, listener, options);
    };

    const stop = watchSubtitleTrackModes(video, () => 'en');
    stop();

    expect(added.length).toBeGreaterThan(0);
    expect(removed.sort()).toEqual(added.sort());
  });
});
