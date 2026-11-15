# DEV-471 Quill repair round 2 (final)

[from Keel] Same worktree/branch and ownership as round1. No other edits, commits, freeze or Gate1 declaration. Return receipt by maestri ask Keel. This is final repair round (2/2).

1. Medium ordering/ownership defect, tasks T002/T004/T005 + FR012: runner/development proof uses single config before phase3 T005 creates it. Brief now records exact resolution: T002 creates web/e2e/fixtures/dev471/budgets.json before first runner use; T005 owns media/fixture settings excluding budgets.json, disjoint file sets, stays parallel after T001. Synchronize tasks diagrams/descriptions/dependencies, plan implementation order, spec config path if useful, and DRAFTING_RECEIPT FR012 pointers. Preserve approved values, one source, no new dependency. Do not add T002 dependency on T005 or a cycle.
2. Low provenance defect: spec Scope1 says existing e2e-smoke is a distinct required-check context. Recon confirms a job, no required registration. Replace with distinct coexisting check/context; reserve required for compose-smoke ticket requirement and actual operator verification. Search only Quill-owned artifacts for the same assertion; preserve correct wording elsewhere. No runtime/protection PASS.

All other round1 repairs verified against brief. Both accepted gaps must close before freeze; no further repair rounds will be sent.
