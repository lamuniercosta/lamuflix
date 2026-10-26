# Quickstart: DEV-330 Enrichment hub contract regeneration

Scope: regenerate the OpenAPI baseline (including the hub DTO components schema), Verify snapshot, and TypeScript types after Phase B implements the document transformer. Commands are Phase B only; Phase A does not regenerate.

Prerequisite: Phase B worktree with hub DTO + `EnrichmentHubSchemaTransformer` (or equivalent document transformer) registered. `<worktree>` is the named worktree root.

## OpenAPI baseline + Verify snapshot

Normal drift check (CI path; never writes):

```powershell
Set-Location <worktree>
dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter 'FullyQualifiedName~OpenApiContractTests'
```

Intentional regeneration (opt-in):

```powershell
Set-Location <worktree>
$env:LAMUFLIX_REGENERATE_OPENAPI = 'true'
dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter 'FullyQualifiedName~OpenApiRegeneration'
Remove-Item Env:\LAMUFLIX_REGENERATE_OPENAPI
```

## TypeScript + generated mocks

```powershell
Set-Location <worktree>/web
npm run generate:api
npm run check:api
```

Hub-only components schema appears in `types.ts`; no new MSW path handler is expected.

## Focused verification filters (after implementation)

```powershell
Set-Location <worktree>
dotnet test tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj --filter 'FullyQualifiedName~SignalREnrichmentStatusNotifier'
dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter 'FullyQualifiedName~EnrichmentHub|FullyQualifiedName~EnrichmentStatusNotification|FullyQualifiedName~ApiCors'
```

Use official `HubConnection` against the test host for hub framing proofs. Persistence transition proofs use real database/Testcontainers — never mock the data-access driver for those semantics.
