# DEV-565 plan adjudication

**Status** — PLAN FROZEN. Ready for Gate 1 ruling. The Q9 owner checkbox remains open.

**Scope** — Phase 2 step 6, `feature/565-spec` in `F:/Dev/LamuFlix.worktrees/feature-565-spec`, at base `15c1e43af7ba6c3d5f37efbabd4a313164588932`. Review inputs are the worktree planning artifacts and the reports in LamuFlix notes.

**Coverage** — 24 dispositions, covering all 9 Sentry, 8 Ledger and 7 Compass findings. Dispositions total 19 accepted, 4 partial and 1 declined. Partial dispositions resolve the in-scope portion and record the boundary of the remainder.

| Finding | Severity | Disposition | Basis and resulting change |
| --- | --- | --- | --- |
| Sentry F1 | High | Accepted | Use inert parity fixture values and omit legacy `.env` assignment/interpolation syntax. Verify gitleaks against the final changed files with the fixture present. Q5, Q7 and recon 41, 66–69. T008/T009. |
| Sentry F2 | High | Partial | Q2/Q7 fix the generated default and require the operator to reapply `false` to the published settings. Automatic retention, cluster reads, and provisioning abort logic exceed that ruling. Make the post-publish override and pre-rollout verification explicit in README and collect redacted procedure evidence in T012. T005/T010/T012. |
| Sentry F3 | Medium | Partial | Document key-or-opt-out before cutover. Q2 and ticket line 34 establish retryable behavior. Record relevant redacted observations if encountered. Retry/log-policy investigation and adapter changes are outside the Q6 evidence bar and ticket line 39. T010/T012. |
| Sentry F4 | Medium | Partial | Tell fresh Compose operators to create the file from the example before startup. Record actual missing-mount behavior in isolated Compose verification. Q7 authorizes the channel cutover, with no new boot-failure requirement. A verified ticket-forced fix needs a cited Patron ruling. T010/T011. |
| Sentry F5 | Medium | Accepted | Q6/Q8 and FR-011/SC-006 require both live proofs for delivery closure. A pending receipt blocks implementation merge clearance. Status can be recorded and review can proceed while proof is pending. T012–T014. |
| Sentry F6 | Low | Accepted | Both live receipts record that rollback inputs were retained through verification. Q3 keeps Secret cleanup manual and post-verification. T012. |
| Sentry F7 | Low | Accepted | README directs operators to keep keys fixed during rollout and verify after rollout completion. No assumption about mixed-pod precedence is needed for that sequence. Q3. T010. |
| Sentry F8 | Low | Accepted | Run smoke suites serially per worktree and record the actual E2E source and container destination from credential-redacted mount evidence. Keep the runner read-only under Q6/Q7. Recon 76–80. T011. |
| Sentry F9 | Low | Partial | Absent or blank input retains the empty template placeholder under Q2. Cover the blank output in home tests and surface enabled/blank behavior in README. No new trimming of supplied credentials or provisioning diagnostics. T003/T005/T010. |
| Ledger S1 | Medium | Accepted | T001b reviews dependent templates and docs, records the result in the Sync Impact Report, and stops for a cited Patron ruling before any required edit beyond the Q9 allowlist. Governance 949–960 and Q7/Q9. |
| Ledger S2 | Medium | Accepted | Use a MAJOR bump under Governance 957 because Q9 replaces the required transport for the affected containerized deployments and the old deployment config requires migration. Use the landed base version. Recheck Q9’s cited ADR triggers and record no new or superseded ADR in the Sync Impact Report. T001b. |
| Ledger S3 | Medium | Accepted | Use inert fixture values and no new allowlist by default. T009 verifies the rewritten tests and fixture in the final diff after obsolete exception removal. Escalate any verified need for a new exception to Patron. Same repair as Sentry F1. |
| Ledger S4 | Low | Declined | Q5 specifies four exact parsed API env names. A prefix or wildcard rule could reject unrelated settings and expands provider coverage beyond this ticket. Extend deliberately when another provider comes into scope. T008. |
| Ledger S5 | Low | Accepted | Keep the existing file and assertion conventions, use repo-standard names and AAA headers, and structurally parse JSON. Disk reads of tracked fixtures are the existing pattern (recon 43, 48). T002/T003. |
| Ledger S6 | Low | Accepted | The one-time migration note lives in README. Q3 requires it outside provisioning, and T005 covers code changes only. T005/T010. |
| Ledger S7 | Low | Accepted | Keep pipeline coordination tasks as explicit prerequisites and review handoffs. Consolidate repeated status prose into the brief and reference it at the relevant gates. T000/T001/T014. |
| Ledger S8 | Low | Accepted | Keep known-red focused-test commits local through the T006 green rerun, and use `DEV-565 - {subject}` commit messages. Q7 atomic delivery and repository commit rules. T002/T003/T006. |
| Compass F1 | Medium | Accepted | Make T003 an explicit T009 prerequisite and verify both rewritten tests and the negative fixture with gitleaks. Recon 39, 41, 44. T009. |
| Compass F2 | Medium | Accepted | Recon 74 establishes the smoke phases only. Assign the missing provider-config wiring fact to Wisp’s fresh T001 pickup, including both phases. Keep the script read-only and require a cited Patron ruling for a verified ticket-forced edit. Q6/Q7. T001/T011. |
| Compass F3 | Low | Accepted | T003/T005 pin the generated missing/blank representation as an explicit empty `Tmdb:ApiKey`, preserving the Q2 template placeholder. Present keys remain supplied values. No public contract changes. |
| Compass F4 | Low | Accepted | After each publish, an opting-out operator updates the `Tmdb:Enabled` member inside the published `api-settings` JSON to `false`, verifies the effective setting without printing credentials, then rolls/restarts the API. Q3/Q7. T005/T010/T012. |
| Compass F5 | Low | Accepted | README-only migration note under Q3. Same placement as Ledger S6. T005/T010. |
| Compass F6 | Low | Accepted | Expand both shorthand sequences to name every task: T007 → T008 → T009 and T010 → T011 → T012. |
| Compass F7 | Low | Accepted | Base the negative case on passing inputs and assert the specific provider-key parity rejection using the runner’s existing expected-output support, established in recon 66–67. Verify both deployment guards. T008. |

## Handoff

- The spec PR carries the open Q9 checkbox. T000 verifies owner approval and the spec PR merge before Build. Principle VII remains a current conflict until the approved T001b amendment and compliance recheck.
- T001 recon must establish the provider settings inputs for both Compose smoke phases. A verified ticket-forced edit outside the file boundary needs a cited Patron ruling.
- The implementation merge bar requires both credential-redacted live enrichment receipts and the applicable Q8 gates. Runtime acceptance is pending.

