import { readFileSync } from 'node:fs';
import { expect, test, type APIRequestContext, type Page, type Route } from '@playwright/test';
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
const seedTitles = ['Big Buck Bunny', 'Elephants Dream'];
const lowQualityTitle = 'Below Floor Contender';
const survivorTitle = 'Next Eligible Survivor';
const firstCollectionTitle = 'Collection Representative';
const duplicateCollectionTitle = 'Collection Follow-up';
const expectedTitles = [context.outsideLibrary.title, survivorTitle, firstCollectionTitle];
const sectionName = 'Not in your library yet';

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
    expect([200, 204, 409]).toContain(marked.status());
  }
}

async function discoveryCards(): Promise<OutsideLibraryCard[]> {
  const response = await fetch(`${context.baseURL}/api/recommendations/not-in-library`);
  expect(response.status).toBe(200);
  return (await response.json()) as OutsideLibraryCard[];
}

async function visibleTitles(page: Page): Promise<string[]> {
  const section = page.getByRole('region', { name: sectionName });
  await expect(section).toBeVisible({ timeout: context.waitBudgetMs });
  return section.getByRole('listitem').getByRole('heading', { level: 3 }).allTextContents();
}

async function openForYouWithDiscovery(
  page: Page,
  handler: (route: Route) => Promise<void> | void,
): Promise<void> {
  await page.route('**/api/recommendations/not-in-library', handler);
  await page.goto(new URL('/for-you', context.baseURL).href, { waitUntil: 'domcontentloaded' });
}

test.describe('DEV-571 outside-library discovery parity', () => {
  test('SMOKE-001 (DEV-571) a known low-quality candidate yields the next eligible survivor in unchanged order', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 2);
    await markSeedsWatched(request);
    await expect
      .poll(async () => (await discoveryCards()).map((card) => card.title), {
        timeout: context.waitBudgetMs,
      })
      .toEqual(expectedTitles);

    const cards = await discoveryCards();
    expect(cards.map((card) => card.tmdbId)).toEqual([945961, 945971, 945972]);
    expect(cards.map((card) => card.title)).not.toContain(lowQualityTitle);
    expect(cards.every((card) => card.reason === context.outsideLibrary.reason)).toBe(true);
    const fields = cards.flatMap((card) => Object.keys(card));
    expect(
      fields.every((field) =>
        ['tmdbId', 'title', 'releaseYear', 'posterPath', 'reason'].includes(field),
      ),
    ).toBe(true);

    await page.goto(new URL('/for-you', context.baseURL).href);
    expect(await visibleTitles(page)).toEqual(expectedTitles);
    await expect(page.getByRole('heading', { level: 1, name: 'For You' })).toBeVisible();
    await expect(page.locator('.for-you-row .movie-card-title').first()).toBeVisible({
      timeout: context.waitBudgetMs,
    });
  });

  test('SMOKE-002 (DEV-571) a known TMDb collection shows at most one representative', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 2);
    await markSeedsWatched(request);
    await expect
      .poll(async () => (await discoveryCards()).map((card) => card.title), {
        timeout: context.waitBudgetMs,
      })
      .toEqual(expectedTitles);

    const titles = await discoveryCards();
    const collectionTitles = titles
      .map((card) => card.title)
      .filter((title) => title.startsWith('Collection '));
    expect(collectionTitles).toEqual([firstCollectionTitle]);
    expect(titles.map((card) => card.title)).not.toContain(duplicateCollectionTitle);

    await page.goto(new URL('/for-you', context.baseURL).href);
    const rendered = await visibleTitles(page);
    expect(rendered.filter((title) => title.startsWith('Collection '))).toEqual([
      firstCollectionTitle,
    ]);
  });

  test('SMOKE-003 (DEV-571) discovery loading empty and error keep the section hidden and the grid usable', async ({
    page,
  }) => {
    test.setTimeout(context.waitBudgetMs * 3);

    const scenarios: Array<{
      label: string;
      handler: (route: Route) => Promise<void> | void;
    }> = [
      {
        label: 'loading',
        handler: async (route) => {
          await new Promise((resolve) => setTimeout(resolve, 1500));
          await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
        },
      },
      {
        label: 'empty',
        handler: (route) =>
          route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }),
      },
      {
        label: 'error',
        handler: (route) =>
          route.fulfill({
            status: 503,
            contentType: 'application/problem+json',
            body: JSON.stringify({ title: 'Discovery unavailable', status: 503 }),
          }),
      },
    ];

    for (const scenario of scenarios) {
      const scenarioPage = await page.context().newPage();
      try {
        await openForYouWithDiscovery(scenarioPage, scenario.handler);
        await expect(
          scenarioPage.getByRole('heading', { level: 1, name: 'For You' }),
        ).toBeVisible();
        await expect(scenarioPage.locator('.for-you-row .movie-card-title').first()).toBeVisible({
          timeout: context.waitBudgetMs,
        });
        await expect(scenarioPage.getByRole('region', { name: sectionName })).toHaveCount(0);
      } finally {
        await scenarioPage.close();
      }
    }
  });
});
