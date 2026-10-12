# Data Model: DEV-323 Movie Details view

**Date**: 2026-10-12 | **Sources**: `brief.md` plan-decisions section, `recon-DEV-323:17-20,58`.

## MovieMetadata (Core, additive)

Existing fields stay: Title, Synopsis, ReleaseYear, Runtime, ImdbRating, ImdbId. Added: nullable `PosterUrl`, nullable `RottenTomatoesRating`, nullable `MetaScore`, non-null `Directors` and `Actors` string collections (empty lists when absent).

## MovieDetailsResponse (API wire, additive)

Outer response, metadata nullability, route, and status codes unchanged. `MovieMetadataResponse` gains nullable `posterUrl`, `rottenTomatoesRating`, `metaScore` plus `directors`/`actors` string arrays mapped from the extended projection.

## Playback (unchanged)

No entity or state change. POST play responses (204/400/403/404/422) and the server-side LocalPlay gate keep existing semantics.

## Out of scope

Backdrop, tagline, writers: no storage, wire, or rendering in this ticket (owner checkbox open). No migration or enrichment-source entity.
