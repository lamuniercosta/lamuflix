# DEV-361 verification scenarios

`$files` is the 25-path list in `tasks.md` (FR-002).

1. Compare `.editorconfig` with `spec.md` FR-001. Exactly six target keys read `warning`, including `resharper_nullable_warning_suppression_is_used_highlighting` (owner B1 answer [x]). No other severity changed, and the comments at `:67,69,72,74` no longer describe these keys as downgraded.
2. Check that `git diff origin/main --stat` lists only `.editorconfig` and FR-002 paths. `FilmesFilterViewModel` is populated by the `[FromQuery]` MVC model binder (`FilmesController.cs:25`). Recheck assignment and binder evidence before suppressing any five-target hit.
3. Check that both equality sites keep their `null != qs.Value` guard and compare key/value fields. Check that the four legacy test conditions keep the non-null membership predicate.
4. For B1, every `NullableWarningSuppressionIsUsed` suppression is a type- or member-scoped `// ReSharper disable/restore` bracket with a one-line reason (FR-008). `git diff origin/main -- 'LamuFlix.Data/*.cs'` shows no property changed from non-nullable to nullable, no `DbSet` declaration change, and no migration. There is no `#nullable disable` and no `[SuppressMessage]`.
5. Run `./scripts/run-jetbrains-inspectcode.ps1 -BaseRef origin/main -Files $files` at the default WARNING level. Record numeric exit 0. The WorkerTests `:1`, `:11`, and `:75` hits are gone.
6. Run these in sequence, each with `-BaseRef origin/main -Files $files`:
   - `./scripts/run-roslyn-analyzers.ps1`;
   - `./scripts/run-cyclomatic-complexity.ps1` at 15;
   - `-Threshold 6`, which must exit 0 after the T019 `DynamicQuery` helper extraction; check the `--color-moved` diff shows a pure move;
   - `./scripts/run-jetbrains-inspectcode.ps1 -MinSeverity WARNING`.

   Then run `dotnet format --verify-no-changes` and `dotnet test`. Record numeric exits, findings, and any skip or opt-out separately. A skipped gate never counts as a pass.
