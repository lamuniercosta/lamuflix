import { readFileSync } from 'node:fs';
import { expect, test, type APIRequestContext } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];
type ForYouItem = components['schemas']['RecommendedMovieDto'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();
const [likedTitle, similarTitle] = context.seedTitles;

async function readBrowse(request: APIRequestContext): Promise<BrowseResponse> {
  const response = await request.get(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
  );
  expect(response.ok()).toBe(true);
  return (await response.json()) as BrowseResponse;
}

async function readForYou(request: APIRequestContext): Promise<ForYouItem[]> {
  const response = await request.get(`${context.baseURL}/api/recommendations/for-you?take=10`);
  expect(response.ok()).toBe(true);
  return (await response.json()) as ForYouItem[];
}

function reasonText(reason: string | { text: string }): string {
  return typeof reason === 'string' ? reason : reason.text;
}

test.describe('for-you review ranking', () => {
  test('SMOKE-003 ranks the similar seeded movie higher after a like, with grounded reasons', async ({
    page,
    request,
  }) => {
    const browse = await readBrowse(request);
    const liked = browse.items.find((movie) => movie.title === likedTitle);
    const similar = browse.items.find((movie) => movie.title === similarTitle);
    expect(liked, `seeded movie ${likedTitle} is browsable`).toBeDefined();
    expect(similar, `seeded movie ${similarTitle} is browsable`).toBeDefined();

    const before = await readForYou(request);
    expect(
      before,
      'For You starts empty on a fresh seeded stack with no positive signal',
    ).toHaveLength(0);

    await page.goto(context.baseURL);
    const grid = page.getByRole('list', { name: 'Movies' });
    const likeButton = grid.getByRole('button', { name: `Like, ${likedTitle}`, exact: true });
    await expect(likeButton).toBeVisible({ timeout: context.waitBudgetMs });
    await likeButton.click();
    await expect(
      grid.getByRole('button', { name: `Clear liked review, ${likedTitle}`, exact: true }),
    ).toBeVisible({ timeout: context.waitBudgetMs });

    await expect
      .poll(
        async () =>
          (await readBrowse(request)).items.find((movie) => movie.title === likedTitle)?.review,
        { timeout: context.waitBudgetMs },
      )
      .toBe('liked');

    let after: ForYouItem[] = [];
    await expect
      .poll(
        async () => {
          after = await readForYou(request);
          return after.length;
        },
        { timeout: context.waitBudgetMs },
      )
      .toBeGreaterThan(0);

    const titles = after.map((movie) => movie.title);
    expect(titles, 'similar seeded movie ranks into For You after the like').toContain(
      similarTitle,
    );

    for (const item of after) {
      for (const reason of item.reasons ?? []) {
        const text = reasonText(reason);
        if (/because you liked/i.test(text)) {
          expect(text, 'liked reason names the actually liked movie').toContain(likedTitle);
          expect(text, 'liked reason never names the non-liked movie').not.toContain(similarTitle);
        }
      }
    }

    await page.reload();
    const rail = page.locator('section.for-you-rail');
    await expect(rail.getByRole('heading', { name: 'For You' })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(rail.getByRole('link', { name: similarTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
  });
});
