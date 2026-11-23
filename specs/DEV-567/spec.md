# Feature Specification: API failure toasts

**Feature Branch**: `feature/567-spec`
**Created**: 2026-11-23
**Status**: gate1: provisional
**Input**: Ticket DEV-567 (parent DEV-490; size M); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-567`, `recon-DEV-567` (intake HEAD `c7d5fb025b3793680a02fdf5f1785553becf9343`); `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Patron rulings Q1–Q8; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-23

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (one dependency-free app-wide toast host; global React Query cache reporting; explicit reporting at the three raw subtitle acquisition calls; C# OpenAPI `ProblemDetails` optional `reason`/`traceId` documentation + regenerated web contract; Q7 evidence matrix) is closed in `brief.md` / `CONCLUSIONS.md` Q1–Q8. Taste `[assumed]` items in `ASSUMPTIONS.md` are Patron-ratified. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: App-wide toast host mounted above route content (visible on any route once the SPA is running).
- **What the user sees or does**: When an API call fails, a temporary toast appears with a short title, the HTTP status (or an explicit unavailable label when there is no response), the problem `reason` when the server sent one, and the `traceId` (or an explicit unavailable label when missing). Toasts stack (at most three; oldest dropped), auto-dismiss after a few seconds (paused while hovered or focused), and can be closed with a keyboard-accessible Dismiss control. Screen readers hear each new toast once without focus moving. Existing inline subtitle-panel error lines remain as they are today.
- **What it needs to show up**: Default Compose / home web stack with the API reachable through the existing proxy (or Vite on `http://localhost:5173`); a failed API call (forced HTTP problem response or a genuine transport failure). No new npm dependency.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** the SPA is open on a playback/subtitle flow that can trigger a raw subtitle API call, **When** that call fails with a known HTTP problem response carrying status, reason, and traceId, **Then** exactly one app-wide toast shows those diagnostics and existing inline subtitle error behaviour still appears as before.
- **SMOKE-002**: **Given** that toast is visible, **When** the user activates Dismiss (pointer or keyboard), **Then** the toast is removed and focus is not stolen away from where they were working.

## Scope

**In scope** (`brief.md` Frozen scope and Patron rulings 1–7; ticket Wanted / Done when):

1. **App-wide toast host** with no new dependency: house aubergine/ink/vermilion tokens, square corners, no glass or blur, reduced-motion styling; bottom-end on desktop and within narrow viewports; cap three visible toasts (FIFO eviction); five-second auto-dismiss of unpaused time with hover/focus pause; keyboard-accessible Dismiss; persistent polite live region announcing each new toast once without moving focus.
2. **Two reporting boundaries**: (a) global React Query `QueryCache` / `MutationCache` `onError` on the real QueryClient; (b) explicit helper at the three raw subtitle acquisition executions in `useSubtitleAcquisition` (languages catalog, search, download — including returned 429 quota failures and thrown transport errors). One toast per terminal failed execution; suppress intentional aborts and superseded stale results; keep `retry: false` and existing inline subtitle error states.
3. **Shared normalization** at those two points only (no global fetch interceptor): prefer actual HTTP response status, then valid structured `ProblemDetails.status`; optional string `reason` and `traceId` as text; short title from problem title or `Request failed` / `Network error`; never expose raw exception messages or `detail`; classify transport failures at API boundaries so programming exceptions are not mislabeled as network errors. Minimal React Query error-adapter edit only if recon shows an existing caller discards diagnostics before the cache.
4. **Contract documentation only**: declare already-emitted optional string `reason` and `traceId` on the C# OpenAPI `ProblemDetails` projection via a narrowly scoped schema transformer registered with the existing v1 transformers; export the app-generated v1 document to `web/src/api/openapi.json`; regenerate `web/src/api/types.ts` via `npm run generate:api`. Zero change to runtime HTTP payloads, routes, or status codes.
5. **Acceptance evidence**: Vitest/RTL + MSW matrix; one deterministic Playwright scenario; contract drift verification; web lint/typecheck/tests; applicable .NET gates for the schema-only C# diff. Size M three-axis review; Critical/High/Medium and failed mandatory ACs block; every Low explicit disposition; accepted in-scope defects close; two-round review cap with remaining blockers routed to Bernstein.

**Out of scope**:

- New npm/NuGet dependency or dependency swap (`PRODUCT.md` §5 item 1; ticket).
- New project, top-level folder, or architectural layer beyond files named inside the brief’s authorized areas.
- Database schema changes; public route / DTO / status-code behaviour changes beyond documenting existing optional problem extensions.
- Anything touching `Features:LocalPlay`, secrets, or `Process.Start`.
- Global fetch interceptor; changing query retry policy; removing or replacing inline subtitle error UI.
- Fabricating HTTP status codes or synthetic trace IDs on transport failures (`PRODUCT.md` §4; Q1).
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft.

**Precedence**: Ticket Scope & Technical Design and acceptance criteria (`ticket-text.txt`) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern plan boundaries. A clean Keel `/speckit-analyze` advances to plan challenge; only Keel declares PLAN FROZEN after that challenge disposition. Spec PR and Gate 1 follow freeze — not claimed from this draft.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**API failure toast**
Temporary app-wide alert for a terminal failed API query, mutation, or raw subtitle acquisition call, carrying short title plus status / reason / traceId diagnostics as ruled.
_Avoid_: treating inline subtitle panel copy as the toast; inventing diagnostics when the transport produced none

**Network error**
Transport failure at a known API boundary where no HTTP response or problem body exists; title `Network error` with explicit unavailable status and traceId labels; omit reason.
_Avoid_: labeling unrelated programming exceptions as network errors

**Reporting boundary**
Either the global React Query cache `onError` callbacks or the explicit raw openapi-fetch helper at the three subtitle acquisition calls — not a third global interceptor.
_Avoid_: duplicate toasts for the same terminal failure; toasting intentional aborts or superseded stale results

## User Scenarios & Testing *(mandatory)*

### User Story 1 - HTTP API failure toast (Priority: P1) 🎯 MVP

When an API call returns an HTTP error, the owner sees one app-wide toast with a short title, the response status, reason when present, and traceId when present (or unavailable when omitted), whether the call went through React Query or the raw subtitle helper.

**Why this priority**: Ticket Done when requires a forced 4xx/5xx to show one toast with status and traceId; this is the primary gap versus silent or panel-only failures.

**Independent Test**: Force a representative React Query query failure, a React Query mutation failure, and each of the three raw subtitle calls with 4xx/5xx (including a 429 quota response on search/download); assert exactly one toast per terminal failure with correct diagnostics and preserved inline subtitle states.

**Acceptance Scenarios**:

1. **Given** a React Query API query or mutation that ends in a forced 4xx or 5xx problem response, **When** the terminal failure reaches the global cache callback, **Then** exactly one toast shows a short title, the HTTP status (preferring the response status over body-only status), `reason` when supplied, and `traceId` when supplied (or an explicit unavailable label when omitted).
2. **Given** a raw subtitle languages catalog, search, or download execution that fails with an HTTP problem body (including search/download 429 with quota diagnostics), **When** the explicit helper reports the failure, **Then** exactly one toast shows the same diagnostic contract and existing inline subtitle error UI still updates as today.
3. **Given** optional `reason` or `traceId` absent from an otherwise successful problem parse, **When** the toast is shown, **Then** reason is omitted when absent and missing traceId uses the explicit unavailable label — no fabricated values.

---

### User Story 2 - Network / transport failure toast (Priority: P1)

When a genuine transport failure occurs at an API boundary (no HTTP response), the owner sees one `Network error` toast with explicit unavailable status and traceId labels and no reason.

**Why this priority**: Ticket Done when and Context require network errors with no problem body to be covered; Patron Q1 ruled unavailable labels satisfy the status/traceId acceptance criterion without fabricating values.

**Independent Test**: Reject fetch at React Query and at each raw subtitle reporting boundary; assert one `Network error` toast with unavailable status/traceId and no reason; programming exceptions outside API boundaries are not labeled as network errors.

**Acceptance Scenarios**:

1. **Given** a rejected fetch / transport failure at a React Query API boundary or at a raw subtitle catalog/search/download boundary, **When** the failure is reported, **Then** exactly one toast titled `Network error` shows explicit unavailable indicators for status and traceId and omits reason.
2. **Given** an intentional abort or a superseded stale subtitle result, **When** that outcome occurs, **Then** no failure toast is emitted for that outcome.
3. **Given** an unrelated programming exception that is not a classified API transport failure, **When** it surfaces, **Then** it is not presented as a `Network error` toast under this feature’s normalization rules.

---

### User Story 3 - Toast lifecycle and accessibility (Priority: P2)

The toast host stacks, times out, pauses, dismisses, and announces without trapping or moving focus, using house styling and reduced-motion rules.

**Why this priority**: Ticket Wanted lines require temporary, closable, capped stacking and aria-live announcement; Patron Q6 froze the lifecycle defaults.

**Independent Test**: Fake timers and keyboard interaction (Vitest/RTL) prove the three-item FIFO cap, five-second unpaused expiry, hover/focus pause and resume, manual Dismiss, stable focus, one polite announcement per addition, and reduced-motion treatment. Narrow viewport-fit (toast host remains within the viewport) is proven in the single Playwright smoke scenario — not measured as browser layout in Vitest/RTL.

**Acceptance Scenarios**:

1. **Given** three toasts already visible, **When** a fourth failure is reported, **Then** the oldest toast is dropped and at most three remain visible.
2. **Given** a visible toast, **When** five seconds of unpaused time elapse, **Then** it auto-dismisses; hover or focus pauses that timer and resume continues it.
3. **Given** a newly added toast, **When** it appears, **Then** a persistent polite live region announces it once without moving keyboard focus, and a keyboard-accessible Dismiss control removes it.

---

### Edge Cases

- What happens when only a structured problem body reaches the cache without a separate HTTP status? Prefer a valid `ProblemDetails.status`; otherwise follow the unavailable rules for missing status.
- How does the system handle missing optional `reason` / `traceId` on HTTP failures? Omit reason; show unavailable for missing traceId; never invent values.
- What happens on intentional `AbortController` cancellation or superseded stale subtitle results? No failure toast.
- What happens when distinct failed executions occur in sequence? Each may report its own toast (subject to the three-toast cap).
- How are quota 429 responses handled on search/download? Reported through the explicit helper like other HTTP failures while inline quota state remains.
- What if an existing React Query caller strips diagnostics before throwing? If recon already identifies that boundary, apply the minimum metadata-preserving adapter edit only; if the required boundary remains unidentified, stop for targeted recon. Routes and retry policy stay unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST show exactly one app-wide failure toast per terminal failed React Query query or mutation execution via the global cache callbacks, and exactly one per failed raw subtitle languages catalog, search, or download execution via the explicit helper (`brief.md` ruling 4; ticket lines 20, 23–25).
- **FR-002**: System MUST NOT emit a failure toast for intentional aborts or superseded stale subtitle results, and MUST preserve existing inline subtitle error states and `retry: false` (`brief.md` ruling 4; ASSUMPTIONS Q4).
- **FR-003**: For HTTP failures, the toast MUST use a short title (`ProblemDetails.title` or `Request failed`), the actual HTTP response status when available (else a valid structured problem status), optional `reason` text when provided, and `traceId` when provided (else an explicit unavailable label) — never raw exception messages or `detail` (`brief.md` rulings 1 and 5; ASSUMPTIONS Q1/Q5).
- **FR-004**: For classified transport failures at API boundaries with no response/problem body, the toast MUST be titled `Network error`, show explicit unavailable indicators for status and traceId, omit reason, and MUST NOT fabricate status codes or trace IDs (`brief.md` ruling 1; ASSUMPTIONS Q1; `PRODUCT.md` §4).
- **FR-005**: System MUST mount a single toast host above route content, place it bottom-end on desktop and within narrow viewports, cap visible toasts at three with oldest-first eviction, auto-dismiss after five seconds of unpaused time with hover/focus pause, provide keyboard-accessible Dismiss, and announce each new toast once via a persistent polite live region without moving focus (`brief.md` ruling 6; ASSUMPTIONS Q6).
- **FR-006**: Toast styling MUST use existing aubergine/ink/vermilion house tokens, square corners, no glass or blur, and reduced-motion treatment under `prefers-reduced-motion: reduce` (`brief.md` ruling 6; ticket lines 17–19).
- **FR-007**: Normalization and reporting MUST share one path serving the two ticket-mandated boundaries and MUST NOT add a global fetch interceptor (`brief.md` ruling 5).
- **FR-008**: The C# OpenAPI v1 projection MUST document optional string `reason` and `traceId` on `ProblemDetails` via a narrowly scoped schema transformer registered with existing v1 transformers; the checked-in `web/src/api/openapi.json` and generated `web/src/api/types.ts` MUST be regenerated from that export without hand-editing generated types; runtime HTTP payloads, routes, and status codes MUST remain unchanged (`brief.md` rulings 2–3; ticket line 26).
- **FR-009**: Acceptance evidence MUST include Vitest/RTL+MSW coverage of QueryClient query and mutation failures, the three raw subtitle calls (4xx/5xx, rejected fetch, missing optional fields, status precedence, 429 quota, single-toast / abort / stale / inline preservation), and toast lifecycle (Vitest/RTL; not browser layout), plus one deterministic Playwright HTTP-failure toast scenario that also asserts narrow viewport-fit and dismiss/focus, zero contract drift on re-export/regenerate, green web lint/typecheck/tests plus the Playwright check, and applicable .NET gates for the C# schema diff (`brief.md` ruling 7).

### Key Entities *(include if feature involves data)*

- **Failure toast**: Ephemeral UI item with title, status display, optional reason, traceId display, and dismiss controls; not persisted.
- **Normalized API failure**: Shared diagnostic view of a terminal API failure (HTTP vs network classification, status, reason, traceId, title) produced only at the two reporting boundaries.
- **ProblemDetails (documented extensions)**: Existing RFC 7807 problem payload whose optional `reason` and `traceId` extensions are already emitted at runtime and must appear as optional strings in the OpenAPI/TypeScript contract.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A forced 4xx or 5xx API failure produces exactly one visible app-wide toast that includes the status and the traceId (or an explicit unavailable label when traceId is absent) (`ticket-text.txt` Done when; Q1/Q4).
- **SC-002**: A genuine network/transport failure at an API boundary produces exactly one `Network error` toast with explicit unavailable status and traceId labels and no fabricated diagnostics (`ticket-text.txt` Done when / Context; Q1).
- **SC-003**: Distinct terminal failures may each produce a toast; intentional aborts and superseded stale results produce none; at most three toasts are visible at once (`brief.md` rulings 4 and 6).
- **SC-004**: Manual Dismiss and five-second unpaused auto-dismiss remove toasts; hover/focus pause works; each new toast is announced once without focus theft (`brief.md` ruling 6).
- **SC-005**: Inline subtitle error behaviour observed before this ticket remains available alongside the toast (`brief.md` ruling 4; ticket line 13).
- **SC-006**: Regenerated OpenAPI and TypeScript show optional `reason` and `traceId` on `ProblemDetails` with zero drift on a second export/generate cycle; runtime API behaviour is unchanged (`brief.md` rulings 2–3 and 7).
- **SC-007**: Web lint, typecheck, Vitest suite, and the targeted Playwright check pass; applicable .NET verification for the schema-only diff reports honest harness outcomes (`brief.md` ruling 7; ticket line 36).

## Assumptions

- Taste and presentation defaults logged in `ASSUMPTIONS.md` (Q1 unavailable indicators; Q4 abort/stale suppression; Q5 short title fallbacks `Request failed` / `Network error`; Q6 stack/timeout/placement/`Dismiss`/`aria-live="polite"`) are Patron-ratified for this draft.
- Quill-chosen new file names stay inside the brief’s authorized areas (`web/src/ui/`, `web/src/api/`, `web/src/index.css`, `web/src/main.tsx`, `web/src/features/playback/`, `web/e2e/`, `src/LamuFlix.Api/` OpenAPI registration area); they are not new §2.3 structural layers.
- The exact app-generated OpenAPI v1 export invocation is verified and recorded as the first contract implementation task; drafting does not invent a checked-in server export script recon did not find (`brief.md` ruling 3; recon addendum line 27).
- Any React Query error-adapter edit remains contingent: apply the minimum approved fix when recon already names a discarding boundary; stop for targeted recon only when a required boundary remains unidentified. Drafting does not preselect that file (`brief.md` ruling 5 / approach step 3; Q5).
