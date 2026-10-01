# Specification Quality Checklist: DirectoryMediaLibraryScanner

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-01
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs) — every technical detail present is fixed by `brief.md` or a Patron ruling in `CONCLUSIONS.md`, not chosen by the writer. House convention, precedent DEV-296 and DEV-301
- [x] Focused on user value and business needs — the five stories are folder naming, file selection, failure signalling, contract shape and provable coverage; none is a component story
- [x] Written for non-technical stakeholders — each story opens with what the library owner or consuming code needs, before any mechanism (same convention as DEV-301)
- [x] All mandatory sections completed — User Scenarios & Testing, Edge Cases, Requirements (Functional Requirements, Key Entities), Success Criteria, Assumptions

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain — zero by construction; a genuine gap is routed to Keel as `needs decision:` rather than guessed, and nothing was guessed
- [x] Requirements are testable and unambiguous — each of FR-001 to FR-020 carries its basis (ticket AC, ruling or brief section) and maps to a case in brief §5.3 or a gate in brief §5.4
- [x] Success criteria are measurable — SC-001 to SC-007 each carry a count, a rate, a set comparison or a diff
- [x] Success criteria are technology-agnostic — SC-002 and SC-003 name the Cobertura report because Q12 and Q12a fix that toolchain as the acceptance evidence; naming it is fidelity to a ruling, not a specification leak
- [x] All acceptance scenarios defined — 3 + 8 + 7 + 5 + 5 = 28 scenarios, plus 13 edge cases
- [x] Edge cases are identified — marker position, mismatched brackets, digit count, both range boundaries, validity-before-stripping, year-only names, literal dots and underscores, extension case, ordinal tie-break, nesting, unnameable whitespace, and pass-through of unrelated I/O errors
- [x] Scope is clearly bounded — frozen scope and out-of-scope restated in the Assumptions section; FR-019 names every exclusion explicitly
- [x] Dependencies and assumptions identified — three new packages with their rulings and their selection-time verification; six `[assumed]` taste items; the two gates the brief's §5.4 does not name

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria — every FR cites the ruling that fixes it and the case or gate that proves it
- [x] User scenarios cover primary flows — happy path (US1), both naming conventions (US2), all selection outcomes (US3), both failures and nesting (US4), and the measurement itself (US5)
- [x] Feature meets measurable outcomes defined in Success Criteria — US1-US4 deliver SC-001, SC-004, SC-005; US5 delivers SC-002, SC-003, SC-006, SC-007
- [x] No implementation detail leaks into specification — mechanism is confined to plan.md; spec.md states only the observable behaviour and its ruling basis

## Notes

Three items were recorded here so they would surface at spec review rather than being absorbed silently. **All three were resolved by `brief.md` §7 (Keel, spec review round 1, commit `5abacd2`); none of them changed scope:**

1. **`brief.md` §5.5 item 5 tension — RESOLVED, D1 (confirmed).** Item 5 places "Scan composition" after the parsing and selection items, while §5.3 exercises all 24 cases through `Scan` and §5.1 makes the helpers `private static`. `tasks.md` reads item 5 as the guard clauses and result mapping, with the minimal `Scan` shell landing with the helpers. Item 5's dependency direction is preserved. Keel confirmed the reading (D1); item 5 means the guard clauses plus the final result mapping.
2. **`internal static` helper alternative is unreachable under the frozen scope — RESOLVED, D3 (alternative withdrawn).** §5.1 offered an `internal static` helper "if Quill needs direct tests", but `LamuFlix.Infrastructure` declares no `InternalsVisibleTo` for the unit test project (only `LamuFlix.Web` and `LamuFlix.Worker` do), so satisfying it would need a `csproj` edit beyond the single package reference §5.2 allows. It was never needed — the folder name is the parser's only input, so all 13 parsing cases are reachable through `Scan` — and D3 withdrew the alternative outright. The helpers are `private static`, reached through `Scan` (plan.md §1 and §4).
3. **Two gates absent from brief §5.4 — RESOLVED, D2 (confirmed in scope).** The property-test gate and the vulnerable-packages gate are configured in `harness.yml:23-25` and `harness.yml:30-34` and two new packages enter the graph here, so `tasks.md` carries both (T022, T023), including the harness-provided mechanism for the expected property-gate exit 2. Keel reads §5.4 as including both; neither was struck. Recorded in spec.md Assumptions and plan.md Gates.

Two further round-1 rulings confirm choices the drafts had already taken, and neither opened a spec change:

4. **D4 — clock stub.** The existing `tests/LamuFlix.UnitTests/Features/FixedTimeProvider.cs` is the stub; `FakeTimeProvider` would require `Microsoft.Extensions.Time.Testing`, a new package outside frozen scope. Matches spec.md Assumptions, plan.md Test Strategy and T006.
5. **D5 — vocabulary (`constitution.md:277`).** No `CONTEXT.md` edit: "primary video" names only a private helper and creates no public type, member or API term; `CONTEXT.md:21-22` governs command naming; the ticket names `DirectoryMediaLibraryScanner` and the port `IMediaLibraryScanner.Scan` already exists.

D6 and D7 from the same round are applied rather than ruled on: the file list is "seven files, or eight if the test file is split per brief §5.2" (plan.md Project Structure, plan.md Close-out gates, T026), and the close-out carries one explicit final gate pass over every changed `.cs` file with complexity run at 15 and then at `-Threshold 6` (plan.md Gates/Close-out, T025).

No finding required a spec update after that round, and none opened Gate 1.