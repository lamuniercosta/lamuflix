# DEV-463 — Embedded trailer: settled implementation brief

## Status and authority

Grill concluded with Patron at Q8/12 on 2026-11-05. This is the decision baseline for Quill to draft spec.md, plan.md and tasks.md. It is not a plan freeze, owner Gate 1 approval, implementation authorization, successful verification, runtime acceptance, or merge clearance. No structural owner checkbox arose: Patron explicitly ruled Q3 consistent with ticket scope. The user owns merge.

- Worktree: F:/Dev/LamuFlix.worktrees/feature-463-spec
- Branch: feature/463-spec
- Recon base: 933e8ca827d18afafd659a2fdecd4f2c21953ce7
- Ticket: DEV-463, Size M, parent DEV-286; no ui: tag.
- Factual sources: canvas notes recon-DEV-463 and recon-DEV-463-2, including section 9. These are bounded, base-specific evidence, not a current implementation inventory.
- Decision sources: CONCLUSIONS.md Q1-Q8; taste decisions: ASSUMPTIONS.md. The full recorded exchanges are reproduced below so this brief contains every grill answer.
- Governing basis: verbatim ticket Scope and Acceptance in recon-DEV-463 lines 24-39; PRODUCT authority/taste/care-list rules; constitution Principles I, IV, VII, VIII, IX and contract rules as cited in recon and each ruling.

## Loop discipline — Q6

Closing bar for code-review and ship-review: zero unresolved Critical or High findings. Every Medium/Low finding receives an explicit recorded disposition. This M ticket requires Sentry, Ledger and Compass axis reports before adjudication, irrespective of a pre-pass tier. Missing axes block adjudication. Review closure does not replace required gates or runtime evidence.

Frozen scope: TMDb trailer retrieval and Q1-Q4 selection; nullable metadata persistence and existing movie-details contract propagation; Q5 lazy privacy embed, isolated blocked-network behavior and CSP/internet documentation; ticket-required enrichment/component tests and corresponding regression checks; anything else is a follow-up issue, not a finding in this round.

Out-of-scope disposition follows Patron Q6: Critical/High or broken behavior is routed to Rigger for a follow-up ticket; otherwise record noted, no ticket. No seat other than Rigger writes YouTrack.

Round cap: two analysis/fix rounds and two challenge/review/fix rounds per applicable loop. Keel sends Quill one numbered fix list per analysis round and independently reads back repairs. Report remaining blockers at the cap; do not silently waive, restart the loop, or lower a gate. Grill ceiling was 12 questions; 8 were used.

## Approach and behavior

### Contract, domain and storage — Q1, Q2, Q8

Use canonical term Trailer key. Extend existing MovieMetadata with nullable string TrailerKey and existing MovieMetadataResponse with nullable trailerKey inside MovieDetailsResponse. No new route or status-code contract. Persist a nullable trailer_key column on movies through the existing EF path; existing rows remain null. Store a selected validated key, never a full provider URL. Carry the value through metadata copy/merge, persistence write/read/projection and clear paths. A tool-generated migration/model snapshot accompanies the nullable column; generated files are never manually edited.

Extend TmdbMetadataProvider on existing IMetadataProvider, and ChainedMetadataProvider supplementary merge. When OMDb is primary, retain its unrelated scalar fields and add the TMDb trailer key. TMDb-found metadata retains its selected key directly. Preserve existing TMDb enablement and credential handling. Disabled/unconfigured TMDb yields no trailer; do not enable it automatically or add secrets.

Trailers are optional enrichment. Videos HTTP/transport/JSON failure yields no candidates for that request and does not turn otherwise valid enrichment into failure. Keep candidates from independently successful requests. Propagate cancellation. A successful result with no eligible candidate has null. Ordinary re-enrichment overwrites or clears the key; no last-known-key preservation, trailer-only retry pipeline, or bulk backfill is added.

### Acquisition and deterministic selection — Q3, Q4

1. Read original_language from the existing TMDb movie-details payload.
2. After resolving TMDb movie identity, request /movie/{id}/videos with language=pt-BR and, when original_language is known and distinct, language=<original_language>. Use the documented language parameter only. Do not add include_video_language or invent a country code.
3. Union returned candidates, without assuming either request is exhaustive or that omission returns every language. Eligible entries have site YouTube and type Trailer (case-insensitive), and a key matching ^[A-Za-z0-9_-]{11}$.
4. Language tier: iso_639_1=pt plus iso_3166_1=BR first; otherwise iso_639_1 matching the movie original_language. With missing original_language, only confirmed pt-BR qualifies. No unrelated-language substitute.
5. Within the language tier prefer official=true, then key ordinal ascending. Deduplicate by key, retaining its best language/official rank. Do not rely on array order or published_at. Nonofficial pt-BR therefore outranks official original-language.
6. If nothing qualifies, persist null. Always request captions at render time. TMDb exposes no caption availability, so original with captions requested and original as-is share the same selection; actual caption support is controlled by the uploader/player.

Evidence limit is explicit: original_language on movie details and the language query parameter are documented, but movie-videos language-list semantics/completeness are not. Q3 ratifies preference over returned candidates and makes no exhaustiveness or subtitle guarantee. Additional discovery APIs, undocumented parameters and caption probing are outside scope.

### Movie Details and internet/CSP behavior — Q5, Q8

Place Trailer immediately before RelatedMovies, after the primary metadata/cast content, in the existing movie-details feature. Hide the entire section for null, empty or invalid keys, with no empty placeholder. Revalidate the same key pattern at the render boundary.

For a valid key, render a native iframe in a responsive 16:9 container:

- Heading: Trailer
- Title: Trailer for <movie title>
- loading=lazy
- Fixed URL: https://www.youtube-nocookie.com/embed/<key>?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR
- No autoplay
- Always-visible adjacent note: Requires internet access. Portuguese captions depend on availability.

The local note and isolated layout keep Movie Details usable with third-party traffic blocked. Do not claim iframe load events or elapsed time prove playability. No YouTube SDK, thumbnail request, automatic retry or caption-availability probe is introduced.

Document in the spec and PR that this is the first third-party embed, requires internet access, and needs frame-src https://www.youtube-nocookie.com in deployments enforcing CSP. Do not add global CSP middleware, broad wildcards, nginx policy rewrites, or change LAN-only/local playback behavior. A working external embed is separate runtime evidence, not inferred from MSW or a loaded iframe.

## Bounded file plan — Q8

Patch existing files narrowly, without deletion or wholesale rewrite:

| Surface | Planned files or responsibility |
| --- | --- |
| Core | src/LamuFlix.Core/Domain/MovieMetadata.cs and necessary metadata-copy paths |
| TMDb and composition | src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.cs, TmdbMetadataProvider.Mapping.cs, related adapter partials if needed, ChainedMetadataProvider.cs |
| Persistence | src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs, Configurations/MovieConfiguration.cs, existing movie persistence entity/read projections; nullable-column migration plus tool-generated model snapshot |
| Details API | Existing LibraryResponses.cs and movie-details response mapping; no new endpoint |
| Contract artifacts | web/src/api/openapi.json from C#; generated web/src/api/types.ts and web/src/mocks/handlers.ts through npm run generate:api |
| Web UI | web/src/features/movie-details/MovieDetailsView.tsx and CSS; small Trailer component/tests in that existing folder if useful; web/src/mocks/fixtures.ts |
| Tests | Corresponding existing domain, metadata adapter/chain, repository, details/OpenAPI and web/MSW suites, with targeted new tests in their established projects |
| Planning/glossary | specs/DEV-463/brief.md, CONCLUSIONS.md, ASSUMPTIONS.md and Quill artifacts; CONTEXT.md Trailer key term |

Unspecified entity/projection/copy-site filenames are implementation recon obligations. This plan does not invent their current shapes or authorize a redesign. Request fresh bounded recon for missing implementation facts or pickup drift.

No new dependency, port, project, top-level folder, architectural layer, preferences table/endpoint, production CSP middleware, LocalPlay behavior, or secret handling is introduced. Patron ratified file scope under section 2.3 item 6. No ADR is required: this M ticket extends an established seam. Patron has recorded the Trailer key glossary entry and taste assumptions; Quill must use that canonical vocabulary and respect existing ADRs.

## Test strategy — Q7

Use existing frameworks and libraries: xUnit v3, Shouldly, NSubstitute, WireMock.Net, Testcontainers Postgres, Verify, Vitest, React Testing Library and MSW. Do not mock a database driver where translation/serialization matters.

- Selection/enrichment: pt-BR versus original-language ordering, nonofficial pt-BR versus official original, official preference within tier, deterministic ties and input-order independence, duplicate keys, unknown original language, empty results, wrong site/type, blank/malformed/URL-shaped keys, partial/all videos failures and cancellation.
- Composition: OMDb-primary supplementary trailer propagation without scalar replacement; TMDb-found path; disabled TMDb; no eligible trailer; metadata copy and clear behavior.
- Persistence/API: real-database key roundtrip, null existing rows, re-enrichment replacement/clear, details JSON null/present field and committed OpenAPI snapshot/drift.
- Web/MSW: present, absent and invalid key; generated-contract fixture compatibility; entire section hidden without key; fixed host/path, title, lazy loading, caption parameters, no autoplay, note, responsive container and placement.
- Runtime acceptance separately observes a working embed and Movie Details usability/layout with external network blocked in the provisioned browser. An iframe load event alone is insufficient. Unavailable external access is Could not run, never PASS.

## Gate expectations and task ordering — Q7

Current phase runs no implementation gates and claims no test/build/runtime success. Phase A continues with Quill drafting spec.md, plan.md and tasks.md from this brief, then read-only speckit-analyze, bounded plan challenge/adjudication and explicit plan freeze. Patron provisional Gate 1 and the owner-controlled spec PR/Gate 1 remain separate.

At Phase B pickup, check drift against main and re-run analysis before implementation; amendments need the corresponding decision recorded in brief first. Keep the contract dependency chain strict:

1. Domain/key copying and nullable EF storage/migration/readback.
2. TMDb retrieval, deterministic selection, chain merge and optional failure handling.
3. Existing details DTO mapping and committed OpenAPI baseline.
4. Regenerate TypeScript and MSW through the existing generator.
5. Integrate Trailer UI and typed fixtures/components.
6. Verify complete behavior, gather runtime evidence, and include CSP/internet note in the PR.

Backend verification uses the configured harness scopes and thresholds: Roslyn analyzers, complexity, JetBrains inspections, property tests, vulnerable packages, full tests, formatting and pre-PR mutation. Frontend follows web-implement gates plus generated-API drift checks. Read each gate help when flags are needed; no invented commands or lowered thresholds. PASS, SKIPPED (scope-empty), SKIP opt-out, mutation N/A and Could not run remain distinct. Runtime acceptance and all three review axes are separate requirements. The user merges; never merge or enable auto-merge.

## Complete recorded grill exchanges and cited rulings

The following is a verbatim copy of the append-only CONCLUSIONS.md at grill closure. Q1-Q8 include each question, Patron answer, cited rationale and implications. Later decision changes must be recorded in this brief before downstream artifacts are changed.

# DEV-463 — Patron Rulings (CONCLUSIONS)

## Q1/12 — Canonical term `Trailer key`, nullable `TrailerKey` on `MovieMetadata` and `trailerKey` on `MovieMetadataResponse`, no new route

- **Question (Keel)**: Shall the canonical term be Trailer key, represented by nullable `TrailerKey` on enriched `MovieMetadata` and nullable `trailerKey` on the existing `MovieMetadataResponse` nested in `MovieDetailsResponse`, with no new route? Recommendation: persist only a selected valid YouTube video key, never a provider URL; the web constructs the fixed `youtube-nocookie` embed URL; absence is `null` (including existing rows) and hides the section; adds a nullable metadata column and EF migration plus generated OpenAPI/types/MSW updates.
- **Verdict**: Approved (`Trailer key` canonical term in `CONTEXT.md`; nullable `string? TrailerKey` on `MovieMetadata` and `MovieMetadataResponse`, serialized as `trailerKey`; no new API route; nullable column on `movies` + EF migration).
- **Basis & Rationale**:
  - **Ticket Scope & Authority (`specs/PRODUCT.md` §3; `DEV-463` Scope bullet 1 & 3)**: Ticket explicitly mandates: *"Store the trailer key with the enriched metadata and expose it on the movie details response (OpenAPI baseline update)"* and *"Embed with a privacy-friendly player (youtube-nocookie.com)"*.
  - **Constitution Principle I & VIII (`.specify/memory/constitution.md:278-305, 456-474`)**: `TrailerKey` is vendor-neutral in Core and API identifiers while adding `Trailer key` to `CONTEXT.md`; no new port is introduced (`IMetadataProvider` seam is reused).
  - **Charter §2.3 items 3 & 4 (`specs/PRODUCT.md:45-46`, API and Contract Rules `:672-674`)**: Adding the nullable `trailer_key` column to `MovieMetadata` in `MovieConfiguration` + migration and `string? TrailerKey` to `MovieMetadataResponse` (`web/src/api/openapi.json` → generated `types.ts` and `handlers.ts`) is the minimal schema and DTO extension required by the ticket.
- **Implications**:
  - Existing enriched or unenriched rows default `TrailerKey` to `null` without breaking queries or requiring backfill migrations; when `metadata?.trailerKey` is `null` or empty, the web hides the Trailer section.
  - Only a validated video key is stored in `MovieMetadata.TrailerKey` (never a full URL); the frontend constructs `https://www.youtube-nocookie.com/embed/{trailerKey}` with the ticket's caption query parameters.

## Q2/12 — Trailer acquisition extends existing TMDb adapter and supplement merge as optional enrichment

- **Question (Keel)**: Shall trailer acquisition extend the existing TMDb adapter and supplement merge, while remaining optional enrichment? Recommendation: no new port, project, dependency, preferences storage, endpoint, or `LocalPlay` changes; TMDb remains governed by its existing enablement and credentials. Fetch videos only after a TMDb movie identity is resolved, carry the selected key through existing metadata copies and persistence read/write/clear paths, and extend the OMDb-primary merge to carry the TMDb trailer key without replacing unrelated scalars. A successful lookup with no eligible trailer produces `null`. Videos transport/status/JSON failure leaves the otherwise valid enrichment successful with `null` for this attempt; cancellation still propagates. Cost: no last-known-trailer preservation or separate trailer retry/backfill pipeline; an ordinary later enrichment may recover it.
- **Verdict**: Approved (trailer acquisition extends `TmdbMetadataProvider` and the existing OMDb+TMDb supplement merge as optional enrichment; videos failure degrades to `TrailerKey = null` while preserving `OperationCanceledException` propagation).
- **Basis & Rationale**:
  - **Ticket Scope & Authority (`specs/PRODUCT.md` §3; `DEV-463` Scope bullet 1 & 4)**: Ticket directs using *"the metadata provider already in use (TMDb videos endpoint, `/movie/{id}/videos`)"*, storing the trailer key with enriched metadata, and hiding the section when no trailer is available.
  - **Constitution Principles I, IV, VII & Coding Conventions (`.specify/memory/constitution.md:278-305, 354-384, 435-452, 629-631`)**: Reusing `IMetadataProvider` / `TmdbMetadataProvider` and existing `TmdbOptions` avoids adding a port, project, dependency, or touching `Features:LocalPlay` (§2.3 items 1, 2, 5). Propagating `CancellationToken` while treating supplementary `/movie/{id}/videos` errors as non-fatal matches the existing TMDb optional signals pattern (`recon-DEV-463-2` §§7.1–7.4) and preserves Principle IV enrichment state machine guarantees.
  - **Charter §2.3 (`specs/PRODUCT.md:41-48`)**: No user-preferences table or endpoint is introduced; trailer key flows strictly through existing `MovieMetadata` copy/merge/persistence/clear paths.
- **Implications**:
  - When OMDb is primary and TMDb is enabled as supplement, the merge retains OMDb primary scalars while carrying `TrailerKey` from TMDb; when TMDb is disabled or finds no eligible trailer, `TrailerKey` is `null`.
  - Transport, non-success HTTP status, or JSON errors on `/movie/{id}/videos` yield `TrailerKey = null` without failing the movie's enrichment attempt; `OperationCanceledException` propagates immediately; re-enrichment overwrites or clears `TrailerKey` along with the rest of `MovieMetadata`.

## Q3/12 — TMDb `/movie/{id}/videos` retrieval and language preference policy (`pt-BR` first, then `original_language`)

- **Question (Keel)**: Approve this retrieval and language policy within the now-established documentation limits? Recommendation: read `original_language` from the existing movie-details payload; request movie videos with `language=pt-BR` and, when known and distinct, `language=<original_language>`, deduplicate returned candidates by validated key, and rank using response fields rather than assuming either query is exhaustive. `pt-BR` means `iso_639_1=pt` AND `iso_3166_1=BR`; original means `iso_639_1` matches movie `original_language`. Select `pt-BR` first, otherwise original-language; captions are never inferred from TMDb and every embed requests the ticket caption parameters (`cc_load_policy=1`, `cc_lang_pref=pt`, `hl=pt-BR`). Thus original with requested captions and original as-is are the same selection, with actual caption availability controlled by YouTube. With missing `original_language` select only a confirmed `pt-BR` candidate, otherwise `null`; no unrelated-language substitute. Independent request failures contribute no candidates but retain successes under Q2. Please also judge any ticket contradiction explicitly under §2.3.
- **Verdict**: Approved (no §2.3 ticket contradiction; read `original_language` from TMDb movie details, query `/movie/{id}/videos` with `language=pt-BR` and distinct `language=<original_language>`, deduplicate by validated key, and rank by `iso_639_1=pt && iso_3166_1=BR` first then `iso_639_1 == original_language`, with player caption params always requested).
- **Basis & Rationale**:
  - **No §2.3 Ticket Contradiction (`specs/PRODUCT.md` §3, §5; `DEV-463` Scope bullet 2)**: The ticket states *"Order of preference: pt-BR trailer, original-language trailer with pt-BR subtitles enabled, then the original trailer as is. Request the pt-BR captions through the player parameters (for YouTube: `cc_load_policy=1`, `cc_lang_pref=pt`, `hl=pt-BR`). The subtitles can only be requested, not guaranteed, because they depend on the uploader."* Because TMDb `/movie/{id}/videos` exposes no caption metadata (`recon-DEV-463-2` §6, §9.2) and the ticket itself specifies that the distinction between step 2 ("original-language trailer with pt-BR subtitles enabled") and step 3 ("the original trailer as is") is realized at playback time by requesting the YouTube caption parameters, selecting `pt-BR` (`iso_639_1=pt` and `iso_3166_1=BR`) first and `original_language` (`iso_639_1 == original_language`) second while always appending `cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR` implements the exact ticket design rather than contradicting it.
  - **TMDb Documented API Surface (`recon-DEV-463-2` §§6, 9.1, 9.2)**: `original_language` is present on TMDb movie details (`§9.1`), and `language` is the documented query parameter on `/movie/{id}/videos` (`§9.2`), avoiding undocumented parameters (`include_video_language`) or country inference.
  - **Resilience & Optional Enrichment (Q2/12; Constitution Principle IV)**: Treating the `language=pt-BR` and `language=<original_language>` video requests independently so a partial failure still yields candidates from the succeeding request (while propagating `OperationCanceledException`) preserves optional enrichment without failing the movie.
- **Implications**:
  - `TmdbMetadataProvider` reads `original_language` from the `/movie/{id}` response, queries `/movie/{id}/videos?language=pt-BR` and (when `original_language` is present, non-empty, and not already `pt-BR`/`pt`) `/movie/{id}/videos?language=<original_language>`, deduplicates by validated video key, and filters to eligible candidates matching either `pt-BR` (`iso_639_1 == "pt"` and `iso_3166_1 == "BR"`, case-insensitive) or `original_language` (`iso_639_1 == original_language`, case-insensitive).
  - Candidates in unrelated languages are rejected (never selected); if `original_language` is missing/blank, only a confirmed `pt-BR` candidate can win, otherwise `TrailerKey` is `null`.

## Q4/12 — Candidate eligibility (`site=YouTube`, `type=Trailer`, 11-char ASCII key) and deterministic ranking/tie-break order

- **Question (Keel)**: How shall eligibility and ties be resolved? Recommendation: eligible videos require `site=YouTube`, `type=Trailer`, and an 11-character ASCII key matching `[A-Za-z0-9_-]{11}`; all other types/sites, blank or URL-shaped keys are excluded. Apply Q3 language tier first (`pt-BR` ahead of `original_language`), then `official=true` ahead of non-official within that language tier, then `key` ordinal ascending (`StringComparer.Ordinal`) for deterministic ties, independent of response order or publication dates. Deduplicate by key using the best eligible language/official rank for that key. No eligible candidate means `null`.
- **Verdict**: Approved (strict `site=YouTube` + `type=Trailer` + `^[A-Za-z0-9_-]{11}$` eligibility; rank by Q3 language tier first, `official=true` second within tier, and `key` ordinal ascending third; deduplicate by key keeping the best rank).
- **Basis & Rationale**:
  - **Ticket Scope & Authority (`specs/PRODUCT.md` §3; `DEV-463` Scope bullets 1–3)**: Ticket specifies *"preferring an official video of type Trailer"*, *"Order of preference: pt-BR trailer, original-language trailer with pt-BR subtitles enabled, then the original trailer as is"*, and *"Embed with a privacy-friendly player (youtube-nocookie.com)"*. Requiring `site=YouTube` and `type=Trailer` (case-insensitive) and validating `^[A-Za-z0-9_-]{11}$` enforces the Q1 boundary and prevents malformed/URL injection into the embed path.
  - **Taste & Tie-Break Rules (`specs/PRODUCT.md` §4; `recon-DEV-463-2` §6)**: Ordering language tier before `official=true` (so a non-official `pt-BR` Trailer beats an official `original_language` Trailer, while `official=true` wins within each language tier) and breaking remaining ties by ordinal `key` ascending is deterministic, independent of unstable TMDb array order or optional `published_at` timestamps, and logged `[assumed]` in `specs/DEV-463/ASSUMPTIONS.md`.
- **Implications**:
  - Non-YouTube sites (`Vimeo`, etc.), non-`Trailer` types (`Teaser`, `Clip`, `Featurette`, etc.), and keys not matching `^[A-Za-z0-9_-]{11}$` are filtered out before ranking; if no candidate remains, `TrailerKey` is `null`.
  - When the same `key` appears in both `language=pt-BR` and `language=<original_language>` responses, deduplication retains its highest-ranking `(languageTier, isOfficial)` tuple; equal `(languageTier, isOfficial)` ties are broken deterministically by `StringComparer.Ordinal` ascending on `key`.

## Q5/12 — Movie Details Trailer section UI, `youtube-nocookie.com` iframe embed, offline/LAN degradation copy, and CSP documentation

- **Question (Keel)**: Shall the UI use a small Trailer section in existing Movie Details, with fixed-host `youtube-nocookie` iframe and an always-visible adjacent note that internet access is required and pt-BR captions depend on the uploader? Recommendation: hide the entire section for `null`/empty/invalid keys; otherwise render a responsive 16:9 container, `loading="lazy"`, descriptive `title` (`Trailer for <movie title>`), `src="https://www.youtube-nocookie.com/embed/<trailerKey>?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR"`, and no `autoplay`. Copy: heading `Trailer`, adjacent note `Requires internet access. Portuguese captions depend on availability.` Keep other details usable when offline/blocked; never claim iframe `onLoad` or a timer proves third-party playability; no YouTube SDK, thumbnail fetch, or automatic retries. CSP: document the first third-party embed, its internet dependency, and required `frame-src https://www.youtube-nocookie.com` where deployment CSP is enforced, with no new global CSP header middleware or wildcard added in this ticket.
- **Verdict**: Approved (render `Trailer` section in `MovieDetailsView` only when `metadata?.trailerKey` matches `^[A-Za-z0-9_-]{11}$`; fixed `https://www.youtube-nocookie.com/embed/<key>?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR` iframe with `loading="lazy"`, `title="Trailer for <movie title>"`, responsive 16:9 container, no autoplay, always-visible local note `Requires internet access. Portuguese captions depend on availability.`, and PR/deployment CSP documentation for `frame-src https://www.youtube-nocookie.com` without adding global CSP middleware).
- **Basis & Rationale**:
  - **Ticket Scope & Acceptance (`specs/PRODUCT.md` §3; `DEV-463` Scope bullets 2–5 & Acceptance bullets 1–3)**: Ticket mandates `youtube-nocookie.com`, lazy-loaded (`loading="lazy"`), `title` attribute, responsive 16:9 container, no autoplay, `cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR`, hiding the section completely when no trailer is available, and noting the first third-party embed / CSP and LAN-only expectations in the PR while degrading cleanly when blocked.
  - **Charter §2.3 items 1, 2 & 6 (`specs/PRODUCT.md:41-48`; `recon-DEV-463` §§4–6)**: Plain `<iframe loading="lazy">` inside existing `web/src/features/movie-details/` requires no npm player SDK (item 1) and no new feature folder (item 2). Because neither `Program.cs` nor `docker/nginx.conf` defines a CSP today (`recon-DEV-463` §6), documenting the `frame-src https://www.youtube-nocookie.com` requirement and LAN/offline behavior in the PR (and spec/docs) satisfies the ticket's *"note it in the PR"* instruction without rewriting unmentioned host/nginx config files (item 6).
  - **Taste & Cross-Origin Honesty (`specs/PRODUCT.md` §4)**: Cross-origin iframe load events cannot reliably detect network blocks or caption tracks; keeping the 16:9 layout isolated so the rest of Movie Details stays interactive and showing the concise static note `Requires internet access. Portuguese captions depend on availability.` degrades cleanly without fake probes. Logged `[assumed]` in `specs/DEV-463/ASSUMPTIONS.md`.
- **Implications**:
  - Client-side guard validates `metadata?.trailerKey` against `^[A-Za-z0-9_-]{11}$` before rendering; if `null`, empty, or invalid, no `<section>` or empty box is rendered.
  - When valid, the section renders in `MovieDetailsView` with heading `Trailer`, iframe `title="Trailer for <movie title>"`, `loading="lazy"`, 16:9 aspect ratio styling, URL `https://www.youtube-nocookie.com/embed/${trailerKey}?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR`, and note `Requires internet access. Portuguese captions depend on availability.`

## Q6/12 — Loop discipline, closing bar, three-axis review requirement, frozen scope, and round caps

- **Question (Keel)**: Approve the loop discipline for this M ticket? Recommendation: closing bar zero unresolved Critical or High findings; all Medium/Low findings explicitly adjudicated and recorded, with valid out-of-scope concerns as follow-up issues (per Charter §2.3 out-of-scope ticket criteria). Require all three review axes (Sentry/Ledger/Compass) for adjudication regardless of pre-pass tier; code-review/ship-review closure never substitutes for required gates or runtime evidence. Frozen scope: TMDb trailer retrieval and Q1–Q4 selection, nullable metadata persistence and existing-details contract propagation, Q5 lazy privacy embed/degradation/CSP documentation, and ticket-required enrichment/component tests with corresponding regression checks; anything else is out of scope for this round. Default cap two analysis/fix rounds and two challenge/review/fix rounds per applicable loop; after cap, unresolved blockers are reported rather than silently waived or reset.
- **Verdict**: Approved (closing bar zero unresolved Critical/High, all Medium/Low explicitly adjudicated; all three review axes Sentry/Ledger/Compass mandatory for this Size M ticket; scope frozen to Q1–Q5 deliverables + ticket tests; cap of 2 analysis/fix rounds and 2 challenge/review/fix rounds per loop).
- **Basis & Rationale**:
  - **Ticket Size & Pipeline Rules (`DEV-463` note line 4: `Size: M`; `specs/PRODUCT.md` §1, §3)**: Size M requires all three review axes (`Sentry`, `Ledger`, `Compass`) and never substitutes review closure for mandatory static-analysis, test, mutation, or contract-drift gates.
  - **Charter §2.3 Standing Loop Discipline (`AGENTS.md` §2.3)**: Freezing scope to the Q1–Q5 rulings and ticket-mandated MSW component + enrichment tests prevents scope creep; out-of-scope Critical/High or broken-behaviour items become follow-up tickets via Rigger, and anything else is logged `noted, no ticket` in `CONCLUSIONS.md`.
- **Implications**:
  - Each loop (`speckit-analyze`, plan challenge, code review) is capped at 2 rounds; any blocker remaining after round 2 escalates rather than resetting or waiving gates.
  - No implementation, commit outside the spec branch, gate skip, or merge is authorized by loop closure.

## Q7/12 — Phase A/B boundary, dependency-ordered task sequence, test matrix, and verification gates

- **Question (Keel)**: Approve this test/gate/order plan? Recommendation: Phase A ends with `brief.md`, then Quill `spec.md`/`plan.md`/`tasks.md`, read-only `speckit-analyze` against `brief.md`, challenge/adjudication/freeze, and separate owner Gate 1; no implementation now. Phase B pickup checks drift against `main` before implementation. Task order: (1) domain/key copying and nullable EF storage/migration/readback; (2) TMDb retrieval/selection and chain merge/failure handling; (3) details DTO mapping and committed OpenAPI baseline; (4) regenerate TypeScript/MSW via existing generator (`npm run generate:api`); (5) integrate Trailer component/fixtures; (6) verify complete behavior and PR CSP/internet note. Tests cover ranking (including non-official `pt-BR` vs official original, missing `original_language`, duplicates/ties/order independence, wrong site/type/invalid key, partial/all video lookup failures and cancellation), OMDb-primary and TMDb-found flows, metadata copy/clear, real DB persistence roundtrip, old null rows, details JSON and OpenAPI drift, and MSW present/absent/invalid-key components plus player attributes/parameters, no autoplay, persistent note, and usable details under blocked network, using existing xUnit v3/Shouldly/NSubstitute, WireMock.Net, Testcontainers, and Vitest/RTL/MSW. Runtime acceptance observes a working embed and blocked-network layout in the provisioned browser (unavailable external access reported as `Could not run`, never `PASS`). C# and web gates run per harness and `web-implement` rules.
- **Verdict**: Approved (Phase A/B boundary, 6-step C# → OpenAPI → generated TS/MSW → web UI task order, unit/integration/component/runtime test matrix, and harness/web verification gates ratified).
- **Basis & Rationale**:
  - **Ticket Acceptance & Contract Chain (`specs/PRODUCT.md` §1, §3; `DEV-463` Scope & Acceptance; `AGENTS.md` Contract chain)**: Strictly ordering C# domain/EF/TMDb → DTO/OpenAPI (`web/src/api/openapi.json`) → `npm run generate:api` (`web/src/api/types.ts`, `web/src/mocks/handlers.ts`) → React component and MSW tests enforces the contract chain without hand-rolled types or handlers.
  - **Constitution Principle IX & API/Contract Rules (`.specify/memory/constitution.md:478-502, 544-592, 672-674`)**: Using existing xUnit v3 + Shouldly + NSubstitute, WireMock.Net for TMDb HTTP adapter tests, Testcontainers Postgres for nullable `trailer_key` persistence, Verify for OpenAPI drift, and Vitest + React Testing Library + MSW for web component tests satisfies Principle IX and the Charter ban on introducing second test/assertion/mock libraries.
  - **Gate Honesty (`AGENTS.md` Verification commands)**: Reporting unavailable external network access in runtime acceptance as `Could not run` (never `PASS`) and running all static-analysis, complexity, test, format, OpenAPI drift, and pre-PR mutation gates at their configured thresholds preserves pipeline integrity.
- **Implications**:
  - Phase A produces only spec/plan/tasks artifacts (`specs/DEV-463/`), `CONTEXT.md` update if part of spec PR or Phase B per task plan, and stops at `gate1: provisional` for owner PR review.
  - Phase B executes the 6-step sequence after checking drift against `main` and runs full C# and `web-implement` verification gates plus browser acceptance before PR.

## Q8/12 — Grill closure, bounded file plan (§2.3 item 6), glossary update (`CONTEXT.md`), and section placement

- **Question (Keel)**: Do we now share sufficient understanding to conclude this grill and let Keel write `brief.md` from Q1–Q7 with the bounded file plan? Modify existing Core `MovieMetadata` and necessary movie persistence entity/read projections and metadata-copy sites, TMDb adapter/mapping and `ChainedMetadataProvider`, EF repository/configuration plus a new nullable-column migration and tool-generated model snapshot, existing `LibraryResponses`/details mapping, OpenAPI baseline (`web/src/api/openapi.json`) and generated TypeScript/MSW (`web/src/api/types.ts`, `web/src/mocks/handlers.ts`), `MovieDetailsView`/CSS and a small Trailer component in `web/src/features/movie-details/` if needed, mocks fixtures (`web/src/mocks/fixtures.ts`), and corresponding existing test suites. Existing files are patched, not wholesale rewritten or deleted; no new project/top-level folder/layer/dependency, preferences surface, production CSP middleware, or `LocalPlay` behavior. Add `specs/DEV-463/` planning artifacts and canonical glossary term `Trailer key` in `CONTEXT.md`; no ADR proposed for this Size M ticket because it extends an established seam. Prefer section placement before `RelatedMovies`; log that taste assumption.
- **Verdict**: Approved (grill concluded at Q8/12; bounded file plan ratified under Charter §2.3 item 6; no ADR required; `Trailer key` appended to `CONTEXT.md`; section placement before `RelatedMovies` logged `[assumed]` in `specs/DEV-463/ASSUMPTIONS.md`; Keel is authorized to write `specs/DEV-463/brief.md`).
- **Basis & Rationale**:
  - **Charter §2.3 item 6 File Scope (`specs/PRODUCT.md:48`; `AGENTS.md` §2.3)**: Every modified tracked file is either explicitly required by the ticket Scope & Acceptance (TMDb `/movie/{id}/videos` adapter + supplement merge, `MovieMetadata` + EF persistence + migration, `MovieDetailsResponse`/`MovieMetadataResponse` + OpenAPI baseline + generated types/handlers, `MovieDetailsView` Trailer section + MSW fixtures/tests) or forced by the Constitution Principle VIII glossary gate (`CONTEXT.md`). Existing tracked files are patched minimally in place, never wholesale rewritten or deleted.
  - **Constitution Principle I & Documentation Rules (`.specify/memory/constitution.md:278-305, 473, 681-684`)**: Because `DEV-463` extends the existing `IMetadataProvider` / `MovieMetadata` seam without adding a port or changing an architectural choice, no ADR is needed; `Trailer key` is appended to `CONTEXT.md`.
  - **Taste Rules (`specs/PRODUCT.md` §4)**: Placing the Trailer section in `MovieDetailsView` immediately before `RelatedMovies` (and after the primary metadata/cast sections) is logged `[assumed]` in `specs/DEV-463/ASSUMPTIONS.md`.
- **Implications**:
  - Grill is complete at Q8/12; Keel writes `specs/DEV-463/brief.md` from Q1–Q8 and closes the grill.
  - Downstream stages (`speckit-specify`/`plan`/`tasks` by Quill, read-only `speckit-analyze`, 3-axis plan challenge, `gate1: provisional`, owner Gate 1, Phase B implementation and verification) remain separate.



## Challenge round 1 decision amendment — 2026-11-05

Authority: Patron challenge-round-1 ruling in CONCLUSIONS.md; all three findings-DEV-463-{Sentry,Ledger,Compass} notes. This amendment precedes downstream edits and refines Q1-Q8 without changing delivery scope.

- Trim original_language; pt and pt-BR are fetch-equivalent using OrdinalIgnoreCase, so their casing variants cause one pt-BR request. Language fields, site and type compare case-insensitively; pt-BR ranking still requires both pt and BR. Keys remain case-sensitive and ordinal. Selection remains over returned candidates only, without completeness guarantees.
- Videos-only 429/quota/timeout/transport/JSON failure contributes no candidates from that request, retains independently successful candidates, and causes no enrichment retry or DeadLetter. Details-path 429 keeps existing retry behavior. A failed TMDb supplement retains OMDb scalars with null Trailer key. Pass the same cancellation token to both requests, check before the second, and never swallow cancellation.
- The single C# key-validator method belongs on MovieMetadata, with exact 11-character ASCII letter/digit/underscore/hyphen checks (no mutable static regex); adapter and pure selector reuse it. Keep pure selection inside TmdbMetadataProvider.Mapping.cs and HTTP fetch in TmdbMetadataProvider.cs; no new abstraction or dependency. Web deliberately repeats validation at its render boundary.
- Add FsCheck permutation-invariance and dedupe-idempotence properties using existing infrastructure; no property-test opt-out for these invariants. New targeted test files in existing test projects are allowed. T001 confirms exact suites and supported OpenAPI/Verify workflow; no asserted env var or test-project path. Update details snapshots only through supported Verify workflow. T011 explicitly green-locks T010. Add hostile-title component coverage.
- CONTEXT.md lands in Phase B T003 with Anvil; T024 only verifies glossary presence. Preserve its pending edit now; Rigger excludes it from the spec PR and carries it forward. Rigger knowingly handles the intake pin (current intake receipt names .specify/feature.json), excludes _quill-report-bernstein.txt and transient reports, and reports the exact manifest. Patron ruling mentions alternate pin/report paths; these are not established inventory facts and do not authorize file creation/deletion.
- Retain local Phase N headings required by task-pipeline Phase 3 step 2; align local 8-9 to canonical 4 and local 10 to canonical 5. Do not run architect: task-pipeline Phase 3 step 4 assigns mutation to Gauge. Restore Q6 cap: two analysis/fix and two challenge/review/fix rounds per applicable loop, with no invented fix-commit cap; report unresolved blockers at cap.
- Sentry L4 iframe referrer/allow hardening: noted, no ticket per Q6; only the hostile-title test is adopted. No new iframe attribute requirement or deployment policy. No owner structural checkbox.

After application/read-back, PLAN FROZEN is planning closure only. Patron provisional Gate 1, Rigger spec PR, user merge, T000 receipt, pickup recon, implementation gates, runtime acceptance and delivery remain separate.
