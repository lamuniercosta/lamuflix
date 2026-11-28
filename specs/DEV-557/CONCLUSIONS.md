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

## Plan challenge adjudication (Keel, step 6, HEAD c8a1b696)

Inputs: findings-DEV-557-Sentry (8), findings-DEV-557-Ledger (10), findings-DEV-557-Compass (0, clean). All three Size M axes present. 0 Critical, 0 High. No ticket-text change and no constitution departure, so no owner checkbox. Decision changes recorded first in `brief.md` "Plan challenge decisions" P1-P11.

| Finding | Ruling | Basis | Fix item |
|---|---|---|---|
| Sentry F1 harness capabilities | Accept (P4) | brief Test strategy; FR-001; Q1 | 1 |
| Sentry F2 isolation / timing | Accept (P5) | plan Technical Context no invented SLA; FR-001, FR-006 | 2 |
| Sentry F3 draft inventory all families | Accept (P6) | brief Contract case set; ticket Wanted 3, Done when 2 | 3 |
| Sentry F4 ordinary-401 stub excludes quota body | Accept (P7) | FR-003; recon OMDb :414, :452-455 | 4 |
| Sentry F5 cancel twin vs stay | Accept (P6), checklist line only | Q4 | 3 |
| Sentry F6 harness line attribution | Accept (P3) | Q5 | 5 |
| Sentry F7 Stryker receipt completeness | Accept, merged into P8 | Q5; pipeline mutation honesty | 6 |
| Sentry F8 Q7 likely | Noted, no change; plan already orders it | Q7 | none |
| Ledger M1 OMDb in Q3 carve-out | Accept (P1) | Q3 brings TMDb to OMDb only; Q6 | 7 |
| Ledger M2 stay files editable | Accept (P2) | Q4 names them stay; Q6 | 8 |
| Ledger M3 open-ended DEV-551 extraction | Accept (P3) | Q1 forbids subtitle assertion changes; Q6 | 5 |
| Ledger M4 mutation gate handling | Accept (P8) | Q5; Done when 3 | 6 |
| Ledger M5 repin ownership | Accept (P9) | Q1 repin; charter commit format | 9 |
| Ledger L1 line-count comparator | Accept (P10) | brief ordering step 2 | 10 |
| Ledger L2 inventory location | Accept (P11) | FR-004 order | 11 |
| Ledger L3 commit format | Accept (P11) | charter commit rule | 11 |
| Ledger L4 story labels | Accept, cosmetic note | Spec Kit tasks format | 12 |
| Ledger L5 expected Q3 outcome none | Accept (brief Files touched) | recon OMDb :452-455, TMDb :537-540 | 7 |
| Compass | Clean, nothing to adjudicate | — | none |

Ledger M4 cites `run-mutation.ps1` SKIPPED/exit 2 and maxKillShare 50% / killFileSpan 15. The ruling does not depend on those values: Gauge reports whatever verdict and kill-share/span lines the script prints. Fix list: `specs/DEV-557/plan-challenge-fixes-r1.md` (12 items, routed to Quill via Bernstein). Plan freezes after Quill applies the list and Keel verifies it.

## Plan freeze (Keel, step 6 verify)

Quill applied fix list r1, 12/12; Keel verified each item read-only. `/speckit-analyze` result: 0 CRITICAL, 0 HIGH, 0 MEDIUM, 2 LOW; FR 9/9 and SC 5/5 covered; private IMPL_PLAN/TASKS byte-identical to canonical. The two LOWs, both non-blocking, are deferred to the Phase B repin commit (names and paths only): I1, plan.md Scale/Scope cites 5,072 without "historical context only" (P10 comparator is correct elsewhere); U1, the plan.md Documentation tree omits `twin-inventory.md`. **PLAN FROZEN** at the commit that adds this section.

## Gate 1 (Patron, step 7, frozen HEAD 808063d7)
**Ruling: gate1: provisional; no owner checkbox.** Spec, plan and tasks are ready for the Spec PR.
- `/speckit-analyze` 0 CRITICAL/HIGH/MEDIUM, FR 9/9, SC 5/5; plan challenge adjudicated (Sentry/Ledger/Compass, table above), fix list r1 12/12 landed (task note DEV-557 lines 14-20). Two LOWs (I1, U1) deferred to the Phase B repin, names only.
- User-visible outcome: spec.md states "Nothing visible" with reason (no `ui` tag); no checklist required.
- No ticket-text change and no constitution departure (Q6, adjudication header), so neither §2.3 escalation applies.
