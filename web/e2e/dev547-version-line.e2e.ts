import { readFileSync } from 'node:fs';
import { expect, test, type Page, type Route } from '@playwright/test';
import type { components } from '../src/api/types';

type VersionDto = components['schemas']['VersionDto'];

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
}

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error(
      'E2E_CONTEXT_FILE is required; run the suite through the Compose smoke entrypoint',
    );
  }
  const parsed = JSON.parse(readFileSync(file, 'utf8')) as Partial<E2EContext>;
  if (parsed.baseURL === undefined || parsed.waitBudgetMs === undefined) {
    throw new Error('the Compose smoke context is missing baseURL or waitBudgetMs');
  }
  return parsed as E2EContext;
}

const context = loadContext();
const configuredCommit = process.env.LAMUFLIX_COMMIT?.trim() ?? '';
const settledSuccess: VersionDto = {
  release: null,
  commit: configuredCommit === '' ? 'unknown' : configuredCommit,
};
const expectedSuccessLine =
  settledSuccess.commit === 'unknown'
    ? 'dev (unknown)'
    : `dev (${settledSuccess.commit.slice(0, 8)})`;

function versionLine(page: Page) {
  return page.locator('.lamu-version-line');
}

async function observeShell(page: Page): Promise<void> {
  await expect(page.getByRole('link', { name: 'LamuFlix' })).toBeVisible();
  await expect(page.getByRole('list', { name: 'Movies' })).toBeVisible({
    timeout: context.waitBudgetMs,
  });
}

type SettledMode = 'success' | 'failure';

async function installHeldVersionRoute(
  page: Page,
  mode: () => SettledMode,
): Promise<{ release: () => void; cleanup: () => Promise<void> }> {
  let releaseHold: () => void = () => {};
  const gate = new Promise<void>((done) => {
    releaseHold = done;
  });
  const routePattern = '**/api/version';
  await page.route(routePattern, async (route: Route) => {
    await gate;
    if (mode() === 'success') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(settledSuccess),
      });
    } else {
      await route.fulfill({ status: 500, contentType: 'text/plain', body: 'controlled failure' });
    }
  });
  return {
    release: () => releaseHold(),
    cleanup: async () => {
      releaseHold();
      await page.unroute(routePattern).catch(() => {});
    },
  };
}

test.describe('DEV-547 version line compose smoke', () => {
  test(`SMOKE-001 exactly one version line in the selected footer (${configuredCommit === '' ? 'unconfigured dev unknown' : 'configured full SHA'})`, async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 4);
    const apiVersion = await request.get(`${context.baseURL}/api/version`);
    expect(apiVersion.status(), 'the Compose API version endpoint is readable').toBe(200);
    const payload = (await apiVersion.json()) as VersionDto;
    expect(payload.release).toBeNull();
    expect(payload.commit).toBe(settledSuccess.commit);

    await page.goto(context.baseURL);
    await observeShell(page);
    await expect(versionLine(page)).toHaveCount(1, { timeout: context.waitBudgetMs });
    await expect(versionLine(page)).toHaveText(expectedSuccessLine);
    console.log(
      `SMOKE-001 pass evidence: API ${JSON.stringify(payload)}; one footer line ${expectedSuccessLine}`,
    );
  });

  test('SMOKE-002 held version request shows no line, then success settles one line and a controlled failure leaves none', async ({
    page,
  }) => {
    test.setTimeout(context.waitBudgetMs * 4);

    const successMode = { current: 'success' as SettledMode };
    const successHold = await installHeldVersionRoute(page, () => successMode.current);
    try {
      await page.goto(context.baseURL);
      await observeShell(page);
      await expect(versionLine(page)).toHaveCount(0);
      console.log(
        'SMOKE-002(a) interception/hold: Playwright page.route("**/api/version") awaits an explicit release promise; shell observed while held',
      );
      console.log('SMOKE-002(a) absence during hold: .lamu-version-line count 0');

      successHold.release();
      await expect(versionLine(page)).toHaveText(expectedSuccessLine, {
        timeout: context.waitBudgetMs,
      });
      await expect(versionLine(page)).toHaveCount(1);
      console.log(`SMOKE-002(a) settled result after release: ${expectedSuccessLine}`);
    } finally {
      await successHold.cleanup();
    }

    const failurePage = await page.context().newPage();
    const failureMode = { current: 'failure' as SettledMode };
    const failureHold = await installHeldVersionRoute(failurePage, () => failureMode.current);
    try {
      await failurePage.goto(context.baseURL);
      await observeShell(failurePage);
      await expect(versionLine(failurePage)).toHaveCount(0);
      console.log(
        'SMOKE-002(b) interception/hold: Playwright page.route("**/api/version") awaits an explicit release promise; shell observed while held',
      );
      console.log('SMOKE-002(b) absence during hold: .lamu-version-line count 0');

      failureHold.release();
      await expect(versionLine(failurePage)).toHaveCount(0);
      await expect(versionLine(failurePage)).toHaveCount(0, { timeout: 1000 });
      console.log(
        'SMOKE-002(b) settled result after release: HTTP 500 leaves no line or placeholder',
      );
    } finally {
      await failureHold.cleanup();
      await failurePage.close();
    }
  });
});
