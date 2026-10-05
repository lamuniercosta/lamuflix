# Feature Specification: Complete DEV-307 Observability and Run the DEV-308 Retrospective

**Feature Branch**: `feature/396-spec`

**Created**: 2026-10-05

**Status**: gate1: provisional (closed - owner Q2 open)

**Input**: Ticket DEV-396 (Size L; started after DEV-309 merged as PR #84), `brief.md` (owns the plan decisions), `CONCLUSIONS.md` Q1-Q6, `ASSUMPTIONS.md`, note `recon-DEV-396` (Phase A recon, `:108-339`) and `specs/PRODUCT.md`. Inherited contract: `specs/DEV-307/spec.md` (FR-001..FR-035, SC-001..SC-014) and `specs/DEV-307/tasks.md`.

## Owner Decisions (Gate 1 remains closed)

- [ ] **OD-1 (inherited DEV-307 Q2)** - `blocked: structural - DEV-307 Q2: health routes return JSON {status, checks[name,status,durationMs]}; /health/ready Unhealthy -> 503 ProblemDetails with traceId and checks extension (no exception text); Degraded -> 200; FR-034 (both health routes in the committed web/src/api/openapi.json, with the generated TS) is deferred to the Epic 6 web scaffold ticket DEV-20, which owns the C# to OpenAPI to TypeScript chain for both routes, with DEV-320 (generate the TypeScript API client from the committed openapi.json and configure the MSW handlers) owning the generated TS client afterwards - a 2.3b departure from constitution.md:359-360 for this ticket only?`
  - Copied verbatim from `specs/DEV-307/brief.md:76`. **Ticking this checkbox without its deferral clause is not an answer** (`specs/DEV-307/brief.md:78`): without the clause Q2 can be ticked while FR-034 stays unsatisfiable and the C# to OpenAPI to TypeScript chain for the two health routes has no owner. With it, the owner either accepts the deferral (FR-034 rides DEV-20, with DEV-320 after it) or declines it (this ticket is then `blocked: structural`, because building the OpenAPI document here needs a new package and a new top-level folder, `PRODUCT.md:43-44`).
  - No Patron ruling closes it. Until it is answered, inherited DEV-307 readiness tasks T037-T041 (carried by local T033-T036, with inherited T048 carried by local T041) are BLOCKED, are not counted as delivered and are not reported as deferred. The conditional DEV-20/DEV-320 ownership is never described as already accepted.

## User Scenarios & Testing

### User Story 1 - Trace, metric and log providers are composed with OTLP exporters (Priority: P1)

As the operator of the platform, I need every request, outbound call, database call and message hand-off to be traceable, and metrics and logs to be composed with OTLP exporters (delivery to a collector is not proven here), so that a slow or failing movie import can be followed end to end.

**Why this priority**: This is the largest piece of DEV-307 that never reached main (`recon:139-151`: no OpenTelemetry composition exists in `src` or `tests`).

**Independent Test**: Build the shared host defaults and read the tracer, meter and log providers; confirm each carries its exporter and that removing any single instrumentation registration fails a check naming that instrumentation.

**Acceptance Scenarios**:

1. **Given** the shared defaults are composed, **When** the providers are resolved, **Then** the trace, metric and log providers all exist and each carries an OTLP exporter configured without an explicit endpoint.
2. **Given** no endpoint override, **When** the exporter options are read, **Then** the default endpoint is used; **Given** the standard environment variable is set, **Then** its value wins, and the environment is restored afterwards.
3. **Given** the traces are composed, **When** activities arrive from ASP.NET Core, HttpClient, Npgsql, the application source and the RabbitMQ client publisher and subscriber sources, **Then** each reaches a recording processor.
4. **Given** the defaults are composed twice, **Then** neither the Serilog nor the OpenTelemetry registrations double.

### User Story 2 - The OMDb key never leaves in telemetry (Priority: P1)

As the owner of the OMDb credential, I need proof that the key, which the provider sends in the request query, appears in no span tag and no log record once HttpClient tracing exists.

**Why this priority**: The leak site is live (`OmdbMetadataProvider.cs:208-223`) and becomes exploitable the moment HttpClient instrumentation lands, so the proof ships in the same commit.

**Independent Test**: Run one real lookup through the real provider against a stub server with a sentinel key and inspect the captured span and every captured log record.

**Acceptance Scenarios**:

1. **Given** the HttpClient instrumentation is registered, **When** a lookup runs with the sentinel key, **Then** at least one real outbound span and at least one HttpClient-category log entry were captured before any absence assertion is made.
2. **Given** that capture, **Then** the sentinel appears in no span tag and in no rendered, structured or exception log content.
3. **Given** the same commit, **Then** no URI-redaction opt-out and no credential literal exists in the source.

### User Story 3 - Readiness membership is registered correctly (Priority: P1)

As the operator, I need the readiness group to contain exactly the PostgreSQL and RabbitMQ checks, so that a missing metadata provider never marks the host unready.

**Independent Test**: Read the real health registrations and assert which checks carry the shared readiness tag.

**Acceptance Scenarios**:

1. **Given** the persistence layer is registered, **Then** a PostgreSQL check over the existing database context carries the shared readiness tag.
2. **Given** the RabbitMQ layer is registered, **Then** its check carries the same shared constant, and no bare readiness literal remains in `src`.
3. **Given** the metadata provider is registered, **Then** its check is still registered and carries no readiness tag.

### User Story 4 - A rejected request is not a server fault in traces (Priority: P1)

As a person reading traces, I need a validation failure to leave the handler span status unset with a validation-failed outcome, and every other exception to mark the span as an error with the exception's full type name, with the exception rethrown unchanged in both cases.

**Independent Test**: Run the decorator with a recording listener through both paths in the active unit-test project, plus the retained one-assertion legacy edit.

**Acceptance Scenarios**:

1. **Given** a validation failure, **Then** status stays unset, the outcome attribute is validation-failed and the exception propagates unchanged.
2. **Given** any other exception, **Then** status is error, the error-type attribute is the full type name and the exception propagates unchanged.
3. **Given** the legacy test file is not part of the solution, **Then** its edit is recorded as ungated (could not run, NU1010) and the active test is the behavioral proof.

### User Story 5 - Readiness answers with the approved body (Priority: P2, BLOCKED on OD-1)

As an orchestrator, I need `/health/ready` to answer 200 or 503 with the approved problem document, so that traffic is only routed to a ready host.

**Why this priority**: It cannot start until the owner answers OD-1; planning it separately keeps all other work deliverable.

**Acceptance Scenarios** (runnable only after OD-1 is answered):

1. **Given** PostgreSQL and RabbitMQ are healthy, **Then** readiness answers 200; **Given** either is unhealthy, **Then** 503 as a problem document carrying the trace identifier and no exception text; **Given** only the metadata provider fails, **Then** 200.
2. **Given** the owner declines the FR-034 deferral, **Then** this ticket is `blocked: structural` and Gate 1 stays closed.

### User Story 6 - The missing DEV-308 review is done and its findings are acted on (Priority: P1)

As the maintainer, I need the never-reviewed DEV-308 range (`122c502b03b0eaffe18b79b0fd26183466d8f0d0..7a35e7727241c3afd92ac9ba21fa9d13da37cdd5`, 16 commits, 18 files) reviewed on all three axes with adjudicated findings and corrective records.

**Independent Test**: The historical artifacts name both full SHAs, all three axis reports exist, and each accepted Medium+ finding has a current-head reproduction before any fix.

**Acceptance Scenarios**:

1. **Given** a disposable detached checkout at the full historical head, **When** the pre-pass and the Sentry, Ledger and Compass axes run, **Then** adjudication happens only after all three reports exist; a missing axis is blocked, never waived.
2. **Given** an accepted Medium+ finding, **When** it is reproduced at the delivery head, **Then** a narrow fix follows only if it is still live; a resolved finding is recorded with evidence.
3. **Given** lower findings, **Then** they become follow-up records; tickets only for Critical/High or broken behaviour, otherwise noted, no ticket.
4. **Given** the corrective records are published, **Then** PR #82 and DEV-307 receive corrective comments through the authorized seats (the DEV-307 tracker comment through Rigger only) and the Keel merge-bar comment is posted.

### User Story 7 - Every inherited task ends with evidence (Priority: P2)

As the reviewer of the spec PR, I need each inherited DEV-307 task ID to end as delivered with receipts, BLOCKED, or an explicit owner deferral, with nothing ticked by inference.

**Acceptance Scenarios**:

1. **Given** the DEV-307 boxes disagree with `main` for the liveness and WAF harness (T013-T017, delivered through DEV-308 `2ce1e39`), **Then** each box is ticked only on a cited receipt.
2. **Given** T020A's owner-answer text has not been found, **Then** T020A stays open and is not ticked from installed pins.
3. **Given** the harness gates run, **Then** each receipt records the command, exit code and full verdict.

### Edge Cases

- The unstaged `harness.yml` deletion of mutation exclusions: the committed file is authoritative; Rigger restores it before any gate run and each gate receipt shows `git diff --quiet HEAD -- harness.yml` exiting 0.
- The standalone legacy test project cannot build (NU1010): recorded separately as Could not run, never as proof or as a waiver of active gates.
- The collector is unreachable: composition proof never claims delivery to a collector.
- A gate exits 2: scope-empty is SKIPPED (never PASS), a disabled gate is SKIP, mutation NOT APPLICABLE only on the actual script verdict, property exit 2 needs a recorded opt-out reason.
- The current-head reproduction shows an accepted finding already resolved: no fix; evidence recorded.

## Requirements

### Functional Requirements

- **FR-001**: The shared defaults MUST compose traces for ASP.NET Core, HttpClient, Npgsql, the application source and the RabbitMQ client publisher and subscriber sources; the two RabbitMQ source names live in the existing telemetry constants (DEV-307 FR-007, T027-T028).
- **FR-002**: The shared defaults MUST compose metrics for ASP.NET Core, HttpClient and the existing application meter identity, with no additional metric identity (FR-008, T029).
- **FR-003**: Logs MUST be composed with an OTLP exporter alongside the console pipeline (composition only, no collector delivery claim); the console pipeline, injected clock, options validation and idempotent composition MUST be preserved (FR-003, FR-018, FR-033).
- **FR-004**: Exporters MUST be configured without an explicit endpoint or protocol so the standard environment variable keeps precedence; no service name is invented (FR-006).
- **FR-005**: A same-commit proof MUST show the OMDb sentinel absent from span tags and rendered, structured and exception log content, after proving an actual outbound span and HttpClient-category entries exist; no redaction opt-out and no credential literal (FR-035, T033A).
- **FR-006**: A shared readiness tag constant MUST exist beside the existing pipeline constants; the PostgreSQL and RabbitMQ checks MUST use it; the metadata-provider check MUST stay registered and untagged (FR-011..FR-013, T034-T036).
- **FR-007**: Health registration proof MUST read the real registrations, independently of endpoint stubs.
- **FR-008**: The tracing decorator MUST leave validation failures with unset status and the validation-failed outcome attribute, and mark all other exceptions as errors with the full type name, rethrowing both unchanged (FR-020..FR-024, T005-T012).
- **FR-009**: The one-assertion legacy test edit (T010) MUST be made, and an equivalent unexpected-exception full-name proof MUST execute in the active unit-test project.
- **FR-010**: Readiness route, response writer and tests (inherited DEV-307 T037-T041, local T033-T036) MUST NOT be built until the owner answers OD-1; no provisional body or default writer; the answer or explicit deferral is recorded at local T041 (inherited DEV-307 T048).
- **FR-011**: The DEV-308 range MUST be reviewed cold in a disposable detached checkout at the full historical head, with base and head SHAs named in every artifact, before adjudication; historical and delivery receipts MUST stay separate and no artifact may be borrowed from another run.
- **FR-012**: Accepted Medium+ findings MUST be reproduced at the delivery head before a narrow fix; any file not already in the plan MUST be recorded in the plan before editing.
- **FR-013**: Medium+ findings block closure for both the retrospective and the delivery review; at most two review/remediation rounds of at most two fix commits each; Sentry, Ledger and Compass are all mandatory.
- **FR-014**: All required current task-pipeline gates MUST run on the delivery diff and be recorded with command, exit and full verdict: Roslyn, complexity (configured normal and refactor ceilings), InspectCode, mutation, property tests, vulnerability scan, format, `dotnet test` and the web gate where applicable. Exit 1 or Could not run never passes; no threshold is lowered; surviving mutants require tests.
- **FR-015**: Every inherited DEV-307 task ID MUST map to a test, gate or inspection receipt, a BLOCKED status or an explicit owner deferral; T020A and OD-1 are never ticked by inference.
- **FR-016**: Corrective records MUST be published: retrospective findings and adjudication summary in the corrective PR, a corrective comment on PR #82 and on DEV-307 through the authorized seats, and the Keel merge-bar PR comment. No merge is authorized; awaiting the user's merge is the terminal state.
- **FR-017**: The work MUST introduce no new dependency, project, top-level folder, architectural layer, database schema, public API shape beyond `specs/DEV-307/spec.md`, `Features:LocalPlay` change, secret, `Process.Start`, threshold change, CPM/csproj/sln edit, Worker/Pomelo repair or unrelated consumer edit.
- **FR-018**: FR-034 (health routes in the committed OpenAPI document and generated TypeScript) is NOT delivered by this branch; no web scaffold, OpenAPI package or generated TypeScript change is made here.

### Key Entities

- **Inherited task**: a `specs/DEV-307/tasks.md` task ID (52 lines at recon: 15 ticked, 37 open) with a final evidence state.
- **Historical review artifact**: pre-pass, three axis reports and adjudication for `122c502..7a35e77`, each naming both full SHAs.
- **Finding**: a Medium+ or lower observation with historical location, current-head reproduction result, disposition and fix or follow-up reference.
- **Receipt**: command, exit code and full verdict for one gate or inspection.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The built host exposes a trace, metric and log provider, 3 of 3 carrying an OTLP exporter, and the default and environment-variable endpoints are each asserted; the environment is restored in 100% of runs.
- **SC-002**: All 6 trace sources arrive at a recording processor, and removing each of the 3 named instrumentation registrations (ASP.NET Core, HttpClient, Npgsql) fails exactly its own check.
- **SC-003**: For one real OMDb lookup, at least 1 outbound span and at least 1 HttpClient-category log entry are captured, and 0 span tags and 0 log records contain the sentinel.
- **SC-004**: The readiness group contains exactly 2 checks (PostgreSQL, RabbitMQ), the metadata-provider check is registered with 0 readiness tags, and 0 bare readiness literals remain in `src`.
- **SC-005**: The validation path and the unexpected-exception path each have an executing test in the active unit-test project, and both rethrow the original exception.
- **SC-006**: Inherited DEV-307 readiness tasks T037-T041 (local T033-T036, plus local T041 for inherited T048) show as BLOCKED with 0 counted as delivered until OD-1 is answered; OD-1 stays unticked in this spec.
- **SC-007**: The historical artifacts include 1 pre-pass and 3 axis reports, each naming both full SHAs; 0 axes are waived; every accepted Medium+ finding has a recorded current-head reproduction result.
- **SC-008**: 100% of inherited task IDs map to a receipt, BLOCKED status or owner deferral; 0 boxes are ticked by inference.
- **SC-009**: Every required gate has a receipt with command, exit and verdict, preceded by a clean-harness receipt (`git diff --quiet HEAD -- harness.yml` exit 0); 0 thresholds changed.
- **SC-010**: 0 files change outside the plan's file boundary, 0 schema or dependency changes, 0 secrets.
- **SC-011**: The corrective PR summary, PR #82 comment, DEV-307 tracker comment and Keel merge-bar comment are all published, and the PR is left unmerged.

## Assumptions

- The phase order (retrospective first, then OpenTelemetry with redaction, health membership, decorator, conditional readiness, evidence and gates) is a taste ruling logged `[assumed]` in `ASSUMPTIONS.md`.
- Baselines (621 passed; 16 property tests; format and vulnerable-package checks exit 0; scoped analyzers exit 0) are historical recon values, never delivery gates; legacy tests contribute zero.
- Installed package pins are reused without claiming an unverified historical owner answer to Q1.
- Provider and exporter composition is never described as delivery to a collector.
- Lower findings follow Q3; scope is frozen to Q1-Q5; anything else is a follow-up issue, not a finding in this round.
