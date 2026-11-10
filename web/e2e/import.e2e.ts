import { execFile } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { promisify } from 'node:util';
import { expect, test } from '@playwright/test';

const execFileAsync = promisify(execFile);

interface E2EContext {
  baseURL: string;
  waitBudgetMs: number;
  project: string;
  composeFiles: string[];
  envFile: string;
  stubService: string;
  reservedMovie: { title: string; year: number };
  reservedFolders: { initial: string; retry: string };
}

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();

async function stubControl(action: string, args: string[] = []): Promise<void> {
  const [baseCompose, overlayCompose] = context.composeFiles;
  const command = [
    'compose',
    '-p',
    context.project,
    '-f',
    baseCompose,
    '-f',
    overlayCompose,
    '--env-file',
    context.envFile,
    'exec',
    '-T',
    context.stubService,
    'node',
    '/e2e/stub.mjs',
    'control',
    action,
    ...args,
  ];

  try {
    await execFileAsync(process.env.DOCKER ?? 'docker', command, {
      timeout: context.waitBudgetMs,
      maxBuffer: 20 * 1024 * 1024,
      windowsHide: true,
    });
  } catch (error) {
    const stderr = (error as { stderr?: string }).stderr ?? (error as Error).message;
    throw new Error(`stub control ${action} failed: ${stderr}`, { cause: error });
  }
}

test.describe('pending import', () => {
  test('shows a Pending row for the imported folder while the provider response is held', async ({
    page,
  }) => {
    await stubControl('arm', [context.reservedMovie.title]);
    try {
      await page.goto(context.baseURL);

      const responsePromise = page.waitForResponse(
        (response) =>
          response.request().method() === 'POST' &&
          new URL(response.url()).pathname === '/api/movies/import/library',
        { timeout: context.waitBudgetMs },
      );

      await page.getByRole('button', { name: 'Bulk import' }).click();
      const dialog = page.getByRole('dialog', { name: 'Bulk import' });
      await expect(dialog).toBeVisible({ timeout: context.waitBudgetMs });
      await dialog.getByRole('button', { name: 'Select all' }).click();
      await dialog.getByRole('button', { name: 'Import', exact: true }).click();

      const response = await responsePromise;
      expect(response.status()).toBe(200);

      const grid = page.getByRole('list', { name: 'Movies' });
      const row = grid.getByRole('listitem').filter({
        has: page.getByRole('link', { name: context.reservedMovie.title, exact: true }),
      });
      await expect(row).toBeVisible({ timeout: context.waitBudgetMs });
      await expect(row).toContainText('Pending', { timeout: context.waitBudgetMs });
    } finally {
      await stubControl('release', [context.reservedMovie.title]);
    }
  });
});
