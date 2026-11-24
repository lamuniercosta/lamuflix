import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test, type APIRequestContext } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
  subtitleFixtures: { listed: { title: string } };
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];
type ProblemDetails = components['schemas']['ProblemDetails'];

const context = loadContext();
const evidenceDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '.runtime/evidence');
const evidenceFile = path.join(evidenceDir, 'dev567-api-failure-toast.json');
const records: Record<string, unknown>[] = [];

const VIEWPORT = { width: 375, height: 800 };
const FAILURE_TITLE = 'Subtitle provider unavailable';
const FAILURE_REASON =
  'provider_timeout: DEV-567 deterministic failure; the upstream subtitle index did not answer before the request deadline';
const FAILURE_TRACE_ID = `dev567-trace-${'9f3c1a7b'.repeat(10)}`;
const INLINE_FAILURE_COPY = 'The subtitle search failed.';
const LEAK_SENTINEL = 'raw detail must not appear';

const FAILURE_BODY: ProblemDetails = {
  type: 'about:blank',
  title: FAILURE_TITLE,
  status: 503,
  detail: LEAK_SENTINEL,
  reason: FAILURE_REASON,
  traceId: FAILURE_TRACE_ID,
};

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
        viewport: VIEWPORT,
        problem: FAILURE_BODY,
        records,
      },
      null,
      2,
    )}\n`,
  );
});

function deferred(): { promise: Promise<void>; resolve: () => void } {
  let resolve!: () => void;
  const promise = new Promise<void>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}

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

test.describe('DEV-567 API failure toasts', () => {
  test('SMOKE-001/SMOKE-002 one narrow-viewport subtitle failure toast with inline error, viewport fit, and keyboard Dismiss', async ({
    page,
    request,
    browser,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const title = context.subtitleFixtures.listed.title;
    const movieId = await movieIdFor(request, title);
    const searchRoute = new RegExp(`/api/movies/${movieId}/subtitles/search(?:\\?|$)`);
    const failureGate = deferred();
    let intercepted = 0;

    await page.setViewportSize(VIEWPORT);
    await page.route(searchRoute, async (route) => {
      intercepted += 1;
      await failureGate.promise;
      await route.fulfill({
        status: 503,
        contentType: 'application/problem+json',
        body: JSON.stringify(FAILURE_BODY),
      });
    });

    await page.goto(`${context.baseURL}/movies/${String(movieId)}`);
    await page
      .locator('.details-actions')
      .getByRole('button', { name: `Play ${title} in browser` })
      .click();
    const player = page.getByRole('region', { name: /^Movie player: / });
    await expect(player).toBeVisible({ timeout: context.waitBudgetMs });
    await player.getByRole('button', { name: 'Subtitles' }).click();

    const panel = player.getByRole('region', { name: `Subtitle settings for ${title}` });
    await expect(panel).toBeVisible({ timeout: context.waitBudgetMs });
    const subtitleTab = player.getByRole('region', { name: 'Subtitle search' });
    const focusTarget = panel.getByRole('tab', { name: 'Subtitles' });
    await focusTarget.focus();
    await expect(focusTarget).toBeFocused();

    const failedResponsePromise = page.waitForResponse(
      (response) => searchRoute.test(response.url()) && response.status() === 503,
      { timeout: context.waitBudgetMs },
    );
    failureGate.resolve();
    const failedResponse = await failedResponsePromise;

    const inlineStatus = subtitleTab.getByRole('status');
    await expect(inlineStatus).toContainText(INLINE_FAILURE_COPY, {
      timeout: context.waitBudgetMs,
    });

    const host = page.getByRole('region', { name: 'API failure notifications' });
    const toasts = host.locator('article.lamu-toast');
    await expect(toasts).toHaveCount(1, { timeout: context.waitBudgetMs });
    const toast = toasts.first();
    await expect(toast.locator('.lamu-toast-title')).toHaveText(FAILURE_TITLE);
    await expect(toast.locator('.lamu-toast-status')).toHaveText('503');
    await expect(toast.locator('.lamu-toast-reason')).toHaveText(FAILURE_REASON);
    await expect(toast.locator('.lamu-toast-trace')).toHaveText(FAILURE_TRACE_ID);
    expect(await toast.textContent(), 'the toast never renders raw detail text').not.toContain(
      LEAK_SENTINEL,
    );
    await expect(focusTarget, 'the toast never moves focus from the subtitles tab').toBeFocused();

    await toast.hover();
    const box = await toast.boundingBox();
    expect(box, 'the toast renders with measurable browser geometry').not.toBeNull();
    expect(box!.x).toBeGreaterThanOrEqual(0);
    expect(box!.y).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width, 'the toast stays inside the narrow viewport').toBeLessThanOrEqual(
      VIEWPORT.width,
    );
    expect(box!.y + box!.height, 'the toast stays inside the narrow viewport').toBeLessThanOrEqual(
      VIEWPORT.height,
    );
    const widths = await page.evaluate(() => {
      const copy = document.querySelector('.lamu-toast-copy');
      return {
        document: document.documentElement.scrollWidth,
        viewport: window.innerWidth,
        copyScroll: copy?.scrollWidth ?? 0,
        copyClient: copy?.clientWidth ?? 0,
      };
    });
    expect(widths.document, 'the narrow page has no horizontal overflow').toBeLessThanOrEqual(
      widths.viewport,
    );
    expect(widths.copyScroll, 'the long diagnostic wraps inside the toast').toBeLessThanOrEqual(
      widths.copyClient,
    );

    record({
      smoke: 'SMOKE-001',
      browser: `chromium ${browser.version()}`,
      viewport: VIEWPORT,
      movieId,
      title,
      interceptedSearchRequests: intercepted,
      responseStatus: failedResponse.status(),
      toastCount: await toasts.count(),
      toastTitle: await toast.locator('.lamu-toast-title').textContent(),
      status: await toast.locator('.lamu-toast-status').textContent(),
      reason: await toast.locator('.lamu-toast-reason').textContent(),
      traceId: await toast.locator('.lamu-toast-trace').textContent(),
      inlineStatus: await inlineStatus.textContent(),
      toastBox: box,
      widths,
      focusKeptOnSubtitlesTab: await focusTarget.evaluate(
        (element) => element === document.activeElement,
      ),
    });

    const dismiss = toast.getByRole('button', { name: 'Dismiss' });
    await dismiss.focus();
    await page.keyboard.press('Enter');
    await expect(toasts, 'keyboard Dismiss removes the toast').toHaveCount(0);
    await expect
      .poll(
        () =>
          page.evaluate(() => ({
            hostFocus: Boolean(
              document.querySelector('.lamu-toast-host')?.contains(document.activeElement),
            ),
            bodyFocus: document.activeElement === document.body,
          })),
        { timeout: context.waitBudgetMs },
      )
      .toEqual({ hostFocus: false, bodyFocus: true });

    record({
      smoke: 'SMOKE-002',
      browser: `chromium ${browser.version()}`,
      toastCountAfterDismiss: await toasts.count(),
      hostFocusAfterDismiss: await page.evaluate(() =>
        Boolean(document.querySelector('.lamu-toast-host')?.contains(document.activeElement)),
      ),
      dismissal: 'keyboard Enter on Dismiss',
    });

    expect(
      intercepted,
      'the deterministic subtitle search failure was intercepted',
    ).toBeGreaterThan(0);
  });
});
