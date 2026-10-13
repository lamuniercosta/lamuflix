import { generateTypes, DEFAULT_OUTPUT } from './generate-api.mjs';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const COMMITTED_FILE = 'src/api/types.ts';

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

const tempDir = await mkdtemp(join(tmpdir(), 'lamuflix-api-'));
try {
  const tempFile = join(tempDir, 'types.ts');
  await generateTypes(tempFile);
  const fresh = await readFile(tempFile);
  const committed = await readCommitted(DEFAULT_OUTPUT);

  if (committed === null) {
    console.error(`Error: ${COMMITTED_FILE} is missing; run npm run generate:api`);
    process.exitCode = 1;
  } else if (!fresh.equals(committed)) {
    console.error(`Error: ${COMMITTED_FILE} is out of date; run npm run generate:api`);
    process.exitCode = 1;
  }
} finally {
  await rm(tempDir, { recursive: true, force: true });
}
