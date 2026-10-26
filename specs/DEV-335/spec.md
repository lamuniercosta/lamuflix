# Feature Specification: DEV-335 Add IMDb rating range filter

**Feature Branch**: `feature/335-spec`

**Created**: 2026-10-26

**Status**: gate1: provisional

**Input**: Ticket DEV-335 (size M, tag `ui:`); settled grill brief `specs/DEV-335/brief.md` (grill closed Q8/12) including Plan challenge Round 1 Patron amendment; facts from note `recon-DEV-335`; Patron rulings in `specs/DEV-335/CONCLUSIONS.md`; taste in `specs/DEV-335/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md` and its appended amendment.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse by IMDb rating range (Priority: P1)

A library browser asks for movies whose IMDb rating falls inside an inclusive range from 0.0 through 10.0 with at most one decimal place. Movies without a rating are kept when no rating filter is active, and are excluded as soon as a rating range is present — including a full-span range and a range with open bounds. A request for a minimum of 7 with an open maximum returns every rated movie at 7.0 or above.

**Why this priority**: Ticket acceptance requires `minRating=7` integration proof and unrated exclusion when a range is set. Without catalog filtering there is no feature.

**Independent Test**: Call `EfMovieCatalog.BrowseAsync` on real PostgreSQL with `RatingRange(7m, null)` and with other inclusive and fractional bounds; prove unrated rows are excluded when `Rating` is present and retained when `Rating` is absent; prove AND composition with other filters and count-before-paging; prove a direct catalog call with invalid or inverted bounds returns an empty result.

**Acceptance Scenarios**:

1. **Given** movies rated below 7, at 7.0, above 7, and unrated, **When** browse runs with `RatingRange(7m, null)`, **Then** only rated movies at or above 7.0 are returned and unrated rows are absent.
2. **Given** the same catalog, **When** browse runs with no rating range (`Rating` null), **Then** unrated movies remain eligible alongside rated ones.
3. **Given** a present rating range including open bounds or full-span `0.0`–`10.0`, **When** browse runs, **Then** every matching rated movie inside the inclusive bounds is returned and every unrated movie is excluded.
4. **Given** inclusive endpoints at 0.0, 7.0, and 10.0, equal min and max, and fractional bounds such as 7.5, **When** browse runs, **Then** matches are inclusive on both ends.
5. **Given** a rating range combined with other active filters, **When** browse runs, **Then** filters AND together and the reported total reflects the filtered set before paging.
6. **Given** a direct catalog call with invalid or inverted rating bounds, **When** browse runs, **Then** the result set is empty (defensive), distinct from HTTP 422 validation.

---

### User Story 2 - Pass rating bounds on the movies API (Priority: P2)

A client calls `GET /api/movies` with the paired canonical query keys `minRating` and `maxRating`. Both keys must be present for a supplied group; either value may be empty for an open bound. Malformed, duplicate, out-of-domain, excess-precision, inverted, or single-key requests fail with 422. Valid decimals use invariant dot syntax; `7` and `7.0` are equivalent. The OpenAPI baseline, Verify snapshot, generated TypeScript, and MSW handlers document and consume the new parameters through the existing regeneration tooling.

**Why this priority**: The ticket names Filter Panel `minRating`/`maxRating` and the contract chain; HTTP is the seam between catalog filtering and the UI.

**Independent Test**: Extend `LibraryEndpointsTests` for paired keys, open bounds, decimals, and 422 cases; extend `OpenApiContractTests` for the exact parameter list and drift; regenerate baseline and derived artifacts with `LAMUFLIX_REGENERATE_OPENAPI=true` then `npm run generate:api` — never hand-edit generated files.

**Acceptance Scenarios**:

1. **Given** `minRating=7&maxRating=`, **When** the movies endpoint is called, **Then** the dispatched query carries an open-maximum rating range and the response is success for otherwise valid input.
2. **Given** only one of `minRating` or `maxRating`, **When** the movies endpoint is called, **Then** the response is 422.
3. **Given** malformed, duplicate, out-of-domain (`<0` or `>10`), excess-precision text (`7.00`, `7.10`), sign/whitespace/exponent/comma/leading-or-trailing-dot forms, or inverted bounds, **When** the movies endpoint is called, **Then** the response is 422 with no silent rounding or clamping. Leading-zero forms such as `07.0` are accepted. The same lexical rule applies in both `MovieQueryString` and endpoint parse paths.
4. **Given** invariant decimals such as `7`, `7.0`, and accepted `07.0`, **When** parsed and formatted, **Then** they are numerically equivalent and Format emits the shortest invariant form with at most one fractional digit (`7`, `7.1`, `0`, `10`). Typed numeric `7.00m` equals `7m`; strict textual scale is a wire concern only.
5. **Given** the regenerated OpenAPI baseline, **When** contract drift tests run, **Then** documented/generated parameter names remain PascalCase `MinRating`/`MaxRating` while canonical raw wire keys remain camelCase `minRating`/`maxRating`; the exact parameter-list assertion stays equality (not subset); generated TypeScript/MSW match the baseline with no new DTO and no hand-edit.

---

### User Story 3 - Adjust the IMDb rating slider (Priority: P3)

A user opens the Filter Panel and uses the IMDb rating dual-range control (extent 0..10, step 0.1). An absent filter shows the full span but stays inactive. The first thumb change activates the range; returning both thumbs to `[0,10]` stays active and continues to exclude unrated movies until Clear all removes the group. Hydrated open bounds display the corresponding 0/10 end without rewriting empty URL values until the user interacts. Moving the slider resets page to 1, writes paired decimal URL/API params, updates the grid and match count, preserves other filters and unknown URL keys, and counts as one active filter group.

**Why this priority**: Ticket acceptance requires the slider to update grid and URL. The panel depends on the API contract from US2.

**Independent Test**: Extend `FilterPanel.test.tsx`, `filterQuery.test.ts`, and `LibraryView.composed.test.tsx` for step/extents/captions/keyboard/non-crossing, absent versus active full-span, open-bound hydration, Clear all, page reset, paired params, and slider-triggered fetch that changes visible grid/count.

**Acceptance Scenarios**:

1. **Given** no rating filter in the URL, **When** the panel loads, **Then** the control shows `[0,10]`, stays inactive, and does not send rating params (keys omitted).
2. **Given** an inactive full-span display, **When** the user moves a thumb, **Then** the range becomes active, page resets to 1, paired `minRating`/`maxRating` appear in the URL and API query with shortest ≤1dp values, slider float noise is normalized once in `filterQuery.ts` at that interaction, and the grid/count refetch with those outgoing keys.
3. **Given** an active range returned to `[0,10]`, **When** the user has not cleared filters, **Then** the range remains active with keys present as `0`/`10` (not elided) and unrated stay excluded until Clear all sets Rating to absent.
4. **Given** a URL with open bounds such as `minRating=7&maxRating=`, **When** the panel hydrates, **Then** the open end displays 0 or 10 without mutating empty URL values; a later thumb interaction materializes finite bounds for both ends.
5. **Given** other filters and unrecognized URL keys, **When** the rating slider changes, **Then** those filters and keys are preserved and rating counts as one active filter group.
6. **Given** a URL with recognized invalid, partial, out-of-domain, excess-precision, or inverted rating values, **When** the panel loads or unrelated filters change, **Then** raw supplied rating key presence and values are preserved through URL state and outgoing browse params, the slider displays safe non-crossing `[0,10]`, the existing HTTP 422 invalid-filter UI appears without rewriting the URL, and only a deliberate rating-thumb interaction replaces both bounds or Clear all removes them.

### Edge Cases

- Absent rating group (`Rating` null): no predicate; unrated movies remain eligible; serialize omits both keys.
- Present group with both keys empty (`minRating=&maxRating=`): excludes unrated; open bounds on both ends; both keys emitted with empty values.
- Present group with one open bound (`minRating=7&maxRating=` or the reverse): inclusive closed end; excludes unrated.
- Active full-span `[0,10]` after user interaction: semantically distinct from absent; emits `minRating=0&maxRating=10` (never elided to absent); excludes unrated until Clear all.
- Single wire key without its pair (partial group): preserve raw key presence/value through URL and outgoing params; 422 at HTTP; do not invent the missing pair; display safe `[0,10]` without URL mutation.
- Malformed / out-of-domain / excess-precision / inverted recognized values: preserve raw supplied text and key presence through URL and outgoing browse params; 422 at HTTP via existing invalid-filter UI; validator rejects; direct catalog returns empty; no silent repair of raw URL text; no `libraryQueries.ts` edit.
- Lexical rejects (both parse paths): signs (`+7`, `-0`), surrounding whitespace, exponents, commas/thousands, `NaN`, `.5`, `7.`, `7.00`, `7.10`. Lexical accepts: unsigned ASCII digits with optional dot + exactly one fractional digit, including leading zeros (`07.0`).
- Slider float noise (e.g. `7.300000000000001`): normalized once on interaction in live `filterQuery.ts` to shortest ≤1dp (`7.3`); captions remain one decimal; raw pasted invalid URL text is never normalized.
- Unrelated filter changes must not repair or rewrite raw invalid/partial rating data.
- No include-unrated toggle (unlike runtime).
- Existing sort-by-rating ordering is unchanged.
- Test-only `libraryUrl.ts` is not extended; live ownership stays `filterQuery.ts`.
- No schema migration; `imdb_rating` already exists.
- No new dependency, project, layer, LocalPlay, secrets, or Process.Start change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST add `RatingRange(decimal? Min, decimal? Max)` and `MovieQuery.Rating`. Bounds are inclusive 0.0 through 10.0 with at most one decimal place, aligned with `ImdbRating` validity. Rating MUST participate in custom equality, hash, and `HasSameCoreValues`.
- **FR-002**: Canonical wire keys MUST be `minRating` and `maxRating` (ticket spelling). A supplied group requires both keys; either value may be empty for an open bound. Absent group means `Rating` is null.
- **FR-003**: Any present `RatingRange`, including both-null bounds, MUST exclude movies whose `ImdbRating` is null. `minRating=7&maxRating=` MUST mean rating >= 7.0. There MUST be no include-unrated toggle.
- **FR-004**: HTTP MUST return 422 for malformed, duplicate, out-of-domain, excess-precision, inverted, or partial (single-key) rating input. Empty bound means open. Otherwise BOTH `MovieQueryString` and endpoint parse paths MUST accept only ASCII unsigned digits optionally followed by a dot and exactly one ASCII fractional digit (`[0-9]+(?:\.[0-9])?`, anchored to the complete value), using invariant culture with only the decimal-point style allowed. Leading zeros such as `07.0` are accepted. Signs including `-0`/`+7`, surrounding whitespace, exponents, commas/thousands, `NaN`, `.5`, `7.`, `7.00`, and `7.10` MUST be rejected before numeric acceptance. Typed numeric `7.00m` equals `7m`; strict textual scale is a wire concern. No silent rounding or clamping.
- **FR-005**: `MovieQueryString` MUST parse/format rating under FR-004, reject invalid/duplicate input, and preserve absent versus present-with-null-bounds. Format MUST emit the shortest invariant form with at most one fractional digit (`7`, `7.1`, `0`, `10`). `MovieQueryValidator` and the defensive catalog predicate MUST reuse public `ImdbRating.TryCreate` for numeric domain/precision (never expose or edit private `IsValid`; no `ImdbRating.cs` edit). Each non-null bound MUST be independently valid; null bounds are valid; min <= max is compared only when both bounds exist.
- **FR-006**: `EfMovieCatalog.BrowseAsync` MUST apply one small `WhereRating` predicate (AND-composed, before count/paging; decimal-specific private helpers only if required, no generic range framework) that excludes null ratings whenever the range is present. Invalid/inverted bounds on a direct catalog call MUST return a defensive empty set (total 0, empty page). Existing ordering MUST NOT change.
- **FR-007**: `BrowseMoviesRequest` MUST add string-bound `MinRating`/`MaxRating` documenting canonical `minRating`/`maxRating` without renaming existing parameters and without a new DTO. OpenAPI/generated names remain PascalCase `MinRating`/`MaxRating`; canonical raw URL/endpoint wire keys remain camelCase `minRating`/`maxRating` (existing case-insensitive binding). Endpoint parsing MUST gain a decimal path that shares FR-004's lexical rule. New request properties MUST stay inside the existing scoped ReSharper suppression pair; no new explanatory comments.
- **FR-008**: Contract chain MUST follow C# request/endpoint → OpenAPI baseline (`web/src/api/openapi.json`) and Verify snapshot → generated `web/src/api/types.ts` and MSW handlers via `LAMUFLIX_REGENERATE_OPENAPI=true` and `npm run generate:api`. Generated files MUST never be hand-edited. Exact OpenAPI parameter-list assertion MUST remain equality (not subset). Web MUST consume generated API types only; browse builders check generated names, not hand-typed duplicates.
- **FR-009**: Web MUST extend only live `filterQuery.ts`, `FilterPanel.tsx`, `browseParams.ts`, and `LibraryView.tsx` (active-group count). Reuse local `DualRangeField` at extent 0..10 step 0.1 with non-crossing thumbs. Valid present rating groups MUST always emit both keys (`null`→empty, `0`→`"0"`, `10`→`"10"`); neither key means absent; never apply runtime/year extent-elision to rating. Absent filter displays `[0,10]` inactive; first thumb change activates; return to `[0,10]` stays active until Clear all. Open-bound hydration MUST NOT rewrite empty URL values until interaction. Slider-produced float noise MUST be normalized once in live `filterQuery.ts` at interaction to shortest ≤1dp; captions remain one decimal; raw URL text MUST never be repaired. Recognized invalid, partial, domain, precision, or order failures MUST preserve raw supplied key presence and values through URL state and outgoing browse params (a partial group stays partial); invalid group display is safe non-crossing `[0,10]`; existing HTTP 422 invalid-filter UI handles them; deliberate rating-thumb interaction replaces BOTH bounds with valid finite displayed bounds, or Clear all removes both; unrelated filter changes MUST NOT rewrite raw rating data. MUST NOT edit `libraryQueries.ts` or add per-control error UI. Page reset, URL ownership, fetch/query key, grid/count, and empty-state machinery MUST be reused. Rating counts as one active filter group when present. Test-only `libraryUrl.ts` MUST remain untouched.
- **FR-010**: Proof MUST use existing xUnit/FsCheck, Testcontainers PostgreSQL, and Vitest suites named in brief Q6, plus conditional rating-specific extensions only in recon-named `MovieQuerySurvivorTests.cs`, `MovieQueryStringSurvivorTests.cs`, and `EfMovieCatalogSurvivorTests.cs` when mutation requires them — no new test framework, no redundant test-only codec work, no unrelated survivor sweep. Composed web proof MUST use test-local `server.use` rating-dependent responses and assert actual outgoing paired canonical `minRating`/`maxRating` plus visible grid/count; generated handlers remain unchanged by hand. Catalog proof MUST retain real PostgreSQL translation under the existing suite posture (no invented client-evaluation harness switches) and prove defensive-empty total 0 / empty page. Gates use current `harness.yml` thresholds without lowering them and without literal threshold overrides in task commands. Exit 2 remains SKIPPED (scope-empty) / SKIP / mutation N/A as appropriate; Could not run is not PASS. Author tests before or with production changes and keep passing phase checkpoints; do not add a universal observed-red gate.
- **FR-011**: This feature MUST NOT add a NuGet/npm dependency, project, architectural layer, schema migration, sort change, CSS/dependency for the slider, parallel-codec cleanup, LocalPlay/secrets/`Process.Start` change, or edits outside the planned file envelope in `brief.md` (including no `ImdbRating.cs` edit).

### Key Entities

- **RatingRange**: Inclusive optional min/max decimal bounds (0.0–10.0, ≤1 decimal). Null `MovieQuery.Rating` means absent filter.
- **MovieQuery.Rating**: Optional rating filter member on the existing browse query; participates in equality/hash/core-value comparison.
- **ImdbRating**: Existing domain/storage value (nullable on the movie). Unrated means null; already projected on browse summaries.
- **Filter Panel rating control**: Local dual-range UI over the live filter URL codec; not a new component package.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A browse with minimum rating 7 and open maximum returns only movies rated ≥ 7.0 and excludes unrated rows (ticket AC).
- **SC-002**: When no rating filter is active, unrated movies remain in browse results; when any rating range is present, unrated movies are excluded.
- **SC-003**: Invalid HTTP rating input (partial, malformed including lexical rejects such as `7.00`/`7.10`/sign/whitespace/exponent/comma/leading-trailing-dot, duplicate, out-of-domain, inverted) yields 422; accepted forms including leading zeros round-trip without silent repair; Format uses shortest ≤1dp.
- **SC-004**: OpenAPI baseline, Verify snapshot, and generated TypeScript/MSW include PascalCase `MinRating`/`MaxRating` while raw wire keys remain `minRating`/`maxRating`; exact parameter-list equality and drift checks pass after regeneration.
- **SC-005**: Moving the IMDb rating slider updates the URL with paired canonical decimal keys (interaction-normalized ≤1dp; full-span not elided), refetches with those outgoing params, and changes the visible grid and match count while preserving other filters; invalid/partial rating URLs keep raw values, show safe `[0,10]`, and surface the existing 422 invalid-filter UI without URL rewrite (ticket AC).
- **SC-006**: Backend and applicable web gates report their script verdicts without lowered thresholds or literal overrides; scope-empty skips are not recorded as pass; all three M review axes (Sentry, Ledger, Compass) run when review starts; plan challenge/fix cap remains two rounds.

## Assumptions

- Label `IMDb rating`, accessible minimum/maximum names, one-decimal captions, placement after Release year, and existing empty-grid/count presentation are confirmed `[assumed]` taste rulings in `specs/DEV-335/ASSUMPTIONS.md` (Q4). Palette `design.md` remains a prerequisite before plan challenge.
- Reuse of native `DualRangeField` and existing CSS with no new dependency is confirmed by Q4.
- Dependencies DEV-322, DEV-297, and DEV-302 are named by the ticket; their specs being present in the worktree is not Phase B landing or current-main drift clearance (brief Q7).
- No glossary rewrite or ADR is required for this additive range (brief Q8).
