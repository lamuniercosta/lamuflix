import { execFile } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:net';
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { promisify } from 'node:util';
import { fileURLToPath } from 'node:url';
import { generateFixtures, RESERVED_MOVIE, SEED_MOVIES } from './fixtures.mjs';

const execFileAsync = promisify(execFile);
const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const webDir = path.resolve(scriptDir, '..');
const repoRoot = path.resolve(webDir, '..');
const runtimeDir = path.join(scriptDir, '.runtime');
const evidenceDir = path.join(runtimeDir, 'evidence');
const libraryDir = path.join(runtimeDir, 'library');
const sampleLibraryDir = path.join(repoRoot, 'docker', 'sample-library');
const baseCompose = path.join(repoRoot, 'docker-compose.yml');
const overlayCompose = path.join(scriptDir, 'compose.e2e.yml');
const stubScript = path.join(scriptDir, 'stub.mjs');
const envFile = path.join(runtimeDir, 'compose.env');
const settingsFile = path.join(runtimeDir, 'api.settings.json');
const responsesFile = path.join(runtimeDir, 'stub.responses.json');
const contextFile = path.join(runtimeDir, 'context.json');
const WAIT_BUDGET_MS = 120_000;
const STACK_START_TIMEOUT_MS = 1_800_000;
const TEARDOWN_TIMEOUT_MS = 180_000;
const STUB_SERVICE = 'omdb-stub';
const STUB_PORT = 18080;
const BASE_URL = 'http://localhost:3000';
const DOCKER = process.env.DOCKER ?? 'docker';

const projectName = `lamuflix-e2e-${randomBytes(4).toString('hex')}`;
const secrets = [];
let exitCode = 0;

function sleep(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function log(message) {
  process.stdout.write(`${message}\n`);
}

function composePath(value) {
  return value.replaceAll('\\', '/');
}

function randomSecret(bytes = 24) {
  return randomBytes(bytes).toString('hex');
}

function rememberSecret(value) {
  secrets.push(value);
  return value;
}

function sanitize(text) {
  let result = String(text ?? '');
  for (const secret of secrets) {
    result = result.split(secret).join('[redacted]');
  }
  return result;
}

function envLine(name, value) {
  return `${name}=${value}`;
}

function credentialPair(name, value) {
  return `${name}=${value}`;
}

function writeComposeEnv(credentials) {
  const lines = [
    envLine('POSTGRES_USER', credentials.postgresUser),
    envLine('POSTGRES_PASSWORD', credentials.postgresPassword),
    envLine('RABBITMQ_USER', credentials.rabbitUser),
    envLine('RABBITMQ_PASSWORD', credentials.rabbitPassword),
    envLine('LIBRARY_PATH', composePath(libraryDir)),
    envLine('E2E_SETTINGS_FILE', composePath(settingsFile)),
    envLine('E2E_STUB_SCRIPT', composePath(stubScript)),
    envLine('E2E_STUB_RESPONSES_FILE', composePath(responsesFile)),
    envLine('E2E_STUB_PORT', String(STUB_PORT)),
    envLine('E2E_WAIT_BUDGET_MS', String(WAIT_BUDGET_MS)),
  ];
  writeFileSync(envFile, `${lines.join('\n')}\n`);
}

function providerBaseUrl() {
  return process.env.E2E_OMDB_BASE_URL ?? `http://127.0.0.1:${STUB_PORT}/`;
}

function validateProviderSettings(settings) {
  if (settings.Features?.LocalPlay !== false) {
    throw new Error('generated settings must set Features.LocalPlay to false');
  }

  const parsed = new URL(settings.Omdb.BaseUrl);
  if (parsed.protocol !== 'http:' || parsed.hostname !== '127.0.0.1') {
    throw new Error('Omdb:BaseUrl must be http on the literal host 127.0.0.1');
  }
}

function writeSettings(settings) {
  writeFileSync(settingsFile, `${JSON.stringify(settings, null, 2)}\n`);
}

function buildSettings() {
  const postgresUser = rememberSecret(`lamuflix_${randomSecretsuffix(8)}`);
  const postgresCredential = rememberSecret(randomSecret());
  const rabbitUser = rememberSecret(`lamuflix_${randomSecretsuffix(8)}`);
  const rabbitCredential = rememberSecret(randomSecret());
  const omdbApiKey = rememberSecret(randomSecret(16));

  const settings = {
    Features: { LocalPlay: false },
    Database: { ApplyMigrationsOnStartup: true },
    ConnectionStrings: {
      DefaultConnection: [
        'Host=postgres',
        'Database=lamuflix',
        credentialPair('Username', postgresUser),
        credentialPair('Password', postgresCredential),
      ].join(';'),
    },
    RabbitMq: {
      HostName: 'rabbitmq',
      UserName: rabbitUser,
      Password: rabbitCredential,
      RetryDelay: '00:00:30',
    },
    Omdb: {
      ApiKey: omdbApiKey,
      BaseUrl: providerBaseUrl(),
    },
    Ollama: {
      Endpoint: providerBaseUrl(),
      ModelName: 'all-minilm',
    },
    Enrichment: {
      SweepInterval: '00:01:00',
      ClaimLease: '00:00:10',
    },
    Library: {
      RootPath: '/media/library',
    },
  };

  validateProviderSettings(settings);
  return {
    settings,
    credentials: {
      postgresUser,
      postgresPassword: postgresCredential,
      rabbitUser,
      rabbitPassword: rabbitCredential,
    },
  };
}

function randomSecretsuffix(length) {
  return randomBytes(length).toString('hex').slice(0, length);
}

function foundBody(movie) {
  return {
    Response: 'True',
    Title: movie.title,
    Year: String(movie.year),
    Runtime: `${movie.runtimeMinutes} min`,
    Plot: `${movie.title} smoke fixture.`,
    Rated: 'N/A',
    imdbRating: movie.rating,
    imdbID: movie.imdbId,
    Poster: 'N/A',
    Genre: movie.genre,
    Director: 'Smoke Fixture',
    Writer: 'Smoke Fixture',
    Actors: 'Smoke Fixture',
    Metascore: 'N/A',
  };
}

function writeStubResponses() {
  const responses = [...SEED_MOVIES, RESERVED_MOVIE].map((movie) => ({
    title: movie.title,
    year: movie.year,
    body: foundBody(movie),
  }));
  writeFileSync(responsesFile, `${JSON.stringify({ responses }, null, 2)}\n`);
}

function writeContext() {
  const context = {
    project: projectName,
    composeFiles: [composePath(baseCompose), composePath(overlayCompose)],
    envFile: composePath(envFile),
    stubService: STUB_SERVICE,
    stubPort: STUB_PORT,
    waitBudgetMs: WAIT_BUDGET_MS,
    baseURL: BASE_URL,
    seedTitles: SEED_MOVIES.map((movie) => movie.title),
    reservedMovie: { title: RESERVED_MOVIE.title, year: RESERVED_MOVIE.year },
    reservedFolders: { ...RESERVED_MOVIE.folders },
    runtimeDir: composePath(runtimeDir),
  };
  writeFileSync(contextFile, `${JSON.stringify(context, null, 2)}\n`);
}

function prepare() {
  rmSync(runtimeDir, { recursive: true, force: true });
  mkdirSync(runtimeDir, { recursive: true });

  const { settings, credentials } = buildSettings();
  writeSettings(settings);
  writeComposeEnv(credentials);
  generateFixtures({ sampleLibraryDir, libraryDir });
  writeStubResponses();
  writeContext();
}

function bindFree(port, host) {
  return new Promise((resolve) => {
    const server = createServer();
    server.once('error', (error) => {
      resolve(error.code === 'EAFNOSUPPORT' || error.code === 'EADDRNOTAVAIL');
    });
    server.listen(port, host, () => {
      server.close(() => resolve(true));
    });
  });
}

async function assertPortFree() {
  const hosts = ['127.0.0.1', '0.0.0.0', '::1'];
  for (const host of hosts) {
    if (!(await bindFree(3000, host))) {
      throw new Error(`localhost:3000 is already in use (${host})`);
    }
  }
}

function composeArgs(args) {
  return [
    'compose',
    '-p',
    projectName,
    '-f',
    baseCompose,
    '-f',
    overlayCompose,
    '--env-file',
    envFile,
    ...args,
  ];
}

async function compose(args, { timeout = WAIT_BUDGET_MS, allowFailure = false } = {}) {
  try {
    const result = await execFileAsync(DOCKER, composeArgs(args), {
      cwd: repoRoot,
      timeout,
      maxBuffer: 20 * 1024 * 1024,
      windowsHide: true,
    });
    return { code: 0, stdout: result.stdout ?? '', stderr: result.stderr ?? '' };
  } catch (error) {
    const result = {
      code: typeof error.code === 'number' ? error.code : 1,
      stdout: error.stdout ?? '',
      stderr: error.stderr ?? error.message ?? '',
    };
    if (allowFailure) {
      return result;
    }

    throw new Error(`docker compose ${args[0]} failed: ${sanitize(result.stderr)}`, {
      cause: error,
    });
  }
}

async function stubControl(action, args = [], { allowFailure = false } = {}) {
  const result = await compose(
    ['exec', '-T', STUB_SERVICE, 'node', '/e2e/stub.mjs', 'control', action, ...args],
    { allowFailure: true, timeout: WAIT_BUDGET_MS },
  );

  if (result.code !== 0 && !allowFailure) {
    throw new Error(`stub control ${action} failed: ${sanitize(result.stderr || result.stdout)}`);
  }

  return result;
}

async function waitForStub() {
  const deadline = Date.now() + WAIT_BUDGET_MS;
  while (Date.now() < deadline) {
    const result = await stubControl('health', [], { allowFailure: true });
    if (result.code === 0) {
      return;
    }
    await sleep(500);
  }

  throw new Error('stub readiness did not arrive within the wait budget');
}

async function stubStatus() {
  const result = await stubControl('status');
  return JSON.parse(result.stdout.trim());
}

async function assertNoUnexpectedProviderRequests() {
  const status = await stubStatus();
  if (Array.isArray(status.unexpectedRequests) && status.unexpectedRequests.length > 0) {
    throw new Error(`unexpected provider requests: ${JSON.stringify(status.unexpectedRequests)}`);
  }
}

async function readJson(url, options = {}, timeoutMs = WAIT_BUDGET_MS) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), Math.max(timeoutMs, 1));
  try {
    const response = await fetch(url, { ...options, signal: controller.signal });
    if (!response.ok) {
      throw new Error(`${options?.method ?? 'GET'} ${url} returned ${response.status}`);
    }
    return await response.json();
  } finally {
    clearTimeout(timer);
  }
}

async function seedMovies() {
  for (const movie of SEED_MOVIES) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), WAIT_BUDGET_MS);
    try {
      const response = await fetch(`${BASE_URL}/api/movies/import`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ folderPath: movie.relativePath }),
        signal: controller.signal,
      });

      if (response.status !== 202) {
        throw new Error(`seed import for ${movie.title} returned ${response.status}`);
      }

      await response.body?.cancel().catch(() => {});
    } finally {
      clearTimeout(timer);
    }
  }
}

async function waitForSeededState() {
  const expectedGenres = SEED_MOVIES.flatMap((movie) =>
    movie.genre.split(',').map((name) => name.trim()),
  );
  const deadline = Date.now() + WAIT_BUDGET_MS;

  while (Date.now() < deadline) {
    const remaining = deadline - Date.now();
    const [browse, genres] = await Promise.all([
      readJson(
        `${BASE_URL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
        {},
        remaining,
      ),
      readJson(`${BASE_URL}/api/genres`, {}, remaining),
    ]);
    const genreNames = new Set(genres.map((genre) => genre.name));
    const moviesEnriched = SEED_MOVIES.every((seed) =>
      browse.items.some(
        (movie) => movie.title === seed.title && movie.enrichmentStatus === 'Enriched',
      ),
    );
    const genresPresent = expectedGenres.every((name) => genreNames.has(name));

    if (moviesEnriched && genresPresent) {
      return;
    }

    await sleep(500);
  }

  throw new Error('seeded movies and genres were not observable within the wait budget');
}

function hasSpecs() {
  return readdirSync(scriptDir).some((name) => name.endsWith('.e2e.ts'));
}

async function runPlaywright() {
  if (!hasSpecs()) {
    log('No smoke specs found; verified stack readiness and seeding only.');
    return 0;
  }

  try {
    const result = await execFileAsync(
      process.execPath,
      [
        path.join(webDir, 'node_modules', '@playwright', 'test', 'cli.js'),
        'test',
        '--config',
        path.join(webDir, 'playwright.config.ts'),
      ],
      {
        cwd: webDir,
        env: {
          ...process.env,
          E2E_CONTEXT_FILE: contextFile,
          E2E_WAIT_BUDGET_MS: String(WAIT_BUDGET_MS),
        },
        timeout: STACK_START_TIMEOUT_MS,
        maxBuffer: 20 * 1024 * 1024,
        windowsHide: true,
      },
    );
    process.stdout.write(result.stdout ?? '');
    process.stderr.write(result.stderr ?? '');
    return 0;
  } catch (error) {
    process.stdout.write(error.stdout ?? '');
    process.stderr.write(error.stderr ?? '');
    return typeof error.code === 'number' ? error.code : 1;
  }
}

async function collectEvidence() {
  mkdirSync(evidenceDir, { recursive: true });
  const logs = await compose(['logs', '--no-color'], {
    allowFailure: true,
    timeout: TEARDOWN_TIMEOUT_MS,
  });
  writeFileSync(path.join(evidenceDir, 'compose.log'), sanitize(`${logs.stdout}${logs.stderr}`));

  const stubLogs = await compose(['logs', '--no-color', STUB_SERVICE], {
    allowFailure: true,
    timeout: TEARDOWN_TIMEOUT_MS,
  });
  writeFileSync(
    path.join(evidenceDir, 'stub.log'),
    sanitize(`${stubLogs.stdout}${stubLogs.stderr}`),
  );
}

function testResultFiles(directory) {
  if (!existsSync(directory)) {
    return [];
  }

  const files = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...testResultFiles(full));
    } else {
      files.push(full);
    }
  }
  return files;
}

function hasFailureEvidence() {
  const resultsDir = path.join(runtimeDir, 'test-results');
  return testResultFiles(resultsDir).some((file) => file.endsWith('.png'));
}

function buildUploadAllowlist() {
  const files = [];
  if (existsSync(evidenceDir)) {
    files.push(...testResultFiles(evidenceDir).filter((file) => file.endsWith('.log')));
  }

  const resultsDir = path.join(runtimeDir, 'test-results');
  files.push(...testResultFiles(resultsDir).filter((file) => file.endsWith('.png')));

  const allowlist = files.map((file) => composePath(path.relative(runtimeDir, file)));
  writeFileSync(
    path.join(runtimeDir, 'upload-allowlist.json'),
    `${JSON.stringify({ files: allowlist }, null, 2)}\n`,
  );
  return files;
}

function scanEvidenceForSecrets() {
  const files = buildUploadAllowlist();
  for (const file of files) {
    if (!file.endsWith('.log')) {
      continue;
    }

    const text = readFileSync(file, 'utf8');
    if (secrets.some((secret) => text.includes(secret))) {
      writeFileSync(
        path.join(runtimeDir, 'upload-blocked.txt'),
        `unsafe evidence file: ${path.basename(file)}\n`,
      );
      return false;
    }
  }

  return true;
}

async function teardown() {
  await stubControl('release-all', [], { allowFailure: true });
  await compose(['down', '-v', '--remove-orphans'], {
    allowFailure: true,
    timeout: TEARDOWN_TIMEOUT_MS,
  });
}

async function main() {
  try {
    prepare();
    await assertPortFree();
    await compose(
      [
        'up',
        '-d',
        '--wait',
        '--wait-timeout',
        String(Math.ceil(WAIT_BUDGET_MS / 1000)),
        'postgres',
        'rabbitmq',
        'api',
        'web',
        'omdb-stub',
      ],
      { timeout: STACK_START_TIMEOUT_MS },
    );
    await waitForStub();
    await assertNoUnexpectedProviderRequests();
    await seedMovies();
    await waitForSeededState();
    await assertNoUnexpectedProviderRequests();
    exitCode = await runPlaywright();
    await assertNoUnexpectedProviderRequests();
  } catch (error) {
    exitCode = 1;
    process.stderr.write(`e2e failed: ${sanitize(error.message)}\n`);
  } finally {
    if (exitCode !== 0 || hasFailureEvidence()) {
      await collectEvidence().catch((evidenceError) => {
        process.stderr.write(`evidence collection failed: ${sanitize(evidenceError.message)}\n`);
      });
    }
    await teardown();
  }

  if (!scanEvidenceForSecrets()) {
    process.stderr.write('Upload blocked: generated secret value found in evidence.\n');
    exitCode = 1;
  }

  process.exitCode = exitCode;
}

main().catch((error) => {
  process.stderr.write(`e2e failed: ${sanitize(error.message)}\n`);
  process.exitCode = 1;
});
