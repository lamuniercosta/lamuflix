# Feature Specification: Subtitles BSPlayer provider

**Feature Branch**: `feature/555-spec`

**Created**: 2026-11-28

**Status**: gate1: provisional

**Input**: Ticket DEV-555 (size M; no ui tag; parent DEV-490) as transcribed in `recon-DEV-555` lines 52–108; grill-closed `brief.md` (Q1–Q5 and plan-challenge D1–D15); `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q3 `[assumed]` server order); Maestri notes `DEV-555`, `recon-DEV-555`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Q1–Q5 / D1–D15; `PRODUCT.md` §5). Patron ruled no §2.3 care-list item is triggered (Q4) and no VII amendment is required for `Subtitles:BsPlayer` under the existing subtitle acquisition exception (Q1). Ordinary owner Gate 1 remains the Spec Kit PR merge path.

## Clarifications

### Session 2026-11-28

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Typed `Subtitles:BsPlayer` options and provider-local validation (Q1), plain-HTTP boundary and recorded-protocol prerequisites (Q2), one provider deadline with configured list order and conditional 8 s wait (Q3; `ASSUMPTIONS.md`), charter §2.3 file/dependency envelope (Q4), and review/delivery closing bar with live home-cluster evidence (Q5) are settled in `brief.md` and `CONCLUSIONS.md`. Plan-challenge D1–D15 (attempt budget, session scope, destination gate, cancellation, XML, hash divergence, inconclusive capture, deterministic time, telemetry/logs, fixtures, options shape, identifiers, home enablement, static state, validator rules, constitution recheck) override the draft wherever they differ. Wire details (session open/close sequence, whether the 8 s wait is required, hash algorithm identity, download-host shape) are implementation prerequisites established from recorded SOAP evidence, not inventable behaviour (Q2). No `[NEEDS CLARIFICATION]` markers inside this spec. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket is `ui:none` / no ui tag. It delivers a movies-only BSPlayer subtitle acquisition adapter behind the DEV-551 provider seam, typed options, recorded SOAP contract proofs, setup documentation, and a home-cluster live search/download receipt. Default plain `docker compose up` product UI is unchanged. No SPA controls, new pages, or browser-visible subtitle UI ship here. DEV-552 owns picker behaviour that can show other providers first when a protocol wait is required.

Operator / API outcomes (not default-Compose UI chrome):

- **Where**: Existing subtitle search and download flows that already call `ISubtitleProvider`; per-provider outcomes from the DEV-551 seam; setup docs / sample settings for `Subtitles:BsPlayer`.
- **What the operator or API caller sees or does**: With BSPlayer enabled and valid settings, movie subtitle search can return BSPlayer candidates matched by file hash (and IMDb id when the movie has one); download yields SRT through the existing publication path; when every configured BSPlayer server fails to answer within the provider deadline, BSPlayer reports `failed` with “BSPlayer is not answering” while other providers’ results still appear; with repository default `Enabled=false`, BSPlayer makes no outbound calls.
- **What it needs to show up**: Landed DEV-551 seam and shared contract suite; `Subtitles:BsPlayer` configured with `Servers`, `RequestTimeout`, `MaxResponseBytes`, `MaxDownloadBytes`, `DownloadAllowedHosts` (empty allowed), and `AllowPlainHttp: true`; scrubbed recorded SOAP fakes under the DEV-549 layout; operator-set untracked home settings for the live receipt; Gate 1 Spec Kit PR merge before Build.

## Scope

**In scope** (`brief.md` Frozen scope / Approach; Q1–Q5; D1–D15; ticket Wanted 1–6 and Done when at `recon-DEV-555:68–82`):

1. **Movies-only, keyless BSPlayer adapter** behind the existing `ISubtitleProvider` / DEV-551 provider contract. Declared search kind is file hash and size, including IMDb id when the movie has one. TV shows are out of scope. HTTPS is in scope only if BSPlayer offers it (ticket Out of scope / Q2).
2. **Typed acquisition settings** under `Subtitles:BsPlayer`: no new options record — bind a named instance of the existing `SubtitleProviderOptions` from `Subtitles:BsPlayer` (D11). Reuse `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, and DEV-551's `Enabled` and `AllowPlainHttp`. Add only `Servers` (`string[]` of absolute URLs) and `MaxResponseBytes` (`long`); both stay inert for OpenSubtitles. BSPlayer ignores `ApiKey`, `BaseUrl`, `UserAgent` and `DownloadLinkHost`. Keep OpenSubtitles validation byte-identical. Follow the download-cap precedence against the global `Subtitles:MaxSubtitleDownloadBytes` (cited at T001). If DEV-551's landed shape makes a named instance impossible, STOP for a ruling. Allowed hosts are always the union of `Servers` and `DownloadAllowedHosts` (D3); an empty `DownloadAllowedHosts` is valid (D15). All settings come from configuration with no guessed code defaults; missing or invalid values make the provider unusable with zero outbound calls. Repository default `Enabled=false` in `appsettings.json`. Home settings are untracked (Principle VII); T004 edits no home file; the tracked example carries the documented block; T017 lists the keys the operator sets for the live receipt (D13). Keep `Subtitles:OpenSubtitles` unchanged and HTTPS-only. Preserve DEV-551 request-time search 409 when no enabled provider is usable, and read-only download policy precedence (Q1).
3. **Plain HTTP and hardening** (Q2; D3; D5): Plain HTTP applies only to keyless BSPlayer SOAP and any BSPlayer download link it returns, opt-in via `AllowPlainHttp: true` only under `Subtitles:BsPlayer`. One shared refusal gate extending the existing host-refusal service applies the D3 destination predicate to every SOAP request and every download URL before contact. Disable redirects (any 3xx is a failed attempt). Bound XML via `MaxResponseBytes` and downloads via `MaxDownloadBytes` (with global-cap precedence). Build SOAP with `XElement`/`XmlWriter`; parse with the D5 `XmlReader` settings; abort over-bound bodies before parsing. Apply existing SRT validation before saving. Scrub anonymous session tokens from logs, telemetry, and recorded fixtures. Content is trusted no further than size/SRT payload checks. DNS-rebinding pinning is out of scope (Q2; H3(e)).
4. **Session, deadline, and server selection** (Q3; D1; D2; D4): Open a session, use the first server that answers, search, and close the session best-effort as the protocol requires. One configured `RequestTimeout` bounds probing, session open, search, any required protocol wait, and close. Try servers in **configured list order** while budget remains (`ASSUMPTIONS.md` `[assumed]`). At the start of each session-open attempt, compute `share = (remaining − W) / untried` (`W` = 8 s if T003 proves the wait is required, else 0); if `remaining ≤ W`, stop and report `failed`. An attempt is the session open only; once open there is no failover. Close gets no reserved budget — it runs only if time remains and never fails returned results (D1). The session token is local to each search call; the adapter is stateless apart from injected services (D2). Linked cancellation tokens: caller cancellation propagates as DEV-551 specifies (never `failed`); provider deadline → `failed` / “BSPlayer is not answering”; attempt-only → next server (D4). Add the 8-second wait only if recorded protocol evidence proves it is required; charge it to the same deadline; DEV-552 owns picker behaviour that shows other providers first if the wait is required. When no server answers, BSPlayer state is `failed` with “BSPlayer is not answering”; other providers return their results as usual. Injected `TimeProvider` for all deadlines/waits; timing tests use stub `HttpMessageHandler` + `FakeTimeProvider.Advance` with no wall-clock assertions; WireMock covers wire shape only (D8).
5. **Recorded protocol evidence prerequisites** (Q2; D7; Done when): Before the adapter relies on them, establish from a scrubbed recorded exchange: session open/close sequence; whether the 8 s wait is required; whether the hash LamuFlix sends matches what BSPlayer expects for a known file; download-link host shape. If the first capture for the generated file returns no candidates or no download link, use the D7 second-capture branch (home-library movie BSPlayer knows; commit only scrubbed SOAP bodies plus that hash and size; hash acceptance = a non-empty match for the LamuFlix-computed hash); if also inconclusive, STOP blocked for a ruling. If a recording shows a new destination or a redirect is required, seek an explicit ruling before relying on it. Use HolyArrs notes for orientation only; copy no AGPL source.
6. **Contract suite and tests**: Run the DEV-551 shared adapter contract suite against a fake built from recorded SOAP responses stored where DEV-549 puts fakes. Assert outgoing hash for a known file generated in the test only (D10) against the recording. Cover D1 attempt-budget tests, concurrent two-search session isolation (D2), D3 destination-table refusals, D4 caller-cancellation-during-probe, D5 over-bound/internal-entity XML, all-servers-down within the timeout with other provider results present; disabled/invalid settings; SRT validation; existing read paths and provider contract intact.
7. **Health and telemetry** (Q4; D9; D12): Config-state-only BSPlayer health registration with no live reachability probe; reuse existing and DEV-551 span/metric/attribute keys with no new attribute keys; server host and attempt number in logs only; logs carry movie id, attempt number and failure category; session tokens scrubbed (Principle VI). Identifiers per D12; a vendor-named `TelemetryConstants` entry is `blocked: structural` (PRODUCT §5 b).
8. **Setup documentation**: Subtitle setup docs list BSPlayer settings and state that the provider uses plain HTTP; covered by `SubtitleSetupDocumentationTests`. Tracked `docker/api.settings.example.json` carries the full `Subtitles:BsPlayer` example block when T001 confirms it as a setup-doc surface (D13). Include necessary appsettings and registration changes only; T004 edits no home file.
9. **File envelope** (Q4; ticket Where it lands; D6; D14): Infrastructure adapter and registration; Core subtitle options and validation (extend existing `SubtitleProviderOptions` / validator); integration contract/fake and supporting tests; subtitle setup docs and `SubtitleSetupDocumentationTests`; necessary appsettings/registration. If T003 proves hash differs, add a separately named functionally named routine in `MovieHash.cs`; existing routine stays byte-identical; existing OpenSubtitles hash tests unchanged (D6). New or changed production code has no `static readonly` and no mutable static state (D14). Existing packages and projects only. No new port, layer, schema, public route, DTO field, or status code. Local playback, process launch, and credential handling remain outside the adapter. Any additional file or dependency needs a separate Patron ruling.
10. **Live home-cluster evidence** (Q5; Done when; D13): Record one real home-cluster search and one download on the delivery PR. T017 lists the exact keys the operator sets in the untracked home settings. If BSPlayer is down that day, the PR says so and DEV-555 stays open until a real search has worked; recorded fixtures cover automated acceptance; the PR carries separate live evidence.

**Out of scope** (`brief.md` Frozen scope; ticket Out of scope; Q4–Q5; H3(e)):

- TV shows.
- Running BSPlayer over HTTPS unless the service offers it.
- DNS-rebinding pinning (Q2; recon line 66; H3(e) dismissed).
- New NuGet/npm dependency; new project, top-level folder, or architectural layer; new Core port; database schema; public API route/DTO/status shape changes.
- `Features:LocalPlay`, secrets hardcoding, `Process.Start`.
- Picker UI / “other providers first” presentation (DEV-552).
- Copying AGPL HolyArrs source.
- Inventing Gate 1 / plan freeze / runtime PASS; anything else is a follow-up issue, not a finding in this round.

**Precedence**: Ticket Wanted / Done when / Where it lands credited from `recon-DEV-555` are approved authority (`PRODUCT.md` §3). Brief Q1–Q5, D1–D15, `CONCLUSIONS.md`, and `ASSUMPTIONS.md` govern plan boundaries. Quill invents no decisions.

**Dependencies**: DEV-551 (Needs; seam, shared contract suite, provider-local validation / `AllowPlainHttp` contract) — cite as dependency, not as landed code at grill HEAD (Q1). DEV-549 fake layout for recorded SOAP fixtures. DEV-552 owns picker behaviour if the 8 s wait is required (Q3).

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**BSPlayer provider**
Movies-only, keyless subtitle acquisition adapter that searches by file hash (and IMDb id when present) over plain HTTP SOAP behind the existing `ISubtitleProvider` / DEV-551 seam.
_Avoid_: treating BSPlayer as a TV provider; treating plain HTTP as HTTPS; inventing credentials or accounts

**Provider search deadline**
The single configured `Subtitles:BsPlayer` `RequestTimeout` that bounds server probing, session open, search, any required protocol wait, and best-effort close for one provider search, with the D1 attempt-budget formula.
_Avoid_: separate unbounded waits outside that deadline; converting caller cancellation into provider timeout; reserving budget for close

**Configured server order**
Try configured BSPlayer servers in list order and use the first that opens a session within remaining budget (`ASSUMPTIONS.md` `[assumed]`).
_Avoid_: random HolyArrs-style order as the delivery rule

**BSPlayer not answering**
Provider-local `failed` outcome with reason text “BSPlayer is not answering” when no configured server answers within the deadline; siblings’ results remain.
_Avoid_: failing the whole aggregate solely because BSPlayer is down

**Recorded SOAP evidence**
Scrubbed recorded request/response fixtures that establish session sequencing, required delay, hash match, and download-host shape before adapter code relies on them.
_Avoid_: assuming wire behaviour from HolyArrs notes alone; storing anonymous session tokens in fixtures; committing media bytes

Consumed vocabulary (already in use; not redefined): `ISubtitleProvider`, DEV-551 provider contract / shared suite, DEV-549 fake placement, `MovieHash`, Subtitle Match Percent, Candidate ID, Local Subtitle Track, `AllowPlainHttp`, existing SRT validation / sidecar policy, `SubtitleProviderOptions`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - BSPlayer movie search by hash with isolated failure (Priority: P1) 🎯 MVP

With BSPlayer enabled and valid settings, a movie subtitle search asks BSPlayer by file hash (and IMDb id when present). The adapter opens a session on the first answering configured server, searches within one provider deadline under the D1 attempt budget, and closes best-effort. When every server is down, BSPlayer reports `failed` / “BSPlayer is not answering” while other providers’ results still return within the timeout.

**Why this priority**: Ticket Goal, Wanted 2–4, and Done when all-servers-down bar (Q3).

**Independent Test**: Shared contract suite and composition tests with recorded SOAP fakes, stub `HttpMessageHandler` timing tests, and FakeTimeProvider: successful hash(+IMDb) search; D1 ≥3-server hanging/refusing/answering path; concurrent two-search token isolation (D2); all-down → `failed` / “BSPlayer is not answering” with sibling results present within the deadline; caller cancellation during probe → cancellation not `failed` (D4); disabled/invalid settings → zero outbound calls.

**Acceptance Scenarios**:

1. **Given** BSPlayer enabled with a configured server list and timeout, **When** movie search runs with a computable file hash, **Then** BSPlayer searches by hash and size and includes IMDb id when the movie has one.
2. **Given** ≥3 configured servers where server 1 hangs, server 2 refuses and server 3 answers, **When** search runs, **Then** server 3 is reached and results return inside the deadline (D1).
3. **Given** every configured server fails to answer within the provider deadline, **When** search runs with another usable provider present, **Then** BSPlayer settles `failed` with “BSPlayer is not answering” and the other providers’ results still appear within the timeout.
4. **Given** `Enabled=false` or invalid BSPlayer settings, **When** search runs, **Then** BSPlayer makes zero outbound calls, the host keeps running, and DEV-551 409 applies only when no enabled provider is usable.
5. **Given** caller cancellation during a probe, **When** cancellation fires, **Then** BSPlayer yields cancellation, not `failed` and not the timeout text (D4).

---

### User Story 2 - Recorded SOAP contract, hash proof, and plain-HTTP safety (Priority: P1)

The DEV-551 contract suite passes against scrubbed recorded SOAP fakes. A known-file hash assertion proves the outgoing hash matches the recording (or D7 second-capture hash acceptance). Plain HTTP is constrained by the D3 destination predicate with redirects disabled, D5 XML bounds, SRT validation, and redacted session tokens.

**Why this priority**: Wanted 5 and Done when contract/hash bars; Q2 safety boundary.

**Independent Test**: Contract suite green on recorded fakes under DEV-549 layout; known-file hash match / D7 hash-acceptance proof; D3 destination-table refusals; D5 over-bound abort and internal-entity refusal; fixture/logs scrub of session tokens.

**Acceptance Scenarios**:

1. **Given** recorded scrubbed SOAP fixtures for BSPlayer, **When** the DEV-551 shared contract suite runs against the BSPlayer fake, **Then** the suite passes.
2. **Given** a known file generated in the test with a recorded BSPlayer exchange (or D7 second capture), **When** LamuFlix computes and sends the hash, **Then** the outgoing hash matches the recording / yields a non-empty match for the LamuFlix-computed hash.
3. **Given** a download or SOAP target that fails the D3 destination predicate, or a 3xx response, **When** the adapter would contact it, **Then** the destination is refused with zero contact to the disallowed host and no redirect follow.
4. **Given** an over-bound body or a DOCTYPE with an internal-entity expansion payload, or oversize/non-SRT download bodies, **When** parse/download runs, **Then** the over-bound body is aborted before parsing, the internal-entity payload is refused, and SRT checks apply.
5. **Given** recorded fixtures and runtime diagnostics, **When** inspected, **Then** anonymous session tokens are absent from fixtures, logs, and telemetry; logs do contain the movie id (D9).

---

### User Story 3 - Setup docs and config-state health (Priority: P2)

Operators can read BSPlayer settings (including plain HTTP) in subtitle setup documentation covered by `SubtitleSetupDocumentationTests`. Health reports config state only without probing BSPlayer.

**Why this priority**: Wanted 6; Principle VI health for a new external dependency (Q4).

**Independent Test**: `SubtitleSetupDocumentationTests` green for BSPlayer settings and plain-HTTP statement; health registration tests show config-state-only behaviour with no live probe and no session secrets; log-scrub test (D9).

**Acceptance Scenarios**:

1. **Given** setup docs and sample/default settings, **When** documentation tests run, **Then** BSPlayer settings (`Enabled`, `Servers`, `RequestTimeout`, `MaxResponseBytes`, `MaxDownloadBytes`, `DownloadAllowedHosts`, `AllowPlainHttp`) and the plain-HTTP transport note are covered; repository default remains off; tracked example carries the full block when confirmed as a setup-doc surface (D13).
2. **Given** BSPlayer registered for health, **When** health is scraped, **Then** no live BSPlayer network probe runs and no session tokens appear.

---

### User Story 4 - Ship gates then home-cluster live receipt (Priority: P2)

Size-M backend gates and review close under the brief bar; the delivery PR then carries a real home-cluster search and one download, or an explicit down-service note that keeps DEV-555 open until a real search works.

**Why this priority**: Done when live bar; Q5 closing bar.

**Independent Test**: Actual Size M gate verdicts on the delivery head; scrubbed live receipt or explicit down-service note on the PR; fixtures remain the automated acceptance path.

**Acceptance Scenarios**:

1. **Given** implementation complete, **When** Size M gates and Risk/Standards/Spec review run, **Then** Critical/High/Medium and accepted in-scope Low are fixed and re-verified within two rounds under Q5.
2. **Given** untracked home settings with the T017-listed BSPlayer keys set by the operator, **When** one movie is searched and one candidate downloaded on the home cluster, **Then** the delivery PR records the scrubbed receipt; if the service is down, the PR states that and the ticket stays open until a real search has worked.

---

### Edge Cases

- Non-responding servers skipped while deadline remains under the D1 attempt-budget formula; first successful session used; once open there is no failover; close skipped if no budget remains and never fails a search that already returned results (Q3; D1).
- Close with zero remaining budget is skipped and results return as success (D1).
- 8 s protocol wait added only when recorded evidence requires it; charged to the same deadline; picker-first behaviour is DEV-552 (Q3).
- Hash algorithm: if T003 proves a difference, add a separately named functionally named routine; existing routine stays byte-identical; existing OpenSubtitles hash tests unchanged (D6; Q4).
- Inconclusive first capture → D7 second capture; if also inconclusive, STOP blocked for a ruling (D7).
- Recording that requires a new destination or redirect → stop for explicit ruling before relying on it (Q2).
- Plain HTTP content trusted only as far as size/SRT checks (Q2).
- DNS-rebinding pinning is out of scope; the configured host name is the trust boundary (Q2; recon line 66; H3(e) dismissed).
- Invalid/missing BSPlayer settings → provider-local zero calls; host continues; search 409 only when no enabled provider is usable (Q1; D15).
- Caller cancellation during a probe → cancellation, not `failed` and not the timeout text (D4).
- Concurrent searches each carry their own session token; adapter is stateless (D2).
- TV media never searched through this adapter.
- HolyArrs AGPL sources are reference only — copy nothing.
- No `static readonly` / no mutable static state in new or changed production code (D14).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add a movies-only, keyless BSPlayer adapter implementing the existing `ISubtitleProvider` / DEV-551 provider contract, declaring file-hash(+size) search with IMDb id when the movie has one, and MUST NOT add TV support under this ticket (Wanted 2; Q4).
- **FR-002**: System MUST bind a named instance of the existing `SubtitleProviderOptions` from `Subtitles:BsPlayer` (no new options record) reusing `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, and DEV-551's `Enabled` and `AllowPlainHttp`, adding only `Servers` and `MaxResponseBytes`; MUST keep OpenSubtitles validation byte-identical; MUST follow the OpenSubtitles download-cap precedence against the global `Subtitles:MaxSubtitleDownloadBytes` (cited at T001); MUST STOP for a ruling if DEV-551's landed shape makes a named instance impossible; MUST treat allowed hosts as the union of `Servers` and `DownloadAllowedHosts` with empty `DownloadAllowedHosts` valid (D3; D15); MUST default `Enabled` off in repository `appsettings.json`; MUST NOT edit home settings in T004 (home settings are untracked under Principle VII; T017 lists operator keys for the live receipt) (D11; D13); MUST keep OpenSubtitles HTTPS-only; MUST take all listed fields from configuration with no guessed defaults; MUST make the provider unusable with zero outbound calls for missing/invalid BSPlayer settings while keeping the host running per the D15 validator rules; MUST preserve DEV-551 request-time search 409 when no enabled provider is usable and read-only download precedence (Q1; Q2; D11; D13; D15).
- **FR-003**: System MUST constrain plain HTTP to keyless BSPlayer SOAP and resulting BSPlayer downloads under the `AllowPlainHttp` opt-in; MUST apply the D3 destination predicate through one shared refusal gate that extends the existing host-refusal service to every SOAP request and every download URL before contact; MUST disable redirects (any 3xx is a failed attempt); MUST build SOAP with `XElement`/`XmlWriter` and parse with the D5 `XmlReader` settings, aborting over-bound bodies before parsing under `MaxResponseBytes`; MUST bound downloads under `MaxDownloadBytes` with global-cap precedence; MUST apply existing SRT validation before save; MUST scrub anonymous session tokens from logs, telemetry, and fixtures (Q2; D3; D5).
- **FR-004**: System MUST open a session, select the first answering server in configured list order within one provider deadline, search, and close best-effort; MUST apply the D1 attempt-budget formula (`share = (remaining − W) / untried`; attempt = session open only; no failover after open; close gets no reserved budget); MUST keep the session token local to each search call with a stateless adapter (D2); MUST classify cancellation per D4 (caller → propagate as DEV-551 specifies, never `failed`; provider deadline → `failed` / “BSPlayer is not answering”; attempt-only → next server); MUST add the 8 s wait only when recorded evidence requires it and charge it to the same deadline; MUST set BSPlayer to `failed` with “BSPlayer is not answering” when no server answers while other providers return results; MUST drive deadlines/waits through injected `TimeProvider` (Q3; D1; D2; D4; `ASSUMPTIONS.md`).
- **FR-005**: Before adapter reliance, Build MUST establish from scrubbed recorded SOAP evidence: session open/close sequence, whether the 8 s wait is required, known-file hash match, and download-host shape; MUST follow the D7 second-capture branch when the first capture is inconclusive and MUST STOP blocked for a ruling if the second capture is also inconclusive; MUST stop for an explicit ruling if a recording requires a new destination or redirect (Q2; D7).
- **FR-006**: System MUST run the DEV-551 shared contract suite against a recorded SOAP fake under the DEV-549 layout; MUST assert the outgoing hash for a test-generated known file against the recording (or D7 hash-acceptance proof); MUST prove all-servers-down within the timeout with sibling results present (Done when; Wanted 5; D7; D10).
- **FR-007**: System MUST register config-state-only BSPlayer health with no live probe; MUST reuse existing and DEV-551 provider spans, metrics and attribute keys with no new attribute keys; MUST put server host and attempt number in structured logs only (never as telemetry tags); MUST log movie id, attempt number and failure category without the session token (Q4; Principle VI; D9; D12).
- **FR-008**: System MUST document BSPlayer settings and plain HTTP in subtitle setup docs and cover them with `SubtitleSetupDocumentationTests`; MUST add the full `Subtitles:BsPlayer` example block to `docker/api.settings.example.json` when T001 confirms it as a setup-doc surface; MUST include only necessary appsettings and registration changes; MUST NOT edit home settings in T004 (Wanted 6; Q4; D13).
- **FR-009**: Planned edits MUST stay inside the Q4 / ticket Where-it-lands envelope (Infrastructure adapters/registration, Core subtitle options/validation, IntegrationTests contract/fake and supporting tests, subtitle setup docs / `SubtitleSetupDocumentationTests`, necessary appsettings/registration); if T003 proves hash differs, MUST add a separately named functionally named routine in `MovieHash.cs` while keeping the existing routine byte-identical and existing OpenSubtitles hash tests unchanged, adding an OpenSubtitles golden-value test only if none exists (cited at T001) (D6); new or changed production code MUST have no `static readonly` and no mutable static state (D14); any other file or new dependency requires a fresh Patron ruling (Q4).
- **FR-010**: After Size M gates/review under Q5, the delivery PR MUST carry a scrubbed home-cluster real search and one download receipt, or an explicit down-service note that keeps DEV-555 open until a real search has worked; T017 MUST list the exact keys the operator sets in the untracked home settings; recorded fixtures remain automated acceptance (Q5; D13).
- **FR-011**: System MUST use existing packages and projects only; MUST NOT change public routes, DTO fields, status codes, schema, LocalPlay, or Process.Start; MUST NOT copy AGPL HolyArrs source (Q4).
- **FR-012**: Build MUST treat DEV-551 (seam, shared suite, `AllowPlainHttp` / provider-local validation contract) and DEV-549 fake layout as prerequisites; cite DEV-551 as dependency, not as assumed landed code at grill HEAD (Q1; Needs).

### Key Entities

- **BSPlayer options**: Named instance of the existing `SubtitleProviderOptions` bound from `Subtitles:BsPlayer` (`Enabled`, `Servers`, `RequestTimeout`, `MaxResponseBytes`, `MaxDownloadBytes`, `DownloadAllowedHosts`, `AllowPlainHttp`). No new options record. Allowed hosts are the union of `Servers` and `DownloadAllowedHosts`; empty `DownloadAllowedHosts` is valid. All bounds come from configuration with no guessed code defaults; missing or invalid values make the provider unusable with zero calls per D15.
- **BSPlayer session**: Anonymous protocol session token local to each search call; opened on a responding server and closed best-effort within the provider deadline; adapter is stateless (D2).
- **Recorded SOAP fixture**: Scrubbed mapping/body under the DEV-549 fake layout used by the contract suite and hash proof; known-file hash proof uses a deterministic test-generated file only — no media or binary committed (D10).
- **Provider outcome**: DEV-551 per-provider state including BSPlayer `failed` / “BSPlayer is not answering”.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: DEV-551 shared contract suite passes for BSPlayer against recorded SOAP fakes (US2).
- **SC-002**: Known-file hash outgoing value matches the recorded BSPlayer exchange, or D7 second-capture evidence proves a non-empty match for the LamuFlix-computed hash (US2; D7).
- **SC-003**: All-servers-down search returns sibling provider results within the configured timeout and BSPlayer settles `failed` with “BSPlayer is not answering”; D1 ≥3-server hanging/refusing/answering path and zero-budget close success are proven (US1; D1).
- **SC-004**: Disabled/invalid BSPlayer settings produce zero BSPlayer outbound calls while the host and existing-track routes remain available; D3 destination refusals, D5 XML bounds, and SRT bounds are proven (US1–US2).
- **SC-005**: `SubtitleSetupDocumentationTests` pass for BSPlayer settings and plain-HTTP documentation; health is config-state-only with no live probe; log scrub shows no token and does show movie id (US3; D9).
- **SC-006**: Size M gates and Risk/Standards/Spec review close under Q5 (Critical/High/Medium and accepted in-scope Low fixed and re-verified within two rounds); delivery PR carries scrubbed live search+download or an explicit down-service note that keeps the ticket open (US4).

## Assumptions

- Configured list order for BSPlayer servers is logged `[assumed]` in `ASSUMPTIONS.md` and is authoritative for this ticket (Q3).
- DEV-551 lands with the provider seam, shared contract suite, and `AllowPlainHttp` / provider-local validation contract before Build relies on them (Needs; Q1).
- DEV-549 fake layout is available for recorded SOAP fixtures (Wanted 5).
- Session sequencing, 8 s wait necessity, hash algorithm identity, and download-host shape are established from recorded evidence before reliance (Q2) — not assumed from HolyArrs notes.
- DEV-552 owns picker “other providers first” behaviour if the wait is required (Q3).
- Quill adds no further taste assumptions.
