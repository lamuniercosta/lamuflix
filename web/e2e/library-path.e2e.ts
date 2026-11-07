import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import type { components } from '../src/api/types';

interface SeedMovie {
  title: string;
  relativePath: string;
}

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  seedMovies?: SeedMovie[];
  waitBudgetMs: number;
}

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type MovieDetailsResponse = components['schemas']['MovieDetailsResponse'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();

test.describe('library-relative movie path', () => {
  test('SMOKE-001 shows and copies the library-relative folder instead of an absolute host path', async ({
    page,
    request,
    context: browserContext,
  }) => {
    test.setTimeout(context.waitBudgetMs);

    const seeded = context.seedMovies?.[0];
    if (seeded === undefined) {
      throw new Error('e2e context must expose seedMovies with a relativePath');
    }

    const browseResponse = await request.get(
      `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
    );
    expect(browseResponse.ok()).toBe(true);
    const { items } = (await browseResponse.json()) as BrowseMoviesResponse;
    const movie = items.find((item) => item.title === seeded.title);
    if (movie === undefined) {
      throw new Error(`seeded movie ${seeded.title} is not in the library`);
    }

    await browserContext.grantPermissions(['clipboard-read', 'clipboard-write'], {
      origin: context.baseURL,
    });

    await page.goto(new URL(`/movies/${String(movie.id)}`, context.baseURL).href);

    const shownPath = page.locator('.details-path');
    await expect(shownPath).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(shownPath).toHaveText(seeded.relativePath);
    await expect(shownPath).not.toHaveText(/^([A-Za-z]:[\\/]|\\\\|\/media\/library)/);

    await page.getByRole('button', { name: 'Copy path' }).click();
    await expect(page.getByRole('status').filter({ hasText: 'Copied' })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    const copied = await page.evaluate(async () => {
      const browserNavigator = (
        globalThis as unknown as { navigator: { clipboard: { readText(): Promise<string> } } }
      ).navigator;
      return browserNavigator.clipboard.readText();
    });
    expect(copied).toBe(seeded.relativePath);

    const detailsResponse = await request.get(`${context.baseURL}/api/movies/${String(movie.id)}`);
    expect(detailsResponse.ok()).toBe(true);
    const details = (await detailsResponse.json()) as MovieDetailsResponse;
    expect(details.path).toBe(seeded.relativePath);
  });
});
