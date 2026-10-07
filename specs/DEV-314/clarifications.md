# Clarifications — DEV-314 (speckit-clarify, 2026-10-07)

Source of truth: `CONCLUSIONS.md` Q1-Q12 rulings (Patron-approved, grill closed) and `brief.md`. This pass asked the settled questions once more against the drafted `spec.md`; every answer below cites the existing ruling. No new decision was made here.

| # | Question | Answer (cited basis) |
|---|---|---|
| C1 | Consumer location and prefetch owner | `src/LamuFlix.Infrastructure/RabbitMq/`, `RabbitMqOptions.Prefetch` — Ruling 1 / Q1, YouTrack receipt DEV-314 note:29-33. |
| C2 | Is an empty production diff acceptable? | Only with deliberate named-test evidence per obligation — Q2. |
| C3 | Trace identity vs resemblance | ParentSpanId, TraceId, TraceStateString pinned; TraceId match alone insufficient — Q3. |
| C4 | Redelivery mechanism | Real broker redelivery; republished retry is not redelivery; synthetic delivery supplements only — Q4. |
| C5 | Scope proof shape | Two-message test with nested scoped handler; distinct instances, async disposal, no root reuse — Q5. |
| C6 | Carrier test extension | Only where tracestate roundtrip is unpinned — Q5/Q6. |
| C7 | Source-edit trigger | Failing Q2-Q5 obligation test cited per edit; both named files authorized — Q6. |
| C8 | Architecture grants | None: no new dependency/project/layer/schema/API/LocalPlay/secrets/glossary — Q7. |
| C9 | Gate labels and thresholds | Exit-2 scope-empty SKIPPED / disabled SKIP / all-excluded N/A are never PASS; thresholds from harness config plus stricter refactor gate — Q8/Q9. |
| C10 | Ordering and routing | Spec PR merge opens Gate 1; Phase B drift-first; Anvil new tests, Cog source changes, Gauge gates — Q9. |
| C11 | Review caps | Two rounds, two fix commits per round; exhausted blockers stay visible — Q10. |
| C12 | ADR and Gherkin | ADR 0019 proposed, frozen with spec PR; no Gherkin/Reqnroll stage — Q11/Q12. |

Open `needs decision` items for Keel: none. No gap was found that the brief and conclusions do not already decide; per the ask, any future gap becomes `needs decision: <question>` to Keel and is never filled here.
