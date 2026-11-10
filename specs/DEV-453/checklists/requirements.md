# Specification Quality Checklist: Subtitle provider and BS Player-compatible sidecar

**Purpose**: Validate specification completeness and quality before Gate 1 / planning freeze
**Created**: 2026-11-09
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
- [x] Smoke Test skipped with reason when nothing is visible on the default Compose stack; runtime proofs are labelled separately
- [x] What prepare/serve and operator setup need (config, mounts, secrets) is listed

## Requirement Clarity & Consistency (requirements quality)

- [x] CHK001 Is OpenSubtitles.com REST identified as the sole provider for personal non-commercial planning, with BS Player hash-service integration explicitly excluded? [Completeness, Spec §FR-001 / Q3]
- [x] CHK002 Are ordered language preferences, no-outside-preference fallback, and deterministic within-language tie-break specified? [Clarity, Spec §FR-002 / Q4 / ASSUMPTIONS]
- [x] CHK003 Is exact-basename sidecar naming (`Film.srt`) required and language-suffix naming excluded, with existing-sidecar preserve/no-overwrite? [Consistency, Spec §FR-003 / Q4]
- [x] CHK004 Are prepare and serve brief-relative routes and streaming non-trigger specified without inventing a new public shape, with MapGroup/registration filenames deferred to T001 (no unsupported `ApiEndpoints.cs` / `/api` fact claims)? [Completeness, Spec §FR-004 / Q5–Q6 / analyze I1]
- [x] CHK005 Is the prepare 200 DTO (`outcome` + nullable `selectedLanguage`) fully specified with `selectedLanguage` always matched on `saved` and null on `alreadyPresent`/`noMatch`? [Clarity, Spec §FR-005 / Q6 / Plan Challenge 6.1]
- [x] CHK006 Does existing-sidecar precedence precede acquisition-configuration checks after authorization? [Consistency, Spec §FR-006 / Q6 / Q10]
- [x] CHK007 Are prepare/serve status maps complete, distinguishing transport malformed-id **400** (raw capture/parse) from FluentValidation application-validation **422**, plus 409 write-denial refinement, without claiming all validation is 400? [Completeness, Spec §FR-007 / Q6 / Q10 / brief addendum C1/V / Plan Challenge 2.4]
- [x] CHK008 Is GET serving specified as bounded `application/x-subrip` with nosniff + safe-basename attachment headers, no provider traffic, and scheduled independent of acquisition settings (T010–T012)? [Completeness, Spec §FR-008 / Q5 / Q10 / Plan Challenge 2.1]
- [x] CHK009 Are Subtitles options, safe defaults, nested OpenSubtitles acquisition settings, functionality-named C# options types, and demo-startup independence specified without guessed numeric defaults? [Clarity, Spec §FR-009 / Q6 / Q10 / Plan Challenge 4]
- [x] CHK010 Are the two narrow ports, read-only `ILibraryFileReader`, zero new packages/schema/layers, and ADR 0038 boundary stated? [Boundary, Spec §FR-010 / Q7]
- [x] CHK011 Are containment, atomic create-without-replace, cancellation cleanup, and concurrent-create→alreadyPresent requirements present? [Coverage, Spec §FR-011 / Q8]
- [x] CHK012 Are pre-request HTTPS/verified-host checks, redirects disabled, all-`3xx`→502, never-contacted proof, size/time bounds, minimal downloaded-SRT criteria, no retry, and secret/path/content log hygiene specified and scheduled in T007/T008? [Coverage, Spec §FR-012 / Q8 / Plan Challenge 1, 10]
- [x] CHK013 Are moviehash-first lookup via `ILibraryFileReader`, title fallback from confirmed Movie primary title (blank→noMatch), and Phase B hash-vector verification specified? [Completeness, Spec §FR-013 / Q8–Q9 / Plan Challenge 5.1, 6.2]
- [x] CHK014 Is LocalPlay/`Process.Start` independence explicit? [Boundary, Spec §FR-014 / Q7]
- [x] CHK015 Is the C# → OpenAPI → generated TypeScript chain specified without hand-edits, including application-validation 422 metadata? [Consistency, Spec §FR-015 / FR-007 / Q6 / C1/V]
- [x] CHK016 Are required automated test families enumerated (selection/hash/store/API/provider/serving), including content/redirect/no-retry, 400-vs-422, acquisition-independent existing-track serve, and health matrix proofs? [Coverage, Spec §FR-016 / FR-021 / Q9 / C1–C2]
- [x] CHK017 Are Phase B pickup dependencies (DEV-457/DEV-454, DI filenames, allowlist, provider schema) specified as verification gates rather than invented facts? [Dependency, Spec §FR-017 / Q9]
- [x] CHK018 Are harness gate honesty labels (PASS/SKIPPED/SKIP/N/A/Could not run) and no-threshold-lowering required? [Process, Spec §FR-018 / Q9]
- [x] CHK019 Are runtime receipts for configured-key download, writable save, BS Player autodetection, and read-only serving specified with Could not run honesty? [Acceptance Criteria, Spec §FR-019 / Q9]
- [x] CHK020 Are Q11 closing bar, Size L three-axis publication, frozen scope, and two-round cap recorded? [Process, Spec §FR-020 / Q11]
- [x] CHK021 Is "Nothing visible" on default Compose justified, with Smoke Test skipped and runtime proofs separated? [User-Visible, Spec §What the User Will See]
- [x] CHK022 Are the five settled vocabulary terms scheduled as append-only repository-root CONTEXT.md entries in the same spec PR (Principle VIII / brief addendum C4/VIII), preserving prior content and superseding feature-local-only claims? [Traceability, Spec §Domain vocabulary / FR-022 / C4/VIII]
- [x] CHK023 Are ASSUMPTIONS.md policy/taste items referenced without inventing new taste? [Traceability, Spec §Assumptions / ASSUMPTIONS.md]
- [x] CHK024 Are obsolete C1/C2/VIII needs-decision placeholders removed, while C4/VII remains visibly unresolved with citations (owner checkbox; Gate 1 closed)? [Traceability, Spec Owner question / DRAFTING_RECEIPT / brief addendum]
- [x] CHK025 Are Principle II sealed handlers / AddHandler / decorators and Principle V FluentValidation + single ProblemDetails+traceId mapper scheduled with explicit 400-vs-422 routing retained? [Consistency, plan Constitution Check / C1/V]
- [x] CHK026 Is Principle VI scheduled as ONE always-Healthy optional-provider local health policy with ready/disabled_or_unconfigured metadata, zero network/quota calls, full enabled/disabled/configured/invalid matrix, exclusion from `/health/live`, functionality-named TelemetryConstants, and movie-id events — without reachability claims? [Coverage, plan Constitution Check / T012b / C2/VI / Plan Challenge 7]
- [x] CHK027 Is C4/VII marked blocked structural with the exact unchecked owner checkbox copied from the brief addendum (verbatim `SubtitlesOptions / OpenSubtitlesOptions`), Gate 1 closed, affected ValidateOnStart implementation blocked until approved scoped constitution amendment including options-record enumeration, Principle I independent, and Q10/no numeric defaults preserved? [Boundary, Spec Owner question / plan Constitution Check / brief addendum C4/VII / Plan Challenge 3–4]
- [x] CHK028 Does the draft require the same unchecked C4/VII owner question on the spec-PR body handoff, without checking it or claiming Gate 1/analyze-clean/freeze? [Process, Spec Owner question / DRAFTING_RECEIPT]
- [x] CHK029 Are pre-request HTTPS/verified API+download host checks, `AllowAutoRedirect=false`, all-`3xx`→502, invalid-local-BaseUrl 409 vs upstream-unsafe 502, and T001 host/protocol STOP specified? [Coverage, Spec §FR-012 / T001 / T007–T008 / Plan Challenge 1]
- [x] CHK030 Are raw `{id}` capture/parse (or equivalent reachable binding), true noninteger 400, well-formed absent 404, and independent reachable 422 documented without invented GET validation failures? [Consistency, Spec §FR-004 / FR-007 / T011–T012 / Plan Challenge 2, 11]
- [x] CHK031 Are caller-abort vs provider-timeout separation, GET nosniff/attachment headers, and accepted concurrent double-download / crash-orphan risks (no lock/janitor) recorded? [Coverage, Spec §FR-007 / FR-008 / FR-011 / T009 / T011–T012 / Plan Challenge 2–4]
- [x] CHK032 Is the provider seam data-only with authorized `ILibraryFileReader` hash reads, and are T001/T001c feature-folder/isolation STOP gates plus single-mapper typed Core failures specified? [Boundary, Spec §FR-010 / FR-011 / T001 / T001c / T002 / T006 / Plan Challenge 5–6]
- [x] CHK033 Are minimal downloaded-SRT validity criteria (UTF-8, blocked prefixes, timestamp cue) and T001 raw/wrapper STOP specified, while existing-track serve remains exact bytes? [Coverage, Spec §FR-012 / T007–T008 / T010 / Plan Challenge 10]
- [x] CHK034 Is safe Retry-After defined as valid nonnegative delta-seconds or HTTP-date (omit absent/malformed; no cap) with both forms covered in tests? [Clarity, Spec §FR-007 / T007 / T011 / Plan Challenge 11]
- [x] CHK035 Does `plan.md` place repository-root `CONTEXT.md` correctly (not under Api), and does the draft preserve intact UTF-8 ≥/arrows without ADR rewrite? [Editorial, plan Project Structure / Spec §FR-013 / Plan Challenge 7 / Ledger L2–L4]

## Notes

- Spec Kit “no implementation details” checklist items are satisfied at the stakeholder-story level; FR/plan/tasks intentionally name ticket-authorised paths because LamuFlix Spec Kit drafts bind to frozen brief/recon surfaces (precedent DEV-456 / DEV-438).
- `/speckit-clarify` found no interactive stakeholder questions after grill Q1–Q12; Clarifications session records that outcome.
- Owner Gate 1 structural checkbox is present and unchecked (brief addendum C4/VII). Phase B verification items remain brief Q9 tasks, not open Quill decisions.
- Checklist created by `/speckit-specify` quality pass and refined by `/speckit-checklist`; analyze round 1 added CHK025–CHK027; analyze round 2 realigned CHK007/015/016/022/024/026/027 and added CHK028; Plan Challenge round 1 realigned CHK005/007–009/012–013/026–027 and added CHK029–CHK035.
- Does **not** claim analyze clean, PLAN FROZEN, Gate 1 clearance, or runtime acceptance already PASS — this is Plan Challenge round 1 planning repairs only; two-round cap remains.
