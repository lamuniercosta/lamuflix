import { execFile } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { promisify } from 'node:util';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import type { components } from '../src/api/types';

const execFileAsync = promisify(execFile);

interface Dev471Fixture {
  folder: string;
  source: string;
  mediaSha256: string;
  title: string;
  year: number;
  imdbId: string;
  genre: string;
  runtimeMinutes: number;
  rating: string;
  director: string;
  plot: string;
}

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
  project: string;
  composeFiles: string[];
  envFile: string;
  stubService: string;
  dev471Fixture: Dev471Fixture;
}

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type CapabilitiesResponse = components['schemas']['PlaybackCapabilitiesDto'];
type MovieSummary = components['schemas']['MovieSummaryResponse'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error(
      'E2E_CONTEXT_FILE is required; run the suite through the required smoke entrypoint',
    );
  }
  const parsed = JSON.parse(readFileSync(file, 'utf8')) as Partial<E2EContext>;
  if (parsed.dev471Fixture === undefined || parsed.baseURL === undefined) {
    throw new Error(
      'the DEV-471 smoke context is missing; run it through the required smoke entrypoint',
    );
  }
  return parsed as E2EContext;
}

const context = loadContext();
const fixture = context.dev471Fixture;
const totalBudgetMs = context.waitBudgetMs * 4;

async function stubControl(action: string, args: string[] = []): Promise<void> {
  const [baseCompose, overlayCompose] = context.composeFiles;
  const command = [
    'compose',
    '-p',
    context.project,
    '-f',
    baseCompose,
    '-f',
    overlayCompose,
    '--env-file',
    context.envFile,
    'exec',
    '-T',
    context.stubService,
    'node',
    '/e2e/stub.mjs',
    'control',
    action,
    ...args,
  ];

  try {
    await execFileAsync(process.env.DOCKER ?? 'docker', command, {
      timeout: context.waitBudgetMs,
      maxBuffer: 20 * 1024 * 1024,
      windowsHide: true,
    });
  } catch (error) {
    const stderr = (error as { stderr?: string }).stderr ?? (error as Error).message;
    throw new Error(`stub control ${action} failed: ${stderr}`, { cause: error });
  }
}

async function browseMovies(request: APIRequestContext): Promise<BrowseMoviesResponse> {
  const response = await request.get(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
  );
  expect(response.status(), 'the library listing is readable through nginx').toBe(200);
  return (await response.json()) as BrowseMoviesResponse;
}

function fixtureRow(page: Page) {
  return page
    .getByRole('list', { name: 'Movies' })
    .getByRole('listitem')
    .filter({ has: page.getByRole('link', { name: fixture.title, exact: true }) });
}

function playerRegion(page: Page) {
  return page.getByRole('region', { name: /^Movie player: / });
}

function playerVideo(page: Page) {
  return playerRegion(page).locator('video');
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
    const explicitPlay = playerRegion(page).getByRole('button', { name: /^play$/i });
    if (await explicitPlay.isVisible()) {
      await explicitPlay.click();
    }
  }
  await expect.poll(async () => isPaused(page), { timeout: context.waitBudgetMs }).toBe(false);
}

/**
 * SignalR sends each hub message as a JSON frame type 1, optionally length-suffixed:
 * `{"type":1,"target":"EnrichmentStatusChanged","arguments":[...]}` + 0x1e.
 */
function signalRFrames(payload: string): { target?: unknown; arguments?: unknown[] }[] {
  const frames: { target?: unknown; arguments?: unknown[] }[] = [];
  for (const chunk of payload.split('\u001e')) {
    const text = chunk.trim();
    if (text.length === 0) {
      continue;
    }
    const body = text.startsWith('{') ? text : text.slice(text.indexOf('{'));
    if (!body.startsWith('{')) {
      continue;
    }
    try {
      const parsed = JSON.parse(body) as { type?: unknown; target?: unknown; arguments?: unknown };
      if (parsed.type === 1) {
        frames.push({
          target: parsed.target,
          arguments: Array.isArray(parsed.arguments) ? parsed.arguments : [],
        });
      }
    } catch {
      /* not a hub message frame */
    }
  }
  return frames;
}

test.describe('DEV-471 compose smoke', () => {
  test('SMOKE-471 stubbed full flow: import, listing, stub enrichment, thumbnail, hub update, browser play and process-play 403', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(totalBudgetMs);
    await page.addInitScript((fixtureTitle) => {
      const records: {
        path: string;
        method: string;
        startedAt: number;
        doneAt: number;
        fixtureEnriched: boolean;
      }[] = [];
      const original = window.fetch;
      window.fetch = function patchedFetch(input: RequestInfo | URL, init?: RequestInit) {
        const url =
          typeof input === 'string' ? input : input instanceof URL ? input.href : input.url;
        const startedAt = performance.now();
        const record = {
          path: '',
          method: 'GET',
          startedAt,
          doneAt: Number.POSITIVE_INFINITY,
          fixtureEnriched: false,
        };
        try {
          record.path = new URL(url, window.location.origin).pathname;
          record.method = (
            init?.method ?? (input instanceof Request ? input.method : 'GET')
          ).toUpperCase();
          records.push(record);
        } catch {
          /* unparsable request target */
        }
        const settled = original.call(window, input as RequestInfo, init);
        settled.then(
          (response) => {
            record.doneAt = performance.now();
            if (record.path !== '/api/movies' || record.method !== 'GET') {
              return undefined;
            }
            return response
              .clone()
              .json()
              .then((body: { items?: { title?: unknown; enrichmentStatus?: unknown }[] }) => {
                record.fixtureEnriched = (body.items ?? []).some(
                  (item) =>
                    String(item.title) === fixtureTitle && item.enrichmentStatus === 'Enriched',
                );
              })
              .catch(() => undefined);
          },
          () => {
            record.doneAt = performance.now();
          },
        );
        return settled;
      };
      (window as unknown as { __dev471Fetches: typeof records }).__dev471Fetches = records;

      // Record every server-originated hub message with the same clock as the fetch log,
      // so the UI update can be attributed to the hub rather than to a listing response.
      const hubFrames: { id: unknown; enrichmentStatus: unknown; at: number }[] = [];
      (window as unknown as { __dev471HubFrames: typeof hubFrames }).__dev471HubFrames = hubFrames;
      const NativeWebSocket = window.WebSocket;
      window.WebSocket = class Dev471WebSocket extends NativeWebSocket {
        constructor(url: string | URL, protocols?: string | string[]) {
          super(url, protocols);
          if (!new URL(String(url), window.location.origin).pathname.startsWith('/hubs/')) {
            return;
          }
          this.addEventListener('message', (event) => {
            if (typeof event.data !== 'string') {
              return;
            }
            for (const chunk of event.data.split('\u001e')) {
              const text = chunk.trim();
              const body = text.startsWith('{') ? text : text.slice(text.indexOf('{'));
              if (!body.startsWith('{')) {
                continue;
              }
              try {
                const frame = JSON.parse(body) as {
                  type?: unknown;
                  target?: unknown;
                  arguments?: unknown[];
                };
                if (frame.type !== 1 || frame.target !== 'EnrichmentStatusChanged') {
                  continue;
                }
                const payload = (frame.arguments?.[0] ?? {}) as {
                  id?: unknown;
                  enrichmentStatus?: unknown;
                };
                hubFrames.push({
                  id: payload.id,
                  enrichmentStatus: payload.enrichmentStatus,
                  at: performance.now(),
                });
              } catch {
                /* not a hub message frame */
              }
            }
          });
        }
      } as unknown as typeof WebSocket;
    }, fixture.title);

    const hubSockets: string[] = [];
    const hubRequests: { path: string; status: number }[] = [];
    const hubEvents: { id: unknown; enrichmentStatus: unknown }[] = [];
    page.on('websocket', (socket) => {
      if (!new URL(socket.url()).pathname.startsWith('/hubs/')) {
        return;
      }
      hubSockets.push(socket.url());
      socket.on('framereceived', (frame) => {
        for (const record of signalRFrames(String(frame.payload))) {
          if (record.target !== 'EnrichmentStatusChanged') {
            continue;
          }
          const payload = Array.isArray(record.arguments) ? record.arguments[0] : undefined;
          hubEvents.push({
            id: (payload as { id?: unknown } | undefined)?.id,
            enrichmentStatus: (payload as { enrichmentStatus?: unknown } | undefined)
              ?.enrichmentStatus,
          });
        }
      });
    });
    page.on('response', (response) => {
      const path = new URL(response.url()).pathname;
      if (path.startsWith('/hubs/')) {
        hubRequests.push({ path, status: response.status() });
      }
    });

    const capabilities = await request.get(`${context.baseURL}/api/playback/capabilities`);
    expect(capabilities.status()).toBe(200);
    const capability = (await capabilities.json()) as CapabilitiesResponse;
    expect(capability.localPlayEnabled, 'Features.LocalPlay stays false').toBe(false);

    // Acceptance scenario 1: the hub transport is established through shipped nginx first.
    await page.goto(context.baseURL);
    await expect
      .poll(() => hubRequests.length, { timeout: context.waitBudgetMs })
      .toBeGreaterThan(0);
    expect(hubRequests.filter((entry) => entry.path === '/hubs/enrichment/negotiate')).not.toEqual(
      [],
    );
    expect(hubRequests.every((entry) => entry.status === 200)).toBe(true);
    await expect
      .poll(() => hubSockets.length, { timeout: context.waitBudgetMs })
      .toBeGreaterThan(0);
    const hubHost = new URL(context.baseURL).host;
    expect(new URL(hubSockets[0]).host, 'the hub connects through shipped nginx').toBe(hubHost);
    const navigationsAtStart = await page.evaluate(
      () => performance.getEntriesByType('navigation').length,
    );

    // Acceptance scenario 2: a known fixture imported through the real stack while the
    // provider response is held, so the pending state is observed before enrichment.
    await stubControl('arm', [fixture.title]);
    const importResponse = page.waitForResponse(
      (response) =>
        response.request().method() === 'POST' &&
        new URL(response.url()).pathname === '/api/movies/import/library',
      { timeout: context.waitBudgetMs },
    );
    await page.getByRole('button', { name: 'Bulk import' }).click();
    const dialog = page.getByRole('dialog', { name: 'Bulk import' });
    await expect(dialog).toBeVisible({ timeout: context.waitBudgetMs });
    await dialog.getByRole('checkbox', { name: fixture.folder }).check();
    await dialog.getByRole('button', { name: 'Import', exact: true }).click();
    const imported = await importResponse;
    expect(imported.status(), 'the fixture folder imports through the real stack').toBe(200);
    expect(Number((await imported.json()).imported)).toBe(1);
    await dialog.getByRole('button', { name: 'Cancel' }).click();

    const row = fixtureRow(page);
    const card = row.locator('article.movie-card');
    await expect(row).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(card).toHaveAttribute('data-status', 'Pending', { timeout: context.waitBudgetMs });

    await page.evaluate(() => {
      const state = window as unknown as {
        __dev471Fetches: unknown[];
        __dev471HubFrames: unknown[];
      };
      state.__dev471Fetches.length = 0;
      state.__dev471HubFrames.length = 0;
    });
    await stubControl('release', [fixture.title]);
    await expect(card).toHaveAttribute('data-status', 'Enriched', {
      timeout: context.waitBudgetMs,
    });
    const transition = await page.evaluate(() => {
      const state = window as unknown as {
        __dev471Fetches: {
          path: string;
          method: string;
          doneAt: number;
          fixtureEnriched: boolean;
        }[];
        __dev471HubFrames: { id: unknown; enrichmentStatus: unknown; at: number }[];
      };
      return {
        hubFrames: state.__dev471HubFrames,
        listingsAnsweringEnriched: state.__dev471Fetches.filter(
          (entry) =>
            entry.path === '/api/movies' && entry.method === 'GET' && entry.fixtureEnriched,
        ).length,
      };
    });
    expect(
      transition.hubFrames.some((frame) => frame.enrichmentStatus === 'Enriched'),
      'a server-originated EnrichmentStatusChanged frame arrived for the fixture',
    ).toBe(true);
    expect(
      transition.listingsAnsweringEnriched,
      'no library listing response ever reported the fixture as already Enriched, so the flip was the hub event',
    ).toBe(0);
    expect(
      await page.evaluate(() => performance.getEntriesByType('navigation').length),
      'the hub update reached the UI without a reload',
    ).toBe(navigationsAtStart);

    // Persisted listing carrying the deterministic stub-defined fields.
    const listing = await browseMovies(request);
    const listed = listing.items.filter((item: MovieSummary) => item.title === fixture.title);
    expect(listed, 'exactly one fixture-correlated record is listed').toHaveLength(1);
    const movie = listed[0];
    expect(movie.enrichmentStatus).toBe('Enriched');
    expect(String(movie.releaseYear)).toBe(String(fixture.year));
    expect(Number(movie.imdbRating)).toBeCloseTo(Number(fixture.rating), 3);
    expect(String(movie.runtime)).toBe(String(fixture.runtimeMinutes));
    const movieId = String(movie.id);

    const fixtureEvents = hubEvents.filter((event) => String(event.id) === movieId);
    expect(
      fixtureEvents.map((event) => event.enrichmentStatus),
      'a server-originated hub event carried the fixture to Enriched',
    ).toContain('Enriched');
    const inPageFixtureFrames = await page.evaluate((id) => {
      const state = window as unknown as {
        __dev471HubFrames: { id: unknown; enrichmentStatus: unknown; at: number }[];
      };
      return state.__dev471HubFrames.filter((frame) => String(frame.id) === id);
    }, movieId);
    expect(
      inPageFixtureFrames.length,
      'the in-page hub listener also received the fixture frame',
    ).toBeGreaterThan(0);

    await expect(row.locator('.movie-card-rating')).toContainText(fixture.rating);
    await expect(row).toContainText(String(fixture.year));

    // Nonempty thumbnail that decodes as an image in the real browser.
    const thumbnailUrl = `${context.baseURL}/api/movies/${movieId}/thumbnail`;
    await expect
      .poll(async () => (await request.get(thumbnailUrl)).status(), {
        timeout: context.waitBudgetMs,
      })
      .toBe(200);
    const thumbnail = await request.get(thumbnailUrl);
    expect(thumbnail.headers()['content-type']).toContain('image/jpeg');
    expect((await thumbnail.body()).length, 'the thumbnail is nonempty').toBeGreaterThan(0);
    const decoded = await page.evaluate(async (url) => {
      const response = await fetch(url);
      const bitmap = await createImageBitmap(await response.blob());
      const size = { width: bitmap.width, height: bitmap.height };
      bitmap.close();
      return size;
    }, thumbnailUrl);
    expect(decoded.width, 'the thumbnail decodes as an image').toBeGreaterThan(0);
    expect(decoded.height, 'the thumbnail decodes as an image').toBeGreaterThan(0);

    // Acceptance scenario 4: in-page browser playback through shipped nginx.
    const streamStatuses: number[] = [];
    page.on('response', (response) => {
      if (response.url().includes(`/api/movies/${movieId}/stream`)) {
        streamStatuses.push(response.status());
      }
    });
    await page
      .getByRole('list', { name: 'Movies' })
      .getByRole('button', { name: `Play ${fixture.title}` })
      .click();
    await expect(page).toHaveURL(new URL(`/movies/${movieId}`, context.baseURL).href, {
      timeout: context.waitBudgetMs,
    });
    await expect(playerRegion(page)).toBeVisible({ timeout: context.waitBudgetMs });
    await expect
      .poll(
        async () =>
          playerVideo(page).evaluate((element) => {
            const video = element as HTMLVideoElement;
            return video.readyState >= 1 && video.videoWidth > 0;
          }),
        { timeout: context.waitBudgetMs },
      )
      .toBe(true);
    const metadata = await playerVideo(page).evaluate((element) => {
      const video = element as HTMLVideoElement;
      return { width: video.videoWidth, height: video.videoHeight, duration: video.duration };
    });
    expect(metadata.width, 'the player reports a real video width').toBeGreaterThan(0);
    expect(metadata.height, 'the player reports a real video height').toBeGreaterThan(0);
    expect(Number.isFinite(metadata.duration), 'the player reports a finite duration').toBe(true);
    expect(metadata.duration).toBeGreaterThan(0);

    await startPlayback(page);
    const startedAt = await currentTime(page);
    await expect
      .poll(async () => currentTime(page), { timeout: context.waitBudgetMs })
      .toBeGreaterThan(startedAt);
    expect(streamStatuses.length, 'the browser fetched the media through nginx').toBeGreaterThan(0);
    expect(
      streamStatuses.every((status) => status === 200 || status === 206),
      `every stream response must succeed, saw ${JSON.stringify(streamStatuses)}`,
    ).toBe(true);

    const processPlay = await request.post(`${context.baseURL}/api/movies/${movieId}/play`);
    expect(processPlay.status(), 'nginx blocks the local process launch').toBe(403);
    expect(
      (processPlay.headers()['content-type'] ?? '').includes('application/problem+json'),
      'the 403 is produced by nginx before the application route answers',
    ).toBe(false);

    test.info().annotations.push({
      type: 'dev471-smoke',
      description: JSON.stringify({
        browser: `chromium ${browser.version()}`,
        hubSocket: hubSockets[0],
        hubRequests,
        movieId,
        title: fixture.title,
        mediaSha256: fixture.mediaSha256,
        sourceMedia: fixture.source,
        thumbnail: decoded,
        playback: { metadata, startedAt },
        processPlay: { status: processPlay.status() },
      }),
    });
  });
});
