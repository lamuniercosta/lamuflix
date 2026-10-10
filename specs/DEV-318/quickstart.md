# Quickstart: DEV-318 Extended Import Scenario

**Date**: 2026-10-09
**Scope**: How the designated owners exercise the proof. No test or gate is run in this
drafting ask; expectations below are for Gauge at build time (brief.md Q6).

## Run the enhanced scenario (Gauge, targeted)

1. Start the dependencies the ApiEndToEnd fixture manages (Postgres, RabbitMQ Testcontainers;
   WireMock in-process stub).
2. Run the extended `ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence`
   scenario in `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs`.
3. Expected: 202 with Location id, Pending observed while gated, Enriched after release,
   GET 200 with full metadata, one measured OMDb request, full trace ancestry proven.

## Run the pipeline gate set (Gauge, full)

Roslyn analyzers, cyclomatic complexity, InspectCode (changed diff, no args), property tests
(recorded opt-out applies to the no-tag exit 2 only), vulnerable packages,
`dotnet format --verify-no-changes`, `dotnet test`, mutation (expected scope-empty SKIPPED),
web gates (expected scope-empty SKIPPED). Scope-empty exit-2 SKIPPED is nonblocking, never
PASS; exit 1 or Could-not-run blocks.

## Diagnose

- Stuck gate: the test names the missing consumer arrival explicitly.
- Missing span: the test names the missing span; capture is disposed in finally.
- Gate release: always in finally before host disposal; no leaked listener or blocking gate.
