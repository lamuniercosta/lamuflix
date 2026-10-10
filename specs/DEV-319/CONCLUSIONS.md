# DEV-319 - Patron conclusions (Phase 2 grill)

Basis sources: ticket text (recon-DEV-319:24-39, from YouTrack get-task, DEV-319 note:17), `specs/PRODUCT.md`,
`.specify/memory/constitution.md`, charter §2.3. Facts come from recon-DEV-319 (base a62fbe8).

## Standing rulings (cited once for this chain)

- S1. The application meter name is `TelemetryConstants.ActivitySourceName` (`LamuFlix`). No second identity constant.
  Basis: specs/DEV-307/CONCLUSIONS.md:71-74, already subscribed at src/LamuFlix.ServiceDefaults/Extensions.cs:50.
- S2. No instrument without a ruled name: this ticket adds exactly three instruments and no others.
  Basis: specs/DEV-307/tasks.md:112 (T029); ticket Scope 1 Metrics.

## Patron rulings (seeded from recon, before Keel's questions)

- P1. **Ticket literals are frozen.** These names are owner decisions: the instrument names
  `lamuflix.enrichment.duration` (Histogram), `lamuflix.enrichment.outcome` (Counter) and `lamuflix.import.count` (Counter);
  the tag keys `provider`, `outcome` and `failure_category`; and the outcome value set `enriched|not_found|failed`.
  Renaming one, adding a fourth outcome value, or adding a tag the ticket does not list would change the ticket text.
  That is `blocked: structural`, not a Keel call.
  - Basis: ticket Scope 1 (recon-DEV-319:29-32); PRODUCT.md:23-24 (the Scope & Technical Design is authoritative).
- P2. **Span names and attributes are already delivered.** `Enrichment.Enqueue`, `Enrichment.Process`, `Metadata.Lookup`,
  `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count` and `error.type` exist at HEAD. They are kept as they are and
  verified, not re-created.
  - Basis: ticket Scope 1 (recon-DEV-319:28,33); recon-DEV-319:78,155.
- P3. **Edits beyond the named file are in scope.** `TelemetryConstants` is named by the ticket. Instrument emission in
  EnrichmentConsumer, OmdbMetadataProvider and the import feature (and their tests) is required by AC1, so it is a
  required edit and not a §2.3 item 6 ruling.
  - Basis: AC1 `Metrics and traces register with consistent naming and dimensions` (recon-DEV-319:38).
- P4. **Dependency guard (care item 1).** The plan adds no NuGet package and no new PackageReference to any project,
  Core included: OpenTelemetry packages are already pinned (Directory.Packages.props:26-34), and BCL
  `System.Diagnostics.Metrics` needs no package. If Keel's placement needs a new reference in any project (for example
  `IMeterFactory` in Core pulling in `Microsoft.Extensions.Diagnostics.Abstractions`), it comes back to Patron for a
  ruling before plan.md is written.
  - Basis: charter §2.3 item 1; PRODUCT.md:43; ArchitectureTests.cs:65 (Core may not reference OpenTelemetry).
- P5. **No new project, layer, port or abstraction (care item 2).** Instruments go in existing projects. A new
  interface or port that exists only to carry telemetry comes back to Patron.
  - Basis: PRODUCT.md:18 and :44.
- P6. **ADR-0008: file, status and scope.**
  - File `docs/adr/ADR-0008.md`, exactly as the ticket names it. The ADR-000N.md series (0001-0010) is the convention
    here, and the 0013+ slug style is not.
  - Status `Accepted`. Decision: polling for enrichment status in P1, SignalR deferred. This matches
    constitution.md:473 and :500, so it is not a departure.
  - The ticket delivers the ADR only. Any polling endpoint, SignalR hub or package would add to what the ticket
    delivers, so it is out of scope.
  - Basis: ticket Scope 2 and AC2 (recon-DEV-319:35,39); recon-DEV-319:132-139.
- P7. **Schema, API, LocalPlay, secrets and `Process.Start` (care items 3-5) are not touched.**
  - Basis: recon-DEV-319:148-150.
- P8. **Existing outcome-family names are out of scope.** `lamuflix.handler.outcome` (TracingDecorator lineage) stays
  as it is. Noted, no ticket.
  - Basis: the ticket does not name it; charter §2.3 item 6.

## Routed to Keel (technical calls, not Patron rulings)

- K1. How consumer results (`completed`, `skipped`, failure paths) map onto `enriched|not_found|failed`. Constraint: P1.
  If no faithful mapping exists without a fourth value, return it to Patron as structural.
- K2. Whether the existing span tag key `enrichment.outcome` (TelemetryConstants.cs:27) is aligned with the metric
  dimension under AC1, and what the C# member names are, so that they do not collide with `EnrichmentOutcome`.
- K3. Histogram unit, measurement point (lookup or whole process) and the `provider` tag value source.
- K4. What one `lamuflix.import.count` increment counts (one per command or one per movie imported), and when
  `failure_category` is present and what its values are (the existing DEV-315 categories).
- K5. Where instruments live and how they are created (static Meter or IMeterFactory), subject to P4 and P5.
- K6. The test approach that proves AC1 (instrument names and dimensions registered).

## Grill log

(Keel questions and Patron answers are appended below; cap 12, then accepted `[assumed]`.)
