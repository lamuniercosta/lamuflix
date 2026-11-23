# DEV-566 Grill Conclusions

## Q1. Review closing bar and round cap

**Question.** [from Keel] /grill-with-docs DEV-566 — Q1 of at most 12. Confirm the review closing bar? I recommend blocking all verified in-scope Critical, High, Medium and Low findings that violate the ticket acceptance criteria, fail-closed provider or secret boundary, or a mandatory gate. Route discretionary improvements and issues beyond the ticket to follow-up. This lets a concrete regression block even at Low severity and keeps the review focused on delivery. The round cap is two; I will record each ruling in specs/DEV-566/CONCLUSIONS.md. Please confirm the bar or give your replacement, with basis.

**Answer.** [from Patron] DEV-566 Q1 confirmed: block all verified in-scope Critical, High, Medium, and Low findings that violate ticket acceptance criteria (specs/DEV-566/ticket-text.txt:48-54), the fail-closed provider/secret boundary (ticket-text.txt:20-31,37), or a mandatory gate (specs/PRODUCT.md:12); route out-of-scope and discretionary items to follow-up, with a two-round review cap.

**Implication.** Each blocking finding needs a concrete, verified failure against the ticket, the provider or secret boundary, or a mandatory gate. The review loop permits two rounds.

---

## Q2. Static configuration

**Question.** [from Keel] DEV-566 Q2 of at most 12. Confirm the static configuration boundary? I recommend treating DownloadLinkHost as a host name only, normalizing it with the same trim and case-insensitive host comparison as the existing URI policy, and requiring membership in DownloadAllowedHosts. Fail closed through the existing configuration guard on search and download with 409 provider_not_configured and zero provider calls for invalid configuration. This preserves the existing disabled-provider behavior and needs loopback test configurations to set the host explicitly. Basis: ticket-text.txt lines 22–25 and recon lines 4–6.

**Answer.** [from Patron] DEV-566 Q2 confirmed: treat Subtitles:OpenSubtitles:DownloadLinkHost (default www.opensubtitles.com) as a host name only, normalize with trim and case-insensitive comparison matching SubtitleRequestUriPolicy.cs:21-32, require membership in DownloadAllowedHosts in SubtitleProviderOptionsValidator.cs:40-54, and fail closed via SubtitleAcquisitionSettings.cs:10-23 with 409 provider_not_configured and zero provider calls (specs/DEV-566/ticket-text.txt:21-25,50).

**Implication.** Existing fake configurations can name their loopback host explicitly. Invalid configuration is handled before any provider call.

---

## Q3. Runtime refusal contract

**Question.** [from Keel] DEV-566 Q3 of at most 12. Confirm the runtime refusal contract? I recommend a thread-safe process-local singleton keyed to OpenSubtitles with a single canonical first refused host. Trip it only for an absolute HTTPS link returned by the download API whose host fails the allowlist, before the file GET. Keep the detecting request on existing 502 provider_unavailable, then reject later searches and direct downloads with 409 provider_not_configured before any provider call until restart. Existing malformed or non-HTTPS link failures stay on their refusal path. This bounds retained data and protects stale download candidates. Basis: ticket-text.txt lines 26–30 and recon lines 5–9. Can Patron confirm the trigger, lifetime, and public outcomes as one refusal contract?

**Answer.** [from Patron] DEV-566 Q3 confirmed: use a thread-safe process-local singleton storing only the canonical refused host name (never token/path/query, ticket-text.txt:27), tripped when OpenSubtitlesSubtitleProvider.cs:84-92 receives an absolute HTTPS download link whose host fails SubtitleRequestUriPolicy.cs:6-32 before the file GET; keep the detecting request on 502 provider_unavailable (ticket-text.txt:13) and fail subsequent searches and downloads closed with 409 provider_not_configured before any provider call until restart (ticket-text.txt:26-30,51).

**Implication.** Share the state across provider and endpoint request instances. Preserve the URI policy and capture only the host component.

---

## Q4. Readiness and sidecar boundary

**Question.** [from Keel] DEV-566 Q4 of at most 12. Confirm the runtime health boundary? I recommend consuming the refusal state in the existing search/download guard only and leaving SubtitleProviderHealthCheck on its existing static-configuration readiness semantics. The runtime trip is provider-specific and an API readiness change could affect unrelated traffic. Treat the optional provider disabled state and existing sidecar paths as they work today. Basis: ticket-text.txt lines 22–30 and recon lines 5–6.

**Answer.** [from Patron] DEV-566 Q4 confirmed: consume runtime refusal state in the search/download acquisition guard (SubtitleAcquisitionSettings.cs:10-23) only, keeping SubtitleProviderHealthCheck.cs:39-46 on static-configuration readiness and preserving optional disabled-provider and sidecar behavior (specs/DEV-566/ticket-text.txt:21-30,50-51).

**Implication.** Static invalid configuration still flows through the existing health check. A runtime refusal affects acquisition requests only.

---

## Q5. Live acceptance receipt

**Question.** [from Keel] DEV-566 Q5 of at most 12. Confirm the live acceptance receipt? I recommend developer-only execution with the shipped host settings, reading the key from user-secrets and sanitizing output including failures. Default is a free search. The required DEV-566 receipt uses explicit -Download, submits at most one quota-charging request with no retry, validates HTTPS plus exact expected and allowed host before GET, disables redirects, and requires GET 200. Record mode, sanitized conditions/outcomes, and status in the brief or PR. Credential or quota unavailability leaves live acceptance pending. It is outside CI and tests. Basis: ticket-text.txt lines 36–41 and 53.

**Answer.** [from Patron] DEV-566 Q5 confirmed: scripts/Test-LiveSubtitleProvider.ps1 runs dev-only outside CI/tests using shipped settings and user-secrets key (sanitized on all paths), defaulting to free search and with -Download running at most one unretried download checking HTTPS, link host == DownloadLinkHost, allowlist membership, and 200 file GET with redirects disabled, with the sanitized receipt recorded in the brief or PR (specs/DEV-566/ticket-text.txt:36-41,53).

**Implication.** The free mode validates search. The explicit download mode supplies the acceptance evidence for the real link and file GET.

---

## Q6. Implement skill trigger and receipt

**Question.** [from Keel] DEV-566 Q6 of at most 12. Confirm the implement-skill workflow boundary? I recommend that the .claude and .agents copies require a developer-only live check and sanitized brief or PR receipt whenever a ticket touches an external provider adapter, its options, or deploy settings. Use the applicable provider check, with scripts/Test-LiveSubtitleProvider.ps1 for OpenSubtitles. Keep any other provider check implementation for its own ticket. For a missing check, credentials, or quota, report the specific pending evidence and require the live receipt for DEV-566 acceptance. Implication: the workflow is general and this ticket adds only its named checker. Basis: ticket-text.txt lines 36–41,53.

**Answer.** [from Patron] DEV-566 Q6 confirmed: update both .claude and .agents implement skill copies to require a dev-only live check and sanitized brief/PR receipt when a ticket touches an external provider adapter, its options, or deploy settings, naming scripts/Test-LiveSubtitleProvider.ps1 for OpenSubtitles, reporting pending evidence when check/credentials/quota are unavailable, and requiring a passing live receipt for DEV-566 acceptance while leaving other provider check scripts to their own tickets (specs/DEV-566/ticket-text.txt:36-41,53).

**Implication.** Both skill copies get the same trigger and evidence requirements. This ticket delivers the OpenSubtitles checker.

---

## Q7. Frozen scope and post-merge operation

**Question.** [from Keel] DEV-566 Q7 of at most 12. Confirm the scope freeze for the drafting brief? I recommend the five ticket items as the complete scope: host validation and 409 gating, process-local refused-host handling, home-cluster settings and script assertions, the OpenSubtitles live checker, and the two implement-skill updates. Include unit/integration regressions and the live receipt. Home-cluster secret regeneration and redeploy are post-merge operator work. UI toasts, external calls from tests or CI, other provider scripts, dependency/schema changes and public route/DTO changes stay outside this delivery. Anything else is a follow-up issue, not a finding in this round. Basis: ticket-text.txt lines 20–54.

**Answer.** [from Patron] DEV-566 Q7 confirmed: freeze the drafting brief scope to the five ticket deliverables (DownloadLinkHost validation and 409 gating, process-local refused-host latch, home-cluster settings plus Test-NewHomeCluster.ps1, scripts/Test-LiveSubtitleProvider.ps1, and .claude/.agents implement skill updates) with unit/integration regression tests and the live check receipt, keeping cluster secret regeneration/redeploy post-merge (ticket-text.txt:35) and routing UI toasts, external calls in tests/CI, other provider scripts, and dependency/schema/route/DTO changes out of scope (specs/DEV-566/ticket-text.txt:20-54).

**Implication.** The drafting scope is limited to the five confirmed items and their verification. Operator receipt follows the owner merge.

---

## Q8. Implementation and verification sequence

**Question.** [from Keel] DEV-566 Q8 of at most 12. Confirm the implementation and verification sequence to close the grill? I recommend options and fake configurations first, then shared runtime state and adapter/endpoint tests, home-cluster generator and assertions, live checker and both workflow updates, then pre-merge verification using isolated unit/integration and home-cluster tests, applicable Size M repository gates, and the separate opt-in live receipt against the shipped host settings. Prove zero upstream calls for 409, zero file GET on refused links, cross-request state, and recovery with fixed settings in a fresh process. This gives Quill concrete acceptance tasks while keeping the owner Gate 1 and post-merge operator step distinct. Basis: ticket-text.txt lines 20–54, recon lines 4–12, and agent-pipeline Loop Discipline.

**Answer.** [from Patron] DEV-566 Q8 confirmed: sequence options/validator and test fake host configuration first, then shared runtime refusal state with adapter/endpoint tests (proving zero upstream calls on 409, zero file GET on refused links, cross-request latch, and fresh-process recovery), then New-HomeCluster.ps1 and Test-NewHomeCluster.ps1, then scripts/Test-LiveSubtitleProvider.ps1 and both implement-skill copies, followed by isolated tests, Size M repository gates, and the opt-in live receipt while keeping Gate 1 and post-merge cluster secret redeploy distinct (specs/DEV-566/ticket-text.txt:20-54, recon-DEV-566:4-12).

**Implication.** Quill can make each verification outcome an explicit task or acceptance scenario. Owner Gate 1 still precedes implementation.

---

## Gate 1 Ruling

**Verdict.** `gate1: provisional` approved; ready for spec PR commit and opening with no owner checkboxes required.

- Clean `/speckit-analyze` (0 open findings) and PLAN FROZEN adjudication of all 22 challenge findings (`specs/DEV-566/PLAN_ADJUDICATION.md:3-7,46`) align with Q1–Q8 (`specs/DEV-566/CONCLUSIONS.md:3-79`, `specs/DEV-566/brief.md:3-32`).
- `specs/DEV-566/checklists/requirements.md:32-37` has all User-Visible Outcome items ticked and `specs/DEV-566/spec.md:5,18-20` carries `**Status**: gate1: provisional` and the non-UI rationale.
- No ticket contradiction or constitution departure exists (`specs/DEV-566/spec.md:8-10`, `specs/DEV-566/plan.md:27-42`), so zero owner checkboxes are needed on the spec PR.

