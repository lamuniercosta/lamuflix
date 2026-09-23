# LamuFlix — Product & Architecture Blueprint

**Status:** Authoritative (proxy owner specification)  
**Reference:** `specs/PRODUCT.md`

## 1. Product Goals

LamuFlix is a high-performance, personal streaming platform for movies and TV series built on modern .NET and web standards:
- **Backend**: .NET 10 Web API (`LamuFlix.Api`) using vertical slice / feature-folder architecture with strict domain boundaries (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.ServiceDefaults`).
- **Worker**: Background metadata enrichment and processing service (`LamuFlix.Work`) powered by RabbitMQ queues.
- **Frontend**: Fast, responsive Single Page Application (`web/`) built with React 19, TypeScript, and Vite.
- **Reliability & Quality**: Central Package Management (CPM), zero compiler warnings (`TreatWarningsAsErrors=true`), mandatory static analysis gates (Roslyn, Cyclomatic Complexity <= 15/6, InspectCode), mutation testing (Stryker >= 80%), and contract drift enforcement (OpenAPI -> TypeScript).

## 2. Non-Goals & Sequencing

- **Epic 8 (Ops / Docker / Compose)**: Scheduled as slack permits ("by pick").
- **Epic 9 (Recommendations / Ollama / pgvector)**: Strictly deferred until after Epic 7 (SPA) completes.
- **Speculative Abstractions**: No generic repository, mediator, UnitOfWork, or complex multi-layer abstractions unless explicit in a ticket.
- **Auto-merge**: All pull requests are reviewed and merged manually by the owner.

## 3. Authority Rules

> **A ticket's *Scope & Technical Design* is authoritative.**  
> `CONTEXT.md` (DEV-292) is the architecture reference once it lands. Anything specified in the ticket or `CONTEXT.md` is an owner decision already made. Patron cites the ticket line and moves on without re-grilling.

## 4. Taste Rules

- Patron may assume **taste** (button label wording, sort order, empty-state copy, UI micro-interactions).
- Every taste assumption made by Patron must be logged with `[assumed]` into `specs/<feature>/ASSUMPTIONS.md`.
- Keep copy clear, concise, and professional. Avoid placeholder text or unnecessary decoration.

## 5. Structural Decisions (§2.3 Blocked List)

Patron may **never** assume any item on this list. If an underspecified ticket implies one of these, Patron must answer `blocked: structural — <question>` and include it as an owner checkbox in the spec PR (gate 1 remains closed until checked):

1. **Dependencies**: Any new NuGet or npm dependency, or changing a dependency already chosen here.
2. **Architecture**: Any new project, top-level folder, or layer (repository/mediator/"services" wrapper, state library).
3. **Database Schema**: Any schema or migration change beyond the ticket's own explicitly named tables.
4. **API Shape**: Any public route, DTO field set, or HTTP status code not already specified in the ticket or `spec.md`.
5. **Security & Local Execution**: Anything touching `Features:LocalPlay`, secrets, or `Process.Start`.
6. **File Scope**: Deleting or rewriting any existing file that the ticket does not explicitly name.
