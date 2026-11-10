# Quickstart: Library bulk import (DEV-487)

## Backend

```powershell
dotnet build LamuFlix.sln
dotnet test tests/LamuFlix.UnitTests
dotnet test tests/LamuFlix.IntegrationTests   # Testcontainers; needs Docker
```

## Scan flow (Compose)

```powershell
docker compose up -d
# candidates
Invoke-RestMethod http://localhost:3000/api/movies/import/candidates
# import all + retry failed
Invoke-RestMethod -Method Post http://localhost:3000/api/movies/import/library `
  -ContentType 'application/json' -Body '{"folders":[],"retryFailed":true}'
```

## Web

```powershell
cd web
npm run generate:api   # after endpoint changes; never hand-edit api/openapi.json or types.ts
npm run check:api
npm run lint
npm run test:run
npm run build
```

## Gates (repo root)

```powershell
./scripts/run-roslyn-analyzers.ps1
./scripts/run-cyclomatic-complexity.ps1
./scripts/run-jetbrains-inspectcode.ps1
./scripts/run-web-gates.ps1
dotnet test
```
