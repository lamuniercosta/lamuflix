# DEV-297 brief

Status: GRILL CLOSED (Phase A), 11 of 12 questions used (Q12 not needed). Q1-Q11 ruled and mirrored from CONCLUSIONS.md. OWNER ANSWERED 2026-09-29 (see "Owner rulings" at end): combined Q4/Q5/Q11 checkbox APPROVED; one extra analyze round authorised. O4 (user): the spec PR is skipped; the owner's direct O2 approval is the operative record and no checkbox remains. Plan challenge adjudicated (P1-P10 at end); plan freeze waits on Quill's fix list P-FIX.
Owner of this file: Keel. Rulings mirror `specs/DEV-297/CONCLUSIONS.md` (Patron).

## Grill answers

### Q1 — MovieSort / SortDirection representation — DECIDED
- Sealed Enumeration types (`Ardalis.SmartEnum`), not C# `enum`.
- Members exactly as the ticket lists them. `MovieSort`: `Title`, `Year`, `Rating`, `Runtime`. `SortDirection`: `Ascending`, `Descending`.
- Parse with `TryFromName`; serialize by `Name`.
- ~~No query execution in this ticket~~ SUPERSEDED by Q2: the live callers are rewired through the typed query and sort whitelist.
- Basis: DEV-297:12-22, DEV-297:33-36; constitution 1.2.0 §III and Coding Conventions → Enumerations; Patron ruling in CONCLUSIONS.md Q1.
- The dependency check is resolved in the Q1 addendum below.

### Q1 addendum: dependencies and test project (DECIDED)
- Add `Ardalis.SmartEnum` and `Ardalis.SmartEnum.SystemTextJson` to CPM (`Directory.Packages.props`), and reference them only where needed. Core references `Ardalis.SmartEnum`. Name conversion uses the SystemTextJson package. Recon (recon-DEV-297:54-57, 160-161) found neither package present.
- Create `tests/LamuFlix.UnitTests`, which does not exist yet (recon-DEV-297:117-118), to hold the FsCheck property test. DEV-297:31 names it, and the constitution's Testing Matrix places property tests there.
- Consumers (H5, Keel, per recon-DEV-297:150-161): `LamuFlix.Core` references `Ardalis.SmartEnum`. `FluentValidation` is already referenced by Infrastructure (recon-DEV-297:150-153), so no new reference there. No production project serialises Core `EnrichmentStatus` with System.Text.Json (recon-DEV-297:155-161), so `Ardalis.SmartEnum.SystemTextJson` is referenced only by `tests/LamuFlix.UnitTests`, where one test proves JSON round-trip by Name for `EnrichmentStatus`, `MovieSort` and `SortDirection`. No production project gains it in this ticket.
- Solution (M2, per recon-DEV-297:92-103): add `tests/LamuFlix.UnitTests` to `LamuFlix.sln` (format 12.00) under the `tests` solution folder.
- Versions (DECIDED, Patron, CONCLUSIONS.md:23, commit dd17a99): exact CPM pins `Ardalis.SmartEnum` 8.2.0 and `Ardalis.SmartEnum.SystemTextJson` 8.1.0. The two numbers deliberately differ; the JSON package accepts SmartEnum >= 8.1.0. Delivery checks: `./scripts/run-vulnerable-packages.ps1` exits 0, and the PR body records both versions. Consumers (CONCLUSIONS.md:25): Core references the base package; only `tests/LamuFlix.UnitTests` references the JSON package. Basis: CONCLUSIONS.md:27-31.
- Basis: constitution 1.2.0 §I, Technology Stack, Testing Matrix; PRODUCT.md §5 items 1–2; Patron ruling in CONCLUSIONS.md.

### Q2: scope of the "Replaces EntityExtensions.cs" acceptance criterion (DECIDED, AMENDED by Patron)
- Remove `DynamicQuery<T>` (EntityExtensions.cs:13) and `DynamicSort<T>` (:123) in this ticket.
- Rewire the live callers MovieService.cs:169, :174, :372 to go through the typed query and the `MovieSort` whitelist.
- Current browse behaviour and the external contract must be preserved.
- Ownership: no DEV-282 sibling owns the predicates or the removal. DEV-298 owns the BrowseAsync port and DEV-299 owns BrowseMoviesQueryHandler (recon-DEV-297:111-114).
- Basis: DEV-297:9-10, 33-36; recon-DEV-297:31, 111-114; constitution 1.2.0 §III; Patron ruling in CONCLUSIONS.md Q2.

### Q3: Core's `EnrichmentStatus` becomes a SmartEnum (DECIDED)
- Convert `src/LamuFlix.Core/Domain/EnrichmentStatus.cs` to `SmartEnum<EnrichmentStatus,int>`, keeping Pending=0, Enriched=1, NotFound=2, Failed=3 unchanged.
- Adapt `src/LamuFlix.Core/Domain/Movie.cs` and the existing tests `tests/LamuFlix.Test/Domain/MovieFixture.cs`, `MovieTests.cs` and `PropertyTests.cs`.
- Leave the Data-layer `MovieEnrichmentStatus` untouched. No EF or schema change.
- Basis: DEV-297:18; constitution 1.2.0 §IV and Coding Conventions; recon-DEV-297:163-178; PRODUCT.md §5 items 3 and 6; Patron ruling in CONCLUSIONS.md Q3.

### Q4: placement (PARTLY DECIDED; owner checkbox combined with Q5/Q11)
- Decided:
  - `MovieQuery`, `RuntimeRange`, `YearRange`, `Page`, `MovieSort` and `SortDirection` go in `src/LamuFlix.Core/Library/`.
  - `MovieQueryValidator` goes in `src/LamuFlix.Infrastructure/Library/`, for the canonical handler path.
- Owner checkbox, now merged into the combined Q4/Q5/Q11 checkbox under Q5 (carry to the spec PR; Gate 1 stays closed until it is answered). Original Q4 wording: may DEV-297 temporarily place explicit, non-reflection filter/sort predicates for the legacy `Data.Models.Movie` path in `LamuFlix.Web`, departing from §III's Infrastructure placement until DEV-298/DEV-299 migrate browse? If not, which ticket or constitution change owns the migration needed to meet DEV-297:34?
- Basis: DEV-297:12-28, 33-36; constitution 1.2.0 §I, §III, Known Technical Debt; recon-DEV-297:24-27, 126-137; Patron ruling in CONCLUSIONS.md Q4.

### Q5 — Legacy behaviour vs typed model: BLOCKED, STRUCTURAL; combined with the Q4/Q11 owner checkbox
- **Conditional design** (Patron's preference; applies only if the owner approves):
  - A compatibility path local to `LamuFlix.Web`. It adapts the **existing** `QueryParams` and `MoviesFilterViewModel` directly (per Q11, no new request record) through explicit expressions with no reflection, plus a closed `LegacyMovieSort` Enumeration: Id, Title, Year, Duration, ImdbRating, MetaScore, RottenTomatoes.
  - It preserves every Razor sort and filter choice: SearchField (case-insensitive substring), Year, DirectorId, CollectionId, GenreIds, ActorIds.
  - An unknown or empty SortBy falls back to `Id desc` instead of throwing. Only an exact `desc` sorts descending.
  - Legacy-only fields are NOT added to `MovieQuery`, and no current UI choice is dropped.
  - The canonical `MovieQuery` is consumed first by DEV-299.
- **Combined Q4/Q5/Q11 owner checkbox**: one checkbox, carried to the spec PR. Gate 1 stays closed until it is answered. Sources: CONCLUSIONS.md:97 (Q4), :117 (Q5), :201 (Q11).
  - [ ] May DEV-297 replace the live legacy reflection path with a temporary, explicit Web compatibility path over the existing MVC request types (`QueryParams`, `MoviesFilterViewModel`) that preserves the wider legacy UI fields? This covers three things: (1) non-reflection filter and sort predicates for `Data.Models.Movie` are placed in `LamuFlix.Web`, a temporary departure from constitution §III's Infrastructure placement; (2) canonical `MovieQuery` is not built on this path, and DEV-299 is its first consumer, which clarifies DEV-297:9-10's replacement wording; (3) the MVC path runs without the canonical handler validation decorator (§V). All three hold until DEV-298/DEV-299 migrate browse. If the owner says no: which ticket or constitution change should own the full migration needed to meet DEV-297:34?
- Facts: recon-DEV-297:24-27, 126-140. No tests cover browse today.
- Basis: DEV-297:9-22 and 34; recon-DEV-297:111-114, 24-27 and 126-137; constitution 1.2.0 §III (lines 147-163); PRODUCT.md §5; CONCLUSIONS.md Q5.

### Q6 — NULLS LAST and page size: DECIDED
- Explicit `NULLS LAST` for nullable keys, in both directions. This applies to the conditional compatibility sort and to the canonical `MovieSort` contract.
- The Razor `QueryParams` binding and fixed `Page=1` stay unchanged. Legacy PageSize is not silently clamped, and the MVC response contract does not change.
- The 1-100 Size rule and the positive-Number rule belong to `MovieQueryValidator` only.
- A new internal compatibility query would need validation to its own contract (§V). Q11 settles this: no new compat query type is created, so no new validator exists on the legacy path.
- Basis: constitution 1.2.0 §III (154-163) and §V (196-199); DEV-297:23-27; recon-DEV-297:24-27, 126-137; CONCLUSIONS.md Q6.

### Q7 — Query-string codec and round-trip property: DECIDED, AMENDED
- A static `MovieQueryString` (Format/TryParse) in `src/LamuFlix.Core/Library/`, using the BCL only.
- Encoding:
  - Arrays are repeated keys, in order.
  - Enumerations are written by Name.
  - Nulls are omitted.
  - Sort, direction and page are always emitted. Clarification (not a decision change): "page" means the Page value, i.e. both the `page` and `pageSize` keys (brief:71; absence fails TryParse per brief:91).
  - **An empty `Text` is valid (unless the validator expressly rejects it) and is encoded distinctly from null.**
  - The keys are `[assumed]` (ASSUMPTIONS.md): text, genreIds, actorIds, runtimeMin, runtimeMax, runtimeIncludeUnknown, yearMin, yearMax, statuses, inWatchlist, sort, direction, page, pageSize.
- `MovieQuery` uses order-preserving sequence equality over its three ImmutableArray members. Default record equality compares them by reference.
- FsCheck property in `tests/LamuFlix.UnitTests/Library/`:
  - The generator produces validator-accepted queries and includes URL-hostile characters.
  - One property asserts TryParse succeeds and the parsed query equals the original after Format/Parse.
  - A second property proves the generator's values pass `MovieQueryValidator`.
  - The valid domain is not narrowed to make the property pass.
- `MovieQueryValidator` boundary example tests.
- Basis: DEV-297:23-31 and 35; constitution 1.2.0 §I, §III (160-163), Testing Matrix (285-288); CONCLUSIONS.md Q7.

### D1 — Empty ranges in the Q7 codec: DECIDED (Patron, option B)
- Refines Q7's "nulls are omitted" for the two range objects only. No new validator rule: `MovieQueryValidator` does NOT reject empty ranges.
- `Runtime` non-null → `Format` always emits `runtimeMin`, `runtimeMax` and `runtimeIncludeUnknown`, writing an empty value for each null bound.
- `Year` non-null → `Format` always emits `yearMin` and `yearMax`, writing an empty value for each null bound.
- Absent range keys = null range. Present key with empty value = null bound inside a present range. So `RuntimeRange(null,null,false)` and `YearRange(null,null)` stay distinct from a null range and round-trip unchanged.
- A partially present range (some but not all of its keys) is malformed: `TryParse` fails (see the TryParse failure policy below).
- Every other null value stays governed by Q7.
- Basis: DEV-297:12-27, 30-31, 35; constitution 1.2.0 §III (160-163); CONCLUSIONS.md D1 (commit 065467c); ASSUMPTIONS.md.

### TryParse failure policy (M9) — DECIDED (Keel, per CONCLUSIONS.md D1)
- `TryParse` returns false (no exception, no partial result) when: an integer or boolean value is malformed; an Enumeration value is not an exact `TryFromName` match; a scalar key (`text`, range keys, `inWatchlist`, `sort`, `direction`, `page`, `pageSize`) appears more than once; a range is partially present (D1); or `sort`, `direction`, `page` or `pageSize` is absent (Q7 always emits them).
- An empty value is a null bound only for range keys (D1); `text=` with an empty value is the empty string, and an absent `text` is null (Q7). An empty value for any other key is malformed.
- Unknown keys are ignored, so a URL carrying unrelated parameters still parses. `TryParse` does not validate: range order and page limits belong to `MovieQueryValidator`.
- Each rule gets one example test in `tests/LamuFlix.UnitTests/Library/`.
- Basis: DEV-297:30-31, 35; constitution 1.2.0 §III (160-163), §V (validation lives in the validator); CONCLUSIONS.md Q7, D1.

### Q8 — Tests: DECIDED
- **(a) Conditional on the owner checkbox:** characterization tests for the Web compatibility path go in `tests/LamuFlix.Test`, which references Web and has InternalsVisibleTo.
  - They cover every sort key, both directions, null placement and every predicate.
  - They are written first, against the legacy helpers, before the helpers are replaced.
- **(b)** An EF translation smoke test on the existing PostgreSQL `LamuFlixContextFactory`/Testcontainers fixture.
  - **Recorded risk:** production uses Pomelo MySQL 8.0.31, so MySQL translation is unproven. This is accepted, and ADR-0009 records it. No MySQL Testcontainers dependency is added.
- **(c)** Tests pinning the values (0-3) and names of Core `EnrichmentStatus` go in `tests/LamuFlix.UnitTests`. The existing assertions in MovieFixture, MovieTests and PropertyTests are adapted without weakening them.
- **(d)** The full `dotnet test` suite and the Core Stryker gate (>= 80) must pass. Stryker is scoped by command line, and the root `stryker-config.json` is not edited.
- Basis: recon-DEV-297:163-178, 35-36 and 143-144; constitution 1.2.0 §IX, Technology Stack; CONCLUSIONS.md Q8.

### Q9 — ADR-0009: DECIDED
- The implementer writes `docs/adr/ADR-0009.md` at exactly that path, as a tasks.md item. Patron and Keel do not author it in Phase A.
- Format: the house style of ADR-0010, with `Status: Accepted` (the proposed status; the owner accepts it by merging), ticket and date metadata, and one page.
- Content:
  - Concise Context, Decision and Consequences.
  - The typed Core model.
  - The closed sort Enumerations.
  - The validator.
  - The URL codec and structural equality.
  - The pinned EnrichmentStatus values.
  - The NULLS LAST rule.
  - Removal of `DynamicQuery<T>`/`DynamicSort<T>`.
- **A conditional compatibility/retirement section**, included only if the owner approves the combined Q4/Q5/Q11 checkbox. It names DEV-298 and DEV-299 as the retirement tickets and states that translation is evidenced on PostgreSQL only (MySQL is unproven).
- Basis: DEV-297:28-29 and 36; constitution 1.2.0 Documentation Rules; CONCLUSIONS.md Q9.

### Q10 — Closing bar, scope, gates, rounds: DECIDED
- See **Plan decisions** below, which mirror CONCLUSIONS.md Q10.
- Basis: DEV-297:9-36; task-pipeline:23-27, 61-76, 86-95; specs/DEV-296/brief.md:72-75; CONCLUSIONS.md Q10.

### Q11 — Legacy request validation boundary: DECIDED, AMENDED
- **No** `LegacyMovieBrowseQuery` record and **no** new Web validator.
- The conditional Web bridge adapts the existing `QueryParams` and `MoviesFilterViewModel` directly, through the closed `LegacyMovieSort` parser and explicit predicates.
- Today's page, id, year and text request behaviour is preserved exactly. The only refinements are the ones already ruled: the unknown or empty sort falls back to `Id desc` (Q5), and NULLS LAST (Q6).
- No new validation failures or thresholds. There is no empty-list-on-invalid behaviour, because it would contradict §V's 422 rule, and no 200-character cap, because the ticket gives no basis for one.
- `MovieQueryValidator` is still required for the canonical typed model.
- The transitional MVC path does not go through the canonical handler validation decorator. The combined owner checkbox (under Q5) discloses this. DEV-299 owns its retirement.
- Basis: DEV-297:12-27; recon-DEV-297:24-27, 126-137; constitution 1.2.0 §V (196-199); PRODUCT.md §5; CONCLUSIONS.md Q11.

## Plan decisions (mirrors CONCLUSIONS.md Q10)

### Closing bar
- Every question asked is ruled in CONCLUSIONS.md and mirrored here.
- The single combined Q4/Q5/Q11 owner checkbox (under Q5) is carried verbatim to the spec PR. **Gate 1 stays closed until the owner answers it.**
- If the owner rejects the checkbox, reopen this brief for these items only: Q5, the compat part of Q6, Q8(a)/(b), Q11, and the ADR-0009 compatibility section. The canonical scope proceeds unchanged, except the deletion of `DynamicQuery<T>`/`DynamicSort<T>` (task order step 7), which stops and waits for the reopened brief because its three live callers need a ruled replacement first (H1).

### Frozen scope
**Unconditional:**
- Ardalis.SmartEnum and Ardalis.SmartEnum.SystemTextJson in CPM (Q1 addendum).
- The Core EnrichmentStatus SmartEnum conversion (Q3).
- In `Core/Library`: `MovieQuery`, `RuntimeRange`, `YearRange`, `Page`, `MovieSort`, `SortDirection`, plus the `MovieQueryString` codec and structural equality (Q4, Q7).
- `Infrastructure/Library/MovieQueryValidator` (Q4, Q6).
- The new `tests/LamuFlix.UnitTests`, holding the FsCheck properties, the validator boundary tests and the status pin tests (Q1 addendum, Q7, Q8c).
- Removal of `DynamicQuery<T>`/`DynamicSort<T>` from `src/LamuFlix.Web/Extensions/EntityExtensions.cs` (Q2).
- `docs/adr/ADR-0009.md` (Q9).
- The tests the above directly requires.

**Conditional on the owner checkbox:**
- The Web compatibility path over the existing `QueryParams`/`MoviesFilterViewModel`, `LegacyMovieSort` and explicit predicates (no new request record or validator, Q11), and rewiring `MovieService.cs:152-180` and `:356-395`.
- The `LamuFlix.Test` characterization tests and the PostgreSQL smoke test (Q8a/b).

**Out of scope:**
- Anything not listed above goes to a follow-up issue and is not a finding in this round.
- No change to `web/src/api`, the OpenAPI contract, the EF schema, the Data enum, `Features:LocalPlay` or the root `stryker-config.json`.

### Task order
1. Packages in CPM, then the Core EnrichmentStatus conversion, with the three existing tests adapted (no weakened assertions).
2. Core Library types, the `MovieQueryString` codec and structural equality.
3. `MovieQueryValidator`.
4. Create `tests/LamuFlix.UnitTests`: the FsCheck round-trip property, the generator-in-domain property, the validator boundary tests and the EnrichmentStatus pin tests.
5. (Conditional) Characterization tests in `LamuFlix.Test` against the **current** legacy helpers: every sort key, both directions, null placement and every predicate. Commit them green before any replacement.
6. (Conditional) The Web compatibility path (`LegacyMovieSort` plus explicit predicates over the existing request types) and the MovieService rewire. Then verify the intended deltas explicitly: an unknown sort falls back to `Id desc`, and NULLS LAST. Page, id, year and text behaviour must not change (Q11).
7. Delete `DynamicQuery<T>`/`DynamicSort<T>`. No references may remain. This step runs only after step 6 is complete and green, because `MovieService.cs:169`, `:174` and `:372` call the helpers; deleting first breaks the build.
   - **H1 reject path:** if the owner rejects the combined Q4/Q5/Q11 checkbox, steps 5, 6, 7 and 8 do NOT run. Implementation stops after step 4; steps 9 and 10 wait for the reopened brief. This brief is reopened per the Closing bar for Q5, the compat part of Q6, Q8(a)/(b), Q11 and the ADR-0009 compatibility section. Step 7 is never labelled "proceed regardless": the deletion is unconditional scope, but it cannot land until a ruled replacement for the three callers exists.
8. (Conditional) The EF translation smoke test on the PostgreSQL fixture.
9. `docs/adr/ADR-0009.md`, per Q9.
10. Run the gates.

### Gate expectations
Report each gate's native exit code. A gate that has not run is not a pass. A scope-empty SKIPPED blocks; a configured OPT-OUT does not.
- `./scripts/run-roslyn-analyzers.ps1`
- `./scripts/run-cyclomatic-complexity.ps1` (at most 15), then the refactor gate at `-Threshold 6`
- `./scripts/run-jetbrains-inspectcode.ps1`
- The property-test gate and the vulnerable-package scan (task-pipeline:86-95)
- `dotnet format --verify-no-changes`
- The full `dotnet test`, including ArchitectureTests. Recon (recon-DEV-297:45-57, 193-195) shows no assembly-wide Core allowlist, only denylists plus a `Core.Features.*` namespace allowlist; `Core/Library` and `Core/Domain` are outside it, so no ArchitectureTests edit is needed (M7)
- Core Stryker at 80 or above, scoped by command line
- Web gates (`./scripts/run-web-gates.ps1`) are NOT in this ticket's required gate set (DECIDED, Patron, CONCLUSIONS.md:11-19, commit 7c73979). Do not schedule them. DEV-297 changes no file under `web/`, and the web gates belong to the `web-implement` stage. They are not run, and are reported as `N/A: no web/ diff`, never as PASS or as a SKIPPED gate. This does not relax the rule that a scope-empty SKIPPED from a REQUIRED gate (every gate listed above) blocks.

### Test strategy
Q7 and Q8 above, placed as follows:
- UnitTests: properties, validator boundaries and status pins.
- LamuFlix.Test: compat characterization and the PostgreSQL smoke test.
- The existing Movie tests are kept.

### Round caps and severity
- Spec Kit analyze/fix: 3 rounds.
- Review: 2 rounds.
- Remediation: 2 commits per round.
- Past a cap, report `blocked`.
- Verified Critical and High findings always block. In-scope Medium behaviour, spec or gate defects also block.
- Low findings, and Medium maintenance findings that do not block, go to follow-up tickets (Rigger files them).

### Recorded risks
- MySQL translation is unproven on the production Pomelo MySQL 8.0.31. The evidence is PostgreSQL-only (Q8b), and ADR-0009 records this.
- Behaviour deltas on the legacy path, both deliberate:
  - An unknown sort now falls back to Id desc (today it returns 500).
  - NULLS LAST ordering.
- The transitional MVC path has no handler validation decorator (Q11). The owner checkbox discloses this, and DEV-299 retires the path.

## Owner rulings (2026-09-29, answered directly to Keel)
Appended at the end so every existing `brief:N` cite in spec/plan/tasks stays valid.

- **O1 — Round cap (brief Q10, "Round caps and severity"):** the owner authorises ONE extra Spec Kit analyze/fix round (round 4) past the 3-round cap. This lifts the "no fourth round" disposition in CONCLUSIONS.md:5 for this one round only. Scope of the correction phase: Quill's fix list from the DEV-297 note R3 Final Preflight (tasks:164 `[US5]` on T036; tasks:165 and :276 "6a/6b/6c/6d"; advisory blank line before the :165 Note), then Keel's round-4 `/speckit-analyze`, plan challenge and freeze. If round 4 is not clean, report `blocked` again; no round 5 without a new owner ruling.
- **O2 — Combined Q4/Q5/Q11 owner checkbox: APPROVED.** DEV-297 replaces the reflection `DynamicQuery<T>`/`DynamicSort<T>` with the temporary, explicit, non-reflection Web compatibility path over `QueryParams`/`MoviesFilterViewModel` described under Q5, until DEV-298/DEV-299 migrate browse. The owner accepts all three parts: (1) predicates for `Data.Models.Movie` in `LamuFlix.Web` (temporary constitution §III departure); (2) canonical `MovieQuery` is not built on this path, DEV-299 is its first consumer; (3) no §V handler validation decorator on the MVC path. Consequences: the H1 reject path (brief:169) does not apply; Phases 5 and 6a-6d are no longer conditional and run in order; ADR-0009 records the departure and its retirement by DEV-298/DEV-299. The checkbox on the spec PR stays as the formal record for the owner to tick; it is not pre-ticked.
- The "if no: which ticket owns the migration" follow-up is moot.
- **O3 — Round 5 (2026-09-29, owner):** round 4 was not clean (1 CRITICAL, 1 blocking MEDIUM, 5 LOW; DEV-297 note "Round-4 speckit-analyze Receipt"). The owner authorises a FULL round 5: Quill fixes all seven round-4 findings (C1, F1, F2, F3, F4, D1, L1), then Keel runs a full round-5 `/speckit-analyze`. If round 5 is not clean, report `blocked`; no round 6 without a new owner ruling. tasks:144 stays as written (6c is deliberately not conditional); the "change line 144" text seen in Quill's prompt was not an owner instruction. Keel corrected the stale CONCLUSIONS cites at brief:21, :51, :183 in place (F3, line count unchanged).
- **O4 — Spec PR skipped (2026-09-29, user instruction via Conductor; DEV-297 note "Owner Instruction Receipt"):** the DEV-297 spec PR is not opened. After Phase 2 completes, the chain goes straight to task-pipeline Phase 3. The owner's direct O2 approval replaces the checkbox tick, so no Gate 1 checkbox remains in any artifact. O2's consequences hold without conditions.

## Plan challenge adjudication (Keel, 2026-09-29)
Inputs: findings-DEV-297-risk (Sentry), -standards (Ledger), -spec (Compass), all at HEAD 8a3a07f. Each finding was verified at file:line in the worktree. Severity follows brief:196-197. A Low whose fix is wording in the unfrozen artifacts goes into the pre-freeze fix list; filing a ticket for it would add nothing. No finding changes ticket text or departs from the constitution, so nothing escalates to the user.

- **P1 — property-gate `-Project` path** (Sentry M1 = Ledger F1 = Compass M1). ACCEPTED, MEDIUM gate defect, blocking. run-property-tests.ps1:80-86 exits 1 on any value that does not end in `.csproj`. plan:257 and tasks:200 (T043) pass a directory. Fix: `-Project tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` in both places. Keep it scoped to that project; do not omit `-Project`. Ledger's HIGH is merged into this MEDIUM: it is a one-token fix, and the gate itself would catch the error.
- **P2 — UnitTests csproj runner shape** (Sentry M2 = Ledger F2 = Compass M2). ACCEPTED, MEDIUM gate/test defect, blocking. Ledger's discovery claim holds and Sentry's does not. _gate-common.ps1:204-208 filters the sln-listed candidates as well, and keeps only files whose text contains `Microsoft.NET.Test.Sdk` or `<IsTestProject>true`. Without that SDK reference, UnitTests is invisible to the property gate and to Stryker's eligible set. Fix plan:154 and tasks:22 (T004) to mirror tests/LamuFlix.Test/LamuFlix.Test.csproj:3-17: `OutputType Exe`, `IsPackable false`, `IsTestingPlatformApplication false`; PackageReferences `Microsoft.NET.Test.Sdk`, `xunit.v3`, `xunit.runner.visualstudio`, `NSubstitute`, `Shouldly`, `FsCheck`, `FsCheck.Xunit.v3`, plus `Ardalis.SmartEnum.SystemTextJson`. Every one of these is already pinned in Directory.Packages.props, so this adds no new dependency (care-list #1 is not triggered).
- **P3 — `Category=Property` trait not applied at authoring time** (Sentry L1 = Ledger F4 = Compass M3). ACCEPTED, MEDIUM gate defect, blocking. Missing, it would turn a required gate into a scope-empty SKIPPED (K9). The existing property tests use the trait explicitly (PropertyTests.cs:13). Fix: T022 and T024 must state `[Trait("Category", "Property")]` on each FsCheck property. T021 is a fixture and carries no trait.
- **P4 — T024 file placement contradicts the plan tree** (Ledger F3). ACCEPTED, MEDIUM spec/plan defect, blocking, because the artifacts disagree. plan:126 and :197 name `MovieQueryGeneratorTests.cs`; tasks:104 says "same test file". Ruling: a separate file, as in the plan. Fix tasks:104 so T024 creates `tests/LamuFlix.UnitTests/Library/MovieQueryGeneratorTests.cs`.
- **P5 — EnrichmentStatus conversion forces test-API rewrites** (Ledger F5 = Compass L4). ACCEPTED, LOW, folded into P-FIX. Verified: SmartEnum members used as InlineData arguments at MovieTests.cs:52-67 fail with CS0182; `Enum.GetValues<EnrichmentStatus>()` at PropertyTests.cs:156; a constant `switch` at MovieFixture.cs:19. Fix T008, and plan:208 to match: InlineData becomes MemberData/TheoryData (constitution §IX), `Enum.GetValues` becomes `EnrichmentStatus.List`, and the switch matches on `.Value` or `.Name`. Assertions stay unchanged (FR-005).
- **P6 — O2 retired the conditional/reject-path text, but the artifacts still present it as live** (Compass L1). ACCEPTED, LOW. Blocking anyway, because O4 removes the spec PR and checkbox that this text points to. The artifacts would otherwise tell the implementer to wait for a tick that will never come. Fix: rewrite every conditional, "if owner approves", "checkbox on the spec PR" and reject-path clause so that Phases 5, 6a-6d and 7 are unconditional and in order, citing brief:210 (O2) and O4. The known sites are spec:82, :86, :90, :119, :149, :150, :154-156; plan:9, :56-59, :190, :218, :232 (the ADR compat section becomes required), :233, :241; tasks:125, :129, :144, :166, :185 (the T039 compat/retirement section becomes required), :206 (T049), :207 (T050), :222, :233, :277. spec:154-156 becomes "Owner decision (recorded): APPROVED 2026-09-29, brief:210 O2; spec PR skipped, brief O4", with the three parts kept as the record. plan:41 "Gate 1: §2.3 Care List" is a constitution-check heading and stays. The deletion ordering (T036 only after 6b is green) stays exactly as it is.
- **P7 — US5 AS4 names non-nullable sort keys** (Compass L2). ACCEPTED, LOW spec defect. The scenario cannot be exercised as written. Verified at src/LamuFlix.Data/Models/Movie.cs: `Title` is `string?` (:8), while `ImdbRating` (:13), `RottenTomatoes` (:14) and `MetaScore` (:15) are non-nullable. Fix spec:95 AS4 so it names `Title`. FR-014 (spec:123) gets a note that `Title` is the only nullable legacy sort key and so the only key where NULLS LAST is observable. T028 and T035 null-placement wording should name `Title`. The NULLS LAST delta (brief:203) itself is unchanged.
- **P8 — Core/Library sits outside the Core.Features architecture guard** (Compass L3). NO ARTIFACT CHANGE. The placement is decided: Patron Q4 at CONCLUSIONS.md:97, and M7 at brief:181 already records that Core/Library is outside the `Core.Features` allowlist. Governance gap: no positive allowlist covers `LamuFlix.Core.Library`. This goes to **Patron** to decide whether Rigger files a follow-up ticket to extend ArchitectureTests. It does not block freeze.
- **P9 — TryParse complexity budget** (Sentry L2). ADVISORY, no artifact change. Guidance for the implementer of T018: build `TryParse` from small per-key parsers (int, bool, enumeration, range pair, required-key check) from the start. A single method with many branches will fail the refactor gate at 6 (brief:177) and use up the 2-commit remediation cap (brief:194).
- **P10 — `[P]` on T039-T051 vs tasks:240** (Sentry advisory). DISMISSED. tasks:240 forbids parallel *phases*. A `[P]` on independent gate runs inside Phase 8 does not contradict it.

**P-FIX (owner: Quill, one pass):** P1, P2, P3, P4, P5, P6, P7, applied to spec/plan/tasks only. brief.md and CONCLUSIONS.md are not edited, and no existing brief:N cite may move. This is a plan-challenge correction pass, not a Spec Kit analyze round. O3's cap governs analyze rounds, round 5 closed clean, and no fresh owner ruling is needed because nothing changes ticket scope or the constitution. Keel then verifies each item at file:line (no `/speckit-analyze`), freezes, and writes the plan-freeze receipt. If any item is still missing after that one pass, the result is `blocked`, not another pass.

## Phase 3 remediation round 2 spec (Keel, 2026-09-29)
Appended at the end so every existing `brief:N` cite stays valid. This is a decision change recorded before Cog acts (task-pipeline:72). It implements K3 in the DEV-297 note. Facts: recon-DEV-297:277-743, abbreviated `R` below (e.g. R:303). Round 2 of 2 (task-pipeline:19): at most 2 commits, starting from 60cc811. The refactor gate stays at 6 (brief:177), InspectCode stays at WARNING+ (brief:178), Core Stryker stays at 80 or above (brief:104, :182). No suppressions, no `[ExcludeFromCodeCoverage]`, no Stryker config or ignore edits, no threshold arguments.

### RS-0: Round-1 defect that round 2 must reverse (blocking, spec defect)
- Round 1 narrowed the valid domain. `MovieQueryFixture` RuntimeGen and YearGen no longer generate half-bounded ranges (R:687-721). This violates brief:77 ("The valid domain is not narrowed to make the property pass") and T021 ("covers the full domain").
- The parser now rejects half-bounded ranges:
  - `HasMatchingRuntimeBoundPresence` (`RuntimeMin.HasValue == RuntimeMax.HasValue`, R:628-629).
  - The `state.YearMin.HasValue != state.YearMax.HasValue` disjunct in `TryYear` (R:574-575).
  - Both contradict spec:178 ("min without max is valid; max without min is valid") and D1 (brief:85: a present key with an empty value is a null bound). D1's partial-range rule (brief:86, spec:179) is about key PRESENCE (`state.Seen`), never value presence.
- Required: restore the pre-round-1 `Gen.Choose(0, 2)` shape switch in RuntimeGen and YearGen exactly as recorded at R:689-697 and R:708-716. Keep the removed explicit type arguments (R:681-705); they are InspectCode cleanups and not a weakening. Delete both value-presence checks listed above.
- The other round-1 test edits are accepted (R:722-741): `!` became `ShouldNotBeNull()`, which strengthens the tests, and the unused usings were removed. Property count and MaxTest are unchanged (R:742-743).

### RS-1: `TryRuntime` / `TryYear` shape (T041, T042; MovieQueryString.cs:233-280, :303-329)
- `TryRuntime` target, complexity at most 4:
  ```csharp
  private static bool TryRuntime(ParseState state, out RuntimeRange? runtime)
  {
      runtime = null;
      if (!HasAnyRuntimeValue(state))
      {
          return true;
      }

      if (!HasCompleteRuntimeValues(state) || state.RuntimeIncludeUnknown is not { } includeUnknown)
      {
          return false;
      }

      runtime = new RuntimeRange(state.RuntimeMin, state.RuntimeMax, includeUnknown);
      return true;
  }
  ```
- Keep `HasAnyRuntimeValue` and `HasCompleteRuntimeValues` (R:606-614) unchanged.
- Delete `TryGetRuntimeIncludeUnknown` (R:616-623). That removes the InspectCode `ConvertTypeCheckPatternToNullCheck` at :315. The target uses `is not { } includeUnknown` for the same reason; never write `is not bool`.
- Delete `HasMatchingRuntimeBoundPresence` (R:628-629, RS-0).
- `TryYear` target, complexity at most 3:
  1. `year = null;`
  2. Return true if `!HasAnyYearValue(state)`.
  3. Return false if `!HasCompleteYearValues(state)`, where the new expression-bodied helper is `state.Seen.Contains(YearMinKey) && state.Seen.Contains(YearMaxKey)`, mirroring the runtime helpers.
  4. Otherwise `year = new YearRange(state.YearMin, state.YearMax); return true;`.

### RS-2: `Order` shape (T041; MovieServiceExtensions.cs:66-99)
- `Order` is identical at 17d0779 and 60cc811: 6 `if`s, complexity 7 (R:459-518). Round 1 did not introduce it. The first gate run did not list it (DEV-297:254-257), for a reason not yet explained. Fix it regardless.
- Target: a `private static readonly Dictionary<LegacyMovieSort, Func<IQueryable<Movie>, bool, IQueryable<Movie>>> Orderings` field. It maps Title to `OrderByTitle` and Year, Duration, ImdbRating, MetaScore and RottenTomatoes to `(query, descending) => OrderByValue(query, movie => movie.<Key>, descending)`, and has no Id entry.
- `Order` becomes the expression body `Orderings.TryGetValue(sort, out var order) ? order(query, descending) : OrderByValue(query, movie => movie.Id, descending)`, complexity 2.
- `OrderByTitle` and `OrderByValue<T>` stay unchanged (R:449-456). Do not use a switch statement or expression: its arms count as branches.
- Behaviour must stay the same: LegacyMovieSortTests (24) and MovieServiceEfTests (1) stay green, and they must not be modified.

### RS-3: NEW surviving mutants and their killing tests (Core Stryker, R:303-352)
Theory data uses TheoryData/MemberData (constitution §IX). Each parse test asserts the TryParse return value and, on success, `ShouldBe` equality with the expected `MovieQuery`. Every failure-case query string also carries valid sort/direction/page/pageSize, so only the rule under test fails.

| Mutant (R line) | Location at 60cc811 | Killing test (file: name) |
|---|---|---|
| 13, 14 (R:315-316) | MovieQuery.cs:78 Text/Runtime/Year `&&` | MovieQueryTests.cs: `Equals_IsFalse_WhenExactlyOneFieldDiffers` theory, rows Text, Runtime, Year |
| 15, 16 (R:317-318) | MovieQuery.cs:83 sequence `&&` | same theory, rows GenreIds, ActorIds, Statuses |
| 17, 18, 19 (R:319-321) | MovieQuery.cs:88 InWatchlist/Sort/Direction/Page `&&` | same theory, rows InWatchlist, Sort, Direction, Page. The base query sets every field to a non-default value; each row changes exactly one field |
| 35 (R:337) | MovieQueryString.cs:244 incomplete runtime | MovieQueryStringTests.cs: `TryParse_Fails_WhenRuntimeRangeIsPartial` theory, all 6 non-empty proper subsets of {runtimeMin, runtimeMax, runtimeIncludeUnknown} |
| 41, 42 (R:343-344) | :304 `HasAnyRuntimeValue` | same theory (max-only, min-only rows) |
| 43, 44 (R:345-346) | :309 `HasCompleteRuntimeValues` | same theory (min+includeUnknown, max+includeUnknown rows) |
| 45 (R:347) | :318 includeUnknown null | `TryParse_Fails_WhenRuntimeIncludeUnknownIsEmpty` (`runtimeMin=1&runtimeMax=2&runtimeIncludeUnknown=`) |
| 37 (R:339) | :256 bound presence | Mutant disappears when RS-0 deletes the helper. Guard test: `TryParse_RoundTrips_HalfBoundedRanges` theory: RuntimeRange(5,null,true), RuntimeRange(null,5,false), YearRange(2000,null), YearRange(null,2000); Format then TryParse returns true and is equal |
| 38 (R:340) | :271 `TryYear` | `TryParse_Fails_WhenYearRangeIsPartial` theory (yearMin only, yearMax only), plus the half-bounded year rows above |
| 39, 40 (R:341-342) | :283 `HasRequiredValues` | `TryParse_Fails_WhenRequiredKeyIsMissing` theory: sort, direction, page, pageSize each omitted alone. This extends T023, which covers pageSize only |

- Arithmetic: at 60cc811, 122 killed + 4 timeout = 126 of 172 detected (73.26%, DEV-297:337). The 17 NEW mutants (R:315-347) bring that to about 143/172 (83.1%) before RS-1/RS-2 remove mutants from the denominator. Killing every NEW mutant is the required margin.
- Equivalent-mutant clause: if `TryParse_Fails_WhenRuntimeIncludeUnknownIsEmpty` already passes before RS-1, and the `is not { }` guard's mutant still survives after RS-1, then the guard is unreachable because the per-key bool parser already rejects the empty value. Keep the test, name that mutant in the receipt as equivalent, and add no code to make it reachable.

### RS-4: PRE mutants (not required; listed so they are not read as regressions)
- MovieQuery.cs :50, :61-70, :75, :95 (R:303-312, :322): GetHashCode statement removals, normalisation and timeouts. They were also non-killed at 17d0779 (R:364-381).
- MovieQueryString.cs :29, :50, :93, :123, :125, :148, :150, :167, :172, :178, :187, :189, :198, :250, :334-338, :406 (R:323-336, :338, :348-352): the same set as 17d0779 (R:382-404). :250 goes away with RS-1.
- Cog may kill PRE mutants but is not required to; none is a round-2 exit criterion.

### RS-5: Commits and mandatory local pre-commit check
- Commit 1 (`DEV-297 - Restore half-bounded ranges and fix refactor-gate shapes`): RS-0 + RS-1 + RS-2 + RS-3.
- Commit 2 is reserved. Use it only if Commit 1's local check needs a fix, and never after Gauge's rerun (that goes to K4).
- Before EACH commit, run in the worktree and record the native exit codes in IMPLEMENTATION_RECEIPT.md. All must exit 0:
  1. `pwsh -NoProfile -File ./scripts/run-cyclomatic-complexity.ps1 -Threshold 6`
  2. `pwsh -NoProfile -File ./scripts/run-jetbrains-inspectcode.ps1`
  3. `pwsh -NoProfile -File ./scripts/run-mutation.ps1 -Project LamuFlix.Core` (score >= 80)
  4. `dotnet test tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj --nologo`, then `dotnet test tests/LamuFlix.Test/LamuFlix.Test.csproj --filter "FullyQualifiedName~LegacyMovieSortTests|FullyQualifiedName~MovieServiceEfTests" --nologo`
  5. `dotnet format --verify-no-changes`
- If any of these stays red, do not commit. Report `blocked` with the output.
- After the commit(s), Gauge reruns T040-T051 in full. A red required gate there means DEV-297 is `blocked` and goes to the user (K4). There is no round 3.

## Phase 4 review decisions (Patron, 2026-09-29; recorded by Keel)
Source: `specs/DEV-297/CONCLUSIONS.md` Phase 4 section (committed f6da6ff); DEV-297:420-422. Scope is unchanged. No owner checkbox. No source change. No gate run.

### P-1: API shape and project reference (DECIDED, Patron)
- K-F3: `MovieQuery.Sort` and `MovieQuery.Direction` are nullable (MovieQuery.cs:40,42). This is the confirmed Core API shape (section 2.3 #4). The values can be absent at construction so that FR-007 rejects them. Any query that FR-006 parses or formats successfully has both. Correction goes in the spec text (`spec.md:104` FR-001, `spec.md:143` SC-001), not in the code.
- K-F4: the Web-to-Core `ProjectReference` at LamuFlix.Web.csproj:19 is confirmed. The FR-011 compatibility path needs it. Correction goes in the plan text (`plan.md:153`), not in the code.
- Basis: spec.md:109,112,120-124,143-147; plan.md:150-154; PRODUCT.md:32-46; recon-DEV-297:402-415.

### P-2: follow-ups (DECIDED, Patron; Rigger records)
- K-F1: Rigger comments on DEV-298 that a leading `?` is a latent parser risk (MovieQueryString.cs:86) once wiring consumes the codec.
- K-F2: Rigger checks open tickets for the empty `EntityExtensions` class and the orphaned `ViewModelExtensions` helpers. If a suitable cleanup ticket exists, fold them into it. If not, file one follow-up dead-code ticket.
- K-F5 and K-F6: noted. No action, no ticket.
- Basis: recon-DEV-297:396-416; brief.md:191-198; PRODUCT.md:18,32-48.
