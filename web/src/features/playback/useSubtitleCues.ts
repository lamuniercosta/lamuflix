import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  activeSubtitleCuesAt,
  copySubtitleCueBaseline,
  cueTimeToMs,
  sameSubtitleCueIds,
  shiftSubtitleCueIntervals,
  type SubtitleCueSeed,
} from './subtitleTiming';

export interface RawSubtitleCue {
  id?: string;
  startTime: number;
  endTime: number;
  text: string;
}

export interface SubtitleCueList {
  readonly cues: ArrayLike<RawSubtitleCue | null> | null;
  addEventListener(type: string, listener: EventListener): void;
  removeEventListener(type: string, listener: EventListener): void;
}

export interface SubtitleCueClock {
  readonly currentTime: number;
  readonly paused: boolean;
  readonly playbackRate: number;
  addEventListener(type: string, listener: EventListener): void;
  removeEventListener(type: string, listener: EventListener): void;
}

export interface UseSubtitleCuesOptions {
  media: SubtitleCueClock | null;
  cueList: SubtitleCueList | null;
  hasTrack: boolean;
  delayMs: number;
}

export interface ActiveSubtitleCue {
  id: string;
  text: string;
}

interface CueSnapshot {
  list: SubtitleCueList | null;
  cues: readonly SubtitleCueSeed[];
}

interface CueSource {
  intervals: readonly SubtitleCueSeed[];
  media: SubtitleCueClock | null;
  hasTrack: boolean;
}

const MEDIA_EVENTS = [
  'timeupdate',
  'seeking',
  'seeked',
  'play',
  'playing',
  'pause',
  'ratechange',
  'loadedmetadata',
  'durationchange',
] as const;

const EMPTY_CUES: readonly ActiveSubtitleCue[] = [];

function readCues(cueList: SubtitleCueList | null): readonly SubtitleCueSeed[] {
  const cues = cueList?.cues;
  if (!cues) return [];
  const seeds: SubtitleCueSeed[] = [];
  for (let index = 0; index < cues.length; index += 1) {
    const cue = cues[index];
    if (!cue) continue;
    const startMs = cueTimeToMs(cue.startTime);
    const endMs = cueTimeToMs(cue.endTime);
    seeds.push({
      id: cue.id ? cue.id : `${startMs}-${index}`,
      startMs,
      endMs,
      text: cue.text,
    });
  }
  return seeds;
}

function snapshotCues(cueList: SubtitleCueList | null): CueSnapshot {
  return { list: cueList, cues: copySubtitleCueBaseline(readCues(cueList)) };
}

function sameCueSeeds(
  previous: readonly SubtitleCueSeed[],
  next: readonly SubtitleCueSeed[],
): boolean {
  if (previous.length !== next.length) return false;
  return previous.every((cue, index) => {
    const other = next[index];
    return (
      cue.id === other.id &&
      cue.startMs === other.startMs &&
      cue.endMs === other.endMs &&
      cue.text === other.text
    );
  });
}

function baselineFor(snapshot: CueSnapshot, cueList: SubtitleCueList | null) {
  return snapshot.list === cueList ? snapshot.cues : snapshotCues(cueList).cues;
}

export function useSubtitleCues({
  media,
  cueList,
  hasTrack,
  delayMs,
}: UseSubtitleCuesOptions): readonly ActiveSubtitleCue[] {
  const [active, setActive] = useState<readonly ActiveSubtitleCue[]>(EMPTY_CUES);
  const [snapshot, setSnapshot] = useState<CueSnapshot>(() => snapshotCues(cueList));
  const activeRef = useRef<readonly ActiveSubtitleCue[]>(EMPTY_CUES);
  const sourceRef = useRef<CueSource>({ intervals: [], media: null, hasTrack: false });

  const baseline = useMemo(() => baselineFor(snapshot, cueList), [cueList, snapshot]);

  const intervals = useMemo(
    () => shiftSubtitleCueIntervals(baseline, delayMs),
    [baseline, delayMs],
  );

  const sync = useCallback(() => {
    const source = sourceRef.current;
    let next: readonly ActiveSubtitleCue[] = EMPTY_CUES;
    if (source.hasTrack && source.media) {
      next = activeSubtitleCuesAt(source.intervals, cueTimeToMs(source.media.currentTime)).map(
        (cue) => ({ id: cue.id, text: cue.text }),
      );
    }
    if (sameSubtitleCueIds(activeRef.current, next)) return;
    activeRef.current = next;
    setActive(next);
  }, []);

  useEffect(() => {
    sourceRef.current = { intervals, media, hasTrack };
    sync();
  }, [hasTrack, intervals, media, sync]);

  useEffect(() => {
    if (!cueList) return;
    const onCueChange = () => {
      const next = snapshotCues(cueList);
      setSnapshot((current) =>
        current.list === cueList && sameCueSeeds(current.cues, next.cues) ? current : next,
      );
    };
    cueList.addEventListener('cuechange', onCueChange);
    return () => {
      cueList.removeEventListener('cuechange', onCueChange);
    };
  }, [cueList]);

  useEffect(() => {
    let frame: number | null = null;
    const stopFrame = () => {
      if (frame !== null) {
        cancelAnimationFrame(frame);
        frame = null;
      }
    };
    const loop = () => {
      frame = null;
      const clock = sourceRef.current.media;
      if (!clock || clock.paused) return;
      sync();
      frame = requestAnimationFrame(loop);
    };
    const startFrame = () => {
      const clock = sourceRef.current.media;
      if (!clock || clock.paused || frame !== null) return;
      frame = requestAnimationFrame(loop);
    };
    const onMediaEvent = () => {
      sync();
      const clock = sourceRef.current.media;
      if (!clock || clock.paused) stopFrame();
      else startFrame();
    };

    if (media) {
      for (const event of MEDIA_EVENTS) media.addEventListener(event, onMediaEvent);
      onMediaEvent();
    }

    return () => {
      stopFrame();
      if (!media) return;
      for (const event of MEDIA_EVENTS) media.removeEventListener(event, onMediaEvent);
    };
  }, [media, sync]);

  return active;
}
