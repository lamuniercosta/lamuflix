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
