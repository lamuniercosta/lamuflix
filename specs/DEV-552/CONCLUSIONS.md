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
# DEV-552 grill conclusions

## Q1 — Incremental provider search (§2.3 API shape)

**Keel.** How should DEV-552 deliver Wanted 1, results filling in as each provider answers? recon-DEV-552:45-50 pins DEV-551 to one synchronous aggregate per language, returning after the slowest provider. I recommend approving an opt-in incremental search response on the existing GET operation, with one complete provider outcome and its candidates emitted as that provider settles, while preserving the current JSON response for existing clients and the single-candidate download route. Existing fan-out remains the only provider search per language. This adds transport and contract tests but preserves actual per-provider progression. Please rule on that direction, or state the alternative with cited basis. Confirm specs/DEV-552/CONCLUSIONS.md as the decision log per the Conductor ask.

**Patron.** APPROVED - opt-in incremental response on the existing GET search operation via Accept: text/event-stream, one complete provider outcome plus candidates per settled provider; built-in ASP.NET Core 10 SSE, no new dependency, no new route, default JSON response and status codes unchanged, download route untouched, server fan-out stays the only provider search (no provider query param). Event payload must be a named C# DTO in openapi.json consumed via generated types.ts (register explicitly if not emitted; plan must confirm emission). Record in ADR 0053. Decision log confirmed: specs/DEV-552/CONCLUSIONS.md (committed on feature/552-spec).

Basis is DEV-552 Wanted 1 and Where it lands, PRODUCT §5, and recon-DEV-552:45-50. Existing callers keep their JSON contract. The picker can render the first settled provider before the slowest provider returns. Verify the actual wire behavior and generated event type.

## Q2 - Preferred-language list and hash-match flag (§2.3 item 4)

**Patron.** APPROVED with one amendment.

- `preferredLanguageCodes` (ordered, canonical supported codes) added to the existing languages response; basis Wanted 1 and 9, recon-DEV-552:53-56 (the catalog orders preferred first but does not mark which are preferred). Amendment: an empty list falls back to today's behaviour, one automatic search of `defaultLanguage` when it is non-null, otherwise none; Add a language stays available. Sound practice: no regression for an owner with no preferences configured.
- `hashMatch` (non-null boolean) added to the candidate DTO and the stream event candidate; true only on affirmative provider evidence, never inferred from match percent; basis Wanted 3, Out of scope (no scorer change). Surfacing it through the `ISubtitleProvider` result is in scope and is not a scorer change.
- Unverified: whether the OpenSubtitles search sends a movie hash today; if it does not, the plan states that and the mark is false until it does (no new hashing work without a further ruling).
