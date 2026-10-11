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
