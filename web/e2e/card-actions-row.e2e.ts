import { readFileSync } from 'node:fs';
import { expect, test, type Page } from '@playwright/test';
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
const GENRE_NAME = 'Animation';

async function expectNoHorizontalScroll(pageTitle: string, page: Page) {
  const metrics = await page.evaluate(() => ({
    scrollWidth: document.documentElement.scrollWidth,
    clientWidth: document.documentElement.clientWidth,
  }));
  expect(
    metrics.scrollWidth,
    `${pageTitle}: page must not scroll sideways (scrollWidth ${metrics.scrollWidth} > clientWidth ${metrics.clientWidth})`,
  ).toBeLessThanOrEqual(metrics.clientWidth);
}

async function expectFourActionsOnOneRow(pageTitle: string, page: Page) {
  const grid = page.getByRole('list', { name: 'Movies' });
  const firstCard = grid.locator('.movie-card').first();
  await firstCard.hover();
  const icons = firstCard.locator(
    '.movie-card-actions .movie-feedback-actions button.movie-feedback-icon',
  );
  await expect(icons).toHaveCount(4, { timeout: context.waitBudgetMs });
  const tops = await icons.evaluateAll((elements) =>
    elements.map((element) => element.getBoundingClientRect().top),
  );
  expect(tops).toHaveLength(4);
  const spread = Math.max(...tops) - Math.min(...tops);
  expect(
    spread,
    `${pageTitle}: the four card actions must share one row (top spread ${spread}px)`,
  ).toBeLessThanOrEqual(1);
}

test.describe('card action row', () => {
  test('stays on one row with no horizontal scroll at 375px', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await page.goto(context.baseURL);

    const grid = page.getByRole('list', { name: 'Movies' });
    await expect(grid).toBeVisible({ timeout: context.waitBudgetMs });

    await expectNoHorizontalScroll('375px', page);
    await expectFourActionsOnOneRow('375px', page);
  });

  test('stays on one row with no horizontal scroll at 1024px with a genre filter', async ({
    page,
    request,
  }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    const response = await request.get(`${context.baseURL}/api/genres`);
    expect(response.ok()).toBe(true);
    const genres = (await response.json()) as GenreDto[];
    const genre = genres.find((item) => item.name === GENRE_NAME);
    if (genre === undefined) {
      throw new Error(`genre ${GENRE_NAME} is not available`);
    }

    await page.goto(context.baseURL);

    const filtersToggle = page.getByRole('button', { name: /^Filters/ });
    await expect(filtersToggle).toBeVisible({ timeout: context.waitBudgetMs });
    if ((await filtersToggle.getAttribute('aria-expanded')) !== 'true') {
      await filtersToggle.click();
    }

    const genreRow = page
      .getByRole('group', { name: 'Genres' })
      .locator('.lamu-filter-row')
      .filter({ has: page.locator('.lamu-filter-row-name', { hasText: GENRE_NAME }) });
    await expect(genreRow).toBeVisible({ timeout: context.waitBudgetMs });
    await genreRow.click();

    await expect
      .poll(() => new URL(page.url()).searchParams.get('genreIds'), {
        timeout: context.waitBudgetMs,
      })
      .toBe(String(genre.id));

    const grid = page.getByRole('list', { name: 'Movies' });
    await expect(grid).toBeVisible({ timeout: context.waitBudgetMs });

    await expectNoHorizontalScroll('1024px with genre filter', page);
    await expectFourActionsOnOneRow('1024px with genre filter', page);
  });
});
