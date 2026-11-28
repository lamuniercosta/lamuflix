---
description: "Task list for DEV-550 HTTP client outbound address guard and architecture rules"
---

# Tasks: HTTP client outbound address guard and architecture rules

**Input**: Design documents from `specs/DEV-550/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q6), `CONCLUSIONS.md`; Maestri notes `DEV-550`, `recon-DEV-550`; `specs/PRODUCT.md`.

**Status**: Draft — Quill drafting only. Gate 1 / T000, Build, and runtime receipts remain pending. Quill does not declare PLAN FROZEN.

**Tests**: Required (`brief.md` Verification / Q1–Q6; FR-007–FR-011; SC-001–SC-007). TDD optional. Ticket-required guard, request behaviour, and DI coverage under IntegrationTests; two FsCheck address-policy properties under UnitTests Adapters (constitution IX Test Pyramid; brief Q5 amendment / `CONCLUSIONS.md` analyze round 1 item 1; path and harness discovery confirmed at T001); ArchitectureTests for NetArchTest rules. Match each project's existing assertion and mocking conventions. No new test framework/dependency. No live provider requests for automated cases.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Edit only the authorized envelope in `plan.md` / brief File envelope; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; serialize with DEV-551 on `SubtitleProviderServiceCollectionExtensions.cs`; never lower harness thresholds; no explanatory comments on changed C# beyond AGENTS exemptions; range citations belong in spec and PR body (Q3), not XML prose. Match each project's existing assertion and mocking conventions (UnitTests/IntegrationTests: Shouldly + NSubstitute per constitution IX Test Pyramid; ArchitectureTests: Shouldly wherever an equivalent exists, xUnit `Assert` only where none exists, per constitution IX Test Pyramid). Cite constitution IX by section name (Test Pyramid), not by line number. **Static-analysis completion (constitution Static-Analysis Gates; `plan.md` Stack / API / workflow check)**: mark each new or materially changed C# task complete only after scoped evidence that `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, and `./scripts/run-jetbrains-inspectcode.ps1` pass on that task's changed `.cs` (no `-All`); retain T013 as the ArchitectureTests + three-gate checkpoint and T015 as the exact-head full sweep. Otherwise-unassigned implementation tasks are owned by the generic Build lane. Named seats own Spec PR, pickup, refactor, gates, review, ship, and receipts per `task-pipeline` (T017 operator evidence: **Rigger**).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: After plan challenge clearance and Patron `gate1: provisional` (if applicable), Bernstein coordinates; Rigger prepares/opens the Spec Kit PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup: DEV-551 serialize recheck + envelope inventory; Keel reanalysis) — blocks production edits
Phase 2   T002–T006     (Foundational: policy, options, defaults, three registration invokes, InternalsVisibleTo) — blocks US1/US2 only
Phase 3   T007–T010     (US1/US2: DI discovery + behaviour + UnitTests properties; subtitle opt-in if needed)
Phase 4   T011–T013     (US3: NetArchTest rules + fixtures) — independent of Phase 2; parallel with Phase 3 after T001
Phase 5   T014–T018     (Cog refactor + Gauge exact-head gates; Size M review with exact-head re-proof; receipts after verification not review; delivery PR range/citation handoff) — T016 and T017 independently runnable after T015
```

Ordering per brief Task order / Q6. Architecture stream (Phase 4) is independent of the guard foundation and of InternalsVisibleTo. Within Phase 5, T016 (review) and T017 (receipts) both depend only on T015 and are independently runnable after T015; receipts do not wait on Size M review closure. If a review fix changes the delivery candidate, both receipts MUST cover that delivery candidate. Exact-head gate evidence must be preserved or re-proven after any review fixes. No dependency cycle.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-550 Spec Kit PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-550 in `specs/DEV-550/CONCLUSIONS.md`. Receipt only. T001–T018 MUST NOT proceed without verified merge (`brief.md` Task order; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon and DEV-551 serialize recheck (blocks production edits)

**Purpose**: Confirm shared-file status with DEV-551, inventory exact authorized paths. Read-only.
**Checkpoint**: Written pickup receipt cites DEV-551 ordering/recheck for `SubtitleProviderServiceCollectionExtensions.cs` and authorized file paths; Keel read-only reanalysis clear. Any authorized drift amendment is recorded as a **separate follow-up** and completed before production edits — T001 itself does not edit production files.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming serialization with DEV-551 for `src/LamuFlix.Infrastructure/Adapters/SubtitleProviderServiceCollectionExtensions.cs` (either order allowed; recheck the shared file at pickup per `brief.md` Task order step 1). Inventory exact paths for the three registration extensions, ArchitectureTests + Valid/Violating fixtures, IntegrationTests homes for new guard/DI coverage, and the UnitTests property-file path plus harness discovery convention for the two FsCheck address-policy properties (planned `tests/LamuFlix.UnitTests/Adapters/OutboundConnectAddressPolicyPropertyTests.cs`; confirm against existing Adapters property peers and `scripts/run-property-tests.ps1`). Confirm whether `InternalsVisibleTo` for `LamuFlix.IntegrationTests` and (if needed) `LamuFlix.UnitTests` is already present on `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` (retain friendship check). Record current typed-client registration sites for OMDb, TMDb, Ollama, and OpenSubtitles (`recon-DEV-550` lines 34–37). Record read-only pickup receipts for: participating host compositions (Api first from `recon-DEV-550` line 13; inventory any other hosts that invoke the three registration extensions); existing redirect handling and any explicit OpenSubtitles follow path; effective factory setup ordering (shared defaults vs per-client configure) and verify applicable ordering against Microsoft Learn (`ConfigureHttpClientDefaults` / `IHttpClientBuilder`) at pickup; production architecture assembly set intended for the NetArchTest rules; baseline EF Core dependency inventory outside `LamuFlix.Infrastructure.Persistence` (including method-body references) and how the installed rule reports generated/nested types. **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. If authorized demonstrated drift needs artifact amendment, record a separate follow-up and complete that amendment before T002+ production edits. **STOP** `needs recon:` on missing required evidence or seams; **STOP** for cited Patron ruling rather than expanding file/protected surface; **STOP** before T011–T013 rule work if an additional EF source dependency outside the two exact exceptions is found. **Strictly read-only reconnaissance and analysis**.

**Checkpoint**: T002+ unlocks only with a passing Wisp receipt, Keel pickup analysis clear, and any authorized drift follow-up amendments already recorded and applied.

---

## Phase 2: Foundational — shared registration, transport, address policy, test options

**Purpose**: Establish shared factory defaults, transport constraints, address policy, and test-only options before US1/US2 proofs depend on them.
**Independent Test**: Types compile; three registration extensions invoke the shared idempotent defaults; OpenSubtitles effective handler path uses `SocketsHttpHandler` with `AllowAutoRedirect = false` without dropping the callback.
**⚠️ CRITICAL**: US1/US2 behaviour tasks must not begin until this phase is complete. US3 does **not** wait on this phase.

- [ ] T002 [P] After T001: **Build lane**. Add `src/LamuFlix.Infrastructure/Adapters/OutboundConnectAddressPolicy.cs` with one internal range table implementing the mandatory deny set and OpenSubtitles stricter set from `spec.md` / Q2, normalizing IPv4-mapped IPv6 with `MapToIPv4` before matching (FR-003–FR-005; Q2). Keep address literals as `const` and mutable or mutation-sensitive computation out of static initialization (parse in getters / per call; expression-bodied static constructors if any) so the table stays mutation-safe (AGENTS mutation guidance; plan Approach). No XML range prose — citations live in spec and PR body (Q3). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T003 [P] After T001: **Build lane**. Add `src/LamuFlix.Infrastructure/Adapters/OutboundHttpGuardOptions.cs` as default-false internal options that relax only the OpenSubtitles stricter set, with no configuration binding or deployment key (FR-004; Q5). Expose internal resolver and connector delegates that default to production DNS and socket connect and are substituted only in test composition (no flag-checked production bypass; the relaxation option controls address policy only) (FR-003; FR-008; Q3; plan Approach). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T004 After T002–T003: **Build lane**. Add `src/LamuFlix.Infrastructure/Adapters/OutboundHttpClientDefaultsExtensions.cs` as one per-service-collection idempotent internal extension that calls `ConfigureHttpClientDefaults`, configures the effective `SocketsHttpHandler.ConnectCallback` in place without replacing the handler, sets `UseProxy = false`, applies the HTTP/2 ceiling, resolves once per connection attempt via the T003 resolver delegate, validates candidates via T002, dials only a validated exact `IPEndPoint` via the T003 connector delegate, may fall back among validated allowed candidates from that same resolution under the existing timeout and cancellation budget, fails closed on resolver exception/empty/(nullable) null with zero dials, never dials a denied candidate, and when no candidate is allowed reports a connect failure and logs a denial reason category whose captured form omits request URLs, query values, and secret-header values (FR-001–FR-003, FR-006; Q1–Q3; `CONCLUSIONS.md` Q2). Leave the idempotence marker representation to Build. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T005 After T004: **Build lane**. Invoke the shared per-service-collection idempotent defaults from `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs`, `src/LamuFlix.Infrastructure/Adapters/OllamaEmbeddingServiceCollectionExtensions.cs`, and `src/LamuFlix.Infrastructure/Adapters/SubtitleProviderServiceCollectionExtensions.cs`. On the subtitle registration, switch to `SocketsHttpHandler` and set `AllowAutoRedirect = false` via configure-in-place on the effective handler (no per-client primary-handler factory that replaces the handler) so the callback is retained; apply ordering confirmed at T001 against Microsoft Learn; preserve OMDb quota, resilience, and timeouts (FR-001–FR-002; Q1). Respect DEV-551 serialization on the subtitle file (FR-012). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T006 [P] After T001: **Build lane**. If T001 shows IntegrationTests cannot access the internal options/policy/extensions, add `InternalsVisibleTo` for `LamuFlix.IntegrationTests` to `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`. If UnitTests property coverage cannot reach the internal policy, add `InternalsVisibleTo` for `LamuFlix.UnitTests` only for that access (Q5 amendment; constitution IX). Skip either friendship with recorded evidence when already present or unnecessary. When the csproj changes, complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on any accompanying changed `.cs` (skip with recorded N/A when this task changes no `.cs`).

**Checkpoint**: Foundation ready for US1/US2. US3 may already be in progress after T001 without waiting on T002–T006.

---

## Phase 3: User Stories 1–2 - Guard registration proofs and dial-time behaviour (Priority: P1) 🎯 MVP

**Goal**: Dynamic DI discovery proves every factory client is guarded; controlled seams prove mandatory and OpenSubtitles address policy; UnitTests host the two FsCheck properties.
**Independent Test**: Registration tests + behaviour tests + UnitTests property tests green for FR-007–FR-009 / SC-001–SC-003 without live provider traffic.

### Tests for User Stories 1–2

- [ ] T007 [P] [US1] **Build lane**. After T005–T006: Add `tests/LamuFlix.IntegrationTests/OutboundHttpClientGuardRegistrationTests.cs` that builds each confirmed participating production service composition from T001 (Api first; others if found) registering the three extensions as Q1 install points, discovers every named and typed factory registration in each composition dynamically and independently of the guard-registration helper (starting from OMDb, TMDb, Ollama, OpenSubtitles; including any registration outside those three extensions), asserts nonempty discovered coverage for each confirmed composition, asserts each effective primary handler is a `SocketsHttpHandler` with non-null shared `ConnectCallback`, asserts OpenSubtitles `AllowAutoRedirect == false`, proves repeated installation through the three install points applies the shared defaults once while the effective subtitle handler retains its callback and redirect setting, runs the same independent discovery and inspection path against an added unguarded factory registration that must be found and fail the check, includes a negative control with an unguarded effective primary handler that fails the same check, and would detect an unguarded new registration without updating a fixed list (FR-001; FR-007; SC-003; Q3; `recon-DEV-550` lines 34–37). **Retention assertions for US1.4**: assert OMDb quota remains, and assert the existing resilience and timeout settings for OMDb, TMDb, and Ollama remain in place after guard installation (FR-002; US1 acceptance scenario 4). Use Method_Condition_Expected naming, AAA headers, and the project's existing Shouldly/NSubstitute conventions. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T008 [P] [US2] **Build lane**. After T005–T006: Add `tests/LamuFlix.IntegrationTests/OutboundHttpClientGuardBehaviorTests.cs` using the T003 internal resolver/connector delegates for controlled resolution and connection (production defaults remain DNS/socket; no live provider traffic) to prove: mandatory denial including `169.254.169.254` before connect on discovered clients; OpenSubtitles private stricter-set denial and public allowance; configured OMDb/TMDb/Ollama private or loopback reach subject to the mandatory set; IPv4-mapped IPv6; mixed candidates; independent expected examples for every approved range boundary and singleton address (with mapped equivalents where applicable, accounting for neighboring ranges); scoped test flag relaxes only the stricter set; proxy disabled and HTTP/2 ceiling boundary; controlled followed-redirect refusal on a redirect-following guarded client whose next hop is denied, plus any pickup-confirmed explicit OpenSubtitles follow path issuing through the guarded client and refusing a private `Location` (FR-008; SC-001–SC-002; Q2–Q3). **Allowed-result dial proof for US2.3/US2.5/US2.7 and FR-003**: for an allowed resolution, assert exactly one resolution and that every connector invocation receives a validated exact `IPEndPoint` (no second lookup; denied candidates never dial); include one fallback case where the first allowed dial fails under the existing timeout/cancellation budget and a later allowed candidate from the same result is dialed. **Fail-closed proof**: resolver exception, empty result, and (when the seam can represent it) null each yield connect failure, zero dials, and a redacted denial reason. **Regression assertion for US2.1 / FR-003**: when no candidate is allowed, assert the guard reports a connect failure and that captured logs contain a denial reason category while omitting request URLs, query values, and secret-header values (US2 acceptance scenario 1; `CONCLUSIONS.md` Q2). Preserve current request-path and retention assertions. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T009 [P] [US2] **Build lane**. After T005–T006: Add `tests/LamuFlix.UnitTests/Adapters/OutboundConnectAddressPolicyPropertyTests.cs` (or the pickup-verified UnitTests property path from T001) covering mandatory-deny invariance under the subtitle test override and equivalent address classification after IPv4 mapping, under existing harness property-test thresholds and the pickup-confirmed discovery convention (FR-009; constitution IX Test Pyramid; brief Q5 amendment; Q6). Use existing UnitTests Infrastructure and FsCheck references and the project's Shouldly assertion convention; friendship only if T006 required UnitTests access to the internal policy. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.

### Implementation follow-through for User Stories 1–2

- [ ] T010 [US2] **Build lane**. After T007–T009 are authored and failing/passing as appropriate: Apply narrow opt-in to `tests/LamuFlix.IntegrationTests/OpenSubtitlesSubtitleProviderTests.cs`, `SubtitleSearchDownloadEndpointTests.cs`, and/or `SubtitleProviderHealthRegistrationTests.cs` **only** where a guarded local endpoint is required; with the flag enabled those cases MUST still prove the mandatory deny set holds. Skip with recorded evidence if no existing subtitle test needs a guarded local endpoint (Q5). Make T007–T009 pass against the T002–T005 implementation. When this task changes `.cs`, complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on those files (skip with recorded N/A when no `.cs` changed).

**Checkpoint**: US1 and US2 independently verifiable via IntegrationTests DI/behaviour coverage and UnitTests property coverage.

---

## Phase 4: User Story 3 - Architecture rules for helper namespaces and EF placement (Priority: P2)

**Goal**: Two NetArchTest rules with Valid/Violating fixtures, including partial-name-legal proofs, exact EF-exception matching, a Violating near-match EF exception outside Persistence with a full name distinct from both allowed types, and an independent method-body-only EF violation.
**Independent Test**: Production assemblies pass; Valid fixtures pass; Violating fixtures fail; fixture assertions independent of production pass.
**Order**: After T001 only. May run in parallel with Phase 2 and Phase 3 (different files). **No** guard-foundation or InternalsVisibleTo prerequisite (`brief.md` Task order step 3). Does not edit host `Program.cs` or `OutboxRelayHostedService.cs`.

- [ ] T011 [P] [US3] **Build lane**. After T001 (and only after the T001 assembly/EF inventory disposition is clear — stop if an additional EF source dependency outside the two exact exceptions was found): Add Valid/Violating fixtures under `tests/LamuFlix.ArchitectureTests/Fixtures/Valid/` and `tests/LamuFlix.ArchitectureTests/Fixtures/Violating/` for the helper-namespace rule and the EF rule, using the same predicates later applied to production. Include: a Valid fixture proving ordinary partial namespace names stay legal; a Valid (or paired) fixture proving EF allow-list matching is exact for the two allowed full type names; a Violating near-match fixture whose EF dependency is outside Persistence with a full type name distinct from both allowed types (`EfNearMatchOutsidePersistenceFixture` or convention-matching name); when T001 shows generated-artifact attribution is needed, a nested or near-match Violating case that exercises attribution logic only with exact origin evidence of the approved source type; an independent `EfOutsidePersistenceMethodBodyFixture` (or convention-matching name) whose EF dependency occurs only inside a method body; plus positive Persistence-side and namespace Violating fixtures as required by existing conventions (FR-010; Q4; US3 Independent Test; recon lines 15–16, 28). Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T012 [US3] **Build lane**. After T011: Extend `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` with (1) exact namespace-segment bans for `Util`, `Utils`, `Utility`, `Common`, `Helper`, `Helpers`, `Misc` across the T001-inventoried production application assemblies (ordinary partial-name matches remain valid); (2) EF Core dependency detection including method-body references outside `LamuFlix.Infrastructure.Persistence` and subnamespaces, with exact full-type allow-list entries for the two approved source types only — `LamuFlix.Infrastructure.Enrichment.OutboxRelayHostedService` and the Api entry-point `Program` — no wildcards (FR-010; SC-004; Q4). Reuse the same parameterized predicates for production and fixture assertions (assembly, Persistence root, allow-list). If generated-artifact attribution is required, attribute only with exact origin evidence of an approved source type. Keep fixture assertions independent of the production pass. Complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on this task's changed `.cs`.
- [ ] T013 [US3] **Gauge checkpoint**. After T012: Run ArchitectureTests focused coverage for the new rules against the T001 inventory (production pass + Valid pass + Violating fail, including partial-name-legal, exact allowed-type EF-exception, Violating near-match / nested attribution case when required, and independent method-body-only EF fixtures) and the three static gates on this phase’s changed `.cs` (`scripts/run-roslyn-analyzers.ps1`, `scripts/run-cyclomatic-complexity.ps1`, `scripts/run-jetbrains-inspectcode.ps1`, no `-All`). Retained architecture checkpoint — does not replace per-task scoped static-analysis completion on T011–T012.

**Checkpoint**: US3 independently verifiable.

---

## Phase 5: Polish — refactor, exact-head gates, Size M review, receipts, delivery PR handoff

**Purpose**: Converge streams; green applicable gates; obtain receipts after verification; close review under Q6; publish ranges/citations on the delivery PR. Preserve exact-head evidence after any review fixes. Per-task scoped static-analysis completion (Conventions) remains in force for any Phase 5 C# edits; T015 remains the exact-head full sweep.

- [ ] T014 **Cog**. After Phases 3–4 delivery stories: Run `/refactor` over `<base>...HEAD` with complexity threshold from `harness.yml` (do not hardcode). No scope expansion. When refactor materially changes `.cs`, complete only after scoped Roslyn, cyclomatic complexity, and InspectCode evidence on those files.
- [ ] T015 **Gauge**. After T014: On the exact delivery head run and record: Roslyn; cyclomatic complexity; InspectCode; property tests (pickup-confirmed UnitTests discovery convention); vulnerable packages; `dotnet format --verify-no-changes`; full `dotnet test`; mutation per harness with static-initialization discipline expected on the address-policy table (const literals; no mutable computation in static init); web gates expected `SKIPPED (scope-empty)`. Record conditional skips honestly — never as PASS (`brief.md` Verification; Q6; SC-005). Retained exact-head sweep — complements, and does not replace, per-task scoped static-analysis completion on new or materially changed C# tasks.
- [ ] T016 **Size M review**. After T015 green applicable gates: Pre-pass + Sentry / Ledger / Compass; close every verified in-scope finding through Low; record all three dispositions; max two review rounds and two fix commits per round (Q6; SC-007). Independently runnable after T015 alongside T017. After any review fix commits, re-run and record exact-head gate evidence on the new HEAD before merge (do not carry forward stale gate receipts). If a review fix changes the delivery candidate, both Compose and home-cluster receipts (T017) MUST cover that delivery candidate.
- [ ] T017 **Rigger** (pipeline Operator / operator evidence owner). After T015 (not after T016): Obtain and attach separate redacted Compose and home-cluster receipts proving OMDb, TMDb, Ollama, and OpenSubtitles remain reachable from the delivery candidate; record environment, candidate revision, and reachability result per provider; redact credentials and sensitive URLs. Runtime acceptance remains pending until both receipts exist (FR-011; SC-006; Q6; `brief.md` Task order step 4). Do not invent receipt success from planning. Independently runnable after T015 while Size M review (T016) proceeds. If a review fix changes the delivery candidate, both receipts MUST cover that delivery candidate (re-collect against the new candidate as needed).
- [ ] T018 **Ship / delivery PR handoff**. After T015 (and after T016 if review produced fixes that moved HEAD): On the delivery PR body, publish the approved mandatory and OpenSubtitles address ranges and their source citations required by `brief.md` Q2/Q3 and `CONCLUSIONS.md` Q2/Q3 (RFC 6890, IANA IPv4/IPv6 Special-Purpose Address Registries, AWS EC2 IMDS, Alibaba Cloud ECS metadata), matching the lists already in `spec.md`. Do not invent ranges beyond those lists.

**Checkpoint**: Exact-head gates green (re-proven after any review fixes); both receipts present; delivery PR carries range/citation handoff; review closed under the bar.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work
- **Phase 1 (T001)**: Depends on T000 — blocks production edits
- **Phase 2 (T002–T006)**: Depends on T001 — blocks US1/US2 proofs only
- **Phase 3 (T007–T010)**: Depends on Phase 2
- **Phase 4 (T011–T013)**: Depends on T001 only; parallel with Phases 2–3 (different files); no guard-foundation or friendship prerequisite
- **Phase 5 (T014–T018)**: Depends on Phases 3–4 for T014–T015; T016 review and T017 receipts both depend only on T015 and are independently runnable after T015 (T017 does not depend on T016; receipt collection may proceed while review runs); if a review fix changes the delivery candidate, both receipts MUST cover that candidate; T018 delivery PR handoff depends on T015 and on post-fix exact-head evidence when T016 moved HEAD

### User Story Dependencies

- **US1 (P1)**: After foundational Phase 2
- **US2 (P1)**: After foundational Phase 2; shares registration with US1
- **US3 (P2)**: After T001; independent of Phase 2 and of US1/US2 behaviour proofs

### Parallel Opportunities

- T002 / T003 / T006 after T001
- T007 / T008 / T009 after Phase 2
- Phase 4 alongside Phase 2 and Phase 3 after T001
- T016 review alongside T017 receipts after T015 (different artifacts; receipts do not wait on review)
- Never parallelize same-file subtitle registration edits with DEV-551

### Parallel Example: Phase 3 proofs

```text
Task: "DI discovery + negative control + quota/resilience/timeout retention in tests/LamuFlix.IntegrationTests/OutboundHttpClientGuardRegistrationTests.cs"
Task: "Controlled behaviour proofs + redacted failure reason in tests/LamuFlix.IntegrationTests/OutboundHttpClientGuardBehaviorTests.cs"
Task: "Address-policy FsCheck properties in tests/LamuFlix.UnitTests/Adapters/OutboundConnectAddressPolicyPropertyTests.cs (or T001-verified path)"
```

---

## Implementation Strategy

### MVP First (US1 + US2 foundational proofs)

1. Complete Phase 0–2
2. Complete Phase 3 (DI + behaviour + UnitTests properties)
3. **STOP and VALIDATE** US1/US2 independently
4. Add US3 architecture stream (or in parallel from T001)
5. Exact-head gates (T015), then dual-environment receipts (T017); Size M review (T016) and delivery PR range/citation handoff (T018) follow with exact-head re-proof after fixes

### Incremental Delivery

1. Shared defaults + policy → foundation (US1/US2 only)
2. DI/behaviour/UnitTests property proofs → MVP security bar
3. Architecture rules → Wanted 3 (independent stream)
4. Exact-head gates → verification bar
5. Compose + home receipts → Done when runtime (after T015, not after T016)
6. Size M review + delivery PR ranges/citations → merge bar inputs

---

## Notes

- `[P]` tasks = different files, no incomplete deps
- `[Story]` maps to US1 / US2 / US3 from `spec.md`
- Inventory acceptance uses four starting providers + dynamic discovery (`CONCLUSIONS.md` Q6), not recon’s headline count of five
- Quill does not claim Gate 1, freeze, gate PASS, or receipt success from this draft
