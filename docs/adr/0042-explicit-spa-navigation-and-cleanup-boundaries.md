---
status: proposed
---

# Explicit SPA navigation and cleanup boundaries

## Context

DEV-475 requires a shared movie-opening helper, removal of synthetic production popstate, evidence-led dead-code removal and cohesive component extraction while preserving behavior. Its Done-when clause permits follow-ups at spec time. The authoritative ticket text is recovered in the evidence section of [the brief](../../specs/DEV-475/brief.md); [CONCLUSIONS Q2-Q5 and Q10-Q12](../../specs/DEV-475/CONCLUSIONS.md) record the navigation, compatibility and extraction decisions. [PRODUCT sections 2, 3 and 5](../../specs/PRODUCT.md) govern authority and architectural scope; the [constitution](../../.specify/memory/constitution.md) establishes the existing web stack, contract chain and ADR governance.

The backend requires GetPeople role=actor while the generated query contract omits it. Q12 preserves the runtime request rather than introduce 422 failures. This differs from the explicit ticket instruction to remove the parameter; Q13 records the structural escalation under PRODUCT section 5, with owner authorization of that delivery change still outstanding. This proposed decision does not supply that authorization.

## Decision

Keep imperative navigation in web/src/features/app-navigation/appNavigation.ts beside useAppNavigation.ts. The sibling owns shared openMovie and one programmatic pushState/replaceState notification/subscription mechanism. useAppNavigation retains React route-state subscription and native Back/Forward popstate handling. Wire its subscription before producer migration; migrate producers and consumers atomically. All movie/player entry points and details exits use the same history-notification mechanism without synthetic native events, duplicate route updates or new router/state dependencies. Retain the existing openMoviePlayer compatibility wrapper used by MovieCard.

Preserve the distinct entry-point policies: App captures pathname/search and requests playerIntent only when supplied; openMoviePlayer captures a library or prior valid return URL and always requests playerIntent; details/related opening carries the validated return URL without adding playerIntent. Preserve URL validation, filter defaults/fallback, history and cleanup semantics.

Retain FilterPanel, LibraryView and MovieDetailsView as orchestration shells. Their Q10-ratified siblings own cohesive local UI state; query/URL/route/data and existing local-play orchestration remain in the shells. Q12 extracts DualRangeField into its sibling while rating/runtime/year fieldset orchestration stays in FilterPanel; FilterSortControls contains only sort/direction. LibraryFilterSummary defines and exports a pure chip-building function consumed by its summary. Search debounce cancels on navigation/unmount; facet instances independently own their draft/index/refs. Siblings use shell callbacks without local history/popstate or duplicate URL effects. Exact siblings and responsibilities are enumerated in [brief.md](../../specs/DEV-475/brief.md).

Q12 specifies a shared literal-typed GetPeople helper retaining role=actor and removing inline casts from LibraryView and movieDetailsQueries, subject to the structural authorization above. Do not invent generated query types or change backend/OpenAPI/generated contracts. Preserve runtime URL validation and literal-type existing sort/direction values. Remove aliases and the named libraryUrl/test pair only with bounded reference evidence; further whole-file deletions require a cited ruling.

Tailwind disposition is deferred to DEV-511. jsx-a11y adoption and associated semantic fixes are deferred to DEV-512 because the evidenced plugin peer range excludes the pinned ESLint version. Do not add compatibility dependencies, force peers or downgrade ESLint. Existing lint rules remain in force.

## Consequences

Regression coverage must distinguish actual imperative entry points from simulated native traversal and exercise subscriber resynchronization, exactly one update, preserved history/state, unsubscribe/unmount, StrictMode lifecycle and ordinary isolation. Cover MovieCard fallback, pure chip building, debounce cancellation and independent facet keyboards. Existing lint/typecheck/unit/e2e evidence remains required; candidate smoke files are not proof of return-navigation or Back/Forward coverage. Missing scenarios must be added and run.

The cleanup introduces no architectural layer, generated contract change or relocated local-play execution. Deletion remains bounded by evidence, and deferred dependency/style work has explicit follow-ups. Proposed status remains until actual acceptance is evidenced in accordance with the constitution.
