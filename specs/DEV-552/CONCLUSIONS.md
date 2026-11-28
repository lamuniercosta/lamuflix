# DEV-552 - Patron rulings

Decision log for DEV-552 (grill with Keel, Phase 1).

## Q1 - Progressive per-provider results (§2.3 item 4, public API shape)

**Ruling: approved.** An opt-in incremental response on the existing
`GET /api/movies/{id}/subtitles/search?language={code}` operation, emitting one
complete provider outcome plus its candidates as each provider settles.

- Basis: Wanted 1 requires results to fill in as each provider answers; DEV-551 returns one
  synchronous aggregate after the slowest provider (recon-DEV-552:47, ADR 0052:7,11), which is
  the gap the ticket's Where-it-lands row permits closing in `SubtitleEndpoints.cs`
  (Only if DEV-551 leaves a gap).
- Conditions: opt-in by `Accept: text/event-stream` on the same operation, no new route; built-in
  ASP.NET Core 10 SSE result, no new NuGet/npm dependency (§2.3 item 1); the default JSON
  response and its status codes stay unchanged for existing clients; the download route is
  untouched; server fan-out stays the only provider search per language (no provider query
  parameter, matching DEV-551 contracts/search-response.md:5-9).
- Contract chain: the per-provider event payload is a named C# DTO that appears in
  `web/src/api/openapi.json` and is consumed from generated `web/src/api/types.ts`; if the
  OpenAPI generator does not emit the stream item schema, register it explicitly rather than
  hand-roll a TS type. Unverified: OpenAPI emission of the SSE item schema - plan must confirm.
  Record the decision in ADR 0053.
