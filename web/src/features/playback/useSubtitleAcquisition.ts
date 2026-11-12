import { useCallback, useEffect, useRef, useState } from 'react';
import { client } from '../../api/client';
import type { components } from '../../api/types';
import {
  readSubtitleProblem,
  type SubtitleCandidate,
  type SubtitleDownloadAvailability,
  type SubtitleLanguageOption,
  type SubtitleProblem,
  type SubtitleQuota,
} from './subtitleAcquisition';

export type SubtitleCatalogStatus = 'loading' | 'ready' | 'error';
export type SubtitleSearchStatus = 'idle' | 'searching' | 'ready' | 'error';

export interface SubtitleDownloadOutcome {
  track: components['schemas']['SubtitleTrackDto'];
  quota: SubtitleQuota | null;
  language: string;
}

export interface SubtitleAcquisition {
  catalogStatus: SubtitleCatalogStatus;
  languages: readonly SubtitleLanguageOption[];
  language: string | null;
  selectLanguage: (code: string) => void;
  searchStatus: SubtitleSearchStatus;
  candidates: readonly SubtitleCandidate[];
  quota: SubtitleQuota | null;
  availability: SubtitleDownloadAvailability | null;
  searchProblem: SubtitleProblem | null;
  downloadProblem: SubtitleProblem | null;
  downloadingCandidateId: string | null;
  lastDownload: SubtitleDownloadOutcome | null;
  getCandidate: (candidateId: string) => void;
  retrySearch: () => void;
}

const EMPTY_LANGUAGES: readonly SubtitleLanguageOption[] = [];
const EMPTY_CANDIDATES: readonly SubtitleCandidate[] = [];

interface SearchSnapshot {
  language: string;
  status: 'ready' | 'error';
  candidates: readonly SubtitleCandidate[];
  quota: SubtitleQuota | null;
  availability: SubtitleDownloadAvailability | null;
  problem: SubtitleProblem | null;
}

interface CatalogSnapshot {
  defaultLanguage: string | null;
  languages: readonly SubtitleLanguageOption[];
  status: SubtitleCatalogStatus;
}

const INITIAL_CATALOG: CatalogSnapshot = {
  defaultLanguage: null,
  languages: EMPTY_LANGUAGES,
  status: 'loading',
};

const UNREACHABLE_PROBLEM: SubtitleProblem = { status: null, reason: null, quota: null };

export function useSubtitleAcquisition(movieId: number | string): SubtitleAcquisition {
  const numericId = Number(movieId);
  const [catalog, setCatalog] = useState<CatalogSnapshot>(INITIAL_CATALOG);
  const [chosenLanguage, setChosenLanguage] = useState<string | null>(null);
  const [snapshot, setSnapshot] = useState<SearchSnapshot | null>(null);
  const [searchAttempt, setSearchAttempt] = useState(0);
  const [downloadProblem, setDownloadProblem] = useState<SubtitleProblem | null>(null);
  const [downloadingCandidateId, setDownloadingCandidateId] = useState<string | null>(null);
  const [lastDownload, setLastDownload] = useState<SubtitleDownloadOutcome | null>(null);

  const catalogGeneration = useRef(0);
  const searchGeneration = useRef(0);
  const downloadGeneration = useRef(0);

  useEffect(() => {
    const generation = catalogGeneration.current + 1;
    catalogGeneration.current = generation;
    void (async () => {
      let next: CatalogSnapshot;
      try {
        const result = await client.GET('/api/subtitles/languages');
        next =
          result.error || !result.data
            ? { defaultLanguage: null, languages: EMPTY_LANGUAGES, status: 'error' }
            : {
                defaultLanguage: result.data.defaultLanguage,
                languages: result.data.languages,
                status: 'ready',
              };
      } catch {
        next = { defaultLanguage: null, languages: EMPTY_LANGUAGES, status: 'error' };
      }
      if (generation !== catalogGeneration.current) return;
      setCatalog(next);
    })();
    return () => {
      if (catalogGeneration.current === generation) catalogGeneration.current += 1;
    };
  }, [numericId]);

  const language = chosenLanguage ?? catalog.defaultLanguage;

  useEffect(() => {
    if (!language) return;
    const generation = searchGeneration.current + 1;
    searchGeneration.current = generation;
    void (async () => {
      let next: SearchSnapshot;
      try {
        const result = await client.GET('/api/movies/{id}/subtitles/search', {
          params: { path: { id: numericId }, query: { language } },
        });
        next = result.error
          ? {
              language,
              status: 'error',
              candidates: EMPTY_CANDIDATES,
              quota: null,
              availability: null,
              problem: readSubtitleProblem(result.error),
            }
          : {
              language,
              status: 'ready',
              candidates: result.data.candidates,
              quota: result.data.quota,
              availability: result.data.downloadAvailability,
              problem: null,
            };
      } catch {
        next = {
          language,
          status: 'error',
          candidates: EMPTY_CANDIDATES,
          quota: null,
          availability: null,
          problem: UNREACHABLE_PROBLEM,
        };
      }
      if (generation !== searchGeneration.current) return;
      setSnapshot(next);
    })();
    return () => {
      if (searchGeneration.current === generation) searchGeneration.current += 1;
    };
  }, [numericId, language, searchAttempt]);

  useEffect(
    () => () => {
      downloadGeneration.current += 1;
    },
    [numericId],
  );

  const isCurrent = snapshot !== null && snapshot.language === language;
  const searchStatus: SubtitleSearchStatus = !language
    ? 'idle'
    : isCurrent
      ? (snapshot?.status ?? 'searching')
      : 'searching';

  const selectLanguage = useCallback((code: string) => {
    setDownloadProblem(null);
    setLastDownload(null);
    setChosenLanguage(code);
  }, []);

  const retrySearch = useCallback(() => {
    setDownloadProblem(null);
    setSearchAttempt((current) => current + 1);
  }, []);

  const getCandidate = (candidateId: string) => {
    if (downloadingCandidateId !== null) return;
    if (searchStatus !== 'ready') return;
    if (isCurrent && snapshot?.availability && !snapshot.availability.enabled) return;
    const downloadLanguage = language ?? '';
    downloadGeneration.current += 1;
    const generation = downloadGeneration.current;
    setDownloadingCandidateId(candidateId);
    setDownloadProblem(null);
    void (async () => {
      let problem: SubtitleProblem | null = null;
      let outcome: SubtitleDownloadOutcome | null = null;
      try {
        const result = await client.POST('/api/movies/{id}/subtitles/download', {
          params: { path: { id: numericId } },
          body: { candidateId },
        });
        if (result.error) {
          problem = readSubtitleProblem(result.error);
        } else {
          outcome = {
            track: result.data.track,
            quota: result.data.quota ?? null,
            language: downloadLanguage,
          };
        }
      } catch {
        problem = UNREACHABLE_PROBLEM;
      }
      if (generation !== downloadGeneration.current) return;
      setDownloadingCandidateId(null);
      if (problem) {
        setDownloadProblem(problem);
        return;
      }
      if (outcome) {
        setLastDownload(outcome);
        setSnapshot((current) =>
          current && current.language === downloadLanguage && outcome
            ? { ...current, quota: outcome.quota ?? current.quota }
            : current,
        );
      }
    })();
  };

  return {
    catalogStatus: catalog.status,
    languages: catalog.status === 'ready' ? catalog.languages : EMPTY_LANGUAGES,
    language,
    selectLanguage,
    searchStatus,
    candidates: snapshot?.candidates ?? EMPTY_CANDIDATES,
    quota: isCurrent ? (snapshot?.quota ?? null) : null,
    availability: isCurrent ? (snapshot?.availability ?? null) : null,
    searchProblem: isCurrent ? (snapshot?.problem ?? null) : null,
    downloadProblem,
    downloadingCandidateId,
    lastDownload,
    getCandidate,
    retrySearch,
  };
}
