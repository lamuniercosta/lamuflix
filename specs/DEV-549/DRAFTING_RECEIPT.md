# DEV-549 drafting receipt (Quill)

**Date**: 2026-11-28
**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-549-spec`
**Branch**: `feature/549-spec` (verified `210eee5cabc5177ad7a1468218daf044574d14e7`; review baseline `6bd8be6712c6239065b22609db1599b4f91d3955`)
**PYTHONUTF8**: 1

## Commands run

- `/speckit-specify` — wrote `specs/DEV-549/spec.md`; quality checklist `checklists/requirements.md`
- `/speckit-clarify` — recorded closed from grill in `spec.md` Clarifications (no interactive questions; no critical ambiguities)
- `/speckit-plan` — wrote `specs/DEV-549/plan.md` (+ mirrored to private Spec Kit IMPL_PLAN); `contracts/shared-fixture-mapping.md`
- `/speckit-tasks` — wrote `specs/DEV-549/tasks.md` (+ mirrored to private Spec Kit TASKS)
- `/speckit-checklist` — wrote `checklists/fakes-and-guard.md` (size L requirements-quality checklist)
- Phase 2 step 3.3 analyze repair (consolidated replacement for interrupted round 1) — repaired `spec.md`, `plan.md`, `tasks.md`, companion checklist items, this receipt; re-mirrored plan/tasks
- Phase 2 step 3.3 final focused repair (Keel 3-item list) — `tasks.md` only: T012 after T010 with FR-007 Compose setup on T012/T013; remove T002||T003 and T007||T008; SC-006 five-surface cite on T025; re-mirrored + hash-checked plan/tasks
- Phase 2 step 6 plan-challenge repair (Keel 6-item list) — repaired `spec.md`, `plan.md`, `tasks.md`, `contracts/shared-fixture-mapping.md`, this receipt; re-mirrored plan/tasks + hash-checked
- Phase 2 step 6 final read-back correction (Keel items 2 and 4) — repaired `spec.md`, `plan.md`, `tasks.md`, this receipt; re-mirrored plan/tasks + hash-checked
- Supporting: this receipt

## Sources used (decisions vs facts)

- Decisions: `brief.md` Q1–Q11 only; `CONCLUSIONS.md`; `ASSUMPTIONS.md` (empty of `[assumed]` entries)
- Facts: Maestri notes `DEV-549`, `recon-DEV-549` (Wanted/Done-when/Where it lands); `specs/PRODUCT.md` §3–5; constitution cite-only in plan (Principles VII, IX and related PR-quality lines)
- Quill owns: `spec.md`, `plan.md`, `tasks.md`, `contracts/shared-fixture-mapping.md`, `checklists/requirements.md`, `checklists/fakes-and-guard.md`, this receipt
- Keel owns: brief/conclusions/assumptions (not edited)

## needs decision

None. Grill closed at Q11; brief Frozen scope and Q1–Q11 were sufficient to draft without inventing undecided scope. Analyze repair round applied from Keel consolidated list without inventing new scope.

## Analyze repair (Phase 2 step 3.3) — applied

1. **Q3/Q10 fake-mode** — Spec US3/FR-005–007, plan VII/Approach/Verification, T012–T015: distinct container names; mount at `/app/appsettings.Production.json` under profile only; URLs/keys via settings file not Compose env; explicit fake-only targets; port constraint; every provider wired; fail-closed naming host on both launches; normal Compose + AppHost non-fake baselines; positive flows + PR log evidence; VII typed-options / request-time subtitle behaviour preserved.
2. **Q4/Q9 guard** — US4/FR-008–010, plan Verification, T017–T021: direct-client helper proof alongside factory named/typed; redirect proof cases (relative/absolute Location, allowed hops, 301/302/303 POST→GET, 307/308 method preserve, cross-host Authorization removal, hop-limit failure); blocked host named + zero forbidden sends; loopback success; test-only wiring.
3. **Q4/Q5/Q8 Live** — US5/FR-011/SC-004, plan, T022–T024/T030: key present → bounded pass; `needs-owner-run` only when key unavailable (distinct from acceptance); exception scoped + negative recording-transport non-inheritance proof without credentials/network; isolated skips; CI unchanged; no login/download/quota; no key values in tracked surfaces.
4. **Q7/Q11 fixtures/deps** — OMDb same-file + unsupported-matcher required evidence; e2e (T010) before Compose/AppHost rollout (T013–T015); phase map/deps/examples aligned; unsafe `[P]` removed; five-surface pin after README pin alignment (T025); independent opt-in (T026).
5. **Q8 closing bar** — plan + T028–T030: web checks alongside e2e; N/A ≠ PASS (Principle II **N/A**); final same-head receipts after refactor before review; acceptance receipts, Low disposition, filing bar, both caps; recon provenance and operator-facing outcome preserved; ordinary Gate 1 and scope boundaries kept; companion checklist descriptions aligned.
6. **Mirrors** — canonical `specs/DEV-549/plan.md` and `tasks.md` synced byte-for-byte to private IMPL_PLAN/TASKS.

## Final focused repair (Phase 2 step 3.3) — applied

1. **FR-007 Compose on T012/T013** — T012 moved after T010; T012/T013 explicitly assign FR-007 Compose fake-mode (every configured provider → fake endpoint; missing wiring fails setup/startup naming the host before live fallback); exact mechanism left to Build within Q3, Q10, Principle VII; T015 remains the proof. Phase map, deps, notes, and parallel examples aligned. Spec/plan already stated FR-007; no inventing beyond the list.
2. **Unsafe parallel pairs removed** — Dropped advertised T002||T003 and T007||T008 (`[P]` markers, deps, examples). Serialized T003→T004→T002 and T008→T007/T009 because excluding `stub.mjs` from test tasks still leaves overlapping WireMock response/consumer edits. Guard (T017/T018) and pin (T025/T026) lanes kept separate. T025 cites SC-006 on the five-surface receipt; T026 keeps SC-006 opt-in half.
3. **Mirrors** — re-synced and SHA256 hash-checked both prerequisite-resolved private mirrors against canonical `plan.md` / `tasks.md`.

## Step 6 plan-challenge repair (Keel numbered list) — applied

Authority: `brief.md` Step 6; `CONCLUSIONS.md` Step 6; review baseline `6bd8be67`; HEAD conclusions-only `210eee5c`. Owned: `spec.md`, `plan.md`, `tasks.md`, `contracts/shared-fixture-mapping.md`. Keel owns brief/conclusions/ADR (not edited). No Gate 1 / plan freeze claimed.

1. **Sentry F1** — FR-011/SC-004, plan Live, T022: fresh per-smoke disposable guarded client; immutable provider-host allowance; no shared/ambient switch; synchronized concurrent negative recording proof (ordinary denies Live host; Live denies different external host; both zero forbidden sends; no keys/network). Independent skips, opt-in, and `needs-owner-run` evidence semantics preserved.
2. **Sentry F2 / Ledger M1** — FR-009/010, SC-003, T017/T018/T020: keep Q9 bounded follower + method matrix; origin = scheme + normalized host + effective port; HTTPS downgrade refused; Authorization retained on same origin (incl. explicit default port) and removed on allowed changed origin; changed-origin proof explicitly covers same-host different effective port and permitted HTTP-to-HTTPS origin change; zero forbidden sends for downgrade and real-host redirect; parameterized redirect table in `TestEgressGuardTests.cs`.
3. **Sentry F3/F6** — FR-001–003/SC-005, contract, T002/T003/T004/T007/T008/T027/T028: explicit credential-only exclusions (`apikey` OMDb; `api_key` TMDb; none OpenSubtitles/Ollama); both readers; functional params retained; fail loud on unsupported matcher; alternate synthetic credential proof in both hosts; redacted gitleaks + scrub review before staging; hosted gitleaks receipt on delivery head; existing rules/workflow only; `fake-key` kept; OMDb zero-copy preserved. No new tooling; no delivery checks run in this planning task.
4. **Ledger M2/L7, Sentry F7** — FR-005–007/SC-002, plan Constitution VII/Approach/Verification, T012/T013/T014/T015/T027: fakes-only preflight in existing `stub.mjs`; fake API depends on validated readiness; same read-only fake JSON; exact fixture authority/path + nonempty required mappings; no provider env override/live fallback; no `src/` startup validation; **Compose preflight only** owns missing bind / empty fixture set / invalid/missing endpoint negatives naming provider/host; AppHost independently proves failure when fixture resource or endpoint wiring cannot resolve (Q10; T014/T015); normalized default-profile Compose config vs baseline; fake-target closure; log destinations vs named fixture endpoint; document shared infrastructure/state and ports.
5. **Sentry F5 / Ledger L2–L3** — T001 inventories all actual named/typed registrations and direct adapter constructions; `tests/fakes` data-only under existing discovery; T017/T019/T020 reconcile inventory; no Evaluation/Tests.Common expansion or new architecture analyzer; named files `TestEgressGuardHandler.cs`, `GuardedHttpClientFactory.cs`, `TestEgressGuardTests.cs`, `LiveProviderSearchTests.cs`.
6. **Ledger L1/L4/L5f/L6** — plan/T028: AppHost-only production C# mutation expected N/A under `harness.yml` `LamuFlix.AppHost` exclusion; verify actual exit on delivery head. ADR 0051 availability recheck via Wisp before publication/merge; stays proposed until Build. Stripped transient drafting status/"Quill-named"/restated caps from durable artifacts; caps sourced once from `brief.md` Q8; operational task gates and T000/T001 lane deps preserved.

## Step 6 final read-back correction (Keel items 2 and 4) — applied

Authority: Keel ask after first Step 6 repair; HEAD `210eee5c`; owned `spec.md`, `plan.md`, `tasks.md`, this receipt. No brief/conclusions/ADR edits.

1. **Item 2 (changed-origin proofs)** — SC-003, FR-009/010, US4 Independent Test/scenario, Edge Cases, plan Approach/Verification Guard, T017/T018/T030 Phase 5 summary: allowed changed-origin Authorization removal explicitly covers same-host different effective port and permitted HTTP-to-HTTPS origin change (HTTPS downgrade remains refused).
2. **Item 4 (Compose vs AppHost negatives)** — SC-002, FR-006/007, US3 Independent Test/scenario, Edge Cases, plan Constitution VII/Approach/Verification Fake mode, T012/T014/T015/T030 Phase 4 summary: the three negatives (missing bind, empty fixture set, invalid/missing endpoint) assigned to Compose preflight only; AppHost independently proves failure when fixture resource or endpoint wiring cannot resolve (Q10; T014/T015).
3. **Mirrors** — re-synced and SHA256 hash-checked both prerequisite-resolved private mirrors against canonical `plan.md` / `tasks.md`.
   - plan.md (canonical = private): `07C880AEE130EE522DEF787D3DDCB079C7000E3F013C6C8F1B2FEAC9FD6A4102`
   - tasks.md (canonical = private): `0F3E19A9ED5DF6C84E1EFCB1FDFA182C6477489C43EE5109761027E1EA49DF22`

## Named identifiers within frozen patterns (not Patron taste assumptions)

| Item | Value | Frozen pattern cited |
|---|---|---|
| Live OMDb env | `LAMUFLIX_TEST_OMDB_API_KEY` | Wanted 4 / Q5 `LAMUFLIX_TEST_<PROVIDER>_*` |
| Live TMDb env | `LAMUFLIX_TEST_TMDB_API_KEY` | same |
| Live OpenSubtitles env | `LAMUFLIX_TEST_OPENSUBTITLES_API_KEY` | same |
| AppHost switch | `UseServiceFakes` | Wanted 3 / Q10 AppHost switch |
| Compose fake API service | `api-fakes` | Q10 distinct fake-profile services |
| Fixture resource name | `service-fakes` | Q10 shared Node fixture host |
| Fake placeholder | `fake-key` | Patron Q3 example |
| Settings path | `docker/api.settings.fakes.json` | Patron Q3 example path |
| Redirect hop limit | `MaxRedirectHops = 5` | Q9 named test constant |
| Guard handler | `TestEgressGuardHandler.cs` | Step 6 |
| Guarded client helper | `GuardedHttpClientFactory.cs` | Step 6 |
| Guard proofs | `TestEgressGuardTests.cs` | Step 6 |
| Live smokes | `LiveProviderSearchTests.cs` | Step 6 |
| OMDb credential query param | `apikey` | adapter query shape / Step 6(5) |
| TMDb credential query param | `api_key` | adapter query shape / Step 6(5) |

## Notes

- `before_specify` hook (`grill-with-docs`) already satisfied by closed grill artifacts; not re-run.
- `after_tasks` hook (`speckit-analyze`) is **Keel's** read-only cross-artifact analysis per Quill duties — Quill does not run or claim analyze.
- Prerequisite-resolved private mirrors: Spec Kit resolves IMPL_PLAN from the private ticket directory. Paths: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-549\plan.md` and `...\tasks.md`. Quill keeps canonical `specs/DEV-549/plan.md` and `specs/DEV-549/tasks.md` authoritative and mirrors both so private and worktree copies stay byte-identical.
- No `research.md` / `data-model.md` / `quickstart.md` — grill freezes inventable unknowns; no schema change; fixture contract under `contracts/`.
- No structural owner checkbox. Gate 1 and delivery acceptance not claimed by Quill. Quill does not declare PLAN FROZEN.
- Recon notes search-tool caller enumeration was unavailable; T001 pickup recon is tasked to inventory exact embedding sites on the Build HEAD. Line-number drift (e.g. `.env.example` / README pin lines vs ticket citations) does not change the named file set. Spec precedence credits recon only for Wanted / Done when / Where-it-lands inventory.
- Interrupted round-1 analyze ask partially edited `spec.md` before this consolidated replacement; plan/tasks were unfinished then and are completed here.
- Conductor no-commit instruction observed — artifacts left uncommitted.
