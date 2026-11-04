# DEV-445 frozen grill brief

Status: BRIEF FROZEN — Patron Q1–Q5 confirmed 2026-11-04. This freezes decisions for drafting, not the plan, Gate 1, implementation, verification, or merge.

Worktree: F:/Dev/LamuFlix.worktrees/feature-445-spec
Branch: feature/445-spec
Evidence baseline: b310c56704ad2a9f3e9ae6fba07b08cbb15561d6, recon-DEV-445 and task note DEV-445. Size M, ui: tag. Facts come from recon; missing facts require needs recon to Bernstein. Full questions, answers, rationale, and implications are append-only in CONCLUSIONS.md. Every Patron answer is also reproduced below.

## Problem and authoritative acceptance

Ticket DEV-445 (recon:7): Compose must wire Tmdb__Enabled and Tmdb__ApiKey from .env and document them in .env.example; enrich an OMDb hit with TMDb collection membership using IMDb /find or merging; hide the Collection facet when no collections exist. With a TMDb key set, importing Bond films must yield the James Bond Collection facet and filter to those films in the running Compose stack.

The ticket describes older first-wins behavior. At this baseline DEV-358 already supplements an OMDb Found result with TMDb signals and collection when enabled (recon:32-39,89-91). Retain that implementation and close the actual identity/config/UI gaps; do not replace the chain. Movie-only scope; series/TV concepts are not found in the recon root (recon:26-28).

## Settled decisions and approach

### Q1 — Merge and IMDb lookup

Retain OMDb ownership of primary metadata and TMDb ownership of signals plus collection identity. Use lookup.ManualImdbId ?? found.Metadata.ImdbId for the supplement lookup; preserve original title/year and use title search only when neither IMDb id exists. An IMDb id routes through the existing /find external_source=imdb_id path. A /find miss returns NotFound, with no title-search retry; preserve primary Found with Unavailable signals. Keep disabled-supplement and fallback behavior, including no duplicate supplement call when TMDb itself supplied the Found result. Basis: CONCLUSIONS Q1; recon:32-39,68-70,89-91.

The collection identity and its availability state travel together through the existing merge. Available upserts/sets membership, KnownEmpty clears MovieRecord.CollectionId while retaining the collection row, and Unavailable preserves existing membership. These are existing semantics, confirmed in Patron Q4 with EfMovieRepository.cs:160-169 and EfMovieRepositoryCollectionTests.cs:24-120; do not redesign persistence.

### Q2 — Compose and secrets

Compose api environment must contain exactly:

```yaml
Tmdb__Enabled: ${TMDB_ENABLED:-true}
Tmdb__ApiKey: ${TMDB_API_KEY:-}
```

.env.example documents TMDB_ENABLED=true and an empty TMDB_API_KEY=. The true Compose default is deliberate: a pre-existing .env adding only TMDB_API_KEY must satisfy key-only acceptance. appsettings.json remains Enabled:false outside Compose; docker/api.settings.example.json stays unchanged. Empty/whitespace key remains unconfigured under the existing provider check. TMDB_ENABLED=false remains an opt-out.

README Run with Docker Compose documents setting the local TMDb key in ignored .env, the enablement option, no committed credentials, and docker compose up -d to apply changed environment. Explicitly distinguish existing OMDb credentials in ignored docker/api.settings.json. Never inspect, print, or commit real keys. Basis: Patron Q2, recon:42-49,84,93. Patron explicitly approves the secret channel and README scope under section 2.3 items 5–6.

### Q3 — Collection facet

Hide only when sortedCollections.length === 0 && !collectionFailed. Keep the existing Collection fieldset and error alert when collection fetch fails. Populated facets keep current ordering, labels, and copy. Never auto-clear collectionId from URL/query state when the facet is hidden: existing active-filter chip and Clear All remove it. Record these UX choices as [assumed] in ASSUMPTIONS.md. Basis: Patron Q3; recon:53-56,76,92.

### Q4 — Test strategy and implementation order

1. Add backend/config regression tests and the minimal supplement identity change; update Compose, .env.example, and README together.
2. Prove composed OMDb hit -> IMDb /find -> TMDb details -> signals and collection -> existing persistence. Extend persistence tests only for a missing handoff assertion, preserving existing translation tests with a real database.
3. Implement the empty-facet change using web-implement TDD; preserve error and active-filter behavior.
4. Run targeted regressions, then applicable backend harness and web gates.
5. Run the live Compose Bond acceptance with operator-provided credentials and media; record sanitized evidence. Unavailable runtime/key is Could not run and leaves acceptance open.
6. Complete review within the frozen loop terms below.

Required evidence:

- Unit chain tests: manual-id precedence, OMDb-id forwarding, no-id original title/year, OMDb metadata retention, TMDb-only merge fields, NotFound -> Found/Unavailable, disabled no call, fallback no repeated call.
- WireMock composition/lookup tests: exact /find IMDb and external_source request, details fetch and collection/signals handoff, zero title search on /find miss. Retain resilience/quota regressions.
- Persistence: Available persists, KnownEmpty clears membership without deleting the collection row, Unavailable preserves membership. Existing endpoint/catalog tests cover facet and collectionId filtering; use them as regression evidence.
- Static configuration assertions: exact Compose interpolation/defaults, .env.example entries/empty key, appsettings disabled default. Do not read .env or use real secrets. Runtime default/override behavior is exercised during Compose acceptance; static checks alone do not prove a running stack.
- Web: hidden healthy-empty group, visible populated group, visible fetch-error group, active collectionId retained, chip removal and Clear All continue to work. Replace the obsolete empty-legend assertion.
- Runtime: with a local key, import Bond films in Compose; verify persisted collection data, visible James Bond Collection facet, and selecting it filters to those films. A mocked pass does not close this runtime criterion.

## Approved implementation file boundary

Production/config/docs:

- src/LamuFlix.Infrastructure/Adapters/ChainedMetadataProvider.cs — minimal supplement lookup forwarding.
- docker-compose.yml — api TMDb environment only.
- .env.example — TMDb option/key placeholders and guidance.
- README.md — Compose credential setup and apply instructions.
- web/src/features/library/FilterPanel.tsx — conditional Collection fieldset.

Tests, approved by Patron Q3–Q4:

- tests/LamuFlix.UnitTests/Adapters/MetadataProviderChainTests.cs
- tests/LamuFlix.IntegrationTests/MetadataProviderChainCompositionTests.cs
- tests/LamuFlix.IntegrationTests/SecondaryMetadataProviderLookupTests.cs
- tests/LamuFlix.IntegrationTests/EfMovieRepositoryCollectionTests.cs — only if composed handoff needs additional coverage.
- tests/LamuFlix.UnitTests/TmdbComposeTests.cs — new, static non-secret configuration tests.
- web/src/features/library/FilterPanel.test.tsx
- web/src/library/LibraryView.toolbar.test.tsx — if needed for active-selection/toolbar integration coverage.

Planning artifacts are specs/DEV-445/brief.md, CONCLUSIONS.md, ASSUMPTIONS.md, then Quill-owned spec.md, plan.md, tasks.md and Palette-owned design.md. No production writes by Keel. Additional source/test write scope needs a recorded Patron decision; missing facts go to recon. No new NuGet/npm dependency, project, architecture layer, schema/migration, public API shape, LocalPlay, or Process.Start work is authorized. Existing C# DTO -> OpenAPI -> generated TS contracts remain the source for web consumers. No ADR is needed for this M-sized bounded fix; do not invent one or its number.

## Loop discipline — Patron Q5

Closing bar: High/Critical findings block. Every Medium/Low gets an explicit cited disposition. Any failing required gate or unrun required live acceptance blocks closure regardless of finding severity.

Frozen scope: Compose TMDb enablement/key wiring and secret documentation, IMDb supplement identity with the existing merge/persistence semantics, healthy-empty Collection facet hiding with preserved error and active-selection behavior, and the named regression/config/runtime evidence; anything else is a follow-up issue, not a finding in this round.

Round cap: two review/fix rounds. Stop and report unresolved blockers at the cap; do not add an immutable review to repair a receipt or encoding problem.

Gate expectations: applicable backend harness analyzers, complexity, InspectCode, unit/integration/property tests, vulnerable-package scan, formatting, and pre-PR Stryker mutation, using harness.yml thresholds without lowering them; frontend gates under web-implement. Live Compose acceptance is independently required. SKIPPED (scope-empty), SKIP opt-out, N/A, and Could not run are distinct from PASS. Do not claim an unrunnable gate passed.

No ticket contradiction or constitution departure was ruled necessary; zero owner escalation checkboxes. Patron explicitly confirmed shared understanding and brief freeze in Q5.

## Handoff and phase boundaries

Quill drafts spec.md, plan.md, and tasks.md from this brief and its complete answers. Palette supplies design.md before any plan challenge because DEV-445 is ui:. Keel then runs read-only speckit-analyze and compares the draft with this brief, issuing one numbered fix list per round. Resolve decision changes in brief.md before corresponding artifact edits. Plan challenge, plan freeze, Gate 1, implementation, runtime validation, review, and user-owned merge remain separate states. This brief grants none of those later clearances.

No new domain vocabulary or ADR was introduced: use existing glossary terms (metadata enrichment, signals, collection membership/facet). Existing vocabulary and the settled decisions are ready to feed speckit-specify; do not contradict established ADRs.

## Complete Patron answers

---

### Patron Q1

[from Patron] DEV-445 Q1 Ruling — Ruled in scope: retain existing DEV-358 OMDb-primary + TMDb-supplement merge ownership and forward `lookup.ManualImdbId ?? found.Metadata.ImdbId` into the supplement lookup via the existing `/find` path, with `/find` no-match returning `NotFound` and preserving primary `Found` with `Unavailable` signals (no fallback to title/year search).

- **Merge & Persistence Ownership (`recon-DEV-445:7, 32-39, 89-91`; `ChainedMetadataProvider.cs:53-73`; `EfMovieRepository.cs:157-168`; `specs/PRODUCT.md` §2–§3)**: Ticket DEV-445 specifies fetching collection membership as enrichment of an OMDb hit (by IMDb id via `/find`) or merging; retaining the DEV-358 `SupplementAsync`/`Merge` ownership (OMDb primary metadata + TMDb signals and collection with availability state) satisfies `EfMovieRepository.ApplyCollectionIdentity` (`Signals.CollectionAvailability != Unavailable`) without introducing any new abstraction or API.
- **Supplement Identity Precedence (`recon-DEV-445:7, 35-36, 90`; `ChainedMetadataProvider.cs:53-68`; `TmdbMetadataProvider.cs:75-77, 154-180, 395-397`; `OmdbMetadataProvider.Mapping.cs:90-91`)**: Preferring explicit `lookup.ManualImdbId`, otherwise `found.Metadata.ImdbId`, and falling back to the original title/year `lookup` only when no IMDb ID is available routes OMDb hits with a valid IMDb ID directly through TMDb's existing `FindThenFetchAsync` (`/find?external_source=imdb_id`) path.
- **No-Match `/find` & Unsuccessful Supplement (`TmdbMetadataProvider.cs:163-176`; `ChainedMetadataProvider.cs:64-67`)**: When `/find` returns no match (`MovieResults` empty or no result), `FindThenFetchAsync` returns `MetadataLookupResult.NotFound` without falling back to a title/year search, and `SupplementAsync` preserves the primary OMDb `Found` with `new MovieSignalSnapshot()` (`Unavailable` signals), maintaining existing chain and composed adapter fallback semantics.


---

### Patron Q2

[from Patron] DEV-445 Q2 Ruling — Ruled in scope: wire `Tmdb__Enabled: ${TMDB_ENABLED:-true}` and `Tmdb__ApiKey: ${TMDB_API_KEY:-}` on the Compose `api` service, document `TMDB_ENABLED=true` and empty `TMDB_API_KEY=` in `.env.example`, keep `appsettings.json` (`Enabled: false`) and `docker/api.settings.example.json` unchanged, and update `README.md` to document the `.env` TMDb key flow alongside the existing `docker/api.settings.json` OMDb flow.

- **Exact Names, Defaults & Key-Only Acceptance (`recon-DEV-445:6-7, 44-49`; `docker-compose.yml:57-63`; `.env.example:1-18`; `appsettings.json:17-20`)**: `docker-compose.yml` (`services.api.environment`) sets `Tmdb__Enabled: ${TMDB_ENABLED:-true}` and `Tmdb__ApiKey: ${TMDB_API_KEY:-}`, and `.env.example` sets `TMDB_ENABLED=true` and `TMDB_API_KEY=` (empty placeholder so `IsNullOrWhiteSpace(ApiKey)` holds when unset); defaulting Compose `Tmdb__Enabled` to `true` directly resolves "TMDb is off in Compose" and satisfies the key-only acceptance criterion ("with a TMDb key set... in the running Compose stack") even when a pre-existing `.env` only adds `TMDB_API_KEY`, while `appsettings.json` remains `Enabled: false` outside Compose.
- **§2.3 Item 5 Secret Channel Ruling (`recon-DEV-445:7, 45-48, 84`; `specs/PRODUCT.md` §3, §5.5; `.gitignore:7, 495`)**: The ticket's *Scope & Technical Design* explicitly mandates wiring `Tmdb__Enabled` and `Tmdb__ApiKey` from `.env` and documenting them in `.env.example`; `.env` is already gitignored, no secret or non-empty placeholder is committed, and the existing OMDb secret channel in gitignored `docker/api.settings.json` remains untouched.
- **§2.3 Item 6 Documentation File Scope (`recon-DEV-445:7, 47, 85, 93`; `specs/PRODUCT.md` §5.6; `README.md:53-63`)**: Editing `.env.example` (explicitly named in the ticket) and `README.md` (`Run with Docker Compose` section) is approved in scope to explain configuring a local `TMDB_API_KEY` (and `TMDB_ENABLED`) in `.env`, keeping credentials uncommitted, running `docker compose up -d` to apply `.env` changes, and distinguishing `.env` (TMDb) from `docker/api.settings.json` (OMDb).


---

### Patron Q3

[from Patron] DEV-445 Q3 Ruling — Ruled in scope (with taste assumption `[assumed]`): hide the Collection `<fieldset>` in `FilterPanel.tsx` only when `collections` is empty and `!collectionFailed`, preserve the error fieldset when `collectionFailed` is true, preserve any active `collectionId` in URL/query state without auto-clearing, and approve edits to `FilterPanel.tsx`, `FilterPanel.test.tsx`, and `LibraryView.toolbar.test.tsx` (if needed) under §2.3 item 6.

- **Empty, Error & Active-Selection Behavior (`recon-DEV-445:7, 53-56, 92`; `FilterPanel.tsx:339, 450, 519-550`; `LibraryView.tsx:507-513`; `specs/PRODUCT.md` §3–§4)**: When `sortedCollections.length === 0 && !collectionFailed`, omit the Collection `<fieldset>` entirely per ticket ("hide the Collection facet when no collections exist"); when `collectionFailed` is true, continue rendering the Collection `<fieldset>` and `"Collections could not be loaded."` alert (`FilterPanel.tsx:521-524`); when populated, render chips with existing ordering/labels/copy; never silently strip an active `collectionId` from URL/query state when the facet is hidden (users clear stale selections via the existing active-filter chip or Clear All).
- **Taste Assumption to Record (`specs/PRODUCT.md` §4; `specs/DEV-445/ASSUMPTIONS.md`)**: Log `[assumed]` that (a) hiding applies only to a healthy empty list (`collections.length === 0 && !collectionFailed`) while fetch failures still render the Collection group with its existing error alert, and (b) an active `collectionId` in URL/state is preserved when the facet is hidden rather than silently mutated, relying on the existing active-filter chip and Clear All action.
- **§2.3 Item 6 File Scope Approval (`recon-DEV-445:7, 53-56, 76, 85, 92`; `specs/PRODUCT.md` §5.6; `FilterPanel.test.tsx:881-900`)**: Editing `web/src/features/library/FilterPanel.tsx`, `web/src/features/library/FilterPanel.test.tsx` (updating lines 881-891 where empty-facet legend rendering was previously asserted), and `web/src/library/LibraryView.toolbar.test.tsx` (if needed for empty-facet/toolbar coverage) is approved in scope as forced by the ticket requirement to hide the empty Collection facet.


---

### Patron Q4

[from Patron] DEV-445 Q4 Ruling — Ruled in scope: test strategy, verified collection-availability persistence semantics, execution order, runtime Compose acceptance rule, and §2.3 item 6 file scope (`MetadataProviderChainTests.cs`, `MetadataProviderChainCompositionTests.cs`, `SecondaryMetadataProviderLookupTests.cs`, `EfMovieRepositoryCollectionTests.cs`, new `tests/LamuFlix.UnitTests/TmdbComposeTests.cs`, `FilterPanel.test.tsx`, and `LibraryView.toolbar.test.tsx`) are approved.

- **Chain, Composed Adapter & Verified Availability Semantics (`recon-DEV-445:32-39, 65, 68-70, 89-91`; `ChainedMetadataProvider.cs:53-73`; `EfMovieRepository.cs:160-169, 516-554`; `EfMovieRepositoryCollectionTests.cs:24-120`)**: `MetadataProviderChainTests.cs`, `MetadataProviderChainCompositionTests.cs`, and `SecondaryMetadataProviderLookupTests.cs` are approved to prove `ManualImdbId ?? found.Metadata.ImdbId` forwarding, `/find?external_source=imdb_id` lookup, zero title search on `/find` miss, and preserved fallback/resilience behavior; `EfMovieRepository.cs:160-169` and `EfMovieRepositoryCollectionTests.cs:24-120` already confirm that `Available` upserts and sets `CollectionId`, `KnownEmpty` clears `CollectionId` to `null` (retaining the `collections` row), and `Unavailable` preserves existing `CollectionId`, so extend `EfMovieRepositoryCollectionTests.cs` only if needed for composed OMDb+TMDb metadata handoff.
- **§2.3 Item 6 Test File Scope & Execution Order (`recon-DEV-445:44-48, 75-76, 85`; `ObservabilityComposeTests.cs:9-60`; `FilterPanel.test.tsx:881-900`; `specs/PRODUCT.md` §5.6)**: Adding `tests/LamuFlix.UnitTests/TmdbComposeTests.cs` (static file assertions on `docker-compose.yml` `Tmdb__Enabled: ${TMDB_ENABLED:-true}` / `Tmdb__ApiKey: ${TMDB_API_KEY:-}`, `.env.example` `TMDB_ENABLED=true` / empty `TMDB_API_KEY=`, and `appsettings.json` `Enabled: false`, without reading `.env` or secrets) and updating the named C# and web test files in the proposed execution order are approved in scope.
- **Runtime Compose Acceptance & Honest Gate Verdict (`recon-DEV-445:7`; `AGENTS.md` Verification commands)**: Live Compose verification (importing Bond films with a local TMDb key produces the "James Bond Collection" facet and filters to those films, without recording secrets) is required runtime acceptance; if live credentials or the runtime stack are unavailable when running verification, it must be reported as `Could not run` and never folded into `PASS`.


---

### Patron Q5

[from Patron] DEV-445 Q5 Ruling — Approved and confirmed for brief freeze: closing terms, frozen scope, two-round review cap, backend/web/runtime gate expectations, Palette `design.md` + Quill spec/plan/tasks handoff before Keel read-only analysis, and zero owner escalations (`blocked: structural` is not triggered).

- **Standing Closing Bar, Frozen Scope & Round Cap (`recon-DEV-445:7, 78-94`; `specs/PRODUCT.md` §2–§5; `specs/DEV-445/CONCLUSIONS.md` Q1–Q4)**: High/Critical findings, any failing required gate, and unrun required runtime acceptance block closure while Medium/Low receive explicit cited disposition; scope is frozen to Compose TMDb enablement/key wiring and docs, `ManualImdbId ?? found.Metadata.ImdbId` supplement identity with existing merge/persistence semantics, healthy-empty Collection facet hiding with preserved error/active-selection behavior, and the approved test/config/runtime evidence (max 2 review/fix rounds, no extra round for receipt/encoding repair).
- **Gate & Runtime Acceptance Honesty (`AGENTS.md` Verification commands; `harness.yml`; `web-implement`)**: All applicable backend harness gates (analyzers, complexity, InspectCode, unit/integration/property tests, vulnerable packages, format, pre-PR Stryker mutation), `web-implement` frontend gates, and live Compose Bond collection acceptance apply; `SKIPPED (scope-empty)`, `SKIP`, `N/A`, and `Could not run` are never folded into `PASS`.
- **Pipeline Handoff & No §2.3 Escalations (`recon-DEV-445:7, 78-85`; `specs/PRODUCT.md` §3, §5)**: No ticket contradiction or constitution departure exists (no new dependency, project/layer, DB schema, public API shape, `Features:LocalPlay`, or `Process.Start`); brief freezes now for Quill (`spec.md`, `plan.md`, `tasks.md`) and Palette (`design.md` before plan challenge), followed by Keel read-only `/speckit-analyze` and separate Gate 1 adjudication.


---

## Q6 — Plan challenge decision amendment (2026-11-04)

Keel question: Recon addendum confirms that enabled blank/whitespace TMDb key sends zero TMDb HTTP but changes OMDb NotFound into retryable ProviderUnavailable and eventual Failed; primary Found stays Found/Unavailable. Retain Q2 exact Compose true default and allowlist while accepting these existing semantics with explicit README opt-out warning and T006 composed blank/whitespace-key proofs? Document rate-limit collapse and require expected/imported/matched Bond counts with sanitized quota evidence, with demonstrated environmental inability only Could not run and acceptance open? Keep existing loading hide, cite typed ImdbId normalization to reject S-9 string guard, and reconcile backend optional TDD/test-before-edit/red-where-feasible versus mandatory web red-green? No ticket or constitution departure proposed.

Patron answer (full ruling):
[from Patron] DEV-445 Q6 Ruling — Approved and ruled in scope: retain Q2 Compose `TMDB_ENABLED:-true` default and frozen production allowlist while expressly accepting existing blank/whitespace-key failure semantics with README warning and `T006` composed proofs (Sentry F1), document supplement rate-limit collapse and `T020` collection-count/quota evidence rules (Sentry F2), keep initial-load empty hiding as accepted existing design (Sentry F4), cite `ImdbId.TryCreate` normalization and retain Q1 null-coalescing without extra file scope (Ledger S-9), and align `tasks.md` on optional backend TDD (`T003` before `T005`, red where feasible) with mandatory web red-green TDD (Ledger S-10).

- **Sentry F1 — Blank/Whitespace Key Semantics, README Warning & T006 Proofs (`recon-DEV-445-2:9-26`; `TmdbMetadataProvider.cs:68-81`; `ChainedMetadataProvider.cs:15-50, 53-92`; `ProcessEnrichmentCommandHandler.cs:44-62, 155-201`; `SecondaryMetadataProviderLookupTests.cs:445-463`; `specs/DEV-445/CONCLUSIONS.md` Q2)**: Retain exact Q2 Compose wiring (`Tmdb__Enabled: ${TMDB_ENABLED:-true}`, `Tmdb__ApiKey: ${TMDB_API_KEY:-}`) and the frozen production allowlist while expressly accepting existing failure semantics when `Enabled=true` with a blank/whitespace `ApiKey` (zero TMDb HTTP; OMDb `NotFound` returns retryable `Failed(ProviderUnavailable)` for up to default `MaxAttempts = 3` before terminal `Failed`; OMDb `Found` swallows supplement failure into `Found` with `Unavailable` signals); require `README.md` (and `plan.md:144` / `T004`) to warn operators to set `TMDB_ENABLED=false` in `.env` until a real `TMDB_API_KEY` exists if OMDb-only `NotFound` behavior is desired, and require `T006` (`MetadataProviderChainCompositionTests.cs` / `SecondaryMetadataProviderLookupTests.cs`) to prove composed blank/whitespace-key behavior (OMDb miss => `Failed(ProviderUnavailable)`, OMDb `Found` => `Found` with `Unavailable` signals, zero TMDb HTTP).
- **Sentry F2 & F4 — Supplement Rate-Limit Collapse, T020 Evidence & Loading State (`recon-DEV-445:32-39, 53-56`; `recon-DEV-445-2:18, 26`; `ChainedMetadataProvider.cs:53-68`; `FilterPanel.tsx:339, 450, 519-550`; `specs/DEV-445/plan.md:122, 143-147`; `specs/DEV-445/tasks.md:120`; `specs/DEV-445/design.md` §4)**: For Sentry F2, document in `plan.md`/`spec.md` that supplement `RateLimited`/`QuotaExhausted`/`ProviderUnavailable` collapses to primary OMDb `Found` with `Unavailable` signals (leaving collection membership unset), retain the existing failure theory without production edits, and require `T020` to record imported/expected/matched Bond collection counts plus sanitized rate-limit/quota evidence (incomplete Bond membership cannot pass; environmental quota/rate-limit unavailability may be reported as `Could not run` with acceptance left open per Q4–Q5); for Sentry F4, confirm that hiding the Collection group during initial load (`collections` undefined/empty and `!collectionFailed`, no loading prop) remains accepted existing design under Q3 without adding props, spinners, or file scope.
- **Ledger S-9 & S-10 — IMDb ID Normalization Citation & Test Sequencing (`recon-DEV-445-2:30-42`; `OmdbMetadataProvider.Mapping.cs:90-91`; `ImdbId.cs:20-34`; `EnrichmentEndpoints.cs:53-57`; `AGENTS.md` `implement` / `web-implement`; `specs/DEV-445/tasks.md:9, 58-65, 83-87, 139-142`)**: For Ledger S-9, cite `OmdbMetadataProvider.Mapping.MapIdentifier` and `ImdbId.TryCreate` (`^tt[0-9]{7,8}$`, normalizing `null`, `""`, whitespace, and `"N/A"` to `null`) plus `PUT /api/movies/{id}/imdb` validation in `plan.md` and keep Q1 null-coalescing (`lookup.ManualImdbId ?? found.Metadata.ImdbId`) with no new string guard or extra normalization test/file scope; for Ledger S-10, reconcile `tasks.md` header, `T003`, and `Dependencies` so backend TDD remains optional while requiring `T003` identity unit tests before `T005` production edits with red-first where feasible (preserving existing green locks), and keep `web-implement` red-green TDD mandatory for `T009`–`T011`.

Decision effect and drafting instructions: Q6 qualifies earlier Q2 wording: unconfigured means zero TMDb HTTP, not disabled-chain equivalence. It leaves all Q1-Q5 scope/default/acceptance terms intact. For the Q6 phrase leaving collection membership unset, apply unchanged Q4 semantics precisely: Unavailable leaves a new membership unset and preserves any existing membership; never clear it. T006 proves composed outcomes and zero TMDb HTTP; no out-of-allowlist end-to-end enrichment test or registration edit is authorized. No new taste choice: loading hide follows the existing Q3 predicate and design, and chip label/order corrections record the incumbent UI.

Plan adjudication decisions: retain Sentry F1-F3; accept F4 loading hide. Retain Ledger S-1-S-8, S-10 wording repair only, S-11-S-12; drop S-9 alleged production defect on recon normalization evidence while adding citation; defer S-13 optional FR-013/SC-007 cleanup to preserve traceability. Retain Compass M1/M2/L1 documentation repairs. T010 uses a derived const beside collectionFailed, no state/effect. No new dependency, layer, schema, route/DTO, LocalPlay, Process.Start, or production file is authorized. All twenty dispositions are in adjudication-DEV-445. Plan remains unfrozen pending Quill repair/read-back; Gate 1 remains closed.

---

## PLAN FROZEN — Phase 2 step 6 adjudication receipt (2026-11-04)

Keel freezes the repaired DEV-445 plan for Gate 1 review on feature/445-spec in F:/Dev/LamuFlix.worktrees/feature-445-spec, recon baseline b310c56704ad2a9f3e9ae6fba07b08cbb15561d6. This receipt supersedes earlier draft, awaiting-design, proposed-Q6, and awaiting-freeze statuses. Patron Q6 is fully appended above; no ticket contradiction, constitution departure, new scope, or owner checkbox was introduced.

All 20 findings from Sentry (4), Ledger (13), and Compass (3) are adjudicated in adjudication-DEV-445, fichario LamuFlix notes: 17 required repair items verified, Sentry F4 accepted existing loading-hide behavior, Ledger S-9 alleged junk-ID defect dropped on typed normalization evidence (citation added), Ledger S-13 optional process cleanup deferred with traceability retained. No unresolved High/Critical finding or unresolved required plan repair remains. The existing blank-key failure semantics are explicitly accepted by Patron Q6 and documented with opt-out warning plus required composed tests; this is not a claim of disabled equivalence.

Keel independently read back all eleven items in Quill repair round 1 and all four Palette design repairs. Sources: recon-DEV-445 and recon-DEV-445-2; full Quill report C:/Users/lamun/AppData/Local/Temp/DEV-445-quill-repair-round1-report.txt (the literal --file transport message was recovered by reading this report, without a new review round). The two-round cap remains unchanged; no extra immutable review was created. Read-only consistency reanalysis is CLEAN: 13 FR + 7 SC covered by 24 tasks T000-T023; correct dependency/TDD sequencing, named file boundaries, unchanged DTO -> OpenAPI -> generated TS contracts, separated C# and web gates, and design baseline corrected. No new analysis finding.

Gate 1 remains CLOSED pending Patron provisional review, Rigger spec PR, and user-owned merge. Freeze does not grant Gate 1 approval, Phase B pickup, implementation, runtime acceptance, review/delivery clearance, or merge. T000 still requires verified user merge receipt; T001 still requires fresh recon, blocking drift reanalysis, and Gauge baseline. Live Compose Bond acceptance and applicable gates remain mandatory later; none ran or passed in Phase A. Existing unrelated harness.yml modification is outside this work and remains untouched.
