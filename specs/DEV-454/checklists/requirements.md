# Specification Quality Checklist: Play existing sidecar subtitles in the browser

**Purpose**: Validate specification completeness and quality before Gate 1 / planning freeze
**Created**: 2026-11-09
**Updated**: 2026-11-09 — plan-challenge repair round 2 (final allowed) applied; **blocked: structural** (Ledger S4). Items below remain **unresolved pending Keel's independent reread**. PLAN FROZEN withheld; Gate 1 closed pending **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) plus verified merge.
**Feature**: [spec.md](../spec.md)

## Content Quality

- [ ] Stakeholder scenarios and smoke criteria remain readable without requiring implementation literacy *(pending Keel reread after plan-challenge repairs)*
- [ ] Focused on user value and business needs *(pending Keel reread)*
- [ ] User stories and visible outcomes are written for non-technical stakeholders *(pending Keel reread)*
- [ ] Ticket/Patron-mandated technical contracts (recognition table, encoding/WebVTT rules, two routes/DTO/statuses/headers, viewer Off policy, dependency STOP rules, bounded files, C1 handlers, C2 CONTEXT append, ProblemDetails, port enumeration extension, LibraryOptions bounds, token derivation, handler/transport ownership, viewer concurrency/list-error, cue-boundary policy, constitution v1.13.0 proposal) are explicit and traceable in FR/plan/tasks rather than omitted for template-generic agnosticism *(pending Keel reread)*
- [ ] All mandatory sections completed *(pending Keel reread)*

## Requirement Completeness

- [ ] No [NEEDS CLARIFICATION] markers remain outside the explicit unticked Ledger S4 owner checkbox *(pending Keel reread)*
- [ ] Requirements are testable and unambiguous *(pending Keel reread)*
- [ ] Success criteria are measurable and outcome-oriented *(pending Keel reread)*
- [ ] Success criteria stay stakeholder-readable while preserving explicit, traceable ticket/Patron-mandated contracts (not rewritten to erase those contracts) *(pending Keel reread)*
- [ ] All acceptance scenarios are defined *(pending Keel reread)*
- [ ] Edge cases are identified *(pending Keel reread)*
- [ ] Scope is clearly bounded *(pending Keel reread)*
- [ ] Dependencies and assumptions identified *(pending Keel reread)*

## Feature Readiness

- [ ] All functional requirements have clear acceptance criteria *(pending Keel reread)*
- [ ] User scenarios cover primary flows *(pending Keel reread)*
- [ ] Feature meets measurable outcomes defined in Success Criteria *(pending Keel reread)*
- [ ] Mandated contracts remain explicit and traceable; stakeholder scenarios remain readable *(pending Keel reread)*

## User-Visible Outcome

- [ ] "What the User Will See" names each visible change by page or component, or says "Nothing visible" with a reason *(pending Keel reread)*
- [ ] Each visible change has a SMOKE-### check in user terms that can run against the Compose stack *(pending Keel reread)*
- [ ] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services) *(pending Keel reread)*

## Requirement Clarity & Consistency (requirements quality)

- [ ] CHK001 Is same-directory exact stem matching frozen for bare/language/default/unknown suffixes including `-pob`, with no fuzzy/recursive/acquisition behaviour, via authorized `ILibraryFileReader` enumeration extension (not reuse-only fiction)? [Completeness, Spec §FR-001–FR-002 / Q1 / Ledger S1] *(pending Keel reread)*
- [ ] CHK002 Are sort order, duplicate counters, basename-only unknown labels, and default-marker-as-sort-hint (never auto-enable) specified? [Clarity, Spec §FR-002–FR-003 / Q4 / Q7 / ASSUMPTIONS] *(pending Keel reread)*
- [ ] CHK003 Are both subtitle routes, SubtitleTrackDto fields, opaque movieId-NUL-filename SHA-256 base64url tokens (fresh allowlist, collision fail-closed, no secret/path-decode), headers including private no-store on errors, Range→full 200, and 403/404/422/500 statuses frozen? [Completeness, Spec §FR-004–FR-005 / FR-017 / Q3 / Q7 / Sentry F2/F8] *(pending Keel reread)*
- [ ] CHK004 Are UTF-8-then-Windows-1252 decode (conditional registration), UTF-16/32 BOM rejection, WebVTT grammar, cue-boundary fixtures preserving valid NOTE/STYLE/REGION words, sanitized 422 only for actually invalid/unrepresentable structure, listing isolation, and successful other-track content specified? [Completeness, Spec §FR-006–FR-007 / Q2 / Q7 / Sentry F5] *(pending Keel reread)*
- [ ] CHK005 Are reauthorization, fresh token allowlist match, adapter-owned safe open with unproven race STOP, traversal/symlink/non-regular rejection, and LocalPlay/Process.Start/secrets isolation required? [Security, Spec §FR-008 / Q5 / Sentry F3] *(pending Keel reread)*
- [ ] CHK006 Is the C# → OpenAPI → generated TypeScript chain required with no handwritten duplicates and no movie-detail DTO change? [Consistency, Spec §FR-009 / Q3] *(pending Keel reread)*
- [ ] CHK007 Are Off-initial selector, single showing track, empty hide vs list-error Retry, text-only hostile labels, served ordering, generation guard, URL cleanup, and `Subtitles unavailable` failure/retry behaviours specified? [Completeness, Spec §FR-010–FR-011 / Q4 / Sentry F4/F6/F7 / Compass F3 / ASSUMPTIONS] *(pending Keel reread)*
- [ ] CHK008 Are bounded edit surfaces (sealed subtitle queries/handlers + thin Api transport, existing-port enumeration, LibraryOptions bounds, ADR0037 accuracy update, HandlerRegistration, conditional GlobalExceptionHandler after amendment) and untouched Stream/local-playback files stated, with no new package/port/project/layer/schema/ADR number? [Boundary, Spec §FR-012 / Q6 / C1 / Phase 2 step 6] *(pending Keel reread)*
- [ ] CHK009 Are DEV-455/456 + encoding capability + safe-open platform + approved constitution-text Phase B STOP checks required before implementation? [Dependency, Spec §FR-013 / FR-020 / Q5–Q6 / Q8 / Ledger S4/S5] *(pending Keel reread)*
- [ ] CHK010 Does acceptance require real filesystem/safe-reader/adversarial proofs plus real-browser receipts with Could not run ≠ PASS? [Measurability, Spec §FR-014 / SC-006 / Q8] *(pending Keel reread)*
- [ ] CHK011 Is the Q8 closing bar / frozen scope / two-round cap / three-axis / filing policy stated, with PLAN FROZEN withheld and Gate 1 closed while Ledger S4 remains unresolved? [Process, Spec §FR-015 / FR-020 / Q8 / Ledger S4] *(pending Keel reread)*
- [ ] CHK012 Do SMOKE-001–004 distinguish planned criteria from observed runtime acceptance, including list-failure vs empty? [User-Visible, Spec §Smoke Test] *(pending Keel reread)*
- [ ] CHK013 Are out-of-scope items explicit (other formats, auto-enable, StreamEndpoints edits, global glossary rewrite, stream-exemption claims, silent packages, route-local 403, blanket NOTE rejection, new ADR)? [Boundary, Spec §Out of scope / Q8 / C1 / Ledger S4] *(pending Keel reread)*
- [ ] CHK014 Is the only remaining Quill `needs decision` the carried Ledger S4 owner checkbox (blocked: structural), with round-1 items 1–8 and round-2 items 1–3 applied and no silent scope additions? [Traceability, DRAFTING_RECEIPT / Ledger S4] *(pending Keel reread)*
- [ ] CHK015 Are Charter §2.3 items 1–6 treated as deliberate Patron rulings rather than silent assumptions, with 2.3(b) structural checkbox explicit and unticked, and only **explicit approval** of exact text (or consistently incorporated owner-amended text) plus verified merge unblocking pickup? [Traceability, Spec §Owner question / FR-020 / CONCLUSIONS Phase 2 Step 6] *(pending Keel reread)*
- [ ] CHK016 Does vocabulary require bounded CONTEXT.md append of only the four C2 terms (term/definition/`_Avoid_:`) with no global rewrite/ADR, aligning constitution VIII? [Process, Spec §Domain vocabulary / FR-016 / C2] *(pending Keel reread)*
- [ ] CHK017 Do plan/tasks require RFC 7807 ProblemDetails **including normative `traceId`** via the single existing `IExceptionHandler`/configured writer (recon non-explicit handler set is not a waiver; bounded writer-config task only if pickup proves missing), reject route-local mapping, preserve frozen 403/404/422/500 contracts, require explicit generic sanitized 500 private/no-store+`traceId` assertions on **both** subtitle routes, and mark LibraryFileAccessDeniedException→403 as pending **explicit owner approval** of exact v1.13.0 II/V text (proposed II sentence + V row recorded; constitution not edited)? [Consistency, Spec §Owner question / FR-008 / FR-014 / FR-020 / plan V / T002 / T006 / Ledger S4] *(pending Keel reread)*
- [ ] CHK018 Are mutation exits stated honestly — SKIPPED scope-empty, N/A only for configured exclusions, FAILED for changed non-excluded project without eligible tests? [Process, plan Gates / T022 / I1] *(pending Keel reread)*
- [ ] CHK019 Are sealed subtitle query/handler + AddHandler requirements explicit, with T007 owning reauth/enumeration/token/cancellation/disposal/decode and T008 pure transport, sealed immutable Core→Api DTO mapping, HandlerRegistration serialization, and T025 execution note? [Consistency, Spec §FR-012 / FR-019 / plan II / T003–T008 / T025 / Compass F1 / Ledger S6] *(pending Keel reread)*
- [ ] CHK020 Are LibraryOptions four Range-validated bounds, exactly-at/over/invalid-config tests, and precise options/reader/ADR0037 files in proposed implementation scope? [Completeness, Spec §FR-018 / plan Project Structure / Sentry F1] *(pending Keel reread)*
- [ ] CHK021 Are MovieId + sanitized TelemetryConstants diagnostics required without new spans/metrics, and encoding registration T001-conditional without mutable static init? [Observability, Spec §FR-006 / FR-019 / Ledger S3/S5] *(pending Keel reread)*
- [ ] CHK022 Does T000/FR-020 state that reject/unanswered/non-approval disposition remains blocked even if a disposition and merge receipt both exist, and that any answer is not counted as approval? [Process, Spec §FR-020 / T000 / Ledger S4] *(pending Keel reread)*

## Notes

- Checklist wording assesses readable stakeholder scenarios plus explicit, traceable ticket/Patron-mandated contracts; it does not require stripping those contracts to satisfy generic Spec Kit “technology-agnostic / no APIs” template phrases (precedent DEV-455 / DEV-456).
- `/speckit-clarify` found no interactive stakeholder questions after grill Q1–Q8; Clarifications session records that outcome.
- Plan-challenge repair rounds 1 and 2 (final allowed) applied to Quill-owned drafts. Ledger S4 remains **blocked: structural** with proposed constitution v1.13.0 text and unticked owner checkbox; only explicit approval of exact text (or consistently incorporated owner-amended text) plus verified merge unblocks pickup. Checklist items are deliberately unchecked until Keel independently rereads — this checklist does **not** claim clean PASS, acceptance tests, Compose evidence, Gate 1 clearance, or PLAN FROZEN.
- Encoding stays conditional; NOTE/STYLE/REGION words are not blanket rejected; normative `traceId` restored; explicit generic 500 assertions required on both subtitle routes.
- Owner Gate 1 checkbox is required and unticked (brief Ledger S4). Quill does not close it. Remaining blockers after this final repair round go to Bernstein — never auto-clear.
