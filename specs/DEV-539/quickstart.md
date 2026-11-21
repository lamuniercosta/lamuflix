# Quickstart: DEV-539 Crowd operator and verification

**Authority**: `brief.md` Q3, Q5, Q7; Test and acceptance strategy; closing bar.

## Prerequisites

1. Spec Gate 1 merged; ADR 0048 Accepted (already required before coding).
2. Migration applied (Api startup migration flag or equivalent operator precondition).
3. TMDb configured: `Enabled` true and nonblank `ApiKey` from existing configuration (never paste the key into tickets, code, or evidence).
4. DEV-543 / DEV-544 / DEV-545 available for scoring integration and measurement per brief order.

## Operator backfill

1. Ensure configuration and DI would start normally.
2. Invoke the explicit `LamuFlix.Api` operator mode (exact argv name is an implementation detail within the ticket boundary; document the chosen flag in the delivery PR).
3. Confirm the process runs after configuration/DI and does not start the normal web host for the operator path.
4. Capture console counts: IMDb-eligible, matched, unmatched, skipped, failures.
5. Re-run once to confirm skip behaviour for stored ids.
6. Attach actual counts to the delivery PR.

## Automated verification (local)

1. Integration: WireMock recommendations path + first-page-only through real typed client.
2. Integration: migration on existing movie fixture; backfill refusal/skip/match/fail/repeat.
3. Integration: late library addition activates stored link without re-enrichment.
4. Unit/integration: criteria order, weight 2, denominator, reason strings, score parity (TMDb off; Enabled+blank key; Crowd inactive; Crowd active).
5. Library-only invariant on returned recommendations.

## DEV-545 evidence

1. Capture baseline leave-one-out top-10 hit rate and MRR on the same library/inputs with Crowd inactive/absent as applicable.
2. Capture candidate run with Crowd enabled.
3. Attach before/after hit rate and MRR, coverage table, judged pairs, and the threshold/default decision.
4. If hit rate falls, leave checked-in Crowd default off.

## Size L gates (delivery)

Run and record actual statuses for Roslyn, complexity (including refactor threshold), InspectCode, mutation, property tests, vulnerable packages, format, and tests. Empty web scope → `SKIPPED (scope-empty)`. Failure or `Could not run` blocks.
