---
description: "Task list for DEV-553 distinct downloaded subtitle track labels"
---

# Tasks: Distinct downloaded subtitle track labels

**Input**: Design documents from `specs/DEV-553/` — `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/subtitle-tracks.md`, `quickstart.md`; grill-closed `brief.md` (Q1–Q10); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-553`, `recon-DEV-553`; `specs/PRODUCT.md`.

**Status**: Draft — Gate 1 / T000 remains **pending**. Build begins only after verified Gate 1 merge, DEV-551 delivery merge, and pickup evidence confirmation (Q8). Spec Kit work stays `specs/**` only.

**Tests**: Required (`brief.md` Task order; Done when; FR proofs; Q2/Q4/Q5/Q7/Q9). Follow implement test-first (failing cases before implementation). Use existing subtitle unit, integration, and web test surfaces. No new test framework/dependency. Green checkpoints attach to the behaviour that just landed.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/review work is never `[P]` and is **not** Anvil-dispatched.

**Conventions**: Edit only the authorized envelope in `plan.md`; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required fact/decision is missing; no new dependencies; no LocalPlay/secrets/`Process.Start`; never lower harness thresholds; never edit existing discovery tests (byte-stable). Otherwise-unassigned **local Build** implementation tasks are owned by the generic Build lane (Anvil). Named seats own Spec PR, pickup, Cog refactor, Gauge gates, review, and ship per `task-pipeline`.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` Build dispatch only. They are **not** canonical `task-pipeline` phases. Under task-pipeline Phase 3 step 2, Anvil receives **one local phase per ask, in order**. Gate-only verification belongs to Gauge Phase 3 step 5 (not claimed as hosted).

**Spec-PR provenance**: After Keel freeze receipt (PLAN FROZEN) and Patron `gate1: provisional`, Bernstein coordinates and Rigger opens the DEV-553 Spec Kit PR (`specs/**` only — ordinary owner Gate 1, no structural checkbox). Explicitly exclude the ADR file from the Spec Kit PR. ADR 0054 Accepted, constitution Principle I MINOR, and ArchitectureTests whitelist are **Build PR** deliverables with the port (Q6). Proposed ADR 0054 is parked in `PRIVATE_TICKET_DIR` as `0054-subtitle-download-record-persistence-port.md` for Build pickup (brief/conclusions durable fallback).

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger open; user merges; Keel Gate 1) — blocks T001+
Phase 1   T001          (Wisp pickup + Keel reanalysis; DEV-551 evidence; read-only) — blocks production edits
Phase 2   T002 → T004   (US1 foundation: governance + port + persistence)
Phase 3   T005 → T007   (US1 download persist success, then failure path)
Phase 4   T008 → T011   (US1/US2 list enrich + ranking + discovery regression)
Phase 5   T012 → T015   (US1 wire endpoint proof → DTO mappings → OpenAPI/TS)
Phase 6   T016 → T018   (US3/US4 web selector + selection)
Phase 7   T019          (US5 glossary; local Build ends)
—— local Build ends ——
Downstream (not Anvil): Cog /refactor → Gauge Phase 3 step 5 (exact-diff; exit 1/Could not run block)
  → Size M Sentry/Ledger/Compass → Keel findings → Phase 5 rebase + rebased-head gates
  → SMOKE-001–003 on Compose (after the applicable gate set on the rebased delivery head; not a local Build task)
```


Ordering per `brief.md` Task order and Q8. No dependency cycle. Do not start Phase 2 production code without T000+T001 pass (including DEV-551 evidence). Endpoint proofs (T012/T014) must pass before OpenAPI → TypeScript (T015) and web (Phase 6).

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1+)

**Purpose**: Produce/open the Spec Kit PR, then confirm ordinary owner Gate 1 merge before pickup/Build.
**Checkpoint**: Spec PR opened; verified user-merge receipt in `CONCLUSIONS.md`, or stop. Gate 1 remains pending until that merge.

- [ ] T000 After Keel freeze receipt (PLAN FROZEN) and Patron `gate1: provisional`: **Bernstein** coordinates and **Rigger** prepares/opens the DEV-553 Spec Kit PR (`specs/**` only) for ordinary owner Gate 1; surface the Q6 note that ADR 0054 Accepted + Principle I MINOR + ArchitectureTests whitelist ship in the Build PR with the port (not a checkbox). Explicitly exclude the ADR file (`docs/adr/0054-subtitle-download-record-persistence-port.md` and any copy) from the Spec Kit PR. Do not include ADR Accepted, constitution, or ArchitectureTests whitelist edits in the Spec Kit PR. Only the user merges (completes Gate 1). After merge, **Keel** records the verified Gate 1 Spec Kit PR merge receipt for DEV-553 in `specs/DEV-553/CONCLUSIONS.md`. Receipt only. T001+ MUST NOT proceed without verified merge (`brief.md` Q6 / Q10; no structural owner checkbox).

---

## Phase 1: Pickup recon (blocks production edits)

**Purpose**: Confirm DEV-551 evidence and authorized seams against pickup HEAD.
**Checkpoint**: Written pickup receipt cites provider ID + candidate evidence and file/test seams; Keel read-only reanalysis clear. T001 itself does not edit production files.

- [ ] T001 After T000: **Wisp** obtains Phase B pickup recon in the Build worktree confirming (1) DEV-551 delivery merge and that the landed candidate/binding contract exposes canonical provider ID plus selected-candidate evidence required for the record (hash-match, match percent, release name, language) — if missing, **STOP** and route `needs recon` (do not hand-roll); (2) authorized envelope paths from `plan.md` (Core port `ISubtitleDownloadRecordStore`, Subtitles handlers, Persistence adapter/migration/DI site, `SubtitleTrackDto` / `SubtitleEndpoints`, web selector/hook, generated contract paths, ArchitectureTests whitelist, constitution Principle I Version and Last Amended, ADR 0054 claim/namespace with proposed draft in `PRIVATE_TICKET_DIR`) — if the ADR number collides, **STOP** and route to Bernstein; (3) existing subtitle unit/integration/web test seams from `recon-DEV-553:19–72` and persistence facts `:74–82` (key evidence `:78–82`). **Keel** reanalyzes brief/spec/plan/tasks against pickup HEAD **read-only**. **Strictly read-only** aside from collecting the receipt artifacts named by the Build lane. Pickup and verification stay on the owning seats (Wisp/Keel). Revalidate ADR claim / constitution Version / Last Amended / whitelist again after the delivery rebase.

**Checkpoint**: T002+ unlocks only when the Wisp receipt is recorded, DEV-551 evidence is confirmed, and Keel pickup analysis is clear.

---

## Phase 2: User Story 1 foundation — port, governance, persistence (Priority: P1) 🎯 MVP

**Goal**: Core port `ISubtitleDownloadRecordStore` + EF adapter/migration/registration with unique composite key and atomic upsert, delivered atomically with ADR 0054 Accepted, Principle I MINOR, and ArchitectureTests whitelist (FR-001, FR-015).
**Independent Test**: ArchitectureTests green; integration/persistence proofs for canonical discovery key, adjacent vs `Subs\` paths, atomic same-key replacement, concurrent separate-context same-key upserts leaving one complete row, real DB round trips.
**Order**: After T001.

### Tests

- [ ] T002 [US1] **Build lane**. After T001: Using implement test-first, author failing persistence/integration tests under `tests/LamuFlix.IntegrationTests/` (and unit doubles as needed under `tests/LamuFlix.UnitTests/Features/Subtitles/`) proving: (1) unique (movie, canonical discovery relative path) including adjacent vs `Subs\` separation with path segments/casing/Unicode preserved and separators mapped to the discovery convention at the publication boundary; (2) atomic same-key upsert replacement; (3) real concurrent same-key upserts against PostgreSQL from separate contexts where both calls complete and one row holds one writer's complete metadata; (4) real PostgreSQL round trips — per `data-model.md`, Q7, and Phase 2 step 6 refinements.

### Implementation

- [ ] T003 [US1] **Build lane**. After T002: **Atomically** introduce Core port `ISubtitleDownloadRecordStore` under `src/LamuFlix.Core/Ports/` (port name locked by ADR 0054), accept ADR 0054 under `docs/adr/0054-subtitle-download-record-persistence-port.md` (carry the proposed draft from `PRIVATE_TICKET_DIR`; brief/conclusions are durable fallback if recreation is needed), amend `.specify/memory/constitution.md` Principle I enumeration with the **next MINOR from the then-current constitution base**, Sync Impact Report and Last Amended, and update `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` `PortWhitelistPattern` to include the new port while preserving intervening whitelist entries — no unrelated Principle or whitelist edits. Implement the EF entity/configuration, migration + snapshot, adapter, and DI binding in `src/LamuFlix.Infrastructure/Persistence/` / `PersistenceServiceCollectionExtensions.cs` for post-publication upsert and per-movie batch read; **Build names table/EF types by existing Persistence conventions** (ADR locks only the port name). Make T002 pass. Keep `LamuFlix.ArchitectureTests` green (Q6).
- [ ] T004 [US1] **Build lane**. After T003: Confirm registration and migration apply through the existing persistence startup path; no other schema changes beyond the ticket-owned table.

**Checkpoint**: Port + governance + persistence foundation green before handler wiring.

---

## Phase 3: User Story 1 — download persistence and failure path (Priority: P1)

**Goal**: After successful publication, upsert provenance from the selected candidate and project provider/release onto the returned Core track; on upsert failure/cancellation/ambiguity keep file and return immediate fallback success (FR-001–FR-004, FR-009; Q2).
**Independent Test**: Unit tests for no-write on publication failure; successful recording plus Core-track provider/release projection; metadata failure keeps file and null provenance on the immediate response; Error log; no retry. Q9 last-committed-row list behaviour is proven in Phase 4 (T009/T010), not here.
**Order**: After Phase 2.

### Success path

- [ ] T005 [US1] **Build lane**. After T004: Using implement test-first, author failing unit tests in `tests/LamuFlix.UnitTests/Features/Subtitles/DownloadSubtitleCommandHandlerTests.cs` (or focused companion) for: publication failure writes nothing; successful publish then upsert success records provider/release/candidate-language/hash/score/UTC with the canonical discovery relative path **including a successful write that stores a null score when match percent is unknown**; and the returned Core `SubtitleTrack` carries the same provider/release while retaining the discovery-derived label, discovery-derived language, and existing track identity from that canonical key (Q1, Q7; Phase 2 step 6). Introduce nullable provider/release on Core `SubtitleTrack` here if not already present so later list (T010) and wire mapping (T013) consume the same shape. Do **not** include metadata-failure cases here.
- [ ] T006 [US1] **Build lane**. After T005: Wire `src/LamuFlix.Core/Features/Subtitles/DownloadSubtitleCommandHandler.cs` to upsert via the new port only after successful `ISubtitleSidecarStore` publication, reading provider ID and ranking evidence from the DEV-551-backed selected candidate/binding confirmed in T001; persist candidate language as provenance and allow null match percent on successful writes; use injected `TimeProvider` for download UTC; project provider/release onto the returned Core `SubtitleTrack` while preserving discovery-derived label, discovery-derived language, and existing identity from the canonical discovery key. Make T005 pass.

**Checkpoint**: Successful download provenance write and Core-track provider/release projection proven (T005–T006 green) before the failure path.

### Failure path

- [ ] T007 [US1] **Build lane**. After T006: Using implement test-first, author failing unit tests then implement metadata-failure handling in `DownloadSubtitleCommandHandler.cs` / collaborating types under `src/LamuFlix.Core/Features/Subtitles/` for the **immediate** outcome only: upsert throw, ambiguous write, and cancellation → existing success shape with discovery-derived label and null provider/release on the returned track; file remains; Error log with movie id + relative path; no delete, rollback, or retry (Q2). Do **not** require a list/read-back of last-committed-row state for this checkpoint — that Q9 expectation lives in T009/T010. Make those immediate failure-path tests pass.

**Checkpoint**: Download success and immediate failure-path outcomes proven at unit/handler level before list ranking. No endpoint provider/release assertions in this phase.

---

## Phase 4: User Story 1 + User Story 2 — list enrichment and ranking (Priority: P1)

**Goal**: Discovery-first list enrichment, ranking, preserved discovery-derived labels, independently projected nullable provider/release (FR-005–FR-007, FR-010–FR-011, FR-014). Display composition stays out of Core (FR-008).
**Independent Test**: Unit/property ranking proofs; list handler tests for join, deleted-file exclusion, hand-added/null fallback, independent field projection; discovery tests unedited and byte-stable.
**Order**: After Phase 3.

### Tests

- [ ] T008 [P] [US2] **Build lane**. After T007: Using implement test-first, author failing unit and property tests under `tests/LamuFlix.UnitTests/Features/Subtitles/` for recorded-first, hash → known match percent desc (missing scores after known at the same hash rank) → UTC desc, stable discovery ties, unrecorded-last, and language partition by first appearance (FR-010; Q4).
- [ ] T009 [P] [US1] **Build lane**. After T007: Using implement test-first, extend `tests/LamuFlix.UnitTests/Features/Subtitles/ListSubtitleTracksQueryHandlerTests.cs` for batch enrichment by the canonical discovery relative-path key, null provenance fallback, independent provider/release projection (one known field not cleared when the other is missing), exclusion when the file is missing while a row remains, discovery-derived language remaining authoritative when the stored candidate language conflicts, and Q9 last-committed-row list behaviour after a same-key failed/ambiguous replacement (old row if the replacement did not commit; new row if it committed — separate from the download’s immediate fallback).

### Implementation

- [ ] T010 [US1] [US2] **Build lane**. After T008 and T009: Update `src/LamuFlix.Core/Features/Subtitles/ListSubtitleTracksQueryHandler.cs` and track projection/ranking helpers under `src/LamuFlix.Core/Features/Subtitles/` to batch-read provenance, join by the canonical discovery relative-path key, apply ranking partitioned by discovery-derived language, preserve discovery-derived `label` and discovery-derived `language` (stored candidate language is provenance only), and set nullable provider/release independently using the same Core `SubtitleTrack` shape introduced in T005/T006 (id/label/language/isDefault retained). Do **not** compose `release name · provider` into the Core/wire label. Make T008–T009 pass, including the conflicting-language and Q9 committed-row list expectations. **Do not edit** `tests/LamuFlix.UnitTests/Features/Subtitles/SubtitleSidecarDiscoveryTests.cs`.
- [ ] T011 [US1] **Build lane**. After T010: Run existing `tests/LamuFlix.UnitTests/Features/Subtitles/SubtitleSidecarDiscoveryTests.cs` unedited and confirm they remain byte-stable and green (FR-014). No endpoint assertions in this phase.

**Checkpoint**: List enrichment, ranking, and discovery regression proven independently of wire endpoint mappings.

---

## Phase 5: User Story 1 — wire contract, endpoint proof, then codegen (Priority: P1)

**Goal**: Endpoint projection proofs → DTO + both mappings → OpenAPI/TS regeneration (FR-007, FR-018; `brief.md` Task order items 3–4).
**Independent Test**: Endpoint projections include successful download/list fields and explicit nulls; OpenAPI/types drift green only after endpoint proof; fixtures use generated types.
**Order**: After Phase 4. Passing endpoint proof is required before T015 and Phase 6.

- [ ] T012 [US1] **Build lane**. After T011: Using implement test-first, author failing endpoint/integration assertions in `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs` (and related) covering: successful download and list projections with provider/release when known; known-provenance responses **preserve the existing discovery-derived wire `label`**; unknown `provider`/`releaseName` serialize as JSON `null` independently on both responses (including fallback — one null must not force the other); listed two-provider English downloads with distinct provider/release; hand-added `Film.en.srt` keeps today’s discovery-derived label with nulls; deleted-file track omitted while its row remains; same-key successful re-download shows replacement provenance; and a **Subs download then list round trip returns the same track ID and provenance** (canonical discovery key / token input unchanged) (Done when; Q7; Phase 2 step 6). Author these before DTO mappings.
- [ ] T013 [US1] **Build lane**. After T012: Extend `src/LamuFlix.Api/Endpoints/SubtitleTrackDto.cs` with nullable `provider` and `releaseName`; update list and download mappings in `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs` per `contracts/subtitle-tracks.md` to map both fields from the Core `SubtitleTrack` shape (preserve discovery-derived `label`; project fields independently).
- [ ] T014 [US1] **Build lane**. After T013: Make T012 pass against both endpoint mappings (download and list).
- [ ] T015 [US1] **Build lane**. After T014: Regenerate `web/src/api/openapi.json` then `web/src/api/types.ts` through the normal contract chain; verify drift checks green; confirm both generated OpenAPI properties for `provider` and `releaseName` accept string or null in the emitted schema dialect and both generated TypeScript properties admit `string | null`; update API/integration fixtures to expect explicit nulls and independently projected fields; confirm no hand-rolled web DTO duplicates. T012/T014 remain the JSON serialization proof.

**Checkpoint**: Endpoint proof green before OpenAPI → TypeScript and web selector work.

---

## Phase 6: User Story 3 + User Story 4 — selector and selection (Priority: P1/P2)

**Goal**: Language headings, composed/fallback labels, order, Off placement; selection survives download reload (FR-008, FR-011–FR-013; SC-002; SMOKE-001–003 behaviour covered in web tests here, Compose smoke downstream).
**Independent Test**: Web tests for first-appearance headings, composed vs partial-provenance labels, selectable manual track, Off; downloaded selection through reorder/relabel; in-flight newer choice/Off kept; absent-track handling preserved.
**Order**: After Phase 5 (T015).

### Tests

- [ ] T016 [P] [US3] **Build lane**. After T015: Using implement test-first, extend `web/src/features/playback/subtitleSelector.test.tsx` (generated `SubtitleTrackDto` fixtures) for first-appearance language-group order **keyed by the wire `language` field**, composed `release name · provider` when both fields are known, partial-provenance fallback to the wire label, selectable manual/hand-added track, preserved incoming order inside groups, and single Off outside groups (FR-008, FR-011–FR-012; SC-002; `ASSUMPTIONS.md`; Phase 2 step 6).
- [ ] T017 [P] [US4] **Build lane**. After T015: Using implement test-first, extend `web/src/features/playback/subtitlesTab.test.tsx` (and BrowserPlayer coverage as today) for downloaded-track selection surviving reload reorder/relabel, newer manual selection or Off during in-flight refresh kept via existing attempt/generation guards, and existing absent-track handling named and asserted as a preserved regression (FR-013; Q5).

### Implementation

- [ ] T018 [US3] [US4] **Build lane**. After T016 and T017: Update `web/src/features/playback/SubtitleSelector.tsx` to render headed language groups in first-appearance order keyed by wire `language`, compose display `release name · provider` only when both wire fields are known otherwise use wire `label`, keep manual tracks selectable, and preserve existing radio semantics; update `web/src/features/playback/useSubtitlePlayback.ts` to keep the downloaded track selected by server id across reload without weakening newer-choice or absent-track guards. Consume generated types only. Make T016–T017 pass.

**Checkpoint**: Player UX proofs green. Compose SMOKE-001–003 remain a downstream checkpoint, not this phase’s exit.

---

## Phase 7: User Story 5 glossary — local Build close (Priority: P3)

**Goal**: Glossary update (FR-016). Gate-only verification is left to Gauge Phase 3 step 5 per task-pipeline Phase 3 step 2.
**Independent Test**: CONTEXT diff against Wanted 6; discovery already proven byte-stable in T011.
**Order**: After Phase 6.

- [ ] T019 [US5] **Build lane**. After T018: Update `CONTEXT.md` Local Subtitle Track so a downloaded track carries its provider and release name (Wanted 6); do not alter recognition-policy entries.

**Checkpoint**: Local Build complete after T019. Gate 1 was already satisfied before Build (T000 blocks T001+). Do not claim delivery merge readiness or that Gauge Phase 3 step 5 itself is hosted. Discovery regression remains T011’s proof; gate-only scripts are Gauge’s.

---

## Downstream checkpoints (Conductor-sequenced; not Anvil)

These are not local Build tasks. They keep read-only pickup/verification and gate execution on the owning seats.

1. **Cog `/refactor`** over the accumulated Build diff after local phases complete.
2. **Gauge Phase 3 step 5**: exact-diff Roslyn, cyclomatic (incl. refactor threshold), InspectCode, mutation, property tests, vulnerable packages, format, full `dotnet test`, web gates, OpenAPI/types drift as applicable. Report exit-2 as the gate reports them — never as PASS. Exit 1 or `Could not run` blocks (`brief.md` Task order item 5).
3. **Size M review**: Sentry, Ledger, Compass with explicit dispositions; Keel adjudication.
4. **Phase 5 ship**: rebase; Gauge repeats the applicable gate set on the rebased delivery head (exact-diff / rebased-head checks explicit; exit 1 / `Could not run` still block).
5. **SMOKE-001–003 on Compose**: after the applicable gate set on the rebased delivery head (same downstream sequence as `plan.md` / `quickstart.md`). Not a local Build task. Covers the three planned Compose scenarios in `spec.md` / `quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Blocks all Build work; Gate 1 completes at T000 user merge before T001+
- **Phase 1**: Depends on Gate 1; blocks production edits; DEV-551 evidence gate; read-only on Wisp/Keel
- **Phase 2**: Depends on Phase 1 — port/governance/persistence foundation
- **Phase 3**: Depends on Phase 2 — download success path (Core track projection) then immediate failure path
- **Phase 4**: Depends on Phase 3 — list + ranking + Q9 committed-row list proof + discovery regression
- **Phase 5**: Depends on Phase 4 — endpoint proof authored before mappings; codegen only after endpoint proof
- **Phase 6**: Depends on Phase 5 — web
- **Phase 7**: Depends on Phase 6 — glossary; local Build ends
- **Downstream**: Depends on Phase 7 — Cog → Gauge Phase 3 step 5 → review → rebase/rebased-head gates → SMOKE-001–003

### User Story Dependencies

- **US1 (P1)**: Foundation through wire (Phases 2–5) — MVP
- **US2 (P1)**: Ranking inside Phase 4; can unit-test without web
- **US3 (P1)**: Needs generated contract (Phase 5) then Phase 6
- **US4 (P2)**: Phase 6 with US3; depends on download refresh behaviour from US1
- **US5 (P3)**: Phase 7 after behaviour lands

### Parallel Opportunities

- T008 / T009 test authorship on different files after T007
- T016 / T017 web test authorship on different files after T015

### MVP scope

Phases 0–5 (US1 + US2 + contract) deliver distinguishable downloaded tracks on the API. Phase 6 delivers the owner-visible selector. Phase 7 closes glossary. Downstream owns gates, review, rebase, and Compose smoke.

---

## Notes

- ADR 0054 locks only Core port name `ISubtitleDownloadRecordStore`; Build names table/EF types by existing Persistence conventions. Proposed draft lives in `PRIVATE_TICKET_DIR`; Spec Kit PR excludes the ADR file; T003 accepts it atomically with the port.
- Existing discovery tests must remain byte-stable; **no edits**.
- Size M review requires Sentry, Ledger, and Compass with explicit dispositions; every confirmed in-scope finding blocks including Low; two remediation rounds max (Q10).
- Wire/display boundary: preserve discovery-derived `label` and discovery-derived `language`; project `provider` / `releaseName` independently; selector groups by wire `language` and composes only when both provenance fields are known (FR-008; Q3; Phase 2 step 6).
- Core `SubtitleTrack` nullable provider/release are introduced and proven on the download success path (T005/T006), including null-score successful writes; list (T010) and wire mappings (T013) consume that same shape.
- Q9 last-committed-row expectations are list proofs (T009/T010), not part of the T007 green checkpoint.
- T001 and the delivery rebase revalidate ADR 0054 claim, constitution Version/Last Amended, and whitelist; numbering collision routes to Bernstein; next MINOR is taken from the then-current constitution base while preserving intervening whitelist entries.
