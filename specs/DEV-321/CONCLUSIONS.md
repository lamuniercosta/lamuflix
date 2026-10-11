# DEV-321 — Patron conclusions

## Q1 (Keel, Phase A grill 1/12) — contract path for tile fields

**Question.** Which contract path supplies every ticket tile field? Keel recommends extending the
existing browse summary and port projection with nullable `posterUrl`, `releaseYear`, `runtime`,
`imdbRating` plus required `enrichmentStatus` (existing enum); preserve `id`/`title`/`items`/`totalCount`
and the existing route; no per-tile detail requests; no schema change.

**Ruling: APPROVED, amended (charter §2.3 item 4, public API shape).** Extend `MovieSummary` (port) →
`MovieSummaryResponse` → `openapi.json` → generated TS, additively, on `GET /api/movies`.

- Basis: ticket DEV-321:24 requires poster, title, year, runtime, IMDb rating badge and EnrichmentStatus
  chip per tile; recon-DEV-321:24-30 shows only `title` is on the browse item today. Charter contract
  chain (C# DTO → OpenAPI → generated TS) and PRODUCT.md:12 (contract drift enforcement) require the
  fields to originate in the C# DTO, not be stitched client-side. Per-tile `GET /api/movies/{id}` calls
  are rejected (N+1, and details carry no poster/status — recon:29).
- Field semantics (amendment):
  - `posterUrl`: string, nullable; the stored `PosterUrl` value passed through unchanged; null when
    not enriched or absent. The SPA renders a placeholder for null.
  - `releaseYear`, `runtime`, `imdbRating`: nullable; **names, types and units must match the existing
    `MovieMetadataResponse` fields** (LibraryResponses.cs:30-46) so one concept has one shape across
    browse and details. Exact names/types: *needs recon* — if details uses e.g. `runtimeMinutes`, use that.
  - `enrichmentStatus`: required, the existing `EnrichmentStatus` enum (Pending/Enriched/NotFound/Failed),
    serialized the same way existing enums/smart-enums are serialized on the API. Serialization form
    (string name vs integer): *needs recon*; string names are required in OpenAPI so the generated TS is a
    string union.
  - Unchanged: `id`, `title`, `items`, `totalCount`, route, query params, status codes. Change is additive.
- Limits: no schema change (§2.3 item 3). That year/runtime/rating/status are readable from existing
  persisted columns without a migration is *needs recon* (only `poster_url` is cited, recon:30); if any
  field needs a migration or new join beyond the existing catalog query, stop and re-ask Patron. Backend
  gates (Roslyn, complexity, InspectCode, mutation) apply to the backend half; the mixed task is accepted.

## Q2 (Keel, Phase A grill 2/12) — URL vocabulary and sync mechanism

**Question.** Approve URL vocabulary and sync mechanism? Keel recommends native `URLSearchParams` plus
`history.pushState`/`popstate`, no router/state/validation dependency beyond ticket-named TanStack Query;
canonical lowercase codec keys incl. `genreIds` and `pageSize`; `genres` accepted as bookmark alias into
`genreIds`, canonicalized via `replaceState`; sort names Title/Year/Rating/Runtime and directions
Ascending/Descending with ticket `Asc`/`Desc` accepted as aliases, canonical names emitted; URL is the
source of truth for all supported filters and page; UI edits reset page to 1 except page changes; push user
changes, replace normalization, restore bookmarks/back/forward without another push; `pageSize` is URL
state with a fixed UI value, no size selector. Rule on alias policy, pageSize/defaults, malformed/unknown keys.

**Ruling: APPROVED, with the three sub-rulings below.**

- Mechanism (charter §2.3 item 1): no router, state or validation library is added; ticket DEV-321:29 names
  only TanStack Query. A small hand-owned URL adapter with explicit Vitest tests is the cheaper and
  sufficient choice for one view; URLSearchParams/History are platform APIs, not dependencies.
- Vocabulary: canonical keys and value names are exactly the server codec's (`MovieQueryString.cs:12-25`,
  recon-DEV-321:33; sort/direction names recon:67), so a canonical SPA URL is also a valid server query.
- **Alias policy:** read-only aliases, exactly two families, both from the ticket example (DEV-321:28):
  `genres` → `genreIds`, and direction `Asc`/`Desc` → `Ascending`/`Descending`. Aliases are accepted on
  read, never emitted, and rewritten with `replaceState`. If both `genres` and `genreIds` are present,
  `genreIds` wins. No other aliases (basis: AC DEV-321:33, a ticket-example bookmark must set the filters;
  nothing broader is asked).
- **pageSize/defaults:** the canonical URL emits `sort`, `direction`, `page`, `pageSize` always, mirroring
  codec `Format` (recon:33). Default values are the Core `MovieQuery`/`Page` defaults; the spec cites their
  exact values (*needs recon* if not already in recon). A `pageSize` arriving in the URL is honored as given;
  the UI offers no selector.
- **Malformed/unknown keys:** unknown keys are dropped on canonicalization. A value that cannot be parsed
  for its key's type (non-integer number, unknown sort/direction/status name, non-boolean flag) is dropped
  to that key's default and the URL is canonicalized with `replaceState`; a bad URL never renders an error
  page by itself. Range and cross-field rules (e.g. min>max, page bounds) are **not** duplicated in the SPA:
  `MovieQueryValidator` stays the single authority, and its 400 surfaces through the ticket's error
  boundary (DEV-321:29). Basis: one validation owner, contract chain.
- Taste items (page reset to 1, push vs replace semantics) logged `[assumed]` in ASSUMPTIONS.md.

## Q3 (Keel, Phase A grill 3/12) — SPA initialization defaults; Q2 default clarification

**Question.** recon-DEV-321:79-117 shows Core construction defaults are null sort/direction and Page 0/0,
which the codec cannot format (RequiredName throws, recon:103) or parse (sort/direction/page/pageSize
required, recon:108). Set usable SPA initialization defaults and reconcile Q2? Keel recommends Title,
Ascending, page 1, pageSize 24; null text/watchlist/ranges and empty lists, as deliberate SPA product
defaults, not server-default claims; Q2 always-emitted keys and server range authority preserved. Incomplete
bound groups: absent bounds materialize as empty (unbounded), absent runtimeIncludeUnknown as false when a
runtime group exists; malformed/repeated scalar drops that scalar/group to its SPA default; list values repeat
in the canonical URL; enum names exact-case; booleans canonical true/false.

**Factual clarification of Q2.** Q2's "defaults are the Core MovieQuery/Page defaults" is superseded: there
is no usable Core browse default (recon:82-96, 117). Defaults are SPA product defaults set here.

**Ruling: APPROVED, with one condition.**

- Defaults: `sort=Title`, `direction=Ascending`, `page=1`, `pageSize=24`; `text`, `inWatchlist`, runtime
  group and year group absent; `genreIds`/`actorIds`/`statuses` empty. These are the SPA's product defaults
  (taste, logged `[assumed]`). Basis: AC DEV-321:33 and the codec requirement that the four keys are always
  present (recon:103,108); a ruling under charter §2.3 that changes nothing the ticket delivers.
- **Condition (needs recon, non-blocking for the grill, blocking for gate1):** the spec must cite the
  `MovieQueryValidator` page-number base and pageSize bounds (not in recon). If page is 0-based or 24 is out of
  range, the spec uses the first valid page and the largest permitted size not above 24, without re-asking.
- Incomplete groups: approved. The SPA reads leniently (absent bound = unbounded; absent
  `runtimeIncludeUnknown` = false when any runtime key is present) and always emits complete groups, so its
  canonical URL satisfies codec completeness (recon:113).
- Malformed/repeated: approved; a malformed or repeated scalar falls back to its SPA default, and for a
  range key the whole group falls back (absent). Lists accumulate and are emitted as repeated keys, matching
  codec Format/Parse (recon:100,110). Consistent with Q2's malformed-key rule.
- Case: enum names (sort, direction, statuses) exact-case per codec (recon:114); the only exceptions are
  the Q2 read-only aliases `Asc`/`Desc`. Booleans are read case-insensitively (as `bool.TryParse`, recon:111)
  and emitted lowercase `true`/`false` (recon:101).

## Q4 (Keel, Phase A grill 4/12) — poster runtime-population boundary

**Question.** recon-DEV-321:70-75 verifies nullable `poster_url` storage/mapping, but no populated
shipped-stack write path is established. Keel recommends: DEV-321 displays every supplied `posterUrl`;
null or broken image gets an accessible, reserved-aspect-ratio placeholder; populated and null/broken paths
are proven with a seeded real-DB browse test and RTL fixtures; no claim that live enrichment produces posters;
no enrichment/worker repair in this ticket. Decide follow-up (Rigger) vs noted-no-ticket.

**Q3 condition closed.** recon:119-124 shows `Page.Number >= 1` (1-based) and `1 <= Page.Size <= 100`, so
page 1 / pageSize 24 are valid as ruled.

**Ruling: APPROVED; out-of-scope follow-up ticket required.**

- Scope: DEV-321 delivers the grid showing the poster it receives (DEV-321:27) through the Q1 nullable
  `posterUrl` passthrough. Making enrichment persist posters is not in the ticket's Scope & Technical Design,
  so it is not added; nothing the ticket delivers is dropped or changed, so no `blocked: structural`.
- Proof: seeded real-DB browse test with one populated and one null `poster_url`, plus RTL fixtures for
  populated, null and image-load-error tiles. The spec must not claim live enrichment produces posters.
- Placeholder: same reserved aspect ratio as a poster (no layout shift, AC DEV-321:34 "renders smoothly"),
  with the movie title as accessible text; taste details logged `[assumed]`.
- Follow-up: **ticket, not noted.** recon:75 shows no shipped Infrastructure code writes `PosterUrl` and the
  only assignment sits in an orphaned project outside LamuFlix.sln, so posters cannot appear in the shipped
  stack: broken behaviour under the charter follow-up rule. Handed to Rigger: check open tickets and fold in
  if one exists; otherwise create under parent DEV-286, Type Bug, estimate 1d, `size:M`.

## Q5 (Keel, Phase A grill 5/12) — composition and dependency timing

**Question.** Approve composition and dependency timing? Keel recommends: replace the readiness `App.tsx` with
the main Library view at the current SPA entry; `main.tsx` installs a single `QueryClientProvider`;
`index.css` owns responsive styling; existing `App.test.tsx` becomes Library integration tests (charter §2.3
item 6 explicit ruling). TanStack Query is ticket-approved (DEV-321:29); no other runtime dependencies. Phase
A can finish without DEV-320, but Phase B waits for the user merge of #124 and a Wisp current-head recon of the
generated types/client, then consumes that client and generated request/response types. C# is extended first,
OpenAPI and types are regenerated with the DEV-320 pipeline, generated files are never hand-edited, DTOs are
never duplicated; lowercase URL keys map to PascalCase API parameters in an adapter typed by the generated
browse query parameters. No copying unmerged branch code; DEV-320 generator/client dependencies are inherited.

**Ruling: APPROVED as recommended.**

- §2.3 item 6: replacing `App.tsx` (recon:38) and its readiness `App.test.tsx` is a forced edit. The ticket
  Overview (DEV-321:24) makes the browse view "the main" view, and the readiness test is orphaned by that
  change, so it is replaced in the same PR (charter dead-code rule).
- Dependencies (§2.3 item 1): `@tanstack/react-query` only, ticket-named (DEV-321:29). The ticket's error
  boundaries are hand-written React error boundaries; no `react-error-boundary` or other runtime package.
  Generator/client packages arrive with DEV-320 and are not re-added here.
- Contract chain (charter): C# DTO → regenerated `openapi.json` → generated `types.ts`, never hand-edited;
  the URL-to-API adapter is typed by the generated browse parameters (recon:35). Phase B is blocked on the
  user's merge of #124 plus Wisp recon of the merged client; Conductor records it as a code prerequisite,
  not a scope change. If #124 merges with a client shape that conflicts with this ruling, re-ask Patron.
- Taste (`index.css` owns styling; single provider in `main.tsx`) logged `[assumed]`.

## Q6 (Keel, Phase A grill 6/12) — fetching, errors and history ownership

**Question.** Keel recommends: one browse query keyed by the complete normalized filter/page state, via the
existing generated client, cancellation propagated, no per-tile fetch or prefetch. Initial load and page change
show reserved-size skeletons; background refetch keeps current-query content with a loading announcement and
never shows previous-page data as current. A hand-written Library error boundary catches render and query
failures with a retry action; it resets on normalized query change; retry resets the boundary and refetches the
same URL; automatic retries disabled for this ticket (400 never retried; other failures show a recoverable
error, no retry loop). The URL adapter owns the initial bookmark read and a `popstate` listener with cleanup;
`pushState` updates adapter state immediately; `popstate` restores all filters/page and the fetch key without
pushing. App composes `QueryClientProvider` and the boundary; the Library view owns controls, grid and
pagination. Approve/amend stale-result and reset semantics.

**Ruling: APPROVED as recommended.**

- Stale results: any change of the normalized key (filters or page) shows skeletons, never the previous key's
  data (no `keepPreviousData`/placeholder data). Same-key background refetch keeps current content, with a
  polite live-region announcement. Basis: DEV-321:29 (loading skeletons) and AC DEV-321:34 (smooth, no layout
  shift: skeletons reserve tile size).
- Errors/reset: approved. Boundary resets when the normalized key changes and on Retry, which refetches the
  same canonical URL without a history entry. Retries off is a ticket-local taste (logged `[assumed]`); a 400
  is shown as an invalid-filter error, consistent with Q2's server-owned range validation.
- History: approved; the adapter is the only owner of `pushState`/`replaceState`/`popstate`, with listener
  cleanup tested. Basis DEV-321:28 and recon:38 (no existing router or owner).
- Cancellation: the query passes its abort signal to the generated client. Whether the DEV-320 client accepts
  a signal is part of the Q5 Phase B recon; if it does not, re-ask Patron rather than hand-rolling a client.
- No new dependency (§2.3 item 1).

## Q7 (Keel, Phase A grill 7/12) — Library UI behaviour and filter-control surface

**Question.** Freeze the UI: browse at the SPA root; responsive CSS poster grid with reserved 2:3 media slots;
title, year, runtime minutes, IMDb one-decimal badge and status chip with exact enum labels; em dash for a
missing scalar; no detail/play/watchlist-mutation controls. Text search submits on Enter or Search, not per
keystroke; sort and direction selects; statuses multiselect; watchlist tri-state; optional year/runtime bounds
plus include-unknown; genreIds/actorIds as repeated numeric-ID controls (no new facet endpoints, no unverified
facet API). Every supported query value restores into its control; filters apply together with page reset.
Previous/Next pagination with page and result count from totalCount/pageSize, disabled past known bounds;
empty-library and filtered-empty states with clear-filters; a valid out-of-range page stays in the URL and
shows an empty state with a return-to-first-page action, no silent history rewrite. Labelled, keyboard
accessible, focus-visible, polite announcements; no measured performance target; visual layout checked by
Palette later.

**Ruling: APPROVED as recommended.**

- Grid and tile fields: DEV-321:27 (poster, title, year, runtime, IMDb badge, EnrichmentStatus chip with the
  four exact labels); 2:3 reserved slot and skeleton parity serve AC DEV-321:34. Formatting (em dash, `min`
  suffix, one decimal) logged `[assumed]`.
- No play, detail or mutation controls: nothing in DEV-321:26-30 asks for them, and play would touch
  `Features:LocalPlay` (§2.3 item 5), which this ticket must not.
- Filter controls: every codec key (recon:33) gets a control so AC DEV-321:33 holds for every supported
  value. Genre/actor as numeric-ID entry is accepted because a named picker would need a facet endpoint, a
  public API shape not in this ticket (§2.3 item 4); a better picker is future work, `noted, no ticket`.
- Pagination and out-of-range page: approved. Total pages = ceil(totalCount/pageSize); out-of-range is
  shown, not rewritten, consistent with Q2 (server owns range rules; the URL is source of truth).
- Accessibility and announcements: approved; consistent with Q6. No numeric performance target: the AC
  says "smoothly" and names no metric.
