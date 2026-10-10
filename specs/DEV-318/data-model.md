# Data Model: DEV-318 End-to-End Import-to-Enrichment Integration Proof

**Date**: 2026-10-09

No data-model change. This feature persists and reads only existing shapes.

## Referenced entities (read-only for this feature)

- **Movie**: existing row; `EnrichmentStatus` transitions Pending (0) to Enriched (1) via
  existing `MarkEnriched`; numeric values are the database contract and unchanged.
- **EnrichmentStatus**: existing SmartEnum Pending/Enriched/NotFound/Failed; no member added.
- **EnrichmentRequested**: existing message carrying MovieId and Attempt only.
- **MovieDetailsResponse / MovieMetadataResponse**: existing GET contract; unchanged.

## Migrations

None. Schema, migrations, converters, and serialization are outside delivery scope.
