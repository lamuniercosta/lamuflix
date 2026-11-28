# Contract: subtitle track list and download (DEV-553)

**Authority**: Ticket Wanted 2 / Where it lands (`recon-DEV-553:19–72`); `brief.md` Q3 and Phase 2 step 6 refinements; `PRODUCT.md` contract chain; existing `SubtitleEndpoints` routes.

## Unchanged surface

| Item | Rule |
|---|---|
| List route | Existing authenticated GET subtitle-tracks route for a movie (no new path). |
| Download route | Existing authenticated download route (no new path). |
| Status codes | Unchanged success and error statuses. |
| Track identity | Existing `id` (server track token). |
| Existing fields | Discovery-derived `label`, discovery-derived `language`, `isDefault` retained on the wire. Discovery-derived language is authoritative; stored candidate language is provenance only and does not override the wire field. |

## Extended DTO — `SubtitleTrackDto`

Shared by list items and the download success track payload:

| Field | Type | Nullability | Notes |
|---|---|---|---|
| `id` | string | required | Unchanged |
| `label` | string | required | Discovery-derived filename label preserved on both list and download (not rewritten to the composed display form) |
| `language` | string | required | Discovery-derived; authoritative for Core partitions and selector grouping. Stored candidate language from the record is not projected here. |
| `isDefault` | boolean | required | Unchanged |
| `provider` | string | nullable | DEV-551 canonical provider ID verbatim when known; JSON `null` when unknown |
| `releaseName` | string | nullable | Release name when known; JSON `null` when unknown |

Not on the wire: hash-match evidence, match percent, download timestamp, relative path.

## Mapping requirements

- Both list and download response mappings in `SubtitleEndpoints.cs` MUST project the new fields (forced by the DTO change).
- Project `provider` and `releaseName` independently: a known field remains populated when the other is null; one missing field MUST NOT erase a known sibling.
- Explicit JSON `null` when a provenance field is absent (including fallback after failed/ambiguous/cancelled metadata write).
- Wire `label` stays the discovery-derived value on both responses even when both provenance fields are present.
- Regenerate committed `web/src/api/openapi.json`, then `web/src/api/types.ts`, only after endpoint projection proofs pass.
- After regeneration, both OpenAPI properties for `provider` and `releaseName` MUST accept string or null in the emitted schema dialect, and both generated TypeScript properties MUST admit `string | null`; retain drift checks.
- Web fixtures and UI MUST consume generated types only — no hand-rolled DTO duplicates.

## Display boundary (selector, not wire)

The player selector composes visible text `release name · provider` only when both `provider` and `releaseName` are non-null; otherwise it uses the wire `label`. Partial provenance therefore surfaces the known field on the wire without inventing a half display label.

## Ranking boundary

Core produces the ordered track list partitioned by discovery-derived language. The web selector groups by the wire `language` field in first-appearance order and MUST preserve the incoming order inside each group. Off is a client-only choice outside the DTO list.
