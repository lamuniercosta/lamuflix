# DEV-313 - Frozen grill brief

## Status and evidence

Phase 2 grill closed by Patron at Q8 of a maximum 12 questions, with shared understanding. Ready for Quill to draft the spec, plan and tasks. This is not plan freeze, clean analysis, Gate 1, implementation authorization or merge evidence. No structural blocker or owner checkbox was identified.

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-313-spec`
- Branch: `feature/313-spec`
- Incoming HEAD and DEV-313 delta baseline: `01caa4d1b3e4e4ba7674abfd91bb6af2f3be21bc`
- Authoritative facts: canvas note `recon-DEV-313`, especially sections 10 (complete ticket) and 11 (runtime path); intake note `DEV-313`.
- Complete exchanges and cited Patron rulings: [CONCLUSIONS.md](CONCLUSIONS.md), Q1-Q8. This brief carries every answer and the final plan decisions; conclusions preserve the full question/answer record.
- Recon records 11 inherited commits containing DEV-311/DEV-20 work. Pre-existing `.specify/feature.json` and `harness.yml` modifications are preserved and excluded from DEV-313 authorship.
- This ask makes documentation changes only, with no commits, source edits or gate execution. Patron identified no taste rulings requiring `ASSUMPTIONS.md`. No new domain vocabulary or ADR is needed.

## Frozen scope

Add real-host integration proof inside the existing `tests/LamuFlix.IntegrationTests` project for all nine mapped API operations. The ticket's five Scope & Technical Design bullets are mandatory scenarios within the comprehensive endpoint envelope, not a limitation of its acceptance criterion. Basis: ticket Overview and AC, recon lines 247 and 259-260, endpoint map 126-130, Patron Q1.

| Operation | Required seam evidence |
| --- | --- |
| GET `/api/movies` | Real Postgres filtering, combined filters, pagination boundaries, sorting, ties and null placement through HTTP |
| GET `/api/movies/{id}` | Seeded success and 404; persisted enrichment visible after import/retry |
| POST `/api/movies/import` | Real scanner and filesystem; 202, relative Location and empty body; persisted movie and causal enrichment proof |
| POST `/api/movies/{id}/enrichment` | Eligible retry: 202 and Location, causal enrichment completion; 409 and 404 contracts |
| POST `/api/movies/{id}/play` | `Features:LocalPlay=false`; 403 ProblemDetails; zero process-start calls |
| POST `/api/movies/{id}/watchlist` | HTTP mutation and persisted watchlist transition |
| DELETE `/api/movies/{id}/watchlist` | HTTP mutation and persisted watchlist transition |
| GET `/api/genres` | Facet values derived from seeded real persisted metadata |
| GET `/api/people` | Facet values derived from seeded real persisted metadata |

All nine operations use a WebApplicationFactory-backed host configured with real Postgres, RabbitMQ and WireMock. Actual OMDb requests are required on import and retry paths; read-only operations do not manufacture metadata requests. Invalid import and browse inputs prove 422 `application/problem+json` with field-keyed errors. Preserve the existing double-based endpoint tests and the focused EF/provider/consumer suites. Do not duplicate their entire matrices.

Frozen scope is Q1-Q6: additive real-host coverage of all nine operations, five mandatory scenarios, causal import/retry-to-OMDb evidence, tests-only disabled playback, and the file envelope below; anything else is a follow-up issue, not a finding in this round.

## All grill answers

1. **Endpoint envelope — accepted.** All nine mapped operations are covered end-to-end, additively; five named bullets remain mandatory. Narrowing to five would drop the explicit AC. No owner checkbox. Basis: recon 247-260, 126-130; Constitution IX :288, :299-301; Q1.
2. **LocalPlay / Process.Start — accepted deliberate tests-only ruling.** Keep LocalPlay false; real HTTP and Postgres; assert 403 problem JSON and zero `IProcessStarter.Start` calls. The substituted starter is a safety tripwire only. No enabled-play scenario or production playback changes. This preserves the existing gate and is not a Constitution IX departure. Basis: ticket line 255, recon 64-70 and 162-173; Q2.
3. **WireMock proof — accepted and frozen.** At least one import success and one retry success must traverse HTTP -> EF/Postgres -> real RabbitMQ publisher -> API-host `EnrichmentConsumer` -> real `OmdbMetadataProvider` -> WireMock -> persisted `Enriched`. Bound DB polling, then GET details and matching WireMock request evidence. A manually invoked handler/provider/consumer is not endpoint evidence. WireMock is configured for all operations; actual OMDb requests are asserted on import/retry only. No legacy Worker or unwired sweep. Basis: recon 11.1, 11.3, 11.5 and AC 260; Q3.
4. **Harness lifecycle — accepted with precision.** Compose existing infrastructure in new factory/fixture classes. Supply configuration before host build. ClaimLease is 1 second; RetryDelay is 2 seconds. Own nonparallel collection; stop/dispose host before resetting DB, broker topology or WireMock logs. Warm up using existing probe precedent, then reset WireMock; warm-up is not proof. Polling retains a real elapsed-time 30-second deadline, 50ms interval and final failing assertion. An injected TimeProvider is registered before build and used only to advance lease expiry, never to shorten the polling bound. Basis: recon 108-124 and 11.4-11.5; Q4.
5. **Scenario matrix and filesystem — accepted.** Deterministic hand-authored seeds and semantic JSON/DTO/header/status assertions; generated IDs rather than assumed identities. Import uses a real temporary `<Title> (<Year>)` directory with one tiny supported `.mkv` or `.mp4` file, deleted after host stop. No playable media or process execution. Real filesystem scanning satisfies the integration seam and does not depart from the constitution. No Verify, AutoFixture or Faker reference added. Basis: recon 41-75, 125-133 and 11.6; Constitution V and IX; cited test-assertions rule in Q5.
6. **Files — accepted with amendment.** Ten additive files below. An existing-file edit forced by a gate or AC is in scope once the exact necessity is cited under Patron's Q6 ruling. Every other existing-file edit or production defect stops for a cited Patron ruling. Q8 further requires disposition before production behavior changes outside the tests-only remit. Preserve inherited work and unrelated modifications. Basis: recon 31-39, 106-160, 179 and 11.7; Q6/Q8.
7. **Loop terms — accepted with amendments.** Critical/High/Medium findings require a concrete failure scenario to meet the bar. Two formal plan-challenge rounds and two delivery-review/remediation rounds are separately counted. At cap, unresolved findings become follow-ups and the stage exits; a deferred Critical/High retains `NEEDS FIXES` and is reported to Bernstein. Below-bar items use one `noted, no ticket` line unless Critical/High, broken behavior or user-requested. All three Sentry/Ledger/Compass reports are mandatory for size:M delivery adjudication. Basis: cited agent-pipeline :72-80, :92-93 and charter routing in Q7.
8. **Order, gates and closure — accepted.** Follow the ordering and gate plan below. No gate claim is inferred from tests-only labeling because inherited production ancestry may affect scope. Patron confirms closure at eight questions, no structural blocker and no owner checkbox; `gate1: provisional` can only be set after clean speckit-analyze and adjudicated plan challenge. Gate 1 remains the user-owned spec merge. Basis: recon 21-23, 207-211 and Q2-Q7; Q8.

## Implementation approach and constraints

Reuse the existing ApiHostFactory composition, PostgresFixture, RabbitMqFixture, MovieCatalogSeed and MetadataProviderProbe/EnrichmentConsumerTests patterns. Configure the running host's persistence, publisher, consumer and provider; do not replace scanner, repository, queue or metadata provider with doubles. The playback starter tripwire is the Q2 exception for preventing an OS side effect.

Before host build, inject the fixture's `ConnectionStrings:DefaultConnection`, RabbitMQ host/port and runtime fixture credentials, loopback `Omdb:BaseUrl`, a non-secret sentinel `Omdb:ApiKey`, `Enrichment:ClaimLease=00:00:01` and `RabbitMq:RetryDelay=00:00:02`. Supply other valid options using existing host/probe patterns. Do not hardcode credentials or connection strings or use the public OMDb service. Host configuration is required before eager persistence registration captures its connection string. Preserve production registration order: persistence and metadata provider before RabbitMQ consumer registration.

Use independently owned container instances and a nonparallel collection for this suite. Initialize/migrate and reset infrastructure, install deterministic stubs, then start a fresh host. Stop and dispose the host before reset/cleanup to prevent consumer races. Keep health-check replacement precedent; it is not evidence of real backing services. Actual persisted metadata plus matching WireMock requests proves that the host consumer runs, retiring the runtime caveat in recon 11.9; a hosted-service descriptor assertion alone does not prove execution.

Set stub title/year from the import folder name. Poll persisted status with the existing bounded pattern, assert final state on timeout, and provide useful failure diagnostics without secrets. Do not wait for an enrichment status route or sweep: neither exists. Confirm terminal success through the details endpoint. Keep tests deterministic and assertions at the missing HTTP/infrastructure seam.

## Files touched

New production-test files, all under `tests/LamuFlix.IntegrationTests/`:

- `ApiEndToEndFactory.cs`: pre-build configuration and host composition.
- `ApiEndToEndFixture.cs`: owned backing infrastructure, reset/cleanup and stubs.
- `ApiEndToEndCollection.cs`: isolated nonparallel collection.
- `ApiEndToEndTestBase.cs`: shared lifecycle, seeding and bounded observations.
- `ApiEndToEndBrowseTests.cs`: HTTP filter/paging/sort proof.
- `ApiEndToEndLibraryTests.cs`: details, facets and watchlist persistence proof.
- `ApiEndToEndImportTests.cs`: real scanner/import-to-OMDb proof.
- `ApiEndToEndEnrichmentTests.cs`: retry-to-OMDb proof and 409/404.
- `ApiEndToEndPlaybackTests.cs`: disabled LocalPlay and process tripwire.
- `ApiEndToEndValidationTests.cs`: HTTP 422 ProblemDetails contract.

Keel owns this brief; Patron owns append-only conclusions. Quill drafts feature artifacts under `specs/DEV-313/` from these decisions. No planned `src/`, schema/migrations, project/solution/CPM, existing tests, harness, web or generated-contract edits. Q6/Q8 govern any forced exception; do not silently widen the envelope.

## Test strategy and gate expectations

Verify common success paths and meaningful error paths against real infrastructure, then run the whole IntegrationTests regression and required solution checks. Build, Roslyn, complexity, InspectCode, tests, property tests, package/security checks, format and required ship-review gates retain their configured thresholds and actual scope.

No gates ran during this grill. Report actual outcomes: PASS only when earned; scope-empty exit-2 is `SKIPPED (scope-empty)`; configured opt-out is `SKIP`; mutation exclusions are N/A only when the script reports that result. None is PASS. A required gate that cannot run blocks. No threshold or harness edits to obtain green, and no substitution of plain build for analyzer verification.

DEV-313's planned code additions are tests-only, but inherited DEV-311/DEV-20 work may make branch-wide production mutation and other gates nonempty. Establish the actual configured diff/scope and keep branch-wide results distinct from the delta from incoming HEAD. No unsupported mutation flags or frontend gate claims. Review the diff rather than treating the entire inherited tree as new DEV-313 work.

## Task ordering and review discipline

1. Quill drafts spec, plan and tasks from this brief and the eight conclusions.
2. Keel runs read-only speckit-analyze and checks plan/tasks against the brief. One numbered fix list per round, followed by independent reread. Any new decision is recorded in the brief before an artifact decision change.
3. Run bounded plan challenge; adjudicate and freeze. Clean analysis, freeze and `gate1: provisional` are distinct receipts. The user merges the spec PR; no seat merges or enables auto-merge.
4. After Gate 1, run Phase B pickup drift analysis against main and reconcile changed assumptions/tasks before implementation.
5. Implement fixture lifecycle, pre-build configuration and playback safety first; then seeded read/library operations.
6. Add real import-to-OMDb proof, then retry-to-OMDb proof, then validation and disabled-play scenarios.
7. Run integration regression and applicable verification; collect all delivery review axes and adjudicate against the fixed diff.
8. Publish required findings/summary on the PR and read back evidence. Remediation replies to findings and resolves threads. End delivery at `awaiting-merge`, never merge.

### Loop Discipline

Closing bar: no open accepted Critical, High or Medium finding with a concrete failure scenario, plus earned required gates. Apply Q7's explicit cap disposition: unresolved items move to follow-up at cap, while a deferred Critical/High keeps `NEEDS FIXES` and is reported to Bernstein. Below-bar routing follows Q7; unnecessary abstraction is follow-up rather than an automatic fix commit.

Round cap: two formal plan-challenge rounds and two formal delivery-review/remediation rounds, separately counted. Never issue a third or replacement immutable review to repair transport. Missing Sentry, Ledger or Compass delivery report is `blocked: missing axis <name>`, never a skip.

Frozen scope is the nine-operation real-host integration suite with five mandatory ticket scenarios, causal import/retry evidence, disabled-play safety and the Q6/Q8 file boundaries; anything else is a follow-up issue, not a finding in this round.

If a required fact is absent from recon, report `needs recon: <question>` to Bernstein and stop. No independent searches or measurements by Keel. Only a ticket-delivery contradiction or Patron-approved necessary constitution departure escalates as an owner checkbox; none arose in this grill.

## Plan challenge adjudication refinements - 2026-10-08

Patron accepted these implementation refinements inside frozen Q1-Q6, with no new section 2.3 item or owner checkbox; the append-only ruling is in CONCLUSIONS.md, section Plan challenge - Adjudication refinements, commit f266ade. Authoritative factual basis: recon-DEV-313 sections 12.1-12.5, lines 615-645. Keel owns the exact Round 2 correction list in plan-challenge-round-2.md; Quill applies it to plan.md and tasks.md only.

- Factory: ApiEndToEndFactory directly implements WebApplicationFactory<Program> and reproduces ApiHostFactory patterns; it cannot subclass the sealed factory. Apply all fixture settings in CreateHost through ConfigureHostConfiguration/AddInMemoryCollection before base.CreateHost and eager persistence capture. Preserve service-provider validation, health-check stub and production registration order. ConfigureAppConfiguration alone is not verified evidence for this capture.
- DI identity: ConfigureTestServices removes existing TimeProvider and playback IProcessStarter registrations and supplies the exact singleton instances asserted from host services. Keep LocalPlay disabled and the real catalog/scanner/repository/publisher/consumer/provider.
- Lifecycle: stopped/disposed prior host -> reset/migrate backing infrastructure -> provider warm-up -> WireMock reset -> final per-test stubs -> start fresh measured host. No reset between final stub installation and measured requests. Warm-up never counts as proof.
- Completion: dispose the seed context; every persisted-status poll and final assertion uses a fresh scoped DbContext with a no-tracking DB query. Preserve the real 30s elapsed-time deadline, 50ms interval and secret-free diagnostics. Clock advancement remains lease-only.
- Retry: persist a retry-eligible seed with LastAttemptAt=null before POST/enqueue so the first Pending claim is eligible. Advancing the clock after a refused claim is ACKed is not recovery evidence.
- Isolation/cleanup: explicit CollectionDefinition DisableParallelization=true and the shared owned collection; register temp-folder cleanup immediately upon creation, stop/dispose any created or partially started host before deletion even on setup/assertion failure.
- Contracts: 200 browse with Items/TotalCount, matching filters, exact totals, stable pagination/ties/NULLS LAST; 200 details with the frozen scalar fields and 404 missing; 200 Id/Name facet arrays; 204 empty watchlist mutation bodies and fresh persistence proof. Generated IDs and semantic values remain mandatory.

All 16 reported items adjudicated: H1, M3 and M6 modified to the verified recon mechanisms; H2, M4, M5 and S1 accepted; L7 modified as FR-006 precision and L8 accepted as FR-007 cleanup. S2, S3 and S4 dismissed as below-bar artifact bookkeeping without concrete failure, noted, no ticket; conventions remain binding and task IDs/order stay unchanged. Compass observations 1-4 dismissed as findings, noted, no ticket: Q8 safety-first order already governs, generated IDs are already mandatory, parallel markers are opportunities, and Q3 requires OMDb evidence on import/retry only.

Status: NEEDS FIXES. This is adjudication of Round 1 plus the single Round 2 fix list, not an additional immutable review. Quill correction and independent Keel reread precede freeze; no freeze, Gate 1, implementation, merge or gate-pass receipt is issued. The existing two-round plan/delivery caps and Q7 cap dispositions remain unchanged.
