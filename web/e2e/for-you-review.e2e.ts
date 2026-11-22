import { readFileSync } from 'node:fs';
import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];
type BrowseItem = BrowseResponse['items'][number];
type ForYouPage = components['schemas']['ForYouRecommendationsPageDto'];
type ForYouItem = components['schemas']['RecommendedMovieDto'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();

const SEED_TITLE = context.seedTitles[0];
const RECOMMENDED_TITLE = context.seedTitles[1];

const DESKTOP_VIEWPORT = { width: 1440, height: 900 };
const SMALL_VIEWPORT = { width: 375, height: 800 };
const EVIDENCE_INPUT_ITEMS = 3;
const DESKTOP_EVIDENCE_LINES = 2;
const SMALL_EVIDENCE_LINES = 1;

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
  return ((await response.json()) as ForYouPage).items;
}

async function readRelated(request: APIRequestContext, movieId: number): Promise<ForYouItem[]> {
  const response = await request.get(
    `${context.baseURL}/api/movies/${String(movieId)}/recommendations`,
  );
  expect(response.ok()).toBe(true);
  return (await response.json()) as ForYouItem[];
}

function requireBrowsable(browse: BrowseResponse, title: string): BrowseItem {
  const movie = browse.items.find((item) => item.title === title);
  expect(movie, `seeded movie ${title} is browsable`).toBeDefined();
  return movie as BrowseItem;
}

function recommendationCard(page: Page, title: string): Locator {
  return page
    .locator('article.movie-card')
    .filter({ has: page.getByRole('link', { name: title, exact: true }) });
}

async function ensureSeedLiked(page: Page, request: APIRequestContext): Promise<void> {
  await page.goto(context.baseURL);
  const grid = page.getByRole('list', { name: 'Movies' });
  const likedButton = grid.getByRole('button', {
    name: `Clear liked review, ${SEED_TITLE}`,
    exact: true,
  });
  const likeButton = grid.getByRole('button', { name: `Like, ${SEED_TITLE}`, exact: true });
  await expect(likeButton.or(likedButton)).toBeVisible({ timeout: context.waitBudgetMs });
  if (await likedButton.isVisible()) {
    return;
  }

  await likeButton.click();
  await expect(likedButton).toBeVisible({ timeout: context.waitBudgetMs });
  await expect
    .poll(
      async () =>
        (await readBrowse(request)).items.find((movie) => movie.title === SEED_TITLE)?.review,
      { timeout: context.waitBudgetMs },
    )
    .toBe('liked');
}

async function waitForForYou(request: APIRequestContext): Promise<ForYouItem[]> {
  let items: ForYouItem[] = [];
  await expect
    .poll(
      async () => {
        items = await readForYou(request);
        return items.length;
      },
      { timeout: context.waitBudgetMs },
    )
    .toBeGreaterThan(0);
  return items;
}

interface EvidenceExpectation {
  rendered: number;
  visible: number;
  accessible: number;
}

async function countAccessibleItems(list: Locator): Promise<number> {
  const snapshot = await list.ariaSnapshot();
  return snapshot.split('\n').filter((line) => line.trimStart().startsWith('- listitem')).length;
}

async function expectEvidenceLines(
  card: Locator,
  label: string,
  expected: EvidenceExpectation,
): Promise<void> {
  const list = card.locator('.movie-card-evidence');
  await expect(list, `${label}: evidence list is rendered`).toBeVisible({
    timeout: context.waitBudgetMs,
  });
  const items = list.locator('li');
  await expect(items, `${label}: rendered evidence items`).toHaveCount(expected.rendered);

  let visible = 0;
  for (let index = 0; index < expected.rendered; index += 1) {
    if (await items.nth(index).isVisible()) {
      visible += 1;
    }
  }
  expect(visible, `${label}: visible evidence lines`).toBe(expected.visible);
  expect(await countAccessibleItems(list), `${label}: accessible evidence items`).toBe(
    expected.accessible,
  );
}

test.describe('for-you review ranking', () => {
  test('SMOKE-003 ranks the similar seeded movie higher after a like, with seed and evidence', async ({
    page,
    request,
  }) => {
    const browse = await readBrowse(request);
    const seedMovie = requireBrowsable(browse, SEED_TITLE);
    const recommendedMovie = requireBrowsable(browse, RECOMMENDED_TITLE);
    const seedId = Number(seedMovie.id);
    const recommendedId = Number(recommendedMovie.id);
    expect(seedId, 'the pinned seed and recommendation fixtures use different movie ids').not.toBe(
      recommendedId,
    );

    const before = await readForYou(request);
    expect(
      before,
      'For You starts empty on a fresh seeded stack with no positive signal',
    ).toHaveLength(0);

    await ensureSeedLiked(page, request);
    const after = await waitForForYou(request);

    const titles = after.map((movie) => movie.title);
    expect(titles, 'similar seeded movie ranks into For You after the like').toContain(
      RECOMMENDED_TITLE,
    );

    for (const item of after) {
      expect(item.seed, `${item.title} carries a seed`).toBeTruthy();
      expect(item.seed?.title, 'the seed names the actually liked movie').toBe(SEED_TITLE);
      expect(item.seed?.movieId, 'the seed points at the liked movie id').toBe(seedId);
      expect(item.seed?.signal, 'the seed reports the liked signal').toBe('liked');
      expect(
        item.seed?.movieId,
        `${item.title} is never its own seed in the seeded stack`,
      ).not.toBe(item.id);
      expect(Array.isArray(item.evidence), `${item.title} carries ordered evidence`).toBe(true);
      expect(
        Object.hasOwn(item as object, 'reasons'),
        `${item.title} does not carry the retired reasons key`,
      ).toBe(false);
    }

    await page.goto(`${context.baseURL}/for-you`);
    await expect(page.getByRole('heading', { name: 'For You', exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });

    const card = recommendationCard(page, RECOMMENDED_TITLE);
    await expect(card).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(card.getByText(`Because you liked ${SEED_TITLE}`, { exact: true })).toBeVisible();
    const seedLink = card.getByRole('link', { name: `Open ${SEED_TITLE}`, exact: true });
    await expect(seedLink).toHaveAttribute('href', `/movies/${String(seedId)}`);
    await seedLink.click();
    await expect(page).toHaveURL(new RegExp(`/movies/${String(seedId)}$`));
    await expect(
      page.getByRole('heading', { level: 1, name: SEED_TITLE, exact: true }),
    ).toBeVisible({ timeout: context.waitBudgetMs });

    await page.goto(`${context.baseURL}/for-you`);
    const recommendationLink = recommendationCard(page, RECOMMENDED_TITLE).getByRole('link', {
      name: RECOMMENDED_TITLE,
      exact: true,
    });
    await expect(recommendationLink).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(recommendationLink).toHaveAttribute('href', `/movies/${String(recommendedId)}`);
    await recommendationLink.click();
    await expect(page).toHaveURL(new RegExp(`/movies/${String(recommendedId)}$`));
    await expect(
      page.getByRole('heading', { level: 1, name: RECOMMENDED_TITLE, exact: true }),
    ).toBeVisible({ timeout: context.waitBudgetMs });
  });

  test('SMOKE-003 keeps visible and accessible evidence lines consistent on both surfaces', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 3);
    const browse = await readBrowse(request);
    const seedMovie = requireBrowsable(browse, SEED_TITLE);
    const recommendedMovie = requireBrowsable(browse, RECOMMENDED_TITLE);
    const seedId = Number(seedMovie.id);
    expect(seedId).not.toBe(Number(recommendedMovie.id));

    await ensureSeedLiked(page, request);
    const forYou = await waitForForYou(request);
    const forYouItem = forYou.find((movie) => movie.title === RECOMMENDED_TITLE);
    expect(forYouItem, `${RECOMMENDED_TITLE} is recommended in For You`).toBeDefined();
    expect(
      (forYouItem as ForYouItem).evidence.map((item) => item.text),
      'For You delivers three evidence items for the viewport check',
    ).toHaveLength(EVIDENCE_INPUT_ITEMS);

    const related = await readRelated(request, seedId);
    const relatedItem = related.find((movie) => movie.title === RECOMMENDED_TITLE);
    expect(relatedItem, `${RECOMMENDED_TITLE} is related to ${SEED_TITLE}`).toBeDefined();
    expect(
      (relatedItem as ForYouItem).evidence.map((item) => item.text),
      'Related delivers three evidence items for the viewport check',
    ).toHaveLength(EVIDENCE_INPUT_ITEMS);
    expect(Object.hasOwn(relatedItem as object, 'seed'), 'Related omits the seed key').toBe(false);

    await page.setViewportSize(DESKTOP_VIEWPORT);
    await page.goto(`${context.baseURL}/for-you`);
    const desktopForYou = recommendationCard(page, RECOMMENDED_TITLE);
    await expect(desktopForYou).toBeVisible({ timeout: context.waitBudgetMs });
    await expectEvidenceLines(desktopForYou, 'For You desktop', {
      rendered: DESKTOP_EVIDENCE_LINES,
      visible: DESKTOP_EVIDENCE_LINES,
      accessible: DESKTOP_EVIDENCE_LINES,
    });

    await page.setViewportSize(SMALL_VIEWPORT);
    await expectEvidenceLines(desktopForYou, 'For You small width', {
      rendered: DESKTOP_EVIDENCE_LINES,
      visible: SMALL_EVIDENCE_LINES,
      accessible: SMALL_EVIDENCE_LINES,
    });

    await page.setViewportSize(DESKTOP_VIEWPORT);
    await page.goto(`${context.baseURL}/movies/${String(seedId)}`);
    await expect(page.getByRole('heading', { name: 'Related movies', exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    const desktopRelated = recommendationCard(page, RECOMMENDED_TITLE);
    await expect(desktopRelated).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(
      desktopRelated.locator('.movie-card-seed'),
      'Related cards render without a seed line',
    ).toHaveCount(0);
    await expectEvidenceLines(desktopRelated, 'Related desktop', {
      rendered: DESKTOP_EVIDENCE_LINES,
      visible: DESKTOP_EVIDENCE_LINES,
      accessible: DESKTOP_EVIDENCE_LINES,
    });

    await page.setViewportSize(SMALL_VIEWPORT);
    await expectEvidenceLines(desktopRelated, 'Related small width', {
      rendered: DESKTOP_EVIDENCE_LINES,
      visible: SMALL_EVIDENCE_LINES,
      accessible: SMALL_EVIDENCE_LINES,
    });
  });
});
