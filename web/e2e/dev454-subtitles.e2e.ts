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

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
  subtitleFixtures: {
    listed: { title: string; relativePath: string; tracks: SubtitleTrackFixture[] };
    broken: { title: string; relativePath: string };
  };
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];
type SubtitleTrackDto = components['schemas']['SubtitleTrackDto'];

const context = loadContext();
const evidenceDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '.runtime/evidence');
const evidenceFile = path.join(evidenceDir, 'dev454-subtitles.json');
const records: Record<string, unknown>[] = [];

const OFF_COPY = 'Off';
const UNAVAILABLE_COPY = 'Subtitles unavailable';

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

function subtitlePanel(page: Page, title: string) {
  return playerRegion(page).getByRole('region', { name: `Subtitle settings for ${title}` });
}

function trackGroup(page: Page, title: string) {
  return playerRegion(page).getByRole('radiogroup', { name: `Subtitles for ${title}` });
}

function trackRadio(page: Page, title: string, name: string) {
  return trackGroup(page, title).getByRole('radio', { name });
}

async function openSubtitlesPanel(page: Page, title: string): Promise<void> {
  await ccTrigger(page).click();
  await expect(subtitlePanel(page, title)).toBeVisible({ timeout: context.waitBudgetMs });
}

async function radioLabels(page: Page, title: string): Promise<string[]> {
  return trackGroup(page, title)
    .getByRole('radio')
    .evaluateAll((elements) => elements.map((element) => element.getAttribute('aria-label') ?? ''));
}

async function currentTime(page: Page): Promise<number> {
  return playerVideo(page).evaluate((element) => (element as HTMLVideoElement).currentTime);
}

async function isPaused(page: Page): Promise<boolean> {
  return playerVideo(page).evaluate((element) => (element as HTMLVideoElement).paused);
}

async function startPlayback(page: Page): Promise<void> {
  await expect(playerVideo(page)).toBeVisible({ timeout: context.waitBudgetMs });
  if (await isPaused(page)) {
    const explicitPlay = playerRegion(page).getByRole('button', {
      name: /^play$/i,
    });
    if (await explicitPlay.isVisible()) {
      await explicitPlay.click();
    }
  }
  await expect.poll(async () => isPaused(page), { timeout: context.waitBudgetMs }).toBe(false);
}

async function assertPlaybackAdvances(page: Page): Promise<void> {
  const started = await currentTime(page);
  await expect
    .poll(async () => currentTime(page), { timeout: context.waitBudgetMs })
    .toBeGreaterThan(started);
}

async function rewindAndResume(page: Page): Promise<void> {
  await playerVideo(page).evaluate((element) => {
    (element as HTMLVideoElement).currentTime = 0;
  });
  await startPlayback(page);
}

async function assertPlaybackStillWorks(page: Page): Promise<boolean> {
  await rewindAndResume(page);
  await assertPlaybackAdvances(page);
  return !(await isPaused(page));
}

interface TrackSnapshot {
  trackId: string;
  label: string;
  language: string;
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
          language: track?.language ?? trackElement.srclang,
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

interface TrackStateProbe {
  tracks: TrackSnapshot[];
  selectorValue: string;
  textTrackCount: number;
}

async function probeTrackState(page: Page, title: string): Promise<TrackStateProbe> {
  const tracks = await readTracks(page);
  const textTrackCount = await playerVideo(page).evaluate(
    (element) => (element as HTMLVideoElement).textTracks.length,
  );
  const group = trackGroup(page, title);
  let selectorValue = '<no selector>';
  if ((await group.count()) > 0) {
    const checked = group.getByRole('radio', { checked: true });
    selectorValue =
      (await checked.count()) > 0
        ? ((await checked.first().getAttribute('aria-label')) ?? '<unlabeled>')
        : '<none checked>';
  }
  return { tracks, selectorValue, textTrackCount };
}

async function waitForTrackState(
  page: Page,
  title: string,
  predicate: (probe: TrackStateProbe) => boolean,
  timeoutMs = 30_000,
): Promise<{ ok: boolean; probe: TrackStateProbe }> {
  const deadline = Date.now() + timeoutMs;
  let probe = await probeTrackState(page, title);
  while (Date.now() < deadline) {
    if (predicate(probe)) return { ok: true, probe };
    await page.waitForTimeout(250);
    probe = await probeTrackState(page, title);
  }
  return { ok: predicate(probe), probe };
}

function describeTracks(probe: TrackStateProbe): string {
  return JSON.stringify({
    selectorValue: probe.selectorValue,
    textTrackCount: probe.textTrackCount,
    tracks: probe.tracks.map((track) => ({
      id: track.trackId,
      label: track.label,
      mode: track.mode,
      cues: track.activeCues,
    })),
  });
}

test.describe('DEV-454 sidecar subtitles in the real browser', () => {
  test('SMOKE-001 lists Off plus the served tracks as plain text in served order', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const fixture = context.subtitleFixtures.listed;
    const movieId = await openPlayer(page, request, fixture.title);
    await startPlayback(page);

    const listResponse = await request.get(
      `${context.baseURL}/api/movies/${String(movieId)}/subtitles`,
    );
    expect(listResponse.status()).toBe(200);
    const served = (await listResponse.json()) as SubtitleTrackDto[];

    await openSubtitlesPanel(page, fixture.title);
    const group = trackGroup(page, fixture.title);
    await expect(group).toBeVisible({ timeout: context.waitBudgetMs });
    const observed = await radioLabels(page, fixture.title);

    record({
      smoke: 'SMOKE-001',
      browser: `chromium ${browser.version()}`,
      movieId,
      title: fixture.title,
      servedTracks: served.map((track) => ({
        id: track.id,
        label: track.label,
        language: track.language,
        isDefault: track.isDefault,
      })),
      observedOptions: observed,
      selectorVisible: await group.isVisible(),
    });

    expect(observed[0], 'the first option is Off').toBe(OFF_COPY);
    expect(observed.slice(1), 'options are the served track labels in served order').toEqual(
      served.map((track) => track.label),
    );
    expect(served.length, 'the fixture movie exposes at least two tracks').toBeGreaterThan(1);
    expect(new Set(served.map((track) => track.id)).size, 'track ids are opaque and distinct').toBe(
      served.length,
    );
  });

  test('SMOKE-002 choosing a track shows its cues, Off clears them, and video keeps playing', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const fixture = context.subtitleFixtures.listed;
    const target = fixture.tracks[fixture.tracks.length - 1];
    await openPlayer(page, request, fixture.title);
    await startPlayback(page);

    await openSubtitlesPanel(page, fixture.title);
    const group = trackGroup(page, fixture.title);
    await expect(group).toBeVisible({ timeout: context.waitBudgetMs });
    const targetRadio = trackRadio(page, fixture.title, target.label);
    await expect(targetRadio).toBeVisible({ timeout: context.waitBudgetMs });

    await targetRadio.click();
    const shownState = await waitForTrackState(
      page,
      fixture.title,
      (probe) => probe.tracks.filter((track) => track.mode === 'hidden').length === 1,
      20_000,
    );
    expect(
      shownState.ok,
      `one track is loaded hidden: ${describeTracks(shownState.probe)}`,
    ).toBe(true);

    const cueState = await waitForTrackState(
      page,
      fixture.title,
      (probe) =>
        (
          probe.tracks.find((track) => track.mode === 'hidden')?.activeCues.join(' ') ?? ''
        ).includes(target.cueText ?? ''),
      20_000,
    );
    const shownCues = cueState.probe.tracks.find((track) => track.mode === 'hidden');
    const othersDisabled = cueState.probe.tracks
      .filter((track) => track.mode !== 'hidden')
      .every((track) => track.mode === 'disabled');
    const playingWithCues = !(await isPaused(page));

    await trackRadio(page, fixture.title, OFF_COPY).click();
    const offState = await waitForTrackState(
      page,
      fixture.title,
      (probe) => probe.tracks.every((track) => track.mode === 'disabled'),
      20_000,
    );
    const playbackContinuedAfterOff = await assertPlaybackStillWorks(page);

    record({
      smoke: 'SMOKE-002',
      browser: `chromium ${browser.version()}`,
      title: fixture.title,
      selectedLabel: shownCues?.label ?? null,
      selectedLanguage: shownCues?.language ?? null,
      selectedActiveCues: shownCues?.activeCues ?? [],
      othersDisabled,
      playingWithCues,
      cueObservation: cueState.ok ? 'active cue text observed' : describeTracks(cueState.probe),
      afterOff: describeTracks(offState.probe),
      playbackContinuedAfterOff,
    });

    expect(
      cueState.ok,
      `the hidden track loads the cue text: ${describeTracks(cueState.probe)}`,
    ).toBe(true);
    expect(shownCues?.label, 'the shown track is the one the viewer chose').toBe(target.label);
    expect(othersDisabled, 'every other listed track stays disabled').toBe(true);
    expect(playingWithCues, 'video keeps playing while cues are shown').toBe(true);
    expect(offState.ok, `Off clears every track: ${describeTracks(offState.probe)}`).toBe(true);
    expect(
      offState.probe.tracks.every((track) => track.activeCues.length === 0),
      'Off shows no active cues',
    ).toBe(true);
    expect(playbackContinuedAfterOff, 'video keeps playing after Off').toBe(true);
  });

  test('SMOKE-003 an unconvertible track returns to Off with Subtitles unavailable while video continues', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const broken = context.subtitleFixtures.broken;
    const brokenMovieId = await movieIdFor(request, broken.title);
    const contentResponse = await request.get(
      `${context.baseURL}/api/movies/${String(brokenMovieId)}/subtitles`,
    );
    expect(contentResponse.status()).toBe(200);
    const brokenTracks = (await contentResponse.json()) as SubtitleTrackDto[];
    expect(brokenTracks.length, 'the broken sidecar is listed so it can be chosen').toBe(1);

    await openPlayer(page, request, broken.title);
    await startPlayback(page);

    await openSubtitlesPanel(page, broken.title);
    const group = trackGroup(page, broken.title);
    await expect(group).toBeVisible({ timeout: context.waitBudgetMs });
    const brokenRadio = trackRadio(page, broken.title, brokenTracks[0].label);
    await expect(brokenRadio).toBeVisible({ timeout: context.waitBudgetMs });
    const contentStatuses: number[] = [];
    page.on('response', (response) => {
      if (response.url().includes('/subtitles/') && response.url().endsWith('/content')) {
        contentStatuses.push(response.status());
      }
    });

    await brokenRadio.click();

    const unavailable = playerRegion(page).getByText(UNAVAILABLE_COPY);
    await expect(unavailable).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(trackRadio(page, broken.title, OFF_COPY)).toHaveAttribute('aria-checked', 'true');
    const playingAfterFailure = !(await isPaused(page));
    const modesAfterFailure = await readTracks(page);
    const playbackContinued = await assertPlaybackStillWorks(page);

    await trackRadio(page, broken.title, OFF_COPY).click();
    await brokenRadio.click();
    await expect
      .poll(async () => contentStatuses.length, { timeout: context.waitBudgetMs })
      .toBeGreaterThan(1);
    await expect(unavailable).toBeVisible({ timeout: context.waitBudgetMs });

    record({
      smoke: 'SMOKE-003',
      browser: `chromium ${browser.version()}`,
      title: broken.title,
      contentStatuses,
      selectorValueAfterFailure: await trackRadio(page, broken.title, OFF_COPY).getAttribute(
        'aria-checked',
      ),
      unavailableVisible: await unavailable.isVisible(),
      playingAfterFailure,
      modesAfterFailure: modesAfterFailure.map((track) => ({
        label: track.label,
        mode: track.mode,
      })),
      retryReRequestedContent: contentStatuses.length > 1,
      playbackContinuedAfterFailure: playbackContinued,
    });

    expect(
      contentStatuses.length,
      'the browser fetched the content route for the chosen track',
    ).toBeGreaterThan(0);
    expect(
      contentStatuses.every((status) => status === 422),
      `an unconvertible sidecar answers the sanitized 422, saw ${JSON.stringify(contentStatuses)}`,
    ).toBe(true);
    expect(playingAfterFailure, 'video keeps playing after the subtitle failure').toBe(true);
    expect(
      modesAfterFailure.every((track) => track.mode === 'disabled'),
      'subtitles return to Off',
    ).toBe(true);
    expect(playbackContinued, 'video still plays after the subtitle failure').toBe(true);
  });

  test('SMOKE-004 a failing list keeps Off, shows Retry, and is never an empty success', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const fixture = context.subtitleFixtures.listed;
    const movieId = await movieIdFor(request, fixture.title);
    const listPattern = `**/api/movies/${String(movieId)}/subtitles`;
    let attempts = 0;
    await page.route(listPattern, async (route) => {
      attempts += 1;
      await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{}' });
    });

    await openPlayer(page, request, fixture.title);
    await startPlayback(page);

    await openSubtitlesPanel(page, fixture.title);

    const unavailable = playerRegion(page).getByText(UNAVAILABLE_COPY);
    await expect(unavailable).toBeVisible({ timeout: context.waitBudgetMs });
    const group = trackGroup(page, fixture.title);
    const groupCount = await group.count();
    const retry = playerRegion(page).getByRole('button', {
      name: `Retry loading subtitles for ${fixture.title}`,
    });
    await expect(retry).toBeVisible({ timeout: context.waitBudgetMs });
    const playingAfterFailure = !(await isPaused(page));
    const playbackContinued = await assertPlaybackStillWorks(page);
    const attemptsBeforeRetry = attempts;

    await page.unroute(listPattern);
    await retry.click();
    await expect(group).toBeVisible({ timeout: context.waitBudgetMs });
    await expect
      .poll(async () => group.getByRole('radio').count(), { timeout: context.waitBudgetMs })
      .toBeGreaterThan(1);

    record({
      smoke: 'SMOKE-004',
      browser: `chromium ${browser.version()}`,
      movieId,
      title: fixture.title,
      failedListAttempts: attemptsBeforeRetry,
      trackGroupCountWhileFailed: groupCount,
      unavailableVisible: await unavailable.isVisible(),
      retryVisible: true,
      playingAfterFailure,
      playbackContinuedAfterFailure: playbackContinued,
      recoveredOptions: await radioLabels(page, fixture.title),
    });

    expect(attemptsBeforeRetry, 'the failed list was actually requested').toBeGreaterThan(0);
    expect(groupCount, 'a failed list never renders as a successful empty selection').toBe(0);
    expect(playingAfterFailure, 'video keeps playing while the list fails').toBe(true);
    expect(playbackContinued, 'video still plays after the list failure').toBe(true);
  });
});
