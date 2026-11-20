import { readFileSync } from 'node:fs';
import { createServer, request as httpRequest } from 'node:http';
import { createServer as createHttpsServer } from 'node:https';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const HOST = '127.0.0.1';
const PORT = Number(process.env.E2E_STUB_PORT ?? '18080');
const TLS_PORT = Number(process.env.E2E_STUB_TLS_PORT ?? '18443');
const WAIT_BUDGET_MS = Number(process.env.E2E_WAIT_BUDGET_MS ?? '120000');
const RESPONSES_FILE = process.env.E2E_STUB_RESPONSES_FILE;
const TLS_CERT_FILE = process.env.E2E_STUB_TLS_CERT_FILE;
const TLS_KEY_FILE = process.env.E2E_STUB_TLS_KEY_FILE;
const CONTROL_PREFIX = '/__e2e/';
const SEARCH_PATH = '/api/v1/subtitles';
const DOWNLOAD_PATH = '/api/v1/download';
const SUBTITLE_DOWNLOAD_PREFIX = '/download/';
const SUBTITLE_RESET_AFTER_MS = (17 * 60 + 9) * 60 * 1000;
const SUBTITLE_API_KEY = process.env.E2E_STUB_SUBTITLE_API_KEY ?? '';
const LINK_HOST = 'localhost';
const API_RATE_LIMIT_HEADERS = {
  'RateLimit-Limit': '5',
  'RateLimit-Remaining': '4',
  'RateLimit-Reset': '1',
  'X-RateLimit-Limit-Second': '5',
  'X-RateLimit-Remaining-Second': '4',
};
const IS_MAIN =
  process.argv[1] !== undefined && import.meta.url === pathToFileURL(resolve(process.argv[1])).href;

const arms = [];
const held = new Map();
const unexpectedRequests = [];
const subtitleProviderRequests = [];
let nextHoldId = 1;

function now() {
  return new Date().toISOString();
}

function log(event, details = {}) {
  process.stdout.write(`${JSON.stringify({ event, at: now(), ...details })}\n`);
}

function same(left, right) {
  return String(left ?? '').toLowerCase() === String(right ?? '').toLowerCase();
}

function sleep(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function loadFixture() {
  if (!RESPONSES_FILE) {
    throw new Error('E2E_STUB_RESPONSES_FILE is required');
  }

  const parsed = JSON.parse(readFileSync(RESPONSES_FILE, 'utf8'));
  if (!Array.isArray(parsed.responses)) {
    throw new Error('stub responses file must contain a responses array');
  }

  return { responses: parsed.responses, subtitleProvider: parsed.subtitleProvider ?? null };
}

function foundBody(title, year) {
  return {
    Response: 'True',
    Title: title,
    Type: 'movie',
    Year: String(year ?? ''),
    Runtime: '90 min',
    Plot: `${title} reserved import fixture.`,
    Rated: 'N/A',
    imdbRating: '7.0',
    imdbID: 'tt2010001',
    Poster: 'N/A',
    Genre: 'Animation',
    Director: 'Smoke Fixture',
    Writer: 'Smoke Fixture',
    Actors: 'Smoke Fixture',
    Metascore: 'N/A',
  };
}

function responseFor(responses, title, year) {
  return responses.find((entry) => same(entry.title, title))?.body ?? foundBody(title, year);
}

function sendJson(response, statusCode, payload) {
  if (response.writableEnded || response.destroyed) {
    return;
  }

  const body = JSON.stringify(payload);
  response.writeHead(statusCode, {
    'content-type': 'application/json',
    'content-length': Buffer.byteLength(body),
  });
  response.end(body);
}

function readBody(request) {
  return new Promise((resolve, reject) => {
    let body = '';
    request.setEncoding('utf8');
    request.on('data', (chunk) => {
      body += chunk;
    });
    request.on('end', () => {
      if (body.length === 0) {
        resolve({});
        return;
      }

      try {
        resolve(JSON.parse(body));
      } catch (error) {
        reject(error);
      }
    });
    request.on('error', reject);
  });
}

function recordUnexpected(title, year, reason) {
  unexpectedRequests.push({ title: title ?? null, year: year ?? null, reason, at: now() });
  log('unexpected-provider-request', { title: title ?? null, year: year ?? null, reason });
}

function rejectUnexpected(response, title, year, reason) {
  recordUnexpected(title, year, reason);
  sendJson(response, 500, { Response: 'False', Error: 'Unexpected provider request' });
}

function hold(response, title, year) {
  const id = nextHoldId;
  nextHoldId += 1;
  held.set(id, { id, title, year, response, released: false });
  response.on('close', () => {
    held.delete(id);
  });
  log('provider-request-held', { title, year });
}

function clearArms(title) {
  for (let index = arms.length - 1; index >= 0; index -= 1) {
    if (!title || same(arms[index].title, title)) {
      const [removed] = arms.splice(index, 1);
      log('provider-arm-cleared', { title: removed.title, year: removed.year });
    }
  }
}

function releaseHeld(responses, title) {
  let released = 0;
  for (const record of held.values()) {
    if (title && !same(record.title, title)) {
      continue;
    }

    if (record.released) {
      continue;
    }

    record.released = true;
    released += 1;
    sendJson(record.response, 200, responseFor(responses, record.title, record.year));
    log('provider-response-released', { title: record.title, year: record.year });
  }

  return released;
}

async function drainHolds() {
  const deadline = Date.now() + WAIT_BUDGET_MS;
  while (held.size > 0 && Date.now() < deadline) {
    await sleep(25);
  }

  if (held.size > 0) {
    throw new Error('held provider requests did not drain within the wait budget');
  }
}

async function handleProvider(request, response, url, responses) {
  if (url.pathname !== '/' && url.pathname !== '') {
    rejectUnexpected(response, null, null, 'request-path');
    return;
  }

  const title = url.searchParams.get('t');
  const year = url.searchParams.get('y');
  const imdbId = url.searchParams.get('i');
  if (imdbId) {
    const known = responses.find((entry) => entry.manual && entry.body.imdbID === imdbId);
    if (!known) {
      rejectUnexpected(response, null, year, 'unknown-imdb-id');
      return;
    }
    sendJson(response, 200, known.body);
    return;
  }

  const search = url.searchParams.get('s');
  const manual = responses.find((entry) => entry.manual && same(entry.title, title ?? search));
  if (manual) {
    sendJson(response, 200, { Response: 'False', Error: 'Movie not found!' });
    return;
  }

  if (!title) {
    rejectUnexpected(response, null, year, 'missing-title');
    return;
  }

  const armIndex = arms.findIndex((arm) => same(arm.title, title));
  if (armIndex >= 0) {
    arms.splice(armIndex, 1);
    hold(response, title, year);
    return;
  }

  const known = responses.find((entry) => same(entry.title, title));
  if (!known) {
    rejectUnexpected(response, title, year, 'unknown-title');
    return;
  }

  sendJson(response, 200, known.body);
}

function embedVector() {
  return Array.from({ length: 384 }, () => 0.5);
}

async function handleEmbed(request, response) {
  const body = await readBody(request);
  const inputs = Array.isArray(body.input) ? body.input : [body.input];
  sendJson(response, 200, {
    model: typeof body.model === 'string' ? body.model : 'all-minilm',
    embeddings: inputs.map(() => embedVector()),
  });
}

async function handleControl(request, response, url, fixture) {
  const responses = fixture.responses;
  const action = url.pathname.slice(CONTROL_PREFIX.length);

  if (request.method === 'GET' && action === 'health') {
    sendJson(response, 200, { ready: true });
    return;
  }

  if (request.method === 'GET' && action === 'status') {
    sendJson(response, 200, {
      ready: true,
      armed: arms,
      held: [...held.values()].map((record) => ({
        id: record.id,
        title: record.title,
        year: record.year,
      })),
      unexpectedRequests,
      subtitleProviderRequests,
    });
    return;
  }

  if (request.method === 'POST' && action === 'arm') {
    const body = await readBody(request);
    if (typeof body.title !== 'string' || body.title.length === 0) {
      throw new Error('arm requires a title');
    }

    if (arms.some((arm) => same(arm.title, body.title))) {
      sendJson(response, 409, { error: 'title is already armed' });
      return;
    }

    arms.push({ title: body.title, year: body.year ?? null });
    log('provider-arm', { title: body.title, year: body.year ?? null });
    sendJson(response, 200, { armed: { title: body.title, year: body.year ?? null } });
    return;
  }

  if (request.method === 'POST' && action === 'release') {
    const body = await readBody(request);
    const released = releaseHeld(responses, body.title);
    clearArms(body.title);
    await drainHolds();
    sendJson(response, 200, { released, remaining: held.size });
    return;
  }

  if (request.method === 'POST' && action === 'release-all') {
    const released = releaseHeld(responses);
    clearArms();
    await drainHolds();
    sendJson(response, 200, { released, remaining: held.size });
    return;
  }

  if (request.method === 'POST' && action === 'drain') {
    await drainHolds();
    sendJson(response, 200, { remaining: held.size });
    return;
  }

  sendJson(response, 404, { error: 'unknown control action' });
}

function recordSubtitleRequest(request, url, outcome) {
  const entry = {
    method: request.method,
    path: url.pathname,
    query: Object.fromEntries(url.searchParams.entries()),
    outcome,
    at: now(),
  };
  subtitleProviderRequests.push(entry);
  log('subtitle-provider-request', entry);
  return entry;
}

// OpenSubtitles emulation. Shapes and rules follow specs/DEV-525/provider-contract.md.

// Lowercases ASCII letters outside %XX escapes. Escapes stay byte-for-byte, which keeps
// observed rows such as "%3A" and "%2B" unredirected (contract section 2).
const PERCENT_ESCAPE_OR_LETTERS = /%[0-9A-Fa-f]{2}|[A-Za-z]+/g;

function isPureAscii(text) {
  return [...text].every((character) => character.codePointAt(0) < 0x80);
}

function percentDecode(value) {
  try {
    return decodeURIComponent(value);
  } catch {
    return null;
  }
}

function lowerOutsideEscapes(value) {
  return value.replace(PERCENT_ESCAPE_OR_LETTERS, (match) =>
    match.startsWith('%') ? match : match.toLowerCase(),
  );
}

function canonicalValue(value) {
  const decoded = percentDecode(value);
  const lowered = decoded !== null && isPureAscii(decoded) ? lowerOutsideEscapes(value) : value;
  return lowered.replaceAll('%20', '+');
}

function compareNames(left, right) {
  if (left.name === right.name) {
    return 0;
  }
  return left.name < right.name ? -1 : 1;
}

// Canonical form of a raw query string (the text after "?" in req.url).
export function canonicalSubtitleQuery(rawQuery) {
  return rawQuery
    .split('&')
    .filter((pair) => pair.length > 0)
    .map((pair) => {
      const equals = pair.indexOf('=');
      const name = (equals < 0 ? pair : pair.slice(0, equals)).toLowerCase();
      const text = equals < 0 ? name : `${name}=${canonicalValue(pair.slice(equals + 1))}`;
      return { name, text };
    })
    .sort(compareNames)
    .map((entry) => entry.text)
    .join('&');
}

function rawQueryOf(requestUrl) {
  const index = (requestUrl ?? '').indexOf('?');
  return index < 0 ? '' : requestUrl.slice(index + 1);
}

function sendApi(response, statusCode, payload) {
  if (response.writableEnded || response.destroyed) {
    return;
  }

  const body = JSON.stringify(payload);
  response.writeHead(statusCode, {
    'content-type': 'application/json',
    'content-length': Buffer.byteLength(body),
    ...API_RATE_LIMIT_HEADERS,
  });
  response.end(body);
}

function sendCanonicalRedirect(response, canonical) {
  const body =
    '<!DOCTYPE html>\n<html><head><title>301 Moved Permanently</title></head>' +
    '<body><h1>301 Moved Permanently</h1></body></html>\n';
  response.writeHead(301, {
    Location: `${SEARCH_PATH}?${canonical}`,
    'X-OS-Rule': 'canonical',
    'X-Canonical': '1',
    'content-type': 'text/html',
    'content-length': Buffer.byteLength(body),
  });
  response.end(body);
}

// The real /download answers 503 with this HTML page when the request lacks Accept: application/json.
function sendServiceUnavailablePage(response) {
  const body =
    '<!DOCTYPE html>\n<html><head><title>Error 503 - OpenSubtitles.com</title></head>' +
    '<body><h1>Error 503</h1></body></html>\n';
  response.writeHead(503, {
    server: 'cloudflare',
    'content-type': 'text/html',
    'content-length': Buffer.byteLength(body),
  });
  response.end(body);
}

function acceptsJson(request) {
  return (request.headers.accept ?? '').toLowerCase().includes('application/json');
}

// The key itself is never logged; only the outcome label and the reason are recorded.
function apiAuthFailure(request) {
  if (!request.headers['user-agent']) {
    return {
      reason: 'subtitle-user-agent-missing',
      outcome: 'auth-user-agent-missing',
      body: { error: 'User agent required' },
    };
  }

  if (SUBTITLE_API_KEY.length === 0 || request.headers['api-key'] !== SUBTITLE_API_KEY) {
    return {
      reason: 'subtitle-api-key-rejected',
      outcome: 'auth-api-key-rejected',
      body: { message: 'You cannot consume this service' },
    };
  }

  return null;
}

function yearFrom(release) {
  const match = /\b(?:19|20)\d{2}\b/.exec(release);
  return match ? Number(match[0]) : null;
}

function searchEntry(entry) {
  const fileId = entry.fileId;
  return {
    id: String(fileId),
    type: 'subtitle',
    attributes: {
      subtitle_id: String(fileId),
      language: entry.language,
      download_count: entry.downloadCount,
      hd: entry.hd,
      fps: entry.fps,
      release: entry.release,
      feature_details: {
        feature_type: entry.featureType,
        title: entry.title,
        year: yearFrom(entry.release),
      },
      url: `https://www.opensubtitles.com/en/subtitles/${fileId}`,
      files: [{ file_id: fileId, cd_number: 1, file_name: `${entry.release}.srt` }],
    },
  };
}

// The Movie entry keeps the release the E2E spec expects. The Episode entry is a decoy
// that the real title search also returns.
function searchEntries(subtitle) {
  return [
    searchEntry({
      fileId: subtitle.fileId,
      language: subtitle.language,
      downloadCount: subtitle.downloadCount,
      hd: true,
      fps: 23.976,
      release: subtitle.release,
      featureType: 'Movie',
      title: subtitle.movieTitle,
    }),
    searchEntry({
      fileId: subtitle.fileId + 1,
      language: subtitle.language,
      downloadCount: 77,
      hd: false,
      fps: 25,
      release: `${subtitle.movieTitle} S01E01 720p DEV525-EPISODE-DECOY`,
      featureType: 'Episode',
      title: subtitle.movieTitle,
    }),
  ];
}

function emptySearchBody() {
  return { total_pages: 0, total_count: 0, per_page: 50, page: 1, data: [] };
}

function subtitleSearchBody(subtitle, type) {
  const entries = searchEntries(subtitle).filter(
    (entry) => type !== 'movie' || entry.attributes.feature_details.feature_type === 'Movie',
  );
  return {
    total_pages: entries.length > 0 ? 1 : 0,
    total_count: entries.length,
    per_page: 50,
    page: 1,
    data: entries,
  };
}

function subtitleDownloadToken(subtitle) {
  return `e2e-token-${subtitle.fileId}`;
}

function subtitleDownloadName(subtitle) {
  const slug = subtitle.release
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return `${slug}.srt`;
}

function subtitleLinkPath(subtitle) {
  return `${SUBTITLE_DOWNLOAD_PREFIX}${subtitleDownloadToken(subtitle)}/subfile/${subtitleDownloadName(subtitle)}`;
}

// The link lives on another authority (localhost) than the API BaseUrl (127.0.0.1),
// matching the real www host, and the adapter therefore sends no Api-Key to it.
function subtitleDownloadLink(subtitle) {
  return `https://${LINK_HOST}:${TLS_PORT}${subtitleLinkPath(subtitle)}`;
}

function subtitleDownloadBody(subtitle) {
  return {
    link: subtitleDownloadLink(subtitle),
    file_name: `${subtitle.release}.srt`,
    requests: 1,
    remaining: 99,
    message: 'You have 99 downloads remaining of your 100 daily allowance.',
    reset_time: '17 hours and 9 minutes',
    reset_time_utc: new Date(Date.now() + SUBTITLE_RESET_AFTER_MS).toISOString(),
    uk: 'x',
    uid: 1,
    ts: 1,
  };
}

function handleSubtitleSearch(request, response, url, subtitle) {
  const languages = (url.searchParams.get('languages') ?? '')
    .split(',')
    .map((language) => language.trim().toLowerCase())
    .filter(Boolean);
  const query = url.searchParams.get('query');
  const movieHash = url.searchParams.get('moviehash');

  if (!languages.includes(subtitle.language)) {
    recordSubtitleRequest(request, url, 'search-unexpected-language');
    recordUnexpected(null, null, 'subtitle-search-language');
    sendApi(response, 200, emptySearchBody());
    return;
  }

  if (movieHash && !query) {
    recordSubtitleRequest(request, url, 'search-hash-empty');
    sendApi(response, 200, emptySearchBody());
    return;
  }

  if (query && query.toLowerCase().includes(subtitle.movieTitle.toLowerCase())) {
    recordSubtitleRequest(request, url, 'search-candidates-returned');
    sendApi(response, 200, subtitleSearchBody(subtitle, url.searchParams.get('type')));
    return;
  }

  recordSubtitleRequest(request, url, 'search-no-candidates');
  sendApi(response, 200, emptySearchBody());
}

async function handleSubtitleDownload(request, response, url, subtitle) {
  if (request.method !== 'POST') {
    recordSubtitleRequest(request, url, 'download-method-rejected');
    sendJson(response, 405, { status: 405, error: 'Method Not Allowed' });
    return;
  }

  if (!acceptsJson(request)) {
    recordSubtitleRequest(request, url, 'download-missing-json-accept');
    recordUnexpected(null, null, 'subtitle-download-accept');
    sendServiceUnavailablePage(response);
    return;
  }

  const body = await readBody(request);
  if (Number(body.file_id) !== subtitle.fileId) {
    recordSubtitleRequest(request, url, 'download-unknown-file-id');
    recordUnexpected(null, null, 'subtitle-download-file-id');
    sendJson(response, 406, { message: 'Invalid file_id' });
    return;
  }

  recordSubtitleRequest(request, url, 'download-accepted');
  sendApi(response, 200, subtitleDownloadBody(subtitle));
}

function handleSubtitleLink(request, response, url, subtitle) {
  if (request.method !== 'GET') {
    recordSubtitleRequest(request, url, 'download-payload-method-rejected');
    sendJson(response, 405, { status: 405, error: 'Method Not Allowed' });
    return;
  }

  recordSubtitleRequest(request, url, 'download-payload-served');
  // write() before end() so no Content-Length is set, as on the real link.
  response.writeHead(200, {
    'content-type': 'text/plain',
    'content-disposition': 'attachment',
  });
  response.write(subtitle.srt);
  response.end();
}

async function handleSubtitleProvider(request, response, url, subtitle) {
  if (!subtitle) {
    recordUnexpected(null, null, 'subtitle-provider-not-configured');
    sendJson(response, 500, { status: 500, error: 'Subtitle provider fixture is not configured' });
    return;
  }

  if (url.pathname === subtitleLinkPath(subtitle)) {
    handleSubtitleLink(request, response, url, subtitle);
    return;
  }

  const apiPath =
    url.pathname.length > 1 && url.pathname.endsWith('/')
      ? url.pathname.slice(0, -1)
      : url.pathname;

  if (apiPath === SEARCH_PATH && request.method === 'GET') {
    const rawQuery = rawQueryOf(request.url);
    const canonical = canonicalSubtitleQuery(rawQuery);
    if (canonical !== rawQuery) {
      recordSubtitleRequest(request, url, 'search-canonical-redirect');
      sendCanonicalRedirect(response, canonical);
      return;
    }
  }

  if (apiPath === SEARCH_PATH || apiPath === DOWNLOAD_PATH) {
    const failure = apiAuthFailure(request);
    if (failure) {
      recordSubtitleRequest(request, url, failure.outcome);
      recordUnexpected(null, null, failure.reason);
      sendJson(response, 403, failure.body);
      return;
    }
  }

  if (apiPath === SEARCH_PATH) {
    if (request.method !== 'GET') {
      recordSubtitleRequest(request, url, 'search-method-rejected');
      sendApi(response, 404, { status: 404, error: 'Not Found' });
      return;
    }

    handleSubtitleSearch(request, response, url, subtitle);
    return;
  }

  if (apiPath === DOWNLOAD_PATH) {
    await handleSubtitleDownload(request, response, url, subtitle);
    return;
  }

  recordSubtitleRequest(request, url, 'unexpected-request');
  recordUnexpected(null, null, 'subtitle-request-path');
  sendJson(response, 404, { status: 404, error: 'Not Found' });
}

function serve() {
  const fixture = loadFixture();
  const server = createServer((request, response) => {
    const url = new URL(request.url ?? '/', `http://${HOST}:${PORT}`);
    const handler = url.pathname.startsWith(CONTROL_PREFIX)
      ? handleControl(request, response, url, fixture)
      : url.pathname === '/api/embed' && request.method === 'POST'
        ? handleEmbed(request, response)
        : handleProvider(request, response, url, fixture.responses);

    Promise.resolve(handler).catch((error) => {
      log('request-error', { message: error.message });
      sendJson(response, 500, { error: error.message });
    });
  });

  server.requestTimeout = 0;
  server.headersTimeout = 0;
  server.listen(PORT, HOST, () => {
    log('stub-listening', { host: HOST, port: PORT });
  });

  const tlsServer = serveSubtitleProvider(fixture.subtitleProvider);

  const stop = () => {
    tlsServer?.close();
    server.close(() => process.exit(0));
  };
  process.on('SIGTERM', stop);
  process.on('SIGINT', stop);
}

function serveSubtitleProvider(subtitle) {
  if (!subtitle || !TLS_CERT_FILE || !TLS_KEY_FILE) {
    return null;
  }

  const server = createHttpsServer(
    { cert: readFileSync(TLS_CERT_FILE), key: readFileSync(TLS_KEY_FILE) },
    (request, response) => {
      const url = new URL(request.url ?? '/', `https://${HOST}:${TLS_PORT}`);
      Promise.resolve(handleSubtitleProvider(request, response, url, subtitle)).catch((error) => {
        log('subtitle-request-error', { message: error.message });
        sendJson(response, 500, { error: error.message });
      });
    },
  );

  server.requestTimeout = 0;
  server.headersTimeout = 0;
  server.listen(TLS_PORT, HOST, () => {
    log('subtitle-provider-listening', { host: HOST, port: TLS_PORT });
  });
  return server;
}

function controlRequest(action, payload) {
  const method = action === 'health' || action === 'status' ? 'GET' : 'POST';
  const body = method === 'POST' ? JSON.stringify(payload ?? {}) : undefined;

  return new Promise((resolve, reject) => {
    const request = httpRequest(
      {
        host: HOST,
        port: PORT,
        path: `${CONTROL_PREFIX}${action}`,
        method,
        timeout: WAIT_BUDGET_MS,
        headers: body
          ? { 'content-type': 'application/json', 'content-length': Buffer.byteLength(body) }
          : undefined,
      },
      (response) => {
        let text = '';
        response.setEncoding('utf8');
        response.on('data', (chunk) => {
          text += chunk;
        });
        response.on('end', () => {
          resolve({ statusCode: response.statusCode ?? 0, text });
        });
      },
    );

    request.on('timeout', () => request.destroy(new Error('control request timed out')));
    request.on('error', reject);
    if (body) {
      request.write(body);
    }
    request.end();
  });
}

async function control(action, args) {
  const payload = {};
  if (action === 'arm') {
    payload.title = args[0];
    payload.year = args[1] ?? null;
  }
  if (action === 'release' && args[0]) {
    payload.title = args[0];
  }

  const result = await controlRequest(action, payload);
  process.stdout.write(`${result.text}\n`);
  if (result.statusCode >= 400) {
    process.exitCode = 1;
  }
}

const mode = process.argv[2] ?? 'serve';

if (!IS_MAIN) {
  // Imported by subtitle-canonical.selfcheck.mjs: expose helpers only, start nothing.
} else if (mode === 'serve') {
  serve();
} else if (mode === 'control') {
  const action = process.argv[3] ?? 'status';
  control(action, process.argv.slice(4)).catch((error) => {
    process.stderr.write(`${error.message}\n`);
    process.exitCode = 1;
  });
} else if (mode === 'health') {
  controlRequest('health')
    .then((result) => {
      process.exitCode = result.statusCode === 200 ? 0 : 1;
    })
    .catch(() => {
      process.exitCode = 1;
    });
} else {
  process.stderr.write(`unknown stub mode: ${mode}\n`);
  process.exitCode = 2;
}
