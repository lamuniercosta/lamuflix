# DEV-408 — Settled grill brief

Status: grill complete, 7 of 12 questions used. Patron confirmed shared understanding in Q7. This closes only the requested Phase 2 grill and brief work; it does not freeze a drafted plan, grant Gate 1, clear review or authorize implementation.

## Identity and authority

- Ticket: DEV-408 — Persist enrichment poster URL so library browse tiles show posters; bug, size M, ui none; parent DEV-286.
- Phase A worktree: `F:/Dev/LamuFlix.worktrees/feature-408-spec`; branch `feature/408-spec`; intake baseline `1de5162257b4eef7f1ca111ef8f6a9d1594ff542`.
- Feature directory: `specs/DEV-408`. Preserve the existing `.specify/feature.json` pin; it is intake-owned and is not part of this brief edit.
- Phase B worktree reserved by intake: `F:/Dev/LamuFlix.worktrees/DEV-408`, branch `feature/DEV-408`; not created or used by this ask.
- Ticket Scope and AC: `recon-DEV-408:18-19`. All implementation facts derive from `recon-DEV-408`, including follow-up sections 10.1–10.2. Normative sources are `specs/PRODUCT.md`, `task-pipeline`, and `lamuflix-team-charter`.
- Patron rulings are append-only in `CONCLUSIONS.md`. Their complete Q1–Q7 exchanges are reproduced below so every grill answer is in this brief. No taste assumptions or owner checkbox were identified. No ADR or global glossary edit is required.

## Settled scope and behavior

Deliver all three ticket scope items and every AC clause:

1. Carry the OMDB `Poster` value through the shipped Infrastructure metadata provider and Core `MovieMetadata`.
2. Persist it to the existing nullable text `MovieRecord.PosterUrl` / `poster_url` whenever metadata is applied, and clear it whenever metadata is cleared.
3. Retain the orphan `src/LamuFlix.Worker` poster code unchanged and record why: it is outside the solution, has no ProjectReference, and refers to absent `LamuFlix.Data` namespaces (`recon-DEV-408:26-29,47-50`). It cannot satisfy the shipped acceptance criteria. The ticket explicitly permits explaining retention. Repairing, reconnecting or removing whole legacy projects is outside this delivery. Do not copy its old-poster fallback semantics.

Acceptance remains: successful enrichment returning a poster populates `poster_url`; `GET /api/movies` returns it as `posterUrl`; clearing metadata clears `poster_url`; real-database tests prove persistence and clearing. No frontend tile implementation is added by the summary wording; ui is none.

### Canonical terms and value semantics

- `PosterUrl`: optional string in Core `MovieMetadata`, and the existing persistence record property.
- `poster_url`: existing nullable text database column. No migration or schema edit.
- `posterUrl`: nullable browse wire field.
- Metadata snapshot: the current metadata value replaces previous metadata, including artwork; a null poster overwrites an older poster with null.

Add an optional poster argument without breaking existing construction call sites unnecessarily. Include `PosterUrl` in metadata equality `State` and repository rehydration, so a poster-only change is saved. The shipped OMDB mapping deserializes `Poster`, trims a present string, and maps missing, null, blank/whitespace and `N/A` to null. Lookup still succeeds without artwork. Treat the value as an opaque provider string: no URI validation, download, proxy or new dependency.

The earlier record-only/no-Core-counterpart rulings cited by recon are superseded for `PosterUrl` alone by this ticket. Other record-only data remains protected.

### Persistence and clearing

Extend existing `ToRecord` / `CopyMetadata` / `ClearMetadata` / `ToDomain` plumbing. Non-null metadata writes all seven fields; null metadata clears all seven. A null incoming poster is written as null, not preserved through a fallback.

Recon sections 10.1–10.2 establish that no supported public non-null-to-null metadata transition exists at the intake HEAD. Add the approved, narrow `Movie.ClearMetadata()` intent operation: set `Metadata` to null, preserve enrichment status, timestamps, attempts and all unrelated state; repeated clearing is idempotent. Saving a loaded aggregate reaches the existing repository metadata-change/null branch. Do not add a repository port method, handler, route or new layer. Do not alter `MarkNotFound`, `MarkFailed`, retry or enrichment state-machine behavior.

Patron explicitly marked any Enriched-implies-non-null-Metadata invariant as unverified. If later evidence establishes such an invariant, stop and reopen Q2 with bounded recon/decision; do not silently change status semantics.

### Browse API and generated contract

Browse item fields are exactly `id`, `title`, `posterUrl`. The poster field is a nullable string and is always present, including explicit JSON null when absent. Project directly from `MovieRecord.PosterUrl` to Core `MovieSummary` to `MovieSummaryResponse`.

No route, HTTP status, filtering, pagination or details-response change. Do not add a poster field to `MovieMetadataResponse` or `MovieDetailsResponse`. The earlier id/title-only browse ruling is superseded for this additional field alone, as required by the AC.

Regenerate `web/src/api/openapi.json` and the snapshot owned by `OpenApiContractTests` from the C# DTO. Generate `web/src/api/types.ts` only through existing approved tooling when it is available; never introduce generation tooling/dependencies in this ticket or hand-edit generated artifacts. Recon reports `types.ts` absent at the intake HEAD and does not establish a generator. Under Q3/Q7, if tooling is absent, the C#-derived OpenAPI and snapshot are the deliverable at that HEAD. Any web consumer must use generated types. Concrete tool/snapshot locations require bounded recon when needed; they are not guessed here.

## Approved files and boundaries

Production files:

- `src/LamuFlix.Core/Domain/MovieMetadata.cs`
- `src/LamuFlix.Core/Domain/Movie.cs`
- `src/LamuFlix.Core/Ports/MovieSummary.cs`
- `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`
- `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`

Existing projection callers may receive only compile-forced adjustments; do not change handler behavior. Test scope includes existing `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`, `MetadataProviderLookupTests.cs`, `ApiEndToEndEnrichmentTests.cs`, `ApiEndToEndBrowseTests.cs`, `ApiEndToEndImportTests.cs`, `ApiEndToEndLibraryTests.cs`, and `OpenApiContractTests.cs`; plus recon-named `BrowseMoviesQueryHandlerTests`, `LibraryEndpointsTests` and `ApiHostCompositionTests` call sites when forced by the shape/construction changes. Add focused domain unit/property tests inside the existing UnitTests project if needed. Obtain bounded recon for any concrete path not already established.

Generated file scope is the OpenAPI document, its existing Verify snapshot, and TypeScript generated only under the tooling condition above. Existing tests are changed only where AC or compilation forces it. Preserve unrelated assertions and changes.

Excluded: dependency/package changes, new project/layer/port, schema/migrations, legacy Worker/Web/Test edits or deletion, LocalPlay/feature flags, secrets, process launching, UI features, unrelated cleanup, ADRs, shared glossary rewrite and edits to old feature documents. Documented legacy issues are `noted, no ticket` under Q4. A discovered need outside the approved boundary is routed for decision, never silently absorbed.

## Test strategy and required proofs

Use the existing test frameworks and real PostgreSQL Testcontainers infrastructure. Recon 10.1 establishes `PostgresFixture`, migrated contexts, reset and sibling-context patterns. Reuse the existing `PostgresCollection`; its definition location is unverified and must not be invented or redefined. Use existing WireMock OMDB integration infrastructure. No EF InMemory or mocked data-access driver for translation/serialization behavior.

- Core unit/property tests: poster participates in metadata equality, including poster-only differences; metadata clearing is idempotent and preserves all unrelated state/status fields. Test optional absence and the intended new operation without adding a new state-machine policy. No property-test opt-out is settled.
- Provider mapping cases: present poster, trimming, missing property, JSON null, blank/whitespace, and `N/A`; poster absence does not turn successful metadata into failure.
- Real-Postgres repository proofs: insert enriched metadata with poster; update to a poster; poster-only replacement; replace a non-null poster with null; load metadata and preserve its poster on an unrelated update; clear previously persisted non-null metadata and poster through `Movie.ClearMetadata()` then `SaveChangesAsync`. Each proof observes committed data through a fresh sibling context, not EF tracked state alone. The clearing scenario must start with a non-null poster persisted through the supported enrichment/repository path, even if separate null-replacement scenarios already ran. Assert all seven metadata fields are null after clearing and unrelated fields remain unchanged.
- Real-Postgres browse projection and API proofs: non-null stored poster is returned, and absent/cleared poster is an explicitly present null field. Preserve id/title, ordering, filters and pagination behavior.
- WireMock plus real-Postgres shipped enrichment-to-browse E2E: OMDB returns a poster, shipped enrichment stores it, and `GET /api/movies` exposes it. Do not use raw DbContext poster seeding as the sole enrichment persistence proof or reflection/private-method bypass as the clearing proof.
- Update the old record-only preservation test expectation for the poster alone; retain preservation assertions for unrelated data. Update exact browse item field lists in affected browse/import/library tests. Keep details and metadata wire field sets unchanged.
- Regenerate and verify OpenAPI/snapshot against the C# response shape; use generated TypeScript only under the approved condition.

## Task ordering for Quill

These are settled dependencies for the later plan/tasks draft, not permission to start that work now:

1. Core metadata carrier, equality and clearing operation, with focused unit/property tests.
2. Shipped provider response/mapping and lookup tests; repository write/read/clear plumbing with real-database tests. Both depend on Core. Serialize tasks that touch any shared file.
3. Browse projection and API response changes, plus compile-forced caller and exact-shape test updates; depend on Core and persistence work.
4. Regenerate C#-derived OpenAPI and Verify snapshot; approved existing TypeScript generation only when available; depends on final API shape.
5. Complete shipped enrichment-to-browse E2E and full persistence/clearing regressions; depends on prior plumbing and contract work.
6. Later pipeline refactor, Phase B gates and three-axis review, under the standing pipeline. This ask starts none of them.

Shared repository, E2E and contract test files have one writer at a time. Do not schedule dependent edits in parallel or split one shared test file among simultaneous tasks. Quill uses this brief and the cited recon; a missing implementation fact is bounded recon, and a missing decision is routed to Keel.

## Loop discipline and later gates

Closing bar: zero unresolved Critical, High or Medium findings within frozen scope. Low findings receive a recorded disposition. M review requires all three Risk, Standards and Spec reports regardless of the pre-pass blast-radius tier. Review cap is two rounds; remediation cap is two fix commits per round. Third-round findings go to the owner with the PR, without extending the cap.

Frozen scope: shipped OMDB poster mapping, optional Core metadata carrier/equality/rehydration, existing poster column persistence and clearing through the narrow domain operation, browse `posterUrl`, generated contracts and necessary tests, and documented retention of the orphan Worker; anything else is a follow-up issue, not a finding in this round.

A follow-up is filed only under charter eligibility: Critical/High, broken behavior or explicit user request, after checking existing tickets; otherwise record `noted, no ticket`. Patron decides and Rigger alone records tracker changes.

Phase B gate expectations follow `task-pipeline` Phase 3 step 5:

- Roslyn analyzers, cyclomatic complexity and InspectCode at default diff scope; do not run InspectCode with `-All`.
- Mutation on changed production C# projects, via `scripts/run-mutation.ps1`, never `since`; use configured thresholds, never lower them. Honor standing mutation evidence checks and report timeout evidence.
- Property tests with no opt-out, vulnerable packages, `dotnet format --verify-no-changes`, and `dotnet test`.
- Web gates when web artifacts change, including the OpenAPI contract document.

Exit 1 and `Could not run` block. Scope-empty exit 2 is non-blocking `SKIPPED (scope-empty)`, never PASS. Configured-disabled gates are SKIP only on their actual evidence. Mutation NOT APPLICABLE is N/A only on the script verdict/configured exclusions. An unaccepted property-test exit 2 blocks. No build substitutes for a gate, and no threshold is changed to achieve a pass.

No gate/test/Spec Kit/plan challenge/implementation was run for this grill. The next authorized pipeline step belongs to the Conductor: ask Quill to draft spec/plan/tasks from this brief. Gate 1 remains a separate later user-owned spec-PR merge decision; no seat merges or enables auto-merge.

## Evidence limits and escalation

Facts not in the named recon remain unknown. Concrete generator, snapshot, additional test path and fixture-definition locations require bounded recon if needed. Do not search outside the named worktree or named notes, infer a missing path, add a duplicate fixture, introduce a dependency, or change a ruling to bypass missing evidence.

No contradiction of ticket delivery or constitution departure was identified by Patron. If either becomes necessary, use `blocked: structural` and the owner checkbox process. A later invariant contradicting the approved metadata-only clear semantics requires recon and a fresh Patron ruling before any change.

## Complete grill exchanges and answers

The following is the read-back Patron conclusions artifact at the close of Q7, reproduced in full. Earlier exchanges remain append-only in the source artifact.

# DEV-408 — Patron conclusions

## Q1 — Poster semantics and vocabulary (grill Q1/12)

**Keel asked:** Approve an optional `string? PosterUrl` on Core `MovieMetadata`, included in its equality `State` and in rehydration; the shipped `OmdbMetadataProvider` deserializes OMDB `Poster`, mapping missing/null/blank/`N/A` to null and trimming a present string, with no URI validation and no new dependency. Rationale: lookup still succeeds when artwork is absent, and poster-only changes flow through persistence; cost is a nullable Core value plus new regression cases.

**Ruling: APPROVED as recommended.**
- Ticket authority: DEV-408 Scope items (1) and (2) require the shipped provider to carry the poster and enrichment to persist/clear it (`recon-DEV-408:18-19`); the only shipped carrier from provider to aggregate is `MetadataLookupResult.Found(MovieMetadata)` (`recon-DEV-408:32-33`), so a Core member is forced by the ticket, not a §2.3 new layer. It supersedes, for `PosterUrl` only, the earlier no-Core-counterpart stance (`specs/DEV-19/CONCLUSIONS.md:40`, `specs/DEV-301/CONCLUSIONS.md:55`), which held because no ticket then required it; tests asserting record-only poster preservation are forced edits under this AC.
- No schema change (`poster_url` text nullable exists, `recon-DEV-408:41`) and no new dependency (§2.3 items 1 and 3 untouched); `N/A`/blank normalization to null matches how absence is already expressed for optional metadata and keeps a poster-less lookup a successful `Found`.
- No URI validation is sound: the value is an opaque provider string stored as text; validation would add a failure mode the ticket does not ask for. Vocabulary `PosterUrl` matches the existing record/column (`MovieRecord.PosterUrl`, `poster_url`) and the AC wire name `posterUrl`.

## Q2 — Metadata clearing path for Scope 2 and the clearing AC (grill Q2/12)

**Keel asked:** `recon-DEV-408` 10.2 (:112-120) shows no public metadata clear path; only the private `EfMovieRepository.ClearMetadata` on new null-metadata inserts is reachable. Recommend an explicit `Movie.ClearMetadata()` intent method that clears `Metadata` alone, preserves enrichment status, timestamps, attempts and all unrelated state, and lets a loaded aggregate's `SaveChangesAsync` run the existing repository null-metadata branch. Persist `PosterUrl` in `ToRecord`/`CopyMetadata` and read it in `ToDomain`; a null incoming `PosterUrl` replaces an old poster with null, and null `Metadata` clears all seven metadata fields. `MarkNotFound`, `MarkFailed` and retry semantics unchanged. Prove non-null poster enrichment, then public `ClearMetadata` plus `SaveChangesAsync`, in real Postgres with a fresh-context reload. Cost: one narrow domain operation and transition tests; no endpoint, port or abstraction. In scope, or owner escalation?

**Ruling: APPROVED, in scope, no escalation.**
- The AC requires that clearing metadata clears `poster_url` and that a real-database test proves it (`recon-DEV-408:19`); recon shows that no supported path can produce that sequence (`recon-DEV-408:112-120`). The smallest operation that satisfies the AC is a domain intent method that reaches the existing `ApplyMetadata` -> `CopyMetadata(null)` -> `ClearMetadata` branch (`recon-DEV-408:115`). It implements the ticket's clearing AC rather than adding to what the ticket delivers, so it is not escalation (a).
- It is not a §2.3 care item: no new dependency, project, layer, port, endpoint or API shape, and no schema change (`poster_url` already exists, `recon-DEV-408:41`). Constraints: do not add `ClearMetadata` to `IMovieRepository`, a handler or `/api`, and do not change `MarkNotFound`, `MarkFailed` or retry.
- Snapshot semantics are approved: `PosterUrl` follows the metadata value like the other six fields, so a re-enrichment without a poster writes null, unlike the orphan Worker's `?? movie.Poster` (`recon-DEV-408:26`). Unverified: whether any code assumes Enriched implies non-null `Metadata`. If the plan finds such an invariant, re-ask before changing the status semantics.

## Q3 — Browse contract shape (grill Q3/12)

**Keel asked:** Approve the `GET /api/movies` item shape `id`, `title`, `posterUrl`: `posterUrl` is a nullable string that is always present (explicit null when absent), projected `MovieRecord.PosterUrl` -> Core `MovieSummary` -> `MovieSummaryResponse`. No route, status, filter or pagination change. No poster field is added to details or `MovieMetadataResponse`; Core metadata carries it for persistence only. Regenerate OpenAPI and the Verify snapshot from C#. Generated TypeScript comes only from existing approved generation tooling, never handwritten types or a new dependency (`types.ts` absent at recon HEAD, `recon-DEV-408:57`; ui none). Existing exact-shape browse, import and library tests change as AC-forced edits; details exact-shape tests stay intact. The prior DEV-302 id/title ruling is superseded for this field only.

**Ruling: APPROVED as recommended.**
- §2.3 item 4 (public API shape) is decided by the ticket: the AC names `posterUrl` on `GET /api/movies` (`recon-DEV-408:19`). The additive nullable field is the minimal shape, and it supersedes `specs/DEV-302/CONCLUSIONS.md:11` (`MovieSummary(Id, Title)`) for `posterUrl` only (`recon-DEV-408:71`).
- Keeping details and `MovieMetadataResponse` unchanged stays inside the AC, which names only browse. Adding the field there would add to what the ticket delivers.
- The contract chain follows the role rules (C# DTO -> `web/src/api/openapi.json` -> generated TS). If no generation tooling exists at this HEAD, do not introduce any; regenerating OpenAPI and the snapshot (`recon-DEV-408:57`) is enough. Changes to the exact-shape tests (`recon-DEV-408:63`) are forced edits.

## Q4 — Fate of the orphan `src/LamuFlix.Worker` poster code (grill Q4/12)

**Keel asked:** Keep the orphan Worker poster code unchanged and record why, in CONCLUSIONS and in the brief. Recon (`recon-DEV-408:26-29,47-50`) shows it has no sln registration and no ProjectReference, and the `LamuFlix.Data` namespaces it uses do not exist, so it cannot satisfy the AC. Repairing, reconnecting or deleting the legacy Worker, Web and test projects would be unrelated cleanup, and ticket Scope 3 explicitly allows recording why the code stays. Do not port its old-poster fallback; the Q2 snapshot replacement is authoritative. No dependency, project, schema, LocalPlay, secrets or `Process.Start` change, and no ADR or shared-glossary rewrite. Local canonical terms: `PosterUrl`, `poster_url`, `posterUrl`, metadata snapshot. Cost: documented dead code stays until a separate cleanup. Approve this disposition and the narrow architecture boundary?

**Ruling: APPROVED as recommended. The Worker code stays unchanged.**
- Ticket Scope 3 offers the choice "delete it or record why it stays" (`recon-DEV-408:18`). The code is outside the build (`recon-DEV-408:27-29,48`), so it neither breaks nor serves the shipped path. Deleting the poster lines alone would leave a non-compiling project equally dead. Deleting the whole project goes beyond the poster code the ticket names, under §2.3 item 6.
- The dead-code-deletion rule covers code orphaned by the ticket's own change. This code was already orphaned before DEV-408, so the rule does not reach it. The `?? movie.Poster` fallback is not ported; Q2 governs.
- The legacy Worker, Web and `tests/LamuFlix.Test` projects outside the sln (`recon-DEV-408:69`), and `specs/PRODUCT.md:10` still describing a Worker, are noted, no ticket: not Critical or High and not broken shipped behaviour. No ADR or glossary change; the terms are as Keel listed.

## Q5 — Verification plan and task ordering (grill Q5/12)

**Keel asked:** Approve this ordering:
1. Core optional `PosterUrl` in `State`, plus an idempotent `ClearMetadata` that preserves unrelated state, with unit/property tests for poster-sensitive equality and clear idempotence.
2. OMDB mapping cases (present, trimmed, missing, null, whitespace, `N/A`), with lookup success unaffected.
3. Repository insert, update, rehydration and clear, plus the direct browse projection.
4. The API field and the regenerated OpenAPI file and snapshot.
5. A shipped enrichment-to-browse end-to-end test.

Real-Postgres tests use the existing `PostgresFixture`/`PostgresCollection` (`recon-DEV-408` 10.1). Fresh sibling contexts prove, in order: a non-null enriched poster, a poster-only replacement, a replacement with null, then public `ClearMetadata` clearing all seven fields with browse returning an explicit null. A WireMock OMDB plus real-Postgres end-to-end test proves that a supplied poster appears in `GET /api/movies`. A raw DbContext poster write may not be the sole persistence proof, and no private-method or reflection bypass is allowed. Update the record-only preservation expectation for the poster only and keep the unrelated-field assertions. Use the existing integration files `EfMovieRepositoryTests`, `MetadataProviderLookupTests`, `ApiEndToEndEnrichmentTests`, `ApiEndToEndBrowseTests`, `ApiEndToEndImportTests`, `ApiEndToEndLibraryTests` and `OpenApiContractTests`, and add focused domain tests in the existing UnitTests project if needed. Tasks on shared files run in sequence, and the dependency chain is Core -> adapter/repository -> browse contract -> regeneration -> end-to-end. No parallel writes to one file, and no test framework or package change.

**Ruling: APPROVED as recommended.**
- It covers every AC clause: populated `poster_url`, `posterUrl` on browse, and clearing, each proven against a real database (`recon-DEV-408:19`). It uses only the existing real-Postgres and WireMock infrastructure (`recon-DEV-408:60-62,84-104`), so no §2.3 dependency or project change is involved.
- Forbidding raw DbContext poster writes and reflection as the sole proof closes the gap recon found, where today's only non-null poster proofs bypass the repository (`recon-DEV-408:118-120`). The forced test edits are limited to poster expectations and exact-shape lists (Q1, Q3).
- The ordering follows the compile dependency (Core -> Infrastructure -> Api -> contract -> end-to-end), and running shared-file tasks in sequence avoids conflicting writes. Unverified: the location of the `PostgresCollection` definition (`recon-DEV-408:105`); it is referenced, so it is reused, not redefined.

## Q6 — Loop discipline and gate expectations (grill Q6/12)

**Keel asked:** Approve these terms:
- **Closing bar:** zero unresolved Critical, High or Medium findings within the frozen ticket scope. Low findings get a recorded disposition and are never silently upgraded or used to demand an unsolicited cleanup commit.
- **Review:** an M review runs all three Risk/Standards/Spec axes regardless of the blast-radius pre-pass; at most 2 review rounds and 2 fix commits per round; third-round findings go to the owner with the PR, never a cap extension.
- **Frozen scope:** OMDB `Poster` -> optional Core metadata -> existing `poster_url` persistence and the `ClearMetadata` operation -> `GET /api/movies` `posterUrl`, plus the generated contract and the necessary tests; the orphan Worker is retained with an explanation. Anything else is a follow-up, not a finding in this round, filed only if it passes charter eligibility, otherwise noted, no ticket.
- **Phase B gates (pipeline Phase 3 step 5):** Roslyn, complexity, InspectCode at default diff scope, mutation without `since`, property tests (no opt-out, because Q5 adds domain properties), vulnerable packages, format, `dotnet test`, and web gates if web artifacts change. Thresholds are never lowered. Exit 1 or `Could not run` blocks. A scope-empty exit 2 is recorded `SKIPPED (scope-empty)`. A configured-disabled SKIP and mutation N/A are accepted only on the script's own evidence.
- **Phase A:** this ask runs no gates and grants no Gate 1, review or merge clearance.

Cost: Medium correctness/spec findings block, and the owner handoff at the cap is bounded.

**Ruling: APPROVED as recommended.**
- The round caps, M fan-out and gate rules restate standing rulings and are not re-ruled here: `task-pipeline` review and remediation caps (:26-27), axis fan-out always on M (:114), the gate list and `Could not run`/exit 1 blocking (:100-107, :132), scope-empty SKIPPED never blocks (:107), mutation never uses `since` (`lamuflix-team-charter`:68), and property-test opt-out only by a recorded line (:102). No opt-out is recorded for DEV-408, so property-test exit 2 fails.
- The closing bar of zero C/H/M within frozen scope is approved as this ticket's bar. The frozen scope is exactly what Q1-Q4 decided.
- Follow-up eligibility follows the charter (:31): an out-of-scope finding is ticketed only if it is Critical or High, is broken behaviour, or the user asked for it, after checking open tickets. Everything else is noted, no ticket.

## Q7 — Shared understanding, file boundary and grill close (grill Q7/12)

**Keel asked:** Confirm the shared understanding and the file boundary for the brief.
- **Production edits:** limited to `src/LamuFlix.Core/Domain/MovieMetadata.cs` and `Movie.cs`, `Core/Ports/MovieSummary.cs`, `Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs`, `Infrastructure/Persistence/EfMovieRepository.cs` and `EfMovieCatalog.cs`, and `Api/Endpoints/LibraryResponses.cs`. Existing projection callers and tests get only compile-forced or AC-forced adjustments; no handler behaviour changes.
- **Tests:** the Q5 files, focused new domain unit/property tests in the existing UnitTests project, and the shape/composition call sites named by recon (`BrowseMoviesQueryHandlerTests`, `LibraryEndpointsTests`, `ApiHostCompositionTests`).
- **Generated artifacts:** `web/src/api/openapi.json` and the snapshot owned by `OpenApiContractTests`. `types.ts` only if existing approved generation tooling exists; never introduce tooling or dependencies or hand-edit generated artifacts.
- **Excluded:** no schema or migration, no legacy Worker/Web/test projects, no package or project, feature flag, secret, process or UI change, and no ADR or global glossary edit.
- **ClearMetadata:** preserves status and state as decided in Q2; if later evidence establishes a contrary invariant, stop for recon or a decision rather than alter the semantics.
- **Evidence limits:** missing generator, snapshot and fixture locations are not guessed; implementation pickup uses bounded recon when it needs concrete paths.

Recommend closing at 7/12 with no taste assumptions and no owner checkbox; Keel writes the brief with the Q1-Q7 receipts, the explicit test, gate and ordering plan, the loop terms and these evidence limits. Authorize the close?

**Ruling: CONFIRMED. The grill closes at 7/12 and Keel writes `brief.md`.**
- The production file set is exactly the recon-located surfaces of Q1-Q3: the domain (`recon-DEV-408:33,43`), the adapter (:34), the repository (:23) and the browse projection chain (:54-55). The Worker stays as Q4 decided, and the test set is what Q5 decided. Nothing is added to or dropped from the ticket's Scope or AC (`recon-DEV-408:18-19`), so there is no `blocked: structural` and no owner checkbox.
- §2.3 review: no dependency, project, layer, schema, LocalPlay, secret or `Process.Start` change. The one public API change, `posterUrl` on browse, is decided by the AC (Q3). Deleting or rewriting a tracked file: only the named or AC-forced files. The ruling contains no taste decision, so `ASSUMPTIONS.md` stays empty.
- The evidence limits are approved as stated: unknown paths go to bounded recon, never guesses, and a contrary Enriched-metadata invariant reopens Q2 rather than altering it silently.

