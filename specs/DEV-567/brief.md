# DEV-567 — API failure toasts

**PLAN FROZEN — Phase 2 step 6 complete (2026-11-23).** Ready for ordinary Gate 1 ruling. Patron closed the grill after 8 of 12 allowed questions. T000 records the verified Spec PR merge before Phase B pickup.

## Frozen scope and closing bar

Deliver one dependency-free app-wide toast host, global React Query cache reporting, and explicit reporting at the three raw subtitle acquisition calls. Document the existing optional `reason` and `traceId` fields in the C# OpenAPI schema and regenerate the web contract. Include the regression evidence below. Preserve runtime HTTP responses, routes, query retry policy, and inline subtitle behavior. Anything else is a follow-up issue, not a finding in this round.

The Size M review requires all three axes (Risk, Standards, Spec) pinned to the reviewed revision and explicit disposition of every finding. Critical, High, Medium, and any failed mandatory ticket acceptance criterion block closure. Every Low needs explicit disposition, and every accepted in-scope defect must close before clearance. Cap the review at two rounds and route remaining blockers at the cap to Bernstein. No owner checkbox is required for the unavailable diagnostic labels.

## Patron rulings

1. **Missing diagnostics.** Show one `Network error` toast on a genuine transport failure. Display HTTP status and trace ID as unavailable when missing. Show reason only when provided. For HTTP failures prefer the actual response status. This satisfies the ticket's network acceptance criterion. [Ticket lines 20, 26, 30 and 34](ticket-text.txt#L20)
2. **Contract boundary.** Keep the existing HTTP payload and endpoints. Document the already-emitted optional string fields on the C# OpenAPI projection and regenerate the JSON and TypeScript in that order. [Ticket line 26](ticket-text.txt#L26)
3. **Projection and export.** Register a narrowly scoped `ProblemDetails` schema transformer alongside the other v1 transformers in `src/LamuFlix.Api/Program.cs`. Verify and record the invocation that exports the app-generated v1 document before regenerating TypeScript. Recon confirms the TypeScript generator and the current framework-projected schema. It does not establish a checked-in server export command. Recon addendum lines 25–29.
4. **Trigger ownership.** Report one toast per terminal failed API query or mutation through its global cache callback and one per failed raw subtitle languages catalog, search or download execution through the explicit helper. Search and download carry quota diagnostics, including returned 429s. Suppress intentional aborts and superseded stale results. Distinct failed executions may each report. Keep the existing `retry: false` setting and inline subtitle error states. [Ticket lines 13 and 20–25](ticket-text.txt#L13) · Recon addendum lines 4–11.
5. **Normalization.** Use actual HTTP status first, then valid structured `ProblemDetails.status` when that is all the cache receives. Display optional string `reason` and `traceId` as text. Use a short problem title or `Request failed` for HTTP errors and `Network error` for classified transport failure. Keep raw exception messages and `detail` out of the toast. Classify transport errors at API boundaries so programming exceptions are not mislabeled. One shared normalization path serves the two reporting points without a global fetch interceptor. Any necessary React Query error-adapter edit must be minimal and supported by recon identifying where an existing caller discards diagnostics. [Ticket lines 20 and 23–30](ticket-text.txt#L20) · Recon addendum lines 7–11 and 30.
6. **Host and accessibility.** Mount a single host above the route content. Use bottom-end placement on desktop and keep it within narrow viewports. Cap visible toasts at three and evict the oldest on overflow. Dismiss after five seconds of unpaused time, pause on hover or focus, and provide a keyboard-accessible `Dismiss` control. Use a persistent polite live region to announce each new toast once without moving focus. Use existing aubergine, ink, and vermilion tokens, square corners, and reduced-motion styling. Omit glass and blur. [Ticket lines 17–22](ticket-text.txt#L17)
7. **Acceptance evidence.** Use Vitest/RTL with MSW, one deterministic Playwright scenario, generated-contract drift verification, green web lint/typecheck/test gates, and applicable .NET gates for the schema change. See the evidence matrix below. [Ticket lines 34–36](ticket-text.txt#L34) · Recon addendum lines 17–20.
8. **Closure.** Patron ratified Q1–Q7 as frozen scope, the Size M closing bar, the two-round review cap, and readiness for Quill drafting.

[Full grill exchange and cited rationale](CONCLUSIONS.md) · [Taste rulings](ASSUMPTIONS.md)

## Approach, files, and task order

| Order | Work and expected files |
| --- | --- |
| 1 | Verify the app-generated v1 OpenAPI export invocation. Add a `ProblemDetails` schema transformer in the existing API OpenAPI code area and register it in `src/LamuFlix.Api/Program.cs:60–67`. Export to `web/src/api/openapi.json`, then run `npm run generate:api` from `web/` to regenerate `web/src/api/types.ts`. Review all generated output for scope and reproducibility. `GlobalExceptionHandler.cs` and `ProblemDetailsTraceId.cs` substantiate runtime emission. |
| 2 | Add the app-wide toast host and focused host tests under `web/src/` and apply house tokens and motion rules in `web/src/index.css`. Mount the host in `web/src/main.tsx` where it remains available across routes. |
| 3 | Implement the shared API failure normalization and reporting helper under `web/src/api/`, consuming the generated contract. In `web/src/main.tsx`, wire `QueryCache` and `MutationCache` global callbacks on the real QueryClient. Exercise representative API query and mutation flows through those callbacks. T001 records an identified boundary. T010 applies only the minimum metadata-preserving edit when the same execution loses available diagnostics, or stops for targeted recon if the needed boundary is unidentified. |
| 4 | Wire the helper explicitly at the languages catalog, search, and download boundaries in `web/src/features/playback/useSubtitleAcquisition.ts:79–96,107–142,177–205`. Check transport and response failures at each reporting boundary while keeping inline handling, 429 quota state and stale-request guards. Extend `web/src/features/playback/subtitlesTab.test.tsx` using the existing handlers. |
| 5 | Add one Playwright failure check under `web/e2e/`, following `web/e2e/dev525-subtitle-search.e2e.ts:166–286`. Run the complete evidence matrix and applicable gates. |

Quill can choose the names of new files within the listed areas. Recon establishes the existing entry points and fixtures. No React Query error-adapter file is preselected.

## Test strategy and gate expectations

- **Vitest/RTL and MSW.** Use the existing MSW server at `web/src/test/server.ts` and nearby acquisition fixtures. Exercise the configured QueryClient through a failed API query and a failed API mutation. Cover the three raw subtitle calls. Across those paths prove forced 4xx and 5xx diagnostics and rejected fetch with unavailable diagnostics. Check response status precedence, structured-status fallback, absent optional problem fields, a 429 quota response, existing inline behavior, one toast per report, distinct failures, and intentional abort and stale-result suppression.
- **Toast lifecycle.** Use fake timers and keyboard interaction to cover the three-item FIFO cap, five-second expiry, pause and resume on hover and focus, manual dismissal, stable focus, and one polite announcement per addition. Check reduced-motion and narrow-safe styling intent in Vitest/RTL. Verify actual viewport fit in the single Playwright check.
- **Playwright.** Use a deterministic raw subtitle HTTP failure with known status, reason, and trace ID. From the playback flow verify exactly one app-wide toast with those diagnostics, then dismiss it and verify removal.
- **Contract and quality gates.** Re-export the C#-generated v1 document and regenerate the TypeScript, checking zero drift on repeat and that both added properties are optional strings. Require web lint, typecheck, the Vitest suite, and the targeted Playwright check to pass. Apply the repository's .NET verification to the C# OpenAPI schema diff and report gate outcomes using the harness vocabulary.

## Phase 2 step 6 plan amendments

- **Failure classification and cancellation (Q1/Q4/Q5).** A known unsuccessful HTTP response or recognized structured problem with valid HTTP failure status is reportable as HTTP. Network reporting requires explicit transport provenance from a scoped API request boundary. Record it for a genuine fetch rejection before parsing or application logic. Ignore cancellations and unclassified throws. Preserve known HTTP status when an error body is empty, HTML or malformed. The reporter tolerates these inputs without throwing or interrupting inline handling. Report raw failures only after the request's abort and currency checks, as applicable at each call site. Preserve additive 429 toasts and inline quota handling. Toasts use the response status first while the inline panel retains its existing decoded-body handling.
- **Wiring and lifecycle (Q4–Q7).** Use a QueryClient factory in the authorized `web/src/api/` area so the tests can drive the same cache wiring as `main.tsx`. Use an app-scoped in-memory toast queue, with the host subscribing to it and each test creating fresh state. Each failure has one reporting owner. Distinct failures may arrive together and use the existing FIFO cap. Dispose evicted items' timers and pause state. Render approved diagnostic fields as text and wrap long values within the host. Preserve the inline subtitle decoder and use the generated `ProblemDetails` type for the shared toast normalizer.
- **Bounded evidence (Q2/Q3/Q7).** T001 records the export invocation and relevant error paths. T002 verifies the app-generated v1 export with its environment/configuration or stops for Wisp recon. Verify optional strings before TypeScript generation. Scope the transformer to the exact `ProblemDetails` schema and check schema output and runtime invariance. The existing .NET mutation gate applies according to `harness.yml`; use configured thresholds. Keep the single HTTP Playwright scenario and prove transport, cancellation and malformed payloads in Vitest/RTL+MSW.

[All 24 finding dispositions and the freeze receipt](PLAN_ADJUDICATION.md)

## Evidence roots

`DEV-567` task note (Size M, branch `feature/567-spec`), `recon-DEV-567` (intake HEAD `c7d5fb025b3793680a02fdf5f1785553becf9343`, addendum lines 4–30), and [ticket text](ticket-text.txt).

