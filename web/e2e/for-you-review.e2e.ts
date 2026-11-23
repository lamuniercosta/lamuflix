import { readFileSync } from 'node:fs';
import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test';
import type { components } from '../src/api/types';

interface CatalogMovie {
  title: string;
  relativePath: string;
}

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
  forYouCatalog?: CatalogMovie[];
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

const FOR_YOU_TAKE = 10;
const REQUIRED_BASELINE_CANDIDATES = 2 * FOR_YOU_TAKE + 2;
const REQUIRED_UNSEEN_AFTER_EXCLUSION = 2;
const REQUIRED_STABLE_READS = 3;
const STABILITY_INTERVAL_MS = 750;
const RELOAD_MARKER = '__dev517NoReload';
const MORE_FOR_YOU_ROW = 'More for you';

async function readBrowse(request: APIRequestContext): Promise<BrowseResponse> {
  const response = await request.get(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=100`,
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

function recommendationCard(scope: Page | Locator, title: string): Locator {
  const root: Page = 'page' in scope ? scope.page() : scope;
  return scope
    .locator('article.movie-card')
    .filter({ has: root.getByRole('link', { name: title, exact: true }) });
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

    const row = moreForYouRow(page);
    await expect(row.getByRole('heading', { name: MORE_FOR_YOU_ROW, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    const card = recommendationCard(row, RECOMMENDED_TITLE);
    await expect(card).toBeVisible({ timeout: context.waitBudgetMs });
    await expect(
      card.locator('.movie-card-seed'),
      'grouped rows hide the per-card seed line',
    ).toHaveCount(0);

    await page.goto(`${context.baseURL}/movies/${String(seedId)}`);
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

function sliceUrl(offset: number, exclude: ReadonlyArray<number>): string {
  const url = new URL('/api/recommendations/for-you', context.baseURL);
  url.searchParams.set('take', String(FOR_YOU_TAKE));
  url.searchParams.set('offset', String(offset));
  for (const id of exclude) {
    url.searchParams.append('exclude', String(id));
  }
  return url.href;
}

async function readSlice(
  request: APIRequestContext,
  offset: number,
  exclude: ReadonlyArray<number> = [],
): Promise<ForYouPage> {
  const response = await request.get(sliceUrl(offset, exclude));
  expect(response.ok()).toBe(true);
  return (await response.json()) as ForYouPage;
}

async function readRanking(
  request: APIRequestContext,
  exclude: ReadonlyArray<number> = [],
): Promise<ForYouItem[]> {
  const byId = new Map<number, ForYouItem>();
  const order: ForYouItem[] = [];
  for (let offset = 0; offset <= 10 * FOR_YOU_TAKE; offset += FOR_YOU_TAKE) {
    const page = await readSlice(request, offset, exclude);
    for (const item of page.items) {
      const id = Number(item.id);
      if (byId.has(id)) {
        continue;
      }
      byId.set(id, item);
      order.push(item);
    }
    if (!page.hasMore) {
      break;
    }
  }
  return order;
}

async function findMovieByTitle(
  request: APIRequestContext,
  title: string,
): Promise<BrowseResponse['items'][number] | undefined> {
  const browse = await readBrowse(request);
  return browse.items.find((movie) => movie.title === title);
}

async function ignoreStatuses(promise: Promise<{ status(): number }>): Promise<void> {
  await (await promise).status();
}

async function resetFeedbackState(request: APIRequestContext): Promise<void> {
  const browse = await readBrowse(request);
  await Promise.all(
    browse.items.flatMap((movie) => {
      const movieUrl = `${context.baseURL}/api/movies/${String(movie.id)}`;
      return [
        ignoreStatuses(request.put(`${movieUrl}/review`, { data: { review: 'none' } })),
        ignoreStatuses(request.delete(`${movieUrl}/watched`)),
        ignoreStatuses(request.delete(`${movieUrl}/not-interested`)),
        ignoreStatuses(request.delete(`${movieUrl}/watchlist`)),
      ];
    }),
  );
}

function sleep(milliseconds: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, milliseconds);
  });
}

function catalogMovies(): CatalogMovie[] {
  const catalog = context.forYouCatalog;
  if (catalog === undefined || catalog.length === 0) {
    throw new Error(
      'e2e context is missing forYouCatalog; run the suite through npm run e2e so the runner stages the DEV-517 catalog',
    );
  }
  return catalog;
}

async function importForYouCatalog(request: APIRequestContext): Promise<void> {
  for (const movie of catalogMovies()) {
    const imported = await request.post(`${context.baseURL}/api/movies/import`, {
      data: { folderPath: movie.relativePath },
    });
    expect(
      [202, 409],
      `import ${movie.title} expected 202 or 409, got ${imported.status()}`,
    ).toContain(imported.status());
  }
}

async function removeForYouCatalog(request: APIRequestContext): Promise<void> {
  const catalogTitles = new Set(catalogMovies().map((movie) => movie.title));
  const browse = await readBrowse(request);
  for (const movie of browse.items) {
    if (!catalogTitles.has(movie.title)) {
      continue;
    }
    const deleted = await request.delete(`${context.baseURL}/api/movies/${String(movie.id)}`);
    expect([204, 404], `delete ${movie.title} expected 204 or 404`).toContain(deleted.status());
  }
}

interface JourneyPlan {
  initial: ForYouItem[];
  appended: ForYouItem[];
  target: ForYouItem;
  replacement: ForYouItem;
}

async function prepareForYouJourney(request: APIRequestContext): Promise<JourneyPlan> {
  await resetFeedbackState(request);
  await importForYouCatalog(request);

  const seed = await findMovieByTitle(request, SEED_TITLE);
  expect(seed, `seeded movie ${SEED_TITLE} must be browsable before the journey`).toBeDefined();
  const liked = await request.put(`${context.baseURL}/api/movies/${String(seed?.id)}/review`, {
    data: { review: 'liked' },
  });
  expect(liked.status()).toBe(204);

  const baseline = await waitForSettledRanking(request);
  const initial = baseline.slice(0, FOR_YOU_TAKE);
  const appended = baseline.slice(FOR_YOU_TAKE, 2 * FOR_YOU_TAKE);
  const target = initial[0];
  const initialIds = new Set(initial.map((item) => Number(item.id)));
  const renderedIds = new Set(baseline.slice(0, 2 * FOR_YOU_TAKE).map((item) => Number(item.id)));
  const afterExclusion = await readRanking(request, [Number(target.id)]);
  const unseenBeyondGrid = afterExclusion.filter((item) => !renderedIds.has(Number(item.id)));

  if (unseenBeyondGrid.length < REQUIRED_UNSEEN_AFTER_EXCLUSION) {
    throw new Error(
      `For You preflight: excluding ${target.title} left ${unseenBeyondGrid.length} unseen candidates outside the initial and appended grid, required at least ${REQUIRED_UNSEEN_AFTER_EXCLUSION}`,
    );
  }

  const replacement = afterExclusion.find((item) => !initialIds.has(Number(item.id)));
  if (replacement === undefined) {
    throw new Error(
      `For You preflight: excluding ${target.title} left no candidate outside the initial page`,
    );
  }

  return { initial, appended, target, replacement };
}

async function waitForSettledRanking(request: APIRequestContext): Promise<ForYouItem[]> {
  const catalogTitles = new Set(catalogMovies().map((movie) => movie.title));
  const deadline = Date.now() + context.waitBudgetMs * 2;
  let previousSignature: string | null = null;
  let stableReads = 0;
  let latest: ForYouItem[] = [];
  let enriched = 0;

  while (Date.now() < deadline) {
    const browse = await readBrowse(request);
    enriched = browse.items.filter(
      (movie) => catalogTitles.has(movie.title) && movie.enrichmentStatus === 'Enriched',
    ).length;
    latest = await readRanking(request);
    const signature = latest.map((item) => Number(item.id)).join(',');
    stableReads = signature === previousSignature ? stableReads + 1 : 1;
    previousSignature = signature;

    if (
      enriched === catalogTitles.size &&
      latest.length >= REQUIRED_BASELINE_CANDIDATES &&
      stableReads >= REQUIRED_STABLE_READS
    ) {
      return latest;
    }

    await sleep(STABILITY_INTERVAL_MS);
  }

  throw new Error(
    `For You preflight: observed ${latest.length} distinct ranked candidates (required at least ${REQUIRED_BASELINE_CANDIDATES}) and ${enriched}/${catalogTitles.size} enriched catalog movies before the ranking settled`,
  );
}

function forYouPage(page: Page): Locator {
  return page.locator('section.for-you-page');
}

function moreForYouRow(page: Page): Locator {
  return page
    .locator('section.for-you-row')
    .filter({ has: page.getByRole('heading', { name: MORE_FOR_YOU_ROW, exact: true }) });
}

function cardIds(page: Page): Promise<number[]> {
  return forYouPage(page)
    .locator('.movie-card-title a')
    .evaluateAll((links) =>
      links.map((link) => Number((link.getAttribute('href') ?? '').split('/').at(-1))),
    );
}

function cardTitles(page: Page): Promise<string[]> {
  return forYouPage(page).locator('.movie-card-title').allTextContents();
}

async function openForYou(page: Page): Promise<void> {
  await page.goto(`${context.baseURL}/for-you`);
  await expect(page.getByRole('heading', { name: 'For You', exact: true })).toBeVisible({
    timeout: context.waitBudgetMs,
  });
  await expect
    .poll(() => cardIds(page), { timeout: context.waitBudgetMs })
    .toHaveLength(FOR_YOU_TAKE);
}

function idsOf(items: ReadonlyArray<ForYouItem>): number[] {
  return items.map((item) => Number(item.id));
}

function titlesOf(items: ReadonlyArray<ForYouItem>): string[] {
  return items.map((item) => item.title);
}

async function markNoReload(page: Page): Promise<void> {
  await page.evaluate((key) => {
    (window as unknown as Record<string, unknown>)[key] = 'kept';
  }, RELOAD_MARKER);
}

async function expectNoReload(page: Page, action: string): Promise<void> {
  const marker = await page.evaluate(
    (key) => (window as unknown as Record<string, unknown>)[key] ?? null,
    RELOAD_MARKER,
  );
  expect(marker, `${action} must not reload the page`).toBe('kept');
}

test.describe('DEV-517 For You review journey', () => {
  test('DEV-517 SMOKE-001 Load more appends the next ranked page without reload or reordering', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 3);
    try {
      const plan = await prepareForYouJourney(request);
      const initialIds = idsOf(plan.initial);
      const appendedIds = idsOf(plan.appended);

      await openForYou(page);
      await expect.poll(() => cardIds(page), { timeout: context.waitBudgetMs }).toEqual(initialIds);
      await markNoReload(page);

      const loadMore = page.getByRole('button', { name: 'Load more', exact: true });
      await expect(loadMore).toBeEnabled({ timeout: context.waitBudgetMs });
      await loadMore.click();

      await expect
        .poll(() => cardIds(page), { timeout: context.waitBudgetMs })
        .toEqual([...initialIds, ...appendedIds]);
      await expectNoReload(page, 'Load more');

      const titles = await cardTitles(page);
      expect(titles.slice(0, FOR_YOU_TAKE), 'the initial page stays in order').toEqual(
        titlesOf(plan.initial),
      );
      expect(titles.slice(FOR_YOU_TAKE), 'the appended page keeps ranked order').toEqual(
        titlesOf(plan.appended),
      );

      const ids = await cardIds(page);
      expect(new Set(ids).size, 'the appended page adds no duplicate cards').toBe(ids.length);
    } finally {
      await resetFeedbackState(request);
      await removeForYouCatalog(request);
    }
  });

  test('DEV-517 SMOKE-002 Replace swaps in place and the retired movie stays absent for the visit', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs * 4);
    try {
      const plan = await prepareForYouJourney(request);
      const initialIds = idsOf(plan.initial);

      await openForYou(page);
      await expect.poll(() => cardIds(page), { timeout: context.waitBudgetMs }).toEqual(initialIds);
      await markNoReload(page);

      const moreIds = await moreForYouRow(page)
        .locator('.movie-card-title a')
        .evaluateAll((links) =>
          links.map((link) => Number((link.getAttribute('href') ?? '').split('/').at(-1))),
        );
      const target = plan.initial.find((item) => moreIds.includes(Number(item.id)));
      expect(target, 'a More for you card is available to replace').toBeDefined();
      const targetId = Number((target as ForYouItem).id);
      const initialIdSet = new Set(initialIds);
      const replacement = (await readRanking(request, [targetId])).find(
        (item) => !initialIdSet.has(Number(item.id)),
      );
      expect(
        replacement,
        `excluding ${(target as ForYouItem).title} leaves an unseen candidate outside the grid`,
      ).toBeDefined();
      const replacementId = Number((replacement as ForYouItem).id);
      const swappedIds = initialIds.map((id) => (id === targetId ? replacementId : id));

      await page
        .getByRole('button', { name: `Replace ${(target as ForYouItem).title}`, exact: true })
        .click();
      await expect.poll(() => cardIds(page), { timeout: context.waitBudgetMs }).toEqual(swappedIds);
      await expectNoReload(page, 'Replace');

      const titles = await cardTitles(page);
      expect(titles, 'the swapped-in suggestion is rendered').toContain(
        (replacement as ForYouItem).title,
      );
      expect(await cardIds(page), 'the retired movie leaves the grid immediately').not.toContain(
        targetId,
      );

      const loadMore = page.getByRole('button', { name: 'Load more', exact: true });
      await expect(loadMore).toBeEnabled({ timeout: context.waitBudgetMs });
      await loadMore.click();

      await expect
        .poll(async () => (await cardIds(page)).length, { timeout: context.waitBudgetMs })
        .toBeGreaterThan(FOR_YOU_TAKE);
      await expectNoReload(page, 'Load more after Replace');

      const after = await cardIds(page);
      expect(after.slice(0, FOR_YOU_TAKE), 'the swapped grid stays in place').toEqual(swappedIds);
      expect(after, 'the retired movie never returns in this visit').not.toContain(targetId);
      expect(new Set(after).size, 'the follow-on page adds no duplicates').toBe(after.length);
    } finally {
      await resetFeedbackState(request);
      await removeForYouCatalog(request);
    }
  });
});
