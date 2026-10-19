import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

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
