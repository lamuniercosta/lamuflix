import { readFileSync } from 'node:fs';
import { expect, test, type APIRequestContext } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
  outsideLibrary: {
    tmdbId: number;
    title: string;
    releaseYear: number;
    reason: string;
  };
}

type OutsideLibraryCard = components['schemas']['OutsideLibraryRecommendation'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();
const fixture = context.outsideLibrary;
const seedTitles = ['Big Buck Bunny', 'Elephants Dream'];

async function markSeedsWatched(request: APIRequestContext): Promise<void> {
  const response = await request.get(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=100`,
  );
  expect(response.ok()).toBe(true);
  const browse = (await response.json()) as { items: Array<{ id: number; title: string }> };
  for (const title of seedTitles) {
    const movie = browse.items.find((item) => item.title === title);
    expect(movie, `${title} is available as a discovery seed`).toBeDefined();
    const marked = await request.post(`${context.baseURL}/api/movies/${String(movie?.id)}/watched`);
    expect([200, 204]).toContain(marked.status());
  }
}

async function discoveryCards(): Promise<OutsideLibraryCard[]> {
  const response = await fetch(`${context.baseURL}/api/recommendations/not-in-library`);
  expect(response.status).toBe(200);
  return (await response.json()) as OutsideLibraryCard[];
}

test.describe('DEV-561 outside-library discovery', () => {
  test('SMOKE-001 (DEV-561) For You shows Not in your library yet with title, two-seed reason and TMDb link', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 2);

    // arrange: make two local library movies eligible seeds for the seeded candidate
    await markSeedsWatched(request);

    // arrange: the local seeded recommendation rows must be observable through the read route
    await expect
      .poll(async () => (await discoveryCards()).map((card) => card.title), {
        timeout: context.waitBudgetMs,
      })
      .toContain(fixture.title);

    const card = (await discoveryCards()).find((item) => item.title === fixture.title);
    expect(card?.reason).toBe(fixture.reason);
    expect(card?.reason).toMatch(/Big Buck Bunny.*Elephants Dream/);

    // act: open the For You route from the browser
    await page.goto(new URL('/for-you', context.baseURL).href);
    const section = page.getByRole('region', { name: 'Not in your library yet' });

    // assert: SMOKE-001 visible section and unchanged library heading
    await expect(section).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(
      section.getByRole('heading', { level: 2, name: 'Not in your library yet' }),
    ).toBeVisible();
    await expect(page.getByRole('heading', { level: 1, name: 'For You' })).toBeVisible();
    const discoveryCard = section.getByRole('listitem').filter({ hasText: fixture.title });
    await expect(discoveryCard.getByRole('heading', { level: 3 })).toHaveText(fixture.title);
    await expect(discoveryCard.getByText(fixture.reason)).toBeVisible();

    // assert: SMOKE-003 read-only card, safe new-tab link and day-verified credit
    const link = discoveryCard.getByRole('link', {
      name: `View ${fixture.title} on TMDB (opens in a new tab)`,
    });
    await expect(link).toHaveAttribute(
      'href',
      `https://www.themoviedb.org/movie/${fixture.tmdbId}`,
    );
    await expect(link).toHaveAttribute('target', '_blank');
    await expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    expect(await discoveryCard.getByRole('button').count()).toBe(0);
    expect(await discoveryCard.getByRole('link').count()).toBe(1);
    const hrefs = await section
      .getByRole('link')
      .evaluateAll((nodes) => nodes.map((node) => node.getAttribute('href') ?? ''));
    expect(hrefs.every((href) => href.startsWith('https://www.themoviedb.org/'))).toBe(true);
    await expect(
      section.getByText(/not endorsed, certified, or otherwise approved by TMDB/),
    ).toBeVisible();
  });

  test('SMOKE-002 (DEV-561) an empty discovery response hides the section and leaves the library grid usable', async ({
    page,
  }) => {
    test.setTimeout(context.waitBudgetMs * 2);
    await page.route('**/api/recommendations/not-in-library', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
    );

    await page.goto(new URL('/for-you', context.baseURL).href);

    await expect(page.getByRole('heading', { level: 1, name: 'For You' })).toBeVisible();
    await expect(page.locator('.for-you-row .movie-card-title').first()).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(page.getByRole('region', { name: 'Not in your library yet' })).toHaveCount(0);
  });
});
