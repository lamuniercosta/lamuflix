# 0023. Compose browser smoke isolation

- Status: Proposed
- Date: 2026-10-19
- Ticket: DEV-334

## Context

DEV-334 requires three headless Chromium smoke specs against the DEV-325 compose stack: seeded browse, genre filtering that updates the grid and URL, and folder import displaying Pending. Imports enqueue enrichment, and genre facets require persisted provider metadata. The compose host has no configuration-only consumer-disable switch; provider URL validation permits plain HTTP only on loopback. A junk key still contacts real OMDb, while a dead loopback endpoint cannot supply genres and eventually produces Failed imports.

Basis: [brief](../../specs/DEV-334/brief.md), Q1–Q4 and Phase 2 steps 5–6; [conclusions](../../specs/DEV-334/CONCLUSIONS.md), cited Patron rulings; [specification](../../specs/DEV-334/spec.md), FR-001–FR-012. The compose host registers the metadata provider unconditionally; OmdbOptions permits plain HTTP only on loopback; existing enrichment persists the genres consumed by the browser.

## Decision

Place the Playwright suite under `web/`, separate from Vitest discovery, with `web/playwright.config.ts` and exactly three `web/e2e/*.e2e.ts` specs. Add only `@playwright/test` as a direct dev dependency, exact-pinned to the highest stable Node-24-compatible release published at least 14 days before selection, supported by Phase B registry evidence. `npm run e2e` runs headless Chromium against the production-built web application, real API, PostgreSQL, RabbitMQ and enrichment consumer.

Add a test-only `web/e2e/compose.e2e.yml` overlay to the original compose definition. Its Node built-in HTTP OMDb-compatible stub shares `network_mode: service:api`; generated API settings point `Omdb:BaseUrl` to `http://127.0.0.1:<port>` in that namespace. Bind to literal 127.0.0.1 with no published port. Reject effective provider hosts other than literal 127.0.0.1 and validate the provider setting and stub readiness before imports. The stub serves known canned responses, rejects unexpected requests, and never forwards or falls back to real OMDb. Browser API interception, SQL seeding, TLS bypasses and a new production consumer switch are rejected because they bypass the required real flow or expand the approved scope.

Generate per-movie folders from committed DEV-325 sample placeholders and mount them read-only through existing `LIBRARY_PATH`. Seed two distinguishable genre-bearing movies through the existing import API once outside browser-worker retries, waiting on observable movie and genre state. Hold the provider response for the UI import until the browser observes the real POST 202 and Pending row; release in attempt finally after Pending observation or failure and await drain before retry rearm. Arm independently before each attempt submits and release every outstanding hold in invocation finally. Host runner and attempt hooks invoke loopback control through bounded docker compose exec -T into the stub service, without a published stub/control port or application endpoint. Give initial and retry imports distinct folders derived from the reserved sample file. Run serially on a fresh database and queue per invocation, independent of spec order, using bounded condition/request synchronization rather than fixed sleeps.

The Node runner generates disposable credentials, settings and fixtures under ignored `web/e2e/.runtime/`, passes a generated env-file, overrides the settings mount and uses an isolated compose project. Preserve existing root `.env`, `docker/api.settings.json`, base compose and committed sample media. Reject conflicting ownership of `localhost:3000`; keep `Features.LocalPlay=false`. Wait for stack health and stub readiness, preserve sanitized failure evidence, then always tear down only this invocation's resources and volumes. Generate credentials with Node crypto.randomBytes. Redact generated database/queue credentials and provider keys and scan only the sanitized upload allowlist for generated secret values; block unsafe upload. Do not retain generated settings or compose-config dumps. Unexpected-request status fails the runner nonzero. One named wait-budget source in run.mjs is passed to the stub and specs.

The blocking PR CI job installs Chromium and runs the same `npm run e2e` path with one retry in CI and zero locally. Assert browser-visible browse results, genre inclusion/exclusion plus `genreIds` URL state, and Pending import. Retain failure screenshots and sanitized compose/stub logs, including an initially failed attempt that passes on retry. Use a PR guard, bounded job timeout, minimum permissions, existing action major pins, explicit retention and always-run sanitized-evidence upload. Typecheck Playwright config/specs through web/e2e/tsconfig.json and the existing build script; exclude runtime output through narrow ESLint/Prettier ignores. Keep visual regression and k6 outside this decision.

## Consequences

The suite exercises real import, messaging, enrichment persistence and browser behavior without real OMDb traffic or production C#, API, schema or generated-client changes. The trade-off is stack startup time, one additional test service, and lifecycle/hold-release code needed for deterministic Pending observation, retry isolation and protection of local data. Verification must exercise the ordinary local run, clean repeatability and scoped cleanup, plus a restored controlled assertion failure proving nonzero exit propagation and retained failure evidence.

