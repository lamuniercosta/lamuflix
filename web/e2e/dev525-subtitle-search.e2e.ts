import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import type { components } from '../src/api/types';

interface SubtitleTrackFixture {
  label: string;
  language: string;
  isDefault: boolean;
  cueText: string | null;
}

interface SubtitleProviderFixture {
  movieTitle: string;
  language: string;
  fileId: number;
  release: string;
  downloadCount: number;
  cueText: string;
}

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
  subtitleProvider: SubtitleProviderFixture;
  subtitleFixtures: {
    listed: { title: string; relativePath: string; tracks: SubtitleTrackFixture[] };
    broken: { title: string; relativePath: string };
  };
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];
type SubtitleSearchResponse = components['schemas']['SubtitleSearchResponse'];
type SubtitleDownloadResponse = components['schemas']['SubtitleDownloadResponse'];

const context = loadContext();
const evidenceDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '.runtime/evidence');
const evidenceFile = path.join(evidenceDir, 'dev525-subtitle-search.json');
const records: Record<string, unknown>[] = [];

const SEARCH_READY_COPY = 'Ready';

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

function record(entry: Record<string, unknown>): void {
  records.push(entry);
}

test.afterAll(() => {
  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(
    evidenceFile,
    `${JSON.stringify(
      {
        baseURL: context.baseURL,
        transport: 'web nginx /api proxy at http://localhost:3000',
        providerFixture: context.subtitleProvider,
        records,
      },
      null,
      2,
    )}\n`,
  );
});

async function movieIdFor(request: APIRequestContext, title: string): Promise<number> {
  const response = await request.get(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
  );
  expect(response.ok()).toBe(true);
  const browse = (await response.json()) as BrowseResponse;
  const movie = browse.items.find((item) => item.title === title);
  expect(movie, `seeded movie ${title} is browsable`).toBeDefined();
  return Number(movie!.id);
}

function playerRegion(page: Page) {
  return page.getByRole('region', { name: /^Movie player: / });
}

function playerVideo(page: Page) {
  return playerRegion(page).locator('video');
}

function ccTrigger(page: Page) {
  return playerRegion(page).getByRole('button', { name: 'Subtitles' });
}

function subtitleSettingsPanel(page: Page, title: string) {
  return playerRegion(page).getByRole('region', { name: `Subtitle settings for ${title}` });
}

function subtitleSearchTab(page: Page) {
  return playerRegion(page).getByRole('region', { name: 'Subtitle search' });
}

function trackGroup(page: Page, title: string) {
  return playerRegion(page).getByRole('radiogroup', { name: `Subtitles for ${title}` });
}

async function isPaused(page: Page): Promise<boolean> {
  return playerVideo(page).evaluate((element) => (element as HTMLVideoElement).paused);
}

async function startPlayback(page: Page): Promise<void> {
  await expect(playerVideo(page)).toBeVisible({ timeout: context.waitBudgetMs });
  if (await isPaused(page)) {
    const explicitPlay = playerRegion(page).getByRole('button', { name: /^play$/i });
    if (await explicitPlay.isVisible()) {
      await explicitPlay.click();
    }
  }
  await expect.poll(async () => isPaused(page), { timeout: context.waitBudgetMs }).toBe(false);
}

interface TrackSnapshot {
  trackId: string;
  label: string;
  mode: TextTrackMode;
  activeCues: string[];
}

async function readTracks(page: Page): Promise<TrackSnapshot[]> {
  return playerVideo(page).evaluate((element) => {
    const video = element as HTMLVideoElement;
    return Array.from(video.querySelectorAll<HTMLTrackElement>('track[data-track-id]')).map(
      (trackElement) => {
        const track = trackElement.track;
        return {
          trackId: trackElement.dataset.trackId ?? '',
          label: track?.label ?? trackElement.label,
          mode: track?.mode ?? 'disabled',
          activeCues: Array.from(track?.activeCues ?? []).map((cue) => (cue as VTTCue).text),
        };
      },
    );
  });
}

async function openPlayer(page: Page, request: APIRequestContext, title: string): Promise<number> {
  const movieId = await movieIdFor(request, title);
  await page.goto(`${context.baseURL}/movies/${String(movieId)}`);
  await page
    .locator('.details-actions')
    .getByRole('button', { name: `Play ${title} in browser` })
    .click();
  await expect(playerRegion(page)).toBeVisible({ timeout: context.waitBudgetMs });
  return movieId;
}

function isSearchRequest(url: string): boolean {
  return url.includes('/subtitles/search');
}

function isDownloadRequest(url: string): boolean {
  return url.includes('/subtitles/download');
}

test.describe('DEV-525 remote subtitle search and apply', () => {
  test('SMOKE-001 searches the provider and applies a returned subtitle', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const fixture = context.subtitleProvider;
    const title = context.subtitleFixtures.listed.title;
    const movieId = await openPlayer(page, request, title);
    await startPlayback(page);

    const searchResponse = page.waitForResponse((response) => isSearchRequest(response.url()), {
      timeout: context.waitBudgetMs,
    });
    await ccTrigger(page).click();
    await expect(subtitleSettingsPanel(page, title)).toBeVisible({ timeout: context.waitBudgetMs });
    const search = await searchResponse;
    const searchBody = (await search.json()) as SubtitleSearchResponse;
    record({
      step: 'search',
      browser: `chromium ${browser.version()}`,
      movieId,
      title,
      language: fixture.language,
      HTTPStatus: search.status(),
      candidates: searchBody.candidates?.map((candidate) => ({
        language: candidate.language,
        releaseName: candidate.releaseName,
        downloadCount: candidate.downloadCount,
      })),
      downloadAvailability: searchBody.downloadAvailability,
      searchProblemBody: search.ok() ? null : searchBody,
    });

    expect(
      search.status(),
      `GET /api/movies/${String(movieId)}/subtitles/search?language=${fixture.language} must answer 200`,
    ).toBe(200);
    expect(
      searchBody.candidates?.map((candidate) => candidate.releaseName),
      'the deterministic provider fixture returns its subtitle candidate',
    ).toContain(fixture.release);
    expect(
      searchBody.candidates.find((candidate) => candidate.releaseName === fixture.release)
        ?.language,
      'the returned candidate matches the requested language',
    ).toBe(fixture.language);

    const tab = subtitleSearchTab(page);
    await expect(tab.getByRole('status'), 'the subtitles tab reports a ready search').toHaveText(
      SEARCH_READY_COPY,
      { timeout: context.waitBudgetMs },
    );
    await expect(
      tab.getByRole('listitem').filter({ hasText: fixture.release }),
      'the returned candidate is offered in the subtitles tab',
    ).toBeVisible({ timeout: context.waitBudgetMs });

    const downloadResponse = page.waitForResponse((response) => isDownloadRequest(response.url()), {
      timeout: context.waitBudgetMs,
    });
    await tab.getByRole('button', { name: `Get subtitle ${fixture.release}` }).click();
    const download = await downloadResponse;
    const downloadBody = (await download.json()) as SubtitleDownloadResponse;
    record({
      step: 'apply',
      movieId,
      title,
      HTTPStatus: download.status(),
      appliedTrack: downloadBody.track,
    });

    expect(
      download.status(),
      `POST /api/movies/${String(movieId)}/subtitles/download must create the applied track`,
    ).toBe(201);
    const appliedTrack = downloadBody.track;
    expect(appliedTrack, 'the download response carries the published track').toBeTruthy();

    const appliedRadio = trackGroup(page, title).locator(`[data-track-id="${appliedTrack.id}"]`);
    await expect(appliedRadio, 'the applied subtitle is listed in the local tracks').toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(appliedRadio, 'the applied subtitle becomes the active track').toHaveAttribute(
      'aria-checked',
      'true',
      { timeout: context.waitBudgetMs },
    );

    await playerVideo(page).evaluate((element) => {
      (element as HTMLVideoElement).currentTime = 0;
    });
    await startPlayback(page);
    await expect
      .poll(
        async () => {
          const tracks = await readTracks(page);
          return (
            tracks.find((track) => track.trackId === appliedTrack.id)?.activeCues.join(' ') ?? ''
          );
        },
        { timeout: context.waitBudgetMs },
      )
      .toContain(fixture.cueText);

    const tracks = await readTracks(page);
    const active = tracks.find((track) => track.trackId === appliedTrack.id);
    record({
      step: 'active-subtitle',
      title,
      appliedTrack,
      activeTrack: active ?? null,
      observedCueText: active?.activeCues ?? [],
    });

    expect(active?.label, 'the active track is the applied subtitle').toBe(appliedTrack.label);
    expect(active?.activeCues.join(' '), 'the applied subtitle renders its cue text').toContain(
      fixture.cueText,
    );
  });
});
