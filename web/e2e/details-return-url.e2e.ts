import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

type GenreDto = components['schemas']['GenreDto'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();

test.describe('details return navigation', () => {
  test('SMOKE-002 (DEV-475) in-app details exits restore the filtered library URL and player intent is kept per entry point', async ({
    page,
    request,
  }) => {
    const response = await request.get(`${context.baseURL}/api/genres`);
    expect(response.ok()).toBe(true);
    const genres = (await response.json()) as GenreDto[];
    const genre = genres.find((item) => item.name === 'Animation');
    if (genre === undefined) {
      throw new Error('genre Animation is not available');
    }

    const title = context.seedTitles[0];
    const grid = page.getByRole('list', { name: 'Movies' });

    await page.goto(`${context.baseURL}/?genreIds=${String(genre.id)}`);
    await expect(grid.getByRole('link', { name: title, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    const filteredUrl = new URL(page.url());

    await grid.getByRole('link', { name: title, exact: true }).click();
    await expect(page.getByRole('heading', { level: 1, name: title })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    expect(
      await page.evaluate(
        () => (window.history.state as { playerIntent?: boolean } | null)?.playerIntent ?? null,
      ),
      'opening a card from the grid is not a player entry',
    ).toBeNull();

    const backLink = page.locator('.details-back-link');
    await expect(backLink).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(backLink).toHaveAttribute('href', `/?genreIds=${String(genre.id)}`);
    await expect(backLink).toHaveText(/Animation/);

    await backLink.click();
    await expect(page).toHaveURL(filteredUrl.href, { timeout: context.waitBudgetMs });
    await expect(grid.getByRole('link', { name: title, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });

    await grid.getByRole('button', { name: `Play ${title}` }).click();
    await expect(page).toHaveURL(/\/movies\/\d+$/, { timeout: context.waitBudgetMs });
    expect(
      await page.evaluate(
        () => (window.history.state as { playerIntent?: boolean } | null)?.playerIntent,
      ),
      'card Play carries player intent',
    ).toBe(true);
    expect(
      await page.evaluate(
        () =>
          (window.history.state as { libraryReturnUrl?: string } | null)?.libraryReturnUrl ?? null,
      ),
      'card Play remembers the filtered library return URL',
    ).toBe(`/?genreIds=${String(genre.id)}`);

    await page.locator('.details-back-link').click();
    await expect(page).toHaveURL(filteredUrl.href, { timeout: context.waitBudgetMs });
    await expect(page.getByRole('heading', { level: 1, name: title })).toHaveCount(0);
    await expect(page.locator('.library-tag-list .lamu-tag-value')).toHaveText(/Animation/);
  });
});
