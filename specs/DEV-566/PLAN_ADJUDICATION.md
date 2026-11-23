# DEV-566 plan adjudication

**PLAN FROZEN** · Phase 2 step 6 · 2026-11-23

The plan is ready for the owner’s Gate 1 ruling. The Gate 1 merge receipt, implementation, automated gates, live acceptance and post-merge redeploy are pending. T000 remains open.

Evidence pinned to `feature/566-spec` at `c7d5fb025b3793680a02fdf5f1785553becf9343`. Sources are the DEV-566 ticket note, [ticket text](ticket-text.txt), `recon-DEV-566`, the three findings notes in LamuFlix notes, [brief](brief.md), [Patron rulings](CONCLUSIONS.md), [spec](spec.md), [plan](plan.md) and [tasks](tasks.md). Q1 supplies the closing bar and two-round cap. The decisions below implement Q2–Q8 within the ticket. The ticket and constitution require no structural owner checkbox.

## Risk · Sentry

| Finding | Disposition | Ruling and closure |
|---|---|---|
| H1 | Accepted | T009 requires atomic first-write instance state. T007 verifies exactly one winner under contention and that subsequent writes preserve it (Q3). |
| H2 | Accepted | T011 logs one structured host-only warning on the winning write with the restart requirement. T008 tests the warning. This distinguishes a runtime trip from static invalid options while readiness and the public 409 contract retain Q4 behavior. |
| H3 | Accepted | T015 fixes user-secrets-only ingress, sanitized receipt fields and error handling. T020 reviews all output paths and the receipt shape. T017 records the opt-in evidence (ticket lines 36–41, 53). |
| M1 | Accepted | T004 shares the host matcher between the Core validator and URI policy, preserving existing trim and case-insensitive runtime semantics. T002 tests blank, URL, userinfo, port and trailing-dot expected-host forms alongside valid loopback values (Q2). |
| M2 | Accepted | T010 makes the latch a required dependency of both verified handler guards. T007 and T012 prove each guard blocks before any provider call. Compass F1 resolves the actual wiring (Q2–Q4). |
| M3 | Accepted | T008 and T011 distinguish the provider response link from the file server’s 3xx. One GET to the allowed source, zero to the redirect target and an empty latch preserve the Q3 trigger and redirect protection. |
| M4 | Accepted | T006 proves invalid membership is static-not-ready. T012 proves readiness with valid settings is unchanged by the latch (Q4). |
| M5 | Accepted in part | T006 and T012 explicitly cover sidecar serving, read-only precedence and disabled-provider behavior for static-invalid and latched cases. For read-only precedence, assert the existing outcome. Requiring download success there could change Q4 behavior. |
| L1 | Accepted | T008 and T012 assert one download-API call and zero file GETs on detecting refusal, then T012 asserts zero additional calls on later blocked acquisition (Q3, Q8). |
| L2 | Accepted | T011 preserves defensive parsing and existing refusal behavior for malformed, hostless or non-HTTPS links. T008 exercises malformed and non-HTTPS inputs without tripping the latch (FR-008). |
| L3 | Accepted | T014 asserts both required allowlist entries, exact expected-host equality and no deploy. T013 pins the generated pair (ticket lines 31–35, FR-010). |

## Standards · Ledger

| Finding | Disposition | Ruling and closure |
|---|---|---|
| S1 | Accepted in part | Require narrow wiring and a concrete type with no new port. Compass F1 establishes that both handler guards are the production seams. Adding a parameter to the unused factory would leave acquisition unguarded (Q2–Q4, Principle I). |
| S2 | Accepted | T009 pins dependency-free singleton instance state. T007/T008/T012 isolate state per independent scenario, and T010 checks root and scope identity (Q3). |
| S3 | Accepted in part | T011 uses bounded canonical host data and logs the refusal distinctly. The proposed exception maps to the wrong problem reason per Compass F1. The handler tests assert the right exception, the adapter tests assert its warning, and the endpoint tests assert the existing public response. The operator diagnostic lives in the warning under Q3/Q7. |
| S4 | Accepted in part | T002/T004 cover blank and invalid-allowlist inputs, shared matching, a literal default and existing request-time validation. Ordering of validation message text is discretionary under Q1. |
| S5 | Accepted | T011 introduces no retry or hedging and preserves guard/write/throw order before any file GET. T008 confirms the refused link never reaches a file GET (FR-006/008). |
| S6 | Accepted | T009 and T007 require atomic, idempotent first-write behavior. T011 records a confirmed refusal synchronously before throwing (Q3). |
| S7 | Accepted | T015 specifies script conventions and pending exit status. T001 verifies both skill files’ provenance; T016 derives generated output from its canonical source and requires a cited ruling for any additional file. Both copies carry identical rules (Q6, AGENTS.md). |

## Spec · Compass

| Finding | Disposition | Ruling and closure |
|---|---|---|
| F1 | Accepted | Compass note lines 7–17 pin the production guards and exception mapping at the recon HEAD. Both handlers use `EnsureProviderConfigured` and `SubtitleProviderNotConfiguredException`. The older recon reference to `SubtitleAcquisitionSettings.Require` is superseded for this contract by Compass’s caller and mapping evidence. T003/T006 and T007/T010/T012 use the correct unit, composition and HTTP seams. T006/T012 protect SaveEnabled=false search. |
| F2 | Accepted | FR-001 explicitly requires host-name-only configuration. FR-002 adds membership. T002/T004 now cite the two requirements together, with negative cases even when an invalid string appears in the allowlist. |
| F3 | Follow-up | Default and explicit host coverage belong in the authorized validator suite in T002. Additional binding coverage and README inventory are discretionary under Q1/Q7. If pickup or a gate establishes a mandatory failing check, surface the concrete failure and obtain a cited file ruling before adding a rewrite. |
| F4 | Accepted | T015/T017 use explicit `-SettingsPath` for local JSON generated by the updated home-cluster generator. Both verification and the live check use that generated settings source. The key comes from developer user-secrets (Q5, FR-011, SC-005). |

**Total** · 22 findings, 17 accepted, 4 accepted in part and 1 follow-up. All accepted plan changes are reflected in the brief, plan and task list. No implementation task is checked off.
