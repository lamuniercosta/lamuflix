import { useEffect, useId, type ChangeEvent } from 'react';
import {
  SUBTITLE_CHOOSE_LANGUAGE_COPY,
  SUBTITLE_GETTING_COPY,
  SUBTITLE_GET_COPY,
  SUBTITLE_INVALID_REQUEST_COPY,
  SUBTITLE_LANGUAGE_LABEL,
  SUBTITLE_LOCAL_TRACKS_LABEL,
  SUBTITLE_NO_RESULTS_COPY,
  SUBTITLE_PROVIDER_FAILURE_COPY,
  SUBTITLE_PROVIDER_TIMEOUT_COPY,
  SUBTITLE_QUOTA_REACHED_COPY,
  SUBTITLE_QUOTA_UNAVAILABLE_COPY,
  SUBTITLE_READY_COPY,
  SUBTITLE_READ_ONLY_COPY,
  SUBTITLE_RETRY_COPY,
  SUBTITLE_RETRY_SEARCH_LABEL,
  SUBTITLE_SEARCHING_COPY,
  SUBTITLE_SEARCH_FAILURE_COPY,
  SUBTITLE_SEARCH_RATE_LIMITED_COPY,
  SUBTITLE_STALE_CANDIDATE_COPY,
  subtitleGetLabel,
  subtitleQuotaReachedResetCopy,
} from './subtitleCopy';
import SubtitleSelector from './SubtitleSelector';
import {
  formatCount,
  formatMatchPercent,
  formatResetTime,
  numericValue,
  type SubtitleCandidate,
  type SubtitleProblem,
  type SubtitleQuota,
} from './subtitleAcquisition';
import { useSubtitleAcquisition, type SubtitleAcquisition } from './useSubtitleAcquisition';
import type { SubtitlePlayback } from './useSubtitlePlayback';
import './SubtitlesTab.css';

export interface SubtitlesTabProps {
  title: string;
  movieId: number;
  playback: SubtitlePlayback;
}

function downloadReasonCopy(problem: SubtitleProblem): string {
  switch (problem.reason) {
    case 'stale_candidate':
      return SUBTITLE_STALE_CANDIDATE_COPY;
    case 'download_quota_exhausted': {
      const localReset = formatResetTime(problem.quota?.resetAtUtc);
      return localReset ? subtitleQuotaReachedResetCopy(localReset) : SUBTITLE_QUOTA_REACHED_COPY;
    }
    case 'read_only_library':
      return SUBTITLE_READ_ONLY_COPY;
    case 'provider_timeout':
      return SUBTITLE_PROVIDER_TIMEOUT_COPY;
    case 'provider_unavailable':
      return SUBTITLE_PROVIDER_FAILURE_COPY;
    case 'invalid_request':
      return SUBTITLE_INVALID_REQUEST_COPY;
    case 'search_rate_limited':
      return SUBTITLE_SEARCH_RATE_LIMITED_COPY;
    default:
      return SUBTITLE_PROVIDER_FAILURE_COPY;
  }
}

function searchReasonCopy(problem: SubtitleProblem): string {
  switch (problem.reason) {
    case 'search_rate_limited':
      return SUBTITLE_SEARCH_RATE_LIMITED_COPY;
    case 'provider_timeout':
      return SUBTITLE_PROVIDER_TIMEOUT_COPY;
    case 'invalid_request':
      return SUBTITLE_INVALID_REQUEST_COPY;
    case 'provider_unavailable':
    case 'provider_not_configured':
    case 'read_only_library':
      return SUBTITLE_PROVIDER_FAILURE_COPY;
    default:
      return SUBTITLE_SEARCH_FAILURE_COPY;
  }
}

function readOnlyCopy(availability: { reason: string } | null): string {
  const reason = availability?.reason.trim();
  return reason && reason !== 'read_only_library'
    ? `${SUBTITLE_READ_ONLY_COPY} (${reason})`
    : SUBTITLE_READ_ONLY_COPY;
}

function statusCopy(acquisition: SubtitleAcquisition, readOnly: string | null): string | null {
  if (acquisition.downloadProblem) return downloadReasonCopy(acquisition.downloadProblem);
  if (acquisition.searchStatus === 'searching') return SUBTITLE_SEARCHING_COPY;
  if (acquisition.searchProblem) return searchReasonCopy(acquisition.searchProblem);
  if (acquisition.searchStatus === 'ready') {
    if (acquisition.candidates.length === 0) return SUBTITLE_NO_RESULTS_COPY;
    if (readOnly) return readOnly;
    return SUBTITLE_READY_COPY;
  }
  return null;
}

function QuotaLine({ quota }: { quota: SubtitleQuota | null }) {
  const remaining = numericValue(quota?.remainingDownloads ?? null);
  if (remaining === null)
    return <p className="subtitles-tab-quota">{SUBTITLE_QUOTA_UNAVAILABLE_COPY}</p>;
  const localReset = formatResetTime(quota?.resetAtUtc ?? null);
  const count = formatCount(remaining);
  return (
    <p className="subtitles-tab-quota">
      {localReset
        ? `Quota: ${count} downloads left, resets ${localReset}`
        : `Quota: ${count} downloads left`}
    </p>
  );
}

function CandidateList({
  candidates,
  statusId,
  downloadingId,
  disabled,
  onGet,
}: {
  candidates: readonly SubtitleCandidate[];
  statusId: string;
  downloadingId: string | null;
  disabled: boolean;
  onGet: (candidateId: string) => void;
}) {
  if (candidates.length === 0) return null;
  return (
    <ul className="subtitles-tab-candidates">
      {candidates.map((candidate) => (
        <li key={candidate.candidateId} className="subtitles-tab-candidate">
          <span className="subtitles-tab-release" tabIndex={0} title={candidate.releaseName}>
            {candidate.releaseName}
          </span>
          <span className="subtitles-tab-metric">{formatMatchPercent(candidate.matchPercent)}</span>
          <span className="subtitles-tab-metric">{formatCount(candidate.downloadCount)}</span>
          <button
            type="button"
            className="lamu-action-quiet"
            aria-label={subtitleGetLabel(candidate.releaseName)}
            aria-describedby={statusId}
            disabled={disabled}
            onClick={() => onGet(candidate.candidateId)}
          >
            {downloadingId === candidate.candidateId ? SUBTITLE_GETTING_COPY : SUBTITLE_GET_COPY}
          </button>
        </li>
      ))}
    </ul>
  );
}

export default function SubtitlesTab({ title, movieId, playback }: SubtitlesTabProps) {
  const acquisition = useSubtitleAcquisition(movieId);
  const statusId = useId();
  const languageId = useId();

  const downloaded = acquisition.lastDownload;
  const { refreshAfterDownload } = playback;
  useEffect(() => {
    if (downloaded) refreshAfterDownload(downloaded.track);
  }, [downloaded, refreshAfterDownload]);

  const readOnly =
    acquisition.availability?.enabled === false ? readOnlyCopy(acquisition.availability) : null;
  const downloading = acquisition.downloadingCandidateId !== null;
  const copy = statusCopy(acquisition, readOnly);
  const retryable = acquisition.searchStatus === 'error' || acquisition.downloadProblem !== null;

  const handleLanguageChange = (event: ChangeEvent<HTMLSelectElement>) => {
    if (!event.target.value) return;
    acquisition.selectLanguage(event.target.value);
  };

  return (
    <section className="subtitles-tab" aria-label="Subtitle search">
      <div className="subtitles-tab-search">
        <label className="subtitles-tab-field" htmlFor={languageId}>
          <span className="subtitles-tab-caption">{SUBTITLE_LANGUAGE_LABEL}</span>
        </label>
        <select
          id={languageId}
          className="subtitles-tab-select"
          value={acquisition.language ?? ''}
          onChange={handleLanguageChange}
          disabled={acquisition.catalogStatus !== 'ready'}
        >
          {acquisition.language === null ? (
            <option value="">{SUBTITLE_CHOOSE_LANGUAGE_COPY}</option>
          ) : null}
          {acquisition.languages.map((language) => (
            <option key={language.code} value={language.code}>
              {`${language.englishName} — ${language.nativeName}`}
            </option>
          ))}
        </select>
        <QuotaLine quota={acquisition.quota} />
      </div>

      <div className="subtitles-tab-tracks">
        <p className="subtitles-tab-heading">{SUBTITLE_LOCAL_TRACKS_LABEL}</p>
        <SubtitleSelector
          title={title}
          listStatus={playback.listStatus}
          contentStatus={playback.contentStatus}
          tracks={playback.tracks}
          selectedId={playback.selection?.trackId ?? null}
          onSelect={playback.select}
          onRetryList={playback.retryList}
        />
      </div>

      <CandidateList
        candidates={acquisition.candidates}
        statusId={statusId}
        downloadingId={acquisition.downloadingCandidateId}
        disabled={readOnly !== null || downloading}
        onGet={acquisition.getCandidate}
      />

      <p id={statusId} className="subtitles-tab-status" role="status">
        {copy}
      </p>
      {retryable ? (
        <button
          type="button"
          className="lamu-action-quiet"
          aria-label={SUBTITLE_RETRY_SEARCH_LABEL}
          onClick={acquisition.retrySearch}
        >
          {SUBTITLE_RETRY_COPY}
        </button>
      ) : null}
    </section>
  );
}
