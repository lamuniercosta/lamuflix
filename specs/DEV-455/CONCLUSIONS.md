# DEV-455 Grill Conclusions

## Q1 — Media Chrome dependency
Question: Approve Media Chrome as the named npm player dependency within the existing web feature structure, with no additional player/state library? Recommendation: yes; DEV-455 explicitly names Media Chrome (recon-DEV-455:16), so this is ticket authority rather than owner escalation. Pin an exact stable version selected and verified at implementation pickup under repository policy, without inventing a version today. Cost: library bundle and maintenance; no new project or top-level architecture.

Patron answer: APPROVED. Media Chrome is the named npm player dependency within existing web feature structure; no additional player or state library. Basis: recon-DEV-455:16 and Charter §2.3 ticket text counts as decided; recon:27,55 establish the new dependency, deliberately approved under item 1. Exact stable version must be selected and verified at implementation pickup under repository policy. No new project or top-level architecture (recon:56). Bundle and maintenance cost accepted.

Implications: Quill must encode this bounded dependency approval and defer exact version selection to a verified pickup task. No owner checkbox arises from this ruling.

---

## Q2 — LocalPlay capability contract
Question: How should web discover whether Open in BS Player may be offered, since recon:34,44 shows no capability surface? Recommendation: minimal GET /api/playback/capabilities, C# PlaybackCapabilitiesDto with only localPlayEnabled:boolean, 200 application/json, effective Features:LocalPlay, no paths/player configuration. Hide secondary action during loading/failure, keep browser Play available. Charter items 4/5 approval; no schema change. Cost: narrow backend contract and generated web types.

Patron answer: APPROVED as proposed. Basis: recon:34,44 — ticket condition needs a web-visible flag; recon:16 and Charter §2.3 items 4/5 — this implements ticket text without owner escalation or schema change. Contract must flow C# DTO → OpenAPI → generated TypeScript, without hand-rolled duplicates or additional fields absent a new ruling.

Implications: Add exactly this capability contract; fail closed for the secondary action only.

---

## Q3 — LocalPlay coexistence
Question: Approve browser Play never invoking POST /play; explicit Open in BS Player alone invokes existing launch contract when capability=true, retaining server enforcement; preserve DEV-458 and launch error meanings (proposed 204 started, 403 disabled/hide/explain, 404 unavailable movie/file, 409 no source, 422 missing player/validation, other/network retry); no automatic fallback, Process.Start/config/bind/schema change. Basis recon:16,18-22,39-44. Cost: two independent actions and launch error UI.

Patron answer: APPROVED with refinement. Browser and launch actions remain explicit and independent (recon:16); server enforcement and DEV-458 hardening retained, no Process.Start/config/bind/schema change (recon:39-44). Do not reinterpret status codes: map 204/403/404/409/422/other/network exactly as the existing generated launch contract and current UI define them (recon:29-31,41), using generic retry only where that contract gives no specific meaning.

Implications: Quill must preserve the upstream launch contract; the proposed shorthand status descriptions are not a new contract.

---

## Q4 — Browser presentation
Question: Choose default presentation. Recommendation: in-page MovieDetailsView player, no route/modal; details Play opens and requests DEV-456 stream under gesture; card Play navigates to details and opens same region, attempts play and retains explicit control if blocked. Single video; focus labelled region; usable navigation. Basis recon:26,28-33. Cost: in-page space.

Patron answer: APPROVED as proposed. Recon:26 has detail navigation and no watch route; recon:28-33 supplies existing entry points. One active video, focus to labelled player region, navigation usable. Extra in-page space accepted rather than a second shell.

Implications: Both entry points converge on one player; autoplay denial is recoverable by explicit Play.

---

## Q5 — Accessible controls
Question: Approve Media Chrome play/pause, seek timeline, elapsed/duration, volume/mute, speed 0.5/1/1.5/2, fullscreen; Space/K toggle, Left/Right seek 10s clamped to seekable bounds, M mute, F fullscreen, Escape browser fullscreen exit; only player-owned focus, never editable fields/dialogs or global interception; labels, Tab access, visible focus; native media events authoritative, shared pointer/keyboard actions. Basis recon:16,33. Cost: focused keyboard/accessibility tests; no captions/playlist/casting/resume/transcoding.

Patron answer: APPROVED as proposed. Ticket requires bounded controls (recon:16), and existing Escape/arrow behavior requires local shortcut scope (recon:33). All conditions and exclusions approved.

Implications: Tests must cover focus scoping and seek bounds as well as positive actions.

---

## Q6 — Playback failure and lifecycle
Question: Approve loading/buffering without premature failure; native events define playing/paused/ended; autoplay denial retains explicit Play; terminal media/network/unsupported failure gives generic readable message, Retry and LocalPlay only if capability=true; no extension codec claims or invented opaque-error HTTP statuses. Retry same source from beginning, no automatic loops. Navigation/unmount/movie change pauses, clears source, releases resource, guards stale updates/overlap. No persistent progress/history writes. Basis recon:16,18,26,35. Cost: no retry resume.

Patron answer: APPROVED as proposed. Recon:16 authorises error states but not codec claims, invented statuses, fallback or resume; recon:26,35 require lifecycle cleanup for the new player. All conditions approved; retry starts at beginning, accepted cost no resume.

Implications: Separate recoverable autoplay denial and buffering from terminal failure; test teardown and stale work.

---

## Q7 — Compose evidence
Question: Choose sample/runtime acceptance. Recommendation: small real compatible sample from ignored runtime directory via LIBRARY_PATH; preserve tracked zero-byte samples and no unlicensed media commits. Record provenance/license, browser/version, codec/container, duration, Compose URL, controls, resumed seek/range through nginx, errors, LocalPlay-off hidden. Enabled launch separately on DEV-458-approved loopback only; no Compose/e2e false weakening. Missing browser/sample = Could not run, acceptance open. Basis recon:16,42,46-51. Cost provisioning/manual evidence.

Patron answer: APPROVED with refinement: verify runtime path is git-ignored before provisioning; otherwise use a location outside the tracked tree through LIBRARY_PATH. All evidence and isolation requirements approved. Basis recon:46-51 placeholder and mount/nginx facts, recon:42 and ADR 0023 preserve LocalPlay=false. Could not run never becomes pass.

Implications: Real media acceptance is a required distinct runtime receipt; mocked controls cannot substitute.

---

## Q8 — Dependency/pickup boundary
Question: Approve Phase A drafting against stated DEV-456/458 contracts, marked absent at base; Phase B waits for delivery and Gate 1 then recons generated contracts/main drift before tasks; consume stream, do not own range/resolver/security/transcoding/conversion/launcher-hardening/schema/history changes; no owner checkbox closure. Basis recon:18-22, Charter contract chain. Cost pickup reconciliation, no fake types.

Patron answer: APPROVED. Recon:18-22 shows dependencies at Spec/open PRs and stream absent; no hand-rolled substitutes. Charter §2.3 preserves owner checkboxes and bounded ownership. Q2 capability is DEV-455-owned. Pickup reconciles to delivered generated stream/launch types; contract drift requires new ruling, never silent adaptation.

Implications: Dependency and Gate 1 delivery are implementation prerequisites, not claims made by this grill.

---

## Q9 — Implementation boundary and UI input
Question: Approve existing movie-details/library modules, cohesive web/src/features/playback components/hooks/tests, existing API client/generated artifacts, manifests/lockfile, app-navigation only for player intent, capability DTO/endpoint in existing API and focused tests, existing web/e2e; no new project/top-level/state layer or unrelated deletions/rewrites; targeted edits forced by ticket AC and item6 approval where applicable. Palette owns design; await design.md for plan challenge. Basis recon:24-35,64-76, Charter §2.3. Cost integration edits.

Patron answer: APPROVED as proposed. Recon identifies forced integration points; feature folder is within existing architecture under items 2/6. Files outside boundary require cited forcing AC/gate or new Patron ruling. No unrelated tracked deletion/rewrite. Palette design and challenge prerequisite confirmed.

Implications: Quill plans bounded modules, not a new architecture; design decisions must respect the settled behavior.

---

## Q10 — Tasks and verification
Question: Approve order: pickup/dependency/Gate1 recon; capability DTO/tests; OpenAPI/generated types; exact dependency; accessible player/lifecycle; detail/card/LocalPlay integration; Vitest/RTL regressions; real-browser Compose last. Tests cover capability true/false/loading/failure, no launch POST from browser Play, launch regressions, media states/retry/teardown, controls/focus/editable isolation/bounds/autoplay denial; API boolean-only effective flag with no sensitive config; existing harness. Web gates and applicable actual-C#-diff gates, honest non-pass labels; jsdom cannot prove codecs/seek/fullscreen. Basis recon:16,34,42,64-76 and contract chain.

Patron answer: APPROVED. Existing regression base and absent player/e2e require these layers (recon:64-76); capability and browser separation and real evidence required (recon:16,34,42). Run every applicable web/backend gate; skipped/scope-empty/opt-out/could-not-run remain exact non-pass outcomes.

Implications: Quill must map every behavior to tasks and meaningful tests/runtime receipts.

---

## Q11 — Loop discipline
Question: Approve zero unresolved Critical/High/Medium in-scope findings, Low documented/dispositioned; frozen Q1-Q10 scope, anything else a follow-up issue, not a finding in this round; Charter filing policy otherwise noted/no ticket; two review/fix rounds then stop for fresh Patron/Conductor ruling without waived blockers or silent expansion; all three M axes, Palette before plan challenge, no runtime/owner claims from planning. Basis Charter §2.3 and loop discipline.

Patron answer: APPROVED as proposed. Charter filing policy applies (Critical/High, broken behavior, user-requested; otherwise noted, no ticket). Two-round cap requires a fresh ruling and never lowers closing bar. All three axes and prior Palette design required; no runtime or owner approval implied.

Implications: Brief must carry all loop terms verbatim in substance, including stop behavior.

---

## Q12 — Shared understanding and closure
Question: Confirm closure on Q1-Q11, no scope contradiction/constitution departure or owner checkbox effect, no new domain term/ADR for this Size M UI integration; existing meanings remain; Quill drafts, Palette before challenge, dependencies/runtime prerequisites. Recommend Play, Open in BS Player, Retry with descriptive accessible labels under Palette, without claiming every configured player is BS Player. Basis recon:16,26,43 and Q1-Q11.

Patron answer: CONFIRMED. Grill closed with shared understanding. No contradiction or constitution departure identified; no owner checkbox added or closed. No new domain term or ADR needed. Decisions belong in brief/CONCLUSIONS/ASSUMPTIONS; Quill drafts spec/plan/tasks; Palette before challenge; DEV-456/458 and runtime evidence remain prerequisites. Visible copy Play, Open in BS Player, Retry; descriptive accessible labels under Palette; do not claim every configured player is BS Player. Palette-refined wording must be logged as assumed taste.

Implications: Grill closed at 12 questions; ready for Quill drafting and Palette design, not plan freeze, Gate 1 or implementation clearance.

---

## C2 — Phase 2 analyze round 2: options resolution outside Core

Patron answer: CONFIRMED and AUTHORIZED. Options resolution stays outside Core; Quill repair round 2 is authorized to apply the C2 fix before clean analyze signoff.

- Basis — Principle I: constitution:288-290 permits Core only BCL, Microsoft.Extensions.Logging.Abstractions, System.Collections.Immutable, and Ardalis.SmartEnum; Microsoft.Extensions.Options/IOptions<FeatureOptions> in Core (plan.md:41,87,95; tasks.md:62) is outside that allowlist and must not be silently widened, and no new Core port, layer, ADR, or package is authorized under Q12.
- Basis — Principle II preserved: the sealed GetPlaybackCapabilitiesQuery/Handler with AddHandler and Tracing → Logging → Validation (constitution:318-321; plan.md:42) stays; the effective LocalPlay bool (bound outside Core per recon-DEV-455:39, FeatureOptions bound in ServiceDefaults) reaches the handler as a plain bool via the composition boundary or as a query/handler parameter, keeping the Q2-approved boolean-only route/DTO unchanged.
- Scope of authorization: this is the fresh Patron ruling required by Q11 and analysis-DEV-455-Keel round 2; Quill may edit only plan.md, tasks.md, corresponding test intent, and DRAFTING_RECEIPT.md to reconcile C2, then Keel must re-analyze clean before any gate1: provisional or plan challenge. The closing bar is not waived, no third round is pre-authorized, and no production code, API shape, schema, or owner checkbox is affected.

---

## Plan challenge adjudication (Sentry/Ledger/Compass, 19 findings)

Patron answer: APPROVED as the bounded disposition packet proposed by Keel. One plan-challenge amendment pass is authorized under Q11, distinct from the completed analyze repair rounds; no ticket contradiction, constitution departure, or owner checkbox arises, and gate1 remains not provisional until Keel independently verifies closure.

- Basis: Q1-Q12 and C2 in this file; recon-DEV-455:16-22,26-51,64-76,98-129; constitution Principles I, II, VI. Closing bar unchanged: zero unresolved in-scope Critical/High/Medium, Lows dispositioned below.
- Sentry: M1/M2 accept T001 pickup checks for media-element credential compatibility and URL lifetime/reuse, with STOP for a new ruling if header-only or expiring/single-use delivery proves incompatible with Q6 same-source Retry; signed URLs, auth redesign, and re-resolution are not preauthorized. M3 accept outgoing pause/source-clear before incoming attach plus stale-work tests T006/T011. M4 accept focused-control Space single-toggle/no-scroll and region-focus parity T006/T007. L1 ignore seeks until valid seekable bounds exist, never throw. L2 record safe media error code/network state in the existing diagnostic pattern, no URL/path/query/config dumps and no new telemetry library. L3 T016/T017 require a scanned/imported sample or existing scanned title. L4 desktop fullscreen acceptance per plan target; mobile-width layout is responsive only, no mobile-specific fullscreen fallback. L5 always show explicit local recovery when capability is true, hide it otherwise, never auto-launch, per design visibility matrix/Q6.
- Ledger: M1 factual correction accepted, no NEW Options dependency in Core; pre-existing wider allowlist drift (recon 98-108) is out of scope, noted with no ticket and no automatic Rigger filing under Q11 unless separately directed. M2 endpoint resolves IOptions outside Core and constructs GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled); handler returns named Core PlaybackCapabilitiesResult(bool LocalPlayEnabled); endpoint maps to the Api DTO, no Core-to-Api reference; composition-boundary alternative removed, AddHandler/decorators/C2 preserved. M3 Media Chrome deliberately adopted at the highest stable Node-24-compatible release published at least 14 days before selection, exact pin with no caret, Phase B registry evidence required; ADR 0023 is Playwright-scoped precedent, not universal policy; no second wrapper dependency. L2 T005 records npm audit --omit=dev outcome/advisory disposition or Could not run, never inferring PASS from Dependabot or the NuGet gate, with no gate script edits. L3 effective flag on/off is tested through real-host options and API JSON, handler tests supplementary.
- Compass: F1 no close control, closed state is initial only; replace closing/focus-return-on-close wording with the existing unmount/navigation/movie-change lifecycle. F2 Palette labels/copy and presentation in design sections 7-9 accepted as assumed taste and logged in ASSUMPTIONS.md, including region label, polite state live region, reduced motion, and responsive 16:9. F3 mark unobserved HandlerRegistration/ApiEndpoints paths as proposed and verify the registration surface at T001; T003 uses the identified existing registration point. F4 accept spec-local vocabulary restating Q2/Q12, with no glossary/CONTEXT/ADR expansion.
- Scope: Keel records this ruling in brief/CONCLUSIONS before Quill and Palette amend their owned drafts within this one pass, then Keel independently verifies closure before PLAN FROZEN. Anything beyond this packet needs a fresh Patron ruling.

## Freeze receipt — Phase 2 Step 6, 2026-11-07 (Keel)

**PLAN FROZEN — cleared to proceed to ordinary Gate 1.** All three reports were in hand before adjudication: Sentry 9 findings, Ledger 6, Compass 4 (19 total: 9 Medium, 10 Low). Prior analyze round 3 was clean before challenge (DEV-455 receipts 21–24); Palette design was available and read. Patron 94c06b57 authorized the bounded disposition packet and one amendment pass. Decisions were recorded in brief before Quill/Palette amended their respective files. Keel independently reread their completed work, rather than relying on completion reports.

| Findings | Verified closure evidence |
|---|---|
| Sentry M1/M2 | spec FR-020; plan:60,131; tasks:51 (T001). Future-delivery credential/lifetime/reuse checks and explicit STOP/new-ruling boundary; no invented auth/expiry facts or re-resolution authority. |
| Sentry M3 | spec FR-010; plan:127; tasks:74,75,88 (T006/T007/T011); design:110. Outgoing pause/clear precedes incoming attach; stale swap work ignored. |
| Sentry M4 / L1 | spec FR-005; plan:128; tasks:74–75; design:155–163. Space single toggle/scroll prevention/region parity; seek ignored until valid bounds. |
| Sentry L2 | spec FR-009; plan:46,127; tasks:75 (T007). Safe media-code/network-state diagnostics, no sensitive values or new dependency. |
| Sentry L3/L4 | spec FR-004/FR-018/FR-019; plan:25,130; tasks:111–112 (T016/T017); design:189–192. Scanned/imported title precondition and desktop fullscreen acceptance; mobile layout only. |
| Sentry L5 | spec FR-009/FR-015 and US4 AC3; plan:125; tasks:99–101; design:109,123,171–173. Deterministic gated local recovery, never auto-launch. |
| Ledger M1 | spec FR-012; plan:41; tasks:62. New code obeys allowlist; recon:98–108 existing drift acknowledged, out of scope, follow-up noted with no ticket. No constitution widening. |
| Ledger M2/L3 | spec FR-012; plan:21,41–42,63,86–93,123; tasks:61–62. Endpoint-owned options resolution, exact Core query/result, Api DTO mapping and decorated handler; real-host options/JSON proof. |
| Ledger M3/L2 | spec FR-003; plan:19,65,129; tasks:73 (T005). Explicit ticket-specific highest stable Node24-compatible exact pin, at least 14 days old, registry evidence, no wrapper; npm audit result/disposition or Could not run. |
| Ledger L1 / Compass F2 | ASSUMPTIONS.md exact labels/state copy/presentation entries; design §§7–9. All Palette taste refinements recorded with cited Patron approval. |
| Compass F1 | spec FR-010; tasks:75; design:19–22,62–65,102,177–179. No close affordance; initial-only idle; existing teardown lifecycle. |
| Compass F3 | spec FR-017; plan:27,91,94; tasks:51,62. Unobserved paths proposed and verified at pickup; existing registration point selected there. |
| Compass F4 | spec:45–63; plan:48. No defect after Patron confirmation: spec-local clarification, no glossary/CONTEXT/ADR expansion. |

**Closing bar:** zero unresolved in-scope Critical, High or Medium; all ten Low findings dispositioned. Eighteen findings accepted/clarified and closed in planning; Compass F4 confirmed no defect. This means the planned tasks address the findings, not that implementation tests or runtime checks have executed. The adjudication is published in canvas note `adjudication-DEV-455-Keel` (LamuFlix notes).

**Boundaries:** no owner structural checkbox added/closed, no ticket contradiction or constitution departure. This receipt supersedes pre-freeze draft handoff wording; it does not grant Patron provisional Gate 1 or user Gate 1 approval/merge. Next: Patron provisional Gate 1 → Rigger spec PR → user merge → T000 verified receipt → delivered DEV-456/458 and T001 recon/reanalysis before implementation. No dependency-delivery, production-gate, runtime-acceptance or merge-clearance claim. No production gates run for this document-only Phase A work. Q11 cap and T001 STOP conditions remain binding.

---

## Gate 1 status

- **Patron ruling:** `gate1: provisional` — `/speckit-analyze` is clean, the three-axis plan challenge is adjudicated (19 findings, 0 Critical/High/Medium remaining), Keel has frozen the plan in `specs/DEV-455/brief.md`, and `checklists/requirements.md` User-Visible Outcome items are ticked (Basis: task-pipeline Phase 2 step 7; Freeze receipt above; `specs/DEV-455/brief.md:4`).
- No owner structural checkbox is added or closed; ordinary owner Gate 1 on the spec PR remains pending, and Patron provisional Gate 1 is not owner approval or merge clearance.

## T000 Gate 1 merge receipt — 2026-11-09 (Keel)

**Gate 1 satisfied; T000 complete.** Spec PR #340 was merged at `1955de8a`, and `specs/DEV-455/` is on main. Authority: Conductor pickup instruction; note `DEV-455:5-7`, corroborated by the original intake `recon-DEV-455:5` read before its replacement with the T001 supplement. No independent hosted API query is claimed. Pickup worktree: `F:/Dev/LamuFlix.worktrees/DEV-455`, branch `feature/DEV-455`, recon HEAD `8df286993320a83ed9ebfacf5bdf40bc79ec3dae` (current supplemental `recon-DEV-455:3`).

This verified receipt supersedes the earlier pending owner Gate 1 wording in this file and the Phase A brief/spec/plan/tasks status statements. No owner structural checkbox existed or is closed by this receipt. Gate 1 approval does not establish dependency delivery, T001 completion, production gate results, runtime acceptance, or merge clearance.

## T001 reanalysis receipt — 2026-11-09 (Keel)

**Verdict: reanalysis recorded; pickup incomplete; implementation blocked.** Reviewed brief Q2/Q3/Q6/Q8/Q10 and handoff, spec FR-012/013/017/020, plan Phase B approach and ordering, and tasks T000-T004/dependency order against the intake recon and its current T001 supplement plus note DEV-455. The frozen artifacts agree on the capability contract and dependency gate; no contract redesign, scope change, or capability-only exception is justified by this recon.

| Check | Evidence and disposition |
|---|---|
| Gate 1 | PR #340 merge receipt above satisfies T000; mark only T000 complete. |
| DEV-456 stream delivery | Note DEV-455:21 and supplemental recon:4-5 evidence `GET /api/movies/{id:int}/stream`, registered under `/api`, plus generated OpenAPI `/api/movies/{id}/stream` and TypeScript `StreamMovie`. Unlike the historical Phase A absence, this contract is now present at pickup HEAD. No handwritten substitute is needed. |
| URL lifetime / same-source Retry | Supplemental recon:7 shows a movie-ID route without server-issued expiry, fresh per-request resolution/open, and range support. Reusing the same URL is compatible while the source remains available and authorized. This is server-route evidence, not proof that a future client retains/retries it correctly. Q6 and its implementation tests remain required. |
| Media-element credentials | Supplemental recon:6 explicitly leaves browser credential mode unestablished. Absence of endpoint auth parameters/decorators does not prove broader auth policy or media-element compatibility. No header-only incompatibility is established either. Keep the T001 compatibility check open; no auth redesign, signed URLs, or re-resolution is authorized. |
| DEV-458 launch delivery | Supplemental recon:8 shows remote branch tip `acca3d62` is not an ancestor of local `origin/main` at pickup HEAD; named hardening files are absent there. Note DEV-455:21 identifies unmerged delivery PR #352. Spec text and remote implementation do not establish delivery on main; no live hosted PR-status query is claimed. Landed launch behavior must be reconfirmed after merge. |
| Capability sequencing | T002 depends explicitly on T001; tasks Phase 1 blocks production edits and requires both deliveries; brief Q8 and plan Phase B retain this gate. T002-T004 are technically a separate boolean capability slice but cannot proceed under the approved ordering. Do not infer an early-start exception from their lack of stream consumption. |
| Baseline | No Gauge pre-edit baseline receipt is supplied in the reviewed notes. Do not claim baseline completion or mark the composite T001 complete. |
| Analysis tooling | The required SpecKit prerequisite command was attempted once during the initial pickup and rejected `feature/DEV-455` as nonnumeric. Scripted analysis: **Could not run**, not PASS. This receipt is the Conductor-authorized bounded manual cross-artifact reanalysis; no script success is claimed. |

No demonstrated drift requires changing the frozen capability DTO/query/result, generated-contract chain, player behavior, test strategy, or task ordering. The task status/receipt now reflects delivery evidence and retains the existing blockers. This bounded review does not claim a new exhaustive clean-analysis or coverage percentage; unresolved pickup evidence prevents completion.

**Recommended chain status:** `blocked: dependency — DEV-458 PR #352 delivery unmerged; T001 pickup incomplete`. Phase 0 complete; Phase 1 reanalysis recorded but T001 unchecked; Phase 2 T002-T004 and subsequent implementation remain blocked. After user merge of DEV-458, Wisp supplies fresh landed-contract/drift and media-element credential evidence, Gauge records baseline, and Keel closes T001 only when its checkpoint is met. No production gates or runtime checks were run for this documentation-only receipt.
