# Fakes and Guard Requirements Checklist: Shared service fakes and test egress guard

**Purpose**: Unit tests for English — validate clarity, completeness, and consistency of DEV-549 requirements for shared fixtures, fake mode, egress guard, Live opt-in, and Ollama pin docs before Gate 1.
**Created**: 2026-11-28
**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [tasks.md](../tasks.md)
**Depth**: Standard · **Audience**: Reviewer (Spec PR / Gate 1) · **Focus**: fixture contract, fake-mode isolation, egress/Live safety

## Requirement Completeness

- [ ] CHK001 Are shared fixture folder locations specified for every in-scope service (OMDb, TMDb, OpenSubtitles, canned Ollama embed)? [Completeness, Spec §FR-001]
- [ ] CHK002 Are both host consumers of shared fixtures named (WireMock / `stub.mjs`) with a zero-copy OMDb rule? [Completeness, Spec §FR-002, SC-005]
- [ ] CHK003 Are Compose `fakes` profile and AppHost switch both required as distinct launch paths with distinct container names, fakes-only mount at `/app/appsettings.Production.json` (no Compose env for provider URLs/keys), every-provider wiring, fail-closed missing-wiring naming the host on both launches, and normal Compose/AppHost non-fake baselines preserved? [Completeness, Spec §FR-005–FR-007, SC-002]
- [ ] CHK004 Are guard entry points specified for both `IHttpClientFactory` clients and directly constructed adapter clients? [Completeness, Spec §FR-008]
- [ ] CHK005 Are Live smokes specified as one search-only case per OMDb, TMDb, and OpenSubtitles with independent skips? [Completeness, Spec §FR-011]
- [ ] CHK006 Are Ollama pin surfaces and the retained real `OllamaIntegration` proof both documented as requirements? [Completeness, Spec §FR-012, SC-006]
- [ ] CHK007 Are README topics listed for fixture location, recording/scrubbing, Live/Ollama commands, both fake launches, and host-port constraint? [Completeness, Spec §FR-013]

## Requirement Clarity

- [ ] CHK008 Is the supported WireMock mapping field subset enumerated so “thin reader” is not open-ended? [Clarity, Spec §FR-003, contracts/shared-fixture-mapping.md]
- [ ] CHK009 Is “loopback” defined without DNS (localhost + `IPAddress.IsLoopback` literals only)? [Clarity, Spec §FR-009]
- [ ] CHK010 Is fake-mode credential rule specific (checked-in secret-free settings / placeholders only; no live settings reuse)? [Clarity, Spec §FR-007, Q3]
- [ ] CHK011 Is Live skip messaging required to name the variable and never a secret value? [Clarity, Spec §FR-011]
- [ ] CHK012 Is “Nothing visible to end users” stated with the operator-visible fake-mode exception? [Clarity, Spec §What the User Will See, Q11]

## Requirement Consistency

- [ ] CHK013 Do Wanted/Done-when mappings in `brief.md` Test strategy align with Spec SC-001–SC-006 without dropping a receipt? [Consistency, Spec §Success Criteria]
- [ ] CHK014 Is CI workflow explicitly unchanged while still requiring Live skips without keys? [Consistency, Spec §FR-011, Q5]
- [ ] CHK015 Are host-local controls/faults kept out of `tests/fakes` except where OMDb consolidation forces a move? [Consistency, Spec §FR-014, Q2/Q7]
- [ ] CHK016 Does redirect handling stay test-only and match the Q9 follow-through choice (no `src/` client change), with explicit proofs for relative/absolute Location, 301/302/303 POST→GET, 307/308 method preservation, cross-host Authorization removal, and hop-limit failure? [Consistency, Spec §FR-009–FR-010, Q9]

## Acceptance Criteria Quality

- [ ] CHK017 Can OMDb zero-copy success be objectively verified from a diff of both hosts? [Measurability, Spec §SC-005]
- [ ] CHK018 Can fake-mode “no outside host” be objectively verified from quoted logs or grep? [Measurability, Spec §SC-002]
- [ ] CHK019 Can guard denial be objectively verified with zero sends on a recording transport? [Measurability, Spec §SC-003]
- [ ] CHK020 Is key-present Live acceptance a bounded search pass, with `needs-owner-run` only when a key is unavailable and kept distinct from acceptance (never a substitute pass)? [Acceptance Criteria, Spec §US5, SC-004, Q8]

## Scenario and Edge Coverage

- [ ] CHK021 Are unsupported-matcher failure requirements defined for the Node loader? [Coverage, Spec §FR-003, Edge Cases]
- [ ] CHK022 Are redirect-to-real-host, hop-limit exceeded, factory named/typed, and direct-client helper proofs addressed in requirements? [Coverage, Spec §FR-009–FR-010, Edge Cases]
- [ ] CHK022a Is Live exception confinement proven with a negative recording-transport case that another host/test cannot inherit, without requiring credentials or network? [Coverage, Spec §FR-011, US5]
- [ ] CHK023 Is fake vs live host-port mutual exclusion documented as an intended constraint? [Coverage, Spec §US3, FR-013]
- [ ] CHK024 Are MSW replacement and `OllamaIntegration` fake-replacement explicitly out of scope? [Coverage, Spec §Out of scope]

## Dependencies and Assumptions

- [ ] CHK025 Are Quill-named identifiers (Live env vars, `UseServiceFakes`, hop limit, service names) traced to Wanted/Q patterns rather than silent new scope? [Assumption, plan.md Quill-named identifiers, DRAFTING_RECEIPT]
- [ ] CHK026 Is the “no new NuGet/npm / no new project-layer” disposition explicit for fixture readers and AppHost `AddContainer`? [Dependency, Spec §Out of scope, Constitution VII/IX]
- [ ] CHK027 Are DEV-554–DEV-563 called out as later fixture contributors rather than in-ticket stubs? [Dependency, Spec §Scope]

## Ambiguities and Conflicts

- [ ] CHK028 Does any requirement still say “robust/intuitive” without a measurable bar? [Ambiguity, Gap]
- [ ] CHK029 Do ticket line-number citations vs current HEAD create a conflicting file set, or only drift within the same named files? [Conflict/Assumption, recon-DEV-549 vs plan pickup T001]
- [ ] CHK030 Is production Compose / `deploy/k8s` non-edit boundary consistent across spec, plan, and tasks? [Consistency, Spec §Out of scope]

## Notes

- Generated by `/speckit-checklist` for size L Spec Kit drafting (2026-11-28). Interaction defaults used: Standard depth; Reviewer audience; focus on fixture/fake-mode/guard-Live clusters from grill-closed brief.
- Items are requirements-quality checks only — not implementation verification steps.
- Companion quality gate for the English of `spec.md` remains `checklists/requirements.md`.
