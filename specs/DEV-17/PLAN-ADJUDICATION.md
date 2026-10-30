# DEV-17 Phase 2 plan challenge — final adjudication

Date: 2026-10-30. Seat: Keel.
Worktree: `F:/Dev/LamuFlix.worktrees/feature-017-spec`; branch verified `feature/017-spec`.

Status: **COMPLETE — all 30 entries dispositioned; no unresolved plan-challenge Critical, High or Medium findings.** This is document adjudication clearance, not Gate 1, implementation approval, a runtime gate pass or merge approval. Plan freeze and Patron provisional Gate 1 remain distinct next actions.

All three reports were read: `findings-DEV-17-Ledger`, `findings-DEV-17-Compass`, `findings-DEV-17-Sentry`. Ledger reports 12 in its header but contains 13 entries (H1, M1–M7, L1–L5); Compass has 5 including Info; Sentry has 12. The disposition table below covers all 30, without counting duplicate cross-axis concerns twice as independent repairs.

Facts: canvas notes `recon-DEV-17`, `recon-DEV-17-2`, and [brief.md](brief.md). Patron approved ten bounded rulings, recorded in [CONCLUSIONS.md](CONCLUSIONS.md) under “Phase 2 plan challenge round 1” (line 153 onward), with assumed conventions in [ASSUMPTIONS.md](ASSUMPTIONS.md). The complete ruling was appended to brief.md at line 143 before the Quill repair request. No ticket-deliverable change, constitution departure or owner structural checkbox was authorized.

## Final dispositions

Paths and lines below are relative to `specs/DEV-17/`, except the ADR.

| Axis / ID | Final disposition | Basis / verified passage |
| --- | --- | --- |
| Ledger H1 | Resolved by explicit blocking prerequisite; hosted tools are not claimed proven | Patron 1; recon-DEV-17-2:9–12; plan.md:21; tasks.md:37 and :108 require pre-coding/pre-PR hosted ffmpeg/ffprobe proof; no skip/filter/workflow edits; unavailable tools stop for recon + Patron |
| Ledger M1 | Corrected | Patron 2; spec.md:100; tasks.md:66, :68, :70; contract:70 — injected TimeProvider, DI/System fallback, controlled timeout proof, no package addition |
| Ledger M2 | Accepted bounded Q7 exception; real-process placement corrected | Patron 2; tasks.md:69 keeps tiny byte stubs only for fake-runner SOI/cleanup in UnitTests; :70 puts real-process runner tests in IntegrationTests; no filesystem abstraction |
| Ledger M3 | Accepted configured mutation boundary; assertions strengthened | Patron 5; tasks.md:69–70; spec.md:104; fractional/culture/discrete-argument/configurable-width/SOI boundaries and stream proofs; Core mutation remains required; no mandatory generator FsCheck suite |
| Ledger M4 | Corrected catalogue; enum/factory proposal rejected | Patron 4; contract:19–42 and :75–82; data-model.md:13, :43; exact closed tokens/mapping/precedence, record signatures preserved, no raw diagnostics/paths |
| Ledger M5 | Rejected with factual evidence | Patron 6; recon-DEV-17-2:15–18 proves existing Hosting.Abstractions reference at Infrastructure csproj:33; plan.md:15 and research.md:25 cite it; no new dependency |
| Ledger M6 | Rejected; Q3 confirmed | Patron 6; ticket recon-DEV-17:84/106 mandates Core port; ADR + MINOR enumeration extension + whitelist follows constitutional procedure, not a departure; plan.md:35 |
| Ledger M7 | Corrected test technique | Patron 3; recon-DEV-17-2:21–25; tasks.md:70 and contract:104 use committed pwsh fixture scripts in existing IntegrationTests, narrow copy entries; missing pwsh fails; no test-executable reentry/custom Main/new project/package |
| Ledger L1 | Corrected | Patron 7; tasks.md:49, :51; T020 has no parallel marker and T020+T022 are one atomic buildable/verified slice |
| Ledger L2 | Corrected ADR editorial | Patron 10; docs/adr/0033-thumbnail-generator-port.md read back in full: durable DEV-17 Scope/AC and repo references, integrated co-location/VI ruling, tokens, TimeProvider, timeout, fixture placement and consumer boundaries; no trailing process edit log |
| Ledger L3 | Noted, no ticket; no DEV-17 fix | Patron 9; duplicate ADR0031 predates DEV-17; recon-DEV-17:163–169; CONCLUSIONS.md records disposition; ADR0033 remains reserved |
| Ledger L4 | Corrected objective completion check | Patron 7; tasks.md:53 requires Thumbnail term, definition and avoid fields in CONTEXT.md; no current glossary state assumed |
| Ledger L5 | Rejected as unsubstantiated corruption | Patron 10; recon-DEV-17-2:35–40 proves strict UTF-8, no BOM/NUL; no encoding rewrite; research.md:43–45 |
| Compass Low-1 | Corrected | Patron 7; tasks.md:107 uses Help-documented invocation and configured refactor threshold; no assumed numeric override/Threshold flag |
| Compass Low-2 | Missing-authoring-task allegation rejected; inclusion wording corrected | Patron 6; recon-DEV-17-2:29–30 proves actual narrow constitution working-tree diff; tasks.md:25 requires Phase A diff/status review and authorized constitution+ADR commit inclusion, harness.yml untouched/unstaged; no duplicate amendment task |
| Compass Low-3 | Corrected ambiguity | Patron 7; spec.md:42, :101; contract:25, :37; data-model.md:13 — missing/unreadable source surfaces through ffprobe as ProbeFailed, no existence precheck |
| Compass Info-1 | Noted, no ticket | Same pre-existing ADR0031 duplicate as Ledger L3; no scope expansion |
| Compass Info-2 | Evidence gap satisfied; inclusion wording corrected | recon-DEV-17-2:29–30 and Patron 6; plan.md:29, :100–103; tasks.md:25 distinguishes present working-tree content from git staging and requires PR inclusion |
| Sentry M1 | Resolved with Ledger H1 | Explicit T010/T061 proof blockers; no claim that CI tools exist, no skipping or CI edit authorized |
| Sentry M2 | Accepted residual risk; future consumer ownership recorded | Patron 9; plan.md:121–129, contract:110–114, ADR:100–102 assign retention/eviction to DEV-428; no retention implementation added here; Rigger reports verified DEV-428 comment |
| Sentry M3 | Accepted residual risk; future consumer concurrency constraint recorded | Patron 9; same DEV-428 ownership passages and verified Rigger comment; no DEV-17 semaphore/new layer |
| Sentry M4 | Accepted configured exclusion with load-bearing proofs | Patron 5; recon-DEV-17:46; tasks.md:69–70, :108 preserve Infrastructure N/A and eligible Core mutation; excluded code never reported mutation PASS |
| Sentry L1 | Corrected install/version receipt; no extra version-pin requirement | Patron 7; tasks.md:83–84 and spec.md:96–97 require one apt update/install/clear layer before USER app and exact version outputs with size delta |
| Sentry L2 | Corrected with Ledger M4 | Closed catalogue/mapping/precedence in contract:29–42, :75–82; no raw diagnostics or file paths |
| Sentry L3 | Corrected timeout accounting | Patron 7; plan.md:25, spec.md:44/:101, contract:26, ADR:69–70; per-call timeout, two calls plus termination/cleanup overhead, no strict total 30-second promise |
| Sentry L4 | Future consumer authorization constraint recorded | Patron 9; DEV-428 owns library-root source authorization in plan/contract/ADR and verified Rigger comment; no endpoint added to DEV-17 |
| Sentry L5 | Corrected trust assumption | Patron 7; spec.md:45/:101; contract:27; ADR:68–69; tool/cache paths are trusted operator configuration, never untrusted user/API input |
| Sentry L6 | Accepted bounded SOI check with real-tool JPEG proof | Patron 8; contract:90, tasks.md:97; minimum two bytes and correct SOI plus required real-fixture proof; no decoder dependency |
| Sentry L7 | Corrected with Ledger M7 | pwsh fixtures never invoke test executable; no recursive xunit reentry; missing pwsh fails; tasks.md:70, contract:104 |
| Sentry L8 | Accepted existing approved ordering | Patron 8 and Q9; tasks.md:92/:97/:120 and plan.md:110 preserve Docker tooling before real-fixture execution |

The additional pre-existing constitution/architecture port-enumeration mismatch in recon-DEV-17-2:31 is separately recorded by Patron as **noted, no ticket** in CONCLUSIONS.md. No unrelated port entry or ADR number was changed.

## Repair and independent passage verification

Quill received one consolidated numbered list (items 1–10), then the final bounded repair round (two omissions); cap 2/2, no third round. Quill repaired only assigned spec artifacts; [DRAFTING_RECEIPT.md](DRAFTING_RECEIPT.md) records its work. Keel independently read the repaired primary/supporting passages rather than relying on that receipt.

The final repair verification read contract:88, tasks.md:69–70 and spec.md:60/:104: production scale uses configured MaxWidth and 320 is only the default test example; runner tests explicitly retain simultaneous stdout and stderr both above cap, complete drain/no deadlock/OutputTruncated, alongside exact-cap boundaries and ordinary capture/lifecycle/cancellation/controlled-timeout proofs. Both passed document verification. ADR0033 was edited by Keel only after its decision was recorded in brief.md, then read back in full.

Rigger reported the DEV-428 comment saved and verified: retention/eviction, queue/endpoint concurrency and library-root source authorization are future consumer constraints. No new ticket or DEV-17 implementation expansion. No tracker writes by Keel.

## Remaining phase boundaries

- Hosted ffmpeg/ffprobe availability remains **unproved**, with explicit pre-coding T010 and pre-PR T061 blockers. This adjudication does not satisfy those checks or dependency landing/pickup drift.
- Plan freeze, Patron provisional Gate 1, Rigger spec PR preparation and owner merge remain separate next actions. Owner structural checkboxes: none.
- No production code, tests or runtime acceptance executed. No analyzer/mutation gate, commit, stage, push, PR publication or merge is claimed by this adjudication.
- Existing harness.yml changes remain outside DEV-17 scope. The actual constitution working-tree amendment is recon evidence, not proof of a staged/committed/landed amendment.

Signed: Keel — Phase 2 plan adjudication complete; all three axes and all 30 entries accounted for.
