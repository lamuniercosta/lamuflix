# DEV-567 — Conclusions

## Q1 — Network failure diagnostic fields (status and traceId when no HTTP response exists)
**Verdict:** Approved (not `blocked: structural`). Explicit unavailable labels for status and traceId on network errors faithfully satisfy the ticket without contradicting it; accept Keel's recommendation (`[assumed]` for exact fallback wording).
- **Ticket harmony (`specs/DEV-567/ticket-text.txt:20,30,34`):** Line 30 explicitly defines network errors as cases "where no problem body exists", and line 20 ties `traceId` to finding the server log entry in Aspire/cluster logs. Reading line 34 ("each show one toast with the status and the traceId") alongside line 30 means the toast's status and traceId slots/fields are rendered for both error classes, showing an explicit unavailable value when transport fails before an HTTP response/traceId exists.
- **Received HTTP responses (`specs/DEV-567/ticket-text.txt:20,26`):** Use the HTTP response status (falling back to problem `status` if needed), include problem `reason` when present, and show `traceId` when supplied (or unavailable if absent from the response).
- **No fabricated diagnostics (`specs/PRODUCT.md` §4):** Never invent fake HTTP status codes or synthetic trace IDs on client-side transport failures.
## Q1. Missing diagnostics

**Keel**

[from Keel] /grill-with-docs DEV-567. Q1/12. What diagnostic fields should the toast show when a network failure produces no response? I recommend one Network error toast with explicit unavailable status and trace ID, and no fabricated diagnostic values. For received HTTP responses, use HTTP response status and include reason and traceId when supplied. Ticket lines 20, 26 and 30 support missing diagnostics, while line 34 literally asks for status and traceId even for network failure. Please rule whether unavailable labels are the faithful way to satisfy that criterion or it needs a structural owner checkbox under section 2.3. I will record each ruling in specs/DEV-567/CONCLUSIONS.md.

**Patron**

[from Patron] DEV-567 Q1/12 ruling:
Verdict: Approved (not blocked: structural) — explicit unavailable labels for status and traceId on network errors faithfully satisfy ticket lines 20, 30, and 34; no owner checkbox required.
- Ticket lines 30 & 34 harmony (`specs/DEV-567/ticket-text.txt:20,30,34`): Line 30 defines network errors as having no problem body, and line 20 defines `traceId` for locating server logs; rendering the status and traceId fields with explicit unavailable labels on network failures satisfies line 34 without contradicting transport reality.
- Received HTTP responses (`specs/DEV-567/ticket-text.txt:20,26`): Show the HTTP response status, include `reason` when present, and show `traceId` when supplied (or unavailable if omitted).
- No fabricated values (`specs/PRODUCT.md` §4): Accept your recommendation (`Network error` title + explicit unavailable status/traceId labels, logged `[assumed]` in `specs/DEV-567/ASSUMPTIONS.md` and recorded in `specs/DEV-567/CONCLUSIONS.md`).


## Q2 — `ProblemDetails` contract repair scope (`reason` and `traceId`)
**Verdict:** Approved (not `blocked: structural`). Update the C# OpenAPI schema projection to declare optional `reason` and `traceId` on `ProblemDetails`, regenerate `web/src/api/openapi.json`, and run `npm run generate:api` to update `web/src/api/types.ts`, with zero change to runtime HTTP payloads or API routes/status codes.
- **Contract chain & generated-file rule (`AGENTS.md` Non-negotiables; role rules §2.3 Contract chain; `specs/PRODUCT.md` §1):** Never hand-edit generated `web/src/api/types.ts` (`web/src/api/types.ts:1-8`, `web/scripts/generate-api.mjs:5-11`). API contracts must flow strictly C# OpenAPI projection → `web/src/api/openapi.json` → `web/src/api/types.ts`.
- **Forced edit in scope (§2.3 item 6; `specs/DEV-567/ticket-text.txt:26`):** Ticket line 26 explicitly requires `ProblemDetails` in `web/src/api/types.ts` to gain optional `traceId` and `reason` ("the API already sends"), and recon (`recon-DEV-567` addendum lines 13-15) shows `web/src/api/openapi.json` currently omits them. Because the contract-drift gate and non-negotiable generated-file rule force the upstream OpenAPI projection + `openapi.json` update to produce `web/src/api/types.ts`, editing the C# OpenAPI projection and `web/src/api/openapi.json` is a forced edit in scope.
- **No runtime API shape change (§2.3 item 4; `specs/DEV-567/ticket-text.txt:20,26`):** The backend already emits `traceId` and `reason` at runtime; the C# change is strictly OpenAPI schema documentation (`string | null` / optional `traceId` and `reason` on `ProblemDetails`) so the exact C# projection file should be verified by recon if not yet named.

---

## Q2. ProblemDetails contract repair

**Keel**

[from Keel] DEV-567 Q2/12. What is the scope of the ProblemDetails contract repair? I recommend documenting the already-emitted optional reason and traceId in the existing C# OpenAPI projection, regenerating web/src/api/openapi.json and then web/src/api/types.ts, and preserving the HTTP payload and API shape. Ticket lines 20 and 26 establish intent, and recon lines 13-15 establish the gap in checked-in schema and types. The proposed chain avoids manual artifact edits and leaves the precise C# projection file contingent on source evidence. Please rule on the contract repair boundary.

**Patron**

[from Patron] DEV-567 Q2/12 ruling:
Verdict: Approved — document optional `reason` and `traceId` in the C# OpenAPI `ProblemDetails` schema projection, regenerate `web/src/api/openapi.json`, and run `npm run generate:api` to produce `web/src/api/types.ts`, with no runtime HTTP payload or endpoint changes.
- Contract chain & generated-file rule (`AGENTS.md` Non-negotiables; §2.3 Contract chain; `specs/PRODUCT.md` §1): `web/src/api/types.ts` is generated from `web/src/api/openapi.json` via `web/scripts/generate-api.mjs` and must never be hand-edited; contracts flow C# OpenAPI → `web/src/api/openapi.json` → `web/src/api/types.ts`.
- Forced edit in scope (§2.3 item 6; `specs/DEV-567/ticket-text.txt:26`; `recon-DEV-567` addendum lines 13-15): Ticket line 26 requires `ProblemDetails` in `web/src/api/types.ts` to have optional `traceId` and `reason`, so updating the upstream C# OpenAPI projection and `web/src/api/openapi.json` is forced by the AC and contract-drift gate.
- Runtime API invariance (§2.3 item 4; `specs/DEV-567/ticket-text.txt:20,26`): Keep runtime HTTP responses unchanged (the API already sends these fields); verify the exact C# OpenAPI projection file via recon before planning tasks. Recorded in `specs/DEV-567/CONCLUSIONS.md`.


## Q3 — `ProblemDetails` OpenAPI schema transformer and export chain
**Verdict:** Approved (not `blocked: structural`). Add a narrowly scoped `ProblemDetails` OpenAPI schema transformer registered in `src/LamuFlix.Api/Program.cs:60-67`, export the app-generated OpenAPI v1 document to `web/src/api/openapi.json`, and run `npm run generate:api` to update `web/src/api/types.ts`.
- **Matches existing repo architecture (`recon-DEV-567` addendum lines 26-29; `AGENTS.md` Non-negotiables):** `Program.cs:60-67` already registers `OpenApiDocumentNormalizer`, `EnrichmentStatusSchemaTransformer`, and `EnrichmentHubSchemaTransformer` under `AddOpenApi("v1", ...)`. Adding a `ProblemDetails` schema transformer alongside them (or inside existing OpenAPI schema normalization) follows the established pattern to project the runtime extensions already emitted by `GlobalExceptionHandler.cs:18,304-308` (`reason`) and `ProblemDetailsTraceId.cs:8-20` (`traceId`).
- **Forced edit in scope (§2.3 item 6 & Contract chain; `specs/DEV-567/ticket-text.txt:26`):** Because `ProblemDetails` is framework-projected today and `web/src/api/types.ts` is generated from `web/src/api/openapi.json` (`web/scripts/generate-api.mjs:5-11`), registering the transformer in `Program.cs` and exporting the v1 document is the minimum forced edit required to satisfy ticket line 26 without hand-editing generated artifacts.
- **Export step verification (`recon-DEV-567` addendum line 27):** Record the exact app-generated OpenAPI v1 export invocation in the first contract task and do not claim a checked-in C# export script that recon did not find.

---

## Q3. Framework OpenAPI projection and export

**Keel**

[from Keel] DEV-567 Q3/12. May the contract work add a narrowly scoped ProblemDetails schema transformer under the existing API OpenAPI configuration? I recommend registering it at Program.cs lines 60-67 alongside existing transformers, documenting reason and traceId as optional strings, then exporting the app-generated v1 document and running npm run generate:api. Recon lines 25-30 establish that the projection is framework-owned today and runtime emission exists. The exact server export invocation can be verified and recorded as the first contract task, using the app-generated document as the source. The recon only confirms the downstream TypeScript command, so the brief would avoid claiming a checked-in server export script.

**Patron**

[from Patron] DEV-567 Q3/12 ruling:
Verdict: Approved — add a narrowly scoped `ProblemDetails` OpenAPI schema transformer registered in `src/LamuFlix.Api/Program.cs:60-67` to declare optional `reason` and `traceId` strings, export the app-generated v1 OpenAPI document to `web/src/api/openapi.json`, and run `npm run generate:api`.
- Existing OpenAPI architecture (`recon-DEV-567` addendum lines 26-29; `AGENTS.md` Non-negotiables): `Program.cs:60-67` already registers schema transformers under `AddOpenApi("v1", ...)` while runtime extensions are already emitted by `GlobalExceptionHandler.cs:18,304-308` (`reason`) and `ProblemDetailsTraceId.cs:8-20` (`traceId`).
- Forced edit in scope (§2.3 item 6 & Contract chain; `specs/DEV-567/ticket-text.txt:26`): Because `ProblemDetails` is framework-generated today and `web/src/api/types.ts` is generated from `web/src/api/openapi.json`, registering the schema transformer in `Program.cs` and exporting `openapi.json` is forced by ticket line 26 and the generated-file rule.
- Honest export task (`recon-DEV-567` addendum line 27): Have the first contract task verify and use the app-generated v1 OpenAPI export step without claiming a pre-existing server export script. Recorded in `specs/DEV-567/CONCLUSIONS.md`.


## Q4 — Toast trigger boundary across React Query and raw `openapi-fetch` calls
**Verdict:** Approved (not `blocked: structural`). Emit one toast per terminal failure of a React Query query/mutation via `QueryCache.onError`/`MutationCache.onError`, and one toast per failed raw `openapi-fetch` request in `web/src/features/playback/useSubtitleAcquisition.ts` (language catalog, subtitle search, and subtitle download, covering both `{ error }` HTTP failures and thrown transport errors) via the explicit helper, while skipping intentional aborts/stale superseded requests and preserving existing inline error UI and `retry: false`.
- **Two-point wiring (`specs/DEV-567/ticket-text.txt:20,23-25`; `recon-DEV-567` addendum lines 4-11):** Ticket lines 23-25 explicitly mandate wiring at React Query's global `QueryCache`/`MutationCache` `onError` (`web/src/main.tsx:11-13`) and at raw `openapi-fetch` calls in `useSubtitleAcquisition.ts`. Because quota is returned inside search/download responses (`recon-DEV-567` addendum line 4), covering the three raw calls in `useSubtitleAcquisition.ts` (languages catalog, search, download — including 429 quota failures) satisfies line 25 without inventing a fourth endpoint.
- **No duplicate or spurious toasts (`specs/DEV-567/ticket-text.txt:20,34`; `specs/PRODUCT.md` §4):** Keeping `queries: { retry: false }` (`web/src/main.tsx:11-13`), ignoring intentional `AbortController` cancellations/superseded stale requests, and letting distinct failed executions each emit one toast ensures a forced 4xx/5xx or network failure produces exactly one toast.
- **Additive to inline UI (`specs/DEV-567/ticket-text.txt:13,20`; `recon-DEV-567` addendum line 18):** Existing inline subtitle/player error states and tests (`subtitlesTab.test.tsx:440-519`) remain intact; the toast is additive global feedback.

---

## Q4. Failure ownership and coverage

**Keel**

[from Keel] DEV-567 Q4/12. What exact failure event should produce one toast? I recommend one report for the terminal failure of each React Query query or mutation execution via its global cache callback, and one report for each failed raw subtitle catalog, search, or download execution via the explicit helper. Recon lines 4-11 show quota in search and download results and no separate quota call. Preserve inline states, keep query retry policy, skip intentional aborts or ignored stale results, and let distinct executions produce distinct toasts. Should this be the integration boundary under ticket lines 20 and 23-25?

**Patron**

[from Patron] DEV-567 Q4/12 ruling:
Verdict: Approved — emit one toast per terminal React Query failure via `QueryCache.onError`/`MutationCache.onError`, and one toast per failed raw `openapi-fetch` call in `web/src/features/playback/useSubtitleAcquisition.ts` (languages catalog, search, and download, including 429 quota responses and thrown network errors) via the explicit helper.
- Two-point wiring (`specs/DEV-567/ticket-text.txt:20,23-25`; `recon-DEV-567` addendum lines 4-11): Matches ticket lines 23-25; since quota is carried in search/download payloads rather than a separate endpoint, the three raw calls in `useSubtitleAcquisition.ts` cover search, download, and quota failures.
- One toast per real failure (`specs/DEV-567/ticket-text.txt:20,34`; `web/src/main.tsx:11-13`): Keep `queries: { retry: false }`, suppress intentional `AbortController` aborts and superseded stale requests, and allow distinct failed requests to produce distinct toasts.
- Additive to inline states (`specs/DEV-567/ticket-text.txt:13`; `recon-DEV-567` addendum line 18): Preserve existing inline error UI and tests alongside the global toast. Recorded in `specs/DEV-567/CONCLUSIONS.md` and `specs/DEV-567/ASSUMPTIONS.md`.


## Q5 — API error normalization contract and React Query error-adapter preservation
**Verdict:** Approved (not `blocked: structural`). Normalize errors at the two ticket-mandated boundaries (`QueryCache`/`MutationCache` `onError` and the explicit raw `openapi-fetch` helper) without adding a global `fetch` interceptor; permit a minimal forced edit to existing React Query error throwing/wrapping if needed so `status`, `reason`, and `traceId` reach the cache callbacks intact.
- **Status, reason, traceId, and title normalization (`specs/DEV-567/ticket-text.txt:20,24-26,30`):** Prefer the HTTP response status (falling back to a valid `ProblemDetails.status` when only the problem body is attached), extract optional `reason` and `traceId` as trimmed strings when present, and use a short title (`ProblemDetails.title` or a clean generic fallback such as `Request failed` / `Network error`) without exposing raw exception stack/messages or verbose `detail` (`specs/PRODUCT.md` §4).
- **Transport vs. programming errors (`specs/DEV-567/ticket-text.txt:24-25,30`):** Classify thrown fetch/transport failures from API calls as network errors (`Network error` with unavailable status/traceId per Q1), and do not mislabel unrelated runtime programming exceptions as network errors.
- **Forced edit for React Query error metadata (§2.3 item 6; `specs/DEV-567/ticket-text.txt:20,24`):** Because ticket lines 20 and 24 require React Query `QueryCache`/`MutationCache` `onError` to display `status`, `reason`, and `traceId`, any existing React Query queryFn/mutationFn helper that strips `ProblemDetails` or HTTP status when throwing is in scope for a minimal metadata-preserving update once identified by recon, keeping routes and retry policy unchanged.

---

## Q5. Common diagnostic normalization

**Keel**

[from Keel] DEV-567 Q5/12. How should the notifier normalize API errors at those boundaries? I recommend HTTP response status first, a valid ProblemDetails status fallback when only a structured problem reaches the cache, optional reason and traceId as text, and a generic short title fallback. Classify transport failure at a known API boundary, avoid exposing raw exception messages or detail, and avoid calling an unrelated programming exception a network failure. If the representative React Query callers drop the diagnostics, permit a minimal change to their existing error adapter after evidence identifies it, with unchanged routes and retry behavior. Does that establish the common error contract without a global fetch interceptor?

**Patron**

[from Patron] DEV-567 Q5/12 ruling:
Verdict: Approved — normalize API errors at the React Query cache callbacks and the raw `openapi-fetch` helper without a global `fetch` interceptor, and allow a minimal forced edit to existing React Query error throwing/adapters if needed to preserve `status`, `reason`, and `traceId`.
- Normalization rules (`specs/DEV-567/ticket-text.txt:20,24-26,30`): Use HTTP response status first with a valid `ProblemDetails.status` fallback, extract optional string `reason` and `traceId`, and use a short title (`ProblemDetails.title` or generic `Request failed` / `Network error`) without exposing raw exception messages or `detail` (`specs/PRODUCT.md` §4).
- Boundary classification (`specs/DEV-567/ticket-text.txt:24-25,30`): Classify transport failures at the API call boundaries as network errors (with unavailable status/traceId per Q1) rather than mislabeling arbitrary programming exceptions.
- Forced edit to preserve diagnostics (§2.3 item 6; `specs/DEV-567/ticket-text.txt:20,24`): If existing React Query callers discard `ProblemDetails` fields or HTTP status before throwing to `QueryCache`/`MutationCache`, a minimal edit to attach those diagnostics (after recon identifies the file) is forced by lines 20 & 24, with routes and retry behavior unchanged. Recorded in `specs/DEV-567/CONCLUSIONS.md` and `specs/DEV-567/ASSUMPTIONS.md`.


## Q6 — Toast host lifecycle, stacking, styling, and accessibility defaults
**Verdict:** Approved (not `blocked: structural`). Accept Keel's recommended toast lifecycle, layout, styling, and accessibility defaults (`[assumed]` for the taste parameters: 3-toast cap, 5-second auto-dismiss with hover/focus pause, bottom-end placement, `Dismiss` button label, and `aria-live="polite"`).
- **House styling & motion (`specs/DEV-567/ticket-text.txt:17-19`):** Style with the existing house tokens in `web/src/index.css:57-98` (aubergine/ink/vermilion palette, square corners, no glass or `backdrop-filter` blur) and disable motion transitions/animations under `prefers-reduced-motion: reduce`.
- **Stacking, auto-dismiss, and manual close (`specs/DEV-567/ticket-text.txt:21`; `specs/PRODUCT.md` §4):** Mount a single app-wide host above route content (bottom-end on desktop, constrained within the viewport on narrow screens), cap the stack at 3 visible toasts (dropping the oldest on overflow), auto-dismiss after 5 seconds (paused on hover or keyboard focus), and provide a keyboard-accessible `Dismiss` button on each toast.
- **Screen-reader announcement (`specs/DEV-567/ticket-text.txt:22`):** Announce each newly added toast once via a persistent `aria-live="polite"` region without stealing keyboard focus.

---

## Q6. Toast stack and accessibility

**Keel**

[from Keel] DEV-567 Q6/12. What should the app-wide toast stack do during display and dismissal? I recommend a single host mounted above route content, bottom-end on desktop and within the viewport on narrow screens, at most three visible, dropping the oldest on overflow. Dismiss after five seconds, pause while hovered or focused, and give each toast a keyboard-accessible Dismiss button. Use a persistent polite aria-live region to announce each newly added error once without moving focus, and use house tokens, square corners, no glass or blur, and reduced-motion styling. Please rule on these lifecycle and accessibility defaults for ticket lines 17-22.

**Patron**

[from Patron] DEV-567 Q6/12 ruling:
Verdict: Approved — accept your recommended toast host placement, 3-toast FIFO cap, 5-second auto-dismiss with hover/focus pause, keyboard-accessible `Dismiss` button, persistent `aria-live="polite"` announcement without focus theft, and house-token styling (`[assumed]` for taste parameters).
- Styling & motion (`specs/DEV-567/ticket-text.txt:17-19`; `web/src/index.css:57-98`): Use the existing house tokens (aubergine/ink/vermilion palette, square corners, no glass/blur) and honor `prefers-reduced-motion`.
- Lifecycle & stacking (`specs/DEV-567/ticket-text.txt:21`; `specs/PRODUCT.md` §4): Mount a single app-wide host (bottom-end on desktop, viewport-safe on narrow screens), cap visible toasts at 3 dropping the oldest on overflow, auto-dismiss after 5s (paused on hover/focus), and provide a keyboard-accessible `Dismiss` button.
- Accessibility (`specs/DEV-567/ticket-text.txt:22`): Announce each new toast once through a persistent `aria-live="polite"` region without moving focus. Recorded in `specs/DEV-567/CONCLUSIONS.md` and `specs/DEV-567/ASSUMPTIONS.md`.


## Q7 — Acceptance test matrix and verification gates
**Verdict:** Approved (not `blocked: structural`). Adopt Keel's recommended Vitest/RTL + MSW suite, one Playwright e2e scenario, contract regeneration/drift verification, web lint/typecheck/tests, and applicable .NET verification gates for the C# OpenAPI schema diff.
- **Vitest/RTL + MSW coverage (`specs/DEV-567/ticket-text.txt:20-26,34-35`; `recon-DEV-567` addendum lines 18-19):** Exercise real `QueryClient` `QueryCache`/`MutationCache` `onError` callbacks and all three raw subtitle calls in `useSubtitleAcquisition.ts` (languages, search, download, including 429 quota) across forced 4xx, 5xx, and rejected-fetch network failures, missing optional problem fields, HTTP status precedence, single-toast emission (no duplicates, intentional abort/stale suppression, inline UI preserved), and toast lifecycle (3-toast cap, 5s fake-timer auto-dismiss, hover/focus pause, keyboard `Dismiss`, and `aria-live="polite"`).
- **Playwright e2e check (`specs/DEV-567/ticket-text.txt:34-35`; `recon-DEV-567` addendum line 20):** Add one deterministic Playwright scenario building on the existing playback/subtitle e2e pattern (`web/e2e/dev525-subtitle-search.e2e.ts`) that intercepts an API call with a known `application/problem+json` failure (`status`, `reason`, `traceId`) and verifies one visible toast and manual dismissal.
- **Web + .NET verification gates (`specs/DEV-567/ticket-text.txt:26,36`; `specs/PRODUCT.md` §1; `AGENTS.md` Verification commands):** Require web `lint`, `typecheck`, Vitest, and the Playwright check to pass, `web/src/api/openapi.json` and `web/src/api/types.ts` to be clean after regeneration, and the standard .NET verification gates to pass on the C# OpenAPI schema transformer diff.

---

## Q7. Acceptance evidence and gates

**Keel**

[from Keel] DEV-567 Q7/12. What acceptance evidence should Quill plan? I recommend Vitest/RTL and MSW covering real QueryClient query and mutation cache callbacks and all three raw subtitle calls, including 4xx, 5xx, rejected fetch, missing problem fields, response status precedence, 429 quota, no duplicate toasts, preserved inline UI, and deliberate abort/stale suppression. Exercise the lifecycle with fake timers and keyboard interaction. Add one Playwright scenario using the DEV-525 playback flow, intercept a deterministic raw HTTP failure with known diagnostics, and verify exactly one app-wide toast and manual close. Require lint, typecheck, the web test suite, that Playwright check, contract regeneration/drift verification, and applicable .NET gates for the C# schema-only diff. Does this evidence bar satisfy the ticket?

**Patron**

[from Patron] DEV-567 Q7/12 ruling:
Verdict: Approved — adopt your recommended Vitest/RTL + MSW suite, one Playwright e2e scenario, OpenAPI/TypeScript contract drift verification, web lint/typecheck/test gates, and .NET verification gates for the C# OpenAPI schema transformer change.
- Vitest/RTL + MSW matrix (`specs/DEV-567/ticket-text.txt:20-26,34-35`; `recon-DEV-567` addendum lines 18-19): Cover real `QueryClient` `QueryCache`/`MutationCache` `onError` callbacks and all three raw subtitle calls (catalog, search, download/429 quota) across forced 4xx, 5xx, and rejected-fetch network failures, missing optional problem fields, HTTP status precedence, single-toast emission, abort/stale suppression, preserved inline UI, and fake-timer/keyboard/aria-live lifecycle.
- Playwright e2e scenario (`specs/DEV-567/ticket-text.txt:34-35`; `recon-DEV-567` addendum line 20): Add one deterministic Playwright e2e check (following the `dev525-subtitle-search.e2e.ts` flow) that forces an API failure with known diagnostics (`status`, `reason`, `traceId`) and verifies one app-wide toast and manual dismissal.
- Quality & contract gates (`specs/DEV-567/ticket-text.txt:26,36`; `specs/PRODUCT.md` §1; `AGENTS.md` Verification commands): Require web lint, typecheck, Vitest, and the Playwright check to be green, zero drift after OpenAPI + `npm run generate:api` regeneration, and applicable .NET gates green for the C# OpenAPI schema diff. Recorded in `specs/DEV-567/CONCLUSIONS.md`.


## Q8 — Grill closure, frozen scope (Q1–Q7), closing bar, and review cap
**Verdict:** Approved (ratified in one line per standing rules). Scope is frozen to Q1–Q7 (one dependency-free app-wide toast host, the two reporting boundaries, the C# `ProblemDetails` OpenAPI schema-only projection + regenerated `openapi.json`/`types.ts`, and the Q7 regression suite), with the standing Size M closing bar (Critical/High/Medium and failed mandatory ACs block; every Low gets explicit disposition and any accepted in-scope defect closes; 2-round review cap across Risk/Standards/Spec routing any remaining blocker to Bernstein); grill closed and cleared for `brief.md` and Quill drafting.


---

## Q8. Closing bar and round cap

**Keel**

[from Keel] DEV-567 Q8/12. May we close the grill with this frozen scope and review loop? Freeze Q1-Q7 to one dependency-free app-wide host, two reporting boundaries, the required ProblemDetails schema-only projection and generated artifacts, and the cited regression coverage. Anything else is a follow-up issue, not a finding in this round. I recommend that Critical, High and Medium findings and failed mandatory ticket criteria block closure, and that every Low receives explicit disposition with any accepted in-scope defect closed before clearance. Require the Size M Risk, Standards and Spec axes, cap review at two rounds, and route any open blocker at the cap to Bernstein. Please confirm scope, closing bar, round cap, and readiness for Quill drafting.

**Patron**

[from Patron] DEV-567 Q8/12 confirmed: Q1-Q7 scope frozen, standing Size M closing bar and 2-round review cap ratified, grill closed and ready for brief.md and Quill drafting (recorded in specs/DEV-567/CONCLUSIONS.md).

## Gate 1 Ruling — Phase 2 Step 7 (2026-11-23)

**Verdict:** `gate1: provisional` — no structural owner checkbox required on the spec PR; ready for spec PR commit and opening.
- **Prerequisites verified (`DEV-567` note lines 34-54; `specs/DEV-567/PLAN_ADJUDICATION.md:3-8`):** `/speckit-analyze` is clean (`0` open findings after round 2) and all 24 plan challenge findings across Sentry (11), Ledger (10), and Compass (3) are adjudicated (`PLAN FROZEN`).
- **User-Visible Outcome checklist (`specs/DEV-567/checklists/requirements.md:32-36`; `specs/DEV-567/spec.md:18-28`):** All three "User-Visible Outcome" items in `checklists/requirements.md` are ticked, covering "What the User Will See" and `SMOKE-001` / `SMOKE-002`.
- **Owner checkboxes (`specs/PRODUCT.md` §5; `specs/DEV-567/CONCLUSIONS.md` Q1–Q8; `specs/DEV-567/spec.md:8-10`):** Zero §2.3 escalations (no ticket contradiction/scope change and no constitution departure). No owner checkboxes are needed on the spec PR; ordinary Gate 1 approval occurs when the user merges the spec PR.

## Gate 1 Merge Receipt

**Verdict:** `gate1: verified`. The owner merged DEV-567 spec PR #485 at `3316637fde71680fe3b8b163cdcd1feb591c99a5`. This completes T000.

- **Evidence:** `recon-DEV-567` lines 60–61 and the `DEV-567` note lines 3–5. The merge commit is an ancestor of the Phase B pickup HEAD `c2cf527de98d6ea247856f56e9de2b634c52e3f3`.
