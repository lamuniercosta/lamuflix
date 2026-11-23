# Feature Specification: Subtitle download-host fail-closed guard

**Feature Branch**: `feature/566-spec`
**Created**: 2026-11-23
**Status**: gate1: provisional
**Input**: Ticket DEV-566 (bug; size M; parent DEV-490); grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; Maestri notes `DEV-566`, `recon-DEV-566` (HEAD `c7d5fb025b3793680a2fdf5f1785553becf9343`); `specs/DEV-566/ticket-text.txt`; `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Patron answers Q1–Q8; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox. Post-merge cluster secret regeneration and redeploy remain operator work after merge (`ticket-text.txt:35`; brief frozen scope item 3).

## Clarifications

### Session 2026-11-23

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (five deliverables, unit/integration regressions, live receipt, post-merge operator step), static `DownloadLinkHost` validation and 409 gating (Q2), process-local refused-host latch and 502→409 contract (Q3), readiness/sidecar boundary (Q4), live checker receipt (Q5), implement-skill trigger (Q6), out-of-scope boundaries (Q7), and implementation/verification sequence (Q8) are frozen in `brief.md` / `CONCLUSIONS.md`. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Gaps Quill cannot close from brief/recon go to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

Nothing visible as a new UI surface: this ticket changes fail-closed search/download behaviour, home-cluster settings generation, a developer live-check script, and implement-skill workflow text only. No SPA, toast, or on-screen error-copy work (`ticket-text.txt:46`; brief frozen scope). Existing subtitle search and download screens continue to consume the existing `409` / `502` problem contracts; with corrected shipped settings, downloadable candidates remain available instead of quota-wasting undownloadable ones.

## Scope

**In scope** (`brief.md` Frozen scope and review terms / Approach; Q1–Q8; `ticket-text.txt:20–54`):

1. **Static `DownloadLinkHost` validation and 409 gating**: Add `Subtitles:OpenSubtitles:DownloadLinkHost` (host name only; default `www.opensubtitles.com`). Validator requires membership in `DownloadAllowedHosts` using trim and case-insensitive host comparison consistent with `SubtitleRequestUriPolicy`. Invalid provider configuration yields `409` `provider_not_configured` on search and download before any provider call; search returns no candidate list. Preserve optional disabled-provider behaviour. Update loopback test fakes to set their configured download link host explicitly.
2. **Process-local refused-host latch**: When the OpenSubtitles download response contains an absolute HTTPS link whose host fails the allowlist, record the canonical refused host name only (never path, query, or token) in a thread-safe process-local singleton before the file GET; detecting request returns `502` `provider_unavailable` with zero file GETs; later searches and downloads in the same API process return `409` `provider_not_configured` before any provider call until settings are fixed and the API restarts. Preserve malformed/non-HTTPS link handling and redirect-disabled HTTP client behaviour. Consume the latch in the search/download acquisition guard only; readiness stays on static provider configuration.
3. **Home-cluster settings**: Update `scripts/home/New-HomeCluster.ps1` so generated settings allowlist `api.opensubtitles.com` and `www.opensubtitles.com` and set `DownloadLinkHost` to `www.opensubtitles.com`. Assert those values in `scripts/home/Test-NewHomeCluster.ps1`. Regeneration of the cluster secret and redeployment are post-merge operator work.
4. **Developer live check**: Add `scripts/Test-LiveSubtitleProvider.ps1` against shipped host settings. Read the developer key from user-secrets; sanitize all output including failures. Default free search. Explicit `-Download` submits at most one unretried quota-charging request, then checks HTTPS, equality with expected `DownloadLinkHost`, allowlist membership, and HTTP 200 file GET with redirects disabled. Never run by CI or automated tests. Completed acceptance requires a passing sanitized `-Download` receipt in the brief or PR; free search alone or unavailable check/credentials/quota leaves acceptance pending (workflow may record that pending evidence without completing acceptance).
5. **Implement-skill workflow**: Update both `.claude/skills/implement/SKILL.md` and `.agents/skills/implement/SKILL.md` to require and record the applicable developer live check when a ticket touches an external provider adapter, its options, or its deploy settings; name `scripts/Test-LiveSubtitleProvider.ps1` for OpenSubtitles; report pending evidence when check/credentials/quota are unavailable. That reporting obligation is distinct from DEV-566 completed acceptance (passing `-Download` receipt). Other provider check scripts remain out of this ticket.
6. **Verification**: Isolated unit and integration regressions named in the brief; home-cluster script test; applicable Size M repository gates with honest labels; separate opt-in live acceptance. Closing bar and two-round review cap per Q1.

**Out of scope**:

- UI toasts / on-screen error-copy changes (`ticket-text.txt:46`).
- External network calls from CI or automated unit/integration/e2e suites; suites stay network-isolated (`ticket-text.txt:45`).
- Other provider live-check scripts besides the OpenSubtitles script named above.
- New NuGet/npm dependency or package swap, new project, top-level folder, architectural layer, database schema change, public route / DTO field set / status-code shape change, `Features:LocalPlay`, or production `Process.Start` (`PRODUCT.md` §5; brief Q7).
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft.
- Performing the post-merge cluster secret regeneration / redeploy as pre-merge work.

**Precedence**: Ticket What changes / Done when (`ticket-text.txt:20–54`) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern plan boundaries. Only Keel declares PLAN FROZEN after independent analyze/reread. Gate 1 is a separate process.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Download link host**
Host-name-only setting `Subtitles:OpenSubtitles:DownloadLinkHost` naming the HTTPS host the provider is expected to serve downloadable files from; default `www.opensubtitles.com`; must be a member of `DownloadAllowedHosts` after trim and case-insensitive comparison.
_Avoid_: treating it as a full URL; storing paths, query strings, or tokens

**Refused-host latch**
Process-local, thread-safe memory of the first canonical refused download host name observed from an absolute HTTPS provider download link that fails allowlisting, retained until API restart after settings correction.
_Avoid_: persisting across process restart; retaining full URLs or tokens; changing readiness/health semantics

**Live provider receipt**
Sanitized developer-run evidence from `scripts/Test-LiveSubtitleProvider.ps1` against shipped settings. A **passing** `-Download` receipt (search status, safe host checks, redirect-disabled HTTP 200) completes live acceptance; free search alone or a pending-evidence statement when check/credentials/quota are unavailable leaves acceptance pending while still satisfying the workflow reporting obligation.
_Avoid_: CI/test execution; printing secrets; inventing a pass; treating pending evidence as completed acceptance

Consumed vocabulary (already in use; not redefined): `provider_not_configured`, `provider_unavailable`, `DownloadAllowedHosts`, `SubtitleAcquisitionSettings`, `SubtitleRequestUriPolicy`, subtitle search/download acquisition routes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fail-closed static download-host configuration (Priority: P1) 🎯 MVP

When OpenSubtitles settings name an expected download host that is missing from the allowlist, subtitle search and download refuse the request with `409` `provider_not_configured` before any provider call, so no undownloadable candidate list is shown.

**Why this priority**: Ticket root cause is a misaligned allowlist; static validation stops the undownloadable list and wasted quota at the configuration boundary (`ticket-text.txt:20–25,50`).

**Independent Test**: Unit validator/acquisition-guard coverage plus isolated integration endpoint tests prove `409` on both entry points, no search candidates, and zero upstream calls when `DownloadLinkHost` is absent from `DownloadAllowedHosts`; optional disabled-provider paths remain unchanged.

**Acceptance Scenarios**:

1. **Given** provider settings whose `DownloadAllowedHosts` does not contain `DownloadLinkHost` (after trim and case-insensitive host comparison), **When** subtitle search runs, **Then** the response is `409` `provider_not_configured`, no candidate list is returned, and zero provider calls are made.
2. **Given** the same invalid settings, **When** subtitle download runs, **Then** the response is `409` `provider_not_configured` before any provider call.
3. **Given** default `DownloadLinkHost` of `www.opensubtitles.com` and an allowlist that includes that host (including case/whitespace variants that normalize equal), **When** validation runs, **Then** the host membership check passes.
4. **Given** loopback/fake provider configurations used by tests, **When** those configurations are updated for this ticket, **Then** each explicitly sets its configured download link host to match its allowlisted loopback host.
5. **Given** optional disabled-provider / existing non-acquisition paths, **When** this validation is added, **Then** those behaviours remain as they work today (readiness still reflects static configuration only).

---

### User Story 2 - Runtime refused-host latch (Priority: P1)

If a live or simulated OpenSubtitles download response returns an absolute HTTPS link on a host outside the allowlist, the detecting request fails closed with `502` `provider_unavailable` without fetching the file, the refused host name is retained process-locally, and later searches and downloads in that API process fail with `409` `provider_not_configured` until restart after settings correction.

**Why this priority**: Provider drift must not keep spending download quota on links that cannot be fetched (`ticket-text.txt:26–30,51`).

**Independent Test**: Isolated provider/endpoint integration coverage proves initial `502`, hostname-only latch state, zero file GETs, later search and download both `409` with zero additional upstream calls, and recovery only in a fresh API process with corrected settings; unit coverage covers shared acquisition-guard outcomes.

**Acceptance Scenarios**:

1. **Given** a configured provider whose download API returns an absolute HTTPS link whose host fails allowlisting, **When** download runs, **Then** the detecting request returns `502` `provider_unavailable`, the canonical refused host name is recorded (host only), and zero file GETs occur.
2. **Given** that latch is set in the same API process, **When** a later search or download runs, **Then** each returns `409` `provider_not_configured` before any provider call.
3. **Given** a malformed or non-HTTPS download link, **When** download runs, **Then** existing refusal handling is preserved and the refused-host latch is not treated as a substitute for that path.
4. **Given** redirect-disabled typed HTTP client behaviour, **When** download link checking runs, **Then** redirects still cannot bypass host allowlisting.
5. **Given** corrected settings and a restarted API process, **When** search/download run again, **Then** the prior process latch is gone and acquisition uses the corrected static configuration.
6. **Given** the latch is set, **When** readiness/health is queried, **Then** readiness continues to report static provider configuration only (runtime latch does not change health semantics).

---

### User Story 3 - Home-cluster allowlist and expected host (Priority: P2)

Home-cluster generation writes OpenSubtitles settings that allowlist the API and `www.opensubtitles.com` hosts and set `DownloadLinkHost` to `www.opensubtitles.com`, proven by the home-cluster script test without deploying.

**Why this priority**: Shipped cluster settings caused the live failure; correcting generation and asserting it closes the configuration hole (`ticket-text.txt:31–35`).

**Independent Test**: `scripts/home/Test-NewHomeCluster.ps1` proves both allowlisted hosts and the explicit expected host in generated settings.

**Acceptance Scenarios**:

1. **Given** `scripts/home/New-HomeCluster.ps1` generation, **When** cluster settings are produced, **Then** `DownloadAllowedHosts` contains `api.opensubtitles.com` and `www.opensubtitles.com`, and `DownloadLinkHost` equals `www.opensubtitles.com`.
2. **Given** `scripts/home/Test-NewHomeCluster.ps1`, **When** it runs, **Then** it asserts those allowlist and expected-host values (and does not deploy).
3. **Given** owner merge of the delivery PR (after Gate 1 and implementation), **When** operators regenerate the cluster secret and redeploy, **Then** that work is recorded as a separate post-merge receipt (not required to green pre-merge gates).

---

### User Story 4 - Developer live check and implement-skill gate (Priority: P2)

Developers can run `scripts/Test-LiveSubtitleProvider.ps1` against shipped settings with a sanitized user-secrets key, and both implement-skill copies require recording the applicable live check (or specific pending evidence) when touching an external provider adapter, its options, or deploy settings. DEV-566 acceptance requires a passing sanitized `-Download` receipt.

**Why this priority**: Automated fakes cannot observe real download-link hosts; the ticket makes the live check and workflow requirement part of Done when (`ticket-text.txt:36–41,53`).

**Independent Test**: Script exists with free-search default and opt-in `-Download` behaviour described below, reading shipped host settings (`DownloadLinkHost`, `DownloadAllowedHosts`); both skill copies carry the same trigger, receipt, and pending-evidence reporting rules. **Completed** DEV-566 acceptance requires a passing sanitized `-Download` receipt against those shipped settings (search status, safe host checks, redirect-disabled HTTP 200). Free search alone, or a recorded pending-evidence statement when check/credentials/quota are unavailable, keeps acceptance **pending** — pending reporting fulfills the workflow obligation and is not a substitute for a passing live receipt (`ticket-text.txt:53`; brief Q5/Q6, lines 22, 31).

**Acceptance Scenarios**:

1. **Given** shipped host settings and a developer user-secrets key, **When** the script runs without `-Download`, **Then** it performs a free search and prints sanitized outcomes (never the key); free search alone leaves download acceptance pending.
2. **Given** the same setup with `-Download`, **When** the script runs and the provider path succeeds, **Then** it submits at most one unretried download request, checks HTTPS plus link host equality with `DownloadLinkHost` and allowlist membership, performs a redirect-disabled file GET expecting HTTP 200, sanitizes all output including failures, and that sanitized passing receipt is what completes live acceptance.
3. **Given** unavailable credentials, check, or quota, **When** the implement workflow records status, **Then** the brief or PR states the specific pending evidence rather than inventing a pass; that statement keeps DEV-566 acceptance pending and does not count as completed live acceptance.
4. **Given** a ticket that touches an external provider adapter, its options, or deploy settings, **When** implement agents follow `.claude` and `.agents` implement skills, **Then** both copies require the applicable live check and sanitized brief/PR receipt (or explicit pending-evidence wording), naming `scripts/Test-LiveSubtitleProvider.ps1` for OpenSubtitles — distinct from DEV-566's Done-when requirement of a passing receipt.
5. **Given** CI or automated test suites, **When** this ticket lands, **Then** they do not invoke the live script or call the real OpenSubtitles network.

---

### Edge Cases

- `DownloadLinkHost` differing from an allowlist entry only by surrounding whitespace or host-letter case → treated as a match (trim + case-insensitive).
- `DownloadLinkHost` missing from allowlist → static invalid configuration → `409` before provider calls on search and download.
- Absolute HTTPS download link on a non-allowlisted host → detecting `502`, latch host name only, zero file GETs; later acquisition `409`.
- Malformed or non-HTTPS download link → existing refusal path preserved; not redefined by this ticket.
- Redirect responses on file GET → still blocked by redirect-disabled client.
- Runtime latch present → readiness/health unchanged (static configuration only).
- SaveEnabled false / optional disabled provider → existing non-acquisition behaviour preserved.
- Fresh process after settings fix → latch absent; static validation governs.
- Live script failure paths → sanitized; never print key or tokenized links.
- Free search alone → download acceptance evidence remains pending until `-Download` passes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add host-name-only option `Subtitles:OpenSubtitles:DownloadLinkHost` with default `www.opensubtitles.com` on existing `SubtitleProviderOptions` (`brief.md` Q2; `ticket-text.txt:22–23`).
- **FR-002**: `SubtitleProviderOptionsValidator` MUST require `DownloadLinkHost` membership in `DownloadAllowedHosts` using trim and case-insensitive host comparison consistent with `SubtitleRequestUriPolicy` host matching (`brief.md` Q2; recon lines 4–6).
- **FR-003**: Invalid provider configuration (including failed `DownloadLinkHost` membership) MUST cause search and download to return `409` `provider_not_configured` via the existing acquisition guard before any provider call, with search returning no candidate list (`brief.md` Q2; `ticket-text.txt:24–25,50`).
- **FR-004**: Loopback and fake test provider configurations MUST set their download link host explicitly to an allowlisted host they use (`brief.md` Approach 1).
- **FR-005**: System MUST provide a thread-safe process-local singleton that stores at most one canonical refused download host name (never URL path, query, or token), shared by the transient provider and the acquisition guard (`brief.md` Q3).
- **FR-006**: When `OpenSubtitlesSubtitleProvider` receives an absolute HTTPS download link whose host fails `SubtitleRequestUriPolicy` allowlisting, it MUST set the latch with the host name before any file GET; the detecting request MUST remain `502` `provider_unavailable` with zero file GETs (`brief.md` Q3; recon lines 6–7).
- **FR-007**: While the latch is set in the same API process, later searches and downloads MUST return `409` `provider_not_configured` before any provider call; recovery requires corrected settings and API restart (`brief.md` Q3; `ticket-text.txt:26–30,51`).
- **FR-008**: Malformed and non-HTTPS download-link handling and redirect-disabled HTTP client behaviour MUST be preserved (`brief.md` Approach 2).
- **FR-009**: Runtime refusal state MUST be consumed by the search/download acquisition guard only; `SubtitleProviderHealthCheck` MUST continue to report static provider configuration readiness; optional disabled-provider and existing sidecar behaviour MUST be preserved (`brief.md` Q4).
- **FR-010**: `scripts/home/New-HomeCluster.ps1` MUST generate OpenSubtitles settings allowing `api.opensubtitles.com` and `www.opensubtitles.com` and setting `DownloadLinkHost` to `www.opensubtitles.com`; `scripts/home/Test-NewHomeCluster.ps1` MUST assert those values without deploying (`brief.md` Q7–Q8; `ticket-text.txt:31–34`).
- **FR-011**: `scripts/Test-LiveSubtitleProvider.ps1` MUST run developer-only outside CI/tests, read the key from user-secrets, sanitize all output including failures, default to free search, and on `-Download` perform at most one unretried download with HTTPS + expected-host + allowlist checks and redirect-disabled HTTP 200 file GET (`brief.md` Q5).
- **FR-012**: Both `.claude/skills/implement/SKILL.md` and `.agents/skills/implement/SKILL.md` MUST require the applicable developer live check and sanitized brief/PR receipt when a ticket touches an external provider adapter, its options, or deploy settings; name `scripts/Test-LiveSubtitleProvider.ps1` for OpenSubtitles; and MUST require recording specific pending evidence when check/credentials/quota are unavailable. That workflow reporting obligation is distinct from completed acceptance: DEV-566 Done when / brief Q5–Q6 require a **passing** sanitized `-Download` receipt against shipped settings (search status, safe host checks, redirect-disabled HTTP 200); free search alone or a pending-evidence statement leaves acceptance pending and MUST NOT be treated as a substitute for the passing receipt (`brief.md` Q6; `ticket-text.txt:53`; brief lines 22, 31).
- **FR-013**: Automated unit/integration suites MUST remain network-isolated and MUST NOT call the real OpenSubtitles API (`ticket-text.txt:45`; brief frozen scope).
- **FR-014**: Closing bar MUST block verified in-scope Critical, High, Medium, and Low findings that violate ticket acceptance, the fail-closed provider/secret boundary, or a mandatory gate; discretionary/out-of-scope items go to follow-up; review cap is two rounds (`brief.md` Q1).

### Key Entities

- **Download link host setting**: Host-name-only configuration value under `Subtitles:OpenSubtitles`.
- **Refused-host latch state**: Process-local canonical refused host name (or empty/unset).
- **Live provider receipt**: Sanitized search/download outcomes recorded for acceptance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With `DownloadLinkHost` absent from `DownloadAllowedHosts`, search returns `409` `provider_not_configured` with no candidate list and zero provider calls; download likewise returns `409` with zero provider calls.
- **SC-002**: On an absolute HTTPS download link whose host fails allowlisting, the detecting request returns `502` `provider_unavailable`, records host name only, performs zero file GETs, and later same-process search and download both return `409` with zero additional upstream calls.
- **SC-003**: After settings correction and API restart, the previous process latch no longer blocks acquisition; static validation alone governs.
- **SC-004**: Generated home-cluster OpenSubtitles settings include allowlisted `api.opensubtitles.com` and `www.opensubtitles.com` plus `DownloadLinkHost=www.opensubtitles.com`, proven by `Test-NewHomeCluster.ps1`.
- **SC-005**: Completed live acceptance records a sanitized **passing** developer `-Download` receipt against shipped settings (search status, safe host checks, redirect-disabled HTTP 200) in the brief or PR. If check, credentials, or quota are unavailable, the brief or PR MUST state the specific pending evidence (workflow obligation); that pending statement leaves acceptance pending and does not complete SC-005. Free search alone likewise leaves download acceptance pending.
- **SC-006**: Both implement-skill copies state the external-provider live-check trigger and OpenSubtitles script name.
- **SC-007**: Applicable Size M gates and isolated regressions complete with honest labels (`PASS` / `SKIPPED` / `N/A` / `Could not run` kept distinct); no invented green outcomes.

## Assumptions

- No Patron `[assumed]` taste items were recorded for this ticket; no `ASSUMPTIONS.md` entries are required for drafting.
- Existing `409` `provider_not_configured` and `502` `provider_unavailable` problem contracts remain the public outcomes; this ticket does not change routes or DTO field sets (`brief.md` Q7; `PRODUCT.md` §5).
- Post-merge cluster secret regeneration and redeploy are operator steps after owner merge, separate from the pre-merge live receipt (`brief.md` frozen scope item 3; `ticket-text.txt:35`).
