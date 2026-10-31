import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:3000').replace(/\/+$/, '');
const BROWSE_TOTAL_COUNT = 500;
const SEARCH_TOTAL_COUNT = 50;
const PAGE_SIZE = 20;
const BROWSE_PAGE_COUNT = 25;
const GROUP_COUNT = 10;
const BROWSE_REQUESTS_PER_CYCLE = 4;
const PROBE_CASES = ['Group', 'group', 'GROUP', 'gRoUp'];
const SEED_TITLE = /^DEV427 Group(\d{2}) Movie(\d{4})$/;
const MEASUREMENT_VUS = 50;
const MEASUREMENT_DURATION_SECONDS = 120;
const PREFLIGHT_ONLY = __ENV.PREFLIGHT_ONLY === '1';
const SUMMARY_DIR = (__ENV.SUMMARY_DIR || '/artifacts/load').replace(/\/+$/, '');
const RUN_ID = __ENV.RUN_ID || '';
const RUN_UTC = __ENV.RUN_UTC || '';
const GIT_HEAD = __ENV.GIT_HEAD || '';
const K6_IMAGE = __ENV.K6_IMAGE || '';

const REQUEST_ERRORS = new Rate('request_errors');
const BROWSE_ERRORS = new Rate('browse_errors');
const SEARCH_ERRORS = new Rate('search_errors');
const MEASURED_DURATION = new Trend('measured_duration', true);
const BROWSE_DURATION = new Trend('browse_duration', true);
const SEARCH_DURATION = new Trend('search_duration', true);
const BROWSE_SAMPLES = new Counter('browse_samples');
const SEARCH_SAMPLES = new Counter('search_samples');

export const options = PREFLIGHT_ONLY
    ? {
          scenarios: {
              preflight: {
                  executor: 'per-vu-iterations',
                  vus: 1,
                  iterations: 1,
                  tags: { phase: 'preflight_only' },
              },
          },
      }
    : {
          scenarios: {
              measurement: {
                  executor: 'constant-vus',
                  vus: MEASUREMENT_VUS,
                  duration: `${MEASUREMENT_DURATION_SECONDS}s`,
                  gracefulStop: '0s',
                  tags: { phase: 'measure' },
              },
          },
          thresholds: {
              measured_duration: ['p(95)<=200'],
              request_errors: ['rate<0.01'],
              browse_samples: ['count>0'],
              search_samples: ['count>0'],
          },
      };

const twoDigits = (value) => String(value).padStart(2, '0');
const groupIndexFor = (vu, iteration) => ((vu - 1) + iteration) % GROUP_COUNT;
const probeFor = (groupIndex, variant) => PROBE_CASES[variant % PROBE_CASES.length] + twoDigits(groupIndex);
const browseUrl = (page) => `${BASE_URL}/api/movies?page=${page}&pageSize=${PAGE_SIZE}&sort=Title&direction=Ascending`;
const searchUrl = (probe) => `${BASE_URL}/api/movies?text=${encodeURIComponent(probe)}&page=1&pageSize=${PAGE_SIZE}&sort=Title&direction=Ascending`;

const parsePayload = (response) => {
    try {
        const payload = JSON.parse(response.body);
        if (payload === null || typeof payload !== 'object') {
            return null;
        }
        if (!Array.isArray(payload.items)) {
            return null;
        }
        if (!Number.isInteger(payload.totalCount)) {
            return null;
        }
        return payload;
    } catch {
        return null;
    }
};

const itemProblems = (items, expectedGroup) => {
    const problems = new Set();
    const ids = new Set();
    for (const item of items) {
        if (!item || !Number.isInteger(item.id) || typeof item.title !== 'string') {
            problems.add('item contract');
            continue;
        }
        if (ids.has(item.id)) {
            problems.add('duplicate item id');
        }
        ids.add(item.id);
        const match = SEED_TITLE.exec(item.title);
        if (match === null) {
            problems.add('item title is not the seeded fixture');
            continue;
        }
        if (expectedGroup !== null && Number(match[1]) !== expectedGroup) {
            problems.add('item title outside the requested group');
        }
    }
    return [...problems];
};

const browseProblems = (response) => {
    if (response.status !== 200) {
        return [`http status ${response.status}`];
    }
    const payload = parsePayload(response);
    if (payload === null) {
        return ['response contract'];
    }
    const problems = [];
    if (payload.totalCount !== BROWSE_TOTAL_COUNT) {
        problems.push(`totalCount ${payload.totalCount} (expected ${BROWSE_TOTAL_COUNT})`);
    }
    if (payload.items.length !== PAGE_SIZE) {
        problems.push(`items ${payload.items.length} (expected ${PAGE_SIZE})`);
    }
    problems.push(...itemProblems(payload.items, null));
    return problems;
};

const searchProblems = (response, expectedGroup) => {
    if (response.status !== 200) {
        return [`http status ${response.status}`];
    }
    const payload = parsePayload(response);
    if (payload === null) {
        return ['response contract'];
    }
    const problems = [];
    if (payload.totalCount !== SEARCH_TOTAL_COUNT) {
        problems.push(`totalCount ${payload.totalCount} (expected ${SEARCH_TOTAL_COUNT})`);
    }
    if (payload.items.length !== PAGE_SIZE) {
        problems.push(`items ${payload.items.length} (expected ${PAGE_SIZE})`);
    }
    problems.push(...itemProblems(payload.items, expectedGroup));
    return problems;
};

const record = (kind, problems, response) => {
    const valid = problems.length === 0;
    const duration = response.timings.duration;
    check(valid, { [`${kind} response valid`]: (value) => value }, { type: kind });
    REQUEST_ERRORS.add(!valid, { type: kind });
    MEASURED_DURATION.add(duration, { type: kind });
    if (kind === 'browse') {
        BROWSE_ERRORS.add(!valid);
        BROWSE_DURATION.add(duration);
        BROWSE_SAMPLES.add(1);
    } else {
        SEARCH_ERRORS.add(!valid);
        SEARCH_DURATION.add(duration);
        SEARCH_SAMPLES.add(1);
    }
    return valid;
};

const metricValue = (data, name, key) => {
    const metric = data.metrics[name];
    if (!metric || !metric.values || typeof metric.values[key] !== 'number') {
        return null;
    }
    return metric.values[key];
};

const thresholdOutcomes = (data) => {
    const outcomes = {};
    for (const [name, metric] of Object.entries(data.metrics)) {
        if (!metric.thresholds || Object.keys(metric.thresholds).length === 0) {
            continue;
        }
        const entries = Object.entries(metric.thresholds);
        outcomes[name] = {
            ok: entries.every(([, threshold]) => threshold.ok),
            thresholds: Object.fromEntries(entries.map(([expression, threshold]) => [expression, threshold.ok])),
        };
    }
    return outcomes;
};

const buildEvidence = (data) => {
    const checks = data.metrics.checks ? data.metrics.checks.values : { passes: 0, fails: 0 };
    const outcomes = thresholdOutcomes(data);
    const browseSamples = metricValue(data, 'browse_samples', 'count') || 0;
    const searchSamples = metricValue(data, 'search_samples', 'count') || 0;
    const outcomesOk = Object.values(outcomes).every((outcome) => outcome.ok);
    return {
        schema: 'dev427-load-evidence/v1',
        run: {
            id: RUN_ID,
            utc: RUN_UTC,
            gitHead: GIT_HEAD,
            k6Image: K6_IMAGE,
        },
        target: BASE_URL,
        configured: {
            scenario: 'measurement',
            vus: MEASUREMENT_VUS,
            durationSeconds: MEASUREMENT_DURATION_SECONDS,
            browsePageCount: BROWSE_PAGE_COUNT,
            pageSize: PAGE_SIZE,
            browseRequestsPerCycle: BROWSE_REQUESTS_PER_CYCLE,
            groupCount: GROUP_COUNT,
        },
        measured: {
            requests: browseSamples + searchSamples,
            p95: metricValue(data, 'measured_duration', 'p(95)'),
            average: metricValue(data, 'measured_duration', 'avg'),
            errorRate: metricValue(data, 'request_errors', 'rate'),
            browse: {
                samples: browseSamples,
                p95: metricValue(data, 'browse_duration', 'p(95)'),
                errorRate: metricValue(data, 'browse_errors', 'rate'),
            },
            search: {
                samples: searchSamples,
                p95: metricValue(data, 'search_duration', 'p(95)'),
                errorRate: metricValue(data, 'search_errors', 'rate'),
            },
        },
        checks: {
            passed: checks.passes || 0,
            failed: checks.fails || 0,
            total: (checks.passes || 0) + (checks.fails || 0),
        },
        thresholdOutcomes: outcomes,
        thresholdsOk: outcomesOk,
        samplesOk: browseSamples > 0 && searchSamples > 0,
        acceptance: outcomesOk && browseSamples > 0 && searchSamples > 0 ? 'PASSED' : 'FAILED',
    };
};

const evidenceLines = (evidence) => [
    `run: ${evidence.run.id} (${evidence.run.utc})`,
    `git head: ${evidence.run.gitHead}`,
    `k6 image: ${evidence.run.k6Image}`,
    `target: ${evidence.target}`,
    `configured: ${evidence.configured.vus} vus for ${evidence.configured.durationSeconds}s`,
    `measured p95: ${evidence.measured.p95} ms`,
    `measured error rate: ${evidence.measured.errorRate}`,
    `browse samples: ${evidence.measured.browse.samples}`,
    `search samples: ${evidence.measured.search.samples}`,
    `checks: ${evidence.checks.passed}/${evidence.checks.total} passed`,
    `acceptance: ${evidence.acceptance}`,
];

export function setup() {
    if (!PREFLIGHT_ONLY && (!RUN_ID || !RUN_UTC || !GIT_HEAD || !K6_IMAGE)) {
        throw new Error('RUN_ID, RUN_UTC, GIT_HEAD and K6_IMAGE are required for a measured run');
    }
    const failures = [];
    for (let page = 1; page <= BROWSE_PAGE_COUNT; page += 1) {
        const response = http.get(browseUrl(page), { tags: { phase: 'preflight', type: 'browse' } });
        const problems = browseProblems(response);
        if (problems.length > 0) {
            failures.push(`browse page ${page}: ${problems.join(', ')}`);
        }
    }
    for (let groupIndex = 0; groupIndex < GROUP_COUNT; groupIndex += 1) {
        const probe = probeFor(groupIndex, groupIndex);
        const response = http.get(searchUrl(probe), { tags: { phase: 'preflight', type: 'search' } });
        const problems = searchProblems(response, groupIndex);
        if (problems.length > 0) {
            failures.push(`search ${probe}: ${problems.join(', ')}`);
        }
    }
    if (failures.length > 0) {
        throw new Error(`preflight failed: ${failures.join('; ')}`);
    }
    return { baseUrl: BASE_URL };
}

export default function () {
    if (PREFLIGHT_ONLY) {
        return;
    }
    const groupIndex = groupIndexFor(__VU, __ITER);
    for (let index = 0; index < BROWSE_REQUESTS_PER_CYCLE; index += 1) {
        const page = (((__VU - 1) + __ITER * BROWSE_REQUESTS_PER_CYCLE + index) % BROWSE_PAGE_COUNT) + 1;
        const response = http.get(browseUrl(page), { tags: { phase: 'measure', type: 'browse' } });
        record('browse', browseProblems(response), response);
        sleep(1);
    }
    const probe = probeFor(groupIndex, groupIndex + __ITER);
    const response = http.get(searchUrl(probe), { tags: { phase: 'measure', type: 'search' } });
    record('search', searchProblems(response, groupIndex), response);
    sleep(1);
}

export function handleSummary(data) {
    if (PREFLIGHT_ONLY) {
        const checks = data.metrics.checks ? data.metrics.checks.values : { passes: 0, fails: 0 };
        return { stdout: `preflight checks: ${checks.passes || 0} passed, ${checks.fails || 0} failed\n` };
    }
    const evidence = buildEvidence(data);
    const fileSafeUtc = RUN_UTC.replace(/[^0-9A-Za-z]/g, '');
    const path = `${SUMMARY_DIR}/${RUN_ID}-${fileSafeUtc}.json`;
    return {
        [path]: JSON.stringify(evidence, null, 2),
        stdout: `${evidenceLines(evidence).join('\n')}\n`,
    };
}
