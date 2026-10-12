# DEV-80 Design: Folder Import Dialog and Smart Polling

**Feature:** DEV-80 | **Branch:** `feature/080-spec` | **Status:** Draft
**Inputs:** `specs/DEV-80/brief.md` (Q1-Q7, R1-R4, A1-A6), `spec.md`, `plan.md`, `tasks.md`, `recon-DEV-80`, `recon-DEV-80-3`, `specs/PRODUCT.md`, constitution
**Scope:** Minimum library list, import dialog, conditional 2 s polling, NotFound/Failed retry. No router, no posters, filters, sorting UI, paging controls, details view, or routing. `frontend-design` skill is not installed in this session; layout follows spec copy and existing Tailwind readiness-screen conventions.

## 1. Route map and URL structure

No router is added (brief.md Q1, brief.md:73-80). Single mount point:

| Route | Host | Content |
|---|---|---|
| `/` | `web/src/App.tsx` | Library screen: header, `Import folder` button, list, import dialog |

Filter state in the query string, browse paging controls, and a details route are out of scope. The browse call fetches the first page only.

## 2. Per-page layout (text wireframes)

### 2.1 Library screen (`App` host)

```text
+--------------------------------------------------+
| Header: LamuFlix              [Import folder]    |
+--------------------------------------------------+
| List (server browse order, no client re-sort)    |
|  +--------------------------------------------+  |
|  | Row: <title>  [Badge]            [Retry?]  |  |
|  +--------------------------------------------+  |
|  ... one row per browse item ...                 |
+--------------------------------------------------+
| Empty state (only when zero items):              |
|   No movies yet. Import a folder to get started. |
+--------------------------------------------------+
```

Row content per A2-A3: title text, status badge text (`Pending`, `Enriched`, `Not found` for `NotFound`, `Failed`), `Retry` button only on `NotFound` / `Failed` rows.

### 2.2 Import dialog (modal over library screen)

```text
               +----------------------------------+
               | Import a movie folder         [x]|
               +----------------------------------+
               | Folder path                      |
               | [______________________________] |
               | <inline error, if any>            |
               +----------------------------------+
               |               [Cancel] [Import]  |
               +----------------------------------+
```

Labels verbatim from A2: button `Import folder`, dialog title `Import a movie folder`, input label `Folder path`, submit `Import`, cancel `Cancel`, row action `Retry`.

Interaction:
- Open: `Import folder` button focuses the `Folder path` input.
- 202: dialog closes, focus returns to `Import folder` button, Pending row prepends (A1 title = last folder-path segment).
- 422: dialog stays open, `folderPath` problem message inline under the input.
- 400 / network failure: dialog stays open, generic inline error under the input.

## 3. Component tree

```text
main.tsx
└─ QueryClientProvider (sole state/cache layer, Q1)
   └─ App (library screen host, Q5)
      ├─ Header (title + Import folder button)
      ├─ LibraryView (minimum list, US1)
      │  └─ MovieRow[] (title + StatusBadge + RetryButton?)
      │     ├─ StatusBadge (enum name text per A3)
      │     └─ RetryButton (only NotFound/Failed; inline row error slot)
      └─ ImportDialog (US2; controlled open state in App)
         ├─ FolderPathInput (labelled, describedby error)
         ├─ DialogInlineError (422 field problem | generic)
         └─ DialogActions (Import submit / Cancel)
```

Data layer (for implementers, no new architectural layer):
- `web/src/api/client.ts`: `openapi-fetch` client typed by generated `paths`.
- `web/src/library/libraryQueries.ts`: browse query (`refetchInterval` function), import mutation (cache prepend), retry mutation (row → Pending / invalidate).
- `web/src/test/handlers.ts`: `openapi-msw` handlers derived from generated `paths`; `server.ts` contract unchanged.

No new project, folder-as-layer, state library, or router.

## 4. Component states

| Component | Loading | Empty | Error | Partial / transitional |
|---|---|---|---|---|
| LibraryView | No loading placeholder is committed (no skeleton rows) | A4 copy only; no rows, no Retry | No list-level transport-error UI is committed (no manual retry affordance) | Poll reconciliation: Pending row replaced by server row in place, server order kept (A5); no re-sort, no scroll jump |
| MovieRow (each status) | n/a (row renders only with data) | n/a | Retry 404/409: inline row error + browse invalidation (FR-007); row keeps prior status until refetch | Pending: no Retry; poll arms. Enriched: no Retry. NotFound/Failed: Retry visible; retry 202 flips that row to Pending and re-arms polling |
| ImportDialog | Submit in flight: `Import` disabled, input readonly, cancellable via `Cancel` | n/a (input starts blank) | 422: field problem inline, dialog open, focus to error; 400/network: generic inline error, dialog open (FR-004) | Optimistic prepend (A1) visible behind closed dialog; next 2 s poll reconciles title/status |
| Polling | n/a | Never polls on empty list (no Pending → `false`) | No poll-error surfacing is committed | `refetchInterval`: `2000` while any current browse item is `Pending`, else `false`; background refetch off (FR-005) |

Edge: row turning Pending→terminal between polls stops polling once no Pending item remains in current browse data. Retry on a row that became Pending returns 409 → inline row error + invalidation.

## 5. Data needs (endpoint + fields)

All web HTTP goes through the `openapi-fetch` client typed by committed generated `web/src/api/types.ts` (C# DTO → `web/src/api/openapi.json` → `types.ts`; never hand-rolled). MSW handlers derive from the same generated `paths` via `openapi-msw`.

| Need | Endpoint | Request fields | Response fields used | Cache effect |
|---|---|---|---|---|
| Browse first page | `GET /api/movies` | A6 standing wire query `sort=Title&direction=Ascending&page=1&pageSize=20` through the corrected generated query types (brief R3/R4, recon-DEV-80-3:22-33; server requires `sort,direction,page,pageSize`, `page=1`, case-sensitive values, `pageSize` 1..100, omission → 422; the `BrowseMoviesOperationTransformer` states the four names, required flags, enum sets, and integer bounds as document metadata only with no binding, validation, route, runtime, or UI change; the historical optional-PascalCase baseline defect is superseded; no handwritten DTOs, casts, or stale PascalCase workaround; no sort UI or paging controls) | `items[].id`, `items[].title`, `items[].enrichmentStatus: Pending \| Enriched \| NotFound \| Failed` | Seeds list; poll target |
| Import folder | `POST /api/movies/import` | `{ folderPath: string }` | 202: empty body + `Location: /api/movies/{id}`; 422: `folderPath` problem; 400/generic: generic problem | 202: prepend `{ id (from Location), title (last path segment, A1), enrichmentStatus: 'Pending' }`; errors: inline only, no cache write |
| Retry enrichment | `POST /api/movies/{id}/enrichment` | path `id` | 202: empty; 404/409: problem | 202: set that row `Pending` (re-arms poll); 404/409: inline row error + invalidate browse query |

No other DTO field, route, or schema item is consumed. `EnrichedAt`, `EnrichmentAttempts`, `LastFailureCategory`, details DTO, and new routes are out of scope.

## 6. Keyboard navigation and accessibility (a11y)

- Dialog: native `<dialog>` semantics or `role="dialog"` + `aria-modal="true"` + `aria-labelledby` (dialog title). Focus moves to `Folder path` on open, traps inside while open (Tab/Shift+Tab cycle), `Escape` activates `Cancel`, focus returns to `Import folder` on close (including 202 close).
- Form: input is a real `<label for>` pair (`Folder path`); inline errors use `aria-describedby` + `role="alert"` so 422/generic messages announce; `Import` is `type="submit"` so Enter submits.
- List: rows are `<li>` in a `<ul>` with the heading hierarchy intact; status badge is text (not color-only) with `aria-label` matching visible text where styling shortens it (`Not found`); `Retry` is a real `<button>` with an accessible name including the row title (e.g. `Retry <title>`) so repeated buttons disambiguate.
- Polling: status changes announce politely (`aria-live="polite"` region on the badge or row) rather than moving focus; no auto-focus steal on reconcile. Background refetch off keeps screen-reader and battery cost bounded.
- Contrast and targets (descriptive only, not frozen delivery requirements and not task-verified): badge text and the `Retry`, `Import`, `Cancel`, `Import folder` controls keep the existing Tailwind theme conventions with visible focus rings; no new contrast or target-size requirement is committed.
