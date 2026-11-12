# Quickstart: DEV-501 subtitle search and download

Worktree: `F:/Dev/LamuFlix.worktrees/feature-501-spec` · branch `feature/501-spec`.

Status note: PLAN FROZEN per `PLAN_ADJUDICATION.md`; Gate 1 not approved; implementation prerequisites still apply; runtime receipts OPEN.

## Prerequisites

1. Phase B: CC panel ticket and Subs discovery ticket ready on the integration base; amend tasks if drift requires authorized changes. Verify current source paths at pickup (recon inventories incomplete).
2. OpenSubtitles Api-Key / BaseUrl / UserAgent via user-secrets or env (never commit).
3. Language catalog: `GET /api/subtitles/languages` supplies bilingual choices; `Subtitles:PreferredLanguages` may be empty (choose-language, no auto-search) or include the language under test (e.g. `pt-BR`).
4. Writable daily-driver: `Subtitles:SaveEnabled=true` and a writable library root; read-only demo: `SaveEnabled=false`.
5. Typed bounds (defaults): `CandidateTokenTtl=00:05:00`, `CandidateStoreMaxEntries=1000`, `MaxSubtitleDownloadBytes=2097152`, `MaxSubtitlePublicationAttempts=10000`.

## Local API checks

```powershell
# Catalog (key-free)
Invoke-RestMethod "https://localhost:<api-port>/api/subtitles/languages"

# Search (free)
Invoke-RestMethod "https://localhost:<api-port>/api/movies/<id>/subtitles/search?language=pt-BR"

# Download (paid; use candidateId from search only)
Invoke-RestMethod -Method Post "https://localhost:<api-port>/api/movies/<id>/subtitles/download" -ContentType "application/json" -Body '{"candidateId":"<id>"}'

# Prepare must be gone
try { Invoke-WebRequest -Method Post "https://localhost:<api-port>/api/movies/<id>/subtitles/prepare" } catch { $_.Exception.Response.StatusCode }  # expect NotFound
```

Expect RFC 7807 ProblemDetails with `traceId` on errors. FluentValidation decorator failures → 422 `invalid_request`. Malformed binding / stale token domain prechecks → 400. GET search rate-limit → 429 `search_rate_limited`. Known download quota zero → 429 `download_quota_exhausted`.

## UI path

1. Open a movie in the browser player.
2. Open CC → Subtitles tab.
3. Confirm bilingual language default from catalog / choose-language prompt when preferences empty.
4. Confirm local tracks + Off above results; active check only on the active local track; quota line truthful.
5. Get one candidate; confirm Subs file `<stem>-<code>.srt` (or `(N)`), active check on local track, uninterrupted video.
6. Toggle SaveEnabled false: search works, Get disabled with reason, no POST.
7. Confirm distinct copy for search rate-limit vs download quota exhaustion.

## Verification commands

```powershell
dotnet test --filter "FullyQualifiedName~Subtitle" --nologo -v q
./scripts/run-roslyn-analyzers.ps1
./scripts/run-cyclomatic-complexity.ps1
./scripts/run-jetbrains-inspectcode.ps1
./scripts/run-property-tests.ps1   # mandatory applicable for SubtitleMatchScorerPropertyTests
cd web; npm run check:api; npm run test:run; npm run lint
```

Discover installer/snapshot approval commands from the current checkout before prepare/harness regeneration (do not assume `scripts/install.ps1`). Report `SKIPPED (scope-empty)`, `SKIP`, mutation `N/A`, and `Could not run` distinctly from PASS. Home-cluster / BS Player receipts stay OPEN until that prerequisite executes.
