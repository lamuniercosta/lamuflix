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

test.describe('seeded browse grid', () => {
  test('shows both seeded movies on the grid', async ({ page }) => {
    await page.goto(context.baseURL);

    const grid = page.getByRole('list', { name: 'Movies' });
    for (const title of context.seedTitles) {
      await expect(grid.getByRole('link', { name: title, exact: true })).toBeVisible({
        timeout: context.waitBudgetMs,
      });
    }
  });
});

test.describe('browse URL state parity', () => {
  test('SMOKE-001 (DEV-475) applied filter and sort survive a reload, and defaults plus invalid values are unchanged', async ({
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

    const sortSelect = page.locator('.library-sort-select');
    const grid = page.getByRole('list', { name: 'Movies' });
    const [includedTitle, excludedTitle] = context.seedTitles;

    await page.goto(context.baseURL);
    await expect(sortSelect).toHaveValue('Title|Ascending', { timeout: context.waitBudgetMs });

    const filtersToggle = page.getByRole('button', { name: /^Filters/ });
    await expect(filtersToggle).toBeVisible({ timeout: context.waitBudgetMs });
    if ((await filtersToggle.getAttribute('aria-expanded')) !== 'true') {
      await filtersToggle.click();
    }
    const genreRow = page
      .getByRole('group', { name: 'Genres' })
      .locator('.lamu-filter-row')
      .filter({ has: page.locator('.lamu-filter-row-name', { hasText: 'Animation' }) });
    await expect(genreRow).toBeVisible({ timeout: context.waitBudgetMs });
    await genreRow.click();

    await expect
      .poll(() => new URL(page.url()).searchParams.get('genreIds'), {
        timeout: context.waitBudgetMs,
      })
      .toBe(String(genre.id));

    await expect(grid.getByRole('link', { name: includedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(grid.getByRole('link', { name: excludedTitle, exact: true })).toHaveCount(0);

    await sortSelect.selectOption('Year|Descending');
    await expect
      .poll(() => new URL(page.url()).searchParams.get('sort'), {
        timeout: context.waitBudgetMs,
      })
      .toBe('Year');
    await expect
      .poll(() => new URL(page.url()).searchParams.get('direction'), {
        timeout: context.waitBudgetMs,
      })
      .toBe('Descending');

    await page.reload();
    await expect(sortSelect).toHaveValue('Year|Descending', { timeout: context.waitBudgetMs });
    await expect
      .poll(() => new URL(page.url()).searchParams.get('genreIds'), {
        timeout: context.waitBudgetMs,
      })
      .toBe(String(genre.id));
    await expect(grid.getByRole('link', { name: includedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(grid.getByRole('link', { name: excludedTitle, exact: true })).toHaveCount(0);
    await expect(page.locator('.library-tag-list .lamu-tag-value')).toHaveText(/Animation/);

    await page.goto(`${context.baseURL}/?collectionId=abc`);
    await expect(page.getByRole('alert')).toHaveCount(0);
    for (const title of context.seedTitles) {
      await expect(grid.getByRole('link', { name: title, exact: true })).toBeVisible({
        timeout: context.waitBudgetMs,
      });
    }
    await expect(sortSelect).toHaveValue('Title|Ascending');
  });

  test('SMOKE-003 (DEV-475) Back and Forward resync the library and details views without duplicate history entries', async ({
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

    const [includedTitle, excludedTitle] = context.seedTitles;
    const grid = page.getByRole('list', { name: 'Movies' });

    await page.goto(context.baseURL);
    const filtersToggle = page.getByRole('button', { name: /^Filters/ });
    await expect(filtersToggle).toBeVisible({ timeout: context.waitBudgetMs });
    if ((await filtersToggle.getAttribute('aria-expanded')) !== 'true') {
      await filtersToggle.click();
    }
    const genreRow = page
      .getByRole('group', { name: 'Genres' })
      .locator('.lamu-filter-row')
      .filter({ has: page.locator('.lamu-filter-row-name', { hasText: 'Animation' }) });
    await expect(genreRow).toBeVisible({ timeout: context.waitBudgetMs });
    await genreRow.click();
    await expect
      .poll(() => new URL(page.url()).searchParams.get('genreIds'), {
        timeout: context.waitBudgetMs,
      })
      .toBe(String(genre.id));

    const historyBeforeOpen = await page.evaluate(() => window.history.length);
    await grid.getByRole('link', { name: includedTitle, exact: true }).click();
    await expect(page.getByRole('heading', { name: includedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    const historyOnDetails = await page.evaluate(() => window.history.length);
    expect(historyOnDetails, 'opening a movie adds exactly one history entry').toBe(
      historyBeforeOpen + 1,
    );

    await page.goBack();
    await expect
      .poll(() => new URL(page.url()).searchParams.get('genreIds'), {
        timeout: context.waitBudgetMs,
      })
      .toBe(String(genre.id));
    await expect(grid.getByRole('link', { name: includedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(grid.getByRole('link', { name: excludedTitle, exact: true })).toHaveCount(0);
    expect(
      await page.evaluate(() => window.history.length),
      'Back does not push a duplicate entry',
    ).toBe(historyOnDetails);

    await page.goForward();
    await expect(page.getByRole('heading', { name: includedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    expect(
      await page.evaluate(() => window.history.length),
      'Forward does not push a duplicate entry',
    ).toBe(historyOnDetails);
  });
});
