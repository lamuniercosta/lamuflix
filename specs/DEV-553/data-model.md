# Data model: DEV-553 subtitle download provenance

**Authority**: `brief.md` Q1, Q4, Q7, Q9 and Phase 2 step 6 refinements; `CONCLUSIONS.md` Q1/Q7/Q9 and plan-challenge dispositions; `spec.md` Key Entities / FR-001–FR-008.

## Subtitle download record (persisted)

One ticket-owned PostgreSQL table. EF mapping and adapter live under `src/LamuFlix.Infrastructure/Persistence/`. ADR 0054 locks only the Core port name `ISubtitleDownloadRecordStore`; Build names the table, entity, configuration, and adapter by existing Persistence conventions. This document fixes the fields and rules only.

| Field | Rules |
|---|---|
| Movie identity | Required; existing movie identity used by subtitle handlers. Part of unique key. |
| Relative published path | Required; the canonical discovery filename representation of the path relative to the movie folder as actually published, including `Subs\` when present (for example `Subs\Film-en.srt` vs adjacent `Film-en.srt`). Part of unique key. Exact string match for joins and for the existing track-token / download-response ID input. At the publication boundary, map path separators to the discovery convention while preserving path segments, casing, and Unicode exactly. Adjacent and `Subs\` paths remain distinct. Physical file names and existing discovery/ID semantics are unchanged. |
| Provider ID | Canonical DEV-551 provider ID string when known; required on successful recorded download. |
| Release name | Release name from the selected candidate when known. |
| Language | Candidate language recorded as provenance (Wanted 1). Discovery-derived language remains authoritative for Core partitions and the wire `language`; stored candidate language MUST NOT override them. |
| Hash-match evidence | Boolean (or equivalent) evidence that the selected candidate was a moviehash match; required for Wanted 4 ranking. |
| Match percent | Integer score when known; null when unknown. A successful publication with unknown score still writes the row with a null score. Ranking places missing scores after known scores at the same hash rank (Q4). |
| Downloaded at UTC | `TimeProvider.GetUtcNow()` at successful persistence time (Q1). |
| Uniqueness | Unique composite on (movie identity, canonical discovery relative published path). |
| Write timing | Only after successful publication. Failed publish writes nothing (Q1). |
| Same-key reuse | Atomic upsert replaces prior metadata when a later successful download publishes under the same key after external deletion (Q7). Concurrent same-key upserts from separate contexts must both complete with one complete committed row from one writer. |
| Failed / ambiguous upsert | Published file remains; immediate response uses discovery-derived label and null provenance either way (Q2). Per Q9: later list shows the old row if the replacement write did not commit, or the new row if it committed. Immediate fallback and later committed-row list are separate. |
| Cleanup | No sweep deleting orphan rows when files are missing; discovery exclusion is sufficient (Q1, Q7). Deleting tracks is out of scope. |

## Local subtitle track (derived)

Built by discovery first, then optional join to the download record:

| Aspect | Rules |
|---|---|
| Eligibility | `SubtitleSidecarDiscovery` only; recognition policy and file names unchanged. |
| Identity | Existing server track id (`SubtitleTrackToken` / DTO `id`), derived from the same canonical discovery relative-path key used for persistence and join. |
| Join key | Canonical discovery relative path under the movie (exact string match). |
| Enrichment | When a matching committed row exists, supply provider, release name, and ranking evidence. Preserve the discovery-derived label and discovery-derived language on the track entity and wire. Project provider and release independently — one known field is not cleared when the other is missing. |
| Language authority | Discovery-derived language drives Core language partitions and wire `language`. Stored candidate language is provenance only; a conflicting stored value does not change the wire language or selector grouping key. |
| Fallback | No row → today’s discovery-derived label; null provider/release on the wire. |
| Display composition | Selector-only: `release name · provider` when both values are known; otherwise wire label (including partial provenance). |
| Missing file | Discovered set omits the path → track not listed even if a row remains. |

## Ranking projection (derived, Core)

Partition discovered tracks by discovery-derived language in first-appearance order. Within a language (FR-010):

1. Recorded tracks before unrecorded tracks.
2. Among recorded: hash match first.
3. Then known match percent descending; missing scores after known scores at the same hash rank.
4. Then download UTC descending.
5. Exact ties: discovery order.
6. Unrecorded tracks: discovery order after all recorded peers.

Ranking evidence is not public wire state.

## Lifecycle

| Event | Record / track effect |
|---|---|
| Publication failure | No write. |
| Publication success + upsert success | Row inserted or replaced for that movie/path key, including a null match percent when the score is unknown. Download and list return nullable provider/release when known and keep the discovery-derived label and language. |
| Publication success + upsert failure/ambiguous/cancelled | File kept; immediate response uses discovery-derived label and null provenance either way; Error log; no delete/rollback/retry. Later list: old row if replacement did not commit, new row if it committed (Q9). |
| External delete of file | Track disappears from list via discovery; row may remain until same-key re-download replaces it. |
| Successful re-download on reused path | Upsert replaces metadata for the new publication. |
| Manual same-path file replacement without download | Prior metadata may still join until a successful download updates the key (path-based limit; noted, no ticket). |
