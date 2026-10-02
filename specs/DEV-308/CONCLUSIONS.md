# DEV-308 grill conclusions

## Authority and evidence

Patron applies the current assigned role and specs/PRODUCT.md section 5. Care items 1-6 require cited Patron rulings, not owner checkboxes. Only a ticket deliverable change or a constitution departure Patron judges necessary escalates as blocked: structural. The older canvas charter/task-pipeline escalation wording is superseded by the current direct instruction and PRODUCT.md.

Named worktree: F:/Dev/LamuFlix.worktrees/feature-308-spec, branch feature/308-spec, intake HEAD 6e0f09b1e476fc10ec3e5829d7b8c73ba4c80d03. Preserve Rigger's unrelated tracked .specify/feature.json edit.

Evidence: live scripts/get-task.ps1 -TaskId DEV-308, DEV-376, DEV-309 and DEV-310 read on 2026-10-02; task note DEV-308:6-18; recon-DEV-308:7-49 (trusted Wisp source citations); chain:5-8; specs/PRODUCT.md; .specify/memory/constitution.md:128-144,196-236,282-295,348-370,461-472. No artifact receipt exists under artifacts/DEV-308; the live task read supplies ticket text. No baseline gates were run; none are claimed green.

## Decided ticket envelope

Live DEV-308 ticket Scope & Technical Design:

1. Api references Infrastructure and registers the handler pipeline through AddHandler, so every handler resolves with its validation, logging and tracing decorators (from DEV-376).
2. Register the Infrastructure adapters, and TimeProvider through host DI (see DEV-307).
3. CORS policy for the Vite dev server (http://localhost:5173).
4. Call app.MapDefaultEndpoints() (DEV-307) and set up the endpoint-group mapping convention (MapLibraryEndpoints, MapImportEndpoints, ...) that DEV-309 and DEV-310 extend.

Acceptance Criteria (verbatim):

- Api starts cleanly as a Minimal API host without MVC or Razor dependencies.
- A test resolves a registered handler from the Api host's service provider and gets the decorated pipeline.
- CORS allows the React dev server origin.

The ticket Overview assigns endpoints to DEV-309/DEV-310 and removal of LamuFlix.Web to DEV-388 after SPA parity. No scope item may be silently omitted, duplicated into a new deliverable, or deferred away. No implementation is authorized by this grill.

---

## Q1 - DEV-307 implementation prerequisite and shared defaults ownership

Question (Keel): does DEV-308 Phase B require DEV-307 implemented on its build base, consuming ServiceDefaults TimeProvider and MapDefaultEndpoints rather than adding provisional copies?

Recommendation (Keel): yes. Phase A proceeds now. Phase B waits for DEV-307 code merged to main; pickup verifies AddServiceDefaults registers TryAddSingleton(TimeProvider.System) and MapDefaultEndpoints exists. Consume the shared registration and mapping; keep any MapDefaultEndpoints call DEV-307 T013 already added rather than duplicating it. Retain the endpoint-group convention half of scope 4. Leave the existing Infrastructure TryAddSingleton calls untouched. Cost: DEV-308 build waits for DEV-307 implementation; no chain reorder.

Patron verdict: ACCEPT; Phase A proceeds, Phase B requires DEV-307 implementation on its merged build base, and DEV-308 consumes the shared defaults once.

- Basis: DEV-308 Scope 2 and 4 expressly reference DEV-307; .specify/memory/constitution.md:235-236 assigns TimeProvider and health wiring to ServiceDefaults and forbids host duplication. Trusted recon-DEV-308:20,28 cites specs/DEV-307/plan.md:172,225-231 and T013 at :87; chain:5-6 already records the dependency.
- Preserve scope 4 in full: retain a single app.MapDefaultEndpoints() call and establish the MapXxxEndpoints convention. Host integration assertions prove the consumed services and mapping, rather than treating a merged spec as implemented code. No owner checkbox or schedule change is required.
- Existing Infrastructure fallback registrations at src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs:31 and Adapters/MetadataProviderServiceCollectionExtensions.cs:30 remain outside this edit. No cleanup is forced by the ticket; noted, no ticket. This ruling neither closes DEV-307 owner checkboxes nor authorizes implementation.

---

## Q2 - Api handler ownership and explicit registrations

Question (Keel): which handlers does the Api host register?

Recommendation (Keel): register every handler called by the DEV-309/DEV-310 endpoints through one explicit AddHandler line per handler in one Api registration file, called by Program.cs. Worker-only handlers such as ProcessEnrichmentCommandHandler remain with the Worker. A host integration test resolves each Api handler and checks its tracing outer decorator. The recon supplies counts rather than exact names; Keel requested the full handler names, current callers, ticket ownership and validators from Bernstein before freezing the manifest.

Patron verdict: ACCEPT the Api-facing ownership principle; freeze the exact manifest only from the requested recon, before the brief closes.

- Basis: DEV-308 Overview composes the Api for DEV-309/DEV-310; Scope 1 requires every registered handler to resolve through AddHandler. DEV-309 Scope 1-2 names BrowseMoviesQuery and GetMovieDetailsQuery; DEV-310 Scope 1 names the remaining dispatching endpoint families. Constitution:133-136 requires explicit registration and Tracing -> Logging -> Validation -> handler; reflection scanning is forbidden. Worker-only dispatch is outside this Api ticket (recon-DEV-308:14-16).
- Approve one internal registration helper in src/LamuFlix.Api/HandlerRegistration.cs called from Program.cs. This is composition in the existing host, not a new project or architectural layer (PRODUCT.md section 2 and section 5 care item 2). Preserve the RabbitMQ-owned ProcessEnrichmentCommandHandler registration; do not duplicate it in the Api inventory merely because the adapter is registered there.
- Host integration coverage resolves every frozen Api service contract through the real AddHandler registrations, using the existing helper's decorator-order tests as supporting evidence (recon-DEV-308:14). Missing concrete names/callers/validator or constructor dependencies remain needs recon, not a silent assumption or owner checkbox. No inventory is declared complete by this principle ruling.
