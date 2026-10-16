# DEV-432: App layout alignment brief

**Status**: Alignment complete; specification and planning authorized; Human Gate 1 approval pending.
**Date**: 2026-10-15
**Branch**: feature/DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba
**Worktree**: F:/Dev/LamuFlix.worktrees/feature-DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba
**Starting HEAD**: 6f5c72cf5a7f4241b3cd0619147c0991ca4ffb91 (freshly fetched origin/main at intake).

## Problem and source

DEV-432: Design and apply the LamuFlix app layout: shell, filter sidebar, poster grid, and details page.

The tracker describes a largely unstyled SPA with Tailwind installed and a minimal global stylesheet. The intended product is a utilitarian personal movie library: modernized existing layout, JustWatch-style browsing, filters as the star, and YTS as a second filter reference. The task supplies the visual design and shared application shell.

DEV-433 is a prerequisite. Its composed LibraryView is present at the starting HEAD: App supplies the header, LibraryView composes FilterPanel and the results grid, and movie details currently bypass that shell. Existing URL/filter, watchlist, import, and playback behavior must be preserved except for the explicitly agreed interactions below.

## Frozen scope

1. Shared dark-theme tokens and typography: Tailwind theme tokens for colors, spacing, radius, and type scale; readable WCAG AA contrast; consistent use across Library, Watchlist, and Movie Details.
2. Shared shell, text search, and client-side navigation: LamuFlix wordmark and Library, Watchlist, and Import folder actions; navigation between browse views and /movies/{id} without a full reload.
3. Responsive filter placement: collapsible desktop sidebar and mobile modal drawer; removable active-filter chips above the grid alongside result count and sorting.
4. Poster grid: 2:3 poster tiles with title, year, IMDb rating, runtime, and watchlist bookmark; compact Pending, NotFound, and Failed enrichment badges; skeletons and designed empty/error states.
5. Movie details: poster beside available metadata, cast, director, ratings, runtime, and plot; Watchlist and existing server-gated Play actions; a back link restoring the filtered library URL. Per the user's accepted contract boundary (Decision 9), per-movie genres and proactive Play hiding are deferred because the current API exposes neither genre associations nor a playback-capability signal.
6. Responsive and keyboard accessibility: usable at 375px, 768px, and 1440px with no horizontal scroll; visible keyboard focus; filter-drawer focus trap and restoration; a representative library screenshot suitable for DEV-23's README.

Existing APIs and behavior are preserved except for the agreed interactions. Recommendations, thumbnail work beyond DEV-429, and i18n are excluded; anything else is a follow-up issue, not a finding in this round.

## Confirmed interaction and design decisions

1. At widths of 768px and above, use a collapsible left sidebar. Below 768px, use a modal filter drawer.
2. On fresh loads, the desktop sidebar starts expanded and the mobile drawer starts closed. Do not persist panel visibility across reloads. Existing filter URL state is separate from panel visibility.
3. Use dark slate surfaces, warm white text, amber accents, a serif LamuFlix wordmark, and system sans-serif controls/metadata. Use system font stacks without downloaded fonts. specs/DEV-321/design.md section 1 is the visual reference, not authority for its historical filter-band layout or scope. Validate actual contrast pairings.
4. Keep live filter updates in the mobile drawer. Done simply closes it and retains selections. Do not add staged Apply/Cancel edits. Keep the drawer focus trap and return focus to its trigger on close. Library search remains debounced.
5. From movie details, typing into top-bar search does not navigate away. Enter submits to the library, retaining the previous browse filters and replacing the search text. The details back link restores the filtered library URL.
6. Keep this task frontend-only: show current API metadata, preserve Play's accessible server-enforced 403 dialog, and defer per-movie genre data and proactive Play hiding to backend follow-up work. Do not infer movie genres from global facets or invent capability fields.

Full questions, recommendations, user responses, rationale, and implications are preserved in CONCLUSIONS.md.

## Loop discipline

- **Closing bar**: Critical and High findings with a concrete failure scenario block code-review/ship-review clearance. Existing mandatory harness gates remain independently required; a frontend-only C# scope decline is not a completed frontend review.
- **Round cap**: At most two review/fix rounds. Report unresolved blockers at the cap and do not claim clearance. Lower-severity follow-ups do not silently widen the frozen scope.
- **Frozen scope**: The six countable areas above apply; anything else is a follow-up issue, not a finding in this round.

## Gate 1 delivery and verification intent

Prepare spec.md, plan.md, tasks.md, requirements checklist, supporting design/research/contracts, and a read-only cross-artifact consistency analysis. Use a Luna subagent for the requested planning work and independently review its artifacts. The user corrected the earlier Haiku instruction; Decision 8 in CONCLUSIONS.md records the correction.

Plan meaningful regression coverage for new interactions and preserve existing component tests. Plan browser verification at the three acceptance widths, keyboard/focus behavior, filtered-URL restoration, client-side navigation, feature-gated Play, contrast, and the screenshot. This planning stage does not claim these implementation checks have passed.

Gherkin/Reqnroll acceptance files are opt-in and were not requested. Existing component and integration coverage remains required by the repository workflow.

Stop at Human Gate 1 for user approval of spec, plan, and tasks. Do not implement, commit, push, or open a PR during this planning request.

## Vocabulary and ADR disposition

CONTEXT.md remains the canonical glossary. Library, Movie, Watchlist, Import, Playback, and Enrichment Status keep their existing meanings. No new domain concept or qualifying hard-to-reverse architectural decision was introduced by alignment, so no glossary rewrite or ADR is needed at this point.
