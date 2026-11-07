# Feature Specification: Browser playback default with Media Chrome

**Feature Branch**: `feature/455-spec`
**Created**: 2026-11-07
**Status**: gate1: provisional
**Input**: Ticket DEV-455 (parent DEV-100); grill-closed `brief.md` (Q1–Q12); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-455`, `recon-DEV-455`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependencies: DEV-456, DEV-458.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Q12; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending. Patron approval is not owner approval. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-07

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity? → A: Behaviour for Media Chrome dependency, capability contract, coexistence with local launch, in-page presentation, controls/keyboard, lifecycle/errors, Compose evidence, dependency/pickup boundary, module boundary, verification order, and loop bar are frozen in brief Q1–Q12. No residual drafting `needs decision`. No `[NEEDS CLARIFICATION]` markers. Palette `design.md` remains a plan-challenge prerequisite (Q9/Q12); Quill does not invent visual design.

## Scope

**In scope** (`brief.md` Outcome and frozen scope; Q1–Q12; ticket Scope & Technical Design):

1. **Browser-default Play**: Both existing Play entry points (movie details and library card) default to browser playback that consumes the DEV-456 stream contract once delivered. Browser Play never invokes `POST /api/movies/{id}/play`.
2. **Media Chrome player**: Add Media Chrome as the named npm player dependency inside existing `web/` feature structure (no second player, state library, or second wrapper dependency). Exact pin at pickup follows the ticket-specific adoption of Playwright-scoped ADR 0023 precedent: highest stable Node-24-compatible release published at least 14 days before selection, exact version with no caret, Phase B registry evidence recorded — never invent a version in this draft; this is not a pre-existing general npm policy.
3. **In-page presentation**: One labelled player region inside `MovieDetailsView` (no new watch route or modal). Details Play opens the player and requests the stream under user gesture. Card Play navigates to details, opens the same player, attempts playback, and retains an explicit Play control when the browser blocks autoplay. One active video; focus moves to the labelled region; navigation remains usable.
4. **Accessible controls**: Media Chrome play/pause, seek timeline, elapsed/duration, volume/mute, speed 0.5 / 1 / 1.5 / 2, fullscreen (desktop-browser acceptance; responsive mobile widths promise layout only — no mobile-specific fullscreen fallback). Keyboard while player-owned focus only: Space/K toggle exactly once (including when a focused Media Chrome control would otherwise double-activate; scroll prevented); Left/Right seek 10s clamped to seekable bounds once valid seekable bounds exist (ignore seeks before then — never throw or corrupt timeline state); M mute; F fullscreen; Escape exits browser fullscreen. Never intercept editable fields, dialogs, or global shortcuts. Labelled, Tab-reachable, visible focus. Native media events are authoritative; pointer and keyboard share the same actions. Space with region focus (no nested control focused) behaves equivalently as a single toggle.
5. **Lifecycle and truthful errors**: Loading/buffering without premature failure; playing/paused/ended from native events. Autoplay denial retains explicit Play (recoverable). Terminal media/network/unsupported-source failure shows a readable generic message, Retry, and — when capability is true — always the explicit local recovery action (hidden otherwise; never auto-launch). Safe diagnostics may record media error code / network-state via the existing diagnostic pattern only; never log URLs, paths, query strings, or configuration; no new telemetry dependency. No extension-based codec claims; no invented HTTP statuses from opaque media errors. Retry restarts the same source from the beginning (T001 must confirm stream URL lifetime/reusability for that Restart; STOP for a new ruling if delivery is header-only or expiring/single-use incompatible with same-source Retry — signed URLs, auth redesign, and re-resolution are not preauthorized); no automatic loop; no automatic local fallback. On unmount, navigation, or movie change, outgoing pause + source-clear completes before any incoming source attaches; resources release; stale async results are ignored so overlapping audio cannot continue. Closed/idle is the initial state only — no close control; teardown wording means unmount, navigation, or movie change. No persistent progress or playback-history writes from this ticket.
6. **LocalPlay capability contract (DEV-455-owned)**: Explicit Playback feature query: endpoint resolves `IOptions<FeatureOptions>` outside Core, constructs `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, invokes the decorated `GetPlaybackCapabilitiesQueryHandler` returning Core `PlaybackCapabilitiesResult(bool LocalPlayEnabled)`, and maps to Api `PlaybackCapabilitiesDto` (`localPlayEnabled` only) for `GET /api/playback/capabilities`, HTTP 200 `application/json`, reflecting effective `Features:LocalPlay`. No constructor-bool/composition-boundary alternative; no Core-to-Api reference; no Options dependency introduced by this new capability code in Core. Pre-existing Core Options-reference drift elsewhere (recon-DEV-455:98–108) is acknowledged and out of scope — follow-up noted, no automatic ticket or allowlist widening. Contract chain C# DTO → OpenAPI → generated TypeScript; no handwritten duplicate. While capability is loading or fails, hide the secondary local-launch action only; browser Play remains available. Binding proof is real-host effective options on/off plus serialized API JSON; handler bool mapping tests are supplementary.
7. **Explicit Open in BS Player**: Always offer when effective capability is true; hide otherwise. That action alone may invoke the existing generated local-launch contract (`POST /api/movies/{id}/play`). Retain server enforcement and DEV-458 hardening. Map 204 / 403 / 404 / 409 / 422 / other / network exactly as the existing generated launch contract and current UI define them; generic retry only where that contract lacks a specific meaning. Visible copy: Play, Open in BS Player, Retry, with accessible descriptive labels (Palette may refine; refinements logged as assumed taste). Do not claim every configured launcher is BS Player.
8. **Compose / real-browser verification**: Real small browser-compatible sample via `LIBRARY_PATH` from a verified git-ignored runtime location or a path outside the tracked tree; T016/T017 require scan/import of that provisioned sample or reuse of an already-scanned title so a playable movie exists. Never replace tracked zero-byte placeholders or commit unlicensed media. Record provenance/license, browser/version, codec/container, duration, Compose URL, controls, resumed seeking plus range evidence through web nginx, desktop fullscreen acceptance, errors, and LocalPlay-off hidden secondary action. Keep Compose/e2e `Features.LocalPlay=false` per ADR 0023; enabled launch only separately in a DEV-458-approved loopback deployment. Missing browser/sample = **Could not run**; runtime acceptance remains open (never PASS).
9. **Bounded modules**: Existing movie-details / library integration; cohesive `web/src/features/playback` components/hooks/tests; existing API client / generated artifacts; `web/package.json` + lockfile; app-navigation only for card player intent; capability query/handler inside existing `Core/Features/Playback` with API DTO/endpoint mapping and focused tests; e2e inside existing `web/e2e`. Handler/`AddHandler` registration uses the existing surface T001 identifies (unobserved `HandlerRegistration` / `ApiEndpoints` paths are proposed until that pickup). No new project, top-level folder, architectural layer, Core port, or state library. Outside that boundary requires a cited forcing AC/gate or a new Patron ruling.

**Out of scope**:

- Transcoding, format conversion, captions, playlists, casting, persisted resume/progress, new playback-history writes.
- Stream range / resolver / security redesign (DEV-456 ownership).
- Launcher hardening, Process.Start / bind / config changes (DEV-458 ownership).
- Schema / migration changes; new NuGet packages; second player or state library.
- New watch route, modal shell, or unrelated tracked-file deletion/rewrite.
- New glossary terms or ADR (Q12).
- Claiming DEV-456/458 already delivered at recon base; inventing stream/launch types; silently adapting contract drift.
- Closing owner checkboxes via dependency progress or this grill.

**Precedence**: Ticket Scope & Technical Design is already approved authority (`PRODUCT.md` §3). Brief Q1–Q12 and `CONCLUSIONS.md` govern boundaries. Ordinary owner Gate 1 remains pending. Quill does not invent PLAN FROZEN.

## Domain vocabulary *(mandatory — spec-local clarification of existing Q2/Q12 meanings only; no glossary, CONTEXT.md, or ADR expansion; Q12 / plan-challenge Compass F4)*

**Browser playback**
In-page streaming of the movie's primary video in the SPA via the DEV-456 stream URL under user gesture (ticket/Q2–Q4 meaning restated locally).
_Avoid_: play (when meaning local launch), Open in BS Player

**Local launch**
Explicit server-side start of a configured local player via the existing `POST /api/movies/{id}/play` contract, gated by effective LocalPlay (ticket/Q3 meaning restated locally).
_Avoid_: browser playback, automatic fallback

**Playback capabilities**
Web-visible effective LocalPlay flag from `GET /api/playback/capabilities` (`localPlayEnabled` only) — Q2-derived contract name restated locally, not a new glossary term.
_Avoid_: player configuration, executable paths, Features dump

**Open in BS Player**
Visible secondary action that invokes local launch when capability is true; label does not assert every configured player is BS Player (ticket/Q12 meaning restated locally).
_Avoid_: default Play, automatic local start

Consumed vocabulary (already in use; not redefined): **Features:LocalPlay**, **Movie**, movie details, library card, DEV-456 stream, DEV-458 launcher trust boundary.

## What the User Will See *(mandatory)*

- **Where**: Movie details page — primary Play control and in-page labelled player region.
- **What the user sees or does**: Choosing Play opens an in-page player (not a new route/modal), starts browser streaming under that gesture when the stream is available, and exposes accessible play/pause, seek, speed, volume/mute, fullscreen, and scoped keyboard controls. On terminal stream failure the user sees a readable generic message with Retry (and Open in BS Player only when LocalPlay capability permits).
- **What it needs to show up**: Default Compose stack with Api + web nginx, DEV-456 stream delivered, at least one movie with a real browser-compatible primary file via `LIBRARY_PATH` (not tracked zero-byte placeholders), and LocalPlay left false for the Compose/e2e path.

- **Where**: Library movie card — Play control.
- **What the user sees or does**: Choosing Play navigates to that movie's details, opens the same in-page player, attempts playback, and keeps an explicit Play control if autoplay is blocked.
- **What it needs to show up**: Same as details Play.

- **Where**: Movie details — secondary Open in BS Player action (when shown).
- **What the user sees or does**: When capability reports LocalPlay enabled, an explicit Open in BS Player control appears; choosing it alone may start local launch and shows existing launch success/error meanings. When LocalPlay is off, loading, or capability fails, that control stays hidden; browser Play remains.
- **What it needs to show up**: Capability endpoint available; LocalPlay-off Compose keeps the control hidden. Enabled-launch visual proof only in a separate DEV-458-approved loopback deployment (not by weakening Compose/e2e LocalPlay=false).

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001** *(planned criteria)*: **Given** Compose is up with a movie whose primary file is a real browser-compatible sample mounted via `LIBRARY_PATH`, **When** the user opens that movie's details and chooses Play, **Then** the in-page player becomes visible and playback proceeds (or shows a truthful recoverable/terminal state — not a silent no-op).
- **SMOKE-001** *(observed runtime acceptance)*: Delivery must execute real-browser Compose verification through web nginx (controls, resumed seeking/range evidence, LocalPlay-off hidden secondary action) and record status. Missing browser/sample ⇒ **Could not run** (never PASS).

- **SMOKE-002** *(planned criteria)*: **Given** the library shows that movie's card, **When** the user chooses Play on the card, **Then** the app navigates to details with the same player open and either playing or offering explicit Play after autoplay denial.
- **SMOKE-002** *(observed runtime acceptance)*: Same Compose/real-browser bar as SMOKE-001; Could not run ≠ PASS.

- **SMOKE-003** *(planned criteria)*: **Given** Compose/e2e LocalPlay is false, **When** the user opens movie details, **Then** Open in BS Player is not offered while browser Play remains available.
- **SMOKE-003** *(observed runtime acceptance)*: Recorded on the LocalPlay-off Compose path; enabled-launch coexistence is a separate loopback receipt, not a Compose LocalPlay=true change.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Playback capabilities contract (Priority: P1)

The web can discover whether Open in BS Player may be offered. The Api endpoint resolves effective LocalPlay outside Core, constructs `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, invokes the decorated handler returning Core `PlaybackCapabilitiesResult(bool LocalPlayEnabled)`, and maps to Api `PlaybackCapabilitiesDto` for `GET /api/playback/capabilities`, returning 200 JSON with only `localPlayEnabled` reflecting effective `Features:LocalPlay`. Generated TypeScript consumes the C# → OpenAPI chain. Capability loading/failure hides the secondary action only.

**Why this priority**: Ticket condition for gated secondary launch; no existing capability surface (brief Q2; recon-DEV-455:34,44).

**Independent Test**: Real-host effective options on/off plus serialized API JSON as binding proof; handler bool mapping unit tests supplementary; OpenAPI/generated-type regeneration; web capability loading/failure/true/false visibility without requiring Media Chrome.

**Acceptance Scenarios**:

1. **Given** effective LocalPlay is true on the real host options, **When** capabilities are requested, **Then** the response is 200 with `localPlayEnabled: true` and no other fields.
2. **Given** effective LocalPlay is false on the real host options, **When** capabilities are requested, **Then** the response is 200 with `localPlayEnabled: false`.
3. **Given** the capability request is in flight or fails, **When** movie details render, **Then** Open in BS Player is hidden and browser Play remains available.

---

### User Story 2 - Accessible in-page Media Chrome player (Priority: P1)

An in-page labelled Media Chrome player on movie details provides play/pause, seeking, elapsed/duration, volume/mute, speed presets, fullscreen, and scoped keyboard controls. Lifecycle follows native media events; autoplay denial is recoverable; terminal failures are truthful with Retry from the beginning; teardown prevents stale/overlap audio.

**Why this priority**: Ticket core delivery (brief Q1, Q4–Q6).

**Independent Test**: Vitest/RTL (and focused player tests) cover states, controls, focus scoping, seek clamping, retry/teardown, and autoplay-denial vs terminal failure — without requiring card navigation or LocalPlay-on. jsdom cannot prove codec decoding, resumed seeking, fullscreen, or nginx range delivery.

**Acceptance Scenarios**:

1. **Given** a movie details page with an available stream source usable by a media element (credential mode confirmed at T001), **When** the user activates Play under gesture, **Then** the labelled player requests the DEV-456 stream URL and reflects native playing/paused/ended states without premature failure during loading/buffering.
2. **Given** player-owned focus, **When** the user uses Space/K (including Space on a focused control — exactly one toggle, scroll prevented — and equivalently with region focus), arrows, M, F, or Escape (desktop fullscreen exit), **Then** the documented actions occur once valid seekable bounds exist (seeks ignored before then) and do not fire when focus is in editable fields or dialogs.
3. **Given** autoplay is blocked, **When** the player cannot start automatically, **Then** an explicit Play control remains and no terminal error is shown solely for that denial.
4. **Given** a terminal media/network/unsupported failure, **When** the error surfaces, **Then** the user sees a readable generic message and Retry that restarts the same source from the beginning without automatic loops or local fallback; safe diagnostics may record media error code/network-state without URLs/paths/query/config.
5. **Given** navigation away, unmount, or movie change (including card-navigation swaps), **When** the player tears down or swaps, **Then** outgoing pause + source-clear completes before any incoming source attaches, resources release, and stale async updates cannot restart audio.

---

### User Story 3 - Details and card Play default to browser (Priority: P1)

Existing details and card Play controls default to browser playback. Details Play opens the in-page player. Card Play navigates to details with player intent, opens the same region, attempts play, and retains explicit control if blocked. Browser Play never POSTs `/play`.

**Why this priority**: Ticket “default” requirement and both entry points (brief Q3–Q4; recon-DEV-455:28–33).

**Independent Test**: Vitest/RTL regressions on MovieDetailsView / MovieCard / navigation intent proving browser path, no launch POST from browser Play, and preserved in-flight guards where still applicable to local launch only.

**Acceptance Scenarios**:

1. **Given** movie details with browser Play available, **When** the user chooses Play, **Then** the in-page player opens and stream playback is attempted under that gesture without calling `POST /play`.
2. **Given** a library card with Play, **When** the user chooses Play, **Then** the app navigates to that movie's details, opens the same player, and attempts playback while keeping explicit Play if autoplay is blocked.
3. **Given** browser Play is used, **When** network traces/assertions run, **Then** no local-launch POST is issued from that action.

---

### User Story 4 - Explicit Open in BS Player (Priority: P1)

When capability is true, movie details offer Open in BS Player. That action alone invokes the existing launch contract and preserves current UI meanings for 204/403/404/409/422/other/network. Server enforcement and DEV-458 hardening remain. No automatic fallback from browser failure to local launch.

**Why this priority**: Ticket secondary offer and coexistence (brief Q3; Q12 copy).

**Independent Test**: Capability-true visibility; launch POST only from the secondary action; existing launch contract regression behaviour and in-flight guard; capability-false/loading/failure hide the action.

**Acceptance Scenarios**:

1. **Given** `localPlayEnabled` is true, **When** details render, **Then** Open in BS Player is offered with accessible labelling and does not claim every configured player is BS Player.
2. **Given** the user chooses Open in BS Player, **When** launch succeeds or fails, **Then** outcomes match the existing generated launch contract and current UI mappings; generic retry only where that contract has no specific meaning.
3. **Given** browser playback hits a terminal error and capability is true, **When** the error UI shows, **Then** the explicit local recovery action is always offered (hidden when capability is false/loading/failed) and the browser path never auto-invokes launch.

---

### User Story 5 - Compose real-browser evidence (Priority: P2)

Delivery records real Compose/web-nginx playback evidence with a provisioned browser-compatible sample that does not dirty tracked placeholders, plus LocalPlay-off secondary-action hiding. Enabled local launch is verified only on a separate DEV-458-approved loopback deployment.

**Why this priority**: Ticket verify-in-Compose clause; cannot be substituted by jsdom (brief Q7, Q10).

**Independent Test**: Documented runtime receipt (or Could not run). Automated e2e may assist but does not replace codec/seek/fullscreen/range evidence requirements.

**Acceptance Scenarios**:

1. **Given** a git-ignored or outside-tree real sample mounted via `LIBRARY_PATH` and scanned/imported (or an already-scanned title reused), **When** Compose playback verification runs on a desktop browser target, **Then** provenance/license, browser/version, codec/container, duration, URL, controls, resumed seeking/range through nginx, desktop fullscreen acceptance, errors, and LocalPlay-off hidden action are recorded against the delivery SHA.
2. **Given** browser or sample is unavailable, **When** verification is attempted, **Then** the result is Could not run and runtime acceptance stays open.

---

### Edge Cases

- Capability endpoint failure or slow load: hide secondary launch only; browser Play stays.
- Autoplay policy denial: recoverable explicit Play; not a terminal error by itself.
- Terminal opaque media error: generic message; no invented HTTP status or codec promise from file extension alone.
- Seek before valid seekable bounds exist: ignore; never throw or corrupt timeline state.
- Seek beyond seekable bounds once bounds exist: clamp to seekable range.
- Space on a focused Media Chrome control: exactly one toggle; scroll prevented; equivalent single toggle with region focus.
- Keyboard shortcuts while focus is in an editable field, dialog, or non-player UI: no player interception.
- Movie change / card navigation / back navigation / unmount: outgoing pause + source-clear before incoming attach; stale-work rejected; no overlapping audio.
- Terminal recovery with capability true: always offer explicit local action; never auto-launch; hide when capability is not true.
- Fullscreen acceptance: desktop browsers; responsive mobile widths are layout-only with no mobile-specific fullscreen fallback.
- Media-element credential / stream URL lifetime at T001: confirmed as future-delivery pickup prerequisites; STOP for new ruling on header-only or expiring/single-use delivery incompatible with same-source Retry — no signed URLs, auth redesign, or re-resolution preauthorized.
- LocalPlay false (Compose/e2e default): Open in BS Player hidden; launch POST still server-enforced if somehow called.
- Stream contract absent or drifted at pickup: stop for recon/new ruling — no fake types or silent adaptation.
- Tracked zero-byte sample placeholders: never overwritten for verification media.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST make both existing Play entry points (movie details and library card) default to browser playback that consumes the delivered DEV-456 stream under user gesture, without invoking `POST /api/movies/{id}/play` from those actions (Q3–Q4).
- **FR-002**: System MUST present a single labelled in-page player region inside movie details (no new watch route or modal); card Play MUST navigate to details and open that same region (Q4).
- **FR-003**: System MUST use Media Chrome as the sole new player dependency inside existing web feature structure (no second player, state library, or second wrapper dependency), with exact pin at pickup equal to the highest stable Node-24-compatible release published at least 14 days before selection, no caret, and Phase B registry evidence recorded — ticket-specific adoption of Playwright-scoped ADR 0023 precedent, not a pre-existing general npm policy; version never invented in this draft (Q1; plan-challenge Ledger M3).
- **FR-004**: Player MUST provide play/pause, seek timeline, elapsed/duration, volume/mute, speed choices 0.5/1/1.5/2, and fullscreen (desktop-browser acceptance; responsive mobile widths are layout-only with no mobile-specific fullscreen fallback), with pointer and keyboard sharing the same actions (Q5; Sentry L4).
- **FR-005**: While player-owned focus is active, Space/K MUST toggle playback exactly once (including Space on a focused control with scroll prevented, and equivalently with region focus); Left/Right MUST seek ±10s clamped to seekable bounds once valid seekable bounds exist, and MUST ignore seeks before then without throwing or corrupting timeline state; M MUST mute/unmute; F MUST toggle fullscreen; Escape MUST exit browser fullscreen; shortcuts MUST NOT apply in editable fields, dialogs, or as global interception (Q5; Sentry M4/L1).
- **FR-006**: Player controls MUST be labelled, Tab-reachable, and show visible focus (Q5).
- **FR-007**: Player lifecycle MUST follow native media events for loading/buffering, playing, paused, and ended without treating buffering as terminal failure (Q6).
- **FR-008**: Autoplay denial MUST retain an explicit Play control and MUST NOT be classified as terminal failure by itself (Q6).
- **FR-009**: Terminal media/network/unsupported-source failures MUST show a readable generic message and Retry that restarts the same source from the beginning, with no automatic retry loop and no automatic local-launch fallback; when capability is true the explicit local recovery action MUST always be offered (hidden otherwise; never auto-invoked); safe diagnostics MAY record media error code / network-state via the existing diagnostic pattern only and MUST NOT log URLs, paths, query strings, or configuration and MUST NOT add a new telemetry dependency (Q6; Sentry L2/L5).
- **FR-010**: On unmount, navigation away, or movie change (including card-navigation swaps), outgoing pause + source-clear MUST complete before any incoming source attaches; the player MUST release media resources and ignore stale async results so overlapping audio cannot continue; closed/idle is initial state only — no close control (Q6; Sentry M3; Compass F1).
- **FR-011**: This ticket MUST NOT write persistent playback progress or playback-history records (Q6).
- **FR-012**: Capability discovery MUST be: Api endpoint resolves `IOptions<FeatureOptions>` outside Core, constructs sealed `GetPlaybackCapabilitiesQuery(bool LocalPlayEnabled)`, invokes sealed `GetPlaybackCapabilitiesQueryHandler` (`IQueryHandler`) registered via `AddHandler` with Tracing → Logging → Validation returning Core `PlaybackCapabilitiesResult(bool LocalPlayEnabled)`, and maps to Api `PlaybackCapabilitiesDto` only `localPlayEnabled: boolean` for `GET /api/playback/capabilities` 200 `application/json` reflecting effective `Features:LocalPlay` — no constructor-bool/composition-boundary alternative, no Core-to-Api reference, no new Options dependency in Core from this code; pre-existing Core Options drift (recon-DEV-455:98–108) is out of scope (Q2; constitution I–II; Ledger M1/M2). Binding proof MUST be real-host effective options on/off plus serialized API JSON; handler bool mapping tests are supplementary (Ledger L3).
- **FR-013**: Capability contract MUST flow C# DTO → OpenAPI (`web/src/api/openapi.json`) → generated TypeScript (`web/src/api/types.ts`) with no handwritten duplicate types or extra DTO fields (Q2; charter contract chain).
- **FR-014**: While capabilities are loading or fail, the UI MUST hide Open in BS Player only; browser Play MUST remain available (Q2).
- **FR-015**: Open in BS Player MUST always be offered when `localPlayEnabled` is true and MUST be hidden otherwise, MUST be the only UI action that invokes the existing launch POST, MUST never be auto-invoked from browser failure, and MUST preserve existing launch status meanings (204/403/404/409/422/other/network) without redefining them (Q3; Sentry L5).
- **FR-016**: Visible copy MUST use Play, Open in BS Player, and Retry with accessible descriptive labels; the BS Player label MUST NOT assert that every configured launcher is BS Player (Q12; ASSUMPTIONS).
- **FR-017**: Implementation MUST stay within existing movie-details/library modules, cohesive `web/src/features/playback`, existing API client/generated artifacts, manifests/lockfile, app-navigation for player intent only, existing `Core/Features/Playback` + API endpoint mapping for the capability query (registration surface identified at T001; unobserved HandlerRegistration/ApiEndpoints paths are proposed until verified), and existing `web/e2e` — no new project/top-level layer/Core port/state library; no unrelated tracked deletion/rewrite (Q9; Compass F3).
- **FR-018**: Compose/e2e verification MUST keep `Features.LocalPlay=false` per ADR 0023; real sample provisioning MUST use git-ignored or outside-tree media via `LIBRARY_PATH`, MUST include scan/import of that sample or reuse of an already-scanned title, and MUST NOT replace tracked zero-byte placeholders or commit unlicensed media (Q7; Sentry L3).
- **FR-019**: Runtime acceptance MUST record Compose/web-nginx evidence (controls, resumed seeking/range, desktop fullscreen acceptance, errors, LocalPlay-off hidden secondary action) or report Could not run; jsdom/unit success MUST NOT substitute for that receipt (Q7, Q10; Sentry L4).
- **FR-020**: Phase B MUST wait for DEV-456 and DEV-458 delivery plus Gate 1, then recon actual generated stream/launch contracts and main drift before implementation tasks that consume them; T001 MUST also confirm media-element credential compatibility and stream URL lifetime/reusability as authorized future-delivery pickup prerequisites (not invented contract facts); STOP for a new ruling if delivery is header-only or expiring/single-use incompatible with Q6 same-source Retry — signed URLs, auth redesign, and re-resolution are not preauthorized; other contract drift likewise requires a new ruling, never silent adaptation or fake types (Q8; Sentry M1/M2).
- **FR-021**: Closing bar for code-review/ship-review: zero unresolved in-scope Critical/High/Medium; every Low documented and dispositioned; frozen Q1–Q10 scope (anything else is a follow-up issue, not a finding in this round); two review/fix rounds then stop for fresh Patron/Conductor ruling; all three M axes required; Charter filing policy otherwise (Q11).

### Key Entities

- **PlaybackCapabilities / PlaybackCapabilitiesResult**: Effective LocalPlay visibility for the web (`localPlayEnabled` / `LocalPlayEnabled` only), produced as Core `PlaybackCapabilitiesResult` by the Playback capability query/handler and mapped by the Api endpoint to public `PlaybackCapabilitiesDto`.
- **Browser playback session**: Transient in-page media session bound to one movie id/stream source; not persisted.
- **Local launch request**: Existing play command invoked only by the explicit secondary action when capability permits.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From movie details, a user can start in-page browser playback with one Play activation when a compatible stream is available.
- **SC-002**: From a library card, a user reaches the same in-page player via Play without a separate watch route.
- **SC-003**: With LocalPlay capability false (Compose default), Open in BS Player is not shown; with capability true, it is shown and is the only control that triggers local launch.
- **SC-004**: Keyboard and pointer controls listed in FR-004/FR-005 are operable when the player owns focus, and seek actions remain within seekable bounds.
- **SC-005**: Terminal playback failures always present readable recovery (Retry) without automatic local fallback; when capability is true the explicit local recovery action is always offered (hidden otherwise; never auto-invoked); autoplay denial remains recoverable via explicit Play.
- **SC-006**: Teardown on navigation/movie change leaves no continuing audio from the prior player instance.
- **SC-007**: Delivery either records real Compose/nginx browser evidence for SMOKE-001–003 family checks or reports Could not run — never treats missing evidence as PASS.

## Assumptions

- In-page labelled detail player (not route/modal); both Play entry points converge there. `[assumed]` ASSUMPTIONS.md (Q4).
- Speed choices 0.5/1/1.5/2; scoped shortcuts Space/K, arrows ±10s, M, F, Escape fullscreen exit. `[assumed]` ASSUMPTIONS.md (Q5).
- Readable generic browser failure with Retry from beginning; no inferred status/codec promise. `[assumed]` ASSUMPTIONS.md (Q6).
- Visible copy Play / Open in BS Player / Retry; Palette may refine wording as assumed taste; label does not assert every launcher is BS Player. `[assumed]` ASSUMPTIONS.md (Q12).
- DEV-456 delivers `GET /api/movies/{id}/stream` (and generated contract) before Phase B consumption; stated here as dependency contract, not present at recon base (Q8; recon-DEV-455:18–22).
- DEV-458 retains launch hardening; DEV-455 does not redefine launch statuses (Q3).
- ADR 0023 continues to require Compose/e2e LocalPlay=false (Q7).
