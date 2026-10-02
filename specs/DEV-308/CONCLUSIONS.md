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
