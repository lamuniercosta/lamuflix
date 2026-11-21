# DEV-538 plan challenge adjudication

**PLAN FROZEN** — Phase 2 step 6, 2026-11-21, `feature/538-spec`. The plan challenge used the Sentry, Ledger, and Compass reports for the named worktree, whose Git base at adjudication was `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`. The planning documents were worktree artifacts.

**Disposition** — 19 findings (0 High, 9 Medium, 10 Low). Accepted 17 with documented repairs. Recorded 2 without further plan changes. Overlapping reports retain separate dispositions.

The freeze is conditional on the open owner checkbox amending Done when line 54 for `startedCollectionId` and `hasSeeds`. Patron provisional clearance and the Spec Kit PR are the next pipeline stage. Gate 1 requires the owner decision and user merge. T001 must establish the landed contracts and read paths before implementation. This receipt closes the plan challenge only.

## Sentry — Risk (9 findings)

| ID | Severity | Disposition | Ruling and closure evidence |
|---|---|---|---|
| Sentry M1 | Medium | Accepted | Q1/Q2/Q7 and FR-001/FR-002 require library facts the current handler shape does not establish. Brief and plan pickup plus T001 require sources for all three seed signals and watched collection membership. Record a sufficient existing mechanism for T004 or stop for bounded recon and any required Patron ruling. Principle I is a design check. |
| Sentry M2 | Medium | Accepted | FR-001 and Q2 define a distinct predicate. T002 requires watchlist-only true and playback-history-only false fixtures. T004 computes seed presence separately from the ranking predicate and preserves ranking behavior. |
| Sentry M3 | Medium | Accepted | Q4 and FR-004/FR-005 require stable rows and cross-row deduplication. T013 uses a shared single-flight guard and suppresses obsolete responses after reset. T012 exercises mixed or repeated actions while pending and a stale completion after reset. T001 verifies the landed DEV-517 lifecycle. |
| Sentry M4 | Medium | Accepted | Wanted 1a, Q1, and FR-002 describe watched membership in the library. The brief, plan, and ADR specify that dislike, candidate eligibility, and exclusions do not remove that qualifier. T002 requires excluded and disliked watched-member fixtures. The existing FR-002 wording remains the basis for that interpretation. |
| Sentry L1 | Low | Accepted | Q4 and FR-006 apply rule 6 to every successful zero-item initial response. Q5 still handles a nonempty initial page whose More for you bucket is empty. The brief and ADR distinguish those cases, T010 tests `items: [], hasMore: true`, and T012 covers the empty bucket with initial items. |
| Sentry L2 | Low | Accepted | Q3 and FR-003 require one row per movie. The brief and ADR retain the first API occurrence and T008 verifies duplicate IDs in the initial response. |
| Sentry L3 | Low | Accepted | Q3 governs the client comparator. The brief and ADR define null or omitted `releaseYear` as unknown, sort unknowns last, and use API rank for all ties. T008 checks both-unknown and equal-known years where API rank differs from movie ID order. |
| Sentry L4 | Low | Recorded | Sentry rates the two fields low risk on the existing local API trust boundary. Existing package scan and screenshot hygiene apply. The field exposure is covered by the open owner checkbox. |
| Sentry L5 | Low | Accepted | Q7 calls for existing reads. T001 identifies the mechanism, T004 reuses the browse pass where possible with no added per-movie round trips, and T005a records a read-path review. The plan adds no separate latency gate. |

## Ledger — Standards (6 findings)

| ID | Severity | Disposition | Ruling and closure evidence |
|---|---|---|---|
| Ledger M1 | Medium | Accepted | Q7's Core-to-DTO flow requires `ForYouRecommendationsPage.cs` and `RecommendedMovie.cs`. The plan names both and narrowly covers existing affected constructors and test builders, including Related. T001 pins their paths and T004 limits edits to result compatibility. |
| Ledger M2 | Medium | Accepted | T002 names the actual Core page unit test. T003 names the existing endpoint integration test for serialized HTTP coverage. Plan and T007 name `forYouApi.test.ts`, `mocks/forYou.test.ts`, and `test/handlers.ts`. Evidence is Ledger M2's file inventory. |
| Ledger M3 | Medium | Accepted | T002/T003 can be authored red against the pending members. Both compile and pass by T005. Gauge owns T005a after T005 and before T006, preserving the constitution's C# gate and the pipeline ownership split. |
| Ledger L1 | Low | Accepted | T007 lists `mocks/handlers.ts` among generated outputs and identifies `mocks/fixtures.ts` and `test/handlers.ts` as hand-maintained. The plan makes the same distinction. |
| Ledger L2 | Low | Accepted | T005 appends `int? StartedCollectionId = null` with property-targeted null omission and preserves the existing scoped ReSharper suppression. The ADR records the default, and T003 verifies Related's serialized omission. |
| Ledger L3 | Low | Accepted | Grouping determinism uses existing Vitest/RTL examples in the plan and T014, within the brief's existing dependency envelope. The existing .NET property-test gate and its opt-out evidence rule remain in T014. |

## Compass — Spec (4 findings)

| ID | Severity | Disposition | Ruling and closure evidence |
|---|---|---|---|
| Compass M1 | Medium | Accepted | Q1/Q7 and FR-002 require the watched collection qualifier. Brief, plan, and T001 require a cited existing source and intended T004 mechanism before Build, with a stop if the evidence fails. Same repair as Sentry M1. |
| Compass M2 | Medium | Accepted | Q3 and FR-003 require stable seed grouping and heading labels. T001 verifies the landed DEV-542 best-seed movie ID, signal, and title or label source, and stops with `needs recon:` if missing. The ADR records the dependency. The owner checkbox still covers only its two fields. |
| Compass L1 | Low | Accepted | FR-001 applies to all successful responses. T002/T004 name empty eligibility after exclusions, the ranking signal guard, and offset past the end. The plan and ADR name the same paths. |
| Compass L2 | Low | Recorded | Compass reports only DEV-541 implemented at the reviewed baseline. This is a planning-time observation. T001 must record any unlanded prerequisite against pickup HEAD and stop for bounded recon if it cannot establish the contract. |

