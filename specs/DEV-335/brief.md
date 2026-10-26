# DEV-335 — Add IMDb rating range filter

Status: Phase 2 grill CLOSED at Q8/12; brief ready for Quill. Scope decisions are frozen; plan freeze, Gate 1 and implementation clearance have not occurred.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-335-spec`
Branch: `feature/335-spec`
Recon identity: `52e443666b5e767edc93befc20eb55de2e77787b`
Size: M; tag: ui:
Evidence: shared notes `DEV-335` and `recon-DEV-335`. No independent source reconnaissance by Keel. Complete verbatim questions and Patron answers: [CONCLUSIONS.md](CONCLUSIONS.md). Presentation rulings: [ASSUMPTIONS.md](ASSUMPTIONS.md).

## Authority and delivery intent

The task note lines 23-25 names MovieQuery RatingRange 0-10, EfMovieCatalog ImdbRating, Filter Panel minRating/maxRating, and acceptance proof for minRating=7, unrated exclusion when a range is set, and slider updates to grid and URL. Dependencies are DEV-322, DEV-297 and DEV-302 (task note:24; recon:17,20,123). Dependency specs being present is not current-main landing or Phase B drift clearance.

Patron decided all open questions under charter §2.3. No ticket change, constitution departure, structural owner checkbox, new dependency, schema migration, project/layer, ADR or glossary rewrite is required (Q8). Vocabulary in this brief is ready to feed Quill specification drafting. Existing glossary/ADR contracts must remain respected; Keel has not inspected or changed them in this ask.

## Loop discipline — Q8

- Closing bar for code-review/ship-review: resolve every confirmed Critical, High and Medium finding; record Low as non-blocking.
- All three M axes are mandatory: Sentry, Ledger and Compass. A missing axis is blocking.
- Maximum two formal review/fix rounds and two plan challenge/fix rounds. At the cap, unresolved blockers stop for adjudication; no silent third round.
- Frozen scope: only Q1-Q7 IMDb rating-range model, query/validation/predicate, paired canonical API/URL keys, generated contract chain, live FilterPanel/LibraryView behavior and the enumerated regression proof. No dependency/schema/sort/parallel-codec/architecture cleanup or unrelated rewrite; anything else is a follow-up issue, not a finding in this round.

## Settled grill answers

### Q1 — Model, bounds and key names (ACCEPT)

Add `RatingRange(decimal? Min, decimal? Max)` and `MovieQuery.Rating`. Bounds are inclusive 0.0 through 10.0 with at most one decimal place, aligned with ImdbRating validity and existing numeric(3,1) storage. Use canonical `minRating` and `maxRating`, preserving the ticket spelling rather than inventing ratingMin/ratingMax. Include Rating in custom equality, hash and HasSameCoreValues.

Basis: task note:23-25; recon:26-31,50-53,109-113; full Q1 ruling.

### Q2 — Presence, open bounds and unrated movies (ACCEPT)

Absent group means Rating is null, applies no predicate and keeps unrated eligible. A supplied group requires both keys; either value may be empty for an open bound. A lone wire key is 422. Every present range excludes unrated, including `minRating=&maxRating=`. `minRating=7&maxRating=` means rating >= 7.0. No include-unrated toggle.

Acceptance proof for the ticket minimum-7 condition uses RatingRange(7m, null), and pairs the empty maximum key when exercising HTTP. Preserve absent versus present-with-null-bounds across all codecs.

Basis: task note:25; recon:30,44,59,79,89,119; full Q2 ruling.

### Q3 — Parsing, validation and defensive predicate (ACCEPT)

HTTP returns 422 for malformed, duplicate, out-of-domain, excess-precision or inverted bounds. Decimal syntax is invariant with dot decimal, no thousands/exponents; 7 and 7.0 are equivalent. No silent rounding or clamping. MovieQueryString.TryParse rejects invalid/duplicate input; Format emits invariant decimal with at most one fractional digit. MovieQueryValidator enforces 0..10, one-decimal validity and min <= max. A direct catalog call with invalid/inverted range returns a defensive empty result, distinct from HTTP validation.

Basis: recon:30-31,45,50,59,89,126 and full Q3 ruling, which supplies Patron-cited duplicate-handling anchors. Keel did not independently inspect those anchors.

### Q4 — Slider and presentation (ACCEPT)

Reuse the local DualRangeField, extent 0..10 and step 0.1, keeping non-crossing thumbs. An absent filter displays [0,10] but remains inactive. The first actual thumb change activates a range. Returning both thumbs to [0,10] remains active and excludes unrated until Clear all resets Rating to absent. Extent-elision must not erase this semantic distinction.

Hydrated open bounds display the corresponding 0/10 end without changing empty URL values on load. An actual slider interaction materializes the displayed finite bounds. Reuse existing state, page reset, URL ownership, fetch/query key, grid/count and empty-state machinery. Count rating as one active filter group when present.

[assumed] Label IMDb rating, accessible minimum/maximum names, one-decimal captions, placement after Release year, existing empty-grid/count presentation. Use existing CSS and native inputs with no dependency. See ASSUMPTIONS.md. Palette design is required before plan challenge.

Basis: recon:64-70,95,119; Q1-Q2 and full Q4 ruling.

### Q5 — Contract and ownership approach (ACCEPT)

BrowseMoviesRequest adds string-bound MinRating/MaxRating explicitly documenting canonical minRating/maxRating, without altering existing parameter names. Add decimal paths in endpoint parsing and MovieQueryString, validation and MovieQuery equality/hash wiring.

Add one WhereRating predicate in MovieRecordQueryExtensions and chain it in EfMovieCatalog before count/paging, AND-composed with existing filters. Exclude null ratings whenever the range is present. Existing ordering remains unchanged.

Follow C# request/endpoint contract -> OpenAPI baseline and Verify snapshot -> generated TypeScript and MSW handlers. Existing tooling uses LAMUFLIX_REGENERATE_OPENAPI=true for baseline/snapshot generation and npm run generate:api for derived web artifacts. Never hand-edit generated files. Consume generated API types; no parallel API DTO definition.

Extend only the live filterQuery.ts URL codec with FilterPanel.tsx, browseParams.ts and LibraryView.tsx; leave test-only libraryUrl.ts untouched.

Basis: recon:26-31,40-46,57-71,90,98,102,106-115,120,122; full Q5 ruling.

### Q6 — Proof strategy (ACCEPT)

Use existing xUnit/FsCheck, real Testcontainers PostgreSQL and Vitest infrastructure. No data-driver mocks for translated rating predicates, new test framework or redundant test-only codec work.

Backend unit cases:
- MovieQueryTests: rating equality and hash participation.
- MovieQueryStringTests: invariant decimal round trips; absent/open/full-span groups; malformed/duplicate/partial/out-of-domain/excess-precision/inverted bounds.
- MovieQueryValidatorTests: domain, precision and bound order.
- MovieQueryFixture: generate valid rating ranges for the existing property suite.

Backend integration cases:
- EfMovieCatalogBrowseTests on real PostgreSQL: minimum 7 with open maximum; inclusive endpoints including 0/7/10 and fractional/equal bounds; unrated excluded for present range and retained with absent range; defensive invalid-empty; AND filtering and count-before-paging.
- LibraryEndpointsTests: canonical paired keys, decimals/open values, partial/duplicate/malformed/domain/precision/order 422 cases.
- OpenApiContractTests: exact parameter list, baseline/snapshot drift and deterministic regeneration.

Web cases in FilterPanel.test.tsx, filterQuery.test.ts and LibraryView.composed.test.tsx:
- Step/extents/captions, keyboard and non-crossing behavior.
- Absent versus active full-span; open-bound hydration without URL mutation; finite bounds after interaction; Clear all.
- Page reset to 1, paired decimal URL/API params, preserved other filters and unknown keys.
- Slider-triggered API fetch produces visible grid/count changes, proving more than a URL update.

Basis: task note:25; recon:74-102; full Q6 ruling.

### Q7 — Ordering and gate expectations (ACCEPT)

Phase A: Quill drafts from this brief -> Keel read-only speckit-analyze and plan/tasks parity against brief -> Palette design prerequisite -> challenge/adjudication -> plan freeze -> Patron provisional Gate 1 process. Neither provisional Gate 1 nor this brief authorizes implementation; the user retains spec-PR merge ownership.

Phase B starts only after authorized pickup checks dependency landing and current-main drift. Build order:
1. Rating model, equality, codec, validator and unit/property fixture coverage.
2. WhereRating, EfMovieCatalog chain and PostgreSQL integration proof.
3. Endpoint parsing/422/OpenAPI contract tests.
4. Generate baseline/snapshot then TypeScript/MSW.
5. Live web state, parameter builder, slider, active-group count and Vitest proof.
6. Configured backend/web gates and all three M review axes.

Enforce current harness.yml thresholds without lowering them. Expected backend gates include Roslyn, complexity, InspectCode, property tests, vulnerable-package/security checks, dotnet tests, format and pre-PR mutation; applicable web gates include tests, lint, typecheck, build and API drift. Use documented/configured commands and scopes at that phase rather than inventing flags or thresholds.

Report exit 2 accurately as SKIPPED (scope-empty), SKIP for configured opt-out, or mutation N/A as appropriate. Could not run is not PASS. No gates are run during this Phase 2 ask.

Basis: recon:15,17,20,90,102,113,123; AGENTS.md verification rules; full Q7 ruling.

### Q8 — Closure (ACCEPT)

Grill closed at Q8/12 with shared understanding. Q1-Q7 scope and the loop terms above are approved. Keel records only conclusions, taste assumptions and this brief. No owner checkbox or outstanding question remains. Quill owns subsequent spec/plan/tasks drafting; later phases remain pending.

Basis: task note:5,7,23-25; recon:105-126; full Q8 ruling.

## Planned file envelope

Production changes approved by Q1-Q5:
- New `src/LamuFlix.Core/Library/RatingRange.cs`; update `src/LamuFlix.Core/Library/MovieQuery.cs` and `MovieQueryString.cs`.
- Update `src/LamuFlix.Infrastructure/Library/MovieQueryValidator.cs`.
- Update `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs` and `EfMovieCatalog.cs`.
- Update `src/LamuFlix.Api/Endpoints/BrowseMoviesRequest.cs` and `LibraryEndpoints.cs`.
- Update `web/src/features/library/filterQuery.ts` and `FilterPanel.tsx`; `web/src/library/browseParams.ts` and `LibraryView.tsx`.
- Regenerate `web/src/api/openapi.json`, its existing OpenApiContractTests Verify snapshot, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts`.

Test files are the Q6 suites at the locations identified in recon sections 3.1-3.3; no unrelated test sweep. Exact existing snapshot/test locations not spelled out by recon must come from recon, not guessed paths.

Phase 2 records: `specs/DEV-335/CONCLUSIONS.md`, `ASSUMPTIONS.md`, `brief.md`. Quill creates later spec.md/plan.md/tasks.md under this feature. Palette owns later design.md.

No edits to ImdbRating/storage mapping/migrations, dependency manifests, sorting, CSS, the parallel libraryUrl codec, playback/secrets, or unrelated files are part of the approved delivery.

## Handoff

Quill may draft spec/plan/tasks using these settled decisions and the full append-only exchanges. Keel then analyzes parity; Palette design must precede challenge. Report missing facts as `needs recon: <question>`; do not broaden discovery. No implementation, tests, gates, commit, PR or Gate 1 claim was made by this Phase 2 work.


## Plan challenge Round 1 — Patron amendment (2026-10-26)

This appended ruling refines Q1-Q7 inside the same ticket delivery. It supersedes ambiguous earlier wording on parsing, malformed URL hydration and test envelope only. Full original ruling and suite-name correction are appended to CONCLUSIONS.md. No ticket or constitution departure or owner checkbox. Plan freeze remains pending verified Quill repairs.

1. **Backend lexical rule**: Empty bound means open. Otherwise accept only ASCII unsigned digits, optionally followed by a dot and exactly one ASCII fractional digit (`[0-9]+(?:\.[0-9])?`, anchored to the complete value). Leading zeros such as `07.0` are accepted. Reject signs including `-0`/`+7`, surrounding whitespace, exponents, commas/thousands, `NaN`, `.5`, `7.`, `7.00`, `7.10` before numeric acceptance in BOTH MovieQueryString and the endpoint. Use invariant parsing with only the decimal-point style allowed. The same rule and failure matrix govern both paths; no new parser layer or file. Numeric domain validity uses existing public `ImdbRating.TryCreate` from validator and defensive predicate, never exposing/editing private IsValid. Each non-null bound must be valid; null bounds are valid; compare order only when both exist. Typed numeric 7.00m is equivalent to 7m; strict textual scale is a wire concern.
2. **Presence and canonical formatting**: Valid present rating groups always emit both keys; null becomes empty, 0 becomes `0`, 10 becomes `10`. Neither key means absent. Never apply runtime/year extent-elision to rating. Canonical output is shortest invariant with at most one fractional digit (`7`, `7.1`); captions remain one decimal. Normalize slider-produced float noise once in live filterQuery.ts at interaction (`7.300000000000001` -> `7.3`). Never round/repair raw URL input.
3. **Invalid URL input**: Recognized invalid rating values, partial groups, domain/precision/order failures preserve raw supplied key presence and values through URL state and outgoing browse parameters; a partial group stays partial (do not invent its pair). The existing HTTP 422 invalid-filter UI handles them. Invalid group display is safe non-crossing `[0,10]`; hydration and unrelated filter changes must not repair or rewrite raw rating data. Deliberate rating-thumb interaction replaces BOTH bounds with valid finite displayed bounds; Clear all removes both. No libraryQueries.ts edit or per-control error UI. Valid both-empty/open groups retain their existing semantics. Presence activation for valid supplied groups must not accidentally drop invalid/partial groups from transport.
4. **Contract casing**: Retain existing documented PascalCase `MinRating`/`MaxRating` and generated type convention; canonical URL/raw endpoint keys remain `minRating`/`maxRating`, per existing case-insensitive binding. No field rename, duplicate API DTO or hand-edited generated file. Tests preserve exact parameter-list equality and prove outgoing canonical keys.
5. **Mutation scope**: Only if rating mutants require them, extend `tests/LamuFlix.UnitTests/Library/MovieQuerySurvivorTests.cs`, `tests/LamuFlix.UnitTests/Library/MovieQueryStringSurvivorTests.cs`, `tests/LamuFlix.IntegrationTests/EfMovieCatalogSurvivorTests.cs` with rating-specific proof. These recon-named existing files join the test envelope; no unrelated sweep. Patron explicitly withdrew the mismatched filenames in the original ruling.
6. **Proof refinements**: T003/T013 cover the lexical matrix including 7.00/7.10 and accepted 07.0; T004 proves null/open bounds valid; T018 proves absent/full-span/both-empty/raw-invalid/partial round trips and interaction-only normalization; T019 proves safe invalid display without URL mutation and keyboard/float precision. T020 uses test-local `server.use` rating-dependent responses and verifies actual outgoing minRating/maxRating plus visible grid/count. Invalid malformed/domain/precision/order/partial URLs fetch 422 and display existing invalid-filter state without URL rewrite. T010 retains real PostgreSQL execution and existing suite translation posture without inventing client-evaluation harness switches; defensive invalid-empty also proves total 0 and empty-page handling.
7. **Implementation/gates**: Keep WhereRating small; decimal-specific private helpers only if required, no generic range framework. Existing suppression scope/comment rules apply to added request properties. No literal threshold override: scripts read configured harness.yml values. Keep author-before-or-with-production tests and passing phase checkpoints; a universal observed-red gate is not added by this challenge. Thresholds and mutation survivor obligations remain unchanged.

Round 1 repair ownership: Quill updates spec.md, plan.md, tasks.md and the exact invalid-state/formatting/casing rules in design.md from this amendment; no production changes. Keel verifies all repairs and records each of the 21 axis findings before plan freeze. Cap remains two plan challenge/fix rounds; no new formal axis round is implied by checking these repairs.

## Plan freeze receipt — current Phase 2 status (2026-10-26)

Keel independently verified Quill repairs against the recorded Patron amendment and all three axis reports. Plan challenge/fix rounds 1 and 2 of the maximum 2 are complete. Round 2 corrected only T021/T022 to separate valid complete paired emission from raw invalid/partial pass-through; no new decision or third review round. All 21 findings are adjudicated, with accepted documentation repairs closed, two partial dispositions explained, and non-blocking Low records retained. No unresolved Critical/High/Medium plan blocker or missing axis remains.

**PLAN FROZEN FOR GATE 1**. This is the current status, superseding earlier historical pending-freeze wording. Freeze covers the working bytes hashed in PLAN_CHALLENGE_ADJUDICATION.md, not merely the unchanged base HEAD. No Gate 1 approval, implementation permission or merge clearance is asserted. Patron provisional Gate 1 process and user-owned spec-PR merge remain next; Phase B still requires authorized pickup, landed DEV-322/DEV-297/DEV-302 and current-main drift check. No tests/gates, production edit, commit, PR or merge performed in this ask.
