import { readFileSync } from 'node:fs';
import { createServer, request as httpRequest } from 'node:http';
import { createServer as createHttpsServer } from 'node:https';

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
const DOWNLOAD_PAYLOAD_PREFIX = '/subtitle-fixture/';

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

function subtitleSearchResponse(subtitle) {
  return {
    total_count: 1,
    data: [
      {
        attributes: {
          language: subtitle.language,
          download_count: subtitle.downloadCount,
          release: subtitle.release,
          files: [{ file_id: subtitle.fileId, file_name: `${subtitle.release}.srt` }],
        },
      },
    ],
  };
}

function subtitleDownloadLink(subtitle) {
  return `https://${HOST}:${TLS_PORT}${DOWNLOAD_PAYLOAD_PREFIX}${subtitle.fileId}.srt`;
}

async function handleSubtitleProvider(request, response, url, subtitle) {
  if (!subtitle) {
    recordUnexpected(null, null, 'subtitle-provider-not-configured');
    sendJson(response, 500, { status: 500, error: 'Subtitle provider fixture is not configured' });
    return;
  }

  if (url.pathname === SEARCH_PATH) {
    if (request.method !== 'GET') {
      recordSubtitleRequest(request, url, 'search-method-rejected');
      sendJson(response, 404, { status: 404, error: 'Not Found' });
      return;
    }

    const languages = (url.searchParams.get('languages') ?? '')
      .split(',')
      .map((language) => language.trim().toLowerCase())
      .filter(Boolean);
    const query = url.searchParams.get('query');
    const movieHash = url.searchParams.get('moviehash');

    if (!languages.includes(subtitle.language)) {
      recordSubtitleRequest(request, url, 'search-unexpected-language');
      recordUnexpected(null, null, 'subtitle-search-language');
      sendJson(response, 200, { total_count: 0, data: [] });
      return;
    }

    if (movieHash && !query) {
      recordSubtitleRequest(request, url, 'search-hash-empty');
      sendJson(response, 200, { total_count: 0, data: [] });
      return;
    }

    if (query && query.toLowerCase().includes(subtitle.movieTitle.toLowerCase())) {
      recordSubtitleRequest(request, url, 'search-candidates-returned');
      sendJson(response, 200, subtitleSearchResponse(subtitle));
      return;
    }

    recordSubtitleRequest(request, url, 'search-no-candidates');
    sendJson(response, 200, { total_count: 0, data: [] });
    return;
  }

  if (url.pathname === DOWNLOAD_PATH) {
    if (request.method !== 'POST') {
      recordSubtitleRequest(request, url, 'download-method-rejected');
      sendJson(response, 405, { status: 405, error: 'Method Not Allowed' });
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
    sendJson(response, 200, {
      link: subtitleDownloadLink(subtitle),
      file_name: `${subtitle.release}.srt`,
      requests: 1,
      remaining: subtitle.downloadCount,
    });
    return;
  }

  if (url.pathname === `${DOWNLOAD_PAYLOAD_PREFIX}${subtitle.fileId}.srt`) {
    if (request.method !== 'GET') {
      recordSubtitleRequest(request, url, 'download-payload-method-rejected');
      sendJson(response, 405, { status: 405, error: 'Method Not Allowed' });
      return;
    }

    recordSubtitleRequest(request, url, 'download-payload-served');
    response.writeHead(200, {
      'content-type': 'application/x-subrip; charset=utf-8',
      'content-length': Buffer.byteLength(subtitle.srt),
    });
    response.end(subtitle.srt);
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

if (mode === 'serve') {
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
