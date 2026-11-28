# Research: DEV-553 distinct downloaded subtitle track labels

**Authority**: `brief.md` Q1–Q10; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri `recon-DEV-553`; `specs/PRODUCT.md`. No inventable unknowns remain after grill closure.

## Decision log

### Persistence store

- **Decision**: One ticket-owned PostgreSQL table; unique key = movie identity + canonical discovery filename representation of the relative published path (including `Subs\` when present); atomic upsert after successful publication, including null match percent when the score is unknown. Concurrent same-key upserts from separate contexts both complete with one complete committed row.
- **Rationale**: Ticket Open points default; Wanted 1 field set; discovery stays authoritative for eligibility (Q1, Q7; Phase 2 step 6 refinements).
- **Alternatives considered**: Filename-embedded provenance (rejected — frozen recognition policy); sidecar-store metadata (rejected — `ISubtitleSidecarStore` is bytes/publication only, `recon-DEV-553:74–82`, key evidence `:78–82`); insert-only without upsert (superseded by Q7 path reuse).

### Metadata write failure

- **Decision**: Keep published file; existing success response with filename label and null provider/release; Error log with movie id + relative path; no delete/rollback/retry (Q2, Q9).
- **Rationale**: Matches Wanted 2 fallback; rollback would be out-of-scope deletion.
- **Alternatives considered**: Roll back published bytes; new error status; fingerprint cleanup — refused under §2.3 / out of scope.

### Public contract

- **Decision**: Nullable `provider` and `releaseName` on existing track DTO for list and download; preserve discovery-derived wire `label`; project the two new fields independently; backend ranks; web groups and preserves order (Q3).
- **Rationale**: Ticket Wanted 2 / Where it lands; Q3 preserves existing `label`; hash/score/timestamp stay internal.
- **Alternatives considered**: Exposing ranking fields on the wire — refused as unnamed public shape; rewriting wire `label` to the composed display form — refused (display composition belongs to the selector).

### Ranking and labels

- **Decision**: Recorded-first within discovery-derived language; hash match → known match percent desc → download UTC desc; missing score after known at same hash rank; discovery order for ties and unrecorded; selector composes display `release name · provider` only when both wire values are present, otherwise uses the discovery-derived wire label; Off outside groups; language groups keyed by wire `language` in first-appearance order; stored candidate language is provenance only (Q4; Phase 2 step 6; `ASSUMPTIONS.md`).
- **Rationale**: Wanted 3–4 plus Patron taste defaults; Q3 wire/display boundary; discovery-derived language is authoritative for partitions and the wire.
- **Alternatives considered**: Client-side ranking; half display labels; alphabetical language sort; composing the enriched string into wire `label`; overriding wire language with stored candidate language.

### Selection after download

- **Decision**: Keep auto-select of downloaded track; preserve by server track id across reload; newer in-flight choice/Off wins via existing guards (Q5).
- **Rationale**: Wanted 5 means selection survives reload, not a change to auto-select.
- **Alternatives considered**: Revert to pre-download selection — rejected by Patron correction.

### Port and governance

- **Decision**: Core port `ISubtitleDownloadRecordStore` (post-publication write + per-movie batch read); one EF adapter + Persistence DI binding with table/EF types named by Persistence conventions; ADR 0054 Accepted + Principle I MINOR (next MINOR from then-current base) + ArchitectureTests whitelist atomic with the port. Spec Kit PR excludes the ADR file; proposed draft parked in `PRIVATE_TICKET_DIR` (Q6; Phase 2 step 6).
- **Rationale**: No existing port fits (`recon-DEV-553:74–82`, key evidence `:78–82`); constitution closed port list requires ADR route — compliance, not departure.
- **Alternatives considered**: Reuse `ISubtitleSidecarStore` / catalog / movie repository ports — unsuitable contracts; ADR locking table/EF type names — refused (Ledger 1; Build owns Persistence naming).

### Dependency order

- **Decision**: Spec now; implement after Gate 1 and DEV-551 delivery merge + pickup evidence check; DEV-552 parallel OK (Q8).
- **Rationale**: Ticket Needs; no hand-rolled provider ID.
- **Alternatives considered**: Blocking the Spec Kit PR on DEV-551 — refused; inventing provider IDs — refused.

## Facts from recon (not decisions)

- Download path: `DownloadSubtitleCommandHandler` publishes via `ISubtitleSidecarStore`, builds track from discovery label today.
- List path: `ListSubtitleTracksQueryHandler` maps discovery into `SubtitleTrack` without provenance.
- Wire: `SubtitleTrackDto(Id, Label, Language, IsDefault)` mapped in `SubtitleEndpoints` for list and download.
- Web: `SubtitleSelector` flat radio list; `useSubtitlePlayback.refreshAfterDownload` auto-selects downloaded track then merges reloaded list.
- Frozen discovery coverage: `SubtitleSidecarDiscoveryTests` must remain unedited.
