# Requirements Quality Checklist: PostgreSQL persistence via EF Core

**Purpose**: Validate that the DEV-19 spec, plan and tasks are complete, consistent and testable before Gate 1
**Created**: 2026-09-30
**Feature**: [spec.md](../spec.md)

## Scope and gate

- [x] CHK001 Spec describes only Q7 option A; no second tasks path exists
- [x] CHK002 Q7 owner checkbox is quoted verbatim and marked as blocking Gate 1
- [x] CHK003 Decline path (option D, Keel reopens brief) is stated
- [x] CHK004 Out-of-scope items name their owning tickets (DEV-301/302/307/308/314/315/316/388)
- [ ] CHK005 Owner has answered the Q7 checkbox (open; owner action, keeps Gate 1 closed)

## Requirement completeness

- [x] CHK006 Every AC1-AC6 maps to a success criterion (SC-001 to SC-006)
- [x] CHK007 All seven tables and their snake_case names are listed
- [x] CHK008 All nine nullable metadata columns have types (FR-005), and every Core `Movie` scalar has a column, type and nullability (FR-005a, brief P2)
- [x] CHK009 All five indexes are specified, including the filtered unique `imdb_id`
- [x] CHK010 Status int mapping 0-3 and failure-category `varchar(32)` code are specified
- [x] CHK011 Exact package and tool versions are stated (10.0.12 / 10.0.3)
- [x] CHK012 Design-time connection behaviour (env var, blank, no echo, no fallback) is specified

## Consistency

- [x] CHK013 Spec, plan, tasks and ADR-0003 agree on paths under `src/LamuFlix.Infrastructure/Persistence/`
- [x] CHK014 Migration command in FR-011 matches CONCLUSIONS Q8
- [x] CHK015 Every functional requirement is covered by at least one task
- [x] CHK016 All implementation, including legacy retirement, follows the Q7 owner answer; retirement precedes the first restore (A1)
- [x] CHK017 ADR-0003 stays Proposed; no task flips its status

## Testability and gates

- [x] CHK018 Converter tests are Docker-free; DB tests are in `tests/LamuFlix.IntegrationTests`
- [x] CHK019 Migration test forbids `EnsureCreated` and asserts applied IDs and no pending model changes
- [x] CHK020 Round-trip reads with a new context and covers the all-null metadata case
- [x] CHK021 All AC6 gates are listed, with "skipped is not a pass"
- [x] CHK022 Generated-migration and mutation-filter contingencies are recorded (the mutation filter is now prerequisite T000, brief P1)

## Risks and open facts

- [x] CHK023 Unverified facts are flagged (Core factory signatures, `MovieId` key type, mutation exclusion of Migrations)
- [x] CHK024 No secrets or connection-string literals appear in any artifact
- [ ] CHK025 T000 mutation-gate mechanism is confirmed by recon and ruled by Patron (open; blocks Phase 2, not Gate 1)
- [x] CHK026 Fixture ownership, per-test unique database, and non-parallel env-var tests are specified (brief P4/P5)

## Notes

- CHK005 is intentionally unchecked and cannot be closed by an agent.
- CHK025 closes when the T000 ruling is recorded in CONCLUSIONS.md.
- Checked items reflect document review only; no build, test or gate was run for this spec kit.
