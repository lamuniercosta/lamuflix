import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:3000').replace(/\/+$/, '');
const BROWSE_TOTAL_COUNT = 500;
const SEARCH_TOTAL_COUNT = 50;
const PAGE_SIZE = 20;
const BROWSE_PAGE_COUNT = 25;
const GROUP_COUNT = 10;
const BROWSE_REQUESTS_PER_CYCLE = 4;
const PROBE_CASES = ['Group', 'group', 'GROUP', 'gRoUp'];
const SEED_TITLE = /^DEV427 Group(\d{2}) Movie(\d{4})$/;
const REQUEST_ERRORS = new Rate('request_errors');

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

const record = (kind, problems) => {
    const valid = problems.length === 0;
    check(valid, { [`${kind} response valid`]: (value) => value }, { type: kind });
    REQUEST_ERRORS.add(!valid, { type: kind });
    return valid;
};

export function setup() {
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
    if (__ENV.PREFLIGHT_ONLY === '1') {
        return;
    }
    const groupIndex = groupIndexFor(__VU, __ITER);
    for (let index = 0; index < BROWSE_REQUESTS_PER_CYCLE; index += 1) {
        const page = (((__VU - 1) + __ITER * BROWSE_REQUESTS_PER_CYCLE + index) % BROWSE_PAGE_COUNT) + 1;
        const response = http.get(browseUrl(page), { tags: { phase: 'measure', type: 'browse' } });
        record('browse', browseProblems(response));
        sleep(1);
    }
    const probe = probeFor(groupIndex, groupIndex + __ITER);
    const response = http.get(searchUrl(probe), { tags: { phase: 'measure', type: 'search' } });
    record('search', searchProblems(response, groupIndex));
    sleep(1);
}
