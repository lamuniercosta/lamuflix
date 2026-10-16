# Feature Specification: DEV-411 Ingest OMDb genres, cast, director, writers, Metascore and full plot

**Feature Branch**: `feature/411-spec`

**Created**: 2026-10-16

**Status**: gate1: provisional

**Input**: Ticket DEV-411 (size M, parent DEV-288); settled grill brief `specs/DEV-411/brief.md` (grill closed 7/12); facts from `recon-DEV-411`; Patron rulings in `specs/DEV-411/CONCLUSIONS.md`. No decisions beyond `brief.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Existing enrichment stores the full metadata snapshot (Priority: P1)

A library movie enriched through the existing enrichment trigger gains the full plot, genres, directors, writers, actors, and Metascore. Details keep the current response shape and show the fields that shape already exposes. The existing genre facet includes genres persisted by that enrichment. Movies that are not enriched do not gain these values.

**Why this priority**: This is the ticket's delivery. Existing library entries gain the data only through existing enrichment behavior.

**Independent Test**: Stub a full OMDb movie response, run the existing import or enrichment path against real Postgres, and read details plus the existing genre facet. Confirm the stored snapshot, the unchanged response field set, and the query that was sent.

**Acceptance Scenarios**:

1. **Given** an OMDb movie match, **When** existing enrichment looks it up, **Then** the request sends one `plot=full` and still sends `apikey`, `t`, `type=movie`, and optional `y` only when a year is already supplied.
2. **Given** a full OMDb payload with plot, genres, directors, writers, actors, and an in-range Metascore, **When** enrichment completes, **Then** those values are persisted as one metadata snapshot, including writers and genres.
3. **Given** that persisted snapshot, **When** details are read, **Then** Synopsis, MetaScore, Directors, and Actors reflect it, and the response field set is unchanged: no Writers field and no per-movie Genres field.
4. **Given** genres persisted for that movie, **When** the existing genre facet is read, **Then** those genres are included.
5. **Given** an existing library movie that has not been enriched again, **When** this change is applied, **Then** no backfill or refresh job writes these values onto it.

---

### User Story 2 - Incomplete optional OMDb data does not fail enrichment (Priority: P2)

Optional credit and score fields that are missing, blank, `N/A`, duplicated, or malformed do not turn an otherwise valid enrichment into a failure. Writer credits stay literal.

**Why this priority**: Enrichment must tolerate incomplete OMDb optional data. Discarding unusable optional values is the accepted cost.

**Independent Test**: Drive the provider mapping with WireMock payloads that omit or corrupt only the optional fields, including a year and a no-year lookup, and assert the mapped snapshot and the exact query.

**Acceptance Scenarios**:

1. **Given** a found movie whose Genre, Director, Writer, or Actors field is missing, blank, or `N/A`, **When** it is mapped, **Then** that collection is empty and the lookup still succeeds when the required title is present.
2. **Given** comma-separated tokens with surrounding whitespace and case-insensitive duplicates, **When** they are mapped, **Then** tokens are trimmed, empty tokens are removed, and the first spelling is kept in first-seen order.
3. **Given** a writer credit that includes a parenthetical annotation, **When** it is mapped, **Then** the credit is stored as that literal text, with no person-name parsing.
4. **Given** Metascore `0` or `100`, **When** it is mapped, **Then** the score is kept as the existing nullable score.
5. **Given** a Metascore that is missing, blank, `N/A`, malformed, or outside 0 through 100, **When** the rest of the response is valid, **Then** the score is null and enrichment still succeeds.

---

### User Story 3 - Snapshots, deletion, and the additive migration keep shared names (Priority: P3)

Saving metadata replaces that movie's associations. Repeating enrichment, enriching two movies that share names, clearing metadata, and deleting a movie do not duplicate or destroy shared lookup rows. The new migration keeps existing movies and does not invent writer links.

**Why this priority**: Partial updates would leave stale credits, and a migration that fabricates writers or drops existing data would corrupt the library.

**Independent Test**: Against real Postgres, save and reload a snapshot from a fresh context, replace it, clear it, repeat it, and enrich two movies concurrently. Upgrade a database that already has the Initial migration and existing movies, then roll that upgrade back. Delete a movie and reset the fixture.

**Acceptance Scenarios**:

1. **Given** a movie that already has associations, **When** a new metadata snapshot is saved, **Then** that movie's genres, directors, writers, actors, and Metascore are replaced; each role reuses a lookup row by trimmed case-insensitive name; the stored name's casing wins; only a genuinely new name is inserted.
2. **Given** the same snapshot saved again, **When** enrichment repeats, **Then** no duplicate lookup or join row is created.
3. **Given** two different movies enriched at the same time with a shared name, **When** both saves finish, **Then** that shared name has one lookup row per role and no duplicate join row.
4. **Given** stored metadata, **When** metadata is cleared, **Then** that movie's associations are removed and shared lookup rows remain.
5. **Given** a movie with join rows, **When** the movie is deleted, **Then** its join rows are removed and shared lookup rows remain. No global orphan cleanup or role reclassification runs.
6. **Given** a database already on the Initial migration with existing movies, **When** the second migration is applied, **Then** those movies remain and no writer associations are fabricated.
7. **Given** that upgrade, **When** it is rolled back, **Then** `movie_writers` is dropped before `writers`, and preexisting tables and rows remain.

### Edge Cases

- A missing collection field and a collection of only empty, blank, or `N/A` tokens both become an empty collection.
- Case-insensitive duplicates keep the first-seen spelling and the first-seen order.
- A parenthetical writer credit is one literal credit, not a structured role.
- Metascore accepts only an invariant whole integer from 0 through 100; every other optional score becomes null without failing the lookup.
- Full plot uses the existing synopsis handling. Required-field behavior, including a missing title, stays as it is.
- Clearing metadata removes associations for that movie only. Deleting a movie cascades its join rows and preserves shared lookup records.
- Concurrent enrichment is safe in the database. A design that needs a schema change to an existing table stops for a ruling before implementation continues.
- Existing callers of metadata construction keep working when genres and writers are omitted.
- Public routes, status behavior, and the details metadata field set do not change. No OpenAPI, generated TypeScript, or web edit is part of this ticket.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Lookup MUST send one `plot=full` and MUST preserve `apikey`, `t`, `type=movie`, and the existing optional `y` behavior.
- **FR-002**: The provider MUST read Genre, Director, Writer, Actors, Metascore, and Plot from the OMDb payload.
- **FR-003**: Each of Genre, Director, Writer, and Actors MUST be split on commas, trimmed, and stripped of empty, blank, and `N/A` tokens. Remaining tokens MUST be deduplicated case-insensitively, keeping the first spelling and first-seen order. A missing field MUST become an empty collection.
- **FR-004**: A Writer MUST be the literal credit text, including any parenthetical annotation. The system MUST NOT infer a person identity or parse a structured credit.
- **FR-005**: Metascore MUST be an invariant whole integer from 0 through 100 on the existing nullable score. Missing, blank, `N/A`, malformed, and out-of-range values MUST become null and MUST NOT fail an otherwise valid enrichment.
- **FR-006**: Full Plot MUST go through the existing synopsis handling. Existing required-field behavior MUST stay in place.
- **FR-007**: Core metadata MUST gain optional Genres and Writers parameters after the existing actors parameter, so existing construction call sites still compile. Genres and Writers MUST be ordered `System.Collections.Immutable` collections: construction copies the supplied sequences into that storage, and the exposed collections MUST NOT be mutable, including by cast. Ordered value equality and hash MUST include Genres and Writers on the same terms as Directors and Actors. Order is the order supplied after normalization. Empty and omitted collections are empty.
- **FR-008**: Saving changed metadata MUST replace that movie's genres, directors, writers, actors, and Metascore as one snapshot. Rehydration and the internal catalog metadata projection MUST return the complete snapshot, including full Synopsis.
- **FR-009**: Lookup identity MUST be resolved independently per role by trimmed case-insensitive name. The existing database row's casing wins. An existing row MUST be reused. Only a genuinely new name MUST be inserted.
- **FR-010**: Repeating enrichment MUST be idempotent. Concurrent enrichment of separate movies that share names MUST NOT create duplicate lookup or join rows. That race handling MUST stay inside the existing repository, using a database transaction with isolation or locking and bounded conflict handling. It MUST NOT add a process-only lock, a dependency, or an architectural layer. A need to change an existing table's schema MUST stop for a ruling.
- **FR-011**: Clearing metadata MUST remove that movie's associations. Deleting a movie MUST cascade its join rows and MUST preserve shared lookup records. The system MUST NOT run global orphan cleanup, reclassify a credit from one role to another, or delete a shared credit.
- **FR-012**: Public routes, status behavior, and the details metadata field set MUST stay unchanged. Existing Synopsis, MetaScore, Directors, and Actors MUST reflect persisted enrichment. Writers and per-movie Genres MUST remain internal: no new response fields, no browse widening, and no OpenAPI, generated TypeScript, or web edits. The existing genre facet MUST consume the newly persisted genres.
- **FR-013**: Persistence MUST add a writers lookup (`id`, `name`) and a `movie_writers` join (`writer_id`, `movie_id`) by the existing lookup and join conventions, in one second migration generated by EF tooling. The Initial migration, existing tables, and existing movie columns MUST stay intact, including the columns that already store plot and Metascore. Up MUST preserve existing movies and MUST NOT fabricate writer associations. Down MUST drop `movie_writers` before `writers` and MUST retain preexisting tables and data. Generated designer and model snapshot files MUST NOT be hand-edited.
- **FR-014**: Proof MUST use the existing WireMock harness and real Postgres. Database query translation MUST NOT be mocked. The proofs are Core construction and equality, provider query and mapping, writer migration and repository snapshot behavior, catalog details and genre facets, and import/enrichment end to end.
- **FR-015**: The system MUST NOT add a UI, a provider replacement, a backfill or refresh job, playback, secret handling, a new dependency, a new project, or a new abstraction layer.

### Key Entities

- **Movie metadata**: The enrichment snapshot. It gains genres and writers beside the existing directors, actors, synopsis, and Metascore. Omitting the new collections leaves existing callers valid.
- **Genre, Director, Writer, Actor**: Role-specific literal credits. The same display text in two roles is two lookup identities. Writer text may include a parenthetical and is not a parsed person.
- **Writers lookup and movie-writers join**: New storage for writer names and the movies that credit them. Genre, director, and actor storage already exists and is reused.
- **Details metadata response**: Unchanged field set. It exposes synopsis, Metascore, directors, and actors from the persisted snapshot. Writers and per-movie genres stay inside the library.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After existing enrichment of one full OMDb movie response, the stored snapshot contains the full plot, genres, directors, writers, actors, and an in-range Metascore; details show Synopsis, MetaScore, Directors, and Actors; the response field set is unchanged; the existing genre facet includes the persisted genres.
- **SC-002**: A found movie whose optional credit or score fields are missing, blank, `N/A`, duplicated, or malformed still enriches. Metascore outside 0 through 100, or not a whole integer in that range, is null. Writer parentheticals remain literal. Lookups with and without a year each send `plot=full` and the existing keys.
- **SC-003**: Replacing metadata replaces that movie's four associations and Metascore. A repeated save and a concurrent save of two movies that share a name create no duplicate lookup or join row. Clearing and movie deletion remove that movie's join rows and leave shared lookup rows in place.
- **SC-004**: Upgrading from the Initial migration keeps existing movies and adds no writer associations. Rolling back drops `movie_writers` before `writers` and keeps preexisting tables and rows.
- **SC-005**: No public route, status code, or details metadata field is added or removed. No web, OpenAPI, or generated TypeScript file needs an edit for this ticket.
- **SC-006**: Configured verification runs without a lowered threshold or a new opt-out. An unchanged web tree is a scope-empty skip and is not recorded as a pass.

## Assumptions

- Comma-delimited collections deduplicate case-insensitively, retaining first-seen spelling and order. Writer parenthetical credits stay literal. These are confirmed `[assumed]` conventions in `specs/DEV-411/ASSUMPTIONS.md`, not open questions.
- On a trimmed case-insensitive match within one role, the existing lookup row's casing wins. This is a confirmed `[assumed]` convention in `specs/DEV-411/ASSUMPTIONS.md`, not an open question.
- No owner checkbox is open. Anything not decided in `brief.md` is a `needs decision:` item, not an assumption added here.
