import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import type { components } from '../src/api/types';

interface ManualMatchMovie {
  smokeId: string;
  relativePath: string;
  title: string;
  imdbId: string;
  posterUrl: string;
  rating: string;
  director: string;
}

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
  manualMatchMovies: ManualMatchMovie[];
}

const file = process.env.E2E_CONTEXT_FILE;
if (!file) {
  throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
}
const context = JSON.parse(readFileSync(file, 'utf8')) as E2EContext;

let importedMovieUrl: URL | undefined;

test.afterEach(async ({ request }) => {
  if (importedMovieUrl) {
    const deleted = await request.delete(importedMovieUrl.href);
    importedMovieUrl = undefined;
    expect(deleted.status()).toBe(204);
  }
});

for (const movie of context.manualMatchMovies) {
  test(`${movie.smokeId} restores ${movie.title} metadata through Match Manually`, async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs);
    const imported = await request.post(`${context.baseURL}/api/movies/import`, {
      data: { folderPath: movie.relativePath },
    });
    expect(imported.status()).toBe(202);
    const location = imported.headers().location;
    expect(location).toMatch(/^\/api\/movies\/\d+$/);
    const movieUrl = new URL(location, context.baseURL);

    importedMovieUrl = movieUrl;
    const movieId = movieUrl.pathname.split('/').at(-1);
    await expect
      .poll(
        async () => {
          const response = await request.get(
            `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
          );
          expect(response.ok()).toBe(true);
          const { items } =
            (await response.json()) as components['schemas']['BrowseMoviesResponse'];
          return items.find((item) => String(item.id) === movieId)?.enrichmentStatus;
        },
        { timeout: context.waitBudgetMs },
      )
      .toBe('NotFound');

    await page.route(movie.posterUrl, (route) =>
      route.fulfill({
        contentType: 'image/svg+xml',
        body: '<svg xmlns="http://www.w3.org/2000/svg" width="10" height="15"><rect width="10" height="15"/></svg>',
      }),
    );
    await page.goto(context.baseURL);
    const link = page.getByRole('link', { name: movie.title, exact: true });
    const row = page.getByRole('listitem').filter({ has: link });
    await expect(row).toContainText('Not found', { timeout: context.waitBudgetMs });
    await link.click();
    await expect(page).toHaveURL(
      new URL(movieUrl.pathname.replace(/^\/api/, ''), context.baseURL).href,
    );

    const match = page.getByRole('button', { name: 'Match manually' });
    await expect(match).toBeVisible({ timeout: context.waitBudgetMs });
    await match.click();
    const dialog = page.getByRole('dialog', { name: 'Match manually' });
    await dialog.getByLabel('IMDb id or URL').fill(movie.imdbId);
    const matched = page.waitForResponse(
      (response) =>
        response.request().method() === 'PUT' &&
        new URL(response.url()).pathname === `${movieUrl.pathname}/imdb`,
    );
    await dialog.getByRole('button', { name: 'Match', exact: true }).click();
    expect((await matched).ok()).toBe(true);

    await expect
      .poll(
        async () => {
          const response = await request.get(movieUrl.href);
          expect(response.ok()).toBe(true);
          const details = (await response.json()) as components['schemas']['MovieDetailsResponse'];
          return details.metadata?.imdbId;
        },
        { timeout: context.waitBudgetMs },
      )
      .toBe(movie.imdbId);
    await page.reload();

    const imdb = page.getByRole('link', { name: 'View on IMDb' });
    await expect(imdb).toHaveAttribute('href', `https://www.imdb.com/title/${movie.imdbId}/`, {
      timeout: context.waitBudgetMs,
    });
    await expect(page.getByRole('heading', { level: 1, name: movie.title })).toBeVisible();
    await expect(page.getByText(`IMDb ${movie.rating}/10`, { exact: true })).toBeVisible();
    await expect(page.getByText(`Director: ${movie.director}`, { exact: true })).toBeVisible();
    const poster = page.getByRole('img', { name: `${movie.title} poster` });
    await expect(poster).toHaveAttribute('src', movie.posterUrl);
    await expect(poster).toHaveJSProperty('naturalWidth', 10);
  });
}
