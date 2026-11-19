# DEV-543 plan challenge adjudication

Reviewed the Sentry, Ledger and Compass findings in Maestri's LamuFlix notes against the DEV-543 ticket export, spec, plan, tasks, brief, Patron's Q1–Q8 rulings and proposed ADR 0047. Sentry lists 11 IDs and Ledger lists 10. Compass reports eight in its summary and lists ten IDs in the body (one High, three Medium and six Low). All 31 listed IDs are adjudicated. Shared fixes count once in the plan and retain a disposition for every source ID.

## Sentry

| ID | Severity | Disposition | Basis and closure |
| --- | --- | --- | --- |
| H1 | High | Accept | Wanted 3 specifies all required positive comparisons. Wanted 13 sets the runtime bar. T025 records pair count and warm run latency before Related work, with the expected comparison bound in the plan. T021 remains the acceptance run. |
| H2 | High | Accept | Wanted 1/8/10 and FR-007 require fresh signal inputs. T001 verifies the landed mutation/read and invalidation paths instead of assuming the challenger snapshot remains current. T016 exercises watchlist add/remove, like/dislike and playback changes with the cache primed. |
| H3 | High | Accept | Wanted 7/8, the EF source row under Where it lands, and Q3 retain per-seed playback for recency. T001 pins the actual seam. T005 joins by movie ID and exercises old, fresh and missing events through production persistence on miss and hit. |
| M1 | Medium | Accept in part | Q4 and Wanted 5 keep one shared policy. The ticket's bar open point permits reconsideration if DEV-545 evidence shows the surfaces need different values. The brief and T017 record both-rail evidence and Patron reconsideration before any separate option. No override is in DEV-543. |
| M2 | Medium | Accept | Constitution VII, Wanted 1/3/4/7 and Q3. T001 verifies binding and DEV-544 bar ownership. T003 tests finite non-negative weights, ordered rank multipliers, valid floor and startup validation. |
| M3 | Medium | Accept | Wanted 3/5 and FR-001/003/004 use DEV-518 evidence. T001 pins its missing-feature contract. T006/T008 cover valid missing inputs and exclude non-finite comparison results from ranking, evidence and admission. |
| M4 | Medium | Accept in part | Wanted 9, Done when and Q2 retain watched adjustment and disliked exclusion. Surviving scores remain stable for unrelated liked and disliked reviews. A dislike can change membership by excluding its own movie. The For you global disliked penalty does not transfer to Related. T013 tests both cases. |
| M5 | Medium | Accept | Wanted 11 and charter §2.3 item 6. T001 and T014 record method, refs and caller dispositions, with downstream refs or recon. An unresolved caller or missing evidence blocks the deletion. |
| L1 | Low | Accept | Wanted 6 and FR-005 carry plain text in the existing list. T009/T010 preserve existing rendering semantics and cover braces and markup as literal title text. |
| L2 | Low | Defer | Wanted 13 and FR-011 need performance and quality evidence. T025/T021 cover counts and runtime in the named evidence. A persistent diagnostic has no acceptance dependency and is a follow-up under Q1/Q7. |
| L3 | Low | Accept | Wanted 13 and SC-007. T002 pins DEV-545's warm timing method or the brief's fallback before baseline. T025/T021 reuse it and record individual latencies and summary statistics. |

## Ledger

| ID | Severity | Disposition | Basis and closure |
| --- | --- | --- | --- |
| L-01 | Medium | Accept in part | Wanted 11 and charter §2.3 item 6. T001/T014 inventory every dependent, including tests, Evaluation, DI and config. T015 preserves retained behavior and changes only obsolete assertions or tests with recorded disposition. No blanket preauthorization of rewrites or deletion. |
| L-02 | Medium | Accept | Wanted 7 and Constitution VII. T004 pins `RecommendationRecencyDecay.cs` and `RecommendationRecencyDecayTests.cs`, with `FakeTimeProvider`, the existing age semantics and the Q3 floor. |
| L-03 | Medium | Accept | Wanted 3/5/6 and the harness complexity gate. T007–T009 put pure scoring and selection in the existing Recommendations folder in `NearestSeedScorer.cs`. The handler supplies inputs and builds output with the existing reason builder. |
| L-04 | Medium | Accept | AGENTS Stryker guidance. T003/T009 use option defaults and constant templates without mutable static initialization. T006/T010 cover individual weights, multipliers and each template. |
| L-05 | Low | Accept | Task parallelism convention. T010 and T013 are sequential on the revised dependency chain. T013 pins the distinct Related test file. |
| L-06 | Low | Accept | AGENTS property-test gate and the deterministic scoring contract in Wanted 3/5 and Q6. T006 adds focused FsCheck coverage for shuffled inputs, ID ties and monotonic disliked penalties. T022 runs the applicable gate. |
| L-07 | Low | Accept | AGENTS testing rules and Constitution IX. Tasks pin repo naming, AAA, Shouldly, the existing mocking library and Testcontainers for persistence without driver mocks. |
| L-08 | Low | Accept | Same validation change as Sentry M2. T003 pins `RecommendationOptionsTests.cs` and the invalid/default cases. |
| L-09 | Low | Accept in part | Wanted 12 and the Conductor's recorded 0047 allocation. T017 has Keel reconcile the ADR rationale and Q1–Q8 reference in the delivery PR. Preserve the historical Patron quotation in CONCLUSIONS and record that 0047 supersedes the provisional 0045 reference in the brief. The ADR retains its proposed status pending delivery. |
| L-10 | Low | Accept | The plan's Approach carries the authoritative delivery/approval distinction. Removed redundant gate and numbering prose from active task text. T017 still verifies the same-PR decision record. |

## Compass

| ID | Severity | Disposition | Basis and closure |
| --- | --- | --- | --- |
| H1 | High | Accept | Ticket Wanted 3/5 explicitly governs the delivered order. T006 tests an output page where MMR would reorder it and T007 removes the For you MMR call in the ticket-named handler. No removal of shared selectors/options or change to Related selection or DEV-541 boundaries. |
| M1 | Medium | Accept | Wanted 10, FR-007 and US4. T016 verifies the real cache-write shape, tests embedding-dimension independence and records fields and evidence in §4. |
| M2 | Medium | Accept | Tasks file-path convention and FR-008. T013 uses `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`. Related property tests are inventory leads if cleanup affects them. |
| M3 | Medium | Accept in part | Same cleanup gate as Ledger L-01. All named leads below feed T001/T014. T015 changes only cleared and authorized dependents. |
| L1 | Low | Accept | Wanted 1 and Q5 already classify a dislike as negative only. The brief and T006 make the watchlisted+disliked case explicit. |
| L2 | Low | Accept | Wanted 4 and the brief's zero penalty when no disliked seed exists. T006 pins the test and covers the no-usable-pair case under Sentry M3. |
| L3 | Low | Accept | Wanted 5/9, Q4 and the DEV-544 pool exclusion. T001 pins the landed policy use and T011 adds no Related admission filter. |
| L4 | Low | Accept | Wanted 12 supersedes the recommendation formula in ADR 0034. Keep the document status and canonical review decision. T018 names the signed-decision supersession explicitly. |
| L5 | Low | Accept in part | Wanted 6, Wanted 11 and charter §2.3 item 6. T009 makes the seed path the sole producer on For you. T014 records `AddLikedReviewReason` separately and routes any removal through Patron. |
| L6 | Low | Accept | Wanted 6 explicitly retains the existing reason list until DEV-542. The brief and T009 pin movie ID, title, signal and evidence as construction inputs. FR-005 governs the unchanged wire shape. |

## Pickup inventory leads

These are challenger-reported leads for Wisp to verify at the landed refs. T001 and T014 own the inventory and decisions. The checklist does not itself authorize rewriting these files.

- Recommendation unit tests: `BlendScorerTests`, `BlendScorerPropertyTests`, `BlendScorerSurvivorTests`, `ProfileSimilarityCalculatorPropertyTests`, `ReviewAffinityTests`, `ReviewAffinityPropertyTests`, `ReviewRankingHandlerTests`, `Dev510MutationSurvivorTests`, `GetRecommendationsQueryHandlerPropertyTests`, `RecommendationScoringSurvivorTests`, `RecommendationReasonBuilderSurvivorTests`, `LikedReviewReasonTests`, `RecommendationProfileSourcePortTests`, `TasteProfileReviewAggregateTests`, `TasteProfileProperties`.
- Options unit tests: `RecommendationOptionsTests`, `RecommendationOptionsSurvivorTests`, `SimilarityWeightsTests`, `SimilarityWeightsSurvivorTests`.
- Evaluation: `EvaluationFixtureLoader`, `EvaluationReplay`, `ReplayFixtureFactory`, `QualityReportRunnerTests`, and `baseline*.json` fixtures. Include DI registrations and appsettings keys in the inventory.

**PLAN FROZEN**. The freeze establishes scope, execution order and proof obligations. Gate 1 and all implementation, runtime and delivery evidence remain pending. Caller-gated deletions retain their pickup proof requirements.
