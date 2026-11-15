# DEV-472 Assumptions

- [assumed] Q4, Patron-approved operational taste: nightly full Core mutation starts from cron `17 3 * * *` (03:17 UTC) on main. Scheduler/runner availability can delay the actual start. Basis: ticket nightly requirement, recon-DEV-472 lines 4,8-10,14,21; full ruling in CONCLUSIONS.md Q4. No scope or acceptance criterion is assumed.
- [assumed] Plan adjudication (1) & (4), Patron-approved naming/location taste: stable PR diff-scoped mutation job id `mutation-pr` with display/check name `PR mutation`, and hosted runtime evidence ledger path `specs/DEV-472/EVIDENCE.md`. Basis: recon-DEV-472 lines 4, 34 and CONCLUSIONS.md Phase 2 Step 6 rulings.
