# Stream Conversion Requirements Checklist: Browser-unsupported audio stream conversion

**Purpose**: Unit tests for English — validate completeness, clarity, consistency, and coverage of DEV-572 stream-conversion requirements (not implementation verification)
**Created**: 2026-11-26
**Feature**: [specs/DEV-572/spec.md](../spec.md)
**Depth**: Standard
**Audience / timing**: Reviewer (Spec PR / Gate 1)
**Focus**: Conversion/raw dispatch; seek/duration/abort; cleanup/trust boundary; proof bars

## Requirement Completeness

- [ ] CHK001 Are AC1–AC8 from `recon-DEV-572` lines 31–39 each mapped to at least one FR or SC? [Completeness, Spec §Requirements / Success Criteria]
- [ ] CHK002 Are probe failure, successful probe with no audio, and known conversion failure distinguished with explicit outcomes? [Completeness, Spec §FR-006 / FR-009, Edge Cases]
- [ ] CHK003 Are Compose and home-cluster proof/acceptance evidence requirements both documented, preserving pre-build cluster tool/scratch/one cold timing vs post-build full playback bars? [Completeness, Spec §FR-013 / FR-015]
- [ ] CHK004 Are subtitle, subtitle-delay, and existing control parity required for Compose pre-build proof (Q2) and enumerated again in post-build FR-015 integrated acceptance on both deployments? [Completeness, Spec §US4 / FR-013 / FR-015; brief Q2/Q6]

## Requirement Clarity

- [ ] CHK005 Is “within a few seconds” seeking quantified with the Patron 3 s bar? [Clarity, Spec §SC-003, brief Q6]
- [ ] CHK006 Is cold preparation capped with a numeric threshold and reopen path? [Clarity, Spec §FR-014 / SC-005]
- [ ] CHK007 Is the raw-path cold probe allowance quantified (300 ms / Q6 bar f) and surfaced as a Spec PR **note** (not an owner checkbox), matching `spec.md` Owner question and T000? [Clarity, Spec §Owner question / SC-004 / FR-014]
- [ ] CHK008 Is converted Content-Type explicitly named as `video/mp4`? [Clarity, Spec §FR-002]

## Requirement Consistency

- [ ] CHK009 Do Q1 route-unchanged rules align with FR-001 and the out-of-scope “no new protocol” list? [Consistency, Spec §Scope / FR-001]
- [ ] CHK010 Does “completed asset survives Range abort” stay consistent between US3, FR-008, and US5 cleanup scenarios? [Consistency, Spec §FR-008 / US3 / US5]
- [ ] CHK011 Does LocalPlay non-interference appear consistently in Scope, FR-012, and edge/test requirements? [Consistency, Spec §FR-012 / FR-016]
- [ ] CHK012 Is library read-only consistent with scratch isolation and Q5 trust detail (authorize/resolve before child; server-resolved paths + generated scratch filenames; shell disabled; no secrets)? [Consistency, Spec §FR-008 / FR-010 / FR-012; brief Q5]

## Acceptance Criteria Quality

- [ ] CHK013 Can audible playback success be objectively observed on the named real samples (959, AC3, DTS)? [Measurability, Spec §SC-001 / SMOKE-001]
- [ ] CHK014 Can video stream-copy vs re-encode be objectively checked (metadata) without inventing a second delivery model? [Measurability, Spec §SC-002]
- [ ] CHK015 Are cleanup success criteria stated as absence of leftover processes and partial files after named fault cases? [Measurability, Spec §SC-007]

## Scenario Coverage

- [ ] CHK016 Are primary converted playback, raw supported-codec, seek/abort, subtitle/control, and cleanup scenario classes all present? [Coverage, Spec §User Scenarios]
- [ ] CHK017 Are denied/missing/invalid id behaviours specified as no-process current responses? [Coverage, Spec §Edge Cases / FR-010]
- [ ] CHK018 Is concurrent sharing of one in-flight conversion per source version specified, including the 1 s same-source reattachment grace and immediate failure cleanup? [Coverage, Spec §FR-007; brief Step 6 R1]

## Edge Case Coverage

- [ ] CHK019 Are source-version changes (path/size/mtime) addressed for cache and scratch invalidation? [Edge Case, Spec §Edge Cases / FR-005 / FR-008]
- [ ] CHK020 Is eviction-while-reader-held explicitly forbidden? [Edge Case, Spec §FR-008 / US5]
- [ ] CHK021 Are missing-ffprobe and incompatible stream-copy stop conditions documented as return-to-ruling, not silent workarounds? [Edge Case, Spec §Edge Cases / FR-013]

## Non-Functional / Trust

- [ ] CHK022 Are Q5 Process.Start trust rules specified (authorize/resolve before child; structured args; shell disabled; server-resolved paths plus generated scratch filenames only; no secrets; no library writes; denied/missing/invalid → no process)? [NFR, Spec §FR-010 / FR-012; brief Q5]
- [ ] CHK023 Is the prohibition on reading/changing `Features:LocalPlay` explicit? [NFR, Spec §FR-012]
- [ ] CHK024 Are Size L review axes, round cap, and blocker routing specified? [NFR, Spec §FR-017]

## Dependencies & Assumptions

- [ ] CHK025 Are the four scratch defaults (concurrency, idle expiry, size bound, directory) present and tagged `[assumed]` in both `ASSUMPTIONS.md` and `plan.md`, with no invented conversion-child timeout floor? [Assumption, Spec §Assumptions / plan Assumed scratch defaults]
- [ ] CHK026 Is reuse of existing ffmpeg install / runner called out without adding a package dependency? [Dependency, Spec §Dependencies / FR-011]
- [ ] CHK027 Is production `web/` non-edit (unless return ruling) documented? [Dependency, Spec §Out of scope / FR-012]

## Ambiguities & Conflicts

- [ ] CHK028 Does any FR still use unquantified adjectives (“fast”, “few”, “reasonable”) without a numeric bar or cited Patron reading? [Ambiguity]
- [ ] CHK029 Does the spec conflict with ticket AC1–8 text captured in `recon-DEV-572` lines 31–39? [Conflict]
- [ ] CHK030 Is the .mkv content-type note explicitly out of scope so it cannot be treated as an in-round finding? [Boundary, Spec §Out of scope]

## Notes

- Generated by `/speckit-checklist` in the one-ask Spec Kit run (Conductor Phase 2 step 3.2). Defaults: Standard depth; Reviewer audience; focus clusters = conversion/raw dispatch, seek/abort, cleanup/trust, proof bars.
- Items test requirement writing quality for Gate 1 review — they are not runtime test cases.
- Analyze repair round 1 refreshed CHK004 (FR-015 + Q2 pre-build parity), CHK007 (300 ms Spec PR note), CHK012/CHK022 (Q5 trust detail), and CHK025 (four assumed scratch defaults only).
- Step 6 plan-challenge repair: Core port ADR is `docs/adr/0050-browser-stream-audio-conversion-port.md` (Proposed now; Accepted in Build PR with Principle I 2.0.0 → 2.1.0 and matching ArchitectureTests whitelist). CHK018 carries the 1 s reattachment grace. Spec Kit PR remains specs-only.
