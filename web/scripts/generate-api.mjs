import openapiTS from 'openapi-typescript';
import { readFile, writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

const INPUT_RELATIVE = '../src/api/openapi.json';
const TYPES_RELATIVE = '../src/api/types.ts';
const HANDLERS_RELATIVE = '../src/mocks/handlers.ts';

export const SOURCE = new URL(INPUT_RELATIVE, import.meta.url);
export const DEFAULT_TYPES_OUTPUT = new URL(TYPES_RELATIVE, import.meta.url);
export const DEFAULT_HANDLERS_OUTPUT = new URL(HANDLERS_RELATIVE, import.meta.url);

const HEADER = `/**
 * AUTO-GENERATED FILE — DO NOT EDIT DIRECTLY.
 *
 * Source:     src/api/openapi.json
 * Regenerate: npm run generate:api
 *
 * Any manual changes will be overwritten by the generator.
 */
`;

const METHOD_ORDER = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];

/** @returns {Promise<string>} */
export async function renderTypes() {
  const result = await openapiTS(SOURCE, { commentHeader: HEADER });
  return result.replace(/\r\n/g, '\n');
}

/** @param {string} openApiPath */
function toMswPath(openApiPath) {
  return `*${openApiPath.replace(/\{([^}]+)\}/g, ':$1')}`;
}

/** @param {Record<string, any>} operation */
function successStatus(operation) {
  return Object.keys(operation.responses)
    .map(Number)
    .filter((status) => status >= 200 && status < 300)
    .sort((a, b) => a - b)[0];
}

/** @returns {Promise<string>} */
export async function renderHandlers() {
  const document = JSON.parse(await readFile(SOURCE, 'utf-8'));
  const lines = [
    HEADER,
    "import { http, HttpResponse } from 'msw';",
    "import { success, acceptedLocation } from './fixtures';",
    '',
    'export const handlers = [',
  ];

  for (const [path, pathItem] of Object.entries(document.paths)) {
    for (const method of METHOD_ORDER) {
      const operation = pathItem[method];
      if (!operation) {
        continue;
      }

      const url = toMswPath(path);
      const status = successStatus(operation);
      const hasContent = Boolean(operation.responses[String(status)]?.content);

      if (hasContent) {
        lines.push(
          `  http.${method}('${url}', () => HttpResponse.json(success.${operation.operationId}, { status: ${status} })),`,
        );
      } else if (status === 202) {
        lines.push(
          `  http.${method}('${url}', () => new HttpResponse(null, { status: 202, headers: { Location: acceptedLocation, 'Content-Length': '0' } })),`,
        );
      } else {
        lines.push(
          `  http.${method}('${url}', () => new HttpResponse(null, { status: ${status} })),`,
        );
      }
    }
  }

  lines.push('];', '');
  return lines.join('\n');
}

/** @param {string | URL} [outFile] */
export async function generateTypes(outFile = DEFAULT_TYPES_OUTPUT) {
  await writeFile(outFile, await renderTypes(), 'utf-8');
}

/** @param {string | URL} [outFile] */
export async function generateHandlers(outFile = DEFAULT_HANDLERS_OUTPUT) {
  await writeFile(outFile, await renderHandlers(), 'utf-8');
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  await generateTypes();
  await generateHandlers();
  console.log('Wrote src/api/types.ts and src/mocks/handlers.ts');
}
