# Constitution amendment v1.6.0 (DEV-331)

**Status**: Applied 2026-10-28 with ADR-0030 (Accepted). Owner-approved on PR #272 (see "Owner approval").  
**Baseline**: Constitution **Version 1.5.0**, Last Amended 2026-10-27.  
**Amendment date**: 2026-10-28. **Ratified**: 2026-09-21 (unchanged).  
**Ticket**: DEV-331. **Companion ADR**: `docs/adr/0030-tmdb-secondary-metadata-provider.md`.  
**Immediate consumer**: DEV-423 (keyed registrations and fallback chain). This amendment does not authorize that chain.

This is the written amendment record required by the Governance amendment procedure (`.specify/memory/constitution.md`, Governance). Layout follows `specs/DEV-330/constitution-amendment-v1.5.0.md`.

## Owner approval

Owner approval: granted on PR #272. Following the DEV-330 convention, the answer is recorded here and in `tasks.md` T026, and the frozen packet copy in `specs/DEV-331/spec.md:10` stays unticked.

Evidence:

- PR #272 body edit history (GraphQL `userContentEdits`): revision `2026-10-27T21:40:22Z` has `- [ ] blocked: structural — Approve amending constitution.md and adding an ADR so TmdbMetadataProvider is permitted beside OmdbMetadataProvider and TmdbOptions is permitted in Core?`, and revision `2026-10-27T21:51:09Z` has the same line as `- [x]`.
- PR #272 owner comment `Accepted.` by `lamuniercosta` (association `OWNER`), `2026-10-27T21:51:20Z`, 11 seconds after the tick.
- The `2026-10-28T02:08:38Z` body rewrite replaced the ticked line with prose, so the current PR body shows no ticked box. This record is the durable copy.

## Sequencing: code merged before the amendment

PR #272 merged at `2026-10-28T02:56:16Z` while the constitution still read 1.5.0. Governance, Compliance requires non-compliant code to be blocked from merging unless an amendment is submitted and approved at the same time. The owner approval predates the merge, but the amendment text and ADR did not land with it: no task drafted them, and T026 only checked that they had landed.

Owner decision (2026-10-28): this amendment is the remedy under Compliance. It lands now, cites the PR #272 approval, and is not treated as a new approval round. Future specs that need a constitution change carry the amendment draft in the spec packet and an explicit Phase B apply task, as DEV-330 did.

## Purpose

Two constitution departures, matching the owner ruling (DEV-331 `CONCLUSIONS.md` Q7, checkbox at `specs/DEV-331/brief.md:19`), plus one owner-approved clarification:

1. **Technology Stack Constraints, Metadata provider row.** OMDb stays the production provider; the TMDb secondary adapter `TmdbMetadataProvider : IMetadataProvider` is named beside it, disabled by default. The row does not define provider selection.
2. **Principle VIII, vendor-name exception** adds the options record `TmdbOptions` in `LamuFlix.Core.Options`.
3. **Principle VIII, vendor-name exception** also adds the provider tag-value constants `OmdbProviderName` and `TmdbProviderName` in `TelemetryConstants` (owner decision 2026-10-28, see D3). `OmdbProviderName` predates DEV-331 (DEV-319); the exception makes the existing practice explicit instead of moving both values to Infrastructure.

Supporting edits:

- Principle VII options-record list adds `TmdbOptions` beside `OmdbOptions`.
- Version 1.5.0 to 1.6.0 (MINOR). Sync Impact Report entry prepended; footer updated.

## Scope limits

- **No chain, fallback, or keyed registration.** DEV-423 owns the `omdb` and `tmdb` keyed registrations and `ChainedMetadataProvider` (`specs/DEV-423/spec.md:14`). Its own Phase B gate is not cleared by this amendment beyond the upstream constitution/ADR dependency.
- **No new port.** The closed port list is unchanged; TMDb implements the existing `IMetadataProvider`. `ArchitectureTests` needs no whitelist change.
- **No activation.** TMDb is disabled by default (`src/LamuFlix.Core/Options/TmdbOptions.cs:11`). Production enrichment stays on OMDb.
- **No broader vendor relaxation.** The exception names `TmdbOptions` and the two tag-value constants only.

## Code as landed (main `f52c83f1`)

| Item | File:line | What the code does |
|---|---|---|
| Options record | `src/LamuFlix.Core/Options/TmdbOptions.cs:6, 8` | `public sealed record TmdbOptions`, section `Tmdb` |
| Enabled | `TmdbOptions.cs:11` | `bool`, default `false` |
| BaseUrl | `TmdbOptions.cs:14-17` | default `https://api.themoviedb.org/3/`; `[Required] [Url] [AllowedBaseUrl]` |
| ApiKey | `TmdbOptions.cs:20` | default `string.Empty`; no `[Required]` |
| Adapter | `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs:22-28` | `: IMetadataProvider` |
| Threshold | `TmdbMetadataProvider.cs:25, 41, 118` | read from `IOptions<OmdbOptions>.Value.MatchThreshold` |
| Telemetry | `TmdbMetadataProvider.cs:37, 50, 59-61` | `ActivitySourceName`, `MetadataLookup`, `TmdbProviderName` |
| DI, OMDb | `src/LamuFlix.Infrastructure/Adapters/MetadataProviderServiceCollectionExtensions.cs:22-28` | unkeyed `IMetadataProvider` to `OmdbMetadataProvider` (unchanged) |
| DI, TMDb | `MetadataProviderServiceCollectionExtensions.cs:30-36` | `AddHttpClient<TmdbMetadataProvider>` with its own resilience handler |
| Health | `MetadataProviderServiceCollectionExtensions.cs:38-45` | `metadata-provider-tmdb`, untagged, outside `/health/ready` |
| Tag constants | `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs:67, 69` | `OmdbProviderName = "omdb"` (pre-existing), `TmdbProviderName = "tmdb"` |

## Edits applied to `.specify/memory/constitution.md`

1. Metadata provider stack row (1.5.0 line 379): names TMDb as the disabled-by-default secondary adapter beside OMDb, refers to ADR-0030, and notes each typed client owns its own resilience pipeline.
2. Principle VII options list (1.5.0 line 303): `TmdbOptions` after `OmdbOptions`.
3. Principle VIII vendor-name rule (1.5.0 lines 329-332): exception extended to `TmdbOptions` and the two tag-value constants.
4. Sync Impact Report: 1.6.0 entry prepended above the 1.5.0 entry, which keeps its text under a `Previous version change` label.
5. Footer: `**Version**: 1.6.0 | **Ratified**: 2026-09-21 | **Last Amended**: 2026-10-28`.

New header text uses a colon after `MINOR` instead of the house em dash; quoted 1.5.0 text keeps its characters.

## Discrepancies and decisions

**D1. Stale citations in the owner checkbox.** The checkbox and Q7 ruling cite `:280-282, :307-310, :357`. In 1.5.0 the stack row is at `:379` and the VIII vendor rule at `:329-332`. The checkbox text stays verbatim (`specs/DEV-331/brief.md:21`); this record cites the correct sections.

**D2. Code merged before the amendment.** Resolved by the sequencing decision above.

**D3. Vendor-named tag constants in Core.** Resolved: owner chose the exception route over moving the values to Infrastructure (Purpose item 3).

**D4. `OmdbOptions` in Core.** `OmdbOptions` is vendor-named in Core and listed in Principle VII, but Principle VIII does not name it. Left as is; open for the owner.

**D5. Other vendor strings.** The health check id `metadata-provider-tmdb`, the shared `Omdb:Resilience` section, and the `Tmdb` section name are not named in the exception. Open for the owner: treat them as configuration keys, add them, or rename.

**D6. TMDb depends on `OmdbOptions`.** The match threshold comes from `OmdbOptions.MatchThreshold` (Q3a). Recorded in ADR-0030; not a constitution breach.

**D7. Domain glossary.** `CONTEXT.md` says the Metadata Provider term avoids "hardcoding OMDb in Core names", and the code uses "SecondaryMetadataProvider" in type names. No `CONTEXT.md` edit here; open for the owner or Quill.

**D8. Principle I port list is incomplete (pre-existing).** It omits `IRecommendationProfileSource` and `IEnrichmentOutbox`, which `ArchitectureTests` whitelists. Not changed here.

**D9. Hard-coded default base URL.** `TmdbOptions.BaseUrl` defaults to the public TMDb URL. Principle VII bans hard-coded fallbacks for connection strings, API keys, library roots, and player paths; a base URL is not on the list, so the literal reading is compliant.

**D10. Adapter gap against Q5.** Non-miss `success = false` 200 bodies do not map to `Failed(InvalidResponse)`. Filed as DEV-436; ADR-0030 records the intended rule.
