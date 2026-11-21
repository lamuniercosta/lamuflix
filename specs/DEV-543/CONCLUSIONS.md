# DEV-543 grill conclusions

## Q1 — Scope boundary

Keel asked: “Can we freeze the scope to nearest-seed For-you ranking, strong-match eligibility and best-seed reasons, plus removal of Related taste-profile scoring and reasons, with the DEV-518 similarity, DEV-544 candidate pool, DEV-541 caps/paging/exclusions and card/API shape outside this ticket? I recommend that boundary because it follows the ticket scope summarized in recon-DEV-543 lines 9–13 and permits isolated ranking and contract tests. Please cite the ticket and charter basis and send the ruling to Keel.”

Patron replied in the connected seat message: “Confirmed scope boundary per ticket Wanted items 1-12, Out of scope, and specs/PRODUCT.md:23-24,38.” The ruling confirms that boundary for this grill question. The separate file referenced in the reply is outside the authorized evidence roots.

Basis: DEV-543 ticket Wanted items 1–12 and Out of scope, as cited by Patron. See also recon-DEV-543 lines 9–13 and specs/PRODUCT.md lines 23–24 and 38.

---

## Q2 — Related scoring and profile cleanup

**Keel**

Shall we settle Related without the taste profile on the ticket default? I recommend DEV-518 similarity with the existing AlreadyWatchedPenalty and disliked exclusion, and removing profile reasons. An unrelated review must not change a result score. Remove the ticket-named legacy profile members after caller checks, keeping the watched penalty. Basis: recon-DEV-543-2 lines 36–49, 57, 107.

**Patron**

Confirmed — settle Related rail on the ticket default: pure DEV-518 similarity with existing AlreadyWatchedPenalty and disliked exclusion, remove profile reasons so unrelated reviews do not change Related scores, and delete the ticket-named legacy profile members (keeping AlreadyWatchedPenalty) after caller checks, recording this in ADR 0045. Basis: DEV-543 Wanted items 9, 11, 12, Done-when item 3, Open points (recon-DEV-543-2 lines 36-49, 57, 107), and specs/PRODUCT.md:23-24.


---

## Q3 — Playback recency

**Keel**

How should recency apply to signed seed contributions? I recommend a configurable 0.25 floor on the existing decay multiplier for positive seeds with playback, unscaled positives without playback, and the unscaled maximum disliked similarity required by Wanted item 4. A liked seed at the floor still carries a quarter of its weight. This preserves the exact dislike formula and the raw strong-match bar. Basis: recon-DEV-543-2 lines 28–34 and 105–111.

**Patron**

Confirmed — apply recency scaling to positive seeds with playback events using a configurable 0.25 lower bound (floor) on exp(-RecencyDecayLambda * age), leave positive seeds without playback events (including watchlist seeds) unscaled, keep disliked negative seeds unscaled per Wanted item 4 (disliked weight * max DEV-518 similarity to any disliked seed), and evaluate strong-match eligibility on raw pair similarity and breakdown before seed weight or recency scaling. Basis: DEV-543 Wanted items 4, 5, 7, Open points (recon-DEV-543-2 lines 28-34, 105-111), and specs/PRODUCT.md:23-24,28.


---

## Q4 — Shared strong-match policy

**Keel**

Shall the spec keep the single DEV-544 strong-match policy for both surfaces, with the ticket default bar 0.225 and Collection, Director or Themes at value 1 as the strong-reason list? I recommend that default and require DEV-545 evidence for any proposed tuning, so an eligibility change can be measured on both surfaces. Basis: recon-DEV-543-2 lines 32 and 102–108.

**Patron**

Confirmed — reuse the single shared DEV-544 strong-match option set for both surfaces, with default bar 0.225 and strong-reason list Collection, Director, or Themes at value 1, requiring DEV-545 quality report evidence for any tuning. Basis: DEV-543 Wanted item 5, Open points (recon-DEV-543-2 lines 32, 102-108), and specs/PRODUCT.md:23-24.


---

## Q5 — Seed precedence and empty results

**Keel**

Shall we define the empty-result boundary as no qualifying positive seed after self-exclusion? I recommend applying ordinary ranking to real watchlist seeds in a watchlist-only library, returning empty when there are no positive seeds or no strong matches, and adding no fallback list. Resolve strongest seed kind once before recency, keeping a watched-and-liked movie as liked and a disliked movie negative only. This preserves the ticket examples and prevents self-matching or padding. Basis: recon-DEV-543-2 lines 28–34 and 104–110.

**Patron**

Confirmed — resolve each movie to its single strongest seed signal before recency scaling (disliked is negative only; liked > watched-without-review > watchlist, so watched-and-liked counts as liked only), apply ordinary nearest-seed ranking when real watchlist positive seeds exist (excluding the candidate itself from its positive seeds), and return an empty list with no fallback or padding when no positive seeds exist or no candidate has a strong-match positive seed after self-exclusion. Basis: DEV-543 Wanted items 1, 2, 3, 5, Open points (recon-DEV-543-2 lines 28-34, 104-110), and specs/PRODUCT.md:23-24.


---

## Q6 — Card seed ranking and deterministic ties

**Keel**

Shall the card seed use the same recency-adjusted weighted similarity as the positive ranking contribution, restricted to strong positive pairs? I recommend that interpretation, with movie ID ascending as a deterministic final tie-breaker for equally scored strong seeds and equally ranked candidates where the existing candidate ordering has no further key. This makes reasons reproducible without changing qualification or outranking stronger candidates. Basis: recon-DEV-543-2 lines 30–34 and PRODUCT.md sections 3–4.

**Patron**

Confirmed — select the card best seed from strong-match positive seeds using the same recency-adjusted weighted similarity (effective seed weight * DEV-518 similarity) used in the positive ranking contribution, and use movie ID ascending as the deterministic final tie-breaker for equally scored strong seeds and equally ranked candidates where existing candidate ordering has no further key (log deterministic tie-breaker tagged [assumed] in specs/DEV-543/ASSUMPTIONS.md). Basis: DEV-543 Wanted items 3, 5, 6, 7 (recon-DEV-543-2 lines 30-34) and specs/PRODUCT.md:23-24,28-29.


---

## Q7 — Review closing bar

**Keel**

What closes the review loop? I recommend requiring all accepted in-scope findings at every severity, including Low, to be fixed and verified, explicit dispositions on the three Size L review axes, and passing applicable repo gates plus the ticket acceptance evidence. That prevents an accepted contract, runtime or lower-severity defect from being carried into the handoff. Basis: DEV-543 Done when and Wanted item 13, PRODUCT.md sections 1 and 5, and charter Size L review discipline.

**Patron**

Confirmed — the review loop closes only when all accepted in-scope findings at every severity (including Low) are fixed and verified, explicit dispositions are recorded on all three Size L review axes, and all applicable repo gates plus the ticket Done-when acceptance and Wanted item 13 speed/quality evidence pass. Basis: DEV-543 Wanted item 13 and Done-when criteria, specs/PRODUCT.md:12,34-39, and standing charter Size L review bar.


---

## Q8 — Review round cap and grill closure

**Keel**

Shall the fix and review loop cap be two rounds, with any remaining blocking findings reported against the closing bar and the phase held open? I recommend two to allow remediation and a verification round while keeping the scope bounded. This is separate from the grill cap of 12. Please give the complete ruling inline and confirm whether this closes the grill on Q1–Q8.

**Patron**

Confirmed — cap the fix and review loop at two rounds (remediation plus verification, separate from the 12-question grill cap), reporting any remaining blocking findings against the Q7 closing bar and holding the phase open; this closes the DEV-543 grill on Q1-Q8. Basis: standing charter Size L review-loop cap and specs/PRODUCT.md:12,34-39.

---

## Gate 1 — Provisional ruling

**Verdict**: `gate1: provisional` — zero structural owner checkboxes (`blocked: structural`).

- `/speckit-analyze` is clean and all 31 three-axis plan challenge findings (Sentry, Ledger, Compass) are adjudicated and PLAN FROZEN in `specs/DEV-543/PLAN_ADJUDICATION.md`, `specs/DEV-543/plan.md:9`, and `specs/DEV-543/tasks.md:9`.
- `specs/DEV-543/checklists/requirements.md:32-36` has all "User-Visible Outcome" items ticked (`SMOKE-001` and `SMOKE-002` in `specs/DEV-543/spec.md:18-32`).
- Zero ticket contradictions or constitution departures (`specs/DEV-543/spec.md:8-10`, `specs/DEV-543/plan.md:37-59`, `specs/PRODUCT.md` §5); ordinary owner Gate 1 merge remains with the user on the spec PR (`T000`).

---

## Gate 1 — Verified spec PR merge

- [Spec PR #450](https://github.com/lamuniercosta/lamuflix/pull/450) merged into `main` on 2026-11-19 at 17:17:09 UTC. GitHub reports merge commit `18e012145a9a8b2f1a3c00b1437611870b5a2433`.
- Phase A base `1ac83297c1ba8ffc936987e8b198ce8dc612b01a` is an ancestor of the spec merge commit. The merge commit is an ancestor of the `feature/DEV-543` pickup HEAD `34c10f3e`.
- T000 complete. T001 is the next checkpoint.

---

## Phase 1 T001 — Upstream dependency checkpoint ruling

**Verdict**: STOP Phase 1 T001 and hold DEV-543 Build (`awaiting-merge` on DEV-518 PR #455 and DEV-544).

- `specs/DEV-543/tasks.md:57-59` requires T001 to confirm DEV-518, DEV-544, and DEV-510 are merged into `main`, pin the landed DEV-544 library feature load and shared strong-match options contract, complete the Wanted 11 caller inventory against landed consumers, and STOP if prerequisites are unmerged.
- `specs/DEV-543/EVIDENCE.md:11-18` and note `DEV-543:41-44` verify that DEV-518 (`feature/DEV-518` at `b00b9af5`, PR #455 open) and DEV-544 (implementation not started/merged) are not on `origin/main` (`34c10f3e`), so the DEV-544 pool/strong-match symbols and `docs/adr/0047-for-you-nearest-seeds.md` draft are absent at pickup HEAD (`2080d59f`).
- Resume DEV-543 Phase 1 T001 on a rebased `feature/DEV-543` worktree only after both DEV-518 (#455) and DEV-544 merge into `main` (and Keel lands the Proposed ADR 0047 draft for T001/T017–T019 per `specs/DEV-543/tasks.md:50,59,131`).


---

## Phase 1 T001 — Pickup reanalysis and Proposed ADR 0047 (2026-11-21)

**Verdict** — Keel reanalysis and ADR draft complete. **T001 remains blocked on missing pickup evidence.** The upstream merge stop recorded above is superseded. Gate 1 remains verified.

- Reviewed `brief.md`, `spec.md`, `plan.md`, and `tasks.md` at `345d3974657d577d60d5caaeac477197dd6d9c2f` against their landed copies at main `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089` and Wisp's fresh pickup update in `recon-DEV-543:49–75`. The main comparison leaves brief, spec and plan identical to their landed copies. The tasks diff carries the Phase 0 receipt and completion state. The SpecKit prerequisite script **Could not run** on the nonnumeric branch name, so this was a bounded manual read-only analysis followed by separate receipt and ADR edits.
- Functional coverage is 12/12 and success-criteria coverage is 10/10 across 26 tasks including T000 and T025. FR-001–004 map to T006–T010, FR-005 to T009–T010, FR-006–007 to T003–T005/T016, FR-008 to T011–T013, FR-009 to T014–T015, FR-010 to T017–T019, FR-011 to T002/T020–T022/T024–T025, and FR-012 to T023. SC-001/002 map to T020 and reason checks T010/T012–T013, SC-003 to T013, SC-004/005 to T006/T008–T010, SC-006 to T005/T016, SC-007 to T002/T021, SC-008 to T022, SC-009 to T017–T019 and SC-010 to T024.
- Q1–Q8 align with the planned signed-score order, raw shared admission, same-pair reasons, Related boundary, guarded legacy cleanup, and two-round Size L closing bar. Dependency order remains sound. T002 precedes T003/T004/T005, T005 precedes T006, T025 precedes the Related switch, T014 guards cleanup, and T018/T019 precede T017. T021/T022/T024 and Q7 gate T023. The planned API, schema, dependency and layer boundaries remain consistent with the constitution. No scope, decision or ordering amendment was authorized or needed. The runtime prerequisites below qualify the plan's Principle VII startup-validation assumption.
- Wisp confirmed DEV-518 `2f1647b2`, DEV-544 `7d31c4b1` and DEV-510 `5fa68c83` are ancestors of pickup main. The DEV-544 feature-load and strong-reason surfaces are identified. The [pickup evidence](EVIDENCE.md#0-prerequisites-and-identity) records provenance and the remaining contract pins.
- **needs recon:** What exact live path binds `RecommendationOptions` and validates it at startup, or which registration file would have to change? Wisp reported a gap at recon line 74. If the required wiring is outside the authorized Files table, T003 requires a cited Patron ruling before any edit.
- **needs recon:** Where are the complete Wanted 11 ledger and downstream evidence (DEV-538/539/540/541/542), the exact DEV-518 missing-feature and DEV-544 Related policy/selection pins, and the DEV-510 playback payload, timestamp join and mutation freshness paths required by T001? Wisp reports a partial ledger at recon lines 70–73. Gauge's pickup harness and pre-edit gate receipt is also pending.
- [ADR 0047](../../docs/adr/0047-for-you-nearest-seeds.md) is **Proposed** and belongs to the delivery PR with T017–T019. T001 stays unchecked and T002 stays blocked while these pickup items are open.

---

## Phase 1 T001 — RecommendationOptions startup binding and validation ruling (2026-11-21)

**Verdict**: `src/LamuFlix.Api/Program.cs` is **NOT authorized** for edit in DEV-543; `RecommendationOptions` validation in DEV-543 is confined to `IValidatableObject` / data-annotations on `src/LamuFlix.Core/Options/RecommendationOptions.cs` (and unit tests in `tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs`), because startup binding and `ValidateOnStart()` are already wired in `ServiceDefaults`.

- **Existing live wiring (`ServiceDefaults`)**: `src/LamuFlix.Api/Program.cs:26` calls `builder.AddServiceDefaults()`, which calls `builder.Services.AddLamuFlixOptions()` (`src/LamuFlix.ServiceDefaults/Extensions.cs:31`). `ServiceDefaultsExtensions.AddLamuFlixOptions` already registers `BindAndValidate<RecommendationOptions>(services, RecommendationOptions.SectionName)` at `src/LamuFlix.ServiceDefaults/Extensions.cs:93`, which executes `services.AddOptions<RecommendationOptions>().BindConfiguration(sectionName).ValidateDataAnnotations().ValidateOnStart()` (`src/LamuFlix.ServiceDefaults/Extensions.cs:102-109`; also documented at `src/LamuFlix.Core/Options/RecommendationOptions.cs:9`).
- **Constitution Principle VI & VII**: Principle VI (`constitution.md:637-638`) mandates that "`ServiceDefaults` owns Serilog, OpenTelemetry (OTLP), health checks, options validation, and `TimeProvider` registration; hosts MUST NOT duplicate that wiring." Principle VII (`constitution.md:648-651`) is already satisfied by `ServiceDefaultsExtensions.AddLamuFlixOptions`, and `Validator.TryValidateObject(..., validateAllProperties: true)` invoked by `ValidateDataAnnotations()` automatically executes `IValidatableObject.Validate` on `RecommendationOptions` at startup.
- **Authorized file boundary (`plan.md` / `tasks.md` / `PRODUCT.md` §5 item 6)**: Neither `src/LamuFlix.Api/Program.cs` nor `src/LamuFlix.ServiceDefaults/Extensions.cs` needs to be edited for T003. Keep all DEV-543 option additions and validation rules inside `src/LamuFlix.Core/Options/RecommendationOptions.cs` and `tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs` (`specs/DEV-543/plan.md:51,118,134`; `specs/DEV-543/tasks.md:70`).

---

## Phase 2 T005 — Mechanical call-site adaptations in `HiddenSeedEvaluationCopy.cs` and `ReviewRankingHandlerTests.cs` (2026-11-21)

**Verdict**: **Authorized** — the mechanical `GetForYouRecommendationsQueryHandler` constructor and `IRecommendationCandidateSource` call-site adaptations in `tests/LamuFlix.Evaluation/RealLibrary/HiddenSeedEvaluationCopy.cs` and `tests/LamuFlix.UnitTests/Features/Recommendations/ReviewRankingHandlerTests.cs` (commit `b0b2d722`) are forced edits in scope for DEV-543.

- **Forced by authorized handler signature change (FR-007 / SC-006 / T005)**: DEV-543 Wanted item 8, `specs/DEV-543/spec.md:140,182,206` (FR-007, SC-006), and `specs/DEV-543/tasks.md:72` (T005) mandate replacing `IMovieCatalog` paged-walk and per-movie `IMovieRepository.GetAsync` in `GetForYouRecommendationsQueryHandler` with a single DEV-544 `IRecommendationCandidateSource.GetLibraryFeaturesAsync` load, which directly changes the handler constructor consumed by both files.
- **Forced by acceptance criteria and build/test gates (§2.3 item 6 / `specs/PRODUCT.md` §1 & §5 item 6)**: Under charter §2.3 item 6, edits to unnamed tracked files forced by acceptance criteria or a gate are in scope. `HiddenSeedEvaluationCopy.cs` powers the DEV-545 evaluation harness required by Wanted item 13 and `specs/DEV-543/spec.md:158-159,186,201,207` (FR-011, SC-001, SC-007; `tasks.md:69,93,143-144`), and `ReviewRankingHandlerTests.cs` must compile and pass under the mandatory `dotnet test` gate (`specs/PRODUCT.md:12`; `specs/DEV-543/spec.md:186,208` SC-008; `tasks.md:11,119-120`).
- **Scope boundary**: Authorization in Phase 2 is strictly limited to mechanical constructor/input adaptation (wiring `SnapshotCandidateSource` in `HiddenSeedEvaluationCopy.cs` and the `IRecommendationCandidateSource` stub + `PlaybackByMovieId` input in `ReviewRankingHandlerTests.cs`); any Phase 5 legacy profile cleanup in those files remains governed by T014–T015 (`specs/DEV-543/tasks.md:119-120`).

---

## Phase 3 T006–T010 / T025 — Test adaptations in `ApiEndToEndRecommendationTests.cs` (2026-11-21)

**Verdict**: **Authorized** — the For you test adaptations in `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` (`GetForYou_PositiveHistory_ReturnsPersonalReasonsAndRetainsVectorlessCandidates` and `GetForYou_MarkWatched_RemovesMovieFromResults`, commit `838d6cbc`) are forced edits in scope for DEV-543 under charter §2.3 item 6.

- **Forced by nearest-seed strong-match admission and best-seed reason requirements (FR-003, FR-004, FR-005 / SC-002 / T008–T010)**: DEV-543 Wanted items 3–6, `specs/DEV-543/spec.md:178-180,202` (FR-003, FR-004, FR-005, SC-002), and `specs/DEV-543/tasks.md:89-91` (T008–T010) mandate strong-match admission on For you (`NeutralStrongMatchBar()` in minimal synthetic fixtures without shared strong reasons), require emitting `Because you watched {title}` (a `personal` reason kind) first for watched seeds, and explicitly prohibit emitting the superseded generic reason template `Matches your {genre} interest`.
- **Forced by acceptance criteria and integration test gate (§2.3 item 6 / `specs/PRODUCT.md` §1 & §5 item 6)**: Under charter §2.3 item 6, editing a tracked test file not listed in the `plan.md` Files table is in scope when forced by acceptance criteria or a gate. `ApiEndToEndRecommendationTests.cs` exercises the live `/api/v1/recommendations/for-you` endpoint and must pass under the mandatory `dotnet test` integration suite required by `specs/PRODUCT.md:12`, `specs/DEV-543/spec.md:186,208` (FR-011, SC-008), and `specs/DEV-543/tasks.md:11,145` (T022).
- **Scope boundary**: Authorization is confined to updating superseded For you strong-match bar settings and reason assertions in `GetForYou_PositiveHistory_ReturnsPersonalReasonsAndRetainsVectorlessCandidates` and `GetForYou_MarkWatched_RemovesMovieFromResults` while preserving the tests' retained endpoint contract, vectorless-candidate retention, and mark-watched cache-invalidation checks (`specs/DEV-543/tasks.md:120`).

---

## Phase 4 T011–T013 — Substantive Related test adaptations in `ReviewRankingHandlerTests.cs` (2026-11-21)

**Verdict**: **Authorized** — the three Related test adaptations in `tests/LamuFlix.UnitTests/Features/Recommendations/ReviewRankingHandlerTests.cs` (`Related_LikedProfile_LeavesSimilarCandidateScoreUnchanged`, `Related_DislikedProfile_LeavesSimilarCandidateScoreUnchanged`, and `Related_LikedProfile_EmitsNoLikedChip`, commit `1db03585`) are forced edits in scope for DEV-543 under charter §2.3 item 6.

- **Forced by Related profile-removal requirements (FR-008 / SC-002 / SC-003 / Q2 / T011–T013)**: DEV-543 Wanted items 9–10, Done-when item 3, `specs/DEV-543/spec.md:114-127,183,202-203` (User Story 3, FR-008, SC-002, SC-003), `specs/DEV-543/CONCLUSIONS.md:13-22` (Q2), and `specs/DEV-543/tasks.md:105-107` (T011–T013) mandate removing taste-profile score contributions and profile reasons (`AddProfileReasons` / `AddLikedReviewReason` on Related) from `GetRecommendationsQueryHandler` so unrelated liked or disliked reviews leave Related scores unchanged and emit no profile or liked-review chips on Related.
- **Forced by acceptance criteria and unit test gate (§2.3 item 6 / `specs/PRODUCT.md` §1 & §5 item 6)**: Under charter §2.3 item 6, rewriting a tracked test file not named in the `plan.md` Files table is in scope when forced by acceptance criteria or a gate. `ReviewRankingHandlerTests.cs` directly asserted the superseded ADR 0034 Related profile-blend score changes and liked-review reason chip, so adapting those three tests to the new no-profile Related contract is required to satisfy the mandatory `dotnet test` unit gate (`specs/PRODUCT.md:12`; `specs/DEV-543/spec.md:186,208` FR-011, SC-008; `specs/DEV-543/tasks.md:11,120,145`).
- **Scope boundary**: Authorization is confined to rewriting the three superseded Related profile-blend and liked-chip assertions in `ReviewRankingHandlerTests.cs` to assert unchanged Related scores and absence of `Because you liked` / `Matches your` profile reasons while preserving the file's disliked-candidate exclusion and remaining retained coverage (`specs/DEV-543/tasks.md:120`).

---

## Phase 5 T014–T015 — `AddLikedReviewReason` deletion, `IRecommendationProfileSource` shrink, and outside-boundary test deletions/rewrites (2026-11-21)

**Verdict**: **Authorized** — delete `RecommendationReasonBuilder.AddLikedReviewReason`, shrink `IRecommendationProfileSource` / `RecommendationProfileInputs` (`likedSourceIds` / `LikedSourceEmbeddings`), delete the 11 obsolete-only test files, and rewrite the 22 retained-coverage test/Evaluation files inventoried in `specs/DEV-543/EVIDENCE.md` §1 (`tasks.md` T014–T015).

- **`AddLikedReviewReason` deletion and `IRecommendationProfileSource` shrink (FR-005, FR-008, FR-009 / Q2 / T009, T012, T014–T015)**: DEV-543 Wanted items 6, 9–11, `specs/DEV-543/brief.md:13,43,56-57`, `specs/DEV-543/spec.md:180,183-184` (FR-005, FR-008, FR-009), and `specs/DEV-543/tasks.md:90,106,119-120` establish the For you seed path (`BuildSeedReasons`) as the sole producer of `Because you liked {title}`, remove Related profile/liked reasons, and mandate removing the Wanted 11 review-affinity members (`LikedReviewWeight`, `BlendProfileWeight`, `ReviewAffinityEvidence`, `TasteProfile.LikedSources`) and shrinking `IRecommendationProfileSource` / EF and cached sources once `src` callers clear (`EVIDENCE.md:47,52`; charter §2.3 dead-code deletion rule).
- **Outside-boundary obsolete test deletions and retained-coverage rewrites forced by AC and gates (§2.3 item 6 / `specs/PRODUCT.md` §1 & §5 item 6 / SC-008 / T014–T015, T022)**: Under charter §2.3 item 6, deleting or rewriting tracked test and Evaluation files not named in the `plan.md` Files table is a forced edit in scope when required by acceptance criteria (`FR-009` Wanted 11 member removal) and mandatory build/test/mutation gates (`specs/PRODUCT.md:12`; `specs/DEV-543/spec.md:184,186,208` FR-009, FR-011, SC-008; `specs/DEV-543/tasks.md:11,119-120,145`).
- **Scope boundary (`tasks.md:120` / `EVIDENCE.md:30-59`)**: Delete only the 11 obsolete-only test files (`BlendScorerTests.cs`, `BlendScorerPropertyTests.cs`, `BlendScorerSurvivorTests.cs`, `ProfileSimilarityCalculatorPropertyTests.cs`, `ReviewAffinityTests.cs`, `ReviewAffinityPropertyTests.cs`, `LikedReviewReasonTests.cs`, `RecommendationScoringSurvivorTests.cs`, `Domain/ReviewProfileChannelsSurvivorTests.cs`, `Domain/TasteProfileTests.cs`, `TasteProfileReviewAggregateTests.cs`) and update the T015-named and 22 inventoried dependent unit, integration, and Evaluation files in `EVIDENCE.md:59` solely to remove references to cleared Wanted 11 members while preserving `AlreadyWatchedPenalty`, DEV-518-owned year/runtime/rating options, and all still-live scoring, cache-budget, and contract assertions.

---

## Phase 5 T014–T016 — Evaluation test adaptation in `LeaveOneOutRunnerTests.cs` (2026-11-21)

**Verdict**: **Authorized** — adapting `tests/LamuFlix.Evaluation/RealLibrary/LeaveOneOutRunnerTests.cs` (`RunAsync_ranks_every_seed_and_reports_full_ranking_metrics`) to the DEV-543 recommendation ranking contract is a forced edit in scope under charter §2.3 item 6.

- **Forced by DEV-543 ranking and strong-match eligibility requirements (FR-001, FR-003, FR-008 / SC-005 / Q2, Q4, Q5 / T008–T015)**: DEV-543 Wanted items 1–5 and 9–11, `specs/DEV-543/spec.md:79-93,164,176,178,183,205` (FR-001, FR-003, FR-008, SC-005), and `specs/DEV-543/CONCLUSIONS.md:13-22,39-61` (Q2, Q4, Q5) replace full-library centroid ranking with strong-match nearest-seed admission (default bar `0.225`, no weak-pair padding) and pure DEV-518 similarity without profile blending, so in `RunAsync_ranks_every_seed_and_reports_full_ranking_metrics` (`LeaveOneOutRunnerTests.cs:17-43`) only the two Drama/vector-aligned candidates (`MovieId(1)` and `MovieId(2)`) qualify against seed `MovieId(4)` while non-matching `MovieId(3)` (Comedy, opposite vector `-1.0`) is excluded.
- **Forced by acceptance criteria and mandatory test/evaluation gates (§2.3 item 6 / `specs/PRODUCT.md` §1 & §5 item 6 / FR-011 / SC-007, SC-008)**: Under charter §2.3 item 6, updating a tracked Evaluation test file not listed in the `plan.md` Files table is in scope when forced by acceptance criteria or a gate. `LeaveOneOutRunnerTests.cs` validates the DEV-545 leave-one-out harness (`LeaveOneOutRunner` -> `HiddenSeedEvaluationCopy` -> `GetForYouRecommendationsQueryHandler`) and must pass under `dotnet test` (`specs/PRODUCT.md:12`; `specs/DEV-543/spec.md:186,207-208` FR-011, SC-007, SC-008; `specs/DEV-543/tasks.md:11,120,144-145`).
- **Scope boundary**: Authorization is confined to updating `RunAsync_ranks_every_seed_and_reports_full_ranking_metrics` in `tests/LamuFlix.Evaluation/RealLibrary/LeaveOneOutRunnerTests.cs` (or its fixture/expected `attempt.Ranking` of `[new MovieId(1), new MovieId(2)]`) to match the landed DEV-543 ranking contract while preserving the test's leave-one-out seed-recovery, `attempt.Take`, and `HitRateAt1`/`HitRateAt3`/`HitRateAt10`/`Mrr` metric assertions.


