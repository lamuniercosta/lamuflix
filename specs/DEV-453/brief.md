# DEV-453 — Subtitle provider and sidecar planning brief

## Status and authority

Phase 2 grill closed with Patron at Q12/12 on 2026-11-09. This is the authoritative planning handoff for Quill, not spec approval, PLAN FROZEN, Gate 1, implementation pickup, runtime acceptance or merge clearance. No structural blocker or constitution departure remains from the grill. Quill drafts spec.md, plan.md and tasks.md; Keel subsequently analyzes them read-only and conducts plan challenge.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-453-spec`; branch: `feature/453-spec`; recorded base: `2ced085e21f2916c035d13de0d281716d823ed2f`. ADR 0038 was assigned by Bernstein and recorded in task/chain notes.

Authority: task note DEV-453 scope; unified recon-DEV-453 lines 1–21; Patron Q1–Q12, reproduced in full in CONCLUSIONS.md. Patron cites specs/PRODUCT.md §§3/5, the charter and ADR 0037 in the rulings. Those citations are recorded rulings, not independent recon by Keel. Earlier recon revisions were replaced; historical line references in the transcript must not be mistaken for current line numbers.

## Frozen scope and loop discipline

Ship one verified subtitle provider, ordered language preferences, BS Player-compatible sidecar names, writable daily-driver saving and existing-track serving from the read-only demo library. Evaluate BS Player hash service first. Deliver backend preparation/serving, generated API contracts, configuration, setup documentation and tests under Q3–Q10; anything else is a follow-up issue, not a finding in this round.

High and Medium findings block. Security, public-contract or ticket failures block regardless of label. Every Low needs an explicit cited disposition: fix, accepted nonblocking risk, or follow-up. Size L requires all three reports from Sentry, Ledger and Compass; findings and summary must be published on the PR and pinned to the exact reviewed head. Two review/remediation rounds maximum; unresolved blockers then go to Bernstein for a deliberate next-step decision, never automatic clearance. Grill cap was 12 questions and was reached. Patron approved these terms in Q11.

## Vocabulary

- **Ordered language preferences:** explicit server-configured provider-language codes in selection priority order; no invented default language.
- **Selected subtitle:** compatible SRT in the first configured preferred language that has an eligible result.
- **Sidecar:** subtitle alongside the primary video, with its exact basename: `Film.mkv` → `Film.srt`.
- **Preparation:** explicit per-movie acquisition/save operation, separate from byte-range video streaming.
- **Existing track:** an authorized exact-basename SRT already on library storage; its language may be unknown.

This feature-local glossary is settled for Quill; no root glossary rewrite is authorized.

## Provider and selection

BS Player was evaluated first; unified recon line 13 found no public supported hash-service API contract. Do not integrate it or claim its endpoint is dead. Patron Q3 selected OpenSubtitles.com REST as the sole provider for personal non-commercial media-player use, based on official staff endorsement and documented integration (recon lines 17–21). This satisfies the planning prerequisite from Q1/Q2; it is not a live successful download or a blanket subtitle-content license. No redistribution rights are inferred.

Use an injected application Api-Key and descriptive application/version User-Agent. Anonymous allowance is documented as 5 downloads/24h/IP; verify current key settings and responses at runtime. Do not cache temporary download URLs or rely on a fixed URL lifetime: unified recon line 21 explicitly limits that evidence. No automatic download retries or invented local quota reset counters. Propagate safe upstream Retry-After on quota responses.

Look up eligible moviehash first, falling back to title only when hash is ineligible or yields no match within preferred languages. Q8 adopts the 16-hex-character size + first/last 65,536-byte algorithm for videos at least 131,072 bytes, with authoritative reference-vector verification required at Phase B pickup before implementing it. These algorithm values describe a protocol, not configurable safety thresholds.

Choose the first matching preferred language, then moviehash match first, highest download_count, lowest subtitle file_id. Never fall back outside preferences. Multiple candidates are resolved deterministically, not rejected merely for a tie. Download compatible SRT only; no eligible result is noMatch. Preserve existing sidecars without overwrite or another download. Language suffixes are not established BS Player autodetection and are not used. The single exact-basename SRT represents one selected language per video.

## Trigger, settings and deployment

Explicit per-movie preparation only; ordinary streaming never initiates provider traffic. Backend-only scope was deliberately ratified in Q5; a web workflow is a follow-up. Existing sidecar checks occur after movie/video/library authorization but before acquisition configuration or write checks, allowing alreadyPresent even with saving disabled, a read-only mount, or no Api-Key.

Server options:

| Setting | Decision |
| --- | --- |
| Subtitles:SaveEnabled | Defaults false; writable acquisition explicitly enabled |
| Subtitles:PreferredLanguages | Empty by default; ordered provider codes required for acquisition |
| Subtitles:OpenSubtitles:ApiKey | Secret injection only; never committed or logged |
| Subtitles:OpenSubtitles:BaseUrl | Explicit validated provider API URL |
| Subtitles:OpenSubtitles:UserAgent | Explicit descriptive application/version value |
| DownloadAllowedHosts, MaxDownloadBytes, RequestTimeout | Explicit acquisition options; allowlist and positive bounds validated; Quill nests under the provider options section |

No guessed numeric safety defaults. Missing/invalid acquisition settings yield an actionable disabled/unconfigured 409 on preparation, without preventing demo startup or existing-track serving. Read-only demo makes no acquisition calls or writes. Daily-driver setup documents a deliberately writable library mount/config without committing machine-specific paths or credentials. Existing-track serving remains bounded and read-only.

## Public contract

Contract chain is C# DTO → web/src/api/openapi.json → generated web/src/api/types.ts, even with no frontend workflow. Never hand-write duplicate TS types.

| Operation | Approved result |
| --- | --- |
| POST /movies/{id:int}/subtitles/prepare | No client path/provider URL or language override; server preferences apply. 200 body `{ outcome: saved\|alreadyPresent\|noMatch, selectedLanguage: string? }`; language populated on saved when known, null for alreadyPresent/noMatch |
| GET /movies/{id:int}/subtitles | Authorized exact-basename SRT bytes, 200 application/x-subrip; no provider traffic |

Prepare failures: 400 malformed ID, 403 library authorization/containment denial, 404 movie/video absent, 409 acquisition disabled/unconfigured/unwritable or local write-permission failure, 429 upstream quota, 502 provider/content/unsafe-URL failure, 504 provider timeout. GET returns 404 for missing movie/video/sidecar and 403 for unauthorized library access. Q10 refines Q6: preparation write-permission/read-only failures are 409; containment denials stay 403. Errors disclose no filesystem paths or secrets. Quill must encode the declared malformed-ID behavior explicitly rather than rely on route-constraint defaults.

## Architecture and file responsibilities

Existing Core, Infrastructure and Api projects only; no new NuGet/npm dependencies, schema changes, top-level folder, project or architectural layer. Two narrow Core ports: `ISubtitleProvider` and `ISubtitleSidecarStore`. Keep `ILibraryFileReader` read-only per ADR 0037. No generic provider registry/repository or unrelated refactor. ADR 0038 records the provider and separate read/write boundary.

Planned responsibility map (new filenames are design targets, not claims of existing code):

- `src/LamuFlix.Core/Ports/ISubtitleProvider.cs`, `ISubtitleSidecarStore.cs`: narrow provider/storage contracts; subtitle models and orchestration follow existing Core conventions.
- `src/LamuFlix.Infrastructure/`: OpenSubtitles HTTP/hash adapter; `FileSystem/` sidecar adapter with contained reads and atomic saves.
- `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs`: approved routes, response DTO/options and composition following existing conventions.
- Existing DI/endpoint mapping entrypoints: names verified by scoped recon at Phase B pickup before edits; no guessed filename rewrites.
- Configuration, Compose and README: explicit read-only/writable setup and injected secrets; retain safe demo defaults.
- `web/src/api/openapi.json`, `web/src/api/types.ts`: contract regeneration only.
- Existing UnitTests/IntegrationTests projects: focused new tests and bounded existing-test edits.

Local subtitle HTTP preparation/serving is independent of Features:LocalPlay. No Process.Start, player arguments or launch-path changes. Actual local-player execution remains gated behind Features:LocalPlay. These are deliberate Q7 care-item rulings, not an exemption for future launch work.

## Safety and failure behavior

Authorize resolved primary video folder and regular file before hashing, reading or writing; server derives the exact sidecar path. Reject traversal, reparse/symlink escapes and nonregular files. Same-directory temporary files, cancellation cleanup and atomic create-without-replace avoid partial sidecars and overwrite. A concurrent-create collision resolves to alreadyPresent after safe recheck.

Validate provider download URLs as HTTPS on an explicit provider-host allowlist; reject redirects outside it and caller-controlled URLs. Verify allowed hosts during pickup. Enforce configured size/time bounds; reject non-SRT/invalid/oversized downloads. No automatic download retry. Never persist temporary URLs or log keys, paths or subtitle content. Bound serving resources as well. Follow existing TimeProvider/seeded Random and harness conventions wherever applicable.

## Task ordering, test strategy and gate expectations

1. After Gate 1, Phase B pickup: recon merged/delivered DEV-457 and DEV-454 contracts, actual composition filenames, current provider headers/error schema and safe download-host allowlist; verify authoritative hash vectors. Amend drift before implementation. Queued dependencies are not proof of delivery.
2. Define approved DTO/options, models and the two narrow ports; validate configuration without breaking demo startup.
3. Implement/test hash and deterministic language selection; then HTTP provider adapter using existing WireMock.NET.
4. Implement/test contained sidecar reads and atomic writes using real temporary filesystem fixtures; then orchestration and API integration.
5. Regenerate OpenAPI/TS, document demo/writable setup and run applicable verification gates.
6. Collect separate live configured-key, writable-save, BS Player autodetection and read-only-serving runtime receipts; report absent receipts honestly.

Use existing xUnit/FsCheck/WireMock.NET conventions and packages only. Cover preference order/ties/noMatch; hash vectors/boundaries; existing-sidecar precedence over missing key/disabled writes; read-only no-network/no-write; ordinary writable save; concurrent creates/no overwrite/cancellation cleanup; containment/reparse/nonregular denial; oversized/invalid content; URL/redirect rejection; provider auth/quota/timeout/error mapping; exact served bytes and playback regressions. No real credentials in fixtures; no driver mocks for translation/serialization-dependent behavior.

Implementation gates: all applicable harness build/analyzer/complexity/inspection/test/property/security/format/mutation gates, with thresholds from harness.yml and no lowering; OpenAPI/TS drift and relevant web checks for generated contract diff. Preserve PASS, SKIPPED (scope-empty), SKIP, N/A and Could not run distinctions. This planning-only commit does not claim those implementation gates ran.

Runtime acceptance needs actual provider search/download using a configured key, successful writable sidecar save, supported BS Player autodetection of exact-basename SRT, and read-only demo serving an existing sidecar without acquisition. Live checks that cannot run are Could not run, never PASS. Automated tests and documented viability do not substitute for runtime evidence.

## Complete grill decision index

Full question and answer text, cited bases, rationale and implications are append-only in CONCLUSIONS.md; that companion is part of this brief, not optional background.

| Question | Settled answer |
| --- | --- |
| Q1 | Preserve ticket scope; provider verification required; initial recon stop |
| Q2 | Uphold stop pending auth/quota/terms and exact filename evidence |
| Q3 | Documentation/staff evidence satisfies planning prerequisite; OpenSubtitles selected; live verification separate |
| Q4 | Exact-basename SRT, configured language order, preserve existing, deterministic tie-break amendment |
| Q5 | Explicit prepare/read operations, backend-only, separate writable/read-only modes |
| Q6 | Approved routes/statuses/options/contract chain; selectedLanguage and existing-sidecar precedence amendments |
| Q7 | Two named narrow ports, no dependencies/schema/new projects, bounded file scope, LocalPlay separation |
| Q8 | Containment/atomic save/URL bounds/quota hygiene; hash-first with limited title fallback |
| Q9 | Task/test/gate ordering; technical verification at Phase B pickup, runtime receipts separately |
| Q10 | Safe settings, bounded responsibilities/serving, 409 write-denial refinement |
| Q11 | Blocking bar, three-axis exact-head publication, frozen scope, two review rounds |
| Q12 | Shared understanding, grill closed; four-file handoff approved, no structural departure |

ASSUMPTIONS.md records the policy/taste choices tagged [assumed]. Quill must use this vocabulary and proposed ADR 0038 consistently. New decision changes are recorded in this brief before any corresponding spec/plan/tasks repair.
