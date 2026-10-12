# DEV-80 Conclusions (Patron)

## R1 - design.md ownership after plan-challenge round 1 (2026-10-12)
Ruling: Quill retains the round 1 scope trim of design.md; Palette does not resume ownership for this round. Keel verifies design.md together with plan.md and tasks.md.
- The edits only remove claims (list-level retry, skeleton, poll-error UI, committed contrast/target). Keel's adjudication forced each removal: Compass F1 is out of frozen Q1-Q6 scope (brief.md:19), and fix item 4 says to align design claims with what tasks verify, with contrast/target not a delivery requirement (DEV-80 note :90, :96).
- No new design decision was made, so this is not a §2.3 care item and does not need a Palette re-draft. A gate forced the edit, so it stays in scope.
- If Keel's verification finds that design.md added or changed UI behaviour, as opposed to deleting it, that part goes back to Palette.

## R2 - ND-01 schema transformer file and registration (2026-10-12)
Ruling: Approved. Add a new file `src/LamuFlix.Api/EnrichmentStatusSchemaTransformer.cs` (`IOpenApiSchemaTransformer`), scoped to `EnrichmentStatus` only, that emits `type: string`, `enum [Pending, Enriched, NotFound, Failed]`. Register it with `AddSchemaTransformer` in the existing `AddOpenApi` options in `src/LamuFlix.Api/Program.cs`.
- The Q-ruling at brief.md:96-100 already approved the string-by-name wire contract. Recon-DEV-80:105-109 names a schema transformer as the supported mechanism, and Sentry F1 (High, accepted) forces the edit. That makes the extra file and the Program.cs edit a forced edit, in scope.
- No new dependency, project or layer (Microsoft.AspNetCore.OpenApi is already referenced). Runtime is unchanged and nothing goes to the user.
- Extend the brief.md:34-38 touch set with these two paths. Nothing else is added.

## R3 - ND-02 first-page constants and browse-query OpenAPI metadata (2026-10-12)
Ruling: Constants approved as taste (A6 [assumed]). Correcting the metadata is approved in principle. Its mechanism and touched files stay pending recon.
- Constants: `sort=Title&direction=Ascending&page=1&pageSize=20`. This is a taste choice inside A5 and brief.md:17 (no sort or paging UI). recon-DEV-80:123-125 proves it valid.
- Metadata: the committed OpenAPI shows optional PascalCase properties, but the server requires lowercase keys (recon-DEV-80:122-126). Correcting the document so it states what the server already enforces is not a new public API shape (§2.3 item 4). The contract chain (brief.md:81-91) and AC require it so generated client and openapi-msw handlers type the real wire keys. The route, accepted values, validation, DTOs and UI must not change.
- The concrete mechanism (attribute, parameter binding or transformer) and the files it touches are not in recon. Recon must establish them, and Keel brings them back to me before drafting. If the fix would change runtime binding or validation, it becomes a new ruling.
