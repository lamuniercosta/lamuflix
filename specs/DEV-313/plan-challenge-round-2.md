# DEV-313 Plan Challenge adjudication and Round 2 fix list

Worktree: F:/Dev/LamuFlix.worktrees/feature-313-spec
Branch: feature/313-spec
Baseline: 01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc
Evidence: all three challenge notes, recon-DEV-313 sections 11-12, current brief/spec/plan/tasks.
Stage: NEEDS FIXES; no freeze receipt. No source edits, commits or gates.
This adjudicates the Round 1 reports and supplies one Round 2 correction list; it is not another immutable review or a consumed third round.

## Decisions

- Sentry H1 MODIFY/ACCEPT fix: T004 places final stubs before warm-up/reset. Follow the verified reset-then-re-stub precedent (recon 615-617), rather than asserting externally unverified WireMock source semantics (616). The current ordering cannot guarantee a matching measured mapping. Reinstate final per-test stubs after reset.
- Sentry H2 ACCEPT: retained LastAttemptAt plus strict claim predicate can refuse the first delivery, which is ACKed (recon 621-625). A later clock advance cannot recover that delivery. Make the successful retry seed claimable before POST.
- Sentry M3 MODIFY/ACCEPT fix: configuration cannot replace the registered clock. Explicit test-service replacement and resolved-instance assertion are required. Reject the report implication that PlaybackEndpointTests proves clock injection: recon 638 confirms no clock registration in ApiHostFactory; it documents the actual TryAddSingleton defaults.
- Sentry M4 ACCEPT: seeding context tracking is not fresh persisted completion evidence (recon 625; completion signal 463-473). A fresh scoped DbContext with a no-tracking database query on each iteration closes that ambiguity.
- Sentry M5 ACCEPT: the asserted starter must be the host-resolved starter instance, registered explicitly without copying the precedent catalog substitute (recon 637). Q2 permits only the process-start tripwire.
- Sentry M6 MODIFY/ACCEPT fix: sealed factory and eager capture are verified (recon 629-633). Use a new direct WebApplicationFactory implementation reproducing the host-config pattern. Dismiss the proposed ConfigureAppConfiguration wrapper as unproven for this capture; it is only verified for lazily bound playback settings.
- Sentry L7 MODIFY: explicit DisableParallelization=true implements the already mandatory nonparallel collection (spec FR-006, brief 52). Include as requirement precision, no new scope or follow-up ticket.
- Sentry L8 ACCEPT: failure-safe temp cleanup is necessary to meet FR-007 and spec edge case 94 when setup/assertions fail. Include cleanup after host stop/dispose, including a partially started host.
- Ledger S1 ACCEPT: exact 200/204 statuses, empty watchlist bodies and browse counts/shapes are real existing contracts (recon 641-645); NULLS LAST is already required by plan 37 and recon 48. IsSuccessStatusCode alone permits a contract regression.
- Ledger S2 DISMISS: below-bar omission of II/VIII disposition prose is not evidence of a constitution departure; no new handler/decorator/domain term is planned (brief 13,48,71; plan 36,43,75). No concrete failure scenario. Noted, no ticket.
- Ledger S3 DISMISS as a required artifact fix: repository conventions remain binding; their absence from each task does not authorize a second assertion library or sync-over-async. Verified AAA/Shouldly/cancellation precedent is recon 651; the full cited rule sources were explicitly not independently verified (648-653). No authored implementation violation or concrete failure scenario. Noted, no ticket; do not add unsupported mandatory framework/rule details.
- Ledger S4 DISMISS: task identity need not equal execution order. T010-before-T009 is explicitly correct at tasks 72/84, and validation covers both US1 and US3 as its text already states. Renumbering adds reference churn without correcting execution or coverage. Noted, no ticket.
- Compass 1 DISMISS as finding: brief 87 already requires safety first and Q8 settles the ordering; T005 placement is correct. Noted, no ticket.
- Compass 2 DISMISS as finding: generated identities already required by spec FR-009 and brief 41. Exact replacements below repeat generated-ID assertions as part of accepted contract fixes; no separate blocker. Noted, no ticket.
- Compass 3 DISMISS as finding: parallel markers list opportunities, not mandatory parallelism; safety first is permitted. Noted, no ticket.
- Compass 4 DISMISS as finding: Q3/brief 31 require actual OMDb request evidence on import/retry only, not negative request assertions on all read-only endpoints. Noted, no ticket.

## Round 2 exact numbered fix list for Quill

All locations refer to the original pre-edit artifacts read for this adjudication. Match task IDs and full original lines after earlier replacements shift line numbers. Preserve IDs, execution order, all nine operations, the ten-file envelope, inherited changes, and gate/round semantics. No spec.md replacement is required.

1. M3/M6: Replace tasks.md:11 (T001) with:
> - [ ] T001 [US1] Create ApiEndToEndFactory.cs in tests/LamuFlix.IntegrationTests as a new WebApplicationFactory<Program> implementation reproducing the existing ApiHostFactory host/configuration, service-provider validation and health-check-stub patterns; do not subclass the sealed ApiHostFactory. In CreateHost, apply fixture ConnectionStrings:DefaultConnection, RabbitMQ host/port/runtime credentials, loopback Omdb:BaseUrl, non-secret sentinel Omdb:ApiKey, Enrichment:ClaimLease=00:00:01, RabbitMq:RetryDelay=00:00:02 and Features:LocalPlay=false through builder.ConfigureHostConfiguration with AddInMemoryCollection before base.CreateHost and eager persistence registration. Preserve production persistence/metadata-before-consumer order. In ConfigureTestServices, remove existing TimeProvider registrations and register the supplied test TimeProvider singleton; before measured requests assert the host resolves that exact instance. Retain the real publisher, consumer, repository, scanner and metadata provider.

Also insert after plan.md:75:
> ApiEndToEndFactory reproduces the ApiHostFactory patterns in a direct WebApplicationFactory<Program> implementation because ApiHostFactory is sealed. Fixture settings are applied in CreateHost via ConfigureHostConfiguration/AddInMemoryCollection before base.CreateHost and eager persistence registration; ConfigureAppConfiguration alone is not accepted as proof of that ordering. ConfigureTestServices replaces the TimeProvider and playback-tripwire registrations with the supplied instances; host-resolution identity is asserted. The existing health-check stub and service-provider validation are preserved. Basis: recon-DEV-313 12.3-12.4; brief plan-challenge refinements.

2. H1/M4: Replace tasks.md:12 (T002) with:
> - [ ] T002 [US1] Create ApiEndToEndFixture.cs in tests/LamuFlix.IntegrationTests with owned Postgres/RabbitMQ/WireMock instances and migrate/reset/cleanup support. Before every measured host starts, with any prior host stopped/disposed, reset/migrate the DB and broker topology, start the WireMock server, perform provider warm-up using the existing probe pattern, reset WireMock, then install the final deterministic per-test stubs. No reset occurs between final stub installation and measured requests; later infrastructure resets run only after host stop/dispose.

Replace tasks.md:14 (T004) with:
> - [ ] T004 [US1] Create ApiEndToEndTestBase.cs in tests/LamuFlix.IntegrationTests with the shared T002 lifecycle: stop/dispose any prior host, prepare/reset/migrate backing infrastructure, warm up the provider, reset WireMock, install final per-test stubs, then start a fresh measured host. Warm-up requests never count as endpoint proof. Seed deterministic rows through MovieCatalogSeed/DbContext; dispose the seeding context. Poll persisted status using a fresh scoped DbContext and a no-tracking database query on every iteration, including the final assertion, rather than reusing a tracked seed entity. Retain the real elapsed-time 30-second deadline, 50ms interval, final failing assertion and secret-free diagnostics. Advance the supplied host-resolved TimeProvider only when lease expiry is needed, never to shorten the polling bound or to recover an already ACKed refused claim. Stop/dispose the host before infrastructure reset/cleanup on both success and failure.

3. L7: Replace tasks.md:13 (T003) with:
> - [ ] T003 [P] [US1] Create ApiEndToEndCollection.cs in tests/LamuFlix.IntegrationTests as an isolated collection with CollectionDefinition DisableParallelization=true; all new ApiEndToEnd test classes use this collection and its owned fixture.

4. M5: Replace tasks.md:24 (T005) with:
> - [ ] T005 [US5] Implement ApiEndToEndPlaybackTests.cs with Features:LocalPlay=false and real HTTP/Postgres. Through ConfigureTestServices remove existing IProcessStarter registrations, register a recording/substituted IProcessStarter singleton as the safety tripwire, and assert the host resolves that exact instance before POST play. Assert 403 application/problem+json and zero Start calls on that instance. Preserve the real catalog and other infrastructure; no enabled-play scenario, process execution or src change.

5. S1: Replace tasks.md:25 (T006) with:
> - [ ] T006 [P] [US1] Implement ApiEndToEndBrowseTests.cs over HTTP with deterministic persisted seeds: assert 200 OK and the frozen Items/TotalCount response shape, matching combined filters, stable pagination across boundaries with exact total counts, and stable sorting with ties and NULLS LAST. Assert returned generated IDs and values rather than assumed identities.

6. S1: Replace tasks.md:26 (T007) with:
> - [ ] T007 [P] [US2] Implement ApiEndToEndLibraryTests.cs over HTTP: details return 200 with the frozen Id/Title/Path/Format/Metadata scalar shape and unknown IDs return 404; genres/people return 200 arrays of Id/Name values derived from persisted metadata. POST and DELETE watchlist each return 204 NoContent with an empty body and persist the corresponding transition, verified through a fresh DbContext or subsequent appropriate HTTP read. Assert generated IDs and semantic values; do not accept generic success status alone.

7. L8: Replace tasks.md:38 (T008) with:
> - [ ] T008 [US3] Implement ApiEndToEndImportTests.cs success path: real temp <Title> (<Year>) folder with one tiny .mkv/.mp4 file, 202 with relative Location using the generated persisted movie ID and an empty body, persisted movie, bounded fresh-DB poll to Enriched, GET details confirmation, and matching measured WireMock OMDb request evidence. Register cleanup as soon as the folder is created; on success, host-start failure or assertion failure, stop/dispose any created or partially started host before deleting the owned temp folder in failure-safe teardown/finally cleanup. No playable media or process execution.

8. H2: Replace tasks.md:50 (T010) with:
> - [ ] T010 [US4] Implement ApiEndToEndEnrichmentTests.cs: persist a retry-eligible movie with LastAttemptAt=null before POST so the first Pending claim is eligible without waiting for lease expiry; use the existing rehydration/seed patterns without production changes. Assert 202, relative Location using the generated movie ID and an empty body, then bounded fresh-DB poll to Enriched, GET details confirmation and matching measured WireMock request evidence. Never rely on clock advancement after a refused claim has been ACKed. Include the existing 409 ineligible and 404 missing contracts.

## Closing receipt

No freeze until Quill applies these eight replacements/insertions and Keel independently rereads for brief alignment and accepted-finding closure. No Gate 1, implementation, merge or runnable-gate proof is inferred. Existing Q1-Q8 remain authoritative; Patron accepted the refinements and recorded the append-only ruling in CONCLUSIONS.md, section Plan challenge - Adjudication refinements, commit f266ade (read back). The decisions are recorded in brief.md before artifact corrections. No new owner checkbox, production behavior, dependency or test-project file is introduced.

