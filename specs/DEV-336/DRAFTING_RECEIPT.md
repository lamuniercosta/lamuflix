# DEV-336 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-19
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-336-spec` | **Branch**: `feature/336-spec` | **HEAD at drafting**: `06aedfeabc9e26ecc0348c71ca9983a2a8d27a65`

**Method**: `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` followed from the skills and `.specify/templates/`. `setup-plan.ps1 -Json` and `setup-tasks.ps1 -Json` were run in this worktree with `PYTHONUTF8=1`. No `.specify/extensions.yml` hooks were present. `setup-tasks.ps1` reported `AVAILABLE_DOCS` empty. Content comes from `brief.md`, `CONCLUSIONS.md` Q1–Q5, `ASSUMPTIONS.md`, note `recon-DEV-336`, and `specs/PRODUCT.md`. No decision was made by Quill.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, this receipt. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` (`plan.md` states why). `AGENTS.md` was not edited: it is outside the feature directory and outside the named file set. `.specify/feature.json` was already pinned to `specs/DEV-336` and is gitignored.

**Owner checkbox**: none. Gate 1 is not claimed.

**Analyze fix round 1** (2026-10-19): Propagated brief Q6 into `spec.md` FR-003, FR-005, acceptance scenarios, edge cases, and SC-004/SC-005; `plan.md` set normalization and equality; `tasks.md` T001, T002, T003, T004, and T005. Propagated brief Q7 into `spec.md` FR-006, `plan.md` mapper, and `tasks.md` T006. No new scope and no implementation.

**Resolved** (brief `specs/DEV-336/brief.md`, section "Phase 2 drafting clarifications — Patron Q6-Q7"):

- Resolved: When a source collection contains a null string, construction and mapping keep that element, collapse duplicate nulls, and do not filter or reject it. Equality is null-safe, and set hash aggregation contributes 0 for a null element. Declared public element types stay `string`. Link: `brief.md` Q6.
- Resolved: `MovieFeaturesMapper` is a public static class. Its sole public mapping member is `public static MovieFeatures From(Movie movie)`. No instance state or construction. Link: `brief.md` Q7.

**Plan challenge refinement round 1** (2026-10-19): Applied the numbered list from brief section "Phase 2 plan challenge decisions" and subsection "Default-array branch resolved from bounded recon". `brief.md` and `CONCLUSIONS.md` were not edited. No implementation, gate, commit, or new file.

- Applied 1: `plan.md` set normalization cites Compass verification item 1 (`findings-DEV-336-Compass:12`): .NET 10 `ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)` on `{"A", null, "a"}` does not throw, count 2, `Contains(null)` true, `SetEquals` true. Q6 retained.
- Applied 2: `spec.md` FR-005, `plan.md` equality, and `tasks.md` T003 require a commutative per-set unchecked sum or XOR of `StringComparer.OrdinalIgnoreCase.GetHashCode`, with 0 for a null element, then combining the completed set aggregates with scalars. FullPlot hash is 0 when null, otherwise `StringComparer.Ordinal.GetHashCode`. Wording that left an order-sensitive set enumeration hash open is removed. Sum versus XOR stays unpinned.
- Applied 3: `tasks.md` T001 names whole-value `GetHashCode()` invariance when adding or removing a null member in each set with identical scalars and non-null members. Equality still distinguishes that membership.
- Applied 4: `plan.md` records the non-nullable annotation versus runtime-null consumer caveat, and that surviving case spelling and iteration order are unspecified. Noted, no ticket. No consumer work.
- Applied 5: Test names follow `Method_Condition_Expected`. Properties use FsCheck.Xunit `[Property(MaxTest = 100)]` and `[Trait("Category", "Property")]`, with an explicit deterministic Replay seed and no `System.Random`. T002 creates `MovieFeaturesPropertyTests.cs`; T005 extends that same file afterward. No extra property-test file.
- Applied 6: T004 covers persisted MetaScore 0, 100, and null. T006 throws `ArgumentNullException(nameof(movie))` and does not pin message text. No-fallback wording uses `Movie.Path`. Frozen-scope capitalization was already "Anything else" in `brief.md` and `plan.md`; those sentences were left as written.
- Applied 7: `spec.md` edge cases and `plan.md` normalization document a deliberately boxed `default(ImmutableArray<string>)` as unsupported input, with no promised normalization or exception, on direct construction and on a hypothetical upstream `MovieMetadata` call. No `IsDefault` branch, upstream edit, or test. No claim that the input is impossible in general, and no claim about the unverified BCL fast-path. Basis: `recon-DEV-336:55-85`. No task schedules that input.
