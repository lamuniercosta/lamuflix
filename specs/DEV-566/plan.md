# Implementation Plan: Subtitle download-host fail-closed guard

**Branch** — `feature/566-spec` | **Date** — 2026-11-23 | **Worktree** — `F:/Dev/LamuFlix.worktrees/feature-566-spec`

**Status** — **PLAN FROZEN**. Ready for owner Gate 1 ruling. See [plan adjudication](PLAN_ADJUDICATION.md).

**Authority** — DEV-566 ticket (`ticket-text.txt`), notes `DEV-566` and `recon-DEV-566` (HEAD `c7d5fb025b3793680a2fdf5f1785553becf9343`), `brief.md`, `CONCLUSIONS.md` (Q1–Q8), `spec.md`, `specs/PRODUCT.md`, `.specify/memory/constitution.md`.

**Spec** — [spec.md](spec.md)

## Summary

Stop subtitle search from offering undownloadable OpenSubtitles candidates by validating an expected download host against the allowlist, latching a refused HTTPS download host in-process so later acquisition fails closed, correcting home-cluster generation, and adding a developer-only live check plus implement-skill workflow requirement.

## Technical context

- **Language/Version** — .NET 10 / C# (existing solution); PowerShell 7 for home-cluster and live-check scripts.
- **Primary Dependencies** — Existing ASP.NET Core options validation, typed `ISubtitleProvider` HttpClient (redirects disabled), xUnit v3 / Shouldly / NSubstitute / Testcontainers / WireMock as already used by subtitle tests. **No new NuGet/npm dependency.**
- **Storage** — N/A (process-local latch only; no schema).
- **Testing** — Existing unit and integration homes listed in Authorized project files; `scripts/home/Test-NewHomeCluster.ps1`; opt-in live script outside CI/tests.
- **Target Platform** — LamuFlix API process and home-cluster operator scripts.
- **Project Type** — Vertical-slice Web API + Infrastructure adapters + operator scripts.
- **Performance Goals** — Fail closed before provider calls on static invalid config and on latched refusal; zero file GET on refused HTTPS host.
- **Constraints** — Principle VII subtitle acquisition `ValidateOnStart` exception retained; secrets never printed; public routes/DTOs/status codes unchanged; suites network-isolated; `Features:LocalPlay` / production `Process.Start` untouched.
- **Scale/Scope** — Size M; five ticket deliverables + regressions + live receipt; two-round review cap (brief Q1).

## Constitution check

| Principle | Plan response |
|---|---|
| I — Ports and adapters | A concrete Core latch is shared by both acquisition handler guards and the existing OpenSubtitles adapter. The closed port list is unchanged. |
| II — Explicit handlers | Extend the existing `EnsureProviderConfigured` guard in each acquisition handler. |
| III — Typed query model | N/A. |
| IV — Domain transitions | N/A for this configuration/guard path. |
| V — Validated inputs and errors | Preserve existing `409` `provider_not_configured` and `502` `provider_unavailable` mappings; no new public status codes or DTO fields. |
| VI — Observability | One structured warning on the first refused host identifies the runtime trip and restart requirement. The latch and warning carry only the canonical host. The live script emits a sanitized receipt. |
| VII — Configuration | Extend existing `SubtitleProviderOptions` / validator under `Subtitles:OpenSubtitles`. Keep request-time acquisition validation (no `ValidateOnStart` for subtitle acquisition). No guessed numeric defaults. User-secrets for live key only. Preserve optional disabled-provider, existing sidecar serving, and read-only library precedence on download. |
| VIII — Ubiquitous language | Spec-local terms only; Core latch type avoids vendor naming; vendor path remains on options section / Infrastructure adapter / script names already authorized. No new `CONTEXT.md` term required for this draft. |
| IX — Test pyramid | Unit + isolated integration regressions; home-cluster script test; ArchitectureTests / analyzers / mutation as applicable; live check is developer opt-in, never CI. |
| API contracts and static-analysis gates | No OpenAPI/DTO/route change. Run applicable Size M gates with honest labels, including constitution Pull Request Quality Gates `dotnet format --verify-no-changes` and `LamuFlix.ArchitectureTests`. |

**Constitution assessment** — Plan complies with the existing Principle VII subtitle acquisition exception. Execution of repository gates is pending.

## Prerequisite — Gate 1

Bernstein coordinates the spec PR and Rigger prepares and opens it. The owner merges it and Keel records the verified merge receipt. Build tasks depend on that receipt.

## Approach

Aligned with `brief.md` Approach 1–5 and Q8:

1. **Options and static configuration (US1).** Add `DownloadLinkHost` (default `www.opensubtitles.com`) to `SubtitleProviderOptions`. Extend the existing validator to require a nonblank bare host and membership in `DownloadAllowedHosts`. Share trim and ordinal case-insensitive host matching with `SubtitleRequestUriPolicy` through a helper in Core Options. Reject URLs, userinfo, paths, queries, explicit ports and trailing-dot values as expected-host configuration. Accept existing host-only loopback forms. Preserve the runtime allowlist comparison semantics. Both handlers’ `EnsureProviderConfigured` guards map options validation failure to `SubtitleProviderNotConfiguredException` and `409` `provider_not_configured` before provider calls. Pin matching hosts in the loopback fixtures and preserve optional disabled-provider, sidecar and read-only library behavior.
2. **Refused-host state and acquisition guard (US2).** Register one dependency-free sealed Core latch as a process-local singleton. Use atomic instance-state first-write semantics (`Interlocked.CompareExchange` or `lock`) and retain only the canonical refused host. Inject it into both handlers and check it within their existing `EnsureProviderConfigured` guards in the acquisition branches, using `SubtitleProviderNotConfiguredException` for the existing `409` reason. Preserve early returns and precedence. Set the latch in the adapter before throwing the existing `502` provider failure and before any file GET on an absolute HTTPS response link with a non-allowlisted host. The winning write emits one structured host-only warning that identifies the runtime refusal and restart requirement. Keep readiness based on static options validation, including membership. Malformed or non-HTTPS links keep the existing refusal path. Disabled redirects remain disabled. A file GET redirect response follows the existing failure path, triggers no target request, and leaves the latch unset. Add no new retry or hedging around the file GET.
3. **Home-cluster settings (US3).** Generate the allowlist entries `api.opensubtitles.com` and `www.opensubtitles.com` and set `DownloadLinkHost` to `www.opensubtitles.com`. Assert both entries and exact expected-host equality in `Test-NewHomeCluster.ps1` without deploying. Cluster secret regeneration and redeploy follow owner merge of the delivery PR.
4. **Live checker and workflow (US4).** Require `-SettingsPath` pointing to a local, non-deployed settings JSON generated by the updated home-cluster script. Read only its two OpenSubtitles host settings and get the key exclusively from developer user-secrets. Free search is the default. `-Download` makes at most one unretried quota-charging request and checks HTTPS, expected-host equality, allowlist membership and a redirect-disabled HTTP 200 file GET. Output only mode, status, validated host, check outcomes and fixed pending reasons. Review all output and error paths. Update both implement-skill copies with the same live-check and receipt requirements, using the canonical source and regeneration if either copy is generated. A passing sanitized `-Download` receipt completes live acceptance.
5. **Verification and handoff.** After US1–US4 implementation (options/fakes → runtime → cluster → live script/skills): run isolated unit/integration regressions and the home-cluster script test; run applicable Size M repository gates including `dotnet format --verify-no-changes` and ArchitectureTests (may be via the full suite); then run the separate opt-in live `-Download` receipt against shipped settings. Keep Gate 1 and post-merge operator step (delivery-PR merge) distinct from pre-merge green checks. Record actual gate outcomes only when gates are run; drafting claims none.

## Authorized project files

| Area | Files and scope |
|---|---|
| Core options | `src/LamuFlix.Core/Options/SubtitleProviderOptions.cs`, `SubtitleProviderOptionsValidator.cs` and the shared host matcher in the same Options folder |
| Core acquisition | `SearchSubtitlesQueryHandler.cs` and `DownloadSubtitleCommandHandler.cs` under `src/LamuFlix.Core/Features/Subtitles/`, plus the concrete latch in that directory |
| Infrastructure adapter / DI | `src/LamuFlix.Infrastructure/Adapters/OpenSubtitlesSubtitleProvider.cs`, `SubtitleProviderServiceCollectionExtensions.cs` and `SubtitleRequestUriPolicy.cs` limited to reusing the shared host match |
| Home cluster | `scripts/home/New-HomeCluster.ps1`, `scripts/home/Test-NewHomeCluster.ps1` |
| Live check | `scripts/Test-LiveSubtitleProvider.ps1` (new) |
| Implement skills | `.claude/skills/implement/SKILL.md`, `.agents/skills/implement/SKILL.md` |
| Unit tests | `tests/LamuFlix.UnitTests/Options/SubtitleProviderOptionsValidatorTests.cs`; `SearchSubtitlesQueryHandlerTests.cs`, `DownloadSubtitleCommandHandlerTests.cs` and a focused latch unit suite in `tests/LamuFlix.UnitTests/Features/Subtitles/`; existing loopback/fake option fixtures owned by the authorized suites |
| Integration tests | `tests/LamuFlix.IntegrationTests/SubtitleAcquisitionOptionsCompositionTests.cs`, `tests/LamuFlix.IntegrationTests/OpenSubtitlesSubtitleProviderTests.cs`, `tests/LamuFlix.IntegrationTests/SubtitleSearchDownloadEndpointTests.cs`; use `SubtitleProviderHealthRegistrationTests.cs` / `SubtitleEndpointTests.cs` only where needed to preserve readiness, sidecar serving, read-only library precedence on download, and existing endpoint behaviour |

**Out of rewrite scope unless a cited Patron ruling adds them** — web SPA/toast files; CI workflows to invoke the live script; other provider check scripts; OpenAPI/DTO/route files; schema/migrations; unrelated subtitle options beyond `DownloadLinkHost` membership.

## Project structure (this feature)

```text
specs/DEV-566/
├── brief.md
├── CONCLUSIONS.md
├── ticket-text.txt
├── spec.md
├── plan.md              # this file
├── tasks.md             # /speckit-tasks
├── checklists/requirements.md
└── DRAFTING_RECEIPT.md
```

## Gate expectations

- Isolated unit + integration regressions for US1/US2 contracts (including non-host/`full URL` validator rejection; malformed/non-HTTPS without latch trip; isolated redirect regression; sidecar serving and read-only library precedence retained).
- `scripts/home/Test-NewHomeCluster.ps1` green for US3 assertions.
- Size M repository gates, including constitution Pull Request Quality Gates:
  - Roslyn analyzers, complexity (incl. refactor threshold when required), InspectCode
  - `dotnet format --verify-no-changes` (mandatory; name explicitly)
  - property tests, vulnerable packages
  - full tests with `LamuFlix.ArchitectureTests` execution explicit (may be via the full suite)
  - applicable mutation
  - three-axis review (Risk/Standards/Spec) with brief Q1 closing bar and two-round cap
- Separate opt-in live `-Download` receipt against shipped settings for **completed** acceptance; workflow pending-evidence reporting when check/credentials/quota are unavailable leaves acceptance pending (not a completed-acceptance alternative). Never required inside CI.
- Honest gate labels: never fold `SKIPPED` / `N/A` / `Could not run` into PASS; keep actual outcomes distinct; drafting claims no results.
- Post-merge operator (after owner merge of the delivery PR, itself after Gate 1 and implementation): regenerate cluster secret and redeploy with updated settings; separate receipt.

## Complexity tracking

> No constitution violations requiring justification.
