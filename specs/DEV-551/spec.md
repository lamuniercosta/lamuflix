# Feature Specification: Multi-provider subtitle acquisition seam

**Feature Branch**: `feature/551-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-551 (size L; ui:none); grill-closed `brief.md` (Q1–Q12); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q6); Proposed ADR `docs/adr/0052-multi-provider-subtitle-acquisition.md`; Maestri notes `DEV-551`, `recon-DEV-551`; `specs/PRODUCT.md`. Decisions come only from brief and its rulings.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Q10–Q11; `PRODUCT.md` §5). Ordinary owner Gate 1 remains the Spec Kit PR merge path. ADR 0052 stays Proposed on Spec PR until Accepted first in the delivery task sequence after Gate 1, with the before-deliverable-merge bar retained (`brief.md` Closing bar / Approach; Q11).

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Search call shape and request contract (Q1), startup refusal vs host continuity and 409 (Q2), additive search response and legacy compatibility (Q3), per-provider destination and credential isolation (Q4), capability precedence / fallback / cancellation / quota (Q5), stable cross-provider ordering (Q6 taste `[assumed]`), provider-bound Candidate IDs and ranked issuance (Q7), named registration/catalog/config (Q8), per-provider health (Q9), frozen scope and file envelope (Q10), closing bar (Q11), and round cap (Q12) are settled in `brief.md` and `CONCLUSIONS.md`. DEV-525 Done is a verify-before-build dependency with no runtime claim here. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap that cannot be closed from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket is `ui:none`. It delivers the multi-provider subtitle acquisition seam, additive search-response fields, regenerated OpenAPI/TypeScript contracts, per-provider health data, glossary and setup-doc updates, and the shared adapter contract suite. End-user browse, playback, and CC/Subtitles UI behaviour stay on the existing legacy contract; DEV-552 owns the picker that consumes provider identity. Existing web tests remain green without UI changes.

## Scope

**In scope** (`brief.md` Frozen scope; Q1–Q12; Proposed ADR 0052):

1. **ADR 0052**: Amend ADR 0041 by reference; leave 0041 intact; document precedence in 0052; Accept ADR 0052 first in the delivery task sequence after separate Gate 1, retaining the before-deliverable-merge bar (Q10–Q11; brief.md:19).
2. **Typed provider seam**: Keep existing `ISubtitleProvider` with provider identity, declared search capabilities, opaque file reference, and provider outcomes on search/download types; OpenSubtitles remains the sole production adapter and runs the shared adapter contract suite (Q8, Q10).
3. **Named registration and configuration**: One ordered concrete provider lookup on the **Core** side of the existing `ISubtitleProvider` seam, shared by search, download, SubtitleLookup, and health (Constitution I; not a new port/layer); that lookup owns **process-local per-provider spacing and health-observation state**, one instance per provider for the **host lifetime**, shared across concurrent search requests and health (Ledger S4); typed HTTP client registration and composition stay **outside** Core (Infrastructure/Api); mutable state remains instance-owned; typed options per adapter section; OpenSubtitles section path preserved; adapter-local `Enabled`, timeout, `MinimumRequestGap`, and credential validation with documented valid ranges and no guessed safety defaults (Patron S5 names); `Subtitles:ProviderOrder` for stable registered names (registration order default; omitted names appended); unknown/duplicate ProviderOrder names log a safe warning and are ignored (first wins), never a startup failure (Q8).
4. **Aggregate search**: Existing `GET /movies/{id:int}/subtitles/search` remains one aggregate call per language; concurrent fan-out to every enabled usable provider; each provider independently timeout-bounded; results merged after all settle; no provider query parameter (Q1).
5. **Capability precedence**: Each provider walks declared inputs in order file hash → IMDb ID → TMDb ID → title plus year; skips unavailable inputs (including null TMDb ID pending DEV-539); advances only after a successful empty search; timeout, quota, or failure stops that provider without further fallback; one requested language per call; no formable input → safe provider-local failure; search 429 → that provider’s quota state without inventing download quota; known download-quota exhaustion blocks downloads on otherwise valid candidates only; whole provider branch (spacing + fallback) inside the per-provider timeout; caller cancellation propagates as cancellation, never as provider timeout (Q5).
6. **Stable merge order**: Subtitle Match Percent desc, download count desc, configured provider order, then existing within-provider tie behavior; stable merge so completion order never reorders ties; single-OpenSubtitles ordering unchanged (Q6; `ASSUMPTIONS.md`).
7. **Additive public search contract**: Every legacy field kept (unchanged legacy contract; baseline parity proven in T010 — do not invent unverified field/type/nullability tables from recon); candidate `providerId` (stable configured provider name; no file reference); ordered `providers` array (`providerId`, `state`, `reason`, nullable `quota` in existing quota shape) including off rows for configured providers skipped before I/O; ProblemDetails extension key `providers` on **settled search** error responses only — **present** on Q2 no-usable search 409 (classified off/unusable rows settled pre-I/O) and on legacy settled search errors, **absent** on request-level errors that preempt settlement (brief.md:69); **download** retains the existing ProblemDetails shape without `providers` (acquisition 409, read-only, stale/invalid); usability frozen at request-time acquisition configuration validation so timeout/quota/failure/no-formable-input cannot turn a configured-usable multi-provider search into Q2 409; HTTP status uses the disjoint precedence in `contracts/search-response.md` (first match wins): request-level errors → existing; no usable provider → Q2 409; **one-provider** OpenSubtitles usable → legacy statuses/values (may include non-200 settled baseline — do not invent); **multi-provider** with ≥1 usable (including one usable plus off/refused siblings, and multi-usable zero-candidate) → 200; legacy quota projects the first usable provider in configured order, never summed; legacy download availability = library policy AND at least one provider permits download; `state`/`reason` are closed Enumerations (or equivalent) of safe codes at the Core subtitle outcome boundary, serialized by JSON `Name` in the approved API DTO scope (Patron S5 closed vocabulary; never upstream message text, URLs, or credentials) with exclusive reason predicates/precedence per the contract; DEV-552 owns any further per-provider field extension (Q3; plan-challenge M4/S2/S5). Contract detail: `contracts/search-response.md`.
8. **Provider-bound Candidate IDs**: Keep 1000-entry and five-minute defaults and existing expiry/eviction; ranked batch binding so one aggregate search never publishes a token it evicts itself; when one aggregate exceeds capacity, return only the highest-ranked bindable rows in Q6 order and every returned token resolves immediately; tokens opaque, bound to movie, language, and provider with a server-owned file reference; route only to the bound provider; same reference strings from providers A and B must never collide; an earlier token evicted by a later search takes the existing stale response with zero paid calls (independently of expiry); log truncation with a safe count only; no new truncation response field (Q7).
9. **Explicit provider-bound download**: Viewer-selected download only; always originates from selection and goes only to the bound provider; cross-provider and cross-movie misuse cannot issue a paid call; stale/evicted tokens require a new search (Q7; ADR 0052).
10. **Outbound request policy**: Each typed client carries its own allow-list of search and download destinations from **adapter declarations or explicit typed configuration, independently of upstream responses**, enforced on the initial URL and every redirect hop **before I/O**; credential headers/login only to configured HTTPS search origin, never to download hosts; cross-host redirects and HTTPS-to-HTTP downgrade refused before following; plain HTTP requires **two independent conditions** — the adapter declares itself credential-free **and** its typed configuration explicitly opts in (opt-in disabled by default) — and is refused when the adapter uses credentials/login or credential/login configuration is present; OpenSubtitles remains HTTPS-only; automatic redirect following disabled on the handler with each hop validated in code; existing size caps, path containment, and SRT checks apply to every download; diagnostics use safe provider codes; credentials stay in existing typed secret configuration (Q4; plan-challenge H1/M1).
11. **Startup refusal and request-time acquisition validation (Constitution VII)**: Provider-local validation under the subtitle acquisition **ValidateOnStart exception** (acquisition settings never abort host startup); unsafe (non-HTTPS) URL refused with a logged warning that carries **only** the stable provider name and `unsafe_base_url` (never raw URL, credentials, or query) and that provider initialised unusable (never sends a request); credential-free dual opt-in plain HTTP remains the separate Q4 path, not this refusal; host, API, and existing-track routes keep running; missing required credentials **or invalid timing** classify that provider off. **Search 409 predicate** (all enabled providers): request-time acquisition configuration validation returns the sole missing/invalid configuration **search** 409 only when **no enabled provider is usable** (including sole OpenSubtitles missing/invalid), with zero paid provider calls, and **freezes usability for that request**; subsequent timeout, quota, failure, or no formable input settles provider outcomes without flipping a configured-usable multi-provider search into that 409; a usable sibling leaves search configured so an off or refused provider is a named per-provider outcome, not a 409. **Download 409 predicate** (bound provider): after ADR 0041 read-only precedence, download validates **only the bound provider’s** acquisition configuration and returns the sole missing/invalid configuration **download** 409 when that bound provider is missing/invalid or unusable — **even when a sibling provider remains usable** — with zero paid calls to either provider; download ProblemDetails stay the existing shape without `providers` (Q2; Ledger S1; ADR 0052; plan-challenge M2/M3).
12. **Per-provider health**: Existing health check reports per registered provider in configured order: enabled, configured, and last settled search outcome as a safe code or unobserved; process-local bookkeeping shared with search path; last completed search wins across overlapping requests; caller cancellations not recorded; timestamps from TimeProvider; health makes no network calls and never exposes raw errors or secrets; off/failed never turns readiness Unhealthy and never affects existing-track routes; at most Degraded on a non-readiness check; state resets to unobserved on restart (Q9).
13. **Glossary and setup docs**: Add Subtitle Provider to `CONTEXT.md`; update existing subtitle setup documentation and `SubtitleSetupDocumentationTests` for named settings, valid ranges, provisioning order, key/enablement behaviour, and credential examples by configuration key or placeholder only (Q8, Q10; brief Approach).
14. **Contract chain**: Map C# DTOs and settled ProblemDetails extensions to OpenAPI and generated TypeScript; preserve all existing web tests (Q3, Q10).
15. **Tests**: Shared abstract xUnit WireMock suite through each adapter’s real typed HttpClient; one-provider parity and two-provider isolation; binder/property coverage; API regression; health; setup-doc and contract-drift checks; preserve `OpenSubtitlesSubtitleProviderTests` and existing OpenSubtitles assertions (`brief.md` Test strategy).

**Out of scope** (`brief.md` Frozen scope / Q10):

- New real subtitle adapters beyond OpenSubtitles.
- Picker UI, track labels, automatic downloads, TV support, scorer changes.
- New NuGet/npm dependency; new project, top-level folder, or architectural layer (catalog is a concrete lookup, not a repository/mediator/services layer).
- Database schema change.
- `Features:LocalPlay`, secrets hardcoding, `Process.Start`.
- Changing ADR 0041 text beyond documenting amendment precedence in 0052.
- Inventing Gate 1 / plan freeze / runtime PASS; anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Wanted / Done when and Where it lands credited from `recon-DEV-551` and the live ticket are approved authority (`PRODUCT.md` §3). Brief Q1–Q12, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, and Proposed ADR 0052 govern plan boundaries. Quill invents no decisions.

**Dependencies**: Confirm DEV-525 is Done before starting Build (verify-before-build; no runtime claim in this grill). DEV-552 owns picker and any further per-provider response-field extension. DEV-539 covers null TMDb ID availability (skip until present).

## Domain vocabulary *(mandatory)*

Qualified glossary names for Principle VIII `CONTEXT.md` (additive Subtitle Provider; keep existing Subtitle Candidate / Candidate ID / Local Subtitle Track / Subtitle Match Percent meanings):

**Subtitle Provider**
A named upstream source that supplies remote Subtitle Candidates for explicit viewer-selected download.
_Avoid_: treating a provider as an installed local track; sharing one provider’s credentials or trust with another

**Subtitle Candidate**
A remote search result; never an installed local track merely because it appears in search. Carries provider identity on the public candidate without exposing the server-owned file reference.

**Candidate ID**
Opaque process-local selection token bound server-side to movie, canonical language, and one provider’s opaque file reference; not a path, URL, or raw provider file ID. Routes only to the bound provider.

**Local Subtitle Track**
A discovered subtitle, including a successfully committed downloaded SRT in Subs; selectable independently of acquisition availability.

**Subtitle Match Percent**
Moviehash 100, otherwise rounded distinct-token overlap; no extra confidence claim. Primary cross-provider sort key (Q6).

Consumed identifiers (unchanged meanings): **SubtitleTrackDto**, **ISubtitleProvider**, **Subtitles:SaveEnabled**, **Subtitles:PreferredLanguages**, **Features:LocalPlay**, **MovieId**, **SupportedSubtitleLanguages**, **SubtitleCandidateBinder**, **SubtitleMatchScorer**, **Subtitle Download Availability**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Aggregate multi-provider search with additive outcomes (Priority: P1) 🎯 MVP

A caller searches one language on an authorized movie. The server concurrently asks every enabled usable Subtitle Provider, waits for each bounded settlement, merges ranked candidates with provider identity, and returns legacy fields plus ordered per-provider outcomes—including off rows—without a provider query parameter.

**Why this priority**: Goal and Wanted 5 require one search across enabled providers with named outcomes; Wanted 10 requires additive compatibility (Q1, Q3, Q5, Q6).

**Independent Test**: API search with one-provider parity (including any baseline non-200 settled legacy outcome) and two stand-in providers; ordered providers array; legacy quota/availability projection; zero-candidate multi-provider 200; off row beside usable providers → 200; sole unusable **search** 409 with zero paid calls (download 409 + read-only precedence proven in US2/T015); missing required key sends zero requests; missing earlier ID with later available input / no formable input at all / stop-on-timeout-quota-failure / search-429 vs download exhaustion.

**Acceptance Scenarios**:

1. **Given** OpenSubtitles-only usable configuration, **When** `GET .../subtitles/search?language=` runs, **Then** legacy statuses, quota, availability, and candidate ordering match today’s single-provider behaviour, with additive `providerId` and `providers` present.
2. **Given** two enabled usable providers, **When** one aggregate search runs for one language, **Then** both are called concurrently, results merge in Q6 order after both settle, and each provider has an independent outcome row.
3. **Given** one provider times out, returns 503, or hits search 429, **When** siblings settle successfully, **Then** the failed origin is a named outcome only; siblings’ candidates remain; the whole call does not fail solely because of that provider.
4. **Given** a configured provider that is off or refused unsafe URL at startup, **When** another provider is usable, **Then** the search is 200 with an off/unusable row and no I/O to the skipped provider; readiness and existing-track routes stay available.
5. **Given** no enabled provider is usable (including sole OpenSubtitles missing/invalid), **When** search is requested, **Then** request-time validation returns 409 with zero paid provider calls (Q2; Constitution VII).
6. **Given** multiple usable providers and zero candidates after settlement, **When** search completes, **Then** response is 200 with empty candidates and settled provider outcomes (Q3).
7. **Given** a multi-provider configuration whose only config-usable provider settles timeout, search 429, failure, or no formable input with an off/refused sibling, **When** search completes, **Then** response is 200 with empty candidates and ordered named outcomes (not Q2 search 409; usability freeze).

---

### User Story 2 - Provider-bound Candidate IDs and explicit download (Priority: P1)

Viewer-selected download uses an opaque Candidate ID bound to movie, language, and one provider’s server-owned file reference. Download routes only to that provider; colliding reference strings across providers never cross; capacity pressure truncates to highest-ranked bindable rows without publishing self-evicted tokens.

**Why this priority**: Wanted 2 and 6 require bound identity and explicit acquisition; security ruling Q4/Q7.

**Independent Test**: Binder and property tests for provider-bound tokens; A/B reference-string isolation; cross-provider and cross-movie misuse blocked before paid call; over-capacity every-returned-token-resolves-immediately; earlier token evicted by a later search → existing stale response with zero paid calls; ranked issuance across providers and preferred languages at full capacity; expiry with injected time and deterministic randomness as its own case; explicit bounded/provider-bound property invariant; US2 scenarios 4–6 covered independently of expiry.

**Acceptance Scenarios**:

1. **Given** a Candidate ID issued from provider B, **When** download is requested, **Then** only B receives the paid call.
2. **Given** identical opaque reference strings from providers A and B, **When** each Candidate ID is resolved, **Then** downloads stay isolated to the bound provider.
3. **Given** cross-provider or cross-movie Candidate ID misuse, **When** download is attempted, **Then** no paid call is issued and the existing stale/invalid response applies.
4. **Given** one aggregate search that would exceed store capacity, **When** binding runs, **Then** only the highest-ranked bindable rows in Q6 order are returned; every returned token resolves immediately; no token published by that search is already evicted by that same search; truncation is logged with a safe count only.
5. **Given** an earlier Candidate ID evicted by a later search (independently of expiry), **When** download uses the old Candidate ID, **Then** the existing stale response applies with zero paid calls and a new free search is required.
6. **Given** ranked issuance across providers and preferred languages at full capacity, **When** binding runs with injected time and deterministic randomness, **Then** only highest-ranked bindable rows are issued; expiry under the injected clock is proven as its own case separate from scenario 5 (Q7; brief Test strategy).
7. **Given** known download-quota exhaustion on an otherwise valid candidate, **When** download is attempted, **Then** that download is blocked without inventing search-quota semantics; search 429 remains a distinct provider search outcome (Q5).
8. **Given** a Candidate ID bound to a provider whose acquisition configuration is missing/invalid or unusable while a sibling provider remains usable, **When** download is requested after ADR 0041 read-only evaluation, **Then** request-time validation returns the bound-provider acquisition **409** with zero paid calls to either provider; legacy HTTP status and ProblemDetails fields are unchanged and omit `providers` (Ledger S1; ADR 0052; FR-002).
9. **Given** read-only library policy or a stale/invalid Candidate ID, **When** download is attempted, **Then** the existing read-only or stale/invalid response applies with legacy status and ProblemDetails fields unchanged (no `providers` key) and zero paid provider calls (M4; FR-004).

---

### User Story 3 - Shared adapter contract suite and OpenSubtitles parity (Priority: P1)

Every adapter, including OpenSubtitles as the sole production adapter, satisfies the shared WireMock contract through its real typed HttpClient: declared search kinds, in-cap SRT download, oversize/non-SRT rejection, 429/timeout/5xx, cross-host redirect and HTTPS downgrade refusal, credentials only on HTTPS search origin, dual opt-in plain HTTP under the two Q4 conditions proven as **four independent typed-client gates** (permit credential-free + opt-in; refuse missing opt-in; refuse missing credential-free declaration despite opt-in; refuse credential/login configuration despite both opt-ins; OpenSubtitles HTTPS-only), response-independent destination trust with refusal before any disallowed contact, unsafe base URL refusal at startup with a redacted warning (provider name + `unsafe_base_url` only), and host/local-track continuity.

**Why this priority**: Wanted 3–4 and Done-when require the shared suite and OpenSubtitles assertions preserved (Q4; brief Test strategy).

**Independent Test**: Abstract shared suite against OpenSubtitles; existing `OpenSubtitlesSubtitleProviderTests` still pass; stand-in adapters prove isolation cases without becoming production adapters.

**Acceptance Scenarios**:

1. **Given** the shared contract suite, **When** OpenSubtitles runs it, **Then** every required case passes through the real typed client.
2. **Given** oversize or non-SRT download bodies, **When** download is attempted, **Then** rejection matches existing caps/validation.
3. **Given** cross-host redirect, HTTPS-to-HTTP downgrade, or a disallowed initial/redirect destination (trust independent of responses), **When** a hop would be followed or contacted, **Then** the destination is refused **before** any request to it and credentials never reach a download host.
4. **Given** an unsafe non-HTTPS base URL, **When** the host starts, **Then** that provider is unusable with a logged warning containing only the provider name and `unsafe_base_url` (no raw URL, credentials, or query); host and existing-track routes remain available; zero requests leave that provider.
5. **Given** plain HTTP exercised through the real typed client, **When** each dual-opt-in gate is proven independently, **Then** (a) credential-free declaration **plus** explicit opt-in is permitted, (b) missing opt-in is refused, (c) missing credential-free declaration despite opt-in is refused, and (d) credential/login configuration despite both opt-ins is refused; OpenSubtitles remains HTTPS-only (Q4; H1).

---

### User Story 4 - Named registration, spacing, and configuration honesty (Priority: P2)

Operators enable and order providers through typed options and `Subtitles:ProviderOrder`. Per-provider call spacing uses injected TimeProvider and host-lifetime instance-owned state on the Core-side lookup, honouring caller cancellation. Missing required keys or invalid timing classify a provider off with zero requests while host, existing tracks, and usable siblings remain available. Credential examples in docs use configuration keys or placeholders only.

**Why this priority**: Wanted 2–4 and Constitution VII require typed config without host failure (Q2, Q8; plan-challenge M3/S4).

**Independent Test**: Composition tests for ordered catalog; ProviderOrder unknown/duplicate warnings; concurrent spacing/cancellation on shared instance-owned state; missing-key and invalid-timing zero-request proofs with usable-sibling search 200; setup-doc tests for named settings and ranges.

**Acceptance Scenarios**:

1. **Given** `Subtitles:ProviderOrder` listing known names, **When** search fans out, **Then** enumeration order matches that list with omitted registered names appended in registration order.
2. **Given** unknown or duplicate names in ProviderOrder, **When** options bind, **Then** a safe warning is logged and the name is ignored (first occurrence wins); startup does not fail.
3. **Given** a missing required provider key, **When** search runs with another usable provider, **Then** the missing provider is off, sends zero requests, and does not 409 the call.
4. **Given** per-provider minimum gap settings, **When** concurrent searches hit the same provider, **Then** spacing is honoured via TimeProvider on host-lifetime instance-owned state and caller cancellation is not converted into a provider timeout.
5. **Given** invalid timing on one provider and a usable sibling, **When** the host runs and search is requested, **Then** the invalid provider is off with zero calls, host and existing-track routes stay available, and search settles 200 via the usable sibling (ValidateOnStart exception; plan-challenge M3).

---

### User Story 5 - Per-provider health without quota probes (Priority: P2)

Health reports per registered provider in configured order without making network calls or exposing secrets. Observed search outcomes update process-local bookkeeping shared with the search path.

**Why this priority**: Wanted 8; ADR 0041 forbids quota-consuming probes (Q9).

**Independent Test**: Health unobserved/reset; safe last settlement; cancellation exclusion; zero health-network calls; readiness continuity when a provider is off or failed.

**Acceptance Scenarios**:

1. **Given** no search has settled since process start, **When** health is read, **Then** each provider shows unobserved for last outcome (or equivalent safe code) with enabled/configured flags.
2. **Given** overlapping searches, **When** both complete, **Then** the last completed search wins in bookkeeping; caller cancellations are not recorded.
3. **Given** a provider off or failed, **When** readiness is evaluated, **Then** readiness is not Unhealthy; existing-track routes remain available; non-readiness checks are at most Degraded.
4. **Given** health is scraped, **When** inspected, **Then** zero provider network calls occurred and no raw errors or secrets appear.

---

### User Story 6 - Glossary, setup docs, and generated contracts (Priority: P3)

`CONTEXT.md` gains Subtitle Provider. Setup documentation covers new settings, valid ranges, and provisioning order. OpenAPI and generated TypeScript reflect additive C# DTOs while existing web tests stay green.

**Why this priority**: Wanted 9–10 and contract-chain Done-when; follows proven API behaviour (brief Approach).

**Independent Test**: Explicitly run `SubtitleSetupDocumentationTests`; OpenAPI/TypeScript drift plus wire-level and generated-schema coverage; full existing web test suite green.

**Acceptance Scenarios**:

1. **Given** the glossary update, **When** `CONTEXT.md` is read, **Then** Subtitle Provider is defined as in this spec without rewriting existing Movie/Library/sidecar vocabulary.
2. **Given** setup docs, **When** `SubtitleSetupDocumentationTests` run, **Then** named settings, valid ranges, provisioning order, and key/enablement behaviour are covered; credential examples use keys or placeholders only.
3. **Given** additive C# search DTOs and ProblemDetails extensions, **When** OpenAPI and `types.ts` are regenerated, **Then** drift checks pass and all existing web tests remain green.

---

### Edge Cases

- Timed-out or failed provider never fails the whole aggregate solely by itself (Q1).
- Caller cancellation is not reported as provider timeout (Q5).
- Missing earlier identifier with a later declared input available: skip the unavailable input and try the next supported available input (not terminal). No formable input at all → safe provider-local `failed`/`no_search_input` without further fallback (Q5; FR-006).
- Search 429 vs known download-quota exhaustion on otherwise valid candidates remain distinct (Q5; ADR 0041).
- Ranked truncation when one aggregate exceeds capacity across providers/preferred languages; every returned token resolves immediately; no self-evicted publish (Q7).
- Earlier token evicted by a later search → existing stale response with zero paid calls, independently of expiry (Q7).
- Same opaque reference string from two providers never collides (Q7).
- Plain HTTP requires credential-free adapter declaration **and** explicit typed opt-in (default disabled); credentialed/login-capable adapters refuse plain HTTP; OpenSubtitles HTTPS-only (Q4; H1).
- Trusted download destinations are independent of upstream responses; disallowed initial URL and redirect hops are refused before any contact (Q4; M1).
- Unsafe base URL at startup → provider unusable; warning carries only provider name + `unsafe_base_url`; host continues (Q2; M2).
- Request-time **search** 409 only when no enabled provider is usable (all-enabled check); **download** 409 when the **bound** provider is missing/invalid/unusable after read-only precedence, even if a sibling is usable, with zero paid calls to either provider; usability frozen at search acquisition configuration validation; ValidateOnStart exception keeps host up for invalid acquisition settings (Q2; Constitution VII; Ledger S1; ADR 0052; M3/S1).
- ProblemDetails extension key `providers` present on Q2 no-usable **search** 409 (classified off/unusable rows settled pre-I/O) and on legacy settled **search** errors; omitted on request-level errors that preempt settlement; **download** ProblemDetails unchanged without `providers` (Q3; M4; brief.md:69).
- Timeout, quota, failure, or no formable input on an otherwise usable multi-provider search settles named outcomes and does not become Q2 409 (usability freeze).
- Scorer, routes, request contract, and existing OpenSubtitles assertions retain current behaviour under compatibility rules (`brief.md` Frozen scope).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST keep `GET /movies/{id:int}/subtitles/search` as one aggregate call per language that concurrently fans out to every enabled usable Subtitle Provider, each independently timeout-bounded, merging after all settle, with no provider query parameter (Q1).
- **FR-002**: System MUST validate each provider locally at startup under the Constitution VII subtitle acquisition **ValidateOnStart exception** (acquisition settings never abort host startup); refuse unsafe non-HTTPS base URLs with a logged warning that carries **only** the stable provider name and `unsafe_base_url` (never raw URL, credentials, or query) and initialise that provider unusable (credential-free dual opt-in plain HTTP remains the separate Q4 path); classify missing required credentials **or invalid timing** as off; keep host, API, and existing-track routes running. **Search 409 predicate**: request-time acquisition configuration validation MUST return the sole missing/invalid configuration **search** 409 only when **no enabled provider is usable** (including sole OpenSubtitles missing/invalid), with zero paid provider calls, and MUST **freeze provider usability for that request** so subsequent timeout, quota, failure, or no formable input MUST NOT turn a configured-usable multi-provider search into that 409; a usable sibling MUST leave search configured so off/refused providers surface as named outcomes, not 409. **Download 409 predicate**: after ADR 0041 read-only precedence, download MUST validate **only the bound provider’s** acquisition configuration and MUST return the sole missing/invalid configuration **download** 409 when that bound provider is missing/invalid or unusable — **even when a sibling provider remains usable** — with zero paid calls to either provider; download ProblemDetails MUST stay the existing shape without `providers` (Q2; Ledger S1; ADR 0052; plan-challenge M2/M3; brief.md:69).
- **FR-003**: System MUST keep every legacy search-response field from the unchanged legacy contract (baseline parity proven in T010; recon-DEV-551 names DTO records only and MUST NOT be treated as an enumerated field/type/nullability table) and add candidate `providerId` plus ordered `providers` outcomes (including off rows) per Q3 and `contracts/search-response.md`; `state` and `reason` MUST be closed Enumerations (or equivalent) of the Patron-approved S5 safe codes at the Core subtitle outcome boundary, serialized by JSON `Name` in the approved API DTO scope, with exclusive reason predicates/precedence (disabled → `off`/`off`; else unsafe URL → `unusable`/`unsafe_base_url`; else missing/invalid required credentials/timeout/gap → `off`/`provider_not_configured`; request-time `no_search_input` only when usable and no formable input remains); legacy reason types and mappings MUST remain intact; legacy quota MUST project the first usable provider in configured order without summing; legacy download availability MUST be library policy AND at least one provider permits download; HTTP status MUST follow the disjoint precedence in `contracts/search-response.md`: request-level errors → existing; all unusable → 409; one-provider OpenSubtitles usable → legacy statuses/values (including any baseline non-200 settled outcome — do not invent); multi-provider with ≥1 usable at config-time freeze (one usable plus off/refused, multi-usable zero-candidate) → 200; classified `off`/`unusable` rows are settled as soon as classification completes (Q3; S2/S5; brief.md:69).
- **FR-004**: System MUST attach provider outcomes as ProblemDetails extension key `providers` (ruled providers shape) on **settled search** error responses only — including the Q2 no-usable **search** 409 (ordered classified off/unusable rows; quota null when unobserved) and legacy **search** errors with settled outcomes — and MUST omit the key on request-level validation/domain errors that preempt settlement, without changing those request-level statuses or the Q2 409 status itself; **download** MUST retain the existing ProblemDetails shape **without** `providers` for acquisition 409, read-only, and stale/invalid candidate errors (Q3; plan-challenge M4; brief.md:69).
- **FR-005**: Each typed client MUST enforce its own search/download destination allow-list from adapter declarations or explicit typed configuration **independently of upstream responses** on the initial URL and every redirect hop **before I/O**; credentials MUST go only to the configured HTTPS search origin; cross-host redirects and HTTPS-to-HTTP downgrades MUST be refused before follow; automatic following MUST be disabled with per-hop validation; plain HTTP MUST require both a credential-free adapter declaration **and** explicit typed operator opt-in (default disabled) and MUST be refused when the adapter uses credentials/login or credential/login configuration is present; OpenSubtitles MUST remain HTTPS-only; existing size/path/SRT checks MUST apply to every download (Q4; plan-challenge H1/M1).
- **FR-006**: Each provider MUST walk declared search inputs in file hash → IMDb ID → TMDb ID → title+year order; skip unavailable inputs (missing identifiers, including null TMDb ID pending DEV-539); report a safe provider-local `failed`/`no_search_input` outcome when no formable input exists; advance only after successful empty search; stop that provider on timeout, quota, or failure without further fallback; bind the whole branch under the per-provider timeout; map search 429 to that provider’s quota state without inventing download quota; block known download-quota exhaustion only on otherwise valid candidates; and propagate caller cancellation without converting it to provider timeout (Q5).
- **FR-007**: Multi-provider candidates MUST sort by Subtitle Match Percent desc, download count desc, configured provider order, then existing within-provider ties, with a stable merge; single-OpenSubtitles ordering MUST be unchanged (Q6; `ASSUMPTIONS.md` `[assumed]`).
- **FR-008**: Candidate ID binding MUST keep existing 1000-entry and five-minute defaults and expiry/eviction; MUST bind movie/language/provider with opaque server-owned file reference; MUST route download only to the bound provider; MUST prevent A/B reference-string collision; MUST apply ranked batch binding so one aggregate never publishes a self-evicted token; MUST return only highest-ranked bindable rows on over-capacity across providers and preferred languages with every returned token resolving immediately; MUST treat an earlier token evicted by a later search as the existing stale response with zero paid calls (independently of expiry); MUST prove expiry with injected time and deterministic randomness as its own case; MUST cover the bounded/provider-bound Candidate ID property invariant; MUST log truncation with a safe count only and MUST NOT add a truncation response field (Q7).
- **FR-009**: System MUST keep existing `ISubtitleProvider`; place one ordered concrete provider lookup on the Core side of that seam shared by search, download, SubtitleLookup, and health; that lookup MUST own process-local per-provider spacing and health-observation state as one instance-owned state object per provider for the host lifetime; keep named typed-client registration and composition outside Core; preserve OpenSubtitles section path; validate adapter-local `Enabled`, timeout, `MinimumRequestGap`, and credentials with documented valid ranges and no guessed safety defaults (Patron S5 names); honour `Subtitles:ProviderOrder` with registration-order default and omitted-name append; ignore unknown/duplicate ProviderOrder names with a safe warning (first wins) without startup failure; space per-provider calls via TimeProvider honouring cancellation (Q8; Constitution I; Ledger S4).
- **FR-010**: Existing health check MUST report per registered provider in configured order: enabled, configured, and last settled search outcome as safe code or unobserved; read the Core-side process-local observation state shared with search (one instance per provider for the host lifetime); last completed wins; omit cancellations; use TimeProvider for timestamps; make no network calls; never expose raw errors/secrets; never mark readiness Unhealthy for provider off/failure; at most Degraded on non-readiness; reset to unobserved on restart (Q9; Ledger S4).
- **FR-011**: System MUST add Subtitle Provider to `CONTEXT.md` and update subtitle setup documentation plus run `SubtitleSetupDocumentationTests` for named settings (including Patron-approved `Enabled` / `MinimumRequestGap` / dual HTTP opt-in), valid ranges, provisioning order, and key/enablement behaviour with credential examples by key or placeholder only (Q10; brief Approach; plan-challenge H1/S5).
- **FR-012**: System MUST regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` from C# contracts with wire-level and generated-schema coverage per `contracts/search-response.md`, and MUST preserve all existing web tests and existing OpenSubtitles assertions (Q3, Q10).
- **FR-013**: OpenSubtitles MUST remain the sole production adapter delivered by DEV-551 and MUST pass the shared WireMock adapter contract suite through its real typed HttpClient (`brief.md` Frozen scope; Test strategy).
- **FR-014**: ADR 0052 MUST be Accepted first in the delivery task sequence after separate Gate 1 (Spec PR may carry Proposed), retaining the before-deliverable-merge bar; ADR 0041 remains intact with amendment precedence documented in 0052 (Q10–Q11; brief.md:19).
- **FR-015**: Build MUST verify DEV-525 is Done before starting implementation; no runtime claim for DEV-525 is made by this ticket (`brief.md` Approach).
- **FR-016**: Planned edits MUST stay inside the brief Planned file scope; bounded adjacent tests and generated contract files are in scope; any other unnamed tracked file requires a fresh Patron ruling under §2.3 item 6 (Q10).

### Key Entities

- **Subtitle Provider**: Named configured upstream source with identity, enablement, credentials, destinations, timeout, spacing, and outcomes.
- **Subtitle Candidate**: Ranked remote result carrying public `providerId` and server-bound opaque file reference.
- **Candidate ID**: Opaque process-local token binding movie, language, provider, and file reference.
- **Provider outcome**: Per-provider `state`, safe `reason`, and nullable quota for one aggregate search settlement.
- **Provider health observation**: Process-local enabled/configured/last-outcome record shared with search, never a network probe.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: One-provider OpenSubtitles search matches legacy statuses, quota projection, availability, and ordering while exposing additive provider fields (US1).
- **SC-002**: Two stand-in providers in one aggregate search are both called concurrently; deterministic merge order holds; 503/quota/timeout affect only their origin (US1).
- **SC-003**: A Candidate ID from provider B never causes a paid call to provider A; A/B identical reference strings stay isolated; cross-movie misuse issues zero paid calls (US2).
- **SC-004**: OpenSubtitles passes the shared WireMock contract suite; existing `OpenSubtitlesSubtitleProviderTests` remain green (US3).
- **SC-005**: Missing required key on one provider sends zero requests to that provider; unsafe base URL leaves host and existing-track routes available; **search** 409 is proven when no enabled provider is usable; **download** 409 is proven for a missing/invalid/unusable **bound** provider even with a usable sibling, with zero paid calls to either provider and legacy ProblemDetails without `providers` (US1–US4; Constitution VII; Ledger S1).
- **SC-006**: Health reports per-provider data with zero health-network calls; readiness stays non-Unhealthy when a provider is off or failed (US5).
- **SC-007**: `SubtitleSetupDocumentationTests` pass; OpenAPI/TypeScript drift and wire-level/generated-schema coverage pass; all existing web tests remain green (US6).
- **SC-008**: Exact-head applicable gates, Size L ship-review split, and rebased-head gate verification are evidenced before merge under the Q11 closing bar; ADR 0052 is Accepted first in the delivery task sequence after Gate 1 with the before-deliverable-merge bar retained (Q11; brief.md:19 Gate expectations).

## Assumptions

- Taste ordering Q6 is logged `[assumed]` in `ASSUMPTIONS.md` and is authoritative for this ticket.
- DEV-525 Done will be verified before Build; this grill makes no runtime claim about DEV-525.
- Null TMDb ID remains skippable pending DEV-539 (Q5).
- DEV-552 owns picker UI and any extension of per-provider response fields beyond this ticket’s closed contract (Q3, Q10).
- Existing RequestTimeout validation (`> TimeSpan.Zero`) and binder defaults (1000 entries, five minutes) remain the documented ranges/defaults already ruled for those settings (Q7–Q8; existing options).
- Closed `state`/`reason` codes and per-adapter `Enabled` / `MinimumRequestGap` keys are Patron-approved under Ledger S5 (CONCLUSIONS.md Phase 2 adjudication; ASSUMPTIONS.md) and are recorded as ruled values throughout the draft artifacts.
