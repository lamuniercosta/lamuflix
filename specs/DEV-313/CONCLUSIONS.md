# DEV-313 - Conclusions (Patron rulings)

Append-only. Each entry records the full grill exchange and the cited basis.

## Q1/12 - Endpoint envelope

**Keel asked:** Which endpoint envelope satisfies the ticket unchanged? Recommend interpret
all endpoints literally as all nine mapped operations: browse, details, import, retry
enrichment, play, watchlist add/remove, genres and people, with additive container-backed
tests rather than rewriting existing double-based tests. Basis: recon lines 246-260
comprehensive suite and all endpoints, versus lines 252-256 five required scenario bullets;
endpoint map lines 126-130. Cost: broader additive coverage than five bullets, but no
delivery reduction or owner checkbox. Alternative five-only interpretation risks dropping
explicit acceptance scope.

**Patron ruling:** Accepted. Envelope = all nine mapped operations, each driven end-to-end
through `WebApplicationFactory<Program>` against real Postgres (Testcontainers) and WireMock,
added as new tests; the five Scope bullets are mandatory scenarios inside that envelope, and
existing double-based tests stay untouched. No owner checkbox.

- Ticket text is decided: AC line 259 *All endpoints tested end-to-end via WebApplicationFactory*
  and Overview line 247 *comprehensive API integration test suite*; reading the five bullets
  (lines 252-256) as the whole set would drop AC scope, which would be a §2.3(a) change,
  so the literal reading is the one that leaves the ticket unchanged.
- Nine operations per recon endpoint map lines 126-130: GET /api/movies, GET /api/movies/{id},
  POST /api/movies/import, POST /api/movies/{id}/enrichment, POST /api/movies/{id}/play,
  POST and DELETE /api/movies/{id}/watchlist, GET /api/genres, GET /api/people.
  Constitution IX (`.specify/memory/constitution.md:288`, :299-301) requires this trio for
  API end-to-end; no departure.
- Additive only, so §2.3 item 6 (rewriting unnamed tracked files) is not triggered (recon
  §6.7, §10 notes lines 267-268); no new route/DTO/status code, so item 4 is not triggered.

## Q2/12 - §2.3 item 5: Features:LocalPlay / Process.Start in play coverage

**Keel asked:** Confirm the deliberate section 2.3 item 5 ruling for tests-only playback
coverage. Recommend real HTTP and Postgres with Features:LocalPlay=false, assert 403
ProblemDetails and no process launch, retain a recording/substituted IProcessStarter strictly
as a safety tripwire, never enable playback or alter src playback code. Basis recon 64-70,
162-173; ticket 255 explicitly mandates disabled play. Cost: no enabled-play proof, which
ticket does not request. All other infrastructure seams remain real per Constitution IX.

**Patron ruling:** Accepted. Play coverage is tests-only with `Features:LocalPlay=false`, real
HTTP and Postgres, asserting 403 `application/problem+json` and zero `IProcessStarter.Start`
calls; the substituted `IProcessStarter` is a safety tripwire only; no test enables LocalPlay
and nothing under `src/` playback changes.

- Ticket line 255 names *play endpoint returning 403 when LocalPlay is disabled*; enabled play
  is not requested, so omitting it changes no delivery.
- The gate stays as built: `PlaybackServiceCollectionExtensions.cs:19` -> `DisabledMediaPlayerLauncher.cs:10`
  -> `GlobalExceptionHandler.cs:41-44` (recon 64-70); `SystemProcessStarter.cs:11` `Process.Start`
  is never reachable from a test (recon 162-173), satisfying the charter LocalPlay hard rule.
- Not a Constitution IX departure: IX :299-301 names Postgres and the metadata provider as the
  infrastructure that must be real, and both stay real; the OS process launcher is a
  side-effect boundary held closed by the disabled gate, with the substitute only proving it.

## Q3/12 - WireMock proof path

**Keel asked:** Freeze the meaningful WireMock proof path? Recommend at least one real import
success and one real enrichment retry success through HTTP -> EF/Postgres -> real RabbitMq
publisher and API-host EnrichmentConsumer -> OMDb WireMock -> persisted Enriched metadata,
observed with bounded polling and then HTTP detail assertions plus matching WireMock request
evidence. All nine operations run in this real-backed harness; read-only operations need no
artificial OMDb calls. Keep queue/repository/scanner/provider real, no direct manual handler or
provider invocation as replacement evidence. Basis updated recon 11.1, 11.3, 11.5 and ticket AC
259-260. Cost additional RabbitMq lifecycle, justified by actual causal provider proof. No
legacy Worker or unwired sweep. Confirm Q1 means configured real WireMock for all operations,
actual requests on enrichment-triggering paths only.

**Patron ruling:** Accepted, frozen. Minimum proof = one import success and one enrichment-retry
success, each HTTP -> EF/Postgres -> real RabbitMQ publisher -> the API host's own
`EnrichmentConsumer` -> real `OmdbMetadataProvider` -> WireMock -> persisted `Enriched`, observed
by bounded DB polling, then confirmed by `GET /api/movies/{id}` and matching WireMock request
log; no hand-invoked handler/provider/consumer counts as evidence. Q1 clarified: all nine
operations run in the same real-backed host with WireMock configured; actual OMDb requests are
asserted only on enrichment-triggering paths (import, retry).

- AC line 260 (*contract compliance verified against real Postgres and WireMock*) is only
  causally met if a real OMDb request flows from an HTTP call; the only such route is
  import/retry -> queue -> consumer -> provider (recon 11.3 steps 1-8), and the only completion
  signal is the persisted row (recon 11.5, no status route).
- The consumer runs inside the API host (recon 11.1, `Program.cs:17-20` ordering;
  `RabbitMqServiceCollectionExtensions.cs:41-52`), so real RabbitMQ is the production path,
  not added scope; `Testcontainers.RabbitMq` 4.15.0 is already pinned (recon §6.1), so no
  §2.3 item 1 trigger. Constitution IX :299-301 forbids mocking that infrastructure.
- Excluded: `LamuFlix.Worker` and `tests/LamuFlix.Test` (not in the solution, recon 11.2,
  11.9) and any wait on the unwired sweep (recon 11.4). Known constraints the plan must carry,
  not re-rule: `RetryDelay > ClaimLease` validator trap (recon 11.4) and the hosted-service-runs
  caveat (recon 11.9).

## Q4/12 - Harness lifecycle and configuration

**Keel asked:** Freeze test harness lifecycle/configuration approach? Recommend add an isolated
ApiEndToEndFactory and ApiEndToEndFixture in IntegrationTests using existing ApiHostFactory
composition, PostgresFixture, RabbitMqFixture and WireMock; supply fixture
ConnectionStrings:DefaultConnection plus RabbitMq host/port/fixture credentials, loopback
Omdb:BaseUrl and nonsecret sentinel ApiKey BEFORE host build. Explicitly choose ClaimLease=1s
and RetryDelay=2s (strict greater-than invariant); no external network or hardcoded
credentials. Serialize the new suite in its own nonparallel collection; stop/dispose host
before resetting database, broker topology or WireMock logs to avoid in-flight races, start
fresh host only after migrated/reset infrastructure and stubs are ready. Warm up provider via
the existing probe pattern before measured requests then reset logs, never count warmup as
endpoint proof. Bound polling with injected TimeProvider, final failing assertion and
diagnostics, follow 30s/50ms existing precedent without arbitrary sleeps. Basis recon 108-124
and 11.4-11.5. Cost slower serialized lifecycle buys deterministic real infrastructure.

**Patron ruling:** Accepted, frozen, with one precision: the polling deadline stays wall-clock
(30s/50ms + final assert) as in the precedent; an injected `TimeProvider` is allowed only when
registered before host build and only to drive lease expiry, never to shorten the poll bound.

- Not a §2.3 item 2 trigger: two new classes inside the existing
  `tests/LamuFlix.IntegrationTests` project, composing existing `ApiHostFactory`,
  `PostgresFixture.cs:27`, `RabbitMqFixture.cs:26` and the `MetadataProviderProbe` loopback
  pattern (recon §2, §5, 11.4); no new project, folder layer or dependency.
- Host-config-before-build is forced by eager capture at
  `PersistenceServiceCollectionExtensions.cs:23-31` (recon 108-112); `ClaimLease=1s` <
  `RetryDelay=2s` is forced by `RabbitMqConsumerOptionsValidator.cs:26-33` (recon 11.4 trap);
  loopback http is allowed by `OmdbOptions.cs:38-40`.
- Secrets rule holds: connection strings and broker credentials come from the containers at
  runtime, ApiKey is a non-secret sentinel (recon §6.6); warm-up + `Server.Reset()` follows
  `MetadataProviderProbe.cs:48-52`; poll shape follows `EnrichmentConsumerTests.cs:645-660`
  (recon 11.5).

## Q5/12 - Scenario matrix and real filesystem import

**Keel asked:** Approve scenario matrix and real filesystem import strategy? Recommend
deterministic hand-authored MovieCatalogSeed rows via real DbContext for browse filter
combinations, pagination boundaries and stable sort/ties/null placement, details success+404,
watchlist add/remove persisted transitions, and genres/people values from seeded metadata.
Import creates a disposable temp folder with title/year name and one tiny supported
video-extension file; real scanner, no playable content, delete after stopped host. Assert
202+relative Location+empty body, persisted import and eventual enrichment; retry seeded
eligible movie asserts 202+Location and eventual Enriched, plus existing 409 and 404 contracts;
invalid import/browse inputs assert 422 problem+json field-keyed errors; disabled play asserts
Q2. No full duplicate matrix of existing EF/provider resilience/consumer tests; critical
missing/error cases stay in the new HTTP seam tests. Tiny filesystem exercise is an
integration-test need, not a constitution departure; record vendor disk-bound guidance
reconciliation. Use explicit semantic JSON/DTO/status/header assertions and generated IDs, no
Verify/AutoFixture/Faker dependency additions. Basis recon 41-75, 125-133, 11.6 and
Constitution V/IX. Cost small disk I/O and fixtures, no unrelated feature scope.

**Patron ruling:** Accepted as stated. Matrix covers all nine operations per Q1, with the five
Scope bullets (ticket 252-256) as mandatory cases; import uses a real temp folder named
`<Title> (<Year>)` with one tiny `.mkv`/`.mp4` file, deleted after the host stops.

- Real disk is required, not a choice: production binds `DirectoryMediaLibraryScanner` over the
  real `FileSystem` (`HandlerRegistration.cs:27-28`, recon 11.6), and a missing folder 500s
  rather than 422s (`DirectoryMediaLibraryScanner.cs:24`). Disk-bound reconciliation:
  `.claude/rules/pipeline/test-assertions.mdc:87-88` forbids *mocking* the file system and
  names real infrastructure as the norm; no vendored testing rule prohibits disk in
  integration tests (searched `.claude/rules/`, no match). Not a constitution departure (IX
  :299-301 real infrastructure).
- Validation cases assert 422 `application/problem+json` field-keyed errors per Constitution V
  (:196-213, `GlobalExceptionHandler.cs:25-28`); 202/Location/empty body per recon 52-57;
  retry 409/404 per recon 72-75. Stub determinism comes from folder-name parsing
  (`DirectoryMediaLibraryScanner.cs:61-63`, recon 11.6).
- No Verify, AutoFixture or Faker reference added: Verify is unpinned (§2.3 item 1 would
  trigger, recon §6.1), and the others are unnecessary; explicit semantic assertions and
  generated IDs suffice. Existing EF/provider/consumer tests are not duplicated (Q1 additive).

## Q6/12 - Implementation file envelope

**Keel asked:** Freeze additive implementation file envelope? Recommend only new files under
tests/LamuFlix.IntegrationTests: ApiEndToEndFactory.cs, ApiEndToEndFixture.cs,
ApiEndToEndCollection.cs, ApiEndToEndTestBase.cs, ApiEndToEndBrowseTests.cs,
ApiEndToEndLibraryTests.cs (details/facets/watchlist), ApiEndToEndImportTests.cs,
ApiEndToEndEnrichmentTests.cs, ApiEndToEndPlaybackTests.cs, and ApiEndToEndValidationTests.cs.
Existing public fixtures/seeder are reused, not rewritten. Spec artifacts under specs/DEV-313 are
Quill scope after brief. No src, migrations, CPM/csproj/solution, existing tests, harness, web or
generated contracts changes. No new project/layer/domain vocabulary/ADR; size:M integration proof
only. If implementation exposes a required existing-file edit or production defect, stop for
cited Patron disposition rather than widening this envelope; unrelated issues become follow-up.
Basis recon 31-39, 106-160, 179 and all previous rulings. Cost more additive helpers preserves
current coverage and unrelated inherited work.

**Patron ruling:** Accepted, envelope frozen at those ten new files in
`tests/LamuFlix.IntegrationTests/`, with one amendment: an edit to an existing file that a gate
or an acceptance criterion forces (charter §2.3 item 6 carve-out) is in scope once the builder
cites the gate/AC; every other existing-file edit or production defect stops for a Patron
ruling, and unrelated findings follow the follow-up-ticket rule.

- No csproj/CPM change needed: the project already references `Mvc.Testing`, `WireMock.Net`
  and `Tests.Common` (recon 33-35), and `RabbitMqFixture`/`PostgresFixture` already serve
  `EnrichmentConsumerTests`/`EfMovie*` tests in this project (recon 11.7, §4).
- No §2.3 item 2/3/4 trigger: same project, no migration, no route/DTO/status change (recon
  §6.2-6.4); additive only, so item 6 is untouched (recon 179, Q1).
- The pre-existing uncommitted `.specify/feature.json` and `harness.yml` modifications on this
  branch are outside the envelope and are preserved untouched, not counted as DEV-313 edits.

## Q7/12 - Loop Discipline terms

**Keel asked:** Set Loop Discipline terms? Recommend closing bar: no open accepted
Critical/High/Medium findings and all required runnable gates earned; Low findings are
documented follow-ups unless required by ticket or constitution. Frozen scope: additive
real-host integration proof of all nine mapped operations, five mandatory scenarios, actual
import/retry-to-OMDb causal evidence, Q2 safety and Q6 envelope; anything else is a follow-up
issue, not a finding in this round. Cap: two formal plan-challenge rounds and two formal
delivery-review/remediation rounds, separately counted, no third or replacement immutable
review; unresolved blocker at cap is reported to Conductor. Analysis fixes remain one numbered
list per round with independent readback; analysis, freeze, Gate 1 and merge remain separate
states. Size:M delivery adjudication requires Sentry, Ledger and Compass reports, missing one
blocks. Taste-only loop bookkeeping tagged assumed in ASSUMPTIONS.md. Cost bounded rounds may
stop delivery for explicit disposition rather than quietly reopen scope.

**Patron ruling:** Accepted with two amendments. (1) A Critical/High/Medium finding meets the
bar only with a concrete failure scenario. (2) Past the cap, unresolved findings become
follow-ups and the stage exits, but a deferred Critical/High keeps the stage NEEDS FIXES and is
reported to Bernstein; Lows and other below-bar items follow the charter routing, i.e. one
`noted, no ticket` line here unless Critical/High or broken behaviour.

- Pipeline Loop Discipline (`.cursor/rules/agent-pipeline.mdc:72-80`) requires exactly these
  three terms, with the frozen-scope closing sentence and a default cap of two; :92-93 keeps a
  deferred Critical/High at NEEDS FIXES.
- Frozen scope = Q1 + Q2 + Q3 + Q5 + Q6 rulings above (ticket 247-260); bar stricter than the
  :72 example (adds Medium) is Keel's call for a size:M integration proof and is accepted.
- Follow-up ticket threshold per charter (*Critical or High, broken behaviour, or user-asked;
  anything else is one line `noted, no ticket`*); Sentry/Ledger/Compass are the three
  challenger seats (charter roster :43-45), so a missing report blocks adjudication.

## Q8/12 - Ordering, gate plan and grill closure

**Keel asked:** Approve final ordering and gate plan and confirm shared understanding closes
grill at eight questions? Recommend order: Quill drafts from brief and conclusions -> read-only
speckit-analyze/brief alignment -> bounded plan challenge/freeze -> user-owned spec merge/Gate 1
-> Phase B pickup drift check -> fixture/host safety first -> seeded read operations -> real
import+OMDb causal proof -> real retry -> validation+disabled play -> entire IntegrationTests
regression -> applicable harness verification/review. No implementation or gates in this Phase 2
ask. Gate expectations: build, Roslyn, complexity, InspectCode, integration/full solution tests,
property/security/format and required ship-review gates reported by actual exits;
scope-empty/disabled skips never PASS, could-not-run blocks. DEV-313 changes are tests-only so
production mutation scope may be empty, but inherited DEV-311/DEV-20 ancestry at recon 21-23 can
make branch-wide gates nonempty: inspect actual configured scope, do not claim mutation SKIP from
ticket label, do not change thresholds or harness to obtain green. Pin incoming HEAD for
DEV-313-specific delta; distinguish inherited files and feature.json/harness modifications from
our additions. No web changes so no frontend implementation gate claim. If AC/gate forces
existing-file work under Q6, cite exact necessity, preserve unrelated changes, stop if it touches
src behavior outside approved tests-only remit for Patron disposition. No new ADR, vocabulary,
dependency or owner checkbox. Cost full regression and bounded review; final brief readiness is
drafting authorization only, not freeze/Gate 1/delivery/merge proof.

**Patron ruling:** Accepted as stated. Grill closed at 8/12 with shared understanding; no
`blocked: structural` item and no owner checkbox arises from Q1-Q8. Patron sets
`gate1: provisional` only after `/speckit-analyze` is clean and plan challenge is adjudicated.

- Gate honesty follows CLAUDE.md static-analysis gates (*never treat a skipped gate as a passed
  gate*) and Q7; mutation scope is read from configured `harness.yml` (recon 207-211), not the
  ticket label, and thresholds/harness are not edited to earn green (Q6 envelope).
- Inherited ancestry: branch is 11 commits ahead of main carrying DEV-311/DEV-20 (recon 21-23),
  so the DEV-313 delta is pinned from the incoming HEAD `01caa4d` and reported separately.
- Ordering puts fixture/host safety (Q2 tripwire, Q4 lifecycle) before any causal proof (Q3),
  and Gate 1 stays the user's merge (charter: never merge).

## Plan challenge - Adjudication refinements (under Q1-Q6, no new question)

**Keel asked:** Rule on plan-challenge refinements: new `ApiEndToEndFactory : WebApplicationFactory<Program>`
reproducing `ApiHostFactory` `CreateHost` / `ConfigureHostConfiguration` / `AddInMemoryCollection`
before `base.CreateHost` (no subclassing the sealed factory, no reliance on
`ConfigureAppConfiguration` for eager connection capture); `ConfigureTestServices` swaps
`TimeProvider` and `IProcessStarter` for exact supplied singletons with resolved-identity asserts;
per-test lifecycle stop prior host -> reset/migrate -> warm-up -> WireMock reset -> final stubs ->
start measured host; fresh no-tracking DbContext per poll at the unchanged 30s/50ms bound;
retry seed with `LastAttemptAt` null before enqueue, never clock-advance after ACK; explicit
`DisableParallelization` and failure-safe temp cleanup; exact 200 browse/details/facets and
204 empty-body watchlist assertions.

**Patron ruling:** Accepted as stated. Implementation refinements inside the frozen Q1-Q6 scope;
no new dependency, project, layer, schema, API shape or production change, so no §2.3 item and
no owner checkbox.

- Factory/config capture and DI identity follow recon 12.3 (629-633) and 12.4 (637-638); a new
  test-only factory sits in the Q6 tests envelope, and `IProcessStarter` replacement plus
  identity assertion strengthens the Q2 tripwire.
- Lifecycle, polling and retry-seed order follow recon 12.1 (615-617) and 12.2 (621-625) and
  realise FR-006 (spec.md:106, host stopped before resets) and Q4; nonparallel collection and
  failure-safe temp cleanup are FR-006/FR-007 (spec.md:106-107), not new scope.
- Exact 200/204 assertions implement frozen contracts per recon 12.5 (641-645) and FR-009
  semantic checks; Q1 envelope unchanged.
