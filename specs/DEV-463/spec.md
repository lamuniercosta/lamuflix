# Feature Specification: Embedded Trailer on Movie Details (pt-BR captions preferred)

**Feature Branch**: `feature/463-spec`

**Created**: 2026-11-05

**Status**: gate1: provisional — PLAN FROZEN (2026-11-05). Analyze clean after round 1 repair (DEV-463 receipt); three-axis plan challenge round 1 adjudicated and approved updates applied. No implementation clearance. Owner spec-PR merge remains pending; T000 blocks pickup.

**Input**: Ticket DEV-463 (feature under DEV-286; Size M; no ui tag); frozen `specs/DEV-463/brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-463`, `recon-DEV-463`, `recon-DEV-463-2` (incl. §9); `specs/PRODUCT.md`. No decisions beyond the brief. Taste assumptions already logged in `ASSUMPTIONS.md`. Canonical term **Trailer key** authorized for `CONTEXT.md` (Q8). No new ADR; no owner structural checkbox (brief Status; Q8).

## Owner question — Gate 1

None. Brief Status and Q3/Q8 record no ticket contradiction and no constitution departure; zero structural owner checkboxes (`PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending. Patron approval is not owner approval.

## Scope

**In scope** (brief Frozen scope Q6; Approach Q1–Q5, Q8; ticket Scope & Acceptance via recon-DEV-463):

1. Canonical term **Trailer key**: nullable `TrailerKey` on enriched `MovieMetadata` and nullable `trailerKey` on existing `MovieMetadataResponse` nested in `MovieDetailsResponse`; no new route or status-code contract (Q1).
2. Persist a nullable `trailer_key` column on `movies` via the existing EF path + tool-generated migration/model snapshot; existing rows remain null; store a selected validated YouTube video key matching `^[A-Za-z0-9_-]{11}$`, never a full provider URL (Q1, Q4).
3. Carry the value through metadata copy/merge, persistence write/read/projection, and clear paths. Ordinary re-enrichment overwrites or clears the key; no last-known-key preservation, trailer-only retry pipeline, or bulk backfill (Q2).
4. Extend existing `TmdbMetadataProvider` on `IMetadataProvider` and `ChainedMetadataProvider` supplementary merge. When OMDb is primary, retain its unrelated scalar fields and add the TMDb trailer key. TMDb-found metadata retains its selected key. Preserve existing TMDb enablement and credentials; disabled/unconfigured TMDb yields no trailer (Q2).
5. Trailers are optional enrichment: videos HTTP/transport/JSON failure yields no candidates for that request and does not turn otherwise valid enrichment into failure; keep candidates from independently successful requests; propagate cancellation; successful result with no eligible candidate has null (Q2).
6. Acquisition and deterministic selection (Q3–Q4):
   - Read `original_language` from the existing TMDb movie-details payload.
   - After TMDb movie identity is resolved, request `/movie/{id}/videos` with `language=pt-BR` and, when trimmed `original_language` is nonblank and neither `pt` nor `pt-BR` (case-insensitive fetch equivalence), `language=<original_language>`. Documented `language` parameter only — no `include_video_language`, no invented country code.
   - Union returned candidates without assuming exhaustiveness. Eligible: `site=YouTube`, `type=Trailer` (case-insensitive), key matching `^[A-Za-z0-9_-]{11}$`.
   - Language fields compare case-insensitively. Language tier: `iso_639_1=pt` AND `iso_3166_1=BR` first; otherwise `iso_639_1` matching movie `original_language`. Missing `original_language` → only confirmed pt-BR qualifies; no unrelated-language substitute.
   - Within tier prefer `official=true`, then key ordinal ascending (`StringComparer.Ordinal`). Deduplicate by key keeping best language/official rank. Nonofficial pt-BR outranks official original-language. Do not rely on array order or `published_at`.
   - Always request captions at render time via player params; TMDb exposes no caption availability (Q3).
7. Movie Details Trailer section (Q5, Q8): place immediately before `RelatedMovies`, after primary metadata/cast content, in existing `web/src/features/movie-details/`. Hide entire section for null/empty/invalid keys (revalidate same key pattern at render). For a valid key: heading `Trailer`; native iframe in responsive 16:9 container; `loading="lazy"`; `title="Trailer for <movie title>"`; fixed URL `https://www.youtube-nocookie.com/embed/<key>?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR`; no autoplay; always-visible adjacent note `Requires internet access. Portuguese captions depend on availability.`
8. Document in spec and PR that this is the first third-party embed, requires internet access, and needs `frame-src https://www.youtube-nocookie.com` where CSP is enforced. Do not add global CSP middleware, broad wildcards, nginx rewrites, or change LAN-only/LocalPlay behavior (Q5).
9. Contract chain: C# DTO → committed `web/src/api/openapi.json` → `npm run generate:api` → generated `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (Q7).
10. Ticket-required enrichment tests (selection/language/official/ties/failures) and component tests with MSW mocks (present/absent/invalid; player attributes) plus corresponding regressions; runtime acceptance observes working embed and blocked-network layout separately (Q7).

**Out of scope** (brief Frozen scope; Q2, Q5–Q8; §2.3):

- New dependency, port, project, top-level folder, architectural layer, preferences table/endpoint, production CSP middleware, LocalPlay behavior, or secret handling.
- YouTube SDK, thumbnail fetch, automatic retry, caption-availability probe, or claiming iframe load events/timers prove playability.
- Undocumented TMDb parameters (`include_video_language`), additional discovery APIs, last-known-key preservation, trailer-only retry/backfill, or bulk historical backfill.
- Deleting or wholesale rewriting files outside the Q8 bounded file plan; inventing unspecified entity/projection/copy-site shapes (those are Phase B recon obligations).
- New ADR or constitution amendment (Size M; extends established seam; Q8).
- Anything outside frozen Q1–Q5 deliverables + ticket tests is a follow-up issue, not a finding in this round (Q6).

**Bounded file plan** (Q8 — patch in place; unspecified copy-site filenames are Phase B recon):

| Surface | Planned files or responsibility |
| --- | --- |
| Core | `src/LamuFlix.Core/Domain/MovieMetadata.cs` and necessary metadata-copy paths |
| TMDb and composition | `TmdbMetadataProvider.cs`, `TmdbMetadataProvider.Mapping.cs`, related adapter partials if needed, `ChainedMetadataProvider.cs` |
| Persistence | `EfMovieRepository.cs`, `Configurations/MovieConfiguration.cs`, existing movie persistence entity/read projections; nullable-column migration + tool-generated model snapshot |
| Details API | Existing `LibraryResponses.cs` and movie-details response mapping; no new endpoint |
| Contract artifacts | `web/src/api/openapi.json` from C#; generated `types.ts` and `handlers.ts` via `npm run generate:api` |
| Web UI | `MovieDetailsView.tsx` and CSS; small Trailer component/tests in `web/src/features/movie-details/` if useful; `web/src/mocks/fixtures.ts` |
| Tests | Corresponding existing domain, metadata adapter/chain, repository, details/OpenAPI and web/MSW suites |
| Planning/glossary | `specs/DEV-463/` artifacts; `CONTEXT.md` Trailer key term |

**Precedence**: Ticket Scope & Acceptance (PRODUCT.md §3) plus Patron Q1–Q8 in `brief.md` / `CONCLUSIONS.md` are authoritative. Taste in `ASSUMPTIONS.md`. Phase A produces documents only and claims no implementation gate evidence.

## Domain vocabulary *(mandatory — glossary term for CONTEXT.md)*

**Trailer key**
The validated YouTube video identifier selected during metadata enrichment and stored with enriched movie metadata so the Movie Details page can embed a privacy-friendly trailer. Absence is null (including existing rows) and hides the Trailer section. Never a full provider URL.
_Avoid_: trailer URL, YouTube link, video URL, embed URL (when meaning the stored value)

Consumed vocabulary (already in use; not redefined here): **MovieMetadata**, **Movie Details**, **IMetadataProvider**, enrichment, TMDb, OMDb.

## What the User Will See *(mandatory)*

- **Where**: Movie Details page (`MovieDetailsView`), Trailer section placed immediately before Related Movies (after primary metadata/cast).
- **What the user sees or does**: When the movie’s enriched metadata carries a valid Trailer key, a Trailer heading, a lazy-loaded privacy-friendly embedded player requesting Portuguese captions, and the note “Requires internet access. Portuguese captions depend on availability.” When there is no valid Trailer key, the Trailer section is absent (no empty box). The rest of Movie Details remains usable if third-party traffic is blocked.
- **What it needs to show up**: Running Compose stack; TMDb enabled and configured so enrichment can obtain a Trailer key (disabled/unconfigured TMDb yields no trailer); at least one enriched movie with a selected key for the positive case; outbound internet for the embed itself (runtime evidence separate from MSW).

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** an enriched movie whose details payload includes a valid Trailer key, **When** the user opens Movie Details, **Then** a Trailer section appears before Related Movies with a lazy iframe from `youtube-nocookie.com` titled “Trailer for \<movie title\>”, caption query parameters present, no autoplay, and the internet/captions note visible.
- **SMOKE-002**: **Given** an enriched or unenriched movie with null/empty/invalid Trailer key, **When** the user opens Movie Details, **Then** no Trailer section or empty placeholder is shown; other details content remains visible.
- **SMOKE-003**: **Given** a movie showing the Trailer section and third-party/network traffic blocked in the provisioned browser, **When** the user views Movie Details, **Then** the local Trailer layout and note remain and the rest of the page stays usable (runtime evidence; unavailable external access is `Could not run`, never PASS).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enrichment selects and persists Trailer key (Priority: P1)

During TMDb enrichment (direct or OMDb-primary supplement), the adapter fetches movie videos, applies the Q3–Q4 eligibility and ranking rules, and stores a nullable Trailer key with metadata. Videos failures degrade to null without failing otherwise valid enrichment. Disabled TMDb yields null. Re-enrichment overwrites or clears.

**Why this priority**: Ticket Scope bullets 1–2 and Acceptance enrichment-test requirement; without a stored key nothing can render (Q1–Q4, Q7).

**Independent Test**: Enrichment/selection unit and WireMock matrices for language tiers, official preference, ties, duplicates, eligibility filters, partial/all videos failures, cancellation, OMDb-primary merge, TMDb-found path, disabled TMDb; real-DB null/present roundtrip and re-enrichment clear/replace.

**Acceptance Scenarios**:

1. **Given** TMDb returns eligible pt-BR and original-language YouTube Trailers, **When** enrichment completes, **Then** the persisted Trailer key is the best-ranked pt-BR candidate (nonofficial pt-BR beats official original).
2. **Given** no pt-BR candidate but an eligible original-language Trailer, **When** enrichment completes, **Then** that key is stored and captions are still requested at render time.
3. **Given** missing/blank `original_language` and no confirmed pt-BR Trailer, **When** enrichment completes, **Then** Trailer key is null.
4. **Given** videos HTTP/transport/JSON failure on one language request and success on the other, **When** enrichment completes, **Then** candidates from the successful request are kept and enrichment is not failed solely for videos failure.
5. **Given** TMDb disabled or unconfigured, **When** enrichment completes via OMDb-only, **Then** Trailer key is null and unrelated OMDb scalars are preserved.
6. **Given** an existing row with a Trailer key, **When** a later enrichment finds no eligible trailer, **Then** the key is cleared (no last-known preservation).

---

### User Story 2 - Details contract exposes nullable trailerKey (Priority: P1)

Existing movie-details response nesting gains nullable `trailerKey` on `MovieMetadataResponse`. Committed OpenAPI baseline and generated TypeScript/MSW update through the existing generator. No new route.

**Why this priority**: Ticket Scope bullet 1 OpenAPI baseline update; contract chain must precede typed web consumption (Q1, Q7).

**Independent Test**: Details JSON null/present field; OpenAPI Verify drift green; `npm run generate:api` / `check:api` green; no hand-rolled types.

**Acceptance Scenarios**:

1. **Given** a movie with stored Trailer key, **When** details are fetched, **Then** `metadata.trailerKey` is the key string.
2. **Given** a movie without a Trailer key (including legacy null rows), **When** details are fetched, **Then** `metadata.trailerKey` is null or absent consistently with existing nullable metadata field conventions, and no new status codes appear.
3. **Given** the DTO change, **When** OpenAPI is regenerated, **Then** committed `openapi.json`, Verify snapshot, `types.ts`, and MSW handlers reflect `trailerKey` without hand edits.

---

### User Story 3 - Movie Details Trailer section embed (Priority: P1)

Movie Details shows a Trailer section only for keys matching `^[A-Za-z0-9_-]{11}$`, with the Q5 iframe contract, placement before Related Movies, and always-visible internet/captions note.

**Why this priority**: Ticket Acceptance bullets 1–3 and Scope bullets 3–4 (Q5, Q8; ASSUMPTIONS).

**Independent Test**: Vitest/RTL/MSW component tests for present, absent, and invalid key; assert host/path, title, lazy, caption params, no autoplay, note, responsive container, and placement relative to Related Movies.

**Acceptance Scenarios**:

1. **Given** details with a valid Trailer key and movie title, **When** Movie Details renders, **Then** the Trailer section appears before Related Movies with heading “Trailer”, iframe `title="Trailer for <movie title>"`, `loading="lazy"`, src `https://www.youtube-nocookie.com/embed/<key>?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR`, no autoplay, 16:9 container, and the internet/captions note.
2. **Given** null, empty, or invalid Trailer key, **When** Movie Details renders, **Then** no Trailer section or empty box is rendered.
3. **Given** a valid key, **When** the iframe URL is inspected, **Then** it uses `youtube-nocookie.com` only (no YouTube SDK).

---

### User Story 4 - Offline/CSP honesty and runtime evidence (Priority: P2)

Document first third-party embed / CSP `frame-src` / internet dependency in the PR. Keep Movie Details usable when third-party traffic is blocked. Runtime acceptance observes a working embed and blocked-network layout; iframe onLoad alone is insufficient; unavailable external access is `Could not run`, never PASS.

**Why this priority**: Ticket Scope bullet 5; Q5/Q7 separate runtime evidence from MSW (Q6 frozen scope).

**Independent Test**: PR/spec CSP and internet note present; browser runtime receipt for working embed and blocked layout, or explicit `Could not run`.

**Acceptance Scenarios**:

1. **Given** a deployment that enforces CSP, **When** operators read the PR/spec note, **Then** they see that `frame-src https://www.youtube-nocookie.com` is required and that no global CSP middleware was added by this ticket.
2. **Given** third-party traffic blocked, **When** Movie Details with a Trailer key is viewed, **Then** the local note and layout remain and other details stay usable.
3. **Given** external YouTube access unavailable in the provisioned environment, **When** runtime embed proof is attempted, **Then** the result is recorded `Could not run` (never PASS).

### Edge Cases

- Videos request cancellation propagates immediately; do not swallow `OperationCanceledException` (Q2).
- Partial language-request failure contributes no candidates from the failed request but retains successes (Q3).
- Wrong site (`Vimeo`, etc.), non-`Trailer` types, blank/malformed/URL-shaped keys are ineligible (Q4; ASSUMPTIONS).
- Duplicate keys across language responses keep the best `(languageTier, isOfficial)` rank; equal ranks break by ordinal key ascending (Q4).
- Client-side render guard revalidates the same key pattern even if a bad value were somehow stored (Q5).
- Cross-origin iframe load events must not be treated as playability or caption proof (Q5).
- Existing enriched rows without a column backfill stay null Trailer key until ordinary re-enrichment (Q1–Q2).
- Unspecified persistence entity/projection/copy-site filenames resolved only by Phase B recon — do not invent shapes in Phase A (Q8).
- Anything outside frozen Q1–Q5 + ticket tests is a follow-up via Rigger when Critical/High/broken; otherwise noted, no ticket (Q6).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Canonical stored value MUST be Trailer key: nullable `string? TrailerKey` on `MovieMetadata` and nullable `trailerKey` on `MovieMetadataResponse`; no new API route or status codes (Q1).
- **FR-002**: Persist nullable `trailer_key` on `movies` via existing EF path + tool-generated migration/snapshot; existing rows null; store only keys matching `^[A-Za-z0-9_-]{11}$`, never a provider URL (Q1, Q4).
- **FR-003**: Carry Trailer key through metadata copy/merge, persistence write/read/projection, and clear paths; re-enrichment overwrites or clears; no last-known preservation, trailer-only retry, or bulk backfill (Q2).
- **FR-004**: Trailer acquisition MUST extend `TmdbMetadataProvider` and OMDb+TMDb supplement merge as optional enrichment; preserve TMDb enablement/credentials; disabled/unconfigured TMDb → null; OMDb-primary merge retains unrelated scalars while adding TMDb Trailer key (Q2).
- **FR-005**: Videos HTTP/transport/JSON failure MUST NOT fail otherwise valid enrichment; independent request failures contribute no candidates; videos-only 429/quota/timeout causes no enrichment retry or DeadLetter (retain successful-request candidates); details-path retry behavior stays intact; the same cancellation token reaches both requests and is checked before the second, never swallowed; no eligible candidate → null (Q2).
- **FR-006**: After TMDb identity resolve, request `/movie/{id}/videos?language=pt-BR` and, when trimmed `original_language` is nonblank and neither `pt` nor `pt-BR` (case-insensitive fetch equivalence), `language=<original_language>`; read `original_language` from existing movie-details payload; documented `language` only (Q3).
- **FR-007**: Eligible candidates REQUIRE `site=YouTube`, `type=Trailer` (case-insensitive), and key `^[A-Za-z0-9_-]{11}$` (Q4; ASSUMPTIONS).
- **FR-008**: Compare language fields and original language case-insensitively; rank by language tier (pt-BR = `iso_639_1=pt` ∧ `iso_3166_1=BR` first; else `iso_639_1 == original_language`), then `official=true`, then key ordinal ascending; dedupe by key keeping best rank; missing `original_language` allows only confirmed pt-BR; no unrelated-language substitute (Q3–Q4; ASSUMPTIONS).
- **FR-009**: Movie Details MUST hide the entire Trailer section for null/empty/invalid keys; for valid keys render Q5 iframe contract, heading, title, lazy loading, fixed nocookie URL with caption params, no autoplay, 16:9 container, always-visible note, placement before Related Movies (Q5, Q8; ASSUMPTIONS).
- **FR-010**: Document first third-party embed, internet dependency, and required `frame-src https://www.youtube-nocookie.com` in PR/spec; MUST NOT add global CSP middleware, wildcards, nginx rewrites, or LocalPlay changes (Q5).
- **FR-011**: Contract chain MUST be C# DTO → committed OpenAPI → `npm run generate:api` → generated TS/MSW; never hand-roll types/handlers (Q7; PRODUCT contract chain).
- **FR-012**: Tests MUST cover the Q7 matrix with existing frameworks only (xUnit v3, Shouldly, NSubstitute, WireMock.Net, Testcontainers Postgres, Verify, Vitest, RTL, MSW); no mocked DB driver where translation/serialization matters; ticket enrichment + MSW component coverage required (Q7).
- **FR-013**: Runtime acceptance MUST separately observe working embed and blocked-network usability; iframe onLoad alone insufficient; unavailable external access → `Could not run`, never PASS (Q5, Q7).
- **FR-014**: Phase B verification MUST run configured harness scopes (Roslyn, complexity, InspectCode, property, vulnerable packages, format, full tests, pre-PR mutation without `--since` in worktrees) and web-implement / generated-API drift gates; thresholds only from `harness.yml`; PASS / SKIPPED (scope-empty) / SKIP / mutation N/A / Could not run remain distinct; Size M requires Sentry, Ledger, and Compass axes under Q6 closing bar (zero unresolved Critical/High; Medium/Low explicit disposition; round cap two/two) (Q6–Q7).
- **FR-015**: Production edits stay inside the Q8 bounded file plan; no new dependency/port/project/layer/preferences/CSP middleware/LocalPlay/secrets; append Trailer key to `CONTEXT.md`; no ADR (Q8).

### Key Entities

- **Trailer key**: Validated 11-character YouTube video id stored on enriched metadata; null means no Trailer section.
- **Eligible trailer candidate**: TMDb video with YouTube site, Trailer type, and valid key, ranked by language tier then official then ordinal key.
- **Movie Details Trailer section**: Optional UI section embedding `youtube-nocookie` iframe with caption params and internet/captions note.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A movie with a valid stored Trailer key shows the Trailer section on Movie Details with the specified embed attributes (ticket AC 1).
- **SC-002**: A movie without a valid Trailer key shows no Trailer section (ticket AC 2).
- **SC-003**: The player URL always requests Brazilian Portuguese captions via `cc_load_policy=1`, `cc_lang_pref=pt`, and `hl=pt-BR` (ticket AC 3).
- **SC-004**: Enrichment tests prove selection order (pt-BR ahead of original; nonofficial pt-BR ahead of official original), eligibility filters, ties/dedupe, failure degradation, and merge behavior (ticket AC 4 enrichment).
- **SC-005**: Component tests with MSW cover present, absent, and invalid key cases including player attributes and section hide (ticket AC 4 component).
- **SC-006**: OpenAPI/TS/MSW drift checks pass for nullable `trailerKey` without hand-rolled contracts (Q1, Q7).
- **SC-007**: Runtime embed and blocked-network checks pass with honest exits, or report `Could not run` when external access is unavailable (Q5, Q7).
- **SC-008**: Applicable configured gates pass or report SKIPPED/SKIP/N/A/Could not run distinctly — never as PASS; Q6 review bar and round cap hold (Q6–Q7).

## Assumptions

- Taste and tie-break assumptions are those already logged in `specs/DEV-463/ASSUMPTIONS.md` (Q4 eligibility/ranking; Q5 copy/title/note; Q8 placement before Related Movies). Quill does not invent further taste.
- TMDb movie-videos `language` list semantics/completeness remain undocumented; Q3 ratifies preference over returned candidates only — no exhaustiveness or subtitle guarantee (brief; recon-DEV-463-2 §9).
- Phase B pickup must refresh recon for main/contract drift and unspecified entity/projection/copy-site filenames; Phase A recon baseline is not clearance (Q7–Q8).
- No new npm/NuGet dependency; plain iframe only (Q5; PRODUCT §5 item 1).

## Clarifications

All grill categories were resolved by Patron Q1–Q8 before drafting. `/speckit-clarify` scan: Functional Scope, Domain/Data, Interaction, NFR, Integration/Dependencies, and Out-of-scope — **Clear**. No `[NEEDS CLARIFICATION]` markers remain. No owner Gate 1 checkbox (brief Status; Q8).

## Traceability

| Source | Encoded as |
|---|---|
| Ticket Scope 1–5 / Acceptance 1–4 | Scope; US1–US4; FR-001–FR-013; SC-001–SC-005 |
| Q1 Trailer key / DTO / column | Domain vocabulary; FR-001–FR-002; US2 |
| Q2 optional enrichment / merge / failure | US1; FR-003–FR-005 |
| Q3 language retrieval policy | US1; FR-006; FR-008; Assumptions |
| Q4 eligibility / ranking | US1; FR-007–FR-008; ASSUMPTIONS |
| Q5 UI / CSP / offline honesty | What the User Will See; US3–US4; FR-009–FR-010, FR-013; ASSUMPTIONS |
| Q6 loop discipline / frozen scope | FR-014; SC-008; Edge Cases |
| Q7 ordering / tests / gates | FR-011–FR-014; plan Ordering; tasks phases |
| Q8 file plan / glossary / placement | Scope table; FR-015; Domain vocabulary; ASSUMPTIONS |
| recon-DEV-463 / recon-DEV-463-2 | Assumptions; Edge Cases; file plan |
