import openapiTS from 'openapi-typescript';
import { writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

const SOURCE = new URL('../src/api/openapi.json', import.meta.url);
const DEFAULT_OUTPUT = new URL('../src/api/types.ts', import.meta.url);
const HEADER = `/**
 * AUTO-GENERATED FILE — DO NOT EDIT DIRECTLY.
 *
 * Source:     src/api/openapi.json
 * Regenerate: npm run generate:api
 *
 * Any manual changes will be overwritten by the generator.
 */
`;

/** @returns {Promise<string>} */
export async function renderTypes() {
  const result = await openapiTS(SOURCE, { commentHeader: HEADER });
  return result.replace(/\r\n/g, '\n');
}

/** @param {string | URL} [outFile] */
export async function generateTypes(outFile = DEFAULT_OUTPUT) {
  const content = await renderTypes();
  await writeFile(outFile, content, 'utf-8');
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  await generateTypes();
  console.log('Wrote src/api/types.ts');
}
