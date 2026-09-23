# Feature Specification: Restore six InspectCode warnings

**Feature Branch**: `feature/361-spec` (Phase A re-spec after the owner B1 answer). Round 1 Phase A merged as PR #3 from `feature/361-dev-361-spec`. Phase B implements on `feature/DEV-361`.  
**Created**: 2026-09-23 · **Revised**: 2026-09-23 (owner B1 scope change; re-baselined on `origin/main` `3cd5802` after DEV-360 merged)  
**Status**: Re-frozen after the Round 2 challenge (T018, 2026-09-23). The owner answered T020 **(A)** on 2026-09-23 and told Phase B to proceed without waiting for the re-spec PR to merge. Gate 1 is open. The T001 drift re-baseline changes counts and line numbers only. The scope, task set, and rulings are unchanged, and Patron's item-5 ruling for `AssistirFilme` adds no checkbox.  
**Input**: DEV-361 Scope & Technical Design (`task-DEV-361:14-23`) and acceptance criteria (`task-DEV-361:25-30`). Owner answer **[x]** on the B1 checkbox of merged PR #3, confirmed by the user through the Conductor on 2026-09-23.

## User Scenarios & Testing

### User Story 1 — Five inspections block real defects (Priority: P1)

As a maintainer, I get warning-level findings for the five downgraded inspections and can fix the concrete cases in their measured files. **Independent test:** the five `.editorconfig` values are `warning`. The five-target hits in `FilmesViewModel.cs`, `TagHelpers/Extensions.cs`, and `UnitTest1.cs` are resolved, and the AC2 run in SC-002 exits 0.

### User Story 2 — External writers remain supported (Priority: P2)

As an MVC consumer, I still have bindable properties even when code search finds no assignment to them. **Independent test:** each remaining false positive is suppressed only at its type or member, with a one-line reason naming the external writer. Genuinely dead members are fixed minimally.

### User Story 3 — New null-forgiving operators are flagged (Priority: P1, owner B1 reversal)

As a maintainer, I get a warning for any new `= null!` or postfix `!`. The 112 legacy sites (measured at `3cd5802`) stay working. Each is fixed where FR-008 allows, otherwise suppressed at its type or member with a reason naming why it is non-null or why the `!` preserves the existing contract. **Independent test:** `.editorconfig` has `resharper_nullable_warning_suppression_is_used_highlighting = warning`. All 112 measured hits are resolved or suppressed locally. No entity nullability, schema, DTO shape, or package changes.

### Edge Cases

- `TagHelpers/Extensions.cs:43,56` must compare `KeyValuePair` fields under the existing match guard. Changing the latent null-value behavior is out of scope (Patron Q2).
- `UnitTest1.TestMethod1` and `TestMethod2` are ignored but still compiled. DEV-280 comes later, so simplify their four dead branches now and quote both names (Patron Q3; `chain:7-11`).
- **B1 answered.** The owner ticked the reversal checkbox **[x]** on PR #3 and the user confirmed it. `resharper_nullable_warning_suppression_is_used_highlighting` becomes the sixth restoration. That widens this ticket to the files it flags: 18 at `3cd5802` (`research.md` "Re-baseline after DEV-360").
- DEV-360 already aligned entity nullability with the EF snapshot. DEV-361 changes no mapped EF Core property's nullability in either direction, because that is §2.3 item 3. The remaining `= null!` initializers (non-null columns, required join navigations, `DbSet<>`) are suppressed per type, citing EF Core materialization.
- DEV-360 added 11 behavior-preserving `!` reads of now-nullable columns in `FilmesServices.cs`. Two of them (`:74,75`) are inside `AssistirFilme`, behind `Features:LocalPlay`. Patron ruled that §2.3 item 5 does not fire for a comment-only member bracket there, as long as no executable line of `AssistirFilme` changes.
- `WorkerTests.cs:37,45` pass `null!` on purpose to assert `ArgumentNullException`. Those are intended suppressions, not defects.
- `EntityExtensions.DynamicQuery` has pre-existing CA1502 complexity 12. Editing the file puts it into the threshold-6 refactor gate, and `constitution.md:344-345` requires a helper-extraction fix. It is refactored by a pure-move extraction of private helpers in the same file (FR-010), subject to the owner plan-change choice (A). The method has no test coverage, and none may be added. If a pure-move extraction cannot bring it to ≤6, that is a hard stop: report `blocked`, and do not suppress, change thresholds, or grant a baseline exception.
- `JetBrains.Annotations` is not added. Any finding that needs it goes on the PR for a Patron follow-up (`task-DEV-361:22`).

## Requirements

### Functional Requirements

- **FR-001**: Set these **six** `.editorconfig` keys to `warning`: `resharper_unused_auto_property_accessor_global_highlighting`, `resharper_collection_never_updated_global_highlighting`, `resharper_usage_of_default_struct_equality_highlighting`, `resharper_condition_is_always_true_or_false_highlighting`, `resharper_condition_is_always_true_or_false_according_to_nullable_api_contract_highlighting` (`task-DEV-361:15-20,26`), and `resharper_nullable_warning_suppression_is_used_highlighting` (owner B1 answer [x], PR #3). Rewrite the stale rationale comments at `.editorconfig:67,69,72,74`.
- **FR-002**: Code fixes and AC2's InspectCode `-Files` target are limited to these **25** measured paths (`research.md`), and nothing else:
  - Five-target inspections, 3 files: `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs`, `LamuFlix.Web/TagHelpers/Extensions.cs`, `LamuFlix.Test/UnitTest1.cs`. FilmesViewModel and UnitTest1 also carry B1 hits.
  - B1 group D (Data), 11 files: `LamuFlix.Data/LamuFlixContext.cs`, and `LamuFlix.Data/Models/{Actor,Collection,Director,Genre,Movie,MovieActors,MovieDirectors,MovieGenre,Player,Temp}.cs`.
  - B1 group T (Test), 2 more files: `LamuFlix.Test/ApiDataModel.cs`, `LamuFlix.Test/WorkerTests.cs`.
  - B1 group M (Web models), 5 more files: `LamuFlix.Web/Models/{AlertModel,ErrorViewModel,FilterViewModel}.cs`, `LamuFlix.Web/Models/Helper/{QueryableResult,QueryParams}.cs`.
  - B1 group W (Web code), 4 files: `LamuFlix.Web/TagHelpers/{AlertsTagHelper,PaginationTagHelper}.cs`, `LamuFlix.Web/Extensions/EntityExtensions.cs`, `LamuFlix.Web/Services/FilmesServices.cs`.

  The out-of-scope WARNING+ inventory stays with the DEV-281 follow-up (`task-DEV-361:23,27`; owner scope change). At `3cd5802` it includes `Details.cshtml:68 AssignNullToNotNullAttribute`, which replaces the old `Details.cshtml:60` hit. After DEV-360, `Actor`, `Collection`, `Director`, `Genre`, `Movie`, and `Player.cs` have zero hits. They stay in the AC2 `-Files` set, and DEV-361 edits nothing in them.
- **FR-003**: Resolve both `KeyValuePair` equality hits under the existing guard. Compare keys with `StringComparison.OrdinalIgnoreCase` and values ordinally, which preserves reachable behavior. `SingleOrDefault` already throws when more than one key matches case-insensitively, so the guarded match has the same key identity, even though default `KeyValuePair` equality is case-sensitive (`task-DEV-361:19`; Patron Q2).
- **FR-004**: Fix the five diagnostic hits at the four always-true branch sites in the ignored `TestMethod1` and `TestMethod2` now: `UnitTest1.cs:69,80,109,120` at `3cd5802` (`:61,72,101,112` at `5a38215`), where line 120 has both condition IDs. Keep their intent and don't rely on DEV-280 deleting them later (`research.md`; `task-DEV-361:20`; Patron Q3).
- **FR-005**: Classify binder/serializer findings for the five-target IDs by searching for in-product assignments. Suppress only proven external-writer false positives, per type or member, with a one-line reason naming the writer. Fix real cases minimally (`task-DEV-361:16-18,27-28`; Patron Q4).
- **FR-006**: Add no package or reference, including `JetBrains.Annotations`. Route wider findings to Patron for a follow-up without scheduling chain work (`task-DEV-361:22,29`; charter §2.3).
- **FR-007**: Set `resharper_nullable_warning_suppression_is_used_highlighting` to `warning` (owner B1 answer [x]). Resolve all 112 measured hits in the 18 B1 files per FR-008 (`3cd5802`).
- **FR-008** (fix-first, `task-DEV-361` AC3): Each B1 hit **must** be fixed in code whenever a fix meets all of these conditions:
  - it preserves behavior;
  - it adds no CA1502 complexity;
  - it edits only FR-002 paths;
  - it changes no mapped entity nullability, `DbSet`, DTO property set, or public member type.

  A fix inside an `IQueryable` lambda must also be legal in an expression tree (for example, no pattern matching, CS8122). Suppression is the **fallback**, used only for a hit with no fix that meets every condition above. Typical cases: a fix would change reachable binding, deserialization, or query behavior, or a Patron ruling forbids executable changes in that member (item 5, `AssistirFilme`). That hit gets a `// ReSharper disable NullableWarningSuppressionIsUsed` … `// ReSharper restore NullableWarningSuppressionIsUsed` bracket at **type or member** scope. The bracket carries a one-line reason naming why the value is non-null: EF Core materialization or `DbSet` initialization, Newtonsoft.Json deserialization, the MVC model binder, Razor tag-helper binding or `[ViewContext]` activation, the in-product initializer at `file:line`, a deliberate test `null`, a reflection lookup of a known member, or a nullable-column read where the `!` preserves the pre-DEV-360 contract (DEV-360 FR-004). That last reason does not claim the value is non-null.

  No file-scoped `.editorconfig` section, no `[SuppressMessage]`, and no `#nullable disable`.
- **FR-009**: Fix the three pre-existing WARNING+ hits in `LamuFlix.Test/WorkerTests.cs` (`:1 RedundantNullableDirective`, `:11 RedundantUsingDirective`, `:75 MethodHasAsyncOverload`). WorkerTests.cs is now an AC2 path, and AC2 requires exit 0. Tell Patron these three leave the DEV-281 inventory (39 → 36).

- **FR-010** (applies under owner choice (A), answered 2026-09-23; Patron T019 ruling): Bring `EntityExtensions.DynamicQuery` to CA1502 ≤6 by extracting private static helpers inside `EntityExtensions.cs` (`constitution.md:338,344-345`). The move must be pure: same `Expression.*` construction order, same exceptions, unchanged public signature and callers, no new file or type. The `!` sites that move into the helpers are suppressed per FR-008. A threshold-6 failure that this extraction cannot remedy is a hard stop, not a waiver. Under owner choice (B), FR-010 moves to a prerequisite follow-up ticket, and FR-001/FR-007 wait for it to merge.

## Success Criteria

- **SC-001**: Exactly the six named inspection keys change from `suggestion` to `warning`. No other `.editorconfig` severity changes.
- **SC-002**: The warning-level InspectCode command with the 25-path FR-002 `-Files` list exits 0.
- **SC-003**: Every remaining suppression is local, at type or member scope, with a one-line reason. No new package reference appears.
- **SC-004**: All ten five-target hits are resolved and MVC-bound properties remain available. That is the five hits across four condition sites, the two equality hits, and the three view-model hits.
- **SC-005**: All 112 `NullableWarningSuppressionIsUsed` hits (measured at `3cd5802`) are handled fix-first. Every hit that can be fixed without changing reachable behavior is fixed, and only the remainder is suppressed per FR-008. The three WorkerTests WARNING+ hits are fixed. `git diff origin/main -- '*.cs'` shows no change to an EF-mapped property's nullability, a `DbSet` declaration's type, or a DTO property set. `git diff origin/main --stat` lists only `.editorconfig` and FR-002 paths under source.
- **SC-006**: `run-cyclomatic-complexity.ps1 -BaseRef origin/main -Files $files -Threshold 6` exits 0.

## Gate 1 and authorization envelope

- [x] **Owner:** blocked: structural — restore `nullable_warning_suppression_is_used` to warning (reverses B1)? **Answered [x] on merged PR #3.** The user confirmed via the Conductor on 2026-09-23: reverse B1, restore the key to `warning`, and widen past the frozen three-file scope (`task-DEV-361:21,30`; charter §2.3).
- [x] **Owner:** blocked: structural — plan change (T019). **Answered (A) on 2026-09-23.** The user's direct answer was relayed by the Conductor. Phase B proceeds without waiting for the re-spec PR to merge. The six-key restoration edits `LamuFlix.Web/Extensions/EntityExtensions.cs`. That puts the pre-existing `DynamicQuery` (CA1502 12) into the threshold-6 refactor gate, and `constitution.md:344-345` requires a helper-extraction fix. Choose one:
  - **(A)** Include T019 in DEV-361: a same-file private-helper extraction of `DynamicQuery`, done as a pure move. **Patron recommends (A).**
  - **(B)** Defer the extraction to a prerequisite follow-up ticket, and block the six-key restoration (T004 onward) until that ticket merges.
- [x] **Process:** T013 plan challenge, adjudication, and freeze (Round 1 of 2), frozen 2026-09-23. The owner scope change **supersedes** it.
- [x] **Process:** T018 Round 2 challenge, adjudication, and re-freeze (Round 2 of 2), re-frozen 2026-09-23 (`plan-challenge-adjudication.md`).
- [x] **Process (status record):** Patron set `gate1: provisional` on 2026-09-23. PR #3 merged after the owner answered B1.
- **Patron T019 ruling (2026-09-23):**
  - The Round 1 three-file "no refactor / no baseline exception, stop and report" clause (`research.md` Phase A baseline) is **superseded** for the expanded modified-file set.
  - T019 helper extraction is constitution-mandated (`constitution.md:344-345`).
  - A failure that extraction cannot remedy remains a hard stop.
  - Size stays **M**, and the L-only mutation gate does not apply.
- **Patron F1 ruling (2026-09-23):**
  - Fix-first (AC3) is in FR-008. `FilmesServices.cs:106` is named as a known fix candidate, and its form is left to T017. The reason is that `is { } x` does not compile in its EF expression tree (CS8122).
  - §2.3 item 5 does **not** fire. That is Patron's ruling, not an assumption. `FilmesServices.cs` is a flagged file of the sixth key, inside the owner-authorized scope. The edit only removes a redundant null-forgiving operator in a query predicate. It does not change `Features:LocalPlay` execution, secrets, or `Process.Start`.
  - The binding constraint is AC3: `GetPlayer` selects the same row and returns the same `Path` for every input. Otherwise the bracket applies. No checkbox and no block.
  - **Re-baseline note (`3cd5802`):** DEV-360 already rewrote `:106` as `x.Formats != null && x.Formats.Contains(format)`, which has no `!`. The hit is gone, and DEV-361 does not edit `GetPlayer`.
- **Patron item-5 ruling, `AssistirFilme` (2026-09-23, folded next to F1):** `FilmesServices.cs:74,75` (`filme.Format!`, `filme.Location!`) sit inside `AssistirFilme`, behind the `Features:LocalPlay` check (`:68`) and before `ProcessStarter` (`:76`). §2.3 item 5 does **not** fire for a comment-only member bracket there. The constraint: no executable line of `AssistirFilme` (`:66-77`) changes, so fix-first does not apply to these two hits. No checkbox and no block.
- The six `.editorconfig` keys and their bounded fixes are authorized: five by the ticket, one by the owner's B1 answer. There is no public API change, database schema change, new dependency, new project, or new layer. Changing EF entity nullability is out of bounds under §2.3 item 3. DEV-360's nullability alignment is upstream and is not undone.
- The owner's authorization lets DEV-361 edit the 25 FR-002 paths, and only those. Editing any other file is §2.3 item 6 and goes back to the user.
