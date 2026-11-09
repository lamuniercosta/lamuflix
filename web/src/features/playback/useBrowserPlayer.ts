import { useCallback, useEffect, useRef, useState, type RefObject } from 'react';
import { PLAYBACK_SPEEDS, type PlaybackSpeed } from './playerCopy';

export type PlayerStatus =
  'loading' | 'playing' | 'paused' | 'buffering' | 'autoplay-blocked' | 'ended' | 'error';

export interface MediaDiagnostic {
  mediaErrorCode: number | null;
  networkState: number | null;
}

export interface UseBrowserPlayerOptions {
  streamUrl: string;
  onDiagnostic?: (diagnostic: MediaDiagnostic) => void;
}

export interface BrowserPlayerControls {
  status: PlayerStatus;
  videoRef: RefObject<HTMLVideoElement | null>;
  retry: () => void;
  togglePlay: () => void;
  seekBy: (delta: number) => void;
  toggleMute: () => void;
  setPlaybackRate: (rate: PlaybackSpeed) => void;
}

export function isAutoplayDenial(error: unknown): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    (error as { name?: unknown }).name === 'NotAllowedError'
  );
}

export function readDiagnostic(video: HTMLVideoElement | null): MediaDiagnostic {
  const code = video?.error?.code;
  return {
    mediaErrorCode: typeof code === 'number' ? code : null,
    networkState: typeof video?.networkState === 'number' ? video.networkState : null,
  };
}

export function useBrowserPlayer({
  streamUrl,
  onDiagnostic,
}: UseBrowserPlayerOptions): BrowserPlayerControls {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [status, setStatus] = useState<PlayerStatus>('loading');
  const [attempt, setAttempt] = useState(0);
  const generationRef = useRef(0);
  const detachingRef = useRef(false);
  const diagnosticRef = useRef(onDiagnostic);

  useEffect(() => {
    diagnosticRef.current = onDiagnostic;
  }, [onDiagnostic]);

  const fail = useCallback((video: HTMLVideoElement | null, generation: number) => {
    if (generation !== generationRef.current) return;
    diagnosticRef.current?.(readDiagnostic(video));
    setStatus('error');
  }, []);

  const requestPlay = useCallback(
    (video: HTMLVideoElement) => {
      const generation = generationRef.current;
      let started: Promise<void> | undefined;
      try {
        started = video.play();
      } catch {
        fail(video, generation);
        return;
      }
      if (!started || typeof started.catch !== 'function') return;
      started.catch((error: unknown) => {
        if (generation !== generationRef.current) return;
        if (isAutoplayDenial(error)) {
          setStatus('autoplay-blocked');
          return;
        }
        fail(video, generation);
      });
    },
    [fail],
  );

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    const isDetaching = () => detachingRef.current;
    const onPlaying = () => setStatus('playing');
    const onBuffering = () =>
      setStatus((current) =>
        current === 'playing' || current === 'loading' ? 'buffering' : current,
      );
    const onPause = () => {
      if (isDetaching()) return;
      setStatus((current) => (current === 'ended' || current === 'error' ? current : 'paused'));
    };
    const onEnded = () => setStatus('ended');
    const onError = () => {
      if (isDetaching()) return;
      diagnosticRef.current?.(readDiagnostic(video));
      setStatus('error');
    };
    video.addEventListener('playing', onPlaying);
    video.addEventListener('waiting', onBuffering);
    video.addEventListener('stalled', onBuffering);
    video.addEventListener('pause', onPause);
    video.addEventListener('ended', onEnded);
    video.addEventListener('error', onError);
    return () => {
      video.removeEventListener('playing', onPlaying);
      video.removeEventListener('waiting', onBuffering);
      video.removeEventListener('stalled', onBuffering);
      video.removeEventListener('pause', onPause);
      video.removeEventListener('ended', onEnded);
      video.removeEventListener('error', onError);
    };
  }, []);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    generationRef.current += 1;
    const generation = generationRef.current;
    detachingRef.current = false;
    setStatus('loading');
    video.setAttribute('src', streamUrl);
    video.currentTime = 0;
    video.load();
    requestPlay(video);
    return () => {
      detachingRef.current = true;
      if (generation === generationRef.current) {
        generationRef.current += 1;
      }
      video.pause();
      video.removeAttribute('src');
      video.load();
    };
  }, [streamUrl, attempt, requestPlay]);

  const retry = useCallback(() => {
    setStatus('loading');
    setAttempt((current) => current + 1);
  }, []);

  const togglePlay = useCallback(() => {
    const video = videoRef.current;
    if (!video) return;
    if (status === 'error') return;
    if (status === 'ended') {
      retry();
      return;
    }
    if (video.paused) {
      requestPlay(video);
      return;
    }
    video.pause();
  }, [requestPlay, retry, status]);

  const seekBy = useCallback((delta: number) => {
    const video = videoRef.current;
    if (!video) return;
    try {
      const ranges = video.seekable;
      if (!ranges || ranges.length === 0) return;
      const start = ranges.start(0);
      const end = ranges.end(0);
      if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) return;
      const current = Number.isFinite(video.currentTime) ? video.currentTime : start;
      video.currentTime = Math.min(Math.max(current + delta, start), end);
    } catch {
      return;
    }
  }, []);

  const toggleMute = useCallback(() => {
    const video = videoRef.current;
    if (!video) return;
    video.muted = !video.muted;
  }, []);

  const setPlaybackRate = useCallback((rate: PlaybackSpeed) => {
    const video = videoRef.current;
    if (!video) return;
    if (!PLAYBACK_SPEEDS.includes(rate)) return;
    video.playbackRate = rate;
  }, []);

  return { status, videoRef, retry, togglePlay, seekBy, toggleMute, setPlaybackRate };
}
