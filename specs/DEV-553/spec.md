# Feature Specification: Distinct downloaded subtitle track labels

**Feature Branch**: `feature/553-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-553 (size M); grill-closed `brief.md` (Q1–Q10); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-553`, `recon-DEV-553`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope; Q6; Q10; `PRODUCT.md` §5). Ordinary owner Gate 1 remains **pending** until the user merges the Spec Kit PR. Sequence: plan challenge → Keel freeze receipt → Patron provisional / Spec PR → owner merge completes Gate 1. Implementation waits for Gate 1 and for DEV-551 delivery merge plus pickup confirmation of provider ID and selected-candidate evidence (`brief.md` Q8).

**Spec PR note (not a checkbox)**: Q6 requires ADR 0054 (`ISubtitleDownloadRecordStore`), a constitution Principle I MINOR amendment, and an ArchitectureTests allowed-port update, delivered atomically with the Core port in the Build PR. Spec Kit PR stays `specs/**` only and explicitly excludes the ADR file. The proposed ADR draft is parked in `PRIVATE_TICKET_DIR` as `0054-subtitle-download-record-persistence-port.md` for Build pickup; brief and conclusions remain the durable fallback if the draft must be recreated. ADR 0054 locks only the Core port name; Build names EF/table types by existing Persistence conventions.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Persistence identity and upsert, metadata-write failure, public DTO fields, ranking and label fallbacks, selection-after-download, Core port plus governance, DEV-551 dependency and proof order, same-key failure limit, frozen scope and closing bar are settled in `brief.md` Q1–Q10 and `CONCLUSIONS.md`. Provider-ID display, Off placement, language-group order and stable tie-breaks are Patron-ratified `[assumed]` taste in `ASSUMPTIONS.md`. Conductor asked Quill to run specify/plan/tasks in one ask without inventing decisions. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: Browser player subtitle selector (`SubtitleSelector` in the playback subtitles panel).
- **What the user sees or does**: Downloaded tracks appear under a heading per language. When both provider and release name are known, the selector shows `release name · provider` (canonical provider ID as supplied); otherwise it shows the discovery-derived filename label from the wire. Hand-added or otherwise unrecorded tracks keep today’s filename-derived label. Tracks inside a language follow hash-match, then match percent, then newest download. One Off option sits outside the headed groups. After a download, the newly downloaded track stays selected through the list reload even when labels or order change; a newer manual pick or Off made while refresh is in flight wins.
- **What it needs to show up**: Default Compose stack with subtitle download available, at least one movie with searchable subtitle candidates, and (for composed display labels) completed downloads whose provenance rows exist. Hand-added sidecar files continue to appear without a provenance row.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a library movie with two English subtitle downloads from two providers that recorded provenance, **When** the viewer opens the player subtitle selector, **Then** an English heading appears with two tracks labeled `release name · provider`, each showing its own provider and release, and a single Off option outside the language groups.
- **SMOKE-002**: **Given** the same movie also has a hand-added sidecar with no provenance record (for example `Film.en.srt`), **When** the viewer opens the selector, **Then** that track keeps today’s filename-derived label under its language heading.
- **SMOKE-003**: **Given** the viewer starts a subtitle download from the player, **When** the download completes and the track list reloads, **Then** the newly downloaded track remains selected even if its label or position among same-language tracks changed.

## Scope

**In scope** (`brief.md` Frozen scope / Grill answers / Approach; Q1–Q10; ticket Wanted 1–6 and Done when at `recon-DEV-553:19–72`):

1. **Download provenance record**: After successful sidecar publication, upsert one ticket-owned PostgreSQL row keyed by movie identity plus the canonical discovery filename representation of the published path relative to the movie folder (including `Subs\` when present). At the publication boundary, map path separators to the convention already used by discovery while preserving path segments, casing, and Unicode exactly; adjacent and `Subs\` paths stay distinct. That same key is the persisted row key, the list join key, and the input to the existing track token / download response ID. Physical file names and existing discovery/ID semantics are unchanged. Store the selected candidate’s canonical provider ID, release name, candidate language (as provenance), hash-match evidence, match percent (nullable when unknown — a successful write still persists the row with a null score), and download UTC from injected `TimeProvider` (`brief.md` Q1, Q7; Phase 2 step 6 refinements).
2. **List and download contract**: Existing list and download responses keep routes, status codes, `id`, discovery-derived `label`, discovery-derived `language`, and `isDefault`. Add nullable string `provider` and `releaseName` with explicit null when unknown; project those two fields independently so one known field is not cleared when the other is missing. `provider` is the DEV-551 canonical provider ID verbatim. Backend ranks; ranking evidence stays internal (`brief.md` Q3–Q4).
3. **Discovery-first enrichment**: Discover eligible files first; batch-read provenance for the movie; join by the canonical discovery relative-path key; never list a row whose file is missing. Wire `label` stays the discovery-derived filename label for recorded and unrecorded files. Discovery-derived language is authoritative for Core language partitions and the wire `language` consumed by the selector; stored candidate language remains provenance only (`brief.md` Q1, Q3, Q4; Wanted 2; Phase 2 step 6 refinements).
4. **Player selector**: Group by the wire `language` field with a heading per language; order groups by first appearance in discovery order; preserve incoming order inside each language; show Off once outside groups; compose display `release name · provider` only when both wire values are present, otherwise use the wire `label` (`brief.md` Q4; `ASSUMPTIONS.md`).
5. **Selection after download**: Keep today’s auto-select of the newly downloaded track; preserve that selection across reload by server track id through reorder/relabel; existing attempt/generation guards so a newer viewer choice or Off wins; absent-track handling unchanged (`brief.md` Q5).
6. **Governance and glossary**: Core port `ISubtitleDownloadRecordStore` (post-publication write + per-movie batch read), one EF adapter under Infrastructure/Persistence (table/EF types named by Persistence conventions), DI in `PersistenceServiceCollectionExtensions.cs`, ADR 0054 Accepted + Principle I MINOR amendment + ArchitectureTests port-list update delivered atomically with the port; update `CONTEXT.md` Local Subtitle Track (`brief.md` Q6; Wanted 6).
7. **Verification**: Unit, integration, property, and web proofs named in Done when and Q8; existing discovery tests pass unedited; Phase 3 and rebased-head gates per brief Task order item 5.

**Out of scope**:

- Deleting a downloaded track.
- Renaming files on disk.
- Remembering the last track chosen per movie.
- External file identity detection beyond path-based matching (manual same-path replacement can retain old metadata until a successful re-download; noted, no ticket — Q7/Q9).
- Anything else is a follow-up issue, not a finding in this round. File follow-ups only for Critical, High, or broken behaviour; otherwise note without opening a ticket (`brief.md` Q10).

**Precedence**: Ticket Wanted / Done when / Where it lands / Open points (`recon-DEV-553:19–72`) are approved authority (`PRODUCT.md` §3). Persistence port facts at `recon-DEV-553:74–82` (key evidence `:78–82`). Brief Q1–Q10 and `CONCLUSIONS.md` govern plan boundaries. Quill invents no decisions. Spec Kit PR remains `specs/**` only; ordinary owner Gate 1 has no structural checkbox.

**Dependencies**: DEV-551 (provider id on each candidate; Spec PR #520 may still be open during this spec work). DEV-553 implementation waits for DEV-551 delivery merge and pickup confirmation of provider ID plus selected-candidate evidence for the record; missing evidence → stop and route `needs recon` (`brief.md` Q8). DEV-552 may proceed in parallel (ticket Needs).

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion beyond Wanted 6)*

**Subtitle download record**
Ticket-owned provenance row for a published local subtitle file: movie identity, canonical discovery relative published path, canonical provider ID, release name, candidate language (provenance), hash-match evidence, match percent (nullable when unknown), and download UTC. Enrichment only; discovery remains authoritative for file eligibility and for track language on the wire.
_Avoid_: embedding provider or release in the sidecar file name; listing a row whose file is missing; treating the record as the track identity; using stored candidate language to override discovery-derived wire language

**Enriched track label**
Selector-composed display string `release name · provider` used only when both wire `provider` and `releaseName` are known; otherwise the discovery-derived wire `label` (including hand-added files, partial provenance, and failed metadata writes). Wire `label` itself stays discovery-derived on list and download.
_Avoid_: rewriting wire `label` to the composed form; half display labels such as release-only or provider-only; nulling a known `provider` or `releaseName` because the other is missing; hand-rolled provider display-name maps in the web app

**Language group**
Selector section headed by the wire `language` field (discovery-derived), ordered by first appearance in the discovery-backed list; tracks inside the group keep the backend’s ranked order. One Off choice sits outside all groups.
_Avoid_: sorting languages alphabetically unless that matches first-appearance order; placing Off inside a language heading; grouping by stored candidate language when it conflicts with wire language

Consumed vocabulary (already in use; not redefined): Local Subtitle Track, Subtitle Match Percent, sidecar discovery / recognition policy, SubtitleTrack / SubtitleTrackDto, contract chain C# DTO → OpenAPI → TypeScript.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record and return download provenance (Priority: P1) 🎯 MVP

After a successful subtitle download, the system stores provenance for the published file and returns provider and release name on list and download when known, while preserving the discovery-derived wire label. Hand-added files and failed metadata writes keep today’s wire label with null provenance fields.

**Why this priority**: Without the record and wire fields, the player cannot show distinct downloaded options (Wanted 1–2; Done when integration and hand-added cases).

**Independent Test**: Endpoint coverage that two English downloads from two providers list as two tracks with distinct provider and release and preserved discovery-derived labels; a hand-added `Film.en.srt` keeps today’s label with JSON nulls; a record whose file was deleted is not listed while its row may remain; unit coverage that metadata write failure keeps the published file and returns success with null provider/release; Q9 later list reflects the last committed row when present, separately from the immediate fallback response.

**Acceptance Scenarios**:

1. **Given** two successful English downloads from two providers for the same movie, **When** the track list is requested, **Then** both tracks appear with their own provider and release name and discovery-derived labels.
2. **Given** a hand-added sidecar with no provenance row, **When** the track list is requested, **Then** that track uses today’s filename-derived label and null provider/release.
3. **Given** a provenance row whose file was removed from disk, **When** the track list is requested, **Then** that track is not listed.
4. **Given** publication succeeds but the provenance upsert fails, is cancelled, or is ambiguous, **When** the download response returns, **Then** the existing success shape is used with discovery-derived label and null provider/release, the file remains discoverable, no delete/rollback/retry occurs, and the failure is logged at Error with movie id and relative path; a later list shows the last committed row if present.

---

### User Story 2 - Rank tracks for selection (Priority: P1)

Within each language, recorded tracks are ordered ahead of unrecorded ones: hash match first, then known match percent descending, then download UTC descending; missing scores follow known scores at the same hash rank; exact ties and unrecorded tracks keep discovery order.

**Why this priority**: Wanted 4 and Done when require deterministic order so the selector can preserve incoming order without ranking on the client.

**Independent Test**: Unit and property tests for ranking precedence, missing scores, stable ties, and unrecorded-last behaviour without requiring the web selector.

**Acceptance Scenarios**:

1. **Given** multiple recorded tracks in one language with mixed hash-match and scores, **When** the list is built, **Then** order is hash match, then match percent descending, then newest download UTC.
2. **Given** a recorded track with unknown match percent and peers with known scores at the same hash rank, **When** the list is built, **Then** the missing score follows the known scores.
3. **Given** exact ranking ties or unrecorded tracks, **When** the list is built, **Then** discovery order is preserved among those peers, and unrecorded tracks follow all recorded tracks for that language.

---

### User Story 3 - Grouped selector labels (Priority: P1)

The player subtitle selector shows language headings, composed or fallback display labels, backend order inside each group, a selectable manual/hand-added track, and a single Off outside the groups.

**Why this priority**: Wanted 3 and web Done-when proofs are the visible owner value once provenance exists.

**Independent Test**: Web tests assert first-appearance language-group order, composed labels when both fields are known, partial-provenance fallback to the wire label, selectable manual track, single Off, and generated DTO usage after OpenAPI/TypeScript regeneration.

**Acceptance Scenarios**:

1. **Given** tracks in more than one language with mixed recorded, partial-provenance, and unrecorded entries, **When** the selector renders, **Then** each language has a heading in first-appearance order, tracks with both provider and release show `release name · provider`, tracks with missing or partial provenance keep the wire label, a manual track remains selectable, and Off appears once outside the groups.
2. **Given** the backend ranked list for a language, **When** the selector renders that group, **Then** it preserves the incoming order.

---

### User Story 4 - Selection survives download reload (Priority: P2)

After a download, the newly downloaded track stays selected through the reloaded list; a newer in-flight manual selection or Off wins over the stale refresh.

**Why this priority**: Wanted 5; depends on US1 contract fields and US3 selector but is independently testable in the playback hook.

**Independent Test**: Web tests that the downloaded track stays selected after reorder/relabel refresh, that a manual track or Off chosen during an in-flight refresh is kept, and that existing absent-track handling remains a preserved regression (FR-013).

**Acceptance Scenarios**:

1. **Given** a download refresh that reorders and changes the downloaded track’s display label, **When** the list reload completes, **Then** that track remains selected by server track id.
2. **Given** the viewer selects another track or Off while a download refresh is in flight, **When** the stale refresh completes, **Then** the newer viewer choice remains.
3. **Given** a selected track that is absent from the refreshed list, **When** the reload completes, **Then** existing absent-track handling is preserved unchanged.

---

### User Story 5 - Glossary reflects downloaded provenance (Priority: P3)

`CONTEXT.md` Local Subtitle Track states that a downloaded track carries its provider and release name.

**Why this priority**: Wanted 6; documentation closure after behaviour lands.

**Independent Test**: Diff review of the Local Subtitle Track entry against Wanted 6.

**Acceptance Scenarios**:

1. **Given** the feature is delivered, **When** Local Subtitle Track is read, **Then** it says a downloaded track carries its provider and release name without changing recognition-policy wording elsewhere.

---

### Edge Cases

- Metadata upsert fails or is ambiguous after successful publication (including same-key reuse): keep file; immediate success response with discovery-derived label and null provenance; Error log; no delete/rollback/retry. The immediate fallback response and a later list that shows the last committed row (if the replacement write did not commit) are separate behaviours (Q2, Q9).
- Same movie/relative-path key reused after external deletion: atomic upsert replaces prior metadata on successful write (Q7).
- Manual file replacement at the same path without a new download: prior metadata may remain until a successful re-download updates the key (path-based limit; noted, no ticket).
- Cancellation during metadata write: same as failed/ambiguous write — no rollback of published bytes, no retry, no duplicate publication.
- Partial provenance (only provider or only releaseName known): wire still returns the known field; selector does not compose and keeps the discovery-derived label (Q3, Q4).
- Recognition policy and sidecar file names remain unchanged; existing discovery tests pass with no edits.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST, after successful subtitle publication, upsert a download record keyed by movie identity and the canonical discovery filename representation of the relative published path (including `Subs\` when present). At the publication boundary the system MUST map path separators to the discovery convention while preserving path segments, casing, and Unicode exactly; adjacent and `Subs\` paths MUST remain distinct. That same key MUST be used for the persisted row, the list join, and the existing track-token / download-response ID input. Physical file names and existing discovery/ID semantics MUST remain unchanged.
- **FR-002**: System MUST store provider ID, release name, candidate language (as provenance), hash-match evidence, match percent, and download UTC (`TimeProvider`) on that record. A successful publication with unknown match percent MUST still write the row with a null score.
- **FR-003**: System MUST NOT write a download record when publication fails.
- **FR-004**: System MUST keep published bytes and return the existing successful download response with filename-derived label and null provider/release when the provenance upsert fails or is ambiguous; MUST log at Error with movie id and relative path; MUST NOT delete, roll back, or re-publish.
- **FR-005**: System MUST discover eligible subtitle files first and enrich from a single per-movie batch read keyed by the canonical discovery relative-path key.
- **FR-006**: System MUST NOT list a provenance row whose file is absent from discovery.
- **FR-007**: Track list and download responses MUST add nullable string fields `provider` and `releaseName` (explicit null when unknown) while preserving `id`, discovery-derived `label`, discovery-derived `language`, `isDefault`, routes, and status codes. Discovery-derived language is authoritative for Core language partitions and the wire `language`; stored candidate language remains provenance only and MUST NOT override the wire language. Each of `provider` and `releaseName` MUST be projected independently — a missing field MUST NOT clear a known sibling field.
- **FR-008**: Wire `label` on list and download MUST remain the discovery-derived filename label. The player selector MUST compose display text `release name · provider` only when both `provider` and `releaseName` are known; otherwise it MUST use the wire `label` (including hand-added files and partial provenance).
- **FR-009**: `provider` MUST be the DEV-551 canonical provider ID verbatim when known.
- **FR-010**: Within each language, Core MUST order recorded tracks before unrecorded ones, then by hash match, then known match percent descending, then download UTC descending; missing scores after known scores at the same hash rank; discovery order for exact ties and among unrecorded tracks.
- **FR-011**: Language partitions MUST follow first discovery appearance order across languages, keyed by discovery-derived language.
- **FR-012**: The player selector MUST group by the wire `language` field, render a heading per language in first-appearance order, preserve incoming order inside each group, compose or fall back display labels per FR-008, keep a manual/hand-added track selectable, and show Off once outside the groups.
- **FR-013**: After download, the playback hook MUST keep the newly downloaded track selected across reload by server track id, honor newer in-flight viewer choices via existing guards, and preserve existing absent-track handling as an unchanged regression.
- **FR-014**: System MUST leave sidecar recognition policy and file naming unchanged; existing discovery tests MUST pass unedited.
- **FR-015**: Delivery MUST introduce Core port `ISubtitleDownloadRecordStore` (post-publication write + per-movie batch read), one EF adapter and registration under Infrastructure/Persistence (table and EF types named by existing Persistence conventions), and MUST deliver ADR 0054 Accepted, the Principle I MINOR amendment (next MINOR from the then-current constitution base, preserving intervening whitelist entries), and the ArchitectureTests port-list update atomically with that port. The Spec Kit PR MUST exclude the ADR file; the proposed draft is carried from `PRIVATE_TICKET_DIR`.
- **FR-016**: `CONTEXT.md` Local Subtitle Track MUST state that a downloaded track carries its provider and release name.
- **FR-017**: Implementation MUST NOT begin until DEV-551 delivery is merged and pickup confirms provider ID plus selected-candidate evidence; missing evidence MUST stop as `needs recon`.
- **FR-018**: Contract chain MUST regenerate `web/src/api/openapi.json` then `web/src/api/types.ts`; web code MUST consume generated types only.

### Key Entities *(include if feature involves data)*

- **Subtitle download record**: Provenance for one published local subtitle path under one movie; unique on (movie identity, canonical discovery relative path); upserted after successful publication, including null match percent when the score is unknown.
- **Local subtitle track (enriched)**: Discovery-derived track optionally joined to a download record for provider/release/ranking while keeping the discovery-derived label and discovery-derived language; identity remains the existing server track id derived from the same canonical key; selector may compose a display label from both provenance fields.
- **Selected subtitle candidate (source of provenance)**: Binding/candidate evidence supplying provider ID (DEV-551), release name, candidate language, hash-match evidence, and match percent at download time.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An owner can distinguish two same-language downloaded tracks in the player by provider and release name without opening files.
- **SC-002**: A hand-added sidecar continues to appear with today’s label and is selectable.
- **SC-003**: Removing a downloaded file from disk removes it from the selector on the next list, even if a provenance row remains.
- **SC-004**: After downloading a subtitle, the owner sees that track remain selected through the automatic list refresh.
- **SC-005**: Sidecar recognition behaviour matches today’s discovery tests with no test edits.
- **SC-006**: Every Done-when automated proof named in the brief Task order passes on the delivery revision.

## Assumptions

- `[assumed]` Show the canonical DEV-551 provider ID as supplied (`ASSUMPTIONS.md`; Q3).
- `[assumed]` Break exact ranking ties by discovery order; keep unrecorded tracks in discovery order after recorded tracks for that language (`ASSUMPTIONS.md`; Q4).
- `[assumed]` Show Off once outside the language groups; order groups by first appearance in discovery order (`ASSUMPTIONS.md`; Q4).
- Spec work may proceed while DEV-551 Spec PR #520 is open; implementation remains gated on DEV-551 delivery merge and pickup evidence (`brief.md` Q8).
- Path-based matching only for external edits; no fingerprint scheme in this ticket (`brief.md` Q7, Q9).
- ADR 0054 is claimed and proposed; Quill does not claim ADR Accepted in the Spec Kit PR. Spec Kit PR excludes the ADR file; Build accepts it atomically with the port (`brief.md` Q6; Phase 2 step 6 refinements).
