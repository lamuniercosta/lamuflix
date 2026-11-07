# DEV-459 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-07
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-459-spec` · branch `feature/459-spec`
**Method**: Spec Kit drafting + analyze round-1 repair + plan-challenge round 1 repair + consolidated plan-challenge repair round 2 (this turn). Grill already closed; `/speckit-analyze` owned by Keel. `.specify/feature.json` already pointed at `specs/DEV-459` and was left unchanged.

**Inputs**: grill-closed `specs/DEV-459/brief.md` (A1–A10), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, proposed `docs/adr/0035-library-relative-movie-paths.md` (Keel-aligned under A10), Maestri notes `DEV-459`, `recon-DEV-459`, `recon-DEV-459-2`, `recon-DEV-459-3`, `adjudication-DEV-459-Keel`, findings notes, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only).

**Artifacts written / repaired (Quill-owned) this round**:

- `specs/DEV-459/spec.md`
- `specs/DEV-459/plan.md`
- `specs/DEV-459/tasks.md`
- `specs/DEV-459/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0035 body (Keel owns ADR alignment), constitution, production/test code, `harness.yml`, `CONTEXT.md`.

**Owner checkbox**: none (brief A7/A9/A10). Proposed ADR 0035 remains proposed until user Gate 1 merge receipt (T000) before any proposed→accepted flip (T032).

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS (A8–A10) and recon facts. Drafting did not run `dotnet ef` and did not embed real connection values.

**needs decision / needs recon (current)**:

- none. A10 + recon-DEV-459-3 §10 fully carried.

---

## Durable Gate 1 prerequisites (not freeze)

Ordinary owner Gate 1 remains pending. Durable handoff: Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000 records verified merge receipt in `CONCLUSIONS.md`. Quill does not invent PLAN FROZEN. After this repair, Keel owns independent final analyze.

---

## Historical — superseded

### Superseded: analyze round-1 needs-decision (resolved by Patron A8)

1. ~~OrdinalIgnoreCase uniqueness mechanism open~~ → Patron A8 Option C.

### Superseded: A9 tooling blocker / plan-challenge round 1 needs-recon #1 (resolved by Patron A10 3096f9a6)

1. ~~Exact no-hand-edit EF tooling sequence for sole additive nullable/no-index migration + final required/unique model snapshot~~ → Patron A10 sanctions recon-DEV-459-3 §10 F5 two-scaffold composition; T007 unblocked with exact commands; F8 recovery + F9 column collation carried. Prior “T007 BLOCKED pending Bernstein/Wisp recon” narration removed from durable plan/tasks/spec/checklist.

### Historical — plan-challenge round 1 applied (Patron A9; retained)

1. Ledger H1: `LamuFlixDbContext` shared SaveChanges choke point; every-writer tests.
2. Ledger H2 / Sentry F1 F3 / Compass 3: A9 additive migration vs final model/snapshot; gate-owned DDL; Migrate→gate→Run; flag-off fail-closed — **tooling sequence later closed by A10**.
3. Ledger M1 M2: sealed `MovieLibraryPathMigrationGate`; ACCESS EXCLUSIVE; single transaction; nonconcurrent index — **collation refined by A10 F9 to property `UseCollation("C")` + gate-set column collation**.
4. Ledger L1 M4: `LibraryOptionsValidator` + pure string parsing / no `System.IO.Path` in Core.
5. Sentry F2 / Ledger M3: hard-stop runtime/globalization/ICU docs; named backstops; no new interlock.
6. Compass 1: reject expected sharp-s/ligature divergence; property gate never waived; mismatch stop before T006+.
7. Compass 2/4/5: T008 Docker map; T010 default-alias; T015/T016 Path assertion; T023 eight families incl. Operations.
8. Ledger L2: remove unsafe `[P]` on T006 and T025–T027; gates sequential.
9. Ledger L3: glossary → builder/Quill; ADR flip after user Gate 1 receipt; AutoFixture/Faker.Net/MockFileSystem retained; transient round narration belongs here (not durable plan/tasks status lines).

### Historical — consolidated repair round 2 applied (Patron A10; this turn)

1. Replaced open T007 tooling blocker with exact F5 two-scaffold commands: Phase A `AddLibraryPathKey` → `Persistence/Migrations`; Phase B final model + `LibraryPathKeyActivation` `--output-dir` absolute TEMP outside worktree; tooling rewrites existing snapshot; only additive ships; no hand-edits/suppression/manual generated-file deletion. No real connection values; tooling not run during drafting.
2. T007/T010 acceptance: pending-check exit 0; live PG17 additive-only Migrate/schema-not-ready; unrelated later scaffold emits no activation; column collation `C` set by gate (not index-level API); final column+index schema proof; retain ordering/flag-off/atomicity assertions.
3. T022 + T007 own F8 immediate-successor migrations-remove recovery: redo Phase B external scaffold; pending zero; inspect next scaffold no activation before apply; no suppression/manual deletion.
4. Removed stale blocked/round narration from durable spec/plan/tasks/checklist status; preserved history here with superseded labels; durable Gate 1 prerequisites retained; no invented freeze.

---

## Transient narration (not durable plan/tasks status)

- Consolidated plan-challenge repair round 2 complete. Ask Keel for independent final analyze.
- No freeze/commit/push/production writes by Quill this turn.
- Fact gap closed earlier by recon-DEV-459-2 §9.8 + A8: host composition `src/LamuFlix.Api/Program.cs`. A10 closed the A9 tooling fact gap via recon-DEV-459-3 §10.

## Encoding notes (not new decisions)

- No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief including A8–A10; public API shape unchanged (value semantics only).
- SMOKE-001 distinguishes planned criteria from observed Compose runtime acceptance.
- Local `tasks.md` Phase N IDs are for `/implement Phase N` dispatch and are not canonical task-pipeline phases.
- Does **not** claim Gate 1, runtime acceptance, Unicode/property proofs already PASS, or PLAN FROZEN.
