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

---

## Q2 — Preferred languages and hash-match evidence (§2.3 API shape)

**Keel.** Which additive metadata should drive the initial searches and hash-match mark? I recommend an ordered preferredLanguageCodes array on the existing languages response, using canonical supported codes, with an empty list triggering no automatic search and leaving Add a language available. Add a boolean hashMatch to the existing candidate DTO and event candidate using actual provider evidence, with no mark without affirmative evidence and no inference from score. This gives the picker explicit data without new routes or a scorer change. Basis: Wanted 1, 3 and 9; recon-DEV-552:50,53-56 shows both contract gaps. Please rule on this additive metadata contract.

**Patron.** APPROVED with one amendment. preferredLanguageCodes (ordered canonical codes) on the existing languages response; hashMatch non-null boolean on candidate DTO and stream event candidate, true only on affirmative provider evidence, never inferred from score; surfacing it via the ISubtitleProvider result is in scope, not a scorer change. Amendment: empty preferred list falls back to today's behaviour, one automatic search of defaultLanguage when non-null, else none; Add a language stays. Unverified: whether OpenSubtitles search sends a movie hash today; plan must state it, mark stays false if not, no new hashing work without a further ruling. Logged in specs/DEV-552/CONCLUSIONS.md (committed).

Basis is DEV-552 Wanted 1, 3 and 9, PRODUCT §5 and recon-DEV-552:50,53-56. A catalog can now distinguish configured membership from display order. OpenSubtitles hash evidence remains unverified in the supplied evidence and is a named verification item.

## Q3 - Provider line and quota across overlapping language requests (taste)

**Patron.** CONFIRMED as Keel recommended; basis Wanted 4-7, recon-DEV-552:48-50. Logged `[assumed]` in ASSUMPTIONS.md.

---

## Q3 — Provider summaries and quota

**Keel.** How should the picker handle provider quota and summaries when several language requests overlap? I recommend one line per configured provider, retaining outcomes per language so that failures and their safe reasons stay visible alongside any successes. Count selected rows per metered provider, display the last observed quota (download observations supersede in-flight search observations), and leave unknown quota explicitly unknown. Permit a selection larger than observed quota and submit every selected row sequentially; the route decides each attempt. Basis: Wanted 4–7 and recon-DEV-552:48-50. This keeps partial failures visible and avoids treating a quota observation as a reservation. Please confirm or amend.

**Patron.** CONFIRMED as recommended (Wanted 4-7, recon-DEV-552:48-50); taste, logged [assumed] in specs/DEV-552/ASSUMPTIONS.md and noted in CONCLUSIONS.md (committed).

Provider outcomes are tracked per language. The visible provider summary preserves safe failed-language reasons, and selected spend and observed remaining quota are per provider. The download route remains authoritative for each attempt.

## Q4 - Stream completion and errors (§2.3 item 4, status codes and stream shape)

**Patron.** APPROVED with one amendment.

- As recommended: DEV-551 request validation and no-usable-provider (409) errors stay JSON and are returned before any stream opens (contracts/search-response.md:81-93,104-110 per recon-DEV-552:49); one event per configured provider, off and unusable rows included; server deadlines and safe closed outcome codes kept; no automatic retry; stale searches cancelled on media change and late events for a superseded query ignored. Basis: Wanted 1 and 5, Q1.
- Amendment: the stream ends with an explicit terminal completion event carrying the settled provider count. The client treats a close without it as incomplete for that language, so a normal end is never confused with a dropped connection. The client knows the provider total only from the server.
- The server honours client disconnect (request abort cancels outstanding provider calls). Contract tests cover the normal end, the interrupted end and the pre-stream JSON errors.

## Q5 - Batch snapshot, order and finish

**Patron.** CONFIRMED as Keel recommended; basis Wanted 2, 4, 7-9, Done when, Open points defaults (one call per download, one at a time, preferred-then-added group order). Lifecycle details logged `[assumed]` in ASSUMPTIONS.md.

## Q6 - Authorized file scope beyond the named picker files (§2.3 item 6)

**Patron.** CONFIRMED as Keel recommended.

- Basis: these edits are forced by the approved Q1-Q4 contract and the Done when tests, so they are in scope under §2.3 item 6 (forced edit); Where it lands names `SubtitleEndpoints.cs` and `CONTEXT.md`.
- `web/src/api/openapi.json` and `web/src/api/types.ts` change only by regeneration, never by hand (contract chain).
- Hard limits stand: no dependency, project, layer, schema, scorer, credential or `Features:LocalPlay` change; the hash mark stays false without verified provider evidence (Q2); a DEV-553 track-flow gap pauses for a scoped ruling.

## Q7 - Closing bar and frozen scope

**Patron.** CONFIRMED as Keel recommended: 2 plan-review and 2 code-remediation rounds maximum, then open blockers go to Bernstein; Critical/High/Medium block, as do verified acceptance, security or contract-compatibility failures at any severity; other findings dispositioned explicitly; frozen scope is Q1-Q6 behaviour within ticket exclusions and Q6 file boundaries, anything else is a follow-up. Basis: Done when, Out of scope, Q1-Q6, task-pipeline Phase 3-5. Grill closed from Patron's side at 7 questions.

## Q8 - downloadAvailability on the providerSettled stream event (§2.3 item 4, Compass M1)

**Patron.** APPROVED as Keel recommended.

- Basis: recon-DEV-552:12-13 shows both baselines return `downloadAvailability` (`SubtitleDownloadAvailabilityDto(bool Enabled, string Reason)`, `SubtitleSearchResponse.cs:13-18`) on every successful search and the picker depends on it to show candidates read-only, disable Get and issue zero download POSTs (`subtitlesTab.test.tsx:420-443`); the approved SSE representation (Q1, Q4) must not regress that, and the first visible rows need it, so `searchCompleted` is too late.
- Shape: reuse the existing `SubtitleDownloadAvailabilityDto` (no new DTO) as a required, non-null `downloadAvailability` on `SubtitleProviderSettledEventDto`; evaluated once when the stream opens and repeated unchanged on every `providerSettled` event of that stream; no second search, no new event type, default JSON and download route unchanged (Q1, Q6). Generated through OpenAPI into `types.ts` (contract chain).
- Tests: stream contract test asserts the same snapshot on every event in read-only and enabled modes; picker test asserts read-only rows from the first streamed event are visible with Get disabled and zero POSTs. The download route remains the authority on each attempt.

## Gate 1 - provisional (Patron @ e9175fcb)

**Ruling: gate1: provisional.** No owner checkbox; no follow-up ticket (noted, no ticket).

- Basis: /speckit-analyze CLEAN and all 15 plan-challenge findings dispositioned (DEV-552 note:35); requirements.md User-Visible Outcome ticked with SMOKE-001; no ticket contradiction or constitution departure (spec.md Owner question); §2.3 care items ruled Q1-Q8, plan adds no dependency, layer, schema, LocalPlay or Process.Start (plan.md:19,21,49,125).
- Conditions: T001 moves ADR 0053 to Accepted; T002 pickup stops on needs recon or a scoped ruling if the landed DEV-551 contract or DEV-553 track refresh leaves a Done-when gap (plan.md:70, Q6); public hashMatch true only on explicit moviehash_match evidence, scoring fallback unchanged (plan.md:73, Q2); openapi.json/types.ts by regeneration only (Q6).
