# Specification Quality Checklist: Migrate stored movie folders to library-relative paths

**Purpose**: Validate specification completeness and quality before Gate 1 / planning freeze
**Created**: 2026-11-07
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## User-Visible Outcome

- [x] "What the User Will See" names each visible change by page or component, or says "Nothing visible" with a reason
- [x] Each visible change has a SMOKE-### check in user terms that can run against the Compose stack
- [x] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services)

## Requirement Clarity & Consistency (requirements quality)

- [x] CHK001 Are Canonical LibraryPath normalization rules specified (separators, casing, `.` root, rejected shapes)? [Clarity, Spec §Domain vocabulary / FR-001]
- [x] CHK002 Are legacy-root mapping case rules distinguished for Windows vs POSIX prefixes? [Clarity, Spec §FR-004 / A2]
- [x] CHK003 Is fail-closed behaviour defined for unmatched, escaping, and conflicting overlapping roots without longest-prefix preference? [Consistency, Spec §Edge Cases / FR-004]
- [x] CHK004 Are exact and case-only collision behaviours specified for all hosts without merge/delete/suffix? [Completeness, Spec §FR-008]
- [x] CHK005 Is the startup barrier required in `LamuFlix.Api` `Program.cs` after Build before Run, covering HTTP and hosted consumers (Worker = hosted in Api)? [Completeness, Spec §FR-006 / A8]
- [x] CHK006 Are transactional atomicity, ACCESS EXCLUSIVE lock, preflight-before-CI-index, path+key+column-collation activation, post-activation no-write rerun, and interruption/restart safety required with real-DB evidence (including rollback after actual writes)? [Measurability, Spec §FR-007 / FR-009 / SC-001 / A8–A10]
- [x] CHK007 Is portable uniqueness required to be race-safe at the database via Option C (`library_path_key` / `IX_movies_library_path_key` with column collation `C`) rather than application-only precheck or SQL `lower()`/generic collation? [Clarity, Spec §FR-010 / A8–A10]
- [x] CHK008 Are new-import legacy-alias and outside-root rejections specified separately from backfill/message alias use? [Consistency, Spec §FR-011 / FR-004]
- [x] CHK009 Is process-CWD resolution explicitly forbidden for runtime media and thumbnail authorization? [Coverage, Spec §FR-012 / FR-017]
- [x] CHK010 Are ThumbnailRequested schema/topology stability and OrdinalIgnoreCase identity comparison specified? [Completeness, Spec §FR-015 / FR-016]
- [x] CHK011 Are terminal ineligible/unauthorized paths required to avoid source access and retry loops? [Edge Case, Spec §FR-017]
- [x] CHK012 Are hard-stop deployment, runtime/globalization/ICU match, backup-restore downgrade, F8 migrations-remove recovery, named existing backstops (no new interlock), and alias-retirement requirements documented without inventing an admin UI/CLI or embedding real connection values? [Completeness, Spec §FR-018 / A9–A10]
- [x] CHK013 Are LocalPlay / launcher / frontend / new-dependency / Worker-project / new-port exclusions explicit? [Boundary, Spec §Out of scope / FR-014 / FR-020]
- [x] CHK014 Does the acceptance matrix cover the eight brief scenario families with named evidence styles, including Operations/T022? [Coverage, Spec §FR-019 / T023]
- [x] CHK015 Is SMOKE-001 defined with planned criteria and required observed Compose runtime execution (Could not run ≠ PASS)? [User-Visible, Spec §SMOKE-001 / SC-006 / A8]
- [x] CHK016 Is Patron A8 Option C recorded as the resolved uniqueness mechanism (no open Quill `needs decision`; no [NEEDS CLARIFICATION])? [Traceability, Spec §Owner question / DRAFTING_RECEIPT / A8]
- [x] CHK017 Is shipping-runtime Unicode equivalence coverage specified (supplementary/Deseret, Turkish i, sharp-s, ligatures, combining forms; samples ≠ proof; mismatch stop before T006+; never waive; expected sharp-s/ligature divergence rejected per recon §9.2)? [Completeness, Spec §FR-021 / A8–A10]
- [x] CHK018 Are `23505` outcomes from both unique indexes required to stay on the existing duplicate-import path with endpoint assertions and no new 409 mapping? [Consistency, Spec §FR-022 / A8]
- [x] CHK019 Is key derivation pinned to one Infrastructure `LamuFlixDbContext` shared SaveChanges choke point (sync+async), with every-writer tests and no Core/converter/per-writer derivation? [Completeness, Spec §FR-010 / A9 / plan Project Structure]
- [x] CHK020 Is A10 two-scaffold composition recorded exactly (Phase A `AddLibraryPathKey` into Persistence/Migrations; Phase B `LibraryPathKeyActivation` to absolute TEMP outside worktree; tooling rewrites existing snapshot; only additive ships; no hand-edit/suppression/manual deletion) with pending-check exit 0, live PG17 additive-only Migrate/schema-not-ready, and unrelated later scaffold emitting no activation? [Completeness, Spec §FR-007 / A10 / recon-DEV-459-3 F5–F7]
- [x] CHK021 Is sealed `MovieLibraryPathMigrationGate` named with no interface/no Core port, ArchitectureTests assertion, ACCESS EXCLUSIVE first SQL, single transaction, nonconcurrent index, and gate-set **column** collation `C` (property `UseCollation("C")`; no index-level API) with real-DB assertions? [Completeness, Spec §FR-007 / FR-020 / A9–A10 / recon F9]
- [x] CHK022 Are `LibraryOptionsValidator` + `AddLamuFlixOptions` registration, retained data annotations/`ValidateOnStart`, and options validation before gate writes specified? [Completeness, Spec §FR-018 / A9]
- [x] CHK023 Is automated `MovieDetailsResponse.Path` relative-value assertion owned (distinct from SMOKE-001)? [Coverage, Spec §FR-022 / US3]
- [x] CHK024 Are Core pure-string parsing (no `System.IO.Path`), both-OS one-host fixtures, and Infrastructure-only resolution specified? [Boundary, Spec §FR-004 / FR-012 / A9]
- [x] CHK025 Are glossary ownership (builder/Quill, not Gauge), ADR proposed→accepted owner/timing tied to user Gate 1 receipt, sequential gates, and retained AutoFixture/Faker.Net/MockFileSystem conventions specified? [Process, plan / tasks / A9]
- [x] CHK026 Is F8 immediate-successor migrations-remove recovery owned by T007/T022 (redo Phase B external scaffold; pending zero; inspect next scaffold no activation; no suppression/manual deletion)? [Completeness, Spec §FR-007 / FR-018 / A10]
- [x] CHK027 Are there no remaining Quill needs-decision or needs-recon items after A10 (prior A9 tooling blocker superseded)? [Traceability, DRAFTING_RECEIPT / A10]

## Notes

- Spec Kit “no implementation details” checklist items are satisfied at the stakeholder-story level; FR/plan/tasks intentionally name ticket-authorized types and recon paths because LamuFlix Spec Kit drafts bind to frozen brief/recon surfaces (precedent DEV-438 / DEV-463).
- `/speckit-clarify` found no interactive stakeholder questions after grill A1–A7; Clarifications session records that outcome. Post-drafting A8–A10 resolved mechanism/composition/placement/tooling residuals.
- No owner Gate 1 checkbox (brief A7/A9/A10). No open Quill `needs decision` or `needs recon`. This checklist does **not** claim acceptance tests, Unicode/property proofs, or Gate 1 clearance have already run.
- Consolidated plan-challenge repair round 2 applied by Quill from Keel numbered list + Patron A10 (3096f9a6). Does **not** invent freeze — Keel owns independent final analyze.
