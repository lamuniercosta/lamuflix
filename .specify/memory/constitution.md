<!--
SYNC IMPACT REPORT
==================
Version change: 1.14.0 → 1.15.0 (MINOR: Principle VII subtitle options enumeration
and owner-approved prepare-time acquisition validation exception; DEV-453)

Amendment 1.15.0 (2026-11-11), DEV-453 scoped C4/VII application:
  Authority: owner approval on PR #358, recorded in specs/DEV-453/CONCLUSIONS.md
    Phase 2 Step 7 (lines 211–216), T000 merged spec-PR receipt, brief.md Principle VII
    amendment execution entry, and explicit Conductor instruction to apply this amendment.
  ADR: docs/adr/0038-subtitle-provider-and-sidecar-boundary.md remains Proposed;
    existing subtitle acquisition/read separation is retained, not reauthored or Accepted.
  Modified principles:
    VII. Configuration Isolation and Deterministic Time (title unchanged): append only
       SubtitlesOptions and SubtitleProviderOptions to the closed options-record list;
       apply the approved subtitle-acquisition ValidateOnStart exception so missing or
       invalid acquisition settings yield prepare-time 409 without breaking startup or
       existing-track serving. Preserve typed binding, secret isolation and no guessed
       numeric defaults; all other options retain startup validation.
    Static-Analysis Gates checklist: reference the same narrow Principle VII exception
       from the startup-validation item, without weakening any unrelated check.
  Added sections: none
  Removed sections: none
  Templates reviewed (no updates required):
    .specify/templates/plan-template.md — Constitution Check derives current principles.
    .specify/templates/spec-template.md — no options inventory or startup-validation rule.
    .specify/templates/tasks-template.md — no closed options inventory; existing governance
       tasks remain applicable. .specify/templates/commands/ is absent.
  Runtime guidance reviewed:
    README.md and AGENTS.md — no duplicated options inventory or unconditional
    ValidateOnStart rule requiring an edit. Feature spec/plan/tasks pickup summaries
    are synchronized with this application; historical ruling text remains historical.
  Migration / follow-up:
    No schema/data migration or existing-code rewrite. T003 introduces functionality-named
    options and prepare-time validation; T004 wires typed binding in HandlerRegistration.cs.
    Do not add ValidateOnStart for subtitle acquisition settings or numeric fallback values.
    Preserve SaveEnabled false, empty preferences, existing-sidecar precedence and secret
    injection; new provider/store registrations remain in their later implementation tasks.
    T003/T004 remain unchecked and retain T002/T003 dependencies. This local amendment still
    requires normal delivery review/approval and claims no implementation or runtime PASS.
  Deferred placeholders / template follow-up: none

Version change: 1.13.0 → 1.14.0 (MINOR: Principle I adds the DEV-453 subtitle ports
and feature-folder enumeration; ADR 0038)

Amendment 1.14.0 (2026-11-11), DEV-453 T001c governance pickup:
  Authority: specs/DEV-453/brief.md Q7, Plan Challenge item 5 and Phase B pickup
    adjudication addendum; CONCLUSIONS.md T000 merged spec-PR #358 receipt and
    Patron pickup ruling at 261290ff; Conductor explicitly authorized T001c execution.
  ADR: docs/adr/0038-subtitle-provider-and-sidecar-boundary.md verified present;
    status remains Proposed. This amendment does not automatically Accept the ADR.
  Modified principles:
    I. Ports and Adapters (title unchanged): append only ISubtitleProvider and
       ISubtitleSidecarStore to the closed named-port list and Subtitles to the
       feature-folder list. Preserve every other entry, reference rule and isolation rule.
  Added sections: none
  Removed sections: none
  Templates reviewed (no updates required):
    .specify/templates/plan-template.md — principle-derived Constitution Check.
    .specify/templates/spec-template.md — no duplicated feature/port inventory.
    .specify/templates/tasks-template.md — existing ADR/constitution tasks apply.
    .specify/templates/commands/ — absent; no command templates to synchronize.
  Runtime guidance reviewed:
    README.md and AGENTS.md — no duplicated closed inventory requiring an edit.
  Migration / follow-up:
    No schema/data migration or existing-code rewrite is required.
    T002 must append |ISubtitleProvider|ISubtitleSidecarStore before )$ in
    ArchitectureTests PortWhitelistPattern atomically with the port definitions,
    preserving the existing 17 alternatives and order (19 after insertion), selector,
    count floor and test bodies. Existing sibling-feature isolation applies to Subtitles;
    handlers use shared Domain/Ports/Pipeline, not sibling feature namespaces.
    Do not backfill historically omitted ports. Principle VII subtitle options and
    owner-approved prepare-time validation require a separate scoped amendment before
    affected T003/T004 implementation; this Principle I amendment does not supply it.
    This document amendment requires normal delivery review/approval; it is not landed
    evidence, implementation compliance, gate PASS, runtime acceptance or merge clearance.
  Deferred placeholders / template follow-up: none

Version change: 1.12.0 → 1.13.0 (MINOR: Principle II expected-domain-failure sentence and
Principle V exception→status row for LibraryFileAccessDeniedException → 403; DEV-454)

Amendment 1.13.0 (2026-11-10), DEV-454 Phase B T001 constitution application:
  Authority: Charter §2.3(b)/item 6; Constitution Governance; verified T000 owner
    checkbox approval + Spec PR #357 merge receipt
    (c0bf581310fdcd0b690f17166e264b89d61bbd9b); specs/DEV-454/CONCLUSIONS.md
    Phase B T001 ruling (2) and lines 163-181 / 185-191.
  ADR: none required for this amendment (maps an already-specified domain exception).
  Modified principles:
    II. Explicit Handlers and Decorators: append sentence that expected domain failures
       also include LibraryFileAccessDeniedException for library file policy and
       containment denial.
    V. Validated Inputs, Consistent Error Responses: add
       LibraryFileAccessDeniedException → 403 to the exception-to-status table.
  Added sections: none
  Removed sections: none
  Templates reviewed (no updates required):
    .specify/templates/plan-template.md — principle-based Constitution Check; no edit needed.
    .specify/templates/spec-template.md — no exception/status inventory; no edit needed.
    .specify/templates/tasks-template.md — generic ADR/constitution tasks; no edit needed.
  Migration / follow-up:
    T001 must verify this text is present before T004/T008 rely on the global 403 arm.
    No schema or code migration is required by this document amendment alone.

Version change: 1.11.0 → 1.12.0 (MINOR: Principle VII records the owner-approved bounded
IConfiguration Kestrel/Endpoints/Url exception for DEV-458 LocalPlay listener startup
validation; ADR 0036)

Amendment 1.12.0 (2026-11-09), DEV-458 Phase 6 sweep alignment:
  Authority: owner Option A on spec PR #336; delivery PR #352 merged at 42ab6fc3;
    specs/DEV-458/CONCLUSIONS.md Q9/Q10 and Phase 6 step 1 ruling.
  ADR: docs/adr/0036-local-launcher-trust-boundary.md (status updated to Accepted).
  Modified principles:
    VII. Configuration Isolation and Deterministic Time: retain the options-record list,
       startup validation, process-launch, LocalPlay, and TimeProvider rules; record the
       narrowly scoped owner-approved exception allowing
       LocalPlayListenerValidationHostedService in LamuFlix.Api to inspect
       IConfiguration section Kestrel:Endpoints and endpoint Url values solely to prove
       pre-bind effective listeners loopback when Features:LocalPlay is true (DEV-458;
       ADR 0036).
  Added sections: none
  Removed sections: none
  Templates reviewed (no updates required):
    .specify/templates/plan-template.md — principle-based Constitution Check; no edit needed.
    .specify/templates/spec-template.md — no configuration-binding enumeration; no edit needed.
    .specify/templates/tasks-template.md — generic ADR/constitution tasks; no edit needed.
  Migration / follow-up:
    LocalPlayListenerValidationHostedService already landed on main in PR #352. No schema
    or code migration is required.

Version change: 1.10.0 → 1.11.0 (MINOR: Principle I named-port enumeration gains
ILibraryFileReader for DEV-456; ADR 0037)

Amendment 1.11.0 (2026-11-08), DEV-456 Build pickup:
  Authority: specs/DEV-456/brief.md Q10 and Q11 item 5;
    specs/DEV-456/CONCLUSIONS.md Q10/Q11 and T000 spec-PR #338 merge receipt.
  ADR: docs/adr/0037-library-file-reader-port.md (verified present; remains Proposed).
    This amendment does not automatically Accept the ADR or claim implementation approval.
  Modified principles:
    I. Ports and Adapters (title unchanged): add only ILibraryFileReader beside
       IMediaLibraryScanner. Preserve every prior entry, project-reference rule,
       BCL/M.E.AI abstraction, ADR requirement and architecture-enforcement rule.
  Added sections: none
  Removed sections: none
  Templates reviewed (no updates required):
    .specify/templates/plan-template.md — Constitution Check derives current principles;
      no duplicated named-port list.
    .specify/templates/spec-template.md — no named-port inventory or new required section.
    .specify/templates/tasks-template.md — existing ADR/constitution gates remain applicable.
    .specify/templates/commands/ — absent; no command templates to synchronize.
  Runtime guidance reviewed:
    README.md and AGENTS.md — no duplicated named-port enumeration requiring an edit.
  Migration / follow-up:
    No schema/data migration or existing-code rewrite is required by this enumeration addition.
    T003 must introduce ILibraryFileReader and append only |ILibraryFileReader before )$
    in ArchitectureTests PortWhitelistPattern atomically, preserving the existing 16 entries,
    order, selector, count floor and test body (brief Q10 prerequisite evidence update;
    T001c; recon-DEV-456-3 section 3). T003 remains future implementation work.
    Prior constitution omissions IEnrichmentOutbox / IRecommendationProfileSource remain
    outside this amendment; DEV-467 tracks that follow-up. No omitted-port backfill here.
    Principle I implementation compliance still requires the atomic port/whitelist evidence;
    this document amendment alone supplies no build/runtime PASS or merge clearance.
  Deferred placeholders / template follow-up: none

Version change: 1.9.0 → 1.10.0 (MINOR: Principle I named-port enumeration gains the two
DEV-333 import ports; Principle IV Movie mutator/claim/sweeper inventories and Enrichment
Reliability Rules gain the quota-day deferral surfaces; ADR-0032)

Amendment 1.10.0 (2026-11-02), DEV-333 delivery alignment:
  Authority: ticket DEV-333 Scope items 1–5; specs/DEV-333/brief.md Q1–Q8;
    specs/DEV-333/CONCLUSIONS.md (Patron rulings, Plan-challenge round 1 §1).
  ADR: docs/adr/0032-recursive-import-ports-and-scope-seam.md. Its status stays Proposed
    until the applicable approval evidence exists; this drafting does not Accept it and
    claims no DEV-333 Gate 1 or owner approval.
  Modified sections:
    I. Ports and Adapters: `ILibraryFolderDiscovery` and `IMovieFolderImporter` added to
       the closed named-port enumeration beside `IMediaLibraryScanner`, retaining every
       existing port, the BCL/M.E.AI abstractions and the `Adding a port requires an ADR.`
       sentence.
    IV. Enrichment Is an Explicit State Machine: Movie mutator inventory adds
       `DeferEnrichmentUntil(DateTimeOffset)` (sets `EnrichmentDeferredUntil`, no status
       change); claim bullet adds the deferral predicate and atomic clear; sweeper bullet
       adds future-deferral exclusion and post-expiry eligibility.
    Enrichment Reliability Rules: one quota-deferral settlement paragraph distinguishing
       same-attempt durable quota-deferred TTL dispatch from failure retries. The DEV-439
       `EnrichmentRetryPolicy` `QuotaExhausted` arm stays unchanged.
  Added / removed sections: none
  Templates reviewed:
    .specify/templates/plan-template.md — principle-based Constitution Check; no edit needed.
    .specify/templates/spec-template.md — no named-port/mutator inventory; no edit needed.
    .specify/templates/tasks-template.md — generic ADR/constitution tasks; no edit needed.
    .specify/templates/checklist-template.md — generic checklist headings; no edit needed.
    .specify/templates/constitution-template.md — template boilerplate only; no edit needed.
  Migration / follow-up:
    `DeferEnrichmentUntil` and `EnrichmentDeferredUntil` land with the DEV-333 deferral
    schema/settlement work; the constitution inventory is recorded now so code and rulebook
    are aligned. Recheck the landed constitution and dependency contracts at pickup.
    Ordinary ADR/Governance alignment, not a constitution departure: no new owner checkbox.

Version change: 1.8.0 → 1.9.0 (MINOR: Principle IV EnrichmentFailureCategory closed list adds
QuotaExhausted; DEV-439)

Amendment 1.9.0 (2026-11-01), proposed for Phase A spec review:
  Authority: ticket DEV-439 Scope item 6 / AC 6; specs/DEV-439/brief.md Q1/Q6;
    specs/DEV-439/CONCLUSIONS.md Q1/Q6. ADR: none (Patron Q6 — no new ADR).
  Modified sections:
    IV. Enrichment: EnrichmentFailureCategory closed list adds QuotaExhausted;
       retry parenthetical notes QuotaExhausted shares the TTL retry-queue path with
       RateLimited until DEV-333. Classifier single-home rule retained.
  Added / removed sections: none
  Templates reviewed:
    .specify/templates/plan-template.md — principle-based Constitution Check; no edit needed.
    .specify/templates/spec-template.md — no EnrichmentFailureCategory list; no edit needed.
    .specify/templates/tasks-template.md — generic ADR/constitution tasks; no edit needed.
  Migration / follow-up:
    Phase B implements QuotaExhausted category/mapping/eligibility after owner Gate 1
    approval of this amendment. No production code changes in this draft.
    Owner Gate 1 remains open; Patron cannot close it. Owner rejection blocks DEV-333
    Scope 3 as blocked: structural.

Version change: 1.7.0 → 1.8.0 (MINOR: Core thumbnail port and options enumeration extension;
DEV-17; ADR-0033)

Amendment 1.8.0 (2026-10-30), proposed for Phase A spec review:
  Authority: specs/DEV-17/brief.md Q3/Q8/Q9; Patron rulings recorded in
    specs/DEV-17/CONCLUSIONS.md. ADR: docs/adr/0033-thumbnail-generator-port.md (Proposed).
  Modified sections:
    I. Ports and Adapters: add IThumbnailGenerator after IOmdbRequestGate, retaining
       every existing port, BCL/M.E.AI abstraction and the ADR requirement.
    VII. Configuration Isolation and Deterministic Time: add ThumbnailOptions to the
       options-record list; retain validation, process-launch and LocalPlay rules.
  Added / removed sections: none
  Templates reviewed:
    .specify/templates/plan-template.md — principle-based Constitution Check; no edit needed.
    .specify/templates/spec-template.md — no named-port/options list; no edit needed.
    .specify/templates/tasks-template.md — generic ADR/constitution tasks; no edit needed.
  Migration / follow-up:
    Phase B adds the new Core port/options and extends PortWhitelistPattern in
    tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs before adapter composition;
    preserve existing whitelist entries. No production/test code changes in this draft.
    Recheck the landed constitution and dependency contracts at Phase B pickup.
    This is the permitted enumeration-extension procedure, not a constitution departure;
    plan freeze, ordinary owner Gate 1 and implementation gates remain outstanding.

Version change: 1.6.0 → 1.7.0 (MINOR: Core port IOmdbRequestGate added to Principle I;
OmdbQuotaOptions added to Principle VII; IOmdbRequestGate and OmdbQuotaOptions added to
Principle VIII vendor-name exceptions; ADR-0031)

Amendment 1.7.0 (2026-10-28):
  Modified sections:
    ✏️ I. Ports and Adapters: `IOmdbRequestGate` added to the closed named-port
       enumeration immediately after `IEnrichmentStatusNotifier` (DEV-426; ADR-0031).
       The BCL/M.E.AI abstractions and the sentence `Adding a port requires an ADR.`
       are retained.
    ✏️ VII. Configuration Isolation and Deterministic Time: `OmdbQuotaOptions` added to
       the options-record list beside `OmdbOptions` / `TmdbOptions`.
    ✏️ VIII. Ubiquitous Language in English: vendor-name exception adds the Core port
       `IOmdbRequestGate` and the options record `OmdbQuotaOptions` in
       `LamuFlix.Core.Options` (ticket-named; DEV-426; ADR-0031). Existing
       `TmdbOptions` and provider tag-value constant exceptions are retained.
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — review at apply time; no port-enumeration, options-list,
       or vendor-exception references expected; record no-edit or required follow-up
  Follow-up:
    ✅ ADR-0031 accepted in the same delivery constraints as this amendment
       (`docs/adr/0031-omdb-request-gate.md`).
    ✅ Update `PortWhitelistPattern` in
       `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` to include
       `|IOmdbRequestGate` while preserving existing whitelist tokens.

Version change: 1.5.0 → 1.6.0 (MINOR: TMDb secondary metadata provider permitted beside
OMDb; `TmdbOptions` and the provider tag-value constants permitted in Core)

Amendment 1.6.0 (2026-10-28):
  Modified sections:
    ✏️ Technology Stack Constraints: Metadata provider row names the TMDb secondary adapter
       `TmdbMetadataProvider : IMetadataProvider` beside OMDb; OMDb stays the production
       provider; TMDb is disabled by default; provider selection is not defined by the row.
    ✏️ VII. Configuration Isolation and Deterministic Time: `TmdbOptions` added to the
       options-record list beside `OmdbOptions`.
    ✏️ VIII. Ubiquitous Language in English: vendor-name exception adds the options record
       `TmdbOptions` in `LamuFlix.Core.Options` and the provider tag-value constants
       `OmdbProviderName` and `TmdbProviderName` in `TelemetryConstants`.
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md: no reference to the metadata provider row or the vendor-name
       exception; no change
  Follow-up:
    ✅ ADR-0030 accepted in the same PR (`docs/adr/0030-tmdb-secondary-metadata-provider.md`).
    ✅ Owner approval recorded from PR #272 (`specs/DEV-331/constitution-amendment-v1.6.0.md`).
       PR #272 merged under 1.5.0; this amendment is the remedy under Governance, Compliance.

Previous version change: 1.4.0 → 1.5.0 (MINOR — Core port enumeration expanded with
`IEnrichmentStatusNotifier`; enrichment status delivery wording aligned to ADR-0022)

Amendment 1.5.0 (2026-10-27):
  Modified sections:
    ✏️ I. Ports and Adapters: `IEnrichmentStatusNotifier` added to the closed named-port
       enumeration immediately after `IMovieFeaturesRepository` (DEV-330; ADR-0029).
       The BCL/M.E.AI abstractions and the sentence `Adding a port requires an ADR.`
       are retained.
    ✏️ API and Contract Rules: enrichment status delivery sentence updated from
       polling-only P1 / SignalR stretch (ADR-0008) to SignalR push per ADR-0022
       (polling superseded for this channel only).
    ✏️ Known Technical Debt: removed the standalone “Polling for enrichment
       status instead of push (ADR-0008)” debt item so it no longer contradicts
       ADR-0022 / ADR-0029 (supersession already recorded on ADR-0008).
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no port-enumeration references; no edits required
  Follow-up: update `PortWhitelistPattern` in
    `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` to include
    `|IEnrichmentStatusNotifier` while preserving existing whitelist tokens

Previous version change: 1.3.0 → 1.4.0 (MINOR — Core port enumeration expanded with `IMovieFeaturesRepository`)

Amendment 1.4.0 (2026-10-22):
  Modified sections:
    ✏️ I. Ports and Adapters: `IMovieFeaturesRepository` added to the closed named-port
       enumeration immediately after `IPlaybackHistory` (DEV-413; ADR-0024). The BCL/M.E.AI
       abstractions and the sentence `Adding a port requires an ADR.` are retained.
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no port-enumeration references; no edits required
  Follow-up: none

Previous version change: 1.2.0 → 1.3.0 (MINOR — retired `LamuFlix.Worker` host removed; the enrichment
consumer is named as hosted in the Api)

Amendment 1.3.0 (2026-10-14):
  Modified sections:
    ✏️ I. Ports and Adapters: `LamuFlix.Worker` row removed from the project reference table.
       DEV-19 retired the project from the solution; ADR-0019 records that `EnrichmentConsumer`
       (Infrastructure) runs in the Api host and rejects resurrecting the Worker.
    ✏️ VI. Observability Across Processes: the trace spans API → RabbitMQ → `EnrichmentConsumer`
       (hosted in the Api) → metadata provider; the worker health-signal rule becomes the Api
       `/health/ready` check, which covers the consumer it hosts (DEV-325 C1-A: no `worker`
       compose service)
    ✏️ IX. Test Pyramid: integration row reads "consumer end-to-end" instead of "worker"
    ✏️ Enrichment Reliability Rules: "A worker MUST claim" becomes "The consumer MUST claim"
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no worker references; no edits required
  Follow-up:
    ⚠ The leftover `src/LamuFlix.Worker` folder is deleted by DEV-388.
    ⚠ README.md still lists the `LamuFlix.Worker.csproj` upgrade in its legacy notes; the README
      rewrite (DEV-23) owns it.

Previous version change: 1.1.0 → 1.2.0 (MINOR — closed-set types standardised on SmartEnum-style Enumerations)

Amendment 1.2.0 (2026-09-28):
  Modified sections:
    ✏️ I. Ports and Adapters: `Ardalis.SmartEnum` added to the packages `LamuFlix.Core` may reference
    ✏️ III. Filters: `MovieSort` / `SortDirection` are Enumerations, not plain enums
    ✏️ IV. Enrichment: `EnrichmentStatus` is `SmartEnum<EnrichmentStatus, int>` (numeric values
       unchanged, still the database contract); `EnrichmentFailureCategory` is an Enumeration
       carrying `IsRetryable` and the caller-safe message
    ✏️ Technology Stack Constraints: Category types row loses its plain-enum exceptions;
       Serialization row uses the SmartEnum System.Text.Json converters
    ✏️ Development Workflow → Coding Conventions: new **Enumerations** rule
    ✏️ Development Workflow → Pull Request Quality Gates: Enumeration checkbox added
    ✏️ Development Workflow → API and Contract Rules: Enumerations serialized by `Name`
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no enum references; no edits required
  Follow-up:
    ⚠ src/LamuFlix.Data/Models/MovieEnrichmentStatus.cs is a plain enum in the transitional
      layout (see Known Technical Debt); convert when Epic 1 replaces it with Core's
      `EnrichmentStatus`, or on contact.

Previous version change: 1.0.0 → 1.1.0 (MINOR — comment policy materially expanded)

Amendment 1.1.0 (2026-09-23):
  Modified sections:
    ✏️ Development Workflow → Coding Conventions → Comments: "unless required by best practice"
       replaced with "unless strictly necessary" (non-obvious WHY only); change narration, task
       references, and commented-out code explicitly banned; exemptions widened from AAA headers
       only to AAA headers, empty-block justifications, and scoped warning suppressions
       (aligns with the existing justification requirement in Static-Analysis Gates).
       Wording adapted from FMC.GrantEval constitution ("No gratuitous comments").
    ✏️ Development Workflow → Pull Request Quality Gates: comment checkbox updated to match
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no comment-policy references; no edits required
  Follow-up (upstream, harness-generated — do not edit locally, reinstall overwrites):
    ⚠ AGENTS.md, .claude/agents/test-writer.md, .codex/agents/test-writer.toml,
      .claude/skills/testing/SKILL.md, .agents/skills/testing/SKILL.md,
      .claude/rules/pipeline/coding-conventions.mdc, .cursor/rules/coding-conventions.mdc
      still call AAA headers "the one exception". Fix in dotnet-agent-harness; until then the
      constitution prevails (see Governance).

Ratification 1.0.0 (2026-09-21):

Source documents:
  - docs/architecture/architecture-plan.md (reviewed 2026-09-21 against commit 9c4765c, DEV-16)
  - CLAUDE.md and .claude/rules/vendor/aaron-*.md (coding style, testing, SDK/dependency rules)

Added sections:
  ✅ Core Principles I–IX
  ✅ Technology Stack Constraints (mandated + forbidden choices)
  ✅ Development Workflow → Pull Request Quality Gates
  ✅ Development Workflow → Static-Analysis Gates (Roslyn, CA1502, InspectCode)
  ✅ Development Workflow → Coding Conventions
  ✅ Development Workflow → Enrichment Reliability Rules
  ✅ Development Workflow → API and Contract Rules
  ✅ Development Workflow → Documentation Rules
  ✅ Development Workflow → Known Technical Debt (Acknowledged Risks)
  ✅ Governance

Modified sections: N/A (initial version)
Removed sections: N/A (initial version)

Templates reviewed:
  ✅ .specify/templates/plan-template.md      — Constitution Check section applies as-is
  ✅ .specify/templates/spec-template.md      — no constitution-specific edits required
  ✅ .specify/templates/tasks-template.md     — Polish phase tasks added: three gate scripts + format check; CONTEXT.md/ADR update
  ✅ CLAUDE.md                                 — gate commands and MCP routing already documented
  ✅ .claude/rules/pipeline/test-assertions.mdc — project-owned override of the vendored xUnit-Assert guidance (Shouldly, NSubstitute, AutoFixture, Faker.Net); mirrored to .cursor/rules/; deviation recorded in NOTICE
  ✅ scripts/run-roslyn-analyzers.ps1          — gate script present
  ✅ scripts/run-cyclomatic-complexity.ps1     — gate script present (CA1502 threshold 15)
  ✅ scripts/run-jetbrains-inspectcode.ps1     — gate script present

Deferred TODOs:
  - Files this constitution references that do not exist yet and are Epic 1 deliverables:
    global.json, Directory.Packages.props, docs/adr/. (BannedSymbols.txt and CONTEXT.md
    landed on main with DEV-359.) Until they land, the ADR rule is enforced by review.
  - Sections that describe the TARGET layout (src/, tests/, LamuFlix.Core, ...) are binding for new
    code from Epic 1 onward. Until Epic 1 lands, the current LamuFlix.Web / LamuFlix.Data /
    LamuFlix.Work / LamuFlix.Test projects remain in place; see Known Technical Debt.
  - ADR numbering (ADR-0001 … ADR-0012) follows the architecture plan; renumber here if the plan
    is renumbered.
-->

# LamuFlix Constitution

## Core Principles

### I. Ports and Adapters with a Feature-Organised Core (NON-NEGOTIABLE)

The solution is ONE bounded context (**Movies**). It is NOT vertical-slice architecture (slices
do not own their own data access) and it is NOT four-layer Clean Architecture (no separate
Domain assembly). The application core is organised BY FEATURE, and the real seams are the four
external boundaries: **file system**, **metadata provider**, **message queue**, and **media
player process**. Each seam is a port in Core with an adapter in Infrastructure.

Project reference rules are strict and unidirectional:

| Project | May reference |
|---|---|
| `LamuFlix.Core` | BCL, `Microsoft.Extensions.Logging.Abstractions`, `System.Collections.Immutable`, `Ardalis.SmartEnum` only |
| `LamuFlix.Infrastructure` | Core |
| `LamuFlix.Api` | Core, Infrastructure, ServiceDefaults |
| `LamuFlix.ServiceDefaults` | nothing application-specific |

- Core MUST NOT reference EF Core, Npgsql, RabbitMQ, `System.IO` process APIs, or any HTTP client.
- Feature folders in Core (`Library`, `Import`, `Enrichment`, `Playback`, `Watchlist`,
  `Recommendations`, `WatchHistory`, `Subtitles`) MUST NOT reference each other; shared concepts live in
  `Domain/`, `Ports/`, or `Pipeline/`.
- Ports live in `Core/Ports/`; the only ports are `IMovieRepository`, `IMovieCatalog`,
  `IMetadataProvider`, `IEnrichmentQueue`, `IMediaLibraryScanner`, `ILibraryFileReader`, `ILibraryFolderDiscovery`,
  `IMovieFolderImporter`, `IMediaPlayerLauncher`,
  `IRecommendationCandidateSource`, `IRecommendationProfileSource`, `IPlaybackHistory`, `IMovieFeaturesRepository`,
  `IEnrichmentOutbox`, `IEnrichmentStatusNotifier`, `IOmdbRequestGate`, `IThumbnailGenerator`,
  `ISubtitleProvider`, `ISubtitleSidecarStore`, and the BCL/M.E.AI abstractions `TimeProvider` and
  `IEmbeddingGenerator<string, Embedding<float>>`. Adding a port requires an ADR.
- These rules MUST be enforced by `LamuFlix.ArchitectureTests`; a violation fails the build.

**Rationale**: One aggregate with eight use cases is the right size for feature folders, and the
four seams are exactly the things that are slow, non-deterministic, or machine-specific. Naming
them keeps every testability, substitutability, and resilience concern in one place without the
ceremony of extra assemblies. The README MUST describe the style honestly as "ports and adapters,
feature-organised core, explicit handlers with decorators" — not as VSA or Clean Architecture.

### II. Explicit Handlers and Decorators (No MediatR)

Every use case is an `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>`
in its feature folder. Commands and queries MUST be `sealed record`s. Handlers MUST be `sealed`.

Cross-cutting behaviour is applied by hand-wired decorators registered explicitly through a
`services.AddHandler<THandler, TCommand, TResult>()` helper. Decorator order, outer to inner,
MUST be: **Tracing → Logging → Validation → handler**. No other mechanism (MediatR, pipeline
behaviours, reflection scanning, source-generated buses) may dispatch handlers.

Expected domain failures are the three exceptions `NotFoundException`, `ValidationException`,
and `InvalidTransitionException` (plus `FeatureDisabledException` for gated features). They are
mapped to HTTP once, in a single `IExceptionHandler` in `LamuFlix.Api`. Expected domain failures
also include `LibraryFileAccessDeniedException` for library file policy and containment denial.
A `Result<T>` type is NOT introduced unless an ADR justifies it.

**Rationale**: Explicit registration makes the call chain readable in one file, keeps decorators
unit-testable without a framework, and matches the original reference implementation. Three
failure kinds plus one mapper is simpler than a Result monad and aligns with the C# style rules.

### III. Filters Are a Typed Query Model (NON-NEGOTIABLE)

Browsing and filtering is the star feature; the filter model MUST be the best-designed code in
the repository. Reflection-based dynamic queries and string-named sort properties are forbidden.

- `MovieQuery` is a `sealed record` with typed members (`Text`, `GenreIds`, `ActorIds`,
  `RuntimeRange`, `YearRange`, `Statuses`, `InWatchlist`, `MovieSort`, `SortDirection`, `Page`).
- `MovieSort` and `SortDirection` are Enumerations (see Coding Conventions). Sorting dispatches
  over the closed `MovieSort` set — an explicit whitelist, never a property name. `NULLS LAST`
  semantics MUST be preserved for nullable sort keys.
- Each predicate is one small `IQueryable<Movie>` extension (`WhereText`, `WhereGenres`,
  `WhereRuntime`, …) in Infrastructure; the browse projection goes straight to `MovieSummary`
  with no entity tracking.
- `MovieQuery` MUST be validated by a FluentValidation validator (page size ≤ 100, `Min ≤ Max`,
  at least one sort) executed by the validation decorator.
- The query string is the single source of truth for filter state in the web app; a
  property-based test MUST prove `MovieQuery` round-trips through a query string unchanged.

**Rationale**: Reflection over user-supplied property names is both an injection surface and
an unreviewable design. A typed model gives exact OpenAPI, exhaustive tests, and a UI whose URL
state is provably faithful.

### IV. Enrichment Is an Explicit State Machine (NON-NEGOTIABLE)

`Movie` is a class (EF-tracked, private setters) with intent-revealing transition methods:
`MarkEnriched`, `MarkNotFound`, `MarkFailed`, `RequestEnrichment`, `DeferEnrichmentUntil`,
`AddToWatchlist`, `RemoveFromWatchlist`. Illegal transitions throw `InvalidTransitionException` (HTTP 409).
`DeferEnrichmentUntil(DateTimeOffset)` records `EnrichmentDeferredUntil` from Pending only and
changes no status.

- `EnrichmentStatus` is a `SmartEnum<EnrichmentStatus, int>` with `Pending = 0, Enriched = 1,
  NotFound = 2, Failed = 3`, persisted as its `Value`; the numeric values are part of the
  database contract and MUST NOT change.
- Claiming work MUST be atomic: `IMovieRepository.TryClaimForEnrichmentAsync` performs a single
  `UPDATE … WHERE Status = Pending` (`ExecuteUpdateAsync`). Exactly one of two concurrent claims
  wins; an integration test MUST prove it. A claim older than the lease window is free. A claim
  MUST also refuse a row whose `EnrichmentDeferredUntil` is still in the future, and an eligible
  claim clears that deadline atomically with the claim bookkeeping.
- Failures are classified into the `EnrichmentFailureCategory` Enumeration (`ProviderUnavailable`,
  `RateLimited`, `InvalidResponse`, `Unknown`, `QuotaExhausted`); each member carries its own
  `IsRetryable` and caller-safe message. Classification
  from the root-cause exception lives in exactly one `EnrichmentFailureClassifier`.
- Retry is category-driven: retryable and attempts < max → republish with `Attempt + 1`
  (`RateLimited` and `QuotaExhausted` via the TTL retry queue); otherwise `MarkFailed(category)`
  and dead-letter. Once chain-final quota settlement is delivered, `QuotaDeferred` is settlement,
  not retry; the landed `QuotaExhausted` retry arm is retained until then.
- A `StrandedMovieSweeper` MUST periodically re-enqueue `Pending` rows whose last attempt is
  null or older than the lease, covering lost messages, failed publishes, and claim-then-crash.
  Rows deferred into the future are excluded while the deadline is unexpired and become eligible
  again after it expires, so the sweeper is the post-expiry backstop.
- Raw exception text MUST NOT be persisted to the row or returned from the API. It goes to logs
  and traces only.

**Rationale**: Idempotency, bounded retries, and a self-healing sweeper are what make the
asynchronous pipeline trustworthy without a transactional outbox. Separating the caller-safe
category from the exception type keeps the taxonomy stable while implementations change.

### V. Validated Inputs, Consistent Error Responses

All commands, queries, and request contracts MUST be validated through FluentValidation in the
validation decorator. Validation failures MUST return `422 Unprocessable Entity`.

Every error response MUST be an RFC 7807 `ProblemDetails` produced by `AddProblemDetails()` and
one `IExceptionHandler`, and MUST include `traceId`. The exception-to-status mapping lives ONLY
in that handler:

| Exception | Status |
|---|---|
| `ValidationException` | 422 |
| `NotFoundException` | 404 |
| `InvalidTransitionException` | 409 |
| `FeatureDisabledException` | 403 |
| `LibraryFileAccessDeniedException` | 403 |
| Unhandled | 500 |

Endpoints MUST NOT return `BadRequest(ex.Message)` or otherwise leak exception text.

**Rationale**: One mapping and one error shape keep the React client's error handling trivial
and prevent status-code drift between endpoints.

### VI. Observability Across Processes (NON-NEGOTIABLE)

One trace MUST span API → RabbitMQ → `EnrichmentConsumer` (hosted in the Api) → metadata provider.

- Publishers inject `traceparent`/`tracestate` into message headers via
  `Propagators.DefaultTextMapPropagator`; consumers extract them and start a consumer Activity,
  using an `ActivityLink` on redelivery.
- All span names, metric names, and attribute keys live in `TelemetryConstants`
  (`ActivitySource` "LamuFlix"; spans `Enrichment.Enqueue`, `Enrichment.Process`,
  `Metadata.Lookup`; metrics `lamuflix.enrichment.duration`, `lamuflix.enrichment.outcome`,
  `lamuflix.import.count`; attributes `lamuflix.movie.id`, `messaging.rabbitmq.delivery_count`,
  `error.type`). Ad-hoc string literals for telemetry names are forbidden.
- Logging is structured (Serilog) and every error or processing event MUST carry the movie id
  and, where applicable, the attempt number and failure category.
- Health endpoints MUST exist and be reachable without authentication: `GET /health/live`
  (no dependency checks) and `GET /health/ready` (Postgres, RabbitMQ). Every new external
  dependency MUST register a health check. The Api `/health/ready` check is the compose health
  signal for the enrichment consumer it hosts.
- `ServiceDefaults` owns Serilog, OpenTelemetry (OTLP), health checks, options validation, and
  `TimeProvider` registration; hosts MUST NOT duplicate that wiring.

**Rationale**: A single clickable trace across processes is the most persuasive artefact of the
project and the primary tool for diagnosing the asynchronous pipeline.

### VII. Configuration Isolation and Deterministic Time

Secrets and machine-specific paths MUST NOT appear in source. There are NO hard-coded fallbacks
for connection strings, API keys, library roots, or player paths.

- All configuration is bound to options records (`LibraryOptions`, `PlaybackOptions`,
  `OmdbOptions`, `OmdbQuotaOptions`, `TmdbOptions`, `RabbitMqOptions`, `EnrichmentOptions`, `FeatureOptions`,
  `RecommendationOptions`, `ThumbnailOptions`, `SubtitlesOptions`, `SubtitleProviderOptions`)
  with data-annotation validation and `ValidateOnStart()`, except for subtitle acquisition
  settings under the narrowly scoped rule below. Reading
  `IConfiguration` by magic string outside options binding is forbidden, except for the
  narrowly scoped owner-approved reader in `LocalPlayListenerValidationHostedService`
  (`LamuFlix.Api`), which may inspect `Kestrel:Endpoints` section presence and endpoint `Url`
  values solely to prove effective listeners loopback before server bind when
  `Features:LocalPlay` is true (DEV-458; ADR-0036).
- **Subtitle acquisition exception (DEV-453; owner C4/VII approval on PR #358):**
  `SubtitlesOptions` and nested `SubtitleProviderOptions` MUST remain typed options;
  the vendor configuration path remains `Subtitles:OpenSubtitles`. Missing or invalid
  subtitle acquisition settings MUST NOT fail host startup via `ValidateOnStart()` or
  prevent existing-track serving. After authorization and the existing-sidecar check,
  `POST /movies/{id}/subtitles/prepare` MUST evaluate acquisition settings and return
  `409 Conflict` when missing or invalid. No guessed numeric safety defaults are allowed;
  `SaveEnabled` defaults false and language preferences default empty. Secret isolation
  remains mandatory. This exception applies only to subtitle acquisition validation;
  all other options retain their startup-validation requirements.
- Local values live in `dotnet user-secrets` or `appsettings.Development.json` (git-ignored);
  containers receive them as environment variables.
- Player mapping is configuration (`PlaybackOptions.Players[]`), never a database table.
- Local playback is registered only when `Features:LocalPlay` is true; otherwise a
  `DisabledMediaPlayerLauncher` throws `FeatureDisabledException`. Process launches MUST use
  `ProcessStartInfo.ArgumentList` — never a shell or a concatenated command line.
- Timestamps MUST come from an injected `TimeProvider`. `DateTime.Now`, `DateTime.UtcNow`,
  `DateTime.Today`, and `DateTimeOffset.Now` are banned via `BannedSymbols.txt`.

**Rationale**: The current codebase carries hard-coded MySQL credentials, an OMDb key, and
`F:/Filmes`. Validated options and banned symbols make those regressions a build failure.

### VIII. Ubiquitous Language in English

Domain terms are defined in `CONTEXT.md` (term, definition, avoid). Identifiers MUST use those
terms. Portuguese identifiers (`Filme`, `CriarFilme`, `AssistirFilme`, `ExcluirFilme`,
`MinhaLista`, `ProcessarFilme`, …) are forbidden and MUST be renamed on contact.

- **Movie** (not Filme, Film) · **Library** · **Import** (not Criar, Create, Scan) ·
  **Enrichment** (not job, task, processing) · **Enrichment Status** · **Failure Category**
  (not error type, exception) · **Metadata Provider** (never OMDb in Core names) ·
  **Watchlist** (not MinhaLista, My List) · **Playback** (not Assistir, Watch) ·
  **Recommendation** · **Reason** · **Taste Profile** · **Playback Event** · **Feedback**.
- Types, members, and telemetry identifiers MUST be named by functionality, not by vendor
  (`IMetadataProvider` and `Metadata.Lookup`, not `IOmdbClient` and `Omdb.Lookup`). Vendor
  names are permitted only on the Infrastructure adapter class itself, on vendor-mandated
  configuration keys, on the Core port `IOmdbRequestGate`, on the options records
  `OmdbQuotaOptions` and `TmdbOptions` in `LamuFlix.Core.Options`, and on the provider
  tag-value constants `OmdbProviderName` and `TmdbProviderName` in `TelemetryConstants`.
- A new domain term MUST be added to `CONTEXT.md` in the same PR that introduces it.

**Rationale**: A single vocabulary shared by code, docs, API, and UI is what makes the bounded
context legible to a reviewer and survives provider or library swaps without renames.

### IX. Test Pyramid with Real Infrastructure (NON-NEGOTIABLE)

| Layer | Project | Tools |
|---|---|---|
| Domain, handlers, filters | `LamuFlix.UnitTests` | xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net |
| Property tests | `LamuFlix.UnitTests` | FsCheck.Xunit |
| Persistence, messaging, API, consumer end-to-end | `LamuFlix.IntegrationTests` | Testcontainers (Postgres, RabbitMQ), `WebApplicationFactory`, WireMock.Net, Verify |
| Architecture | `LamuFlix.ArchitectureTests` | NetArchTest.Rules or ArchUnitNET |
| Mutation | `LamuFlix.UnitTests` | Stryker, break threshold 80 on `LamuFlix.Core` |

- xUnit v3 is the only test framework; MSTest and NUnit are forbidden.
- Assertions use Shouldly. xUnit's built-in `Assert` is acceptable only where Shouldly has no
  equivalent (e.g. `Assert.Collection`). FluentAssertions MUST NOT be introduced: versions
  8+ are under a paid commercial licence.
- Test data is built with AutoFixture (anonymous objects, `[AutoData]` theories) and Faker.Net
  (realistic titles, names, years). Hand-written builders are reserved for domain invariants
  that AutoFixture cannot satisfy (e.g. a `Movie` in a specific `EnrichmentStatus`).
- Mocking uses NSubstitute; Moq is forbidden. Infrastructure MUST NOT be mocked — use
  Testcontainers for Postgres and RabbitMQ, WireMock.Net for the metadata provider, and
  `MockFileSystem` (System.IO.Abstractions) for the scanner. EF Core InMemory is forbidden.
- Tests follow Arrange-Act-Assert, use `Theory` + `MemberData` over repeated `Fact`s, use fresh
  data per test, and never call `.Result`/`.Wait()`/`.GetAwaiter().GetResult()`.
- The claim-atomicity, retry-queue, DLQ, sweeper, and single-trace-id end-to-end scenarios
  MUST each have an integration test.

**Rationale**: The plan's guarantees (atomic claim, NULLS LAST, trace propagation) are only
true against the real engine. Mutation testing on Core is what proves the state machine tests
mean something.

## Technology Stack Constraints

The following choices are established and MUST NOT be changed without a constitution amendment
and an ADR.

| Concern | Mandated Choice | Notes |
|---|---|---|
| Runtime | .NET 10 / C# 14, `global.json` pinned, `rollForward: latestPatch` | `Nullable` enable, `TreatWarningsAsErrors` true, central package management |
| Web host | ASP.NET Core Minimal API, `TypedResults`, `Results<…>` unions | No MVC controllers, Razor, or session |
| Database | PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` | One `IEntityTypeConfiguration<T>` per entity; skip navigations; single fresh `Initial` migration |
| Vector store | pgvector via `Pgvector.EntityFrameworkCore` | HNSW index on cosine distance (Recommendations Phase B) |
| Message queue | RabbitMQ via `RabbitMQ.Client` 7.x (`IChannel`, `AsyncEventingBasicConsumer`) | Quorum queues; publisher confirms; topology in one `RabbitMqTopology` class |
| Metadata provider | OMDb behind `IMetadataProvider`, typed `HttpClient`, is the production provider; TMDb secondary adapter `TmdbMetadataProvider : IMetadataProvider`, typed `HttpClient`, is registered beside it and is disabled by default (`Tmdb:Enabled` false); provider selection is not defined by this row (ADR-0030) | `Microsoft.Extensions.Http.Resilience`: retry with jitter, `Retry-After`, timeout, circuit breaker; each typed client owns its own pipeline |
| Embeddings | `IEmbeddingGenerator<string, Embedding<float>>` (Microsoft.Extensions.AI) | Ollama via OllamaSharp at runtime; deterministic `FakeEmbeddingGenerator` in tests |
| File system | `System.IO.Abstractions` | `MockFileSystem` in unit tests |
| Time | `TimeProvider` | `DateTime.Now` family banned |
| Validation | FluentValidation, executed by the validation decorator | |
| Test tooling | xUnit v3, NSubstitute, Shouldly, AutoFixture, Faker.Net | See Principle IX |
| Category types | Enumerations: `Ardalis.SmartEnum` (or an equivalent `Enumeration` base class in `Core/Domain/`) | Plain `enum` forbidden for project-owned types; see Coding Conventions |
| Serialization | `System.Text.Json`; `Ardalis.SmartEnum.SystemTextJson` name converters for Enumerations | Newtonsoft.Json MUST NOT be introduced |
| Object mapping | Mapster | `IRegister` configs live beside the feature they serve; AutoMapper MUST NOT be introduced |
| Logging | Serilog (console JSON; OTLP/Seq optional) | Wired in ServiceDefaults |
| Telemetry | OpenTelemetry traces/metrics/logs, OTLP exporter | Aspire dashboard as local target |
| API docs | `Microsoft.AspNetCore.OpenApi` + Scalar at `/scalar` | Document committed at `web/src/api/openapi.json` |
| Web app | Vite + React + TypeScript strict, TanStack Query, Zod, `openapi-typescript` + `openapi-fetch` | Vitest + React Testing Library + MSW |
| Containers | Multi-stage Dockerfiles, `docker-compose.yml`, `docker-compose.observability.yml` | Health checks and dependency ordering |
| CI | GitHub Actions (`.github/workflows/ci.yml`) | build, test, format, web lint/test/build, OpenAPI drift check |

**Forbidden**: MediatR, AutoMapper, Moq, MSTest, NUnit, FluentAssertions (paid licence; use
Shouldly), Bogus (use Faker.Net),
EF Core InMemory provider, Pomelo/MySQL, Newtonsoft.Json, `System.Reflection`-driven query or
sort building, `cmd.exe`/shell-based process launching.

## Development Workflow

### Pull Request Quality Gates

Every PR MUST satisfy all of the following before merging:

- [ ] Project reference rules respected; `LamuFlix.ArchitectureTests` passes
- [ ] New use cases are sealed record commands/queries with sealed handlers registered via
      `AddHandler<…>`; no MediatR or reflection dispatch introduced
- [ ] New ports have an ADR; adapters live in `LamuFlix.Infrastructure`
- [ ] New filters extend `MovieQuery` and the sort whitelist; no dynamic property access
- [ ] New closed sets of values are Enumerations (`Ardalis.SmartEnum` or equivalent); no new
      project-owned C# `enum`
- [ ] New `Movie` state transitions are validated methods with unit tests for every legal and
      illegal transition
- [ ] New endpoints use `TypedResults`, are covered by `WebApplicationFactory` integration
      tests, and update the committed OpenAPI document (drift check green)
- [ ] All error paths produce `ProblemDetails`; new exception types are mapped in the single
      `IExceptionHandler`
- [ ] New external dependencies register a health check and telemetry names in
      `TelemetryConstants`
- [ ] No secrets, machine paths, or `IConfiguration` magic strings; new options records are
      validated at startup, except subtitle acquisition settings under Principle VII
- [ ] No `DateTime.Now`-family calls; `TimeProvider` injected
- [ ] `CancellationToken` flows through every async path; no sync-over-async
- [ ] New domain terms added to `CONTEXT.md`; no Portuguese identifiers introduced
- [ ] Tests use xUnit v3 + NSubstitute + Shouldly + AutoFixture/Faker.Net;
      infrastructure via Testcontainers, WireMock.Net, or `MockFileSystem`
- [ ] No comments added unless requested or strictly necessary; the only exemptions are AAA
      test headers, empty-block justifications, and scoped warning suppressions (see Coding
      Conventions)
- [ ] New or modified `.cs` files pass all three static-analysis gates (see below)
- [ ] `dotnet format --verify-no-changes` passes

### Static-Analysis Gates

New or materially changed C# code MUST pass all three gates before a task or PR is considered
complete. They are NOT interchangeable; `dotnet build` alone surfaces none of them reliably.

| Gate | Script | Requirement |
|---|---|---|
| Roslyn analyzers (CA/IDE at warning+) | `./scripts/run-roslyn-analyzers.ps1` | Zero warnings |
| Cyclomatic complexity (CA1502) | `./scripts/run-cyclomatic-complexity.ps1` | No method > **15**; refactor gate `-Threshold 6` after implementation works |
| JetBrains InspectCode | `./scripts/run-jetbrains-inspectcode.ps1` | Zero issues at WARNING or higher |

- Scope is new or modified files (diff against `main`) unless `-All` or `-Files` is used.
- A gate whose analyzer is not actually enabled exits 1 with remediation; a skipped gate is
  never a passed gate.
- Complexity failures MUST be fixed by extracting private helpers, early returns, and guard
  clauses — not by suppression. `#pragma warning disable` requires a brief justification.
- Intentional InspectCode violations MUST use a targeted ReSharper suppression with a brief
  justification.

**Rationale**: Each gate catches a different class of problem. Automating them during agent
implementation prevents drift that would otherwise only surface when a developer opens Rider.

### Coding Conventions

Code MUST follow `.claude/rules/vendor/aaron-csharp-coding-style.md` and these project-specific rules:

- **Types**: data types are `sealed record`s; classes are `sealed` unless designed for
  inheritance; identifiers and constrained values are value objects (`MovieId`, `ImdbId`,
  `ImdbRating`, `Runtime`, `ReleaseYear`, `LibraryPath`, `MediaFormat`) with `TryParse`-style
  factories. Collections in records are `System.Collections.Immutable`.
- **Enumerations**: every closed set of named values (statuses, categories, sort keys,
  directions, formats) MUST be an Enumeration — `Ardalis.SmartEnum` (`SmartEnum<T>` or
  `SmartEnum<T, TValue>`) or an equivalent sealed `Enumeration` base class — never a C#
  `enum`. Behaviour that varies by member (flags such as `IsRetryable`, messages, sort
  expressions, allowed transitions) lives on the member instead of in `switch` statements
  scattered across callers. Parsing uses `TryFromName`/`TryFromValue`, never casts from `int`
  or `Enum.Parse`. EF Core persists the `Value` through a value converter; JSON uses the name
  converter. A plain `enum` is permitted only where an external API requires one (BCL or
  third-party types such as `HttpStatusCode`, `[Flags]` interop); it MUST NOT leak into Core
  signatures.
- **Comments**: code MUST NOT carry comments (inline `//`, XML `///`, block) unless the user
  explicitly requests one or it is strictly necessary — a non-obvious WHY the code cannot
  express, such as a hidden constraint or a workaround for a specific external bug. Names carry
  the WHAT. Comments that restate the code, narrate a change, reference a task, ticket, or
  caller, or hold commented-out code MUST be removed. Only three uses are exempt:
  - **Test structure**: `// arrange`, `// act`, `// assert` section headers in test methods.
  - **Empty-block justification**: one comment inside an intentionally empty block (a
    swallowed `catch`, a no-op override, an empty `default:`) stating why it is empty.
  - **Temporary warning suppression**: `#pragma warning disable`/`restore` pairs and
    `// ReSharper disable once …` directives, scoped to the fewest lines possible, with a brief
    justification on the same line.
- **Interface placement**: ports live in `Core/Ports/`, separate from their adapters. Any other
  interface MUST be defined in the same file as its primary implementation.
- **Naming**: functionality-based, English, per `CONTEXT.md`; `nameof` for every symbol
  reference in exceptions, logging, and attributes.
- **Async**: no `async void`; `CancellationToken` flows end to end; `await` over
  `ContinueWith`; never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`;
  `CancellationTokenSource` disposed.
- **Safety**: `Try*` methods over exception-driven parsing; `TryGetValue` over indexers;
  `Uri.TryCreate` over `new Uri`.
- **Dependencies**: constructor injection kept minimal; a handler that needs more than three
  ports signals a missing abstraction.
- **Packages**: added with `dotnet add package`, versions in `Directory.Packages.props`;
  no floating versions; `dotnet list package --vulnerable` clean.

### Enrichment Reliability Rules

- The `EnrichmentRequested` message carries `MovieId` and `Attempt` ONLY. Title, year, and IMDb
  id are read from the row at processing time so a message can never be stale.
- The consumer MUST claim (`TryClaimForEnrichmentAsync`) before calling the metadata provider; a
  false claim is acknowledged and ignored.
- The consumer (`EnrichmentConsumer`) is thin: it resolves a scope per message, calls Core
  handlers, and translates outcomes to ack / nack / republish. All decisions live in Core
  handlers so they are unit-tested without RabbitMQ.
- Retry policy is owned by the failure category and `EnrichmentOptions` (max attempts, lease,
  sweep interval). Application code MUST NOT implement ad-hoc retry loops.
- The insert-then-enqueue dual write is mitigated by the sweeper (ADR-0005); a transactional
  outbox is the documented stretch. Silently ignoring a failed publish is forbidden.
- Graceful shutdown: `stoppingToken` flows into handlers; an in-flight message finishes or is
  nacked with requeue on cancellation.
- `FeaturesRequested` (Recommendations) reuses the same claim, retry, DLQ, category, and
  sweeper machinery; it MUST NOT fork a second pipeline.
- Quota-day deferral is settlement, not a failure retry. Chain-final `QuotaExhausted` leaves the
  row Pending, records `EnrichmentDeferredUntil` on the same attempt, writes no failure category,
  and dispatches the durable quota-deferred TTL route; a non-final quota remains the landed
  chain-fallback case with zero deferral, and HTTP 429 keeps its bounded retries. Claim-before-
  provider still holds, and the sweeper's post-expiry eligibility is the backstop, not a second
  deferral path.

### API and Contract Rules

- Endpoints are grouped per feature in `MapXxxEndpoints` extension methods. No API versioning
  in P1 (single consumer); introducing versioning requires an ADR.
- Success/error shapes follow the endpoint table in the architecture plan (§6): `202` with a
  `Location` header for import and enrichment requests, `204` for watchlist and play, `403`
  when `LocalPlay` is off.
- Enumerations are serialized by `Name` as strings; numeric values never cross the API. `Movie` enrichment fields are exposed as
  `EnrichmentStatus`, `EnrichedAt`, `EnrichmentAttempts`, `LastFailureCategory`; never raw error text.
- The OpenAPI document is committed at `web/src/api/openapi.json`, snapshot-tested with Verify,
  and regenerated in CI; drift fails the build. The TypeScript client and MSW handlers are
  generated from that document, never hand-written.
- Enrichment status is delivered by SignalR push from the Api host (ADR-0022; port and
  post-commit semantics in ADR-0029). Client reconnect refetches pending movies rather
  than replaying missed events. Polling (ADR-0008) is superseded for this channel only.

### Documentation Rules

- Architectural decisions are recorded as one-page ADRs in `docs/adr/` (status Accepted),
  numbered per the architecture plan (ADR-0001 … ADR-0012). A PR that changes an architectural
  choice MUST add or supersede an ADR.
- `CONTEXT.md` is the ubiquitous-language source of truth in the original format (term,
  definition, avoid).
- The README MUST carry the modernisation narrative, a C4 container diagram, a trace screenshot,
  how to run with compose, how to run the quality gates, and links to the ADRs.
- Documentation lookups use `microsoft-learn` for Microsoft-owned technology and `context7` for
  third-party libraries, never the reverse (see `CLAUDE.md`).

### Known Technical Debt (Acknowledged Risks)

The following are documented and accepted for the current iteration. They MUST NOT be silently
worked around or extended; retiring them follows the epic order in the architecture plan.

- **Transitional layout**: until Epic 1 lands, `LamuFlix.Web`, `LamuFlix.Data`, `LamuFlix.Work`,
  and `LamuFlix.Test` remain. New code in those projects MUST already follow Principles II–IX
  (no new reflection queries, no new `DateTime.Now`, no new Portuguese identifiers, no new
  MSTest tests).
- **Current hard-coded secrets and paths** (MySQL credentials, OMDb key, `F:/Filmes`, player
  paths) are known violations of Principle VII, retired in Epics 1–3. They MUST NOT be copied.
- **Dual write on import** (insert, then enqueue) is mitigated by the sweeper; a transactional
  outbox is Epic 8 (ADR-0005).
- **OMDb free tier** (1,000 requests/day): bulk cutover is slow by design; `RateLimited`
  retries MUST be delayed, never tight loops.
- **Single user**: no cross-user signal, so collaborative filtering is excluded (ADR-0011).
- **Embedding model download** (hundreds of MB) slows first compose start; tests default to the
  fake generator and the single Ollama Testcontainers test sits in its own category.
- **Windows daily-driver host** runs the API as a process while Postgres and RabbitMQ run in
  containers; `LocalPlay` is off in compose.

## Governance

This constitution is the authoritative, non-negotiable rulebook for LamuFlix. It supersedes
oral agreements, inline comments, and informal conventions. When in conflict with any other
document except the locked product plan (`F:\Dev\lamuflix-react-pivot-plan.md`, which owns
WHAT is built; this constitution owns HOW), the constitution prevails.

**Amendment procedure**:
1. Submit a written proposal describing the change, rationale, and impact on existing code.
2. Record the decision as an ADR in `docs/adr/` when it changes an architectural choice.
3. Provide a migration plan if existing code must be updated to comply.
4. Apply the appropriate version bump, update the Sync Impact Report and `Last Amended`, and
   review `.specify/templates/*` for required follow-up before merging.

**Version semantics**:
- MAJOR — a principle is removed, redefined, or made backward-incompatible with existing code
- MINOR — a new principle or section is added, or existing guidance is materially expanded
- PATCH — wording clarification, typo fix, or non-semantic refinement

**Compliance**: All PRs and code reviews MUST verify compliance with this constitution. The
`/speckit.plan` Constitution Check MUST cite the principle number for every gate it evaluates.
Non-compliant code MUST be blocked from merging unless an amendment is simultaneously submitted
and approved.

**Version**: 1.15.0 | **Ratified**: 2026-09-21 | **Last Amended**: 2026-11-11
