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

### Keel batch 1 (HEAD 21be958): K1-K6 settled by Keel, Q1-Q4 for Patron. 4 of 12 questions used.

Keel's technical answers (summary; Keel's brief.md is the full record):
- K1: The outcome counter is emitted in ProcessEnrichmentCommandHandler from the actual lookup result.
  Found -> enriched, NotFound -> not_found, provider Failed or a classified exception -> failed. There is one
  increment per lookup attempt, retries included. Refused claims and cancellation emit nothing. Persistence,
  republish and malformed-body errors are not lookup outcomes.
- K2: The span key `enrichment.outcome` and its `completed`/`skipped` values are preserved. The new constants use
  distinct member names, and every ticket literal stays exact.
- K3: `Histogram<double>` with unit `s`. It measures the whole OmdbMetadataProvider lookup through `TimeProvider`
  and records once, in `finally`. The tag is `provider=omdb`.
- K4: `import.count` is a `Counter<long>` with no tags. It does Add(1) after SaveChangesAsync succeeds and before
  EnqueueAsync. `failure_category` appears only when the outcome is `failed` and takes the closed Category.Code set
  `provider_unavailable|rate_limited|invalid_response|unknown`.
- K5: One static BCL `Meter` named `ActivitySourceName` holds the three instruments, in a concrete static class at
  `src/LamuFlix.Core/Pipeline/EnrichmentMetrics.cs`. There is no interface, port, package, project, DI registration or
  factory.
- K6: Tests use a test-only `MeterListener` and run in non-parallel collections. They follow the existing real
  Postgres/RabbitMQ/WireMock conventions and extend the host OTLP composition proof. The existing span and attribute
  assertions are kept.

Patron rulings:
- Q1. **Ratified.** The outcome counter counts lookup attempts. No-op claims and cancellation emit no outcome.
  - Basis P1: the value set is frozen at three. A `skipped` value would be a fourth value, which is structural.
  - Translating `completed` to `enriched` would be wrong. AC1 asks for consistent dimensions, so the metric is
    emitted where the result is actually known.
  - No fabricated `failure_category` for non-lookup errors. P1 freezes the dimensions.
- Q2. **Ratified.** `import.count` counts persisted movies, including the case where the enqueue fails after the save.
  - Basis: the ticket names only `lamuflix.import.count (Counter)` (recon-DEV-319:32) and no unit of count, so the
    meaning is a technical call (K4).
  - Sound practice: the counter reflects durable state. A retried command cannot re-insert, so counting commands would
    misstate imports.
  - The cost Keel names (a 500 response can still increment the count) is accepted. Noted, no ticket.
- Q3. **Ratified.** The span semantics are preserved and the static holder at `Core/Pipeline/EnrichmentMetrics.cs` is
  approved.
  - P2 and P8: the existing span keys and values are unchanged. P3 is satisfied by emitting from the handler, so no
    consumer edit is needed.
  - P4 and P5: a concrete static class inside an existing folder is file organization, not a new layer, port or
    abstraction. The BCL `System.Diagnostics.Metrics` needs no PackageReference, and ArchitectureTests.cs:65 (no
    OpenTelemetry in Core) still holds.
  - Condition: `TimeProvider` (K3) is BCL. If OmdbMetadataProvider needs a new constructor parameter, that is a forced
    AC1 edit. Any new package or port still comes back under P4 and P5.
- Q4. **Ratified as the closing bar for this chain.** Critical, High and unmet ticket AC block. Lower findings are
  disposed of or filed as follow-ups. The frozen scope is P1-P6. Out-of-scope work becomes a follow-up issue, not a
  finding. The caps: review at most 2 rounds, at most 2 fix commits per round, grill at most 12 questions. Property
  tests opt out (pure instrumentation of existing behaviour, no new domain invariant). Phase A runs no executable
  gates. Phase B runs every applicable gate, mutation included, with no threshold changes.
  - Basis: charter §2.3 (scope); task-pipeline caps as Keel cited them. Patron did not re-read task-pipeline, so that
    citation rests on Keel's read.
  - Standing for this chain; cited once, not re-ruled per ticket.

No structural blocks. The remaining questions go to `brief.md`, and Keel closes the grill when the brief is written.

---

### Full decision questions and confirmation - Keel batch 1

The full settled K1-K6 technical answers are preserved in brief.md, sections K1-K6, together with every seeded and ratified answer. The questions sent by Keel were:

Q1: Ratify lookup-attempt counting and omission of no-op/cancellation outcomes above? I recommend it because the frozen three values cannot faithfully represent skipped delivery; adding a fourth value would be structural. It also avoids pretending completed means enriched.

Q2: Ratify import.count as persisted movies, including an enqueue failure after save, rather than successful commands? I recommend it because the movie already exists and a retry may fail uniqueness; cost is that a 500 response can still increment imports.

Q3: Ratify preservation of existing span outcome semantics and the concrete Core static instrument holder? I recommend this to honor P2/P4/P5 without a new dependency, port or change to ProcessEnrichmentOutcome public shape. Instrumentation belongs in the handler where result distinctions exist, so no consumer emission edit is needed.

Q4: Ratify closing terms: Critical/High and unmet ticket AC block; lower findings are explicitly disposed/follow-up. Frozen scope is the three metrics and literal constants, emission/tests required by AC1, preservation/verification of named spans and attributes, Accepted docs/adr/ADR-0008.md only; anything else is a follow-up issue, not a finding in this round. Review cap two rounds; remediation max two fix commits per round; grill cap twelve per task-pipeline. propertyTests: opt-out because these additions instrument existing behavior and introduce no new domain invariants; existing property tests still run. Phase A no executable gates; Phase B all applicable pipeline gates, including mutation, retain script verdicts with no threshold changes.

Patron confirmation received by Keel:

> DEV-319 Q1-Q4 all ratified with no structural block; 4 of 12 questions used. Recorded at specs/DEV-319/CONCLUSIONS.md (Grill log, commit c63ef06). The Q3 condition: TimeProvider is a forced AC1 edit, and any new package or port still comes back to me. Proceed to brief.md, and send me grill-closed when it is written.

Rationale, cited basis and implications for every answer are the preceding Patron rulings and the complete frozen brief. The conclusions and brief now persist the exchange; the resolved grill may be summarized without losing its decisions. No further product or scope question remains. Next owner is Quill through Bernstein; plan challenge and Gate 1 remain pending.
