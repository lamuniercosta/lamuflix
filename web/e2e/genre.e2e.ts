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
const GENRE_NAME = 'Animation';
const [includedTitle, excludedTitle] = context.seedTitles;

test.describe('genre filter', () => {
  test('keeps the matching seeded movie, drops the other, and writes genreIds to the URL', async ({
    page,
    request,
  }) => {
    const response = await request.get(`${context.baseURL}/api/genres`);
    expect(response.ok()).toBe(true);
    const genres = (await response.json()) as GenreDto[];
    const genre = genres.find((item) => item.name === GENRE_NAME);
    if (genre === undefined) {
      throw new Error(`genre ${GENRE_NAME} is not available`);
    }

    await page.goto(context.baseURL);

    const genreControl = page
      .getByRole('group', { name: 'Browse by genre' })
      .getByRole('button', { name: GENRE_NAME, exact: true });
    await expect(genreControl).toBeVisible({ timeout: context.waitBudgetMs });
    await genreControl.click();

    await expect
      .poll(() => new URL(page.url()).searchParams.get('genreIds'), {
        timeout: context.waitBudgetMs,
      })
      .toBe(String(genre.id));

    const grid = page.getByRole('list', { name: 'Movies' });
    await expect(grid.getByRole('link', { name: includedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(grid.getByRole('link', { name: excludedTitle, exact: true })).toHaveCount(0, {
      timeout: context.waitBudgetMs,
    });
  });
});
