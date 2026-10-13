import {
  DEFAULT_HANDLERS_OUTPUT,
  DEFAULT_TYPES_OUTPUT,
  generateHandlers,
  generateTypes,
} from './generate-api.mjs';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';

/**
 * @param {Uint8Array | null} committed
 * @param {Uint8Array} fresh
 * @returns {boolean}
 */
export function filesMatch(committed, fresh) {
  return committed !== null && Buffer.from(committed).equals(Buffer.from(fresh));
}

/**
 * @param {string | URL} filePath
 * @returns {Promise<Buffer | null>}
 */
async function readCommitted(filePath) {
  try {
    return await readFile(filePath);
  } catch (err) {
    if (err?.code === 'ENOENT') {
      return null;
    }
    throw err;
  }
}

/**
 * @param {string} label
 * @param {(outFile: string | URL) => Promise<void>} generate
 * @param {string | URL} committedPath
 * @param {string} tempFile
 * @returns {Promise<boolean>}
 */
export async function checkGenerated(label, generate, committedPath, tempFile) {
  await generate(tempFile);
  const fresh = await readFile(tempFile);
  const committed = await readCommitted(committedPath);

  if (committed === null) {
    console.error(`Error: ${label} is missing; run npm run generate:api`);
    return false;
  }

  if (!filesMatch(committed, fresh)) {
    console.error(`Error: ${label} is out of date; run npm run generate:api`);
    return false;
  }

  return true;
}

export async function main() {
  const tempDir = await mkdtemp(join(tmpdir(), 'lamuflix-api-'));
  try {
    const results = [
      await checkGenerated(
        'src/api/types.ts',
        generateTypes,
        DEFAULT_TYPES_OUTPUT,
        join(tempDir, 'types.ts'),
      ),
      await checkGenerated(
        'src/mocks/handlers.ts',
        generateHandlers,
        DEFAULT_HANDLERS_OUTPUT,
        join(tempDir, 'handlers.ts'),
      ),
    ];

    if (results.includes(false)) {
      process.exitCode = 1;
    }
  } finally {
    await rm(tempDir, { recursive: true, force: true });
  }
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  await main();
}
