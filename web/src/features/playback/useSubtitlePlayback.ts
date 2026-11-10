import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { client } from '../../api/client';
import type { components } from '../../api/types';
import { subtitleContentUrl } from './subtitleContent';

const EMPTY_TRACKS: readonly [] = [];

export type SubtitleTrack = components['schemas']['SubtitleTrackDto'];

export type SubtitleListStatus = 'loading' | 'ready' | 'error';
export type SubtitleContentStatus = 'idle' | 'loading' | 'ready' | 'error';

export interface SubtitleSelection {
  trackId: string;
  label: string;
  language: string;
  /**
   * Track source: the content route built from the movie id and the server-issued opaque token,
   * replaced by an owned object URL once the converted WebVTT has loaded.
   */
  src: string;
  /** True when `src` is an object URL this viewer owns and must revoke. */
  owned: boolean;
  /** Monotonic selection attempt; only the current attempt may apply a success or a failure. */
  attempt: number;
  status: Exclude<SubtitleContentStatus, 'idle' | 'error'>;
}

interface SubtitleState {
  /** Movie this snapshot belongs to; a different movie id makes the snapshot stale by definition. */
  movieId: number;
  /** List attempt this snapshot answers; a newer attempt makes it stale by definition. */
  listAttempt: number;
  listStatus: SubtitleListStatus;
  tracks: readonly SubtitleTrack[];
  selection: SubtitleSelection | null;
  contentError: boolean;
}

export interface SubtitlePlayback {
  listStatus: SubtitleListStatus;
  tracks: readonly SubtitleTrack[];
  selection: SubtitleSelection | null;
  contentStatus: SubtitleContentStatus;
  select: (trackId: string | null) => void;
  retryList: () => void;
}

async function fetchTrackList(movieId: number): Promise<readonly SubtitleTrack[] | null> {
  try {
    const result = await client.GET('/api/movies/{id}/subtitles', {
      params: { path: { id: movieId } },
    });
    if (result.error || !Array.isArray(result.data)) return null;
    return result.data;
  } catch {
    return null;
  }
}

async function fetchTrackContent(src: string): Promise<string | null> {
  try {
    const response = await fetch(src);
    if (!response.ok) return null;
    const text = await response.text();
    return text.length > 0 ? text : null;
  } catch {
    return null;
  }
}

function createOwnedUrl(text: string): string | null {
  try {
    return URL.createObjectURL(new Blob([text], { type: 'text/vtt;charset=utf-8' }));
  } catch {
    return null;
  }
}

function revokeOwnedUrl(url: string | null): void {
  if (!url) return;
  try {
    URL.revokeObjectURL(url);
  } catch {
    // Releasing an already-released URL is not a viewer failure.
  }
}

function initialSnapshot(movieId: number): SubtitleState {
  return {
    movieId,
    listAttempt: 0,
    listStatus: 'loading',
    tracks: [],
    selection: null,
    contentError: false,
  };
}

/**
 * Session-local subtitle selection for one movie.
 *
 * Selection starts Off even when the served list carries a default marker and never reads persisted
 * storage or the browser language. Track identity, labels and the content route come only from the
 * served list, so nothing is derived from filenames or paths. The stored snapshot is tagged with the
 * movie and list attempt it answers and every content load with a selection attempt, so only the
 * current movie and selection may apply a success or a failure. Object URLs the viewer created are
 * revoked on Off, movie change and unmount.
 */
export function useSubtitlePlayback(movieId: number | string): SubtitlePlayback {
  const numericId = Number(movieId);
  const [listAttempt, setListAttempt] = useState(0);
  const [snapshot, setSnapshot] = useState<SubtitleState>(() => initialSnapshot(numericId));
  const listGeneration = useRef(0);
  const attemptRef = useRef(0);
  const ownedUrlRef = useRef<string | null>(null);

  const releaseOwnedUrl = useCallback(() => {
    revokeOwnedUrl(ownedUrlRef.current);
    ownedUrlRef.current = null;
  }, []);

  const isCurrent = snapshot.movieId === numericId && snapshot.listAttempt === listAttempt;
  const listStatus: SubtitleListStatus = isCurrent ? snapshot.listStatus : 'loading';
  const tracks = useMemo<readonly SubtitleTrack[]>(
    () => (isCurrent ? snapshot.tracks : EMPTY_TRACKS),
    [isCurrent, snapshot.tracks],
  );
  const selection: SubtitleSelection | null = isCurrent ? snapshot.selection : null;
  const contentStatus: SubtitleContentStatus = !isCurrent
    ? 'idle'
    : snapshot.contentError
      ? 'error'
      : (snapshot.selection?.status ?? 'idle');

  useEffect(() => {
    const generation = listGeneration.current + 1;
    listGeneration.current = generation;
    void (async () => {
      const loaded = await fetchTrackList(numericId);
      if (generation !== listGeneration.current) return;
      setSnapshot({
        movieId: numericId,
        listAttempt,
        listStatus: loaded ? 'ready' : 'error',
        tracks: loaded ?? [],
        selection: null,
        contentError: false,
      });
    })();
  }, [numericId, listAttempt]);

  // A movie change or unmount releases whatever this viewer still owns.
  useEffect(() => () => releaseOwnedUrl(), [numericId, releaseOwnedUrl]);

  const select = useCallback(
    (trackId: string | null) => {
      attemptRef.current += 1;
      const attempt = attemptRef.current;
      releaseOwnedUrl();
      if (trackId === null) {
        setSnapshot((current) => ({ ...current, selection: null, contentError: false }));
        return;
      }
      const track = tracks.find((candidate) => candidate.id === trackId);
      const route = subtitleContentUrl(numericId, trackId);
      setSnapshot((current) => ({
        ...current,
        contentError: false,
        selection: {
          trackId,
          label: track?.label ?? '',
          language: track?.language ?? '',
          src: route,
          owned: false,
          attempt,
          status: 'loading',
        },
      }));
      void (async () => {
        const text = await fetchTrackContent(route);
        if (attempt !== attemptRef.current) return;
        const ownedUrl = text === null ? null : createOwnedUrl(text);
        if (attempt !== attemptRef.current) {
          revokeOwnedUrl(ownedUrl);
          return;
        }
        if (!ownedUrl) {
          attemptRef.current += 1;
          setSnapshot((current) => ({ ...current, selection: null, contentError: true }));
          return;
        }
        ownedUrlRef.current = ownedUrl;
        setSnapshot((current) =>
          current.selection && current.selection.attempt === attempt
            ? {
                ...current,
                selection: { ...current.selection, src: ownedUrl, owned: true, status: 'ready' },
              }
            : current,
        );
      })();
    },
    [numericId, releaseOwnedUrl, tracks],
  );

  const retryList = useCallback(() => {
    releaseOwnedUrl();
    setListAttempt((current) => current + 1);
  }, [releaseOwnedUrl]);

  return { listStatus, tracks, selection, contentStatus, select, retryList };
}
