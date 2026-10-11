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
