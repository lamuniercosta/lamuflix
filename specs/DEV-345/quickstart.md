# Quickstart: DEV-345

Verification map for the port and the test generator. This drafting pass did not run these commands and claims no pass.

Work from `F:/Dev/LamuFlix.worktrees/feature-345-spec` on `feature/345-spec`.

## What to exercise

1. Startup validation accepts a generator that reports dimension 384 and rejects a different integer, missing metadata, a null dimension, and a null generator. It does not generate a vector.
2. The test generator returns length-384 vectors with no model, network, or database. Same text matches across calls and instances. Representative different texts differ. Batches, empty inputs, missing inputs, requested length, cancellation, the reported-dimension override, service lookup, and disposal match `spec.md` US2.
3. Architecture tests still pass after `Microsoft.Extensions.AI` is added to the feature-dependency allowlist, and unrelated dependency rules are unchanged.

## Commands

From the brief and the constitution static-analysis gates:

```powershell
dotnet test --filter "FullyQualifiedName~EmbeddingDimensions|FullyQualifiedName~FakeEmbeddingGenerator" --nologo -v q
./scripts/run-roslyn-analyzers.ps1
./scripts/run-cyclomatic-complexity.ps1
./scripts/run-cyclomatic-complexity.ps1 -Threshold 6
./scripts/run-jetbrains-inspectcode.ps1
./scripts/run-property-tests.ps1
./scripts/run-vulnerable-packages.ps1
dotnet format --verify-no-changes
dotnet test
pwsh -NoProfile -File ./scripts/run-mutation.ps1
```

Mutation is pre-PR, uses no `since` switch from this worktree, and covers eligible changed Core code. Keep `PASS`, `SKIPPED (scope-empty)`, opt-out `SKIP`, mutation `N/A`, and `Could not run` distinct. Do not lower a threshold.

Exception types, the hash layout, the `1e-6` norm, and `GetService` order are the brief rulings. A mismatch with those rulings is a product bug. `CLAUDE.md` is not part of this check.
