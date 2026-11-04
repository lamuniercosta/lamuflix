# Feature Specification: Collection Filter TMDb Enablement

**Feature Branch**: `feature/445-spec`

**Created**: 2026-11-04

**Status**: gate1: provisional

**Input**: Ticket DEV-445 (bug under DEV-100; Size M, `ui:`); frozen `specs/DEV-445/brief.md` (Q1–Q5) plus Q6 plan-challenge amendment; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-445`, `recon-DEV-445`, `recon-DEV-445-2`; `specs/PRODUCT.md`. No decisions beyond the brief/CONCLUSIONS. No new domain vocabulary or ADR (brief Handoff). Taste assumptions already logged in `ASSUMPTIONS.md` (Q3). Palette `design.md` exists with Compass M1/M2/L1 and Ledger S-8 corrections verified.

## Scope

**In scope** (brief Problem and authoritative acceptance; Settled decisions Q1–Q4; Approved implementation file boundary; Frozen scope Q5):

1. Wire Compose `api` environment with exact `Tmdb__Enabled: ${TMDB_ENABLED:-true}` and `Tmdb__ApiKey: ${TMDB_API_KEY:-}`; document `TMDB_ENABLED=true` and empty `TMDB_API_KEY=` in `.env.example`; keep `appsettings.json` `Tmdb.Enabled: false` and `docker/api.settings.example.json` unchanged.
2. Document the local TMDb key flow in `README.md` (`Run with Docker Compose`): set key/enablement in ignored `.env`, no committed credentials, `docker compose up -d` to apply env changes, and distinguish `.env` (TMDb secrets only) from ignored `docker/api.settings.json` (OMDb). Warn operators to set `TMDB_ENABLED=false` until a real `TMDB_API_KEY` is set when OMDb-only `NotFound` behavior is desired (Q6 / Sentry F1). Never inspect, print, or commit real keys.
3. Retain existing DEV-358 OMDb-primary + TMDb-supplement merge ownership (OMDb primary metadata; TMDb signals and collection identity with availability). Minimal change: forward `lookup.ManualImdbId ?? found.Metadata.ImdbId` into the supplement lookup so an available IMDb id uses the existing `/find?external_source=imdb_id` path; preserve original title/year and use title search only when neither IMDb id exists.
4. On `/find` miss: return NotFound with no title-search retry; preserve primary Found with Unavailable signals. Keep disabled-supplement and fallback behavior, including no duplicate supplement call when TMDb itself supplied the Found result.
5. Preserve existing collection persistence semantics: Available upserts/sets membership; KnownEmpty clears `MovieRecord.CollectionId` while retaining the collection row; Unavailable leaves new membership unset and preserves any existing membership (Q4; Q6 precise wording). Do not redesign persistence. Supplement `RateLimited` / `QuotaExhausted` / `ProviderUnavailable` collapses to primary OMDb `Found` with `Unavailable` signals under the existing failed-supplement theory (recon-DEV-445-2:18,26; Q6 / Sentry F2) — no production resilience/pacing scope.
6. Hide the Collection `<fieldset>` in Filter Panel only when the healthy-empty condition holds (`sortedCollections.length === 0 && !collectionFailed`). Preserve error fieldset and `"Collections could not be loaded."` alert on fetch failure; preserve populated ordering/labels/copy; never auto-clear `collectionId` from URL/query when the facet is hidden (existing chip / Clear All remove it).
7. Prove required unit/composition/persistence/config/web regressions and live Compose Bond acceptance per Q4 evidence list.

**Out of scope** (brief Frozen scope Q5; §2.3; recon gaps closed without expansion):

- Replacing or redesigning the metadata chain merge/ownership (retain DEV-358).
- Series/TV/episode collection concepts (not found in recon root).
- New NuGet/npm dependency, project, architectural layer, schema/migration, public API shape (route/DTO/status), `Features:LocalPlay`, or `Process.Start`.
- Changing OMDb secret channel (`docker/api.settings.json`); committing non-empty TMDb keys; reading real `.env` in tests.
- New ADR, glossary/`CONTEXT.md` rewrite, or constitution amendment (Size M; Q5).
- Auto-clearing hidden-facet URL state; new Collection labels/copy; hiding the facet on fetch error.
- Anything outside the frozen Q1–Q4 surfaces — follow-up issue, not a finding in this round (Q5).

**Production/config/docs allowlist** (Q1–Q3; brief Approved implementation file boundary):

- `src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs` — minimal supplement lookup forwarding.
- `docker-compose.yml` — api TMDb environment only.
- `.env.example` — TMDb option/key placeholders and guidance.
- `README.md` — Compose credential setup and apply instructions.
- `web/src/features/library/FilterPanel.tsx` — conditional Collection fieldset.

**Test allowlist** (Q3–Q4):

- `tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs`
- `tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs`
- `tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderLookupTests.cs`
- `tests/LamuFlix.IntegrationTests/EfMovieRepositoryCollectionTests.cs` — only if composed handoff needs additional coverage.
- `tests/LamuFlix.UnitTests/TmdbComposeTests.cs` — new, static non-secret configuration tests.
- `web/src/features/library/FilterPanel.test.tsx`
- `web/src/library/LibraryView.toolbar.test.tsx` — if needed for active-selection/toolbar integration coverage.

**Precedence**: Ticket Fix/Acceptance text (recon) plus Patron Q1–Q6 in `brief.md` / `CONCLUSIONS.md` are authoritative. Phase A produces documents only and claims no implementation gate evidence. Palette owns `design.md` (`ui:`); Quill does not author it.

## What the User Will See *(mandatory)*

- **Where**: Library Filter Panel Collection section; Library browse filtered by collection; Compose-based local run setup docs.
- **What the user sees or does**:
  - With a local TMDb key in ignored `.env` and Compose up, importing Bond films yields a visible **James Bond Collection** facet; selecting it filters the library to those films.
  - When no collections exist and the collections fetch succeeded, the Collection filter group is absent (healthy empty).
  - When collections fetch fails, the Collection group remains with the existing error alert.
  - An active collection selection survives a hidden facet until the user removes it via the active-filter chip or Clear All.
- **What it needs to show up**: Running Compose stack; operator-provided TMDb API key in local `.env` (never committed); Bond-film media for the runtime acceptance; OMDb credentials remain in ignored `docker/api.settings.json` as today.

### Smoke Test *(maps to live Compose Bond acceptance — not a new web e2e suite)*

- **SMOKE-001**: **Given** Compose is running with a local `TMDB_API_KEY` set (Compose TMDb enabled by default), **When** the operator imports Bond films and enrichment completes, **Then** the Library Filter Panel shows the James Bond Collection facet and selecting it filters the grid to those films.
- **SMOKE-002**: **Given** the library has no stored collections and collections fetch succeeded, **When** the operator opens the Filter Panel, **Then** the Collection group is not shown.

SMOKE-001 is the ticket runtime acceptance (Q4); unavailable credentials/stack → `Could not run`, never PASS. Web unit coverage separately proves healthy-empty hide, error-visible, and active-selection retention (Q3–Q4).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Compose TMDb enablement and secret docs (Priority: P1)

Compose wires TMDb enablement and API key from `.env` so a key-only local setup turns the supplement on. Documentation covers `.env` TMDb vs JSON OMDb, empty key placeholder, applying env changes, and the Q6 operator warning to set `TMDB_ENABLED=false` until a real key exists. Non-Compose appsettings stay disabled by default. Enabled + blank/whitespace key is not disabled-chain equivalence (Q6 / Sentry F1).

**Why this priority**: Ticket root cause is TMDb off in Compose; without enablement the collection facet stays empty even with a key (brief Problem; Q2).

**Independent Test**: Static assertions on Compose interpolation/defaults, `.env.example` entries/empty key, `appsettings.json` disabled default, and tracked `docker/api.settings.example.json` has no `Tmdb` section or `TMDB_API_KEY` material — without reading `.env` or secrets; README documents the dual credential channels and blank-key opt-out warning. Runtime default/override + Bond sequence is the single operational receipt in T020 (do not duplicate the full sequence here).

**Acceptance Scenarios**:

1. **Given** Compose `api` environment, **When** inspected, **Then** it contains exactly `Tmdb__Enabled: ${TMDB_ENABLED:-true}` and `Tmdb__ApiKey: ${TMDB_API_KEY:-}`.
2. **Given** `.env.example`, **When** inspected, **Then** it documents `TMDB_ENABLED=true` and empty `TMDB_API_KEY=`.
3. **Given** `appsettings.json`, **When** inspected, **Then** `Tmdb.Enabled` remains `false`; tracked `docker/api.settings.example.json` is unchanged and contains no `Tmdb` section or `TMDB_API_KEY` material.
4. **Given** README Run with Docker Compose, **When** read, **Then** it documents setting the local TMDb key/enablement in ignored `.env` (TMDb secrets only), no committed credentials, `docker compose up -d` to apply changes, distinguishes OMDb’s ignored JSON file, and warns operators to set `TMDB_ENABLED=false` until a real `TMDB_API_KEY` is set when OMDb-only `NotFound` behavior is desired.
5. **Given** a pre-existing `.env` that only adds `TMDB_API_KEY`, **When** Compose starts with defaults, **Then** enablement defaults true so key-only acceptance is satisfiable — proven by the single operational runtime sequence in T020 (static checks alone do not prove a running stack).
6. **Given** `Tmdb.Enabled=true` with blank or whitespace `ApiKey`, **When** enrichment runs, **Then** zero TMDb HTTP occurs; an OMDb miss yields retryable `Failed(ProviderUnavailable)` (default three attempts then terminal `Failed`); an OMDb hit yields `Found` with `Unavailable` signals (new membership unset; any existing membership preserved per Q4). This is not disabled-chain equivalence (Q6; composed proofs in T006; existing chain failure matrices remain regression evidence; no out-of-allowlist end-to-end retry test).

---

### User Story 2 - IMDb supplement identity and collection merge (Priority: P1)

When OMDb returns Found and TMDb supplement is enabled, the chain forwards `ManualImdbId ??` OMDb `ImdbId` into the supplement lookup so collection membership resolves via `/find`. Merge ownership and persistence availability semantics stay as today.

**Why this priority**: Ticket requires enriching an OMDb hit with TMDb collection membership by IMDb id; recon shows the IMDb handoff gap at HEAD (brief Q1; recon:35–36,90).

**Independent Test**: Unit chain tests for identity precedence and NotFound/Unavailable; WireMock composition/lookup for exact `/find` request and zero title search on miss; persistence Available/KnownEmpty/Unavailable regressions (extend only if composed handoff needs it).

**Acceptance Scenarios**:

1. **Given** primary Found with `lookup.ManualImdbId` set, **When** supplement runs, **Then** that manual id is preferred for the TMDb lookup.
2. **Given** primary Found with no manual id but OMDb metadata `ImdbId` present, **When** supplement runs, **Then** that id is forwarded and TMDb uses `/find?external_source=imdb_id`.
3. **Given** neither IMDb id, **When** supplement runs, **Then** the original title/year lookup is preserved (title search path).
4. **Given** an IMDb `/find` miss, **When** supplement finishes, **Then** result is NotFound with no title-search retry, and primary Found is preserved with Unavailable signals.
5. **Given** TMDb disabled or fallback where TMDb itself supplied Found, **When** FindAsync runs, **Then** disabled path makes no supplement call and fallback does not repeat the supplement call.
6. **Given** merged collection availability Available / KnownEmpty / Unavailable, **When** persistence applies, **Then** Available upserts/sets membership, KnownEmpty clears membership without deleting the collection row, Unavailable leaves new membership unset and preserves any existing membership (never clears it).
7. **Given** primary OMDb `Found` and supplement returns `RateLimited`, `QuotaExhausted`, or `ProviderUnavailable`, **When** the chain finishes, **Then** the result remains primary `Found` with `Unavailable` signals under the existing failed-supplement theory (recon-DEV-445-2:18,26; Q6 / Sentry F2) — no new resilience/pacing production scope.

---

### User Story 3 - Hide healthy-empty Collection facet (Priority: P1)

The Filter Panel omits the Collection group when there are no collections and fetch did not fail. Errors and populated facets keep current behavior; active `collectionId` is never silently cleared.

**Why this priority**: Ticket requires hiding the Collection facet when no collections exist; current UI always renders it (brief Q3; recon:54–55,92).

**Independent Test**: Web-implement TDD on FilterPanel (and LibraryView toolbar tests if needed): hidden healthy-empty, visible populated, visible fetch-error, active `collectionId` retained, chip removal and Clear All still work; replace obsolete empty-legend assertion.

**Acceptance Scenarios**:

1. **Given** `collections` empty and `!collectionFailed`, **When** Filter Panel renders, **Then** the Collection fieldset is omitted.
2. **Given** `collectionFailed` is true, **When** Filter Panel renders, **Then** the Collection fieldset and existing error alert remain visible.
3. **Given** a populated collections list, **When** Filter Panel renders, **Then** chips use existing ordering, labels, and copy.
4. **Given** an active `collectionId` while the facet is hidden, **When** the panel re-renders, **Then** URL/query state is unchanged until the user clears via active-filter chip or Clear All.

---

### User Story 4 - Live Compose Bond acceptance (Priority: P2)

With operator-provided credentials and Bond media, the running Compose stack persists collection membership, shows the James Bond Collection facet, and filters to those films. Mocked passes do not close this criterion.

**Why this priority**: Ticket Acceptance and Q4 require live proof; unavailable runtime/key is `Could not run` and leaves acceptance open (never PASS).

**Independent Test**: Single operational runtime sequence in T020 (key-only default, explicit false opt-out, restore enabled Bond). Record expected/imported/matched Bond membership counts plus sanitized environmental quota/rate-limit evidence; incomplete membership never passes. Sanitized evidence only (no secrets recorded).

**Acceptance Scenarios**:

1. **Given** Compose with a local TMDb key and Bond films imported per T020, **When** enrichment completes with complete Bond membership, **Then** James Bond Collection appears as a facet and selecting it filters to those films; expected/imported/matched counts and any sanitized quota/rate-limit evidence are recorded.
2. **Given** demonstrated environmental inability (credentials, stack, media, or environmental quota/rate-limit unavailability), **When** acceptance cannot run, **Then** it is reported `Could not run` and remains open — never folded into PASS, never reclassified from unexplained mismatch, and never substituted by mocked evidence alone. Incomplete membership cannot pass.

### Edge Cases

- Empty/whitespace `TMDB_API_KEY` with `Enabled=true` is not disabled-chain equivalence (Q6 / Sentry F1; recon-DEV-445-2:9-26): zero TMDb HTTP; OMDb miss → retryable `Failed(ProviderUnavailable)` (default `MaxAttempts = 3` then terminal `Failed`); OMDb hit → `Found` with `Unavailable` signals. `IsConfigured` still requires a non-whitespace key. Compose enablement default remains `true` (Q2).
- `TMDB_ENABLED=false` remains an explicit Compose opt-out; README must warn operators to use it until a real key is set when OMDb-only `NotFound` is desired (Q2, Q6).
- `/find` miss must not fall back to title search (Q1).
- Supplement NotFound / RateLimited / QuotaExhausted / ProviderUnavailable collapses to primary Found with Unavailable signals under existing failed-supplement theory; Unavailable leaves new membership unset and preserves any existing membership — never clears it (Q1, Q4, Q6).
- Disabled supplement path (`Enabled=false` / TMDb absent from chain): no TMDb call; primary unchanged — distinct from blank-key enabled path (Q1, Q6).
- Fallback Found from TMDb itself: no duplicate supplement call (Q1).
- Healthy-empty hide only — fetch failure still shows the group; initial-load hide (`collections` undefined/empty and `!collectionFailed`, no loading prop) remains accepted existing design (ASSUMPTIONS.md; Q3; Q6 / Sentry F4).
- Active selection retained across hidden facet — no silent URL mutation (ASSUMPTIONS.md; Q3).
- Static Compose/config tests must not read `.env` or real secrets; tracked `api.settings.example.json` has no Tmdb material (Q4, Q6 / Sentry F3).
- Existing endpoint/catalog tests and existing chain failure matrices remain regression evidence; public API shape unchanged; no out-of-allowlist end-to-end enrichment retry test or registration edit (Q4; Q6; §2.3).
- Incomplete Bond membership never passes runtime acceptance; only demonstrated environmental inability may be `Could not run` (Q6 / Sentry F2).
- Anything outside frozen scope is a follow-up issue, not a finding in this round (Q5).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Compose `api` environment MUST set `Tmdb__Enabled: ${TMDB_ENABLED:-true}` and `Tmdb__ApiKey: ${TMDB_API_KEY:-}` exactly (Q2).
- **FR-002**: `.env.example` MUST document `TMDB_ENABLED=true` and empty `TMDB_API_KEY=`; MUST NOT commit a non-empty key placeholder (Q2).
- **FR-003**: `appsettings.json` MUST remain `Tmdb.Enabled: false` outside Compose; tracked `docker/api.settings.example.json` MUST remain unchanged and MUST contain no `Tmdb` section or `TMDB_API_KEY` material (Q2, Q6 / Sentry F3).
- **FR-004**: README Run with Docker Compose MUST document local TMDb key/enablement via ignored `.env` (TMDb secrets only), no committed credentials, `docker compose up -d` to apply env changes, distinguish OMDb’s ignored JSON channel, and warn operators to set `TMDB_ENABLED=false` until a real `TMDB_API_KEY` is set when OMDb-only `NotFound` behavior is desired (Q2, Q6; §2.3 items 5–6).
- **FR-005**: Retain OMDb-primary + TMDb-supplement merge ownership; production chain edit MUST stay minimal forwarding in `ChainedMetadataProvider.cs` — `lookup.ManualImdbId ?? found.Metadata.ImdbId` for supplement lookup; title/year only when neither id exists (Q1).
- **FR-006**: An IMDb id MUST route through existing `/find?external_source=imdb_id`; a `/find` miss MUST return NotFound with no title-search retry; SupplementAsync MUST preserve primary Found with Unavailable signals (Q1).
- **FR-007**: Disabled-supplement and fallback (no duplicate supplement when TMDb supplied Found) MUST remain unchanged (Q1).
- **FR-008**: Collection availability persistence MUST remain Available upsert/set, KnownEmpty clear membership retain row, Unavailable leave new membership unset and preserve any existing membership — no redesign (Q1, Q4, Q6). Supplement `RateLimited`/`QuotaExhausted`/`ProviderUnavailable` MUST continue to collapse to primary Found with Unavailable signals under the existing failed-supplement theory without new production resilience/pacing scope (Q6 / Sentry F2).
- **FR-009**: Filter Panel MUST omit Collection fieldset iff healthy empty (`sortedCollections.length === 0 && !collectionFailed`); MUST preserve error fieldset/alert on failure; MUST preserve populated ordering/labels/copy; MUST NOT auto-clear `collectionId` (Q3; ASSUMPTIONS.md).
- **FR-010**: Production/config/docs/web edits MUST stay on the named allowlist; test edits MUST stay on the named test allowlist (extend `EfMovieRepositoryCollectionTests.cs` / `LibraryView.toolbar.test.tsx` only when needed per Q4) (Q1–Q4; §2.3).
- **FR-011**: Required evidence MUST cover Q4 unit chain, WireMock composition/lookup, persistence semantics, static Compose/config assertions, web hide/error/active-selection cases, and live Compose Bond acceptance (Q4).
- **FR-012**: No new dependency, project/layer, schema/migration, public API shape, LocalPlay, or Process.Start; existing C# DTO → OpenAPI → generated TS contracts remain the consumer source without hand-rolled duplicates (Q5; §2.3).
- **FR-013**: Phase B verification MUST run applicable backend harness gates (analyzers, complexity, InspectCode never `-All`, unit/integration/property tests, vulnerable packages, format, pre-PR mutation without `--since` in worktrees) and `web-implement` / web gates; thresholds only from `harness.yml`; live Compose Bond acceptance independently required; `SKIPPED (scope-empty)`, `SKIP`, `N/A`, and `Could not run` never recorded as PASS; Size M three-axis review under Q5 closing bar (High/Critical block; Medium/Low explicit cited disposition); round cap two (Q5).

### Key Entities

- **Supplement lookup identity**: Prefer explicit `ManualImdbId`, else OMDb Found `ImdbId`, else original title/year lookup.
- **Collection membership + availability**: Collection id/name travel with signal availability through existing merge; drive persistence via Available / KnownEmpty / Unavailable.
- **Healthy-empty Collection facet**: Empty collections list with successful fetch — hide the Filter Panel Collection group.
- **Compose TMDb secret channel**: Ignored `.env` `TMDB_API_KEY` / `TMDB_ENABLED`, distinct from OMDb’s ignored JSON settings file.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With a local TMDb key in Compose, importing Bond films produces a James Bond Collection facet that filters to those films with recorded expected/imported/matched membership counts (ticket acceptance; SMOKE-001; T020) — incomplete membership never passes; only demonstrated environmental inability reports `Could not run` without claiming PASS (Q4, Q6).
- **SC-002**: Static Compose/`.env.example`/`appsettings`/tracked `api.settings.example.json` negative-lock assertions match the exact Q2 names and defaults without reading secrets (Q2, Q4, Q6).
- **SC-003**: Unit/WireMock proofs show four identity branches, `/find` on IMDb id, zero title search on `/find` miss, NotFound → Found/Unavailable, blank/whitespace-key composed outcomes with zero TMDb HTTP, disabled/fallback behaviors (Q1, Q4, Q6).
- **SC-004**: Persistence Available/KnownEmpty/Unavailable semantics remain green — Unavailable leaves new membership unset and preserves any existing membership (Q1, Q4, Q6).
- **SC-005**: Web tests prove healthy-empty hide (role/name absence + positive controls), populated visible, fetch-error visible, and active `collectionId` retention with chip/Clear All (Q3, Q4, Q6; SMOKE-002).
- **SC-006**: No new public API/DTO/schema/dependency/LocalPlay/Process.Start change is required (Q5; §2.3).
- **SC-007**: Applicable gates pass or report SKIPPED/SKIP/N/A/Could not run distinctly — never as PASS; review closing bar and round cap hold (Q5). Retained with traceability; optional S-13 trimming deferred (Q6).

## Assumptions

- Taste assumptions already recorded in `specs/DEV-445/ASSUMPTIONS.md` (Q3): healthy-empty-only hide; preserve active `collectionId` when facet hidden.
- DEV-358 supplement/merge at baseline is retained; this ticket closes identity/config/UI gaps rather than replacing the chain (brief Problem; recon:32–39,89).
- Movie-only scope; series/TV not present in recon root (recon:26–28).
- Phase B pickup must refresh recon for main/contract drift; Phase A recon baseline is not clearance (Q5 handoff).

## Clarifications

All grill categories were resolved by Patron Q1–Q5 before drafting; Q6 amends plan-challenge decisions without ticket or constitution departure. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR, Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. No owner Gate 1 checkbox (Q5 — zero owner escalations). Gate 1 remains closed pending Keel freeze verification.

## Traceability

| Source | Encoded as |
|---|---|
| Ticket Fix / Acceptance (Compose + enrich + hide + Bond) | Scope; US1–US4; FR-001–FR-011; SC-001–SC-005; SMOKE-001/002 |
| Q1 merge ownership + IMDb supplement identity | US2; FR-005–FR-008; Edge Cases; SC-003–SC-004 |
| Q2 Compose defaults + secret docs | US1; FR-001–FR-004; SC-002 |
| Q3 healthy-empty facet + taste assumptions | US3; FR-009; ASSUMPTIONS.md; SC-005 |
| Q4 tests / order / runtime acceptance | US2–US4; FR-010–FR-011; plan Test Strategy; tasks phases |
| Q5 closing bar / frozen scope / round cap / gates | FR-012–FR-013; SC-006–SC-007; plan Gates |
| Q6 blank-key semantics, rate-limit collapse, T020 counts, TDD sequencing, ImdbId citation | US1 #4–#6; US2 #6–#7; US4; Edge Cases; FR-004, FR-008; plan Strategy/Risks; T002–T006, T009–T012, T020 |
| recon-DEV-445 / recon-DEV-445-2 paths / HEAD gaps | Scope allowlists; Edge Cases; Assumptions |
| §2.3 secret channel + file scope (Patron) | FR-001–FR-004, FR-010, FR-012; Scope |
