# Specification Quality Checklist: Browser playback default with Media Chrome

**Purpose**: Validate specification completeness and quality before Gate 1 / planning freeze
**Created**: 2026-11-07
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] Stakeholder scenarios and smoke criteria remain readable without requiring implementation literacy
- [x] Focused on user value and business needs
- [x] User stories and visible outcomes are written for non-technical stakeholders
- [x] Ticket/Patron-mandated technical contracts (Media Chrome, capability route/DTO/generated artifacts, module boundaries, Compose/nginx) are explicit and traceable in FR/plan/tasks rather than omitted for template-generic agnosticism
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable and outcome-oriented
- [x] Success criteria stay stakeholder-readable while preserving explicit, traceable ticket/Patron-mandated contracts (not rewritten to erase those contracts)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] Mandated contracts remain explicit and traceable; stakeholder scenarios remain readable

## User-Visible Outcome

- [x] "What the User Will See" names each visible change by page or component, or says "Nothing visible" with a reason
- [x] Each visible change has a SMOKE-### check in user terms that can run against the Compose stack
- [x] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services)

## Requirement Clarity & Consistency (requirements quality)

- [x] CHK001 Are both Play entry points required to default to browser playback without invoking local launch POST? [Completeness, Spec §FR-001 / Q3–Q4]
- [x] CHK002 Is in-page labelled player presentation specified (no new route/modal) with card Play converging on the same region? [Clarity, Spec §FR-002 / Q4]
- [x] CHK003 Is Media Chrome named as the sole new player dependency with exact ADR-0023 ticket-specific pin (Node-24, ≥14 days, no caret, registry evidence; no second wrapper) deferred to verified pickup (not invented in draft)? [Boundary, Spec §FR-003 / Q1 / Ledger M3]
- [x] CHK004 Are control and keyboard requirements enumerated (play/pause, seek, speed presets, desktop fullscreen, Space/K/arrows/M/F/Escape) with Space single-toggle on focused control, seek ignore-until-bounds then clamp, and player-owned focus scoping? [Completeness, Spec §FR-004–FR-006 / Q5 / Sentry M4/L1/L4]
- [x] CHK005 Are loading/buffering, autoplay denial, terminal failure+Retry-from-beginning, safe diagnostics, deterministic local recovery, outgoing-before-incoming teardown, and no history-write behaviours distinguished? [Consistency, Spec §FR-007–FR-011 / Q6 / Sentry M3/L2/L5]
- [x] CHK006 Is the capability contract fixed as endpoint-resolved IOptions outside Core → GetPlaybackCapabilitiesQuery(bool) → PlaybackCapabilitiesResult → Api PlaybackCapabilitiesDto for GET /api/playback/capabilities with only localPlayEnabled, real-host options proof, and no new Core Options dependency? [Clarity, Spec §FR-012 / Q2 / constitution I–II / Ledger M1/M2/L3]
- [x] CHK007 Is the C# → OpenAPI → generated TypeScript chain required with no handwritten duplicates or extra fields? [Consistency, Spec §FR-013]
- [x] CHK008 Is secondary-action hide-on-loading/failure specified while browser Play remains available? [Coverage, Spec §FR-014 / Q2]
- [x] CHK009 Is Open in BS Player always offered when capability true / hidden otherwise, restricted to that explicit action, with preserved launch status meanings and no automatic fallback? [Completeness, Spec §FR-015 / Q3 / Sentry L5]
- [x] CHK010 Are visible copy and “not every launcher is BS Player” constraints recorded? [Clarity, Spec §FR-016 / Q12 / ASSUMPTIONS]
- [x] CHK011 Is module boundary limited to existing details/library/playback feature folder/Core Playback query + API mapping/e2e without new architecture layers or Core ports, with HandlerRegistration/ApiEndpoints marked proposed until T001? [Boundary, Spec §FR-017 / Q9 / Compass F3]
- [x] CHK012 Are Compose sample provisioning rules (git-ignored/outside-tree, scan/import or already-scanned title, no placeholder overwrite, LocalPlay=false) specified? [Completeness, Spec §FR-018 / Q7 / Sentry L3]
- [x] CHK013 Is real-browser Compose evidence required with Could not run ≠ PASS, desktop fullscreen acceptance, and jsdom called out as insufficient for codec/seek/fullscreen/range? [Measurability, Spec §FR-019 / SC-007 / Q7 / Sentry L4]
- [x] CHK014 Are DEV-456/458 delivery + Gate 1 + pickup recon required before consuming stream/launch types, including T001 media-element credential and URL lifetime/reuse STOP checks, with drift → new ruling? [Dependency, Spec §FR-020 / Q8 / Sentry M1/M2]
- [x] CHK015 Is the Q11 closing bar / frozen scope / two-round cap / three-axis / filing policy stated? [Process, Spec §FR-021 / Q11]
- [x] CHK016 Do SMOKE-001–003 distinguish planned criteria from observed runtime acceptance? [User-Visible, Spec §Smoke Test]
- [x] CHK017 Are out-of-scope items explicit (transcoding, captions, resume, stream redesign, launcher hardening, schema, new ADR/glossary), with Domain vocabulary confirmed as spec-local Q2/Q12 clarification only? [Boundary, Spec §Out of scope / Domain vocabulary / Q12 / Compass F4]
- [x] CHK018 Is there no Quill `needs decision` remaining after grill Q1–Q12 and the plan-challenge amendment pass? [Traceability, DRAFTING_RECEIPT]
- [x] CHK019 Is Palette `design.md` identified as Palette-owned (taste / close-affordance amendments separate) without Quill inventing visual design? [Process, Spec §Clarifications / plan Status / Q9 / Compass F1–F2]
- [x] CHK020 Are Charter §2.3 items 1/4/5 treated as deliberate Patron rulings (Media Chrome, capability API, LocalPlay) rather than silent assumptions? [Traceability, Spec §Scope / CONCLUSIONS Q1–Q3]
- [x] CHK021 Are all relevant plan-challenge dispositions (Sentry M1–M4/L1–L5, Ledger M1–M3/L2–L3, Compass F3/F4) applied in owned artifacts with none silently deferred past freeze? [Traceability, brief Plan challenge decision register / CONCLUSIONS adjudication]
- [x] CHK022 Does T005 require `npm audit --omit=dev` outcome/advisory disposition or Could not run without gate-script changes? [Supply chain, tasks T005 / Ledger L2]

## Notes

- Checklist wording assesses readable stakeholder scenarios plus explicit, traceable ticket/Patron-mandated contracts; it does not require stripping those contracts to satisfy generic Spec Kit “technology-agnostic / no APIs” template phrases (precedent DEV-456 / DEV-459).
- `/speckit-clarify` found no interactive stakeholder questions after grill Q1–Q12; Clarifications session records that outcome.
- No owner Gate 1 checkbox (brief Q12). No open Quill `needs decision`. This checklist does **not** claim acceptance tests, Compose evidence, or Gate 1 clearance have already run.
- Plan-challenge amendment pass applied; freeze remains pending Keel's independent reread. Does **not** invent freeze — Keel verifies closure before PLAN FROZEN.
- Ledger L1 / Compass F1–F2 design/ASSUMPTIONS amendments belong to Palette; Quill does not edit those files.
