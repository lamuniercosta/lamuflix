import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type RefObject,
} from 'react';
import { PLAYBACK_SPEEDS, type PlaybackSpeed } from './playerCopy';

export type PlayerStatus =
  'loading' | 'playing' | 'paused' | 'buffering' | 'autoplay-blocked' | 'ended' | 'error';

export interface MediaDiagnostic {
  mediaErrorCode: number | null;
  networkState: number | null;
}

export interface UseBrowserPlayerOptions {
  streamUrl: string;
  movieId?: number | string;
  onDiagnostic?: (diagnostic: MediaDiagnostic) => void;
}

interface PlaybackContinuity {
  time: number;
  canSeek: boolean;
  paused: boolean;
  volume: number;
  muted: boolean;
  rate: number;
}

interface AudioGraph {
  context: AudioContext;
  gain: GainNode;
  compressor: DynamicsCompressorNode;
}

interface BoostBinding extends AudioGraph {
  element: HTMLVideoElement;
  source: MediaElementAudioSourceNode;
  generation: number;
}

export interface BrowserPlayerControls {
  status: PlayerStatus;
  videoRef: RefObject<HTMLVideoElement | null>;
  elementKey: number;
  boostSupported: boolean;
  boostPercent: number;
  boostActive: boolean;
  setBoost: (percent: number) => void;
  resetBoost: () => void;
  retry: () => void;
  togglePlay: () => void;
  seekBy: (delta: number) => void;
  toggleMute: () => void;
  setPlaybackRate: (rate: PlaybackSpeed) => void;
}

const BOOST_MIN = 100;
const BOOST_MAX = 300;
const DIRECT_STREAM_PATH = /^\/api\/movies\/(\d+)\/stream\/?$/;
const BOOST_BINDING = Symbol.for('lamuflix.playback.boostBinding');

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

function clampBoost(value: number): number {
  if (!Number.isFinite(value)) return BOOST_MIN;
  return Math.min(BOOST_MAX, Math.max(BOOST_MIN, Math.round(value)));
}

function hasWebAudio(): boolean {
  return typeof window !== 'undefined' && typeof window.AudioContext === 'function';
}

function matchesDeliveryPath(streamUrl: string, movieId: number | string | undefined): boolean {
  if (typeof streamUrl !== 'string' || streamUrl.length === 0) return false;
  let resolved: URL;
  try {
    resolved = new URL(streamUrl, window.location.href);
  } catch {
    return false;
  }
  if (resolved.protocol !== 'http:' && resolved.protocol !== 'https:') return false;
  if (resolved.origin !== window.location.origin) return false;
  if (resolved.search !== '' || resolved.hash !== '') return false;
  const match = DIRECT_STREAM_PATH.exec(resolved.pathname);
  if (!match) return false;
  if (movieId !== undefined && String(movieId) !== match[1]) return false;
  return true;
}

function readBinding(element: HTMLVideoElement | null): BoostBinding | null {
  if (!element) return null;
  const stored = (element as unknown as Record<symbol, BoostBinding | undefined>)[BOOST_BINDING];
  return stored ?? null;
}

function writeBinding(element: HTMLVideoElement, binding: BoostBinding | null): void {
  Object.defineProperty(element, BOOST_BINDING, {
    configurable: true,
    writable: true,
    enumerable: false,
    value: binding,
  });
}

function applyBoost(graph: AudioGraph, percent: number): void {
  const limiting = percent > BOOST_MIN;
  graph.gain.gain.value = percent / 100;
  graph.compressor.threshold.value = limiting ? -12 : 0;
  graph.compressor.knee.value = 0;
  graph.compressor.ratio.value = limiting ? 12 : 1;
  graph.compressor.attack.value = 0.003;
  graph.compressor.release.value = 0.25;
}

async function closeGraph(graph: AudioGraph | null): Promise<void> {
  if (!graph) return;
  try {
    await graph.context.close();
  } catch {
    // A failed close never changes native playback or element ownership.
  }
}

function captureContinuity(element: HTMLVideoElement): PlaybackContinuity {
  let canSeek = false;
  try {
    const seekable = element.seekable;
    if (seekable && seekable.length > 0) {
      const start = seekable.start(0);
      const end = seekable.end(0);
      canSeek =
        Number.isFinite(start) &&
        Number.isFinite(end) &&
        end > start &&
        element.currentTime >= start;
    }
  } catch {
    canSeek = false;
  }
  return {
    time: element.currentTime,
    canSeek,
    paused: element.paused,
    volume: element.volume,
    muted: element.muted,
    rate: element.playbackRate,
  };
}

export function useBrowserPlayer({
  streamUrl,
  movieId,
  onDiagnostic,
}: UseBrowserPlayerOptions): BrowserPlayerControls {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [status, setStatus] = useState<PlayerStatus>('loading');
  const [attempt, setAttempt] = useState(0);
  const [boostPercent, setBoostPercent] = useState(BOOST_MIN);
  const [boostActive, setBoostActive] = useState(false);
  const [elementKey, setElementKey] = useState(0);
  const generationRef = useRef(0);
  const elementKeyRef = useRef(0);
  const detachingRef = useRef(false);
  const diagnosticRef = useRef(onDiagnostic);
  const streamUrlRef = useRef(streamUrl);
  const movieIdRef = useRef(movieId);
  const boostPercentRef = useRef(BOOST_MIN);
  const continuityRef = useRef<PlaybackContinuity | null>(null);
  const pendingRecoveryRef = useRef<HTMLVideoElement | null>(null);

  useEffect(() => {
    streamUrlRef.current = streamUrl;
  }, [streamUrl]);

  useEffect(() => {
    movieIdRef.current = movieId;
  }, [movieId]);

  const eligible = useMemo(() => matchesDeliveryPath(streamUrl, movieId), [streamUrl, movieId]);
  const boostSupported = eligible && hasWebAudio();

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

  const recoverElement = useCallback((element: HTMLVideoElement) => {
    if (pendingRecoveryRef.current === element) return;
    continuityRef.current = captureContinuity(element);
    pendingRecoveryRef.current = element;
    elementKeyRef.current += 1;
    setElementKey(elementKeyRef.current);
  }, []);

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
  }, [elementKey]);

  useLayoutEffect(() => {
    const element = videoRef.current;
    const binding = readBinding(element);
    if (!binding) return;
    if (eligible && binding.generation === generationRef.current) return;
    if (element) writeBinding(element, null);
    void closeGraph(binding);
    setBoostActive(false);
    recoverElement(element as HTMLVideoElement);
  }, [eligible, recoverElement, streamUrl, elementKey]);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    if (pendingRecoveryRef.current === video) return;
    pendingRecoveryRef.current = null;
    generationRef.current += 1;
    const generation = generationRef.current;
    detachingRef.current = false;
    setStatus('loading');
    const continuity = continuityRef.current;
    continuityRef.current = null;
    video.setAttribute('src', streamUrl);
    video.load();
    video.currentTime = continuity?.canSeek ? continuity.time : 0;
    if (continuity) {
      video.volume = continuity.volume;
      video.muted = continuity.muted;
      video.playbackRate = continuity.rate;
    }
    if (continuity?.paused) {
      setStatus('paused');
    } else {
      requestPlay(video);
    }
    return () => {
      detachingRef.current = true;
      if (generation === generationRef.current) {
        generationRef.current += 1;
      }
      video.pause();
      video.removeAttribute('src');
      video.load();
    };
  }, [streamUrl, attempt, elementKey, requestPlay]);

  const activateBoost = useCallback(
    (requested?: number) => {
      const value = clampBoost(requested ?? boostPercentRef.current);
      if (!hasWebAudio() || !matchesDeliveryPath(streamUrlRef.current, movieIdRef.current)) return;
      const element = videoRef.current;
      if (!element) return;
      const generation = generationRef.current;
      const existing = readBinding(element);
      if (existing && existing.generation === generation && existing.context.state !== 'closed') {
        applyBoost(existing, value);
        if (existing.context.state === 'suspended') {
          void existing.context.resume().catch(() => {});
        }
        setBoostActive(true);
        return;
      }

      void (async () => {
        let graph: AudioGraph | null = null;
        let createdSource: MediaElementAudioSourceNode | null = null;
        try {
          const context = new AudioContext();
          const gain = context.createGain();
          const compressor = context.createDynamicsCompressor();
          graph = { context, gain, compressor };
          gain.connect(compressor);
          compressor.connect(context.destination);
          const resumed = context.resume();
          if (resumed && typeof resumed.then === 'function') await resumed;
          if (context.state !== 'running') throw new Error('audio context did not resume');

          const current = videoRef.current;
          const stillEligible = matchesDeliveryPath(streamUrlRef.current, movieIdRef.current);
          if (
            current !== element ||
            !element.isConnected ||
            generation !== generationRef.current ||
            !stillEligible
          ) {
            await closeGraph(graph);
            return;
          }

          try {
            createdSource = context.createMediaElementSource(element);
          } catch {
            await closeGraph(graph);
            recoverElement(element);
            return;
          }
          try {
            createdSource.connect(gain);
          } catch {
            try {
              createdSource.disconnect();
            } catch {
              // Disconnect failure does not affect the native fallback.
            }
            await closeGraph(graph);
            recoverElement(element);
            return;
          }

          const binding: BoostBinding = {
            context,
            gain,
            compressor,
            source: createdSource,
            element,
            generation: generationRef.current,
          };
          writeBinding(element, binding);
          applyBoost(binding, value);
          setBoostActive(true);
        } catch {
          if (createdSource) {
            try {
              createdSource.disconnect();
            } catch {
              // Native playback remains untouched after a failed bind.
            }
          }
          await closeGraph(graph);
        }
      })();
    },
    [recoverElement],
  );

  const setBoost = useCallback(
    (percent: number) => {
      const next = clampBoost(percent);
      boostPercentRef.current = next;
      setBoostPercent(next);
      activateBoost(next);
    },
    [activateBoost],
  );

  const resetBoost = useCallback(() => {
    boostPercentRef.current = BOOST_MIN;
    setBoostPercent(BOOST_MIN);
    activateBoost(BOOST_MIN);
  }, [activateBoost]);

  useEffect(
    () => () => {
      const element = videoRef.current;
      const binding = readBinding(element);
      if (element && binding) {
        writeBinding(element, null);
        void closeGraph(binding);
      }
    },
    [],
  );

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

  return {
    status,
    videoRef,
    elementKey,
    boostSupported,
    boostPercent,
    boostActive,
    setBoost,
    resetBoost,
    retry,
    togglePlay,
    seekBy,
    toggleMute,
    setPlaybackRate,
  };
}
