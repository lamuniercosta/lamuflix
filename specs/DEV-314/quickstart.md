# Quickstart — DEV-314 Phase B validation

Run after the spec PR merges (Gate 1 open) in `F:/Dev/LamuFlix.worktrees/DEV-314`, with `$env:PYTHONUTF8='1'`.

1. Fresh recon + drift check against main; re-run analysis; reconcile drift before work (Q9).
2. Map actual assertions of the 12 existing tests to Scope 1-3/AC1-2; record exact locations; unproven rows stay work.
3. Strengthen the producer-trace test and add the redelivery + two-message scope tests per `contracts/trace-contract.md`.
4. Run focused tests; run temporary negative controls per obligation (break, observe failure, restore, record).
5. Fix `EnrichmentConsumer.cs`/`TraceContextCarrier.cs` only where a failing obligation test cites the defect.
6. Refactor changed code only (stricter refactor gate); run full delivery gates on diff/head; never `-All`, never `--since`, never lowered thresholds.
7. Three-axis review (Sentry/Ledger/Compass), PR comments, two-round/two-commit caps, merge bar; user merges.
