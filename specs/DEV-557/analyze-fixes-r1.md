# DEV-557 analyze round 1: fix list for Quill (Keel)

Base: feature/557-spec @ c8a1b696. brief.md now has the new decision bullet "Contract case set" under Plan decisions.
Read-only /speckit-analyze over spec.md, plan.md and tasks.md, checked against brief.md and the constitution. No CRITICAL findings. Coverage: all 9 FRs and 5 SCs map to at least one task.

1. [HIGH, coverage, follows the brief decision] The contract case list leaves out the resilience twin "transient server error then success" (tasks.md:39). It does not map to any *Wanted* 1 case, so it would stay in both families and fail *Done when* 2.
   - spec.md:29 (Scope 1) and FR-001 (spec.md:145): state that *Wanted* 1 is the minimum, and that the suite also covers every twin in the inventory. Name "transient server error then success" as a known extra case.
   - spec.md:108 (US2 scenario 1) and FR-004: a twin with no matching case gets a contract case added. It is never kept as a residual.
   - tasks.md: add a draft twin inventory before T004 (it can be read-only, inside T001 or a new task), and have T004 cover it. T008: if the final inventory finds another unmatched twin, add the case and re-run T005, T006 and T007 green before T009.
   - plan.md:73 (task order step 3): reflect the same rule.
2. [MEDIUM, constitution VIII] Fixture type names: vendor names are allowed only on the Infrastructure adapter class (constitution.md:852-857). The existing families use functional names (`MetadataProvider*` and `SecondaryMetadataProvider*`). Add this constraint to plan.md:93-100 (roles table) and tasks.md T001, T005 and T006: the fixture and contract class names pinned at Phase B MUST be functional, not Omdb*/Tmdb*. "OMDb fixture" and "TMDb fixture" stay as role labels only.
3. [MEDIUM, envelope] plan.md:146 says `MetadataProviderServiceCollectionExtensions.cs` gets "no edit unless Q1 forces harness-adjacent touch". The brief does not authorize any production composition edit: Q1 covers the test harness only. Change it to "read-only reference; no edit".
4. [MEDIUM, coverage] No task produces the PR-body items the brief requires: the removed-test to contract-case table (brief Test strategy) and the Q3 401 difference statement, or "none" (brief Frozen scope). Add them to a task, for example a PR-body step in T014 or a new task before T014: "pr-body-DEV-557 carries the T008 inventory table and the Q3 401 statement".
5. [LOW, ambiguity] FR-003 (spec.md:147) and the glossary (spec.md:74) give the 401 outcome as "key advice / unauthorized recording". The advice text differs per adapter, and the recon lookup assertions differ on health (only the TMDb test asserts unhealthy, recon lines 14-15). State that the expected advice is supplied by the fixture hook, and that the shared assertion is Failed `InvalidResponse` + one request + `wasUnauthorized` recording.
6. [LOW, coverage] FR-008 (fake keys) is mapped only to T004. Also cite it on T005 and T006, where the WireMock stubs and fixture options with keys are written.
7. [LOW, edge case] The OMDb request-limit-body 401 (quota) test is an OMDb-only case. Add it to the stay list in spec.md:31 and T008 so that nobody removes it as a 401 twin.
8. [LOW, wording] spec.md:23 says the ticket is "`ui:none`". The ticket has no `ui` tag (brief line 3). Say "no `ui` tag".

Not a Quill fix (for the Conductor): the `recon-DEV-557` note now holds only the 16-line 401 follow-up. The original recon that CONCLUSIONS Q5 cites ("recon-DEV-557 lines 19-32", the list of 12 files and 5,072 lines) is no longer in the note. The plan's list of 12 files cannot be checked against recon until Wisp restores it. T001 item (4) re-verifies the paths at pickup.
