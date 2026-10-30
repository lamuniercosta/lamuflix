# Quickstart — DEV-17 Phase B validation

Prerequisites: Gate 1 merged; worktree on the build branch; `$env:PYTHONUTF8=1` when running Spec Kit scripts. Do not claim gates green from Phase A docs alone. Hosted CI `ffmpeg`/`ffprobe` availability is unproved — prove in T010 (pre-coding) and T061 (pre-PR) without skip/filter/workflow edits; stop for bounded recon + Patron if unavailable. Full suite / RequiresFfmpeg fail-if-missing remains.

## 1. Drift and dependency check

```powershell
Set-Location <build-worktree>
git fetch origin
# Verify DEV-304 / DEV-314 landing and baseline alignment against brief + constitution 1.8.0
# Re-run cross-artifact analysis before coding (brief Q9)
# Prove hosted-runner ffmpeg/ffprobe availability (T010); no skip/filter/workflow changes
```

## 2. Focused tests during implementation

```powershell
dotnet test --filter "FullyQualifiedName~Thumbnails" --nologo -v q
dotnet test --filter "FullyQualifiedName~ThumbnailOptions" --nologo -v q
dotnet test --filter "FullyQualifiedName~OptionsStartupValidation" --nologo -v q
dotnet test --filter "Category=RequiresFfmpeg" --nologo -v q
dotnet test tests/LamuFlix.ArchitectureTests --nologo -v q
```

RequiresFfmpeg must fail when ffmpeg/ffprobe are absent — never skip as acceptance passed. IntegrationTests `SystemProcessRunnerTests` require `pwsh`; missing `pwsh` fails loudly. Fake-runner UnitTests never require ffmpeg.

## 3. Docker image acceptance (AC1 + size/version receipt)

Dockerfile tooling (`T040`) before real-fixture execution (`T050`/`T051`). Single `RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*` before `USER app`.

```powershell
docker build -f docker/Dockerfile.api -t lamuflix-api:dev-17 .
# Override the image ENTRYPOINT (dotnet Api); do not pass ffmpeg/ffprobe as Api args
docker run --rm --user app --entrypoint ffmpeg lamuflix-api:dev-17 -version
docker run --rm --user app --entrypoint ffprobe lamuflix-api:dev-17 -version
# Capture exact version outputs + size delta vs comparable baseline; route receipt to Rigger for YouTrack DEV-17
```

## 4. Required gates (thresholds from harness.yml)

```powershell
./scripts/run-roslyn-analyzers.ps1
./scripts/run-cyclomatic-complexity.ps1
# Refactor gate: check -Help for supported parameters; enforce harness.yml gates.complexity.refactor
# Do not assume an unverified -Threshold flag or pin a numeric override in tasks
./scripts/run-cyclomatic-complexity.ps1   # use -Help-documented invocation against configured refactor threshold
./scripts/run-jetbrains-inspectcode.ps1
./scripts/run-property-tests.ps1
./scripts/run-vulnerable-packages.ps1
dotnet test
dotnet format --verify-no-changes
pwsh -NoProfile -File ./scripts/run-mutation.ps1      # no --since from worktrees; Infrastructure N/A; Core unchanged bar
```

Pre-PR (T061): re-prove hosted ffmpeg/ffprobe availability with the same no-skip/filter/workflow rules as T010 before treating full-suite/RequiresFfmpeg evidence as merge-bar ready.

Status vocabulary: exit 0 PASS; scope-empty exit 2 `SKIPPED (scope-empty)`; configured opt-out `SKIP`; mutation exclusion N/A; unrunnable `Could not run`. None of the last four is PASS.

## 5. Review bar

Complete Sentry, Ledger, and Compass reports against the same HEAD as gate evidence. Closing bar: zero unresolved Critical/High/Medium; every Low dispositioned; two-round remediation cap (brief Q10).
