# Quickstart: DEV-312 OpenAPI Regeneration

Scope: the one explicit local step that regenerates the authoritative
`web/src/api/openapi.json` plus the derived Verify snapshot
(`tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`) together
(brief Q4; tasks.md T015–T016). This is the only legitimate way to change
the baseline; every other difference is drift and fails the test.

Prerequisite: the Phase B worktree with Phase 1–3 implemented. Paths below
are explicit: `<worktree>` is the named worktree root. File resolution inside
the test uses the repository-root anchor named in plan.md (stable
`Directory.Packages.props` marker — never the generated baseline; fail closed
on a missing/ambiguous marker), so the commands work regardless of the shell's
working directory. A missing baseline or snapshot file fails the normal drift
check; only the explicit opt-in regeneration below may create the approved
output files inside the verified root.

Normal drift check (no switch, never writes; this is what CI runs):

```powershell
Set-Location <worktree>
dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter 'FullyQualifiedName~OpenApiContractTests'
```

Intentional local regeneration (explicit opt-in; skipped unless the switch
is set; uses the same Development host, `http://localhost/` client base URI,
and document path as the drift
test; writes the identical normalized bytes to `openapi.json` plus the derived snapshot together for human
review in one commit):

```powershell
Set-Location <worktree>
$env:LAMUFLIX_REGENERATE_OPENAPI = 'true'
dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter 'FullyQualifiedName~OpenApiRegeneration'
Remove-Item Env:\LAMUFLIX_REGENERATE_OPENAPI
```

Review surface: the `web/src/api/openapi.json` diff. CI never sets
`LAMUFLIX_REGENERATE_OPENAPI`. `docs/adr/ADR-0007.md` (Keel-owned)
references this command.
