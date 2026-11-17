import { readFileSync } from 'node:fs';
import { expect, test, type Page } from '@playwright/test';

interface E2EContext {
  baseURL: string;
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
const CANDIDATES_PATTERN = '**/api/movies/import/candidates';

async function openDialog(page: Page) {
  await page.goto(context.baseURL);
  await page.getByRole('button', { name: 'Bulk import' }).click();
  const dialog = page.getByRole('dialog', { name: 'Bulk import' });
  await expect(dialog).toBeVisible({ timeout: context.waitBudgetMs });
  return dialog;
}

test.describe('bulk import dialog scan states', () => {
  test('shows progress while the scan runs', async ({ page }) => {
    let release!: () => void;
    const scanGate = new Promise<void>((resolve) => {
      release = resolve;
    });
    await page.route(CANDIDATES_PATTERN, async (route) => {
      await scanGate;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ newFolders: [] }),
      });
    });

    const dialog = await openDialog(page);
    await expect(dialog.getByText(/scanning library/i)).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeVisible({
      timeout: context.waitBudgetMs,
    });

    release();
    await expect(dialog.getByText('No new movies found')).toBeVisible({
      timeout: context.waitBudgetMs,
    });
  });

  test('reports an empty scan with no new movies', async ({ page }) => {
    await page.route(CANDIDATES_PATTERN, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ newFolders: [] }),
      });
    });

    const dialog = await openDialog(page);
    await expect(dialog.getByText('No new movies found')).toBeVisible({
      timeout: context.waitBudgetMs,
    });
  });

  test('offers Close and closes the finished dialog', async ({ page }) => {
    await page.route(CANDIDATES_PATTERN, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ newFolders: [] }),
      });
    });

    const dialog = await openDialog(page);
    await expect(dialog.getByText('No new movies found')).toBeVisible({
      timeout: context.waitBudgetMs,
    });
    await expect(dialog.getByRole('button', { name: 'Cancel' })).toHaveCount(0, {
      timeout: context.waitBudgetMs,
    });
    await dialog.getByRole('button', { name: 'Close' }).click();
    await expect(dialog).toBeHidden({ timeout: context.waitBudgetMs });
  });
});
