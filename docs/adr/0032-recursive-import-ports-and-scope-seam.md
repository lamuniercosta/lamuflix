# Recursive import ports and per-folder scope ownership

Status: Proposed. Records Patron's settled DEV-333 design for spec review; DEV-333 Gate 1, constitution alignment and implementation approval are not claimed.

Ticket: DEV-333. Authority: `specs/DEV-333/brief.md` Q1–Q8 and post-draft process decision; full Patron rulings in `specs/DEV-333/CONCLUSIONS.md`. Evidence: notes `recon-DEV-333` sections 2–5, `recon-DEV-333-2` ticket and constitution sections, and `recon-DEV-333-2-2` ADR numbering/format sections.

## Context

Ticket Scope 1 mandates recursive discovery behind a Core port while retaining the single-folder scanner; Scope 2 requires bounded concurrency and Scope 4 reuses the existing import handler and outbox. That handler and its persistence dependencies are scoped. Concurrent folders therefore need independent scopes without placing DI or filesystem dependencies in Core.

Constitution v1.6.0 Principle I requires an ADR for new ports and enforces project boundaries through architecture tests. Principles II, VII and IX require explicit decorated handlers, validated configuration and real persistence proofs. Patron Q3 accepts both ports as ordinary boundary-preserving additions, with Principle I list/whitelist alignment through the existing ADR process; no constitution departure or owner checkbox is introduced by this decision. Governance requirements still apply to that alignment.

ADR-0031 exists only in unapproved, unlanded DEV-426 specification evidence at `fee5c7a8` at the recon baseline. DEV-426 must separately obtain amended quota-outcome/constitution approval and land before DEV-333 implementation. This ADR does not approve or implement DEV-426.

## Decision

1. **Discovery port.** Add `ILibraryFolderDiscovery` under `Core/Ports`, yielding `IAsyncEnumerable<LibraryPath>` with cancellation. Infrastructure `FileSystem/FileSystemLibraryFolderDiscovery` uses existing System.IO.Abstractions. It lazily yields canonical Candidate Movie Folders under the configured root, continues below qualifying parents, and neither follows nor yields linked subdirectories. Root identity, containment and shared supported extensions follow brief Q4 exactly. `IMediaLibraryScanner.Scan(LibraryPath)` remains single-folder.
2. **Execution port.** Add `IMovieFolderImporter` under `Core/Ports`, exposing `Task<MovieId> ImportAsync(LibraryPath folder, CancellationToken cancellationToken)`. Infrastructure `Import/ScopedMovieFolderImporter` creates and disposes a fresh async DI scope for each folder and resolves the decorated `ICommandHandler<ImportMovieFolderCommand, MovieId>` within it. Each folder retains the existing movie/outbox save boundary; concurrent folders never share a DbContext.
3. **Bounded orchestration.** A sealed, payload-free `ImportLibraryCommand`, sealed handler and sealed record `ImportLibraryResult(long ImportedCount)` live in Core `Features/Import`. The handler uses the two ports and validated LibraryOptions. One producer and exactly MaxConcurrency consumers use a Wait-mode channel of the same capacity. Normal completion drains; cancellation or fault cancels peers and awaits all tasks and scope disposal. Success counts saved imports, not enrichment completion; first-fault/caller-cancellation ordering and duplicate rerun behavior follow brief Q5–Q6.
4. **Explicit composition.** Register the handler and adapters explicitly through existing Api registration conventions. Initial invokers are tests resolving the handler in a fresh caller scope. No endpoint, recurring schedule or startup scan is added. Discovery/import never acquire quota or call providers; quota-day settlement is downstream under brief Q1–Q2 and the separately amended DEV-426 outcome contract.

## Consequences

- Core orchestration can be tested through narrow ports while persistence and scope ownership remain in Infrastructure. No package, project or architectural layer is added.
- Update the Principle I port list and architecture whitelist together, preserving existing entries and applying Governance version/sync-impact/template review against the actual implementation baseline. Land the exact Candidate Movie Folder glossary entry with the spec.
- Acceptance requires lazy candidate identity/boundary/property proofs, deterministic bounded-channel and shutdown proofs, and real PostgreSQL import/outbox/rerun evidence. Durable quota deferral additionally requires the separate real DB/RabbitMQ and composed fake-gate proofs in brief Q7.
- Partial failures retain earlier folder commits; a fresh rerun faults on existing paths. No skip-existing, partial-run resume or batch rollback is introduced.
- Recheck ADR numbering and landed dependency contracts at rebase and Phase B pickup. ADR creation does not clear analysis, three-axis plan challenge, either ticket's Gate 1, implementation gates or delivery review.

## Plan challenge clarification — Patron round 1

The full cited ruling in `specs/DEV-333/CONCLUSIONS.md` under `DEV-333 Phase 2 step 6 round 1` and its brief amendment settle implementation details without changing the ports or scope boundary. Discovery uses an explicit stack of lazy enumerators, not call-stack recursion; an internal containment helper in the discovery file permits deterministic comparison-mode proofs using the existing Infrastructure test visibility. Only the configured root link is resolved to its final target; linked children are never followed. MockFileSystem link attributes do not establish traversal of linked contents.

The payload-free command still passes through the mandatory validation decorator via AddHandler, with no empty validator stub: it has no payload input and LibraryOptions is startup-validated. Quota settlement remains a non-failure Pending write through `DeferEnrichmentUntil(DateTimeOffset)`, with atomic claim clear and future-deferral exclusion. Delivery aligns the Principle IV mutator/predicate inventories and Enrichment Reliability Rules alongside Principle I and Governance; Patron ruled this compliant clarification, not a departure. DEV-426 retains its separate category amendment, owner checkbox and chain eligibility implementation.

T002/T004 must change this ADR from Proposed to Accepted only on the applicable approval evidence, alongside the delivery constitution alignment. Its status remains Proposed in this Phase A artifact. No Gate 1, implementation, test or gate outcome is claimed here.
