# DEV-329 grill brief — OPEN, NOT FROZEN

Identity: F:/Dev/LamuFlix.worktrees/feature-329-spec; branch feature/329-spec; HEAD bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3.

Status: Q1-Q13 (including Q6a) are recorded; the Q4 resumption decision pass is complete. This remains an OPEN, NOT FROZEN brief. The next required action is Conductor routing the Q7/Q9 Gauge baseline refresh; Quill drafting, challenge, freeze and Gate 1 require their separate workflow. This is not a spec, plan, implementation authorization, or Gate 1 clearance. Full exchanges and cited Patron rulings are preserved append-only in CONCLUSIONS.md.

## Settled answers

### Q1 — prerequisite authority

Patron approved requiring the authoritative DEV-419 outbox ADR before design decisions or grill closure. Missing evidence was a recon prerequisite, not an owner structural checkbox. Basis: DEV-329:27-29; recon-DEV-329:24,83; specs/PRODUCT.md:18,23-24; charter section 2.3. No schema, Stage signature/serialization, or save/retry boundary was decided by Q1. Full question, answer, rationale and implications: CONCLUSIONS.md Q1.

### Q2 — supplied authority

Patron approved recon-DEV-329-3 as satisfying Q1 for continued grill. Design authority: docs/adr/0027-transactional-outbox-enrichment.md at cb6c6a780d77f41b32d69689f237bdce43d2562d, blob a000f752bae0d20425f6a52c0673b5906dca5de8. Same Postgres and transaction as movie write; ProcessedAt only after settled broker confirmation, never on uncertain outcome; at-least-once, one Api relay and retention are fixed ADR outcomes. Relay implementation remains DEV-420. Basis: recon-DEV-329-3:14-18,20-26,28-47,51-53; ADR:15,17,21-23; DEV-329:27-29; charter section 2.3. Schema is delegated by ADR:23; Stage contract and handler/DbContext/retry boundaries are absent and require explicit rulings here. Full exchange: CONCLUSIONS.md Q2.

Provenance caveats: no fetch; cited remote branch tip resolves, but main integration and original adoption commit were not established. Header says Accepted, date 2026-10-25, Ticket DEV-329. Revision is a correction commit. Earlier absence verdicts remain valid for their checked roots. Implementation pickup must verify prerequisite landing.

## Ticket-decided delivery scope

DEV-329:27-29 fixes outbox_messages schema and EF migration; IEnrichmentOutbox.Stage; ImportMovieFolder and RequestEnrichment stage before SaveChangesAsync; retain direct enqueue; no relay; unit and integration tests. Do not extend staging to other enqueue paths. No new ticket scope or constitution departure has been ruled.

## Open decisions and plan work

Current decisions: Q4 payload, Q5 schema, Q6/Q6a atomic save/failure boundary, Q7 exact proposed file set, Q8 test strategy, Q9 task dependency ordering, Q10 gate expectations, Q11 closing bar, Q12 proposed scope/state terminology and Q13 round cap are approved below. Prior Q4 recon request is satisfied by recon parts 4 and 5. Outstanding workflow prerequisite: Conductor obtains Gauge baseline for the changed Q7 scope before drafting/freeze, marking new files absent. No additional contract recon was needed on this resumption. No spec/plan/tasks drafting, freeze, gates or implementation occurred.

## Q3 — approved typed staging contract

Patron approved IEnrichmentOutbox in existing LamuFlix.Core.Ports with void Stage(EnrichmentRequested request). The Infrastructure persistence adapter is scoped and shares the movie repository DbContext. Stage is in-memory only: no cancellation parameter, save, commit, broker call or retry. The two named handlers stage before shared SaveChangesAsync and retain direct enqueue after success. No FeaturesRequested support, new dependency, project, layer, or generic abstraction. Extend the port whitelist explicitly. Basis: DEV-329:28; recon-DEV-329:31,35-36,68-75; ADR-0027:15 at cb6c6a780d77f41b32d69689f237bdce43d2562d; recon-DEV-329-3:34-42; charter section 2.3 item 2; specs/PRODUCT.md:18. Full exchange and implications: CONCLUSIONS.md Q3.

## Historical recon request before Q4 — satisfied by parts 4 and 5

needs recon: provide the existing EnrichmentRequested broker serialization/deserialization contract and reusable settings/payload conventions; exact EfMovieRepository save/retry/ChangeTracker.Clear flow and whether a staged Added outbox row survives each retry path; concrete shared-DbContext DI lifetimes for both named handler paths. Use only this worktree at the recorded HEAD with file:line evidence and existing tests. Existing recon identifies these surfaces but does not supply those details. Serialization/schema and retry decisions wait for this evidence; do not invent compatibility or replay behavior. Grill remains open. No freeze, spec/plan/tasks drafting, gates or implementation.

## Q4 — stored payload (approved)

Q4 approved: serialize the existing typed EnrichmentRequested once in Stage using adapter-local System.Text.Json Web defaults; store lossless UTF-8 Payload as bytea, no envelope/type/version field or MovieId reshaping. No publisher/consumer/relay edits. Serialization errors precede save and enqueue. Verify byte parity, consumer-default round trip, and Attempt preservation. Basis: recon-DEV-329-4:16-43; recon-DEV-329-5:3; publisher:17,75-85,155-156; consumer:36,335-346; ADR-0027:15,23; DEV-329:27-29; charter section 2.3 items 3,6. Full exchange: CONCLUSIONS.md Q4.

The earlier Q4 recon request is satisfied by recon-DEV-329-4 and recon-DEV-329-5, including scoped AddHandler evidence at part 5:23-43. Metadata save-path clearing behavior is now known and awaits its own ruling. Next Q5: exact outbox schema.

## Q5 — minimal outbox schema (approved)

Q5 approved: only outbox_messages with id bigint generated identity PK; payload bytea NOT NULL; created_at timestamptz NOT NULL assigned once by injected TimeProvider.GetUtcNow() at Stage; processed_at timestamptz NULL. Partial pending index (created_at,id) WHERE processed_at IS NULL. No other columns, FK, movie/attempt uniqueness, table changes, claim/retry/error metadata, relay or retention worker. Repeated staging creates distinct rows. Direct enqueue neither marks nor deletes rows. DEV-420 owns confirmed processing and retention behavior. Verify migration empty/upgrade/down, types/nullability/index, fixed-clock timestamp, initially-null ProcessedAt and repeated staging. Basis: DEV-329:27-29; ADR-0027:15,17,21-23; recon-DEV-329-3:28-32; charter section 2.3 item 3; AGENTS.md time rule; specs/PRODUCT.md:18.

Full exchange: CONCLUSIONS.md Q5. Next Q6: supported save path and failure/retry policy.

## Q6 — path A and failure boundary (approved; clarified by Q6a below)

Q6 approved: use shared scoped DbContext path A for the two ticket handlers only. Stage once, save once, direct enqueue after successful save. Stage error prevents save/enqueue; save failure/cancellation prevents enqueue; verify no partial commit from fresh Testcontainers context. Failed scopes are disposed rather than reused/retried. Post-commit direct publish error propagates with committed movie and unprocessed outbox retained. No Stage/handler retry or new transaction coordinator, and no EfMovieRepository production edits. Metadata paths B/C clear Added rows outside identityMap and are explicitly unsupported for staging; pickup guard requires a future explicit design/test decision before any metadata-diverging caller uses Stage. Patron disposition for that caveat: noted, no ticket (not a follow-up issue request). Basis: recon-DEV-329-4:47-66; recon-DEV-329-5:23-43; ADR-0027:15; DEV-329:27-29; charter section 2.3 item 6; specs/PRODUCT.md:18.

Full exchange: CONCLUSIONS.md Q6. Q6a below settles the stronger zero-row wording and governs failure assertions.

## Q6a — precise atomicity invariant (approved)

Q6a approved clarification supersedes only the overstrong zero-rows-on-any-exception wording in Q6. The movie write and outbox insert are atomic (both-or-neither); uncertain completion is not proof of rollback. Any save exception/cancellation propagates, skips direct enqueue, and disposes the failed scope without replay. Tests assert zero durable changes for deterministic pre-commit failures and explicit rollbacks; for RequestEnrichment this means the pre-existing movie remains in its original state and no new outbox row, not deletion of the movie. Future recovery remains DEV-420/sweeper. No provider-specific failure taxonomy is introduced. Basis: ADR-0027:15,17; recon-DEV-329-3:41,46; recon-DEV-329-4:49,60,66; DEV-329:28-29; AGENTS.md Testcontainers rule.

Full exchange: CONCLUSIONS.md Q6a. Q6a is authoritative for interpreting Q6 failure assertions. Q6 metadata-path disposition remains noted, no ticket.

## Q7 — approved proposed delivery file set (NOT FROZEN)

Q7 approves the exact delivery file set listed below and supporting DI/test/architecture adaptations (charter section 2.3 item 6). Exactly one EF-tool-generated AddEnrichmentOutbox migration pair and snapshot; never hand-edit generated files. HandlerRegistration, EfMovieRepository production code, broker/consumer, other handlers, API shape, web and package files stay outside delivery. Gauge must rerun baseline via Conductor on the existing files in this changed set, recording new files absent until created. Additional files need a new cited Patron ruling. Basis: DEV-329:27-29,60-99; recon-DEV-329:31-75; recon-DEV-329-4:81-104; recon-DEV-329-5:23-43; AGENTS.md generated-files rule. Patron used the word freeze in the answer, but the active Conductor instruction forbids freeze on this ask: this is an approved proposed file boundary only; no brief/plan freeze has occurred.

- src/LamuFlix.Core/Ports/IEnrichmentOutbox.cs
- src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs
- src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs
- src/LamuFlix.Infrastructure/Persistence/EfEnrichmentOutbox.cs
- src/LamuFlix.Infrastructure/Persistence/Records/OutboxMessageRecord.cs
- src/LamuFlix.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs
- src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs
- src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
- src/LamuFlix.Infrastructure/Persistence/Migrations/<EF timestamp>_AddEnrichmentOutbox.cs
- src/LamuFlix.Infrastructure/Persistence/Migrations/<same EF timestamp>_AddEnrichmentOutbox.Designer.cs
- src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs
- tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs
- tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs
- tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs
- tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs
- tests/LamuFlix.IntegrationTests/EnrichmentEndpointTests.cs
- tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs
- tests/LamuFlix.IntegrationTests/MigrationTests.cs
- tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs
- tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs
- tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs

Full exchange: CONCLUSIONS.md Q7. The EF timestamp is supplied by tooling at implementation, not guessed in Phase A.

## Q8 — approved test strategy

Q8 approved test strategy: extend existing handler unit suites for Stage -> Save -> enqueue exactly once with identical request, negative guards and scan/add/Stage/save/cancellation failures, post-save broker errors, existing metrics/status. Real Postgres Testcontainers in EnrichmentOutboxPersistenceTests proves Web-default payload parity/roundtrip, Attempt, injected-clock CreatedAt, null ProcessedAt, distinct repeated staging, no pre-save persistence, both handler atomic commits, controlled insert rollback and ambient rollback (request movie unchanged), and retained rows after broker error. MigrationTests proves empty/upgrade/down schema/types/nullability/pending index and no other-table delta; PersistenceCompositionTests proves scoped outbox shared context and isolation; ArchitectureTests allows only the new port. Existing fixtures/constructors adapt within Q7. Existing libraries only, no driver mocks for real translation/serialization, no speculative failure taxonomy, relay tests or B/C support tests. Basis: DEV-329:28-29; recon-DEV-329:47-69; recon-DEV-329-4:81-104; ADR-0027:15; Q4-Q7; AGENTS.md testing rules.

Full exchange: CONCLUSIONS.md Q8. No tests or gates executed in this grill.

## Q9 — dependency ordering (approved)

Q9 approved dependencies: (1) Conductor obtains refreshed Gauge baseline on Q7 existing-file scope before drafting/freeze; record new files absent. Later Phase B pickup verifies landed DEV-419 and main drift, not just the available cited branch object. (2) After separately authorized Phase A drafting/challenge/Gate 1 and Phase B pickup, implement typed port, outbox record/config/context, EF-generated migration, adapter and scoped DI with schema/wire/scoping/no-pre-save tests. (3) Wire both handlers, orchestration tests and all constructor/endpoint/repository fixture adaptations as a coherent buildable unit. (4) Complete real-DB atomicity/rollback/broker-failure, migration/composition/architecture proof before full verification and review. Brief-level ordering only; Quill authors spec/plan/tasks separately. No Gherkin/Reqnroll, new ADR, relay, metadata-retry refactor or web work. Basis: DEV-329:27-29,60-99; task-pipeline section 2.2 as cited by Patron; Q2-Q8; AGENTS.md verification/Testcontainers rules.

Full exchange: CONCLUSIONS.md Q9. This is not Quill plan/tasks drafting or authorization to implement.

## Q10 — gate expectations (approved, not executed)

Q10 approved gate expectations: Gauge refresh is baseline only. Separately authorized Phase A drafting is followed by read-only speckit-analyze and brief-vs-plan/tasks consistency review; challenge, brief/plan freeze and Gate 1 remain distinct. Later Phase B uses configured backend build/format, exact-changed-file Roslyn/complexity/InspectCode, unit/integration/architecture/full dotnet test, property tests, vulnerable packages and pre-PR mutation through a worktree-valid invocation without --since. Use live harness thresholds/kill-share/file-span/exclusions; do not hardcode, lower or hand-edit generated gate config. Report generated-file exclusions, actual executed pass, exit-2 scope-empty SKIPPED (scope-empty), opt-out SKIP, mutation exclusions-only N/A and unavailable gate Could not run separately; build alone is no substitute. No web/OpenAPI gates or regeneration for current unchanged API shape. All evidence is same-head and cannot replace three-axis review. Basis: AGENTS.md non-negotiables/verification; task-pipeline section 2.2 and charter section 2.3 cited by Patron; Q7,Q9. No gates executed now.

Full exchange: CONCLUSIONS.md Q10. No threshold numbers are inferred from old baseline output.

## Loop discipline — closing bar (Q11 approved)

Q11 chosen closing bar: verified in-scope Critical/High/Medium findings block review closure; Low/informational findings are recorded with disposition without forcing fix rounds. All Sentry Risk, Ledger Standards and Compass Spec axes are required for M, pinned to current HEAD and agreed scope, with concrete file:line/scenario evidence. Missing axis => blocked: missing axis <name>, never skip. Gates do not supersede retained findings or review clearance; merge bar and user merge remain separate. Plan challenge uses the same severity bar for concrete brief/spec/plan/tasks inconsistencies; unrequested abstractions and unrelated improvements are not blocking findings. Basis: standing task-pipeline section 2.2, charter section 2.3, grill-with-docs Loop Discipline and specs/PRODUCT.md:18, as cited by Patron.

Full exchange: CONCLUSIONS.md Q11. Review has not been run or cleared.

## Q12 — state terminology (approved)

Q12 approves the exact bounded scope below for eventual freeze only; active no-freeze instruction remains in force. Technical artifact terms: staged = in-memory tracking by Stage before SaveChangesAsync; durable = committed in Postgres with the movie write; unprocessed = ProcessedAt IS NULL. Direct enqueue never marks processed. This disambiguates ADR unstaged language without root CONTEXT.md edits. Metadata B/C remains noted, no ticket with pickup guard, not a newly requested issue. Basis: DEV-329:27-29; specs/PRODUCT.md:18; charter section 2.3; Q3-Q8; ADR-0027:15,17,22-23; recon-DEV-329-3:37.

Full exchange: CONCLUSIONS.md Q12.

## Loop discipline — proposed frozen scope wording (Q12 approved; NOT FROZEN)

DEV-329 delivers only typed EnrichmentRequested staging for ImportMovieFolder and RequestEnrichment before their shared path-A save; four-column outbox_messages with generated EF migration and pending index; scoped EF adapter and lossless Web-default payload; retained post-save direct enqueue; the Q7 exact files and Q8 tests proving wire compatibility, atomicity, schema, DI and architecture. Exclude relay/retention execution, direct-publish removal, other enqueue paths, metadata-save compatibility, new dependencies/layers, public API/web changes and other tables; metadata B/C is noted, no ticket with the pickup guard. anything else is a follow-up issue, not a finding in this round.

## Loop discipline — round cap (Q13 approved [assumed])

Q13 approved [assumed]: maximum two numbered full plan-challenge rounds and two full code-review/ship-review rounds. The answer accepts the full recommendation: Round 1 complete reports/fix list, Round 2 current-head verification; no silent reset for changed HEAD, renamed review, receipt/encoding/publication repair or new findings, and no extra immutable review to repair publication. If any blocking finding or missing axis remains at the cap, report blocked with exact evidence to Conductor and stop that cycle; do not claim clearance or start Round 3. Low/info disposition does not add a fix round; grill decision Q numbers do not consume review rounds. Basis: grill-with-docs Loop Discipline; task-pipeline section 2.2; charter section 3.1 13th-question assumed rule, as cited by Patron. Also recorded in ASSUMPTIONS.md. Brief remains unfrozen.

Full exchange: CONCLUSIONS.md Q13.

---

## Keel Phase A freeze — CURRENT STATUS (2026-10-25)

**FROZEN — brief and current spec/plan/tasks frozen for Phase A. Gate 1 is not claimed.**

Authority: Conductor separately authorized this freeze after Keel Round 2 current-artifact verification (task note DEV-329:191). This dated status supersedes earlier OPEN / NOT FROZEN and pending-baseline workflow statements only; those remain historical receipts. Gauge refresh is complete per DEV-329:125-171, measurement only. Q1–Q13 including Q6a, all cited Patron rulings and Q7's exact 21-file boundary remain unchanged.

Freeze criteria met: settled decisions and Q13 [assumed] are recorded; Quill drafting is complete; read-only speckit-analyze Round 2 is CLEAN (DEV-329:179); all three plan-challenge axes are present in the Round 1 adjudication; both accepted Medium blockers are closed on current working artifacts in Round 2 of 2 (DEV-329:191). T012 explicitly requires no Stage/SaveChangesAsync/EnqueueAsync for both missing-movie and guard/state-rejected RequestEnrichment. T017 remains preliminary, T018 refactor, and T019 requires final changed-HEAD build/format/full unit/integration/architecture tests after T018 before listed gates/review; plan Phase B wording agrees. No remaining above-bar blocker. Every Q11 Low/Info disposition in PLAN_CHALLENGE_ADJUDICATION.md stands, including unverified constitution citations and descriptive TreatWarningsAsErrors. No forced edits, new recon prerequisite, new decision or owner structural checkbox.

**Frozen scope (Q12, unchanged):** DEV-329 delivers only typed EnrichmentRequested staging for ImportMovieFolder and RequestEnrichment before their shared path-A save; four-column outbox_messages with generated EF migration and pending index; scoped EF adapter and lossless Web-default payload; retained post-save direct enqueue; the Q7 exact files and Q8 tests proving wire compatibility, atomicity, schema, DI and architecture. Exclude relay/retention execution, direct-publish removal, other enqueue paths, metadata-save compatibility, new dependencies/layers, public API/web changes and other tables; metadata B/C is noted, no ticket with the pickup guard. anything else is a follow-up issue, not a finding in this round.

Q11 closing bar is unchanged: verified in-scope Critical/High/Medium block; Low/Info receive explicit disposition; all three axes required. Q13 remains [assumed]: maximum two full plan-challenge rounds (now completed, no Round 3) and two full code-review/ship-review rounds, without silent resets. ASSUMPTIONS.md remains unchanged; its unfrozen wording records the earlier ruling time.

**Phase B STOP remains:** verify landed DEV-419 authority on main plus contract/file/handler/save/DI drift at pickup; the cited branch object alone is not landing proof. No implementation clearance, Gate 1 (including provisional), spec-PR merge, delivery review clearance, or merge bar is established by this freeze. The drafting receipt and earlier phase wording remain point-in-time history. No tests/gates, production edits, commit, PR, tracker writes, or main refresh performed for freeze.

### Freeze input identity and preservation receipt

F:/Dev/LamuFlix.worktrees/feature-329-spec; branch feature/329-spec; HEAD bf1cb3b9c25dbc81abb4306b9fd12b84e25801b3.

These are SHA-256 hashes of the eight inputs immediately before this append-only freeze. Spec/plan/tasks, ASSUMPTIONS.md and DRAFTING_RECEIPT.md are unchanged and frozen at these hashes. Brief/CONCLUSIONS/adjudication retain their complete input bytes as prefixes and receive only dated freeze appendices; they are untracked working artifacts, not content pinned by HEAD alone.

- `brief.md`: `02808b8de6085ba51e619b806c28604ec7f38170bb5ecde3e5c1e34519f5caa5`
- `CONCLUSIONS.md`: `a4fffa0dcdc724545abb950e50c923f61c0eedc288d19d746d7a36f35ff68823`
- `ASSUMPTIONS.md`: `83907da966ef87351120482c2d80181f51b09d1c23e39592a7b98dcf8fc805eb`
- `spec.md`: `07dc7cc7858f53be6f57db6458bb8dd502f9866c182ef3b949cdbd907944dd11`
- `plan.md`: `49f22cc13b86f492ff640a53cfb568a277b2901d4c82b4768a6c192ceb089ea9`
- `tasks.md`: `c72bc25aac36bc28a74fede497bc875254cb9747393eb1e6f6f38f1f6e556986`
- `DRAFTING_RECEIPT.md`: `720781a807fc69e5d0c7ed205fa0f3d28d0d3325f423062a315351025ffa80d7`
- `PLAN_CHALLENGE_ADJUDICATION.md`: `2a99685d15e66c9c11894efcb7fe4e040ef19483f54174029d292346a307e922`
