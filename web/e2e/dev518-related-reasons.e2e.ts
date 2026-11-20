import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

type BrowseMoviesResponse = components['schemas']['BrowseMoviesResponse'];
type RecommendedMovie = components['schemas']['RecommendedMovieDto'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();

const seedTitle = 'A Fistful of Dollars';
const relatedTitle = 'The Good, the Bad and the Ugly';
const forbiddenReasonPatterns = [
  /runtime/i,
  /IMDb rating/i,
  /\bwriter/i,
  /identical profile/i,
  /^\d{4}$/,
];

async function browseTitles(): Promise<BrowseMoviesResponse> {
  const response = await fetch(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=100`,
  );
  expect(response.status).toBe(200);
  return (await response.json()) as BrowseMoviesResponse;
}

async function readRelated(movieId: string): Promise<RecommendedMovie[]> {
  const response = await fetch(`${context.baseURL}/api/movies/${movieId}/recommendations`);
  expect(response.status).toBe(200);
  return (await response.json()) as RecommendedMovie[];
}

function reasonTexts(movie: RecommendedMovie): string[] {
  return (movie.reasons ?? []).map((reason) => reason.text);
}

function expectReasonsContract(texts: string[], owner: string): void {
  for (const text of texts) {
    for (const pattern of forbiddenReasonPatterns) {
      expect(text, `${owner} reason must not carry removed criteria (${pattern})`).not.toMatch(
        pattern,
      );
    }
  }
}

test.describe('DEV-518 related reasons', () => {
  test('SMOKE-001 (DEV-518) Related lists The Good, the Bad and the Ugly with Same director and Shared themes', async ({
    page,
  }) => {
    test.setTimeout(context.waitBudgetMs * 2);
    const browse = await browseTitles();
    const seed = browse.items.find((movie) => movie.title === seedTitle);

    if (seed === undefined) {
      const candidates = browse.items;
      expect(candidates.length).toBeGreaterThan(0);
      let contractChecked = false;
      for (const candidate of candidates) {
        const related = await readRelated(String(candidate.id));
        for (const item of related) {
          expectReasonsContract(reasonTexts(item), item.title);
          contractChecked = true;
        }
        if (contractChecked) {
          break;
        }
      }
      expect(contractChecked, 'at least one Related rail carries reasons to check').toBe(true);
      return;
    }

    const related = await readRelated(String(seed.id));
    const match = related.find((movie) => movie.title === relatedTitle);
    expect(match, `${relatedTitle} is related to ${seedTitle}`).toBeDefined();
    const texts = reasonTexts(match as RecommendedMovie);
    expect(texts.some((text) => text.startsWith('Same director'))).toBe(true);
    expect(texts.some((text) => text.startsWith('Shared themes'))).toBe(true);
    expectReasonsContract(texts, relatedTitle);

    await page.goto(new URL(`/movies/${String(seed.id)}`, context.baseURL).href);
    await expect(page.getByRole('heading', { name: 'Related movies' })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(page.getByRole('link', { name: relatedTitle, exact: true })).toBeVisible({
      timeout: context.waitBudgetMs,
    });
  });
});
