# Tasks: DEV-313 API End-to-End Integration Suite

**Input**: Design documents from `/specs/DEV-313/`

**Prerequisites**: plan.md (required), spec.md (required for user stories)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Harness composition shared by every story

- [X] T001 [US1] Create ApiEndToEndFactory.cs in tests/LamuFlix.IntegrationTests as a new WebApplicationFactory<Program> implementation reproducing the existing ApiHostFactory host/configuration, service-provider validation and health-check-stub patterns; do not subclass the sealed ApiHostFactory. In CreateHost, apply fixture ConnectionStrings:DefaultConnection, RabbitMQ host/port/runtime credentials, loopback Omdb:BaseUrl, non-secret sentinel Omdb:ApiKey, Enrichment:ClaimLease=00:00:01, RabbitMq:RetryDelay=00:00:02 and Features:LocalPlay=false through builder.ConfigureHostConfiguration with AddInMemoryCollection before base.CreateHost and eager persistence registration. Preserve production persistence/metadata-before-consumer order. In ConfigureTestServices, remove existing TimeProvider registrations and register the supplied test TimeProvider singleton; before measured requests assert the host resolves that exact instance. Retain the real publisher, consumer, repository, scanner and metadata provider.
- [X] T002 [US1] Create ApiEndToEndFixture.cs in tests/LamuFlix.IntegrationTests with owned Postgres/RabbitMQ/WireMock instances and migrate/reset/cleanup support. Before every measured host starts, with any prior host stopped/disposed, reset/migrate the DB and broker topology, start the WireMock server, perform provider warm-up using the existing probe pattern, reset WireMock, then install the final deterministic per-test stubs. No reset occurs between final stub installation and measured requests; later infrastructure resets run only after host stop/dispose.
- [X] T003 [P] [US1] Create ApiEndToEndCollection.cs in tests/LamuFlix.IntegrationTests as an isolated collection with CollectionDefinition DisableParallelization=true; all new ApiEndToEnd test classes use this collection and its owned fixture.
- [X] T004 [US1] Create ApiEndToEndTestBase.cs in tests/LamuFlix.IntegrationTests with the shared T002 lifecycle: stop/dispose any prior host, prepare/reset/migrate backing infrastructure, warm up the provider, reset WireMock, install final per-test stubs, then start a fresh measured host. Warm-up requests never count as endpoint proof. Seed deterministic rows through MovieCatalogSeed/DbContext; dispose the seeding context. Poll persisted status using a fresh scoped DbContext and a no-tracking database query on every iteration, including the final assertion, rather than reusing a tracked seed entity. Retain the real elapsed-time 30-second deadline, 50ms interval, final failing assertion and secret-free diagnostics. Advance the supplied host-resolved TimeProvider only when lease expiry is needed, never to shorten the polling bound or to recover an already ACKed refused claim. Stop/dispose the host before infrastructure reset/cleanup on both success and failure.

**Checkpoint**: Harness starts a real-backed host whose consumer path is live; warm-up traffic is reset and never asserted as proof

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Playback safety and seeded-read proof before causal OMDb work

- [X] T005 [US5] Implement ApiEndToEndPlaybackTests.cs with Features:LocalPlay=false and real HTTP/Postgres. Through ConfigureTestServices remove existing IProcessStarter registrations, register a recording/substituted IProcessStarter singleton as the safety tripwire, and assert the host resolves that exact instance before POST play. Assert 403 application/problem+json and zero Start calls on that instance. Preserve the real catalog and other infrastructure; no enabled-play scenario, process execution or src change.
- [X] T006 [P] [US1] Implement ApiEndToEndBrowseTests.cs over HTTP with deterministic persisted seeds: assert 200 OK and the frozen Items/TotalCount response shape, matching combined filters, stable pagination across boundaries with exact total counts, and stable sorting with ties and NULLS LAST. Assert returned generated IDs and values rather than assumed identities.
- [X] T007 [P] [US2] Implement ApiEndToEndLibraryTests.cs over HTTP: details return 200 with the frozen Id/Title/Path/Format/Metadata scalar shape and unknown IDs return 404; genres/people return 200 arrays of Id/Name values derived from persisted metadata. POST and DELETE watchlist each return 204 NoContent with an empty body and persist the corresponding transition, verified through a fresh DbContext or subsequent appropriate HTTP read. Assert generated IDs and semantic values; do not accept generic success status alone.

**Checkpoint**: Fixture/host safety and seeded read/library operations proven over HTTP

---

## Phase 3: User Story 3 - Import through real scanner to OMDb (Priority: P1)

**Goal**: Real import-to-OMDb causal proof over HTTP

**Independent Test**: Temp `<Title> (<Year>)` folder with one tiny .mkv/.mp4 file; POST import returns 202, relative Location, empty body; poll persisted status to Enriched; GET details plus WireMock request log match

- [X] T008 [US3] Implement ApiEndToEndImportTests.cs success path: real temp <Title> (<Year>) folder with one tiny .mkv/.mp4 file, 202 with relative Location using the generated persisted movie ID and an empty body, persisted movie, bounded fresh-DB poll to Enriched, GET details confirmation, and matching measured WireMock OMDb request evidence. Register cleanup as soon as the folder is created; on success, host-start failure or assertion failure, stop/dispose any created or partially started host before deleting the owned temp folder in failure-safe teardown/finally cleanup. No playable media or process execution.

**Checkpoint**: Import-to-OMDb causal chain proven; no hand-invoked handler/provider/consumer counted as evidence

---

## Phase 4: User Story 4 - Enrichment retry to OMDb (Priority: P2)

**Goal**: Real retry-to-OMDb causal proof plus 409/404 contracts

**Independent Test**: Seeded eligible movie retried over HTTP reaches Enriched with WireMock evidence; ineligible and missing rows return 409/404

- [ ] T010 [US4] Implement ApiEndToEndEnrichmentTests.cs: persist a retry-eligible movie with LastAttemptAt=null before POST so the first Pending claim is eligible without waiting for lease expiry; use the existing rehydration/seed patterns without production changes. Assert 202, relative Location using the generated movie ID and an empty body, then bounded fresh-DB poll to Enriched, GET details confirmation and matching measured WireMock request evidence. Never rely on clock advancement after a refused claim has been ACKed. Include the existing 409 ineligible and 404 missing contracts.
- [ ] T009 [US3] Implement ApiEndToEndValidationTests.cs: invalid import and browse inputs assert 422 application/problem+json with field-keyed errors

**Checkpoint**: Retry-to-OMDb causal chain proven alongside 409/404 and validation contracts

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Regression, verification honesty, and review discipline

- [ ] T011 Run full tests/LamuFlix.IntegrationTests regression plus required solution checks; keep DEV-313 delta (from incoming HEAD 01caa4d) distinct from inherited DEV-311/DEV-20 ancestry
- [ ] T012 Report actual gate outcomes (build, Roslyn, complexity, InspectCode, tests, property tests, package/security, format, required ship-review gates): PASS only when earned; scope-empty exit-2 is SKIPPED (scope-empty); configured opt-out is SKIP; mutation N/A only when the script reports it; could-not-run blocks; no threshold or harness edits
- [ ] T013 Publish delivery findings/summary on the delivery PR and read back evidence; remediation replies to findings and resolves threads; end delivery at awaiting-merge, never merge

---

## Dependencies & Execution Order

- Phase 1 blocks everything: factory, fixture, collection, base must land first.
- Phase 2 next: playback safety (T005) and seeded read/library (T006, T007); T006 and T007 run in parallel.
- Phase 3 next: import proof (T008).
- Phase 4 next: retry proof (T010) after import proof, then validation (T009).
- Phase 5 last: regression, honest gate reporting, PR publication.

## Parallel Opportunities

- T003 (collection) is parallel-safe alongside T001/T002.
- T006 and T007 are parallel-safe (different files).

## Implementation Strategy

1. Complete Phase 1 harness.
2. Complete Phase 2 safety plus seeded reads.
3. Add Phase 3 import-to-OMDb proof, then Phase 4 retry-to-OMDb proof, then validation.
4. Run Phase 5 regression and verification; collect all delivery review axes (Sentry, Ledger, Compass mandatory for size:M adjudication).
5. Loop discipline per Q7: Critical/High/Medium findings need a concrete failure scenario; two formal plan-challenge rounds and two delivery-review/remediation rounds, separately counted; past the cap unresolved findings become follow-ups, deferred Critical/High keeps NEEDS FIXES reported to Bernstein.

## Notes

- No commits, source edits, or gate execution in this Phase 2 ask; these tasks are the implementation plan for after Gate 1.
- Gate 1 stays the user-owned spec merge; clean speckit-analyze, plan-challenge adjudication, and gate1:provisional precede it; Phase B pickup drift analysis follows it.
