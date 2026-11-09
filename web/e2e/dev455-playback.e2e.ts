import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];
type CapabilitiesResponse = components['schemas']['PlaybackCapabilitiesDto'];

const context = loadContext();
const evidenceDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '.runtime/evidence');
const evidenceFile = path.join(evidenceDir, 'dev455-playback.json');
const records: Record<string, unknown>[] = [];

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

function detailsActions(page: Page) {
  return page.locator('.details-actions');
}

async function currentTime(page: Page): Promise<number> {
  return playerVideo(page).evaluate((element) => (element as HTMLVideoElement).currentTime);
}

async function isPaused(page: Page): Promise<boolean> {
  return playerVideo(page).evaluate((element) => (element as HTMLVideoElement).paused);
}

async function startPlayback(page: Page, title: string): Promise<void> {
  const video = playerVideo(page);
  await expect(video).toBeVisible({ timeout: context.waitBudgetMs });
  if (await isPaused(page)) {
    const explicitPlay = playerRegion(page).getByRole('button', {
      name: `Play ${title} in browser`,
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

test.describe('DEV-455 browser playback through web nginx', () => {
  test('SMOKE-001 (DEV-455) details Play opens the in-page player, seeks, and fullscreens', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const title = context.seedTitles[0];
    const movieId = await movieIdFor(request, title);

    const streamResponses: { status: number; range: string | null }[] = [];
    const launchPosts: string[] = [];
    page.on('response', (response) => {
      if (response.url().includes(`/api/movies/${String(movieId)}/stream`)) {
        streamResponses.push({
          status: response.status(),
          range: response.request().headers()['range'] ?? null,
        });
      }
    });
    page.on('request', (request_) => {
      if (request_.method() === 'POST' && /\/api\/movies\/\d+\/play$/.test(request_.url())) {
        launchPosts.push(request_.url());
      }
    });

    const capabilities = await request.get(`${context.baseURL}/api/playback/capabilities`);
    expect(capabilities.status()).toBe(200);
    const capability = (await capabilities.json()) as CapabilitiesResponse;
    expect(capability.localPlayEnabled, 'Compose keeps Features.LocalPlay=false').toBe(false);

    await page.goto(`${context.baseURL}/movies/${String(movieId)}`);
    await detailsActions(page).getByRole('button', { name: 'Play', exact: true }).click();

    const region = playerRegion(page);
    await expect(region).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(region).toBeFocused();

    await startPlayback(page, title);
    await assertPlaybackAdvances(page);

    await expect
      .poll(async () => currentTime(page), { timeout: context.waitBudgetMs })
      .toBeGreaterThan(3);
    const beforeSeek = await currentTime(page);
    const responsesBeforeSeek = streamResponses.length;
    await region.focus();
    await page.keyboard.press('ArrowLeft');
    await expect
      .poll(async () => currentTime(page), { timeout: context.waitBudgetMs })
      .toBeLessThan(beforeSeek);
    const afterSeek = await currentTime(page);
    await expect
      .poll(async () => currentTime(page), { timeout: context.waitBudgetMs })
      .toBeGreaterThan(afterSeek);
    const seekRequests = streamResponses.slice(responsesBeforeSeek);

    const fullscreenButton = region.getByRole('button', { name: /enter fullscreen mode/i });
    await expect(fullscreenButton).toBeVisible();
    await fullscreenButton.click();
    await expect
      .poll(async () => page.evaluate(() => document.fullscreenElement !== null), {
        timeout: context.waitBudgetMs,
      })
      .toBe(true);
    const enteredFullscreen = await page.evaluate(() => document.fullscreenElement !== null);
    await page.keyboard.press('Escape');
    await expect
      .poll(async () => page.evaluate(() => document.fullscreenElement === null), {
        timeout: context.waitBudgetMs,
      })
      .toBe(true);
    const leftFullscreen = await page.evaluate(() => document.fullscreenElement === null);

    const localControls = region.getByRole('button', { name: /in BS Player/ });
    const localControlCount = await localControls.count();

    record({
      smoke: 'SMOKE-001',
      browser: `chromium ${browser.version()}`,
      movieId,
      title,
      localPlayEnabled: capability.localPlayEnabled,
      beforeSeek,
      afterSeek,
      seekRangeRequests: seekRequests.filter((entry) => entry.range !== null),
      streamStatuses: [...new Set(streamResponses.map((entry) => entry.status))],
      rangedStreamRequests: streamResponses.filter((entry) => entry.range !== null).length,
      enteredFullscreen,
      leftFullscreen,
      localControlCount,
      launchPosts,
    });

    expect(streamResponses.length, 'the browser fetched the stream through nginx').toBeGreaterThan(
      0,
    );
    expect(
      streamResponses.every((entry) => entry.status === 200 || entry.status === 206),
      `every stream request must succeed, saw ${JSON.stringify(streamResponses)}`,
    ).toBe(true);
    expect(
      streamResponses.some((entry) => entry.status === 206 && entry.range !== null),
      'a seek produced a ranged 206 through nginx',
    ).toBe(true);
    expect(enteredFullscreen, 'desktop fullscreen is accepted by the real browser').toBe(true);
    expect(leftFullscreen, 'fullscreen exits in the real browser').toBe(true);
    expect(localControlCount, 'LocalPlay=false hides the local playback action').toBe(0);
    expect(launchPosts, 'browser Play never posts a local launch').toEqual([]);
  });

  test('SMOKE-002 (DEV-455) card Play opens the same player and playback proceeds', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const title = context.seedTitles[0];
    const movieId = await movieIdFor(request, title);

    await page.goto(context.baseURL);
    const grid = page.getByRole('list', { name: 'Movies' });
    await expect(grid.getByRole('link', { name: title, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await grid.getByRole('button', { name: `Play ${title}` }).click();

    await expect(page).toHaveURL(new URL(`/movies/${String(movieId)}`, context.baseURL).href, {
      timeout: context.waitBudgetMs,
    });
    const region = playerRegion(page);
    await expect(region).toBeVisible({ timeout: context.waitBudgetMs });

    const explicitPlay = region.getByRole('button', { name: `Play ${title} in browser` });
    const blockedByAutoplay = await isPaused(page);
    await startPlayback(page, title);
    const started = await currentTime(page);
    await assertPlaybackAdvances(page);

    record({
      smoke: 'SMOKE-002',
      browser: `chromium ${browser.version()}`,
      movieId,
      title,
      autoplayDenied: blockedByAutoplay,
      explicitPlayOffered: await explicitPlay.isVisible(),
      startedAt: started,
    });

    expect(await isPaused(page), 'card Play ends in playing media').toBe(false);
  });

  test('SMOKE-003 (DEV-455) LocalPlay off hides Open in BS Player and browser Play stays available', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const title = context.seedTitles[0];
    const movieId = await movieIdFor(request, title);

    const launchPosts: string[] = [];
    page.on('request', (request_) => {
      if (request_.method() === 'POST' && /\/api\/movies\/\d+\/play$/.test(request_.url())) {
        launchPosts.push(request_.url());
      }
    });

    const capabilities = page.waitForResponse(
      (response) => new URL(response.url()).pathname === '/api/playback/capabilities',
      { timeout: context.waitBudgetMs },
    );
    await page.goto(`${context.baseURL}/movies/${String(movieId)}`);
    await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    const capabilityResponse = await capabilities;
    expect(capabilityResponse.status()).toBe(200);
    const capability = (await capabilityResponse.json()) as CapabilitiesResponse;

    const browserPlay = detailsActions(page).getByRole('button', { name: 'Play', exact: true });
    const localControl = page.getByRole('button', { name: /in BS Player/ });
    await expect(browserPlay).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(localControl).toHaveCount(0);
    await browserPlay.click();
    await expect(playerRegion(page)).toBeVisible({ timeout: context.waitBudgetMs });

    record({
      smoke: 'SMOKE-003',
      browser: `chromium ${browser.version()}`,
      movieId,
      title,
      localPlayEnabled: capability.localPlayEnabled,
      browserPlayVisible: await browserPlay.isVisible(),
      localControlCount: await localControl.count(),
      launchPosts,
    });

    expect(capability.localPlayEnabled, 'Compose keeps Features.LocalPlay=false').toBe(false);
    expect(launchPosts, 'no local launch is attempted from browser Play').toEqual([]);
  });

  test('SMOKE-001 (DEV-455) terminal stream failure offers Retry with no local fallback', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const title = context.seedTitles[0];
    const movieId = await movieIdFor(request, title);

    const launchPosts: string[] = [];
    page.on('request', (request_) => {
      if (request_.method() === 'POST' && /\/api\/movies\/\d+\/play$/.test(request_.url())) {
        launchPosts.push(request_.url());
      }
    });

    await page.route(`**/api/movies/${String(movieId)}/stream`, (route) =>
      route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{}' }),
    );

    await page.goto(`${context.baseURL}/movies/${String(movieId)}`);
    await detailsActions(page).getByRole('button', { name: 'Play', exact: true }).click();

    const region = playerRegion(page);
    const error = region.getByRole('alert');
    await expect(error).toContainText("Couldn't play this movie", {
      timeout: context.waitBudgetMs,
    });
    const retry = region.getByRole('button', {
      name: `Retry playing ${title} from the beginning`,
    });
    await expect(retry).toBeVisible();
    const localRecovery = region.getByRole('button', { name: /in BS Player/ });
    const localRecoveryCount = await localRecovery.count();
    const errorText = (await error.textContent()) ?? '';

    await page.unroute(`**/api/movies/${String(movieId)}/stream`);
    await retry.click();
    await startPlayback(page, title);
    const recovered = await currentTime(page);
    await assertPlaybackAdvances(page);

    record({
      smoke: 'SMOKE-001',
      browser: `chromium ${browser.version()}`,
      movieId,
      title,
      terminalErrorText: errorText,
      localRecoveryCount,
      retryRecoveredAt: recovered,
      launchPosts,
    });

    expect(localRecoveryCount, 'LocalPlay=false hides local recovery').toBe(0);
    expect(launchPosts, 'a browser failure never auto-launches the local player').toEqual([]);
  });

  test('SMOKE-001 (DEV-455) mobile width keeps the desktop chrome with no mobile fallback', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const title = context.seedTitles[0];
    const movieId = await movieIdFor(request, title);

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${context.baseURL}/movies/${String(movieId)}`);
    await detailsActions(page).getByRole('button', { name: 'Play', exact: true }).click();

    const region = playerRegion(page);
    await expect(region).toBeVisible({ timeout: context.waitBudgetMs });
    const fullscreenButtons = region.getByRole('button', { name: /fullscreen mode/i });
    await expect(fullscreenButtons).toHaveCount(1);
    const box = await region.locator('.browser-player-frame').boundingBox();

    await startPlayback(page, title);
    const started = await currentTime(page);
    await assertPlaybackAdvances(page);

    record({
      smoke: 'SMOKE-001',
      browser: `chromium ${browser.version()}`,
      viewport: '390x844',
      movieId,
      title,
      fullscreenControlCount: await fullscreenButtons.count(),
      playerBoxWidth: box?.width ?? null,
      startedAt: started,
    });

    expect(box, 'the player renders at mobile width').not.toBeNull();
    expect(box!.width, 'the player stays inside the mobile viewport').toBeLessThanOrEqual(390);
  });
});
