# Contract: Library failure projection (DEV-522)

**Source**: Patron Q3 (`brief.md` Approach 4; `CONCLUSIONS.md` Q3); `recon-DEV-522` lines 62–66.
**Status**: Ruled additive public API shape for this ticket. Not an implementation commit.

## Surface

Existing library movie **summary** and **detail** JSON responses (current routes preserved).

## Additive fields

| JSON name | Type | When status is `Failed` | When status is `Pending`, `Enriched`, or `NotFound` |
|---|---|---|---|
| `failureCategory` | string or null | Existing enrichment failure category code when available (including `duplicate_imdb_id`) | Always null |
| `failureMessage` | string or null | Canonical caller-safe message for that category when available | Always null |

## Invariants

- Preserve existing `EnrichmentStatus` values and library routes.
- Never persist or return raw exception text.
- Generate OpenAPI from C# DTOs, then TypeScript from OpenAPI; web consumes generated types only.
- Null failure details retain existing Failed UI label (`ASSUMPTIONS.md`); do not invent fallback cause wording.

## Out of contract for this ticket

- New routes or status codes.
- Public import-result DTO redesign (preserve existing import semantics; any new import-contract shape needs a separate ruling — `brief.md` Q5).
