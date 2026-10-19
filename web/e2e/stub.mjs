import { readFileSync } from 'node:fs';
import { createServer, request as httpRequest } from 'node:http';

const HOST = '127.0.0.1';
const PORT = Number(process.env.E2E_STUB_PORT ?? '18080');
const WAIT_BUDGET_MS = Number(process.env.E2E_WAIT_BUDGET_MS ?? '120000');
const RESPONSES_FILE = process.env.E2E_STUB_RESPONSES_FILE;
const CONTROL_PREFIX = '/__e2e/';

const arms = [];
const held = new Map();
const unexpectedRequests = [];
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

function loadResponses() {
  if (!RESPONSES_FILE) {
    throw new Error('E2E_STUB_RESPONSES_FILE is required');
  }

  const parsed = JSON.parse(readFileSync(RESPONSES_FILE, 'utf8'));
  if (!Array.isArray(parsed.responses)) {
    throw new Error('stub responses file must contain a responses array');
  }

  return parsed.responses;
}

function foundBody(title, year) {
  return {
    Response: 'True',
    Title: title,
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

async function handleControl(request, response, url, responses) {
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
    await drainHolds();
    sendJson(response, 200, { released, remaining: held.size });
    return;
  }

  if (request.method === 'POST' && action === 'release-all') {
    const released = releaseHeld(responses);
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

function serve() {
  const responses = loadResponses();
  const server = createServer((request, response) => {
    const url = new URL(request.url ?? '/', `http://${HOST}:${PORT}`);
    const handler = url.pathname.startsWith(CONTROL_PREFIX)
      ? handleControl(request, response, url, responses)
      : handleProvider(request, response, url, responses);

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

  const stop = () => {
    server.close(() => process.exit(0));
  };
  process.on('SIGTERM', stop);
  process.on('SIGINT', stop);
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
