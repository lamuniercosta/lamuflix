import { useCallback, useEffect, useRef, useState } from 'react';
import {
  DEFAULT_SUBTITLE_STYLE,
  readSubtitleDelay,
  readSubtitleStyle,
  removeSubtitleDelay,
  removeSubtitleStyle,
  sanitizeSubtitleStyle,
  subtitleDelayKey,
  writeSubtitleDelay,
  writeSubtitleStyle,
  type SubtitleStorage,
  type SubtitleStyleProfile,
} from './subtitlePreferences';
import { shiftSubtitleDelay, type SubtitleDelayDirection } from './subtitleTiming';

export interface UseSubtitleOptionsOptions {
  movieId: number | string;
  trackId: string | null;
  storage: () => SubtitleStorage | null;
}

export interface SubtitleOptionsState {
  delayMs: number;
  style: SubtitleStyleProfile;
  delayPersisted: boolean;
  stylePersisted: boolean;
  adjustDelay: (direction: SubtitleDelayDirection, shift: boolean) => void;
  resetDelay: () => void;
  updateStyle: (patch: Partial<SubtitleStyleProfile>) => void;
  resetStyle: () => void;
}

interface PreferenceSnapshot {
  key: string | null;
  delayMs: number;
  style: SubtitleStyleProfile;
  delayPersisted: boolean;
  stylePersisted: boolean;
}

function resolveStorage(accessor: () => SubtitleStorage | null): SubtitleStorage | null {
  try {
    return accessor();
  } catch {
    return null;
  }
}

function defaultStyle(): SubtitleStyleProfile {
  return { ...DEFAULT_SUBTITLE_STYLE };
}

function pairKey(movieId: number | string, trackId: string | null): string | null {
  if (trackId === null) return null;
  return subtitleDelayKey(movieId, trackId);
}

function initialSnapshot(
  movieId: number | string,
  trackId: string | null,
  store: SubtitleStorage | null,
): PreferenceSnapshot {
  const key = pairKey(movieId, trackId);
  return {
    key,
    delayMs: store && trackId !== null ? readSubtitleDelay(store, movieId, trackId) : 0,
    style: store ? readSubtitleStyle(store) : defaultStyle(),
    delayPersisted: store !== null,
    stylePersisted: store !== null,
  };
}

export function useSubtitleOptions({
  movieId,
  trackId,
  storage,
}: UseSubtitleOptionsOptions): SubtitleOptionsState {
  const accessorRef = useRef(storage);
  useEffect(() => {
    accessorRef.current = storage;
  }, [storage]);

  const key = pairKey(movieId, trackId);
  const [snapshot, setSnapshot] = useState<PreferenceSnapshot>(() =>
    initialSnapshot(movieId, trackId, resolveStorage(storage)),
  );

  useEffect(() => {
    if (snapshot.key === key) return;
    const store = resolveStorage(accessorRef.current);
    setSnapshot((previous) => ({
      key,
      delayMs: store && trackId !== null ? readSubtitleDelay(store, movieId, trackId) : 0,
      style: previous.style,
      delayPersisted: store !== null,
      stylePersisted: previous.stylePersisted,
    }));
  }, [key, movieId, snapshot.key, trackId]);

  const adjustDelay = useCallback(
    (direction: SubtitleDelayDirection, shift: boolean) => {
      if (key === null || trackId === null) return;
      const store = resolveStorage(accessorRef.current);
      const base =
        snapshot.key === key
          ? snapshot.delayMs
          : store
            ? readSubtitleDelay(store, movieId, trackId)
            : 0;
      const delayMs = shiftSubtitleDelay(base, direction, shift);
      const persisted = store ? writeSubtitleDelay(store, movieId, trackId, delayMs) : false;
      setSnapshot({ ...snapshot, key, delayMs, delayPersisted: persisted });
    },
    [key, movieId, snapshot, trackId],
  );

  const resetDelay = useCallback(() => {
    if (key === null || trackId === null) return;
    const store = resolveStorage(accessorRef.current);
    const persisted = store ? removeSubtitleDelay(store, movieId, trackId) : false;
    setSnapshot({ ...snapshot, key, delayMs: 0, delayPersisted: persisted });
  }, [key, movieId, snapshot, trackId]);

  const updateStyle = useCallback(
    (patch: Partial<SubtitleStyleProfile>) => {
      const style = sanitizeSubtitleStyle({ ...snapshot.style, ...patch });
      const store = resolveStorage(accessorRef.current);
      const persisted = store ? writeSubtitleStyle(store, style) : false;
      setSnapshot({ ...snapshot, style, stylePersisted: persisted });
    },
    [snapshot],
  );

  const resetStyle = useCallback(() => {
    const store = resolveStorage(accessorRef.current);
    const persisted = store ? removeSubtitleStyle(store) : false;
    setSnapshot({ ...snapshot, style: defaultStyle(), stylePersisted: persisted });
  }, [snapshot]);

  return {
    delayMs: snapshot.delayMs,
    style: snapshot.style,
    delayPersisted: snapshot.delayPersisted,
    stylePersisted: snapshot.stylePersisted,
    adjustDelay,
    resetDelay,
    updateStyle,
    resetStyle,
  };
}
