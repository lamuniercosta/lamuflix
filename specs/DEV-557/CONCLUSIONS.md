# DEV-557 — Patron rulings (grill with Keel, HEAD e1649d53)

Sources: ticket DEV-557 (`get-task.ps1`), `recon-DEV-557`, task note `DEV-557`, `specs/PRODUCT.md` §5, DEV-551 frozen `specs/DEV-551/plan.md` @ `d115a12e` (branch `feature/551-spec`, not on main).

## Q1 — Shared harness with DEV-551
**Ruling: accept Keel (1), with one forced-edit clause.** DEV-557 consumes the harness DEV-551 lands and defines no rival harness; spec.md names the contract cases and adapter fixtures only; Phase B pickup is blocked until DEV-551 implementation merges to main; Phase B drift check re-verifies harness type names on main and repins the plan to them.
- Ticket *Wanted* 4 ("shares one harness with the subtitle contract suite from DEV-551") and *Needs* ("Run after DEV-551") are ticket text, so the sequencing is already decided — not a §2.3 escalation.
- DEV-551 plan.md (`d115a12e`) freezes only a "shared abstract WireMock suite through real typed HttpClients" and names no generic harness types, so the spec must not hard-code names before main has them.
- If the merged DEV-551 harness is subtitle-bound, extracting the provider-agnostic part (WireMock server lifetime, DI host build, telemetry capture) into a base both suites use is in scope: forced by *Wanted* 4 (charter §2.3 item 6 forced-edit carve-out). Any change to DEV-551's subtitle assertions is not.

## Q2 — Run against `ChainedMetadataProvider`
**Ruling: no** (ticket default).
- Ticket *Open points*: "Default: no. The chain has its own tests for fallback order"; *Out of scope*: changing chain selection.

## Q3 — 401 reporting
**Ruling: accept ticket default.** spec.md records each adapter's current 401 outcome with file:line; if they differ the suite asserts OMDb behaviour, TMDb is brought to it only if the ticket's "reported as a key problem" requires it, and the PR body states the difference.
- Ticket *Open points*, second bullet, verbatim default.

## Q4 — What leaves the adapter classes
**Ruling: accept Keel (4).** Only cases present in both families (lookup, resilience, telemetry — not resilience alone) move to the suite; single-adapter cases (TMDb cancel-during-request, `TmdbSignal`, `Trailer`, `Mapping`, composition and chain-composition tests) stay. Where a twin carries an adapter-specific assertion the contract cannot express, only that residual assertion stays in the adapter class.
- Ticket *Wanted* 3 and *Done when* 2.

## Q5 — Stryker and line count
**Ruling: accept Keel (5), measurement defined.** Stryker before (on base) and after (on head) mutating `OmdbMetadataProvider.cs` and `TmdbMetadataProvider.cs` with identical config/settings; receipts recorded; score must not drop per file. Line-count drop is measured over the 12 recon files (5,072 at e1649d53) **plus** every new DEV-557 contract/fixture file; harness lines that DEV-551 merged are excluded, harness lines DEV-557 adds are counted.
- Ticket *Done when* 3–4; recon-DEV-557 lines 19–32 (12-file baseline).

## Q6 — Charter §2.3 care items
**Ruling: none triggered.** No new dependency (WireMock.Net, xUnit v3 already in IntegrationTests per DEV-551 plan), no new project/layer (lands in existing `tests/LamuFlix.IntegrationTests/`), no schema, no public API, no LocalPlay/secrets/`Process.Start`. Test-file deletes and rewrites are limited to twin-case removal in the named families; edits outside them other than the Q1 harness clause need a new ruling.
- Ticket *Where it lands*; `specs/PRODUCT.md` §5; recon-DEV-557 line 9.
- WireMock key/token values in tests are fake literals only; no real key is committed (scrub rule).

## Q7 — Baseline InspectCode warnings
**Ruling: fix them if `SecondaryMetadataProviderTelemetryTests.cs` is in the diff at all** (not only if the two lines are touched); otherwise leave them, noted, no ticket.
- The InspectCode gate analyses changed files, so any edit to that file surfaces `MethodHasAsyncOverloadWithCancellation` at :98,:102 and fails the gate (task note DEV-557, Gauge baseline) — gate-forced edit. Likely, since telemetry twins leave that file.

Grill closed at 7 questions; no taste item arose, so no `ASSUMPTIONS.md` entry.
