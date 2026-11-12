import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ComponentProps } from 'react';
import BrowserPlayer from './BrowserPlayer';
import { playerRegionLabel } from './playerCopy';
import {
  SUBTITLE_CHOOSE_LANGUAGE_COPY,
  SUBTITLE_GETTING_COPY,
  SUBTITLE_GET_COPY,
  SUBTITLE_LANGUAGE_LABEL,
  SUBTITLE_LOCAL_TRACKS_LABEL,
  SUBTITLE_NO_RESULTS_COPY,
  SUBTITLE_QUOTA_REACHED_COPY,
  SUBTITLE_QUOTA_UNAVAILABLE_COPY,
  SUBTITLE_READY_COPY,
  SUBTITLE_READ_ONLY_COPY,
  SUBTITLE_RETRY_SEARCH_LABEL,
  SUBTITLE_SEARCHING_COPY,
  SUBTITLE_SEARCH_RATE_LIMITED_COPY,
  SUBTITLE_STALE_CANDIDATE_COPY,
  subtitleGetLabel,
} from './subtitleCopy';
import { server } from '../../test/server';
import { installMediaStubs, type MediaStubHandle } from '../../test/mediaElement';
import type { components } from '../../api/types';

type SubtitleLanguagesResponse = components['schemas']['SubtitleLanguagesResponse'];
type SubtitleSearchResponse = components['schemas']['SubtitleSearchResponse'];
type SubtitleDownloadResponse = components['schemas']['SubtitleDownloadResponse'];
type SubtitleTrackDto = components['schemas']['SubtitleTrackDto'];

const CATALOG: SubtitleLanguagesResponse = {
  defaultLanguage: 'pt-BR',
  languages: [
    { code: 'pt-BR', englishName: 'Portuguese (BR)', nativeName: 'Português (BR)' },
    { code: 'en', englishName: 'English', nativeName: 'English' },
  ],
};

const EMPTY_DEFAULT_CATALOG: SubtitleLanguagesResponse = {
  defaultLanguage: null,
  languages: CATALOG.languages,
};

const TRACKS: SubtitleTrackDto[] = [
  { id: 'token-pt', label: 'Dune (2021)-pt-BR', language: 'pt-BR', isDefault: true },
  { id: 'token-pt-1', label: 'Dune (2021)-pt-BR(1)', language: 'pt-BR', isDefault: false },
];

const SEARCH: SubtitleSearchResponse = {
  candidates: [
    {
      candidateId: 'cand-96',
      language: 'pt-BR',
      releaseName: 'Dune.2021.1080p.BluRay.x264-GROUP',
      matchPercent: 96,
      downloadCount: 1204,
    },
    {
      candidateId: 'cand-82',
      language: 'pt-BR',
      releaseName: 'Dune.2021.WEB-DL.H264-OTHER',
      matchPercent: 82,
      downloadCount: 860,
    },
    {
      candidateId: 'cand-41',
      language: 'pt-BR',
      releaseName: 'Dune.2021.HDCAM-LOW',
      matchPercent: 41,
      downloadCount: 12,
    },
  ],
  quota: { remainingDownloads: 98, resetAtUtc: '2026-03-04T14:32:00+00:00' },
  downloadAvailability: { enabled: true, reason: '' },
};

const DOWNLOADED: SubtitleDownloadResponse = {
  track: { id: 'token-new', label: 'Dune (2021)-pt-BR(2)', language: 'pt-BR', isDefault: false },
  quota: { remainingDownloads: 97, resetAtUtc: '2026-03-04T14:32:00+00:00' },
};

const VTT = 'WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nHello.\n';

let media: MediaStubHandle;
let catalogRequests = 0;
let searchQueries: string[] = [];
let downloadBodies: unknown[] = [];
let owned: string[] = [];

function renderPlayer(props: Partial<ComponentProps<typeof BrowserPlayer>> = {}) {
  return render(
    <BrowserPlayer title="Dune" movieId={1} streamUrl="/api/movies/1/stream" {...props} />,
  );
}

function region(title = 'Dune'): HTMLElement {
  return screen.getByRole('region', { name: playerRegionLabel(title) });
}

function languageSelect(): HTMLSelectElement {
  return screen.getByLabelText(SUBTITLE_LANGUAGE_LABEL) as HTMLSelectElement;
}

function serveCatalog(body: SubtitleLanguagesResponse = CATALOG) {
  server.use(
    http.get('*/api/subtitles/languages', () => {
      catalogRequests += 1;
      return HttpResponse.json(body, { status: 200 });
    }),
  );
}

function serveTracks(next: SubtitleTrackDto[] = TRACKS) {
  server.use(
    http.get('*/api/movies/:id/subtitles', () => HttpResponse.json(next, { status: 200 })),
  );
  server.use(
    http.get(
      '*/api/movies/:id/subtitles/:trackId/content',
      () =>
        new HttpResponse(VTT, {
          status: 200,
          headers: { 'Content-Type': 'text/vtt; charset=utf-8' },
        }),
    ),
  );
}

function serveSearch(body: SubtitleSearchResponse = SEARCH, delayMs = 0) {
  server.use(
    http.get('*/api/movies/:id/subtitles/search', async ({ request }) => {
      searchQueries.push(new URL(request.url).searchParams.get('language') ?? '');
      if (delayMs > 0) await new Promise((resolve) => setTimeout(resolve, delayMs));
      return HttpResponse.json(body, { status: 200 });
    }),
  );
}

function serveSearchProblem(status: number, problem: Record<string, unknown>) {
  server.use(
    http.get('*/api/movies/:id/subtitles/search', () =>
      HttpResponse.json(problem, {
        status,
        headers: { 'Content-Type': 'application/problem+json' },
      }),
    ),
  );
}

function serveDownload(
  body: SubtitleDownloadResponse = DOWNLOADED,
  options: { delayMs?: number } = {},
) {
  server.use(
    http.post('*/api/movies/:id/subtitles/download', async ({ request }) => {
      downloadBodies.push(await request.json());
      if (options.delayMs) await new Promise((resolve) => setTimeout(resolve, options.delayMs));
      return HttpResponse.json(body, { status: 201 });
    }),
  );
}

function serveDownloadProblem(status: number, problem: Record<string, unknown>) {
  server.use(
    http.post('*/api/movies/:id/subtitles/download', async ({ request }) => {
      downloadBodies.push(await request.json());
      return HttpResponse.json(problem, {
        status,
        headers: { 'Content-Type': 'application/problem+json' },
      });
    }),
  );
}

function getButtons(): HTMLElement[] {
  return within(region()).getAllByRole('button', { name: /^Get subtitle / });
}

beforeEach(() => {
  media = installMediaStubs();
  catalogRequests = 0;
  searchQueries = [];
  downloadBodies = [];
  owned = [];
  serveCatalog();
  serveTracks();
  serveSearch();
  serveDownload();

  let counter = 0;
  Object.defineProperty(URL, 'createObjectURL', {
    configurable: true,
    value: vi.fn(() => {
      counter += 1;
      const url = `blob:owned-${counter}`;
      owned.push(url);
      return url;
    }),
  });
  Object.defineProperty(URL, 'revokeObjectURL', {
    configurable: true,
    value: vi.fn((url: string) => {
      owned = owned.filter((candidate) => candidate !== url);
    }),
  });
});

afterEach(() => {
  Reflect.deleteProperty(URL, 'createObjectURL');
  Reflect.deleteProperty(URL, 'revokeObjectURL');
  media.restore();
  vi.restoreAllMocks();
});

describe('subtitle language search', () => {
  it('defaults to the first preferred language from the catalog and searches it', async () => {
    renderPlayer();

    await waitFor(() => expect(languageSelect().value).toBe('pt-BR'));
    expect(Array.from(languageSelect().options).map((option) => option.textContent)).toEqual([
      'Portuguese (BR) — Português (BR)',
      'English — English',
    ]);
    await waitFor(() => expect(searchQueries).toEqual(['pt-BR']));
    expect(catalogRequests).toBe(1);
  });

  it('prompts for a language and does not search when the catalog default is null', async () => {
    serveCatalog(EMPTY_DEFAULT_CATALOG);
    renderPlayer();

    await waitFor(() => expect(languageSelect().value).toBe(''));
    expect(languageSelect().options[0].textContent).toBe(SUBTITLE_CHOOSE_LANGUAGE_COPY);
    expect(searchQueries).toEqual([]);
    expect(screen.queryByText(SUBTITLE_SEARCHING_COPY)).toBeNull();
  });

  it('starts a free search on language change and renders server order as-is', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    fireEvent.change(languageSelect(), { target: { value: 'en' } });

    await waitFor(() => expect(searchQueries).toEqual(['pt-BR', 'en']));
    await screen.findByText(SEARCH.candidates[0].releaseName);
    const names = within(region())
      .getAllByTitle(/^Dune\.2021/)
      .map((row) => row.textContent);
    expect(names).toEqual(SEARCH.candidates.map((candidate) => candidate.releaseName));
  });

  it('discards a stale search response from a replaced language', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    server.use(
      http.get('*/api/movies/:id/subtitles/search', async ({ request }) => {
        const language = new URL(request.url).searchParams.get('language');
        if (language === 'en') await new Promise((resolve) => setTimeout(resolve, 120));
        return HttpResponse.json(
          {
            ...SEARCH,
            candidates:
              language === 'en'
                ? [
                    {
                      ...SEARCH.candidates[0],
                      candidateId: 'cand-en',
                      releaseName: 'Dune.2021.EN-STALE',
                    },
                  ]
                : [
                    {
                      ...SEARCH.candidates[0],
                      candidateId: 'cand-pt',
                      releaseName: 'Dune.2021.PT-FRESH',
                    },
                  ],
          },
          { status: 200 },
        );
      }),
    );

    fireEvent.change(languageSelect(), { target: { value: 'en' } });
    fireEvent.change(languageSelect(), { target: { value: 'pt-BR' } });

    await screen.findByText('Dune.2021.PT-FRESH');
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 200));
    });
    expect(screen.queryByText('Dune.2021.EN-STALE')).toBeNull();
  });

  it('keeps local tracks and Off above the remote results in every state', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    const container = within(region());
    const heading = container.getByText(SUBTITLE_LOCAL_TRACKS_LABEL);
    const offOption = container.getByRole('option', { name: 'Off' });
    const firstResult = container.getByTitle(SEARCH.candidates[0].releaseName);
    expect(
      heading.compareDocumentPosition(offOption) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
    expect(
      offOption.compareDocumentPosition(firstResult) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it('renders the quota line with a local reset time and unavailable when unknown', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    expect(within(region()).getByText(/Quota: 98 downloads left/)).toBeInTheDocument();

    fireEvent.change(languageSelect(), { target: { value: 'en' } });
    server.use(
      http.get('*/api/movies/:id/subtitles/search', () =>
        HttpResponse.json(
          { ...SEARCH, quota: { remainingDownloads: null, resetAtUtc: null } },
          { status: 200 },
        ),
      ),
    );
    fireEvent.change(languageSelect(), { target: { value: 'pt-BR' } });

    expect(await screen.findByText(SUBTITLE_QUOTA_UNAVAILABLE_COPY)).toBeInTheDocument();
    expect(screen.queryByText(/Quota: 0/)).toBeNull();
  });
});

describe('subtitle download states', () => {
  it('posts only the candidate id and activates the returned track for the current movie', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    fireEvent.click(
      screen.getByRole('button', { name: subtitleGetLabel(SEARCH.candidates[0].releaseName) }),
    );

    await waitFor(() => expect(downloadBodies).toEqual([{ candidateId: 'cand-96' }]));
    await waitFor(() => {
      const select = within(region()).getByRole('combobox', { name: 'Subtitles for Dune' });
      expect((select as HTMLSelectElement).value).toBe(DOWNLOADED.track.id);
    });
    expect(
      within(region()).getByRole('option', { name: DOWNLOADED.track.label }),
    ).toBeInTheDocument();
  });

  it('shows Getting while one download is in flight and blocks a second Get', async () => {
    serveDownload(DOWNLOADED, { delayMs: 120 });
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    fireEvent.click(
      screen.getByRole('button', { name: subtitleGetLabel(SEARCH.candidates[0].releaseName) }),
    );

    const getting = await screen.findByRole('button', {
      name: subtitleGetLabel(SEARCH.candidates[0].releaseName),
    });
    expect(getting).toHaveTextContent(SUBTITLE_GETTING_COPY);
    getButtons().forEach((button) => expect(button).toBeDisabled());
    expect(downloadBodies).toHaveLength(1);

    await waitFor(() =>
      expect(getButtons().every((button) => !button.matches(':disabled'))).toBe(true),
    );
    expect(
      screen.getByRole('button', { name: subtitleGetLabel(SEARCH.candidates[0].releaseName) }),
    ).toHaveTextContent(SUBTITLE_GET_COPY);
  });

  it('never fires POST when download availability is disabled and searches anyway', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles/search', () =>
        HttpResponse.json(
          {
            ...SEARCH,
            downloadAvailability: { enabled: false, reason: 'read_only_library' },
          },
          { status: 200 },
        ),
      ),
    );
    renderPlayer();

    await screen.findByText(SUBTITLE_READ_ONLY_COPY);
    expect(within(region()).getByText(SEARCH.candidates[0].releaseName)).toBeInTheDocument();
    getButtons().forEach((button) => expect(button).toBeDisabled());

    fireEvent.click(getButtons()[0]);
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 30));
    });
    expect(downloadBodies).toEqual([]);
  });

  it('maps a download quota 429 to quota copy with a local reset and disables Get', async () => {
    serveDownloadProblem(429, {
      status: 429,
      reason: 'download_quota_exhausted',
      quota: { remainingDownloads: 0, resetAtUtc: '2026-03-04T14:32:00+00:00' },
    });
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    fireEvent.click(getButtons()[0]);

    expect(await screen.findByText(/Download quota reached\. Resets at .+\./)).toBeInTheDocument();
    expect(screen.queryByText(SUBTITLE_SEARCH_RATE_LIMITED_COPY)).toBeNull();
  });

  it('maps a search 429 to search rate-limit copy and never to download quota copy', async () => {
    serveSearchProblem(429, { status: 429, reason: 'search_rate_limited' });
    renderPlayer();

    expect(await screen.findByText(SUBTITLE_SEARCH_RATE_LIMITED_COPY)).toBeInTheDocument();
    expect(screen.queryByText(SUBTITLE_QUOTA_REACHED_COPY)).toBeNull();
    expect(screen.queryByText(/Download quota reached/)).toBeNull();
    expect(within(region()).getByRole('option', { name: 'Off' })).toBeInTheDocument();
  });

  it('shows a stale candidate 400 inline without retrying the paid call', async () => {
    serveDownloadProblem(400, { status: 400, reason: 'stale_candidate' });
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    fireEvent.click(getButtons()[0]);

    expect(await screen.findByText(SUBTITLE_STALE_CANDIDATE_COPY)).toBeInTheDocument();
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 40));
    });
    expect(downloadBodies).toHaveLength(1);
    expect(getButtons().every((button) => !button.matches(':disabled'))).toBe(true);
  });

  it('offers a manual Retry after a provider timeout without an automatic retry', async () => {
    serveSearchProblem(504, { status: 504, reason: 'provider_timeout' });
    renderPlayer();

    expect(await screen.findByText('The subtitle provider timed out.')).toBeInTheDocument();
    const attemptsAfterFailure = searchQueries.length;
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 60));
    });
    expect(searchQueries.length).toBe(attemptsAfterFailure);

    serveSearch();
    fireEvent.click(within(region()).getByRole('button', { name: SUBTITLE_RETRY_SEARCH_LABEL }));

    await waitFor(() => expect(searchQueries.length).toBe(attemptsAfterFailure + 1));
    expect(await screen.findByText(SUBTITLE_READY_COPY)).toBeInTheDocument();
    expect(media.calls).not.toContain('pause');
  });

  it('keeps playback and the active local track through every acquisition failure', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);
    const localSelect = within(region()).getByRole('combobox', { name: 'Subtitles for Dune' });
    fireEvent.change(localSelect, { target: { value: TRACKS[0].id } });
    await waitFor(() =>
      expect(within(region()).getByRole('combobox', { name: 'Subtitles for Dune' })).toHaveValue(
        TRACKS[0].id,
      ),
    );

    serveDownloadProblem(502, { status: 502, reason: 'provider_unavailable' });
    fireEvent.click(getButtons()[0]);

    expect(await screen.findByText('The subtitle provider is unavailable.')).toBeInTheDocument();
    expect(within(region()).getByRole('combobox', { name: 'Subtitles for Dune' })).toHaveValue(
      TRACKS[0].id,
    );
    expect(media.calls).not.toContain('pause');
  });

  it('shows no-results copy while keeping Off and local tracks usable', async () => {
    serveSearch({ ...SEARCH, candidates: [] });
    renderPlayer();

    await screen.findByText(SUBTITLE_NO_RESULTS_COPY);
    expect(within(region()).getByRole('option', { name: 'Off' })).toBeInTheDocument();
    expect(within(region()).getByRole('option', { name: TRACKS[0].label })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Get subtitle / })).toBeNull();
  });

  it('adds a changed-language download to local tracks without replacing newer results', async () => {
    server.use(
      http.get('*/api/movies/:id/subtitles/search', ({ request }) => {
        const language = new URL(request.url).searchParams.get('language');
        return HttpResponse.json(
          {
            ...SEARCH,
            candidates: [
              {
                ...SEARCH.candidates[0],
                candidateId: `cand-${language}`,
                releaseName: `Dune.2021.${String(language)}-${language}`,
              },
            ],
          },
          { status: 200 },
        );
      }),
    );
    serveDownload(DOWNLOADED, { delayMs: 100 });
    server.use(
      http.get('*/api/movies/:id/subtitles', () =>
        HttpResponse.json([...TRACKS, DOWNLOADED.track], { status: 200 }),
      ),
    );
    renderPlayer();
    await screen.findByText('Dune.2021.pt-BR-pt-BR');

    fireEvent.click(getButtons()[0]);
    fireEvent.change(languageSelect(), { target: { value: 'en' } });
    await screen.findByText('Dune.2021.en-en');

    await waitFor(() => expect(downloadBodies).toHaveLength(1));
    await waitFor(() => {
      expect(
        within(region()).getByRole('option', { name: DOWNLOADED.track.label }),
      ).toBeInTheDocument();
    });
    expect(within(region()).getByText('Dune.2021.en-en')).toBeInTheDocument();
    expect(screen.queryByText('Dune.2021.pt-BR-pt-BR')).toBeNull();
  });

  it('marks the active local track only and never shows a candidate active indicator', async () => {
    renderPlayer();
    await screen.findByText(SUBTITLE_READY_COPY);

    const local = within(region()).getByRole('combobox', {
      name: 'Subtitles for Dune',
    }) as HTMLSelectElement;
    fireEvent.change(local, { target: { value: TRACKS[0].id } });
    await waitFor(() =>
      expect(within(region()).getByRole('combobox', { name: 'Subtitles for Dune' })).toHaveValue(
        TRACKS[0].id,
      ),
    );

    const checked = Array.from(local.options).filter((option) => option.selected);
    expect(checked.map((option) => option.textContent)).toEqual([TRACKS[0].label]);

    for (const candidate of SEARCH.candidates) {
      const row = within(region()).getByTitle(candidate.releaseName).closest('li');
      expect(row).not.toBeNull();
      expect(row?.querySelector('[aria-checked]')).toBeNull();
      expect(row?.querySelector('[role="radio"]')).toBeNull();
      expect(row?.textContent).not.toMatch(/active/i);
    }
  });
});
